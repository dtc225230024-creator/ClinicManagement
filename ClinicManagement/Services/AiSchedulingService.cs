using ClinicManagement.Data;
using ClinicManagement.Models;
using ClinicManagement.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ClinicManagement.Services;

public class AiSchedulingService(ClinicStore store, ClinicDbContext db, IMemoryCache cache)
{
    private const string AiSymptomRuleCacheKey = "AiSchedulingService.SymptomRules.v2";
    private const string GeneralDepartmentName = "Nội tổng quát";

    private static readonly string[] Slots =
    [
        "08:00-08:30", "08:30-09:00", "09:00-09:30", "09:30-10:00",
        "10:00-10:30", "10:30-11:00", "13:30-14:00", "14:00-14:30",
        "14:30-15:00", "15:00-15:30", "15:30-16:00"
    ];

    private static readonly HashSet<string> PeakSlots =
    [
        "08:00-08:30",
        "09:00-09:30",
        "13:30-14:00"
    ];

    public IEnumerable<Department> SuggestDepartments(string reason)
    {
        var scores = ScoreDepartments(reason);
        if (scores.Count == 0)
        {
            return store.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.DepartmentName);
        }

        return store.Departments
            .Where(d => d.IsActive)
            .OrderByDescending(d => scores.TryGetValue(d.DepartmentId, out var score) ? score : 0)
            .ThenByDescending(d => scores.ContainsKey(d.DepartmentId))
            .ThenBy(d => d.DepartmentName);
    }

    public IReadOnlySet<int> GetSuggestedDepartmentIds(string reason)
    {
        return ScoreDepartments(reason).Keys.ToHashSet();
    }

    public void ClearSymptomRuleCache()
    {
        cache.Remove(AiSymptomRuleCacheKey);
    }

    public IReadOnlyDictionary<int, int> ScoreDepartments(string reason)
    {
        var tokens = VietnameseTextNormalizer.Tokenize(reason).ToList();
        if (tokens.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        var departments = store.Departments
            .Where(d => d.IsActive)
            .ToList();
        var activeDepartmentIds = departments
            .Select(d => d.DepartmentId)
            .ToHashSet();
        var scores = new Dictionary<int, int>();
        foreach (var rule in GetCachedSymptomRules().Where(x => activeDepartmentIds.Contains(x.DepartmentId)))
        {
            if (!ContainsTerm(tokens, rule.TermTokens))
            {
                continue;
            }

            scores[rule.DepartmentId] = scores.GetValueOrDefault(rule.DepartmentId) + rule.Score;
        }

        var generalDepartmentId = departments
            .FirstOrDefault(d => VietnameseTextNormalizer.Normalize(d.DepartmentName) == VietnameseTextNormalizer.Normalize(GeneralDepartmentName))
            ?.DepartmentId;
        if (scores.Count == 0 && generalDepartmentId is not null)
        {
            scores[generalDepartmentId.Value] = 1;
        }

        return scores;
    }

    public IEnumerable<TimeSlotSuggestion> SuggestTimeSlots(DateTime desiredDate, int? departmentId = null)
    {
        return GetSlotAvailability(desiredDate.Date, departmentId)
            .Where(x => x.AvailableDoctorCount > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => ParseStart(x.TimeSlot))
            .Take(6)
            .Select(x => new TimeSlotSuggestion
            {
                TimeSlot = x.TimeSlot,
                AvailableDoctorCount = x.AvailableDoctorCount,
                BusyDoctorCount = x.BusyDoctorCount,
                Score = x.Score,
                Recommendation = BuildRecommendation(x.AvailableDoctorCount, x.BusyDoctorCount, x.IsPeakHour)
            });
    }

    public IEnumerable<AppointmentSuggestion> SuggestAppointments(
        int departmentId,
        DateTime desiredDate,
        int? ignoredAppointmentId = null,
        string? preferredTimeSlot = null,
        bool includeNearbyDates = true)
    {
        var doctorLookup = store.Doctors
            .Where(d => d.IsActive && d.DepartmentId == departmentId)
            .ToDictionary(d => d.DoctorId);

        if (doctorLookup.Count == 0)
        {
            return [];
        }

        var departmentName = store.Departments
            .Where(d => d.DepartmentId == departmentId)
            .Select(d => d.DepartmentName)
            .FirstOrDefault() ?? string.Empty;

        var windowStart = desiredDate.Date;
        var windowEnd = includeNearbyDates ? desiredDate.Date.AddDays(3) : desiredDate.Date;
        var schedules = store.Schedules
            .Where(s =>
                s.IsActive &&
                doctorLookup.ContainsKey(s.DoctorId) &&
                s.WorkDate.Date >= windowStart &&
                s.WorkDate.Date <= windowEnd)
            .ToList();

        if (schedules.Count == 0)
        {
            return [];
        }

        var appointments = store.Appointments
            .Where(a =>
                a.AppointmentId != ignoredAppointmentId &&
                doctorLookup.ContainsKey(a.DoctorId) &&
                a.AppointmentDate.Date >= windowStart &&
                a.AppointmentDate.Date <= windowEnd &&
                a.Status != AppointmentStatus.Cancelled)
            .ToList();

        var dailyLoad = appointments
            .GroupBy(a => new { a.DoctorId, Date = a.AppointmentDate.Date })
            .ToDictionary(g => (g.Key.DoctorId, g.Key.Date), g => g.Count());

        var slotAvailability = Enumerable.Range(0, 4)
            .SelectMany(offset =>
            {
                var date = windowStart.AddDays(offset);
                return GetSlotAvailability(date, departmentId, ignoredAppointmentId)
                    .Select(x => new { Date = date, Snapshot = x });
            })
            .ToDictionary(x => (x.Date, x.Snapshot.TimeSlot), x => x.Snapshot);

        var candidates =
            from schedule in schedules
            let doctor = doctorLookup[schedule.DoctorId]
            from slot in Slots
            where SlotFitsSchedule(slot, schedule) &&
                  (string.IsNullOrWhiteSpace(preferredTimeSlot) || slot == preferredTimeSlot) &&
                  store.IsSlotAvailable(doctor.DoctorId, schedule.WorkDate, slot, ignoredAppointmentId)
            let distance = Math.Abs((schedule.WorkDate.Date - desiredDate.Date).Days)
            let dayLoad = dailyLoad.TryGetValue((doctor.DoctorId, schedule.WorkDate.Date), out var load) ? load : 0
            let availability = slotAvailability[(schedule.WorkDate.Date, slot)]
            select new AppointmentSuggestion
            {
                DoctorId = doctor.DoctorId,
                DoctorName = doctor.FullName,
                DepartmentName = departmentName,
                Date = schedule.WorkDate.Date,
                TimeSlot = slot,
                Score = 100
                    - distance * 12
                    - dayLoad * 4
                    + availability.AvailableDoctorCount * 6
                    - (availability.IsPeakHour ? 6 : 0)
            };

        return candidates
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Date)
            .ThenBy(x => x.TimeSlot)
            .Take(8);
    }

    public static bool IsKnownSlot(string? slot) => !string.IsNullOrWhiteSpace(slot) && Slots.Contains(slot);

    private IEnumerable<SlotAvailabilitySnapshot> GetSlotAvailability(DateTime desiredDate, int? departmentId, int? ignoredAppointmentId = null)
    {
        var doctorIds = store.Doctors
            .Where(d => d.IsActive && (departmentId is null || d.DepartmentId == departmentId.Value))
            .Select(d => d.DoctorId)
            .ToHashSet();

        if (doctorIds.Count == 0)
        {
            return [];
        }

        var schedules = store.Schedules
            .Where(s =>
                s.IsActive &&
                doctorIds.Contains(s.DoctorId) &&
                s.WorkDate.Date == desiredDate.Date)
            .ToList();

        if (schedules.Count == 0)
        {
            return [];
        }

        var bookedBySlot = store.Appointments
            .Where(a =>
                a.AppointmentId != ignoredAppointmentId &&
                doctorIds.Contains(a.DoctorId) &&
                a.AppointmentDate.Date == desiredDate.Date &&
                a.Status != AppointmentStatus.Cancelled)
            .GroupBy(a => a.TimeSlot)
            .ToDictionary(g => g.Key, g => g.Select(a => a.DoctorId).ToHashSet());

        return Slots
            .Select(slot =>
            {
                var workingDoctorIds = schedules
                    .Where(schedule => SlotFitsSchedule(slot, schedule))
                    .Select(schedule => schedule.DoctorId)
                    .Distinct()
                    .ToHashSet();

                if (workingDoctorIds.Count == 0)
                {
                    return null;
                }

                bookedBySlot.TryGetValue(slot, out var bookedDoctorIds);
                bookedDoctorIds ??= [];

                var busyDoctorCount = bookedDoctorIds.Count(workingDoctorIds.Contains);
                var availableDoctorCount = Math.Max(0, workingDoctorIds.Count - busyDoctorCount);
                var isPeakHour = PeakSlots.Contains(slot);
                var proximityPenalty = (int)(Math.Abs(ParseStart(slot).Ticks - new TimeSpan(9, 30, 0).Ticks) / TimeSpan.FromMinutes(30).Ticks);
                var score = availableDoctorCount * 20 - busyDoctorCount * 9 - (isPeakHour ? 6 : 0) - proximityPenalty;

                return new SlotAvailabilitySnapshot(slot, availableDoctorCount, busyDoctorCount, isPeakHour, score);
            })
            .Where(x => x is not null)
            .Select(x => x!);
    }

    private static bool SlotFitsSchedule(string slot, WorkSchedule schedule)
    {
        var start = ParseStart(slot);
        var end = start.Add(TimeSpan.FromMinutes(30));
        return start >= schedule.StartTime && end <= schedule.EndTime;
    }

    private static string BuildRecommendation(int availableDoctorCount, int busyDoctorCount, bool isPeakHour)
    {
        if (availableDoctorCount >= 3)
        {
            return isPeakHour
                ? "Nhiều lựa chọn, nhưng đang là giờ cao điểm."
                : "Nhiều bác sĩ đang trống ở khung giờ này.";
        }

        if (availableDoctorCount == 2)
        {
            return busyDoctorCount == 0
                ? "Khung giờ thông thoáng và còn nhiều lựa chọn."
                : "Còn hai bác sĩ phù hợp để đặt lịch.";
        }

        return busyDoctorCount == 0
            ? "Khung giờ này vẫn còn trống."
            : "Chỉ còn một bác sĩ trống, nên đặt sớm.";
    }

    private IReadOnlyList<CachedSymptomRule> GetCachedSymptomRules()
    {
        return cache.GetOrCreate(AiSymptomRuleCacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            entry.SlidingExpiration = TimeSpan.FromMinutes(5);

            return db.AiSymptomRules
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.Score)
                .Select(x => new { x.DepartmentId, x.Score, x.Term, x.NormalizedTerm })
                .AsEnumerable()
                .Select(x =>
                {
                    var normalizedTerm = string.IsNullOrWhiteSpace(x.NormalizedTerm)
                        ? VietnameseTextNormalizer.Normalize(x.Term)
                        : x.NormalizedTerm;
                    return new CachedSymptomRule(
                        x.DepartmentId,
                        x.Score,
                        VietnameseTextNormalizer.Tokenize(normalizedTerm).ToArray());
                })
                .Where(x => x.TermTokens.Count > 0)
                .ToList();
        }) ?? [];
    }

    private static bool ContainsTerm(IReadOnlyList<string> inputTokens, IReadOnlyList<string> termTokens)
    {
        if (termTokens.Count == 0 || inputTokens.Count < termTokens.Count)
        {
            return false;
        }

        for (var start = 0; start <= inputTokens.Count - termTokens.Count; start++)
        {
            var matched = true;
            for (var offset = 0; offset < termTokens.Count; offset++)
            {
                if (!string.Equals(inputTokens[start + offset], termTokens[offset], StringComparison.Ordinal))
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
            {
                return true;
            }
        }

        return false;
    }

    private static TimeSpan ParseStart(string slot) => TimeSpan.Parse(slot.Split('-')[0]);

    private sealed record CachedSymptomRule(int DepartmentId, int Score, IReadOnlyList<string> TermTokens);

    private sealed record SlotAvailabilitySnapshot(
        string TimeSlot,
        int AvailableDoctorCount,
        int BusyDoctorCount,
        bool IsPeakHour,
        int Score);
}
