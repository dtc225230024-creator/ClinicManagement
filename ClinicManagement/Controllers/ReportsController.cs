using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Controllers;

[Authorize(Roles = "Admin")]
public class ReportsController(ClinicStore store) : Controller
{
    public IActionResult Index(DateTime? fromDate, DateTime? toDate, int? departmentId, string? rangePreset, string? sort, int page = 1, int pageSize = 10)
    {
        var normalizedPreset = string.IsNullOrWhiteSpace(rangePreset)
            ? "all"
            : rangePreset.Trim().ToLowerInvariant();
        var presetRanges = BuildRangePresets(ClinicDate.Today);
        var appointments = store.GetAppointmentItems().ToList();
        var normalizedFrom = fromDate?.Date;
        var normalizedTo = toDate?.Date;

        if (presetRanges.TryGetValue(normalizedPreset, out var presetRange))
        {
            normalizedFrom = presetRange.From;
            normalizedTo = presetRange.To;
        }
        else if (normalizedPreset == "all")
        {
            normalizedFrom = null;
            normalizedTo = null;
        }
        else
        {
            normalizedPreset = "custom";
        }

        if (departmentId is not null)
        {
            appointments = appointments
                .Where(x => x.Department.DepartmentId == departmentId.Value)
                .ToList();
        }

        if (normalizedFrom is not null)
        {
            appointments = appointments
                .Where(x => x.Appointment.AppointmentDate.Date >= normalizedFrom.Value)
                .ToList();
        }

        if (normalizedTo is not null)
        {
            appointments = appointments
                .Where(x => x.Appointment.AppointmentDate.Date <= normalizedTo.Value)
                .ToList();
        }

        var displayedFromDate = appointments.Count == 0 ? (DateTime?)null : appointments.Min(x => x.Appointment.AppointmentDate.Date);
        var displayedToDate = appointments.Count == 0 ? (DateTime?)null : appointments.Max(x => x.Appointment.AppointmentDate.Date);

        var departmentSummaries = appointments
            .GroupBy(x => new { x.Department.DepartmentId, x.Department.DepartmentName })
            .Select(g => new ReportDepartmentSummaryViewModel
            {
                DepartmentId = g.Key.DepartmentId,
                DepartmentName = g.Key.DepartmentName,
                AppointmentCount = g.Count(),
                CompletedCount = g.Count(x => x.Appointment.Status == AppointmentStatus.Completed),
                CancelledCount = g.Count(x => x.Appointment.Status == AppointmentStatus.Cancelled),
                Revenue = g
                    .Where(x => x.Invoice?.PaymentStatus == PaymentStatus.Paid)
                    .Sum(x => x.Invoice?.TotalAmount ?? 0m)
            })
            .OrderByDescending(x => x.Revenue)
            .ThenByDescending(x => x.AppointmentCount)
            .ToList();

        var trendCharts = BuildTrendCharts(appointments, displayedFromDate, displayedToDate);

        var model = new ReportsViewModel
        {
            FromDate = normalizedFrom ?? displayedFromDate,
            ToDate = normalizedTo ?? displayedToDate,
            DepartmentId = departmentId,
            Sort = string.IsNullOrWhiteSpace(sort) ? "date_desc" : sort,
            Page = page,
            PageSize = pageSize,
            Departments = store.Departments
                .Where(x => x.IsActive || x.DepartmentId == departmentId)
                .OrderBy(x => x.DepartmentName)
                .ToList(),
            DepartmentSummaries = departmentSummaries,
            TotalAppointments = appointments.Count,
            CompletedCount = appointments.Count(x => x.Appointment.Status == AppointmentStatus.Completed),
            CancelledCount = appointments.Count(x => x.Appointment.Status == AppointmentStatus.Cancelled),
            WaitingPaymentCount = appointments.Count(x =>
                x.Appointment.Status == AppointmentStatus.Completed &&
                (x.Invoice is null || x.Invoice.PaymentStatus == PaymentStatus.Unpaid)),
            Revenue = appointments
                .Where(x => x.Invoice?.PaymentStatus == PaymentStatus.Paid)
                .Sum(x => x.Invoice?.TotalAmount ?? 0m),
            RangePreset = normalizedPreset,
            RangePresets = BuildRangePresetOptions(),
            DisplayedFromDate = displayedFromDate,
            DisplayedToDate = displayedToDate,
            TrendCharts = trendCharts
        };

        IEnumerable<AppointmentListItem> results = model.Sort switch
        {
            "patient" => appointments.OrderBy(x => x.Patient.FullName).ThenBy(x => x.Appointment.AppointmentDate).ThenBy(x => x.Appointment.TimeSlot),
            "patient_desc" => appointments.OrderByDescending(x => x.Patient.FullName).ThenByDescending(x => x.Appointment.AppointmentDate).ThenByDescending(x => x.Appointment.TimeSlot),
            "doctor" => appointments.OrderBy(x => x.Doctor.FullName).ThenBy(x => x.Appointment.AppointmentDate).ThenBy(x => x.Appointment.TimeSlot),
            "doctor_desc" => appointments.OrderByDescending(x => x.Doctor.FullName).ThenByDescending(x => x.Appointment.AppointmentDate).ThenByDescending(x => x.Appointment.TimeSlot),
            "status" => appointments.OrderBy(x => x.Appointment.Status).ThenByDescending(x => x.Appointment.AppointmentDate).ThenByDescending(x => x.Appointment.TimeSlot),
            "status_desc" => appointments.OrderByDescending(x => x.Appointment.Status).ThenByDescending(x => x.Appointment.AppointmentDate).ThenByDescending(x => x.Appointment.TimeSlot),
            "payment" => appointments.OrderBy(x => x.Invoice?.PaymentStatus ?? PaymentStatus.Unpaid).ThenByDescending(x => x.Appointment.AppointmentDate).ThenByDescending(x => x.Appointment.TimeSlot),
            "payment_desc" => appointments.OrderByDescending(x => x.Invoice?.PaymentStatus ?? PaymentStatus.Unpaid).ThenByDescending(x => x.Appointment.AppointmentDate).ThenByDescending(x => x.Appointment.TimeSlot),
            "date" => appointments.OrderBy(x => x.Appointment.AppointmentDate).ThenBy(x => x.Appointment.TimeSlot).ThenBy(x => x.Appointment.AppointmentId),
            _ => appointments.OrderByDescending(x => x.Appointment.AppointmentDate).ThenByDescending(x => x.Appointment.TimeSlot).ThenByDescending(x => x.Appointment.AppointmentId)
        };

        model.Results = PagingHelper.ApplyPaging(results, model);

        return View(model);
    }

    private static IReadOnlyList<LineChartViewModel> BuildTrendCharts(
        IReadOnlyCollection<AppointmentListItem> appointments,
        DateTime? displayedFromDate,
        DateTime? displayedToDate)
    {
        if (appointments.Count == 0 || displayedFromDate is null || displayedToDate is null)
        {
            return [];
        }

        var startDate = displayedFromDate.Value.Date;
        var endDate = displayedToDate.Value.Date;
        var dates = Enumerable.Range(0, (endDate - startDate).Days + 1)
            .Select(offset => startDate.AddDays(offset))
            .ToList();

        var appointmentCounts = appointments
            .GroupBy(x => x.Appointment.AppointmentDate.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var paidRevenue = appointments
            .Where(x => x.Invoice?.PaymentStatus == PaymentStatus.Paid)
            .GroupBy(x => x.Appointment.AppointmentDate.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Invoice?.TotalAmount ?? 0m));

        var appointmentPoints = dates
            .Select(date =>
            {
                var count = appointmentCounts.GetValueOrDefault(date, 0);
                return new LineChartPointViewModel
                {
                    Label = date.ToString("dd/MM"),
                    Value = count,
                    Tooltip = $"{date:dd/MM/yyyy}: {count} lịch"
                };
            })
            .ToList();

        var revenuePoints = dates
            .Select(date =>
            {
                var value = paidRevenue.GetValueOrDefault(date, 0m);
                return new LineChartPointViewModel
                {
                    Label = date.ToString("dd/MM"),
                    Value = value,
                    Tooltip = $"{date:dd/MM/yyyy}: {value:N0} đ"
                };
            })
            .ToList();

        return
        [
            BuildChartModel(
                "Xu hướng lịch khám",
                "Số lịch phát sinh theo từng ngày trong khoảng dữ liệu đang xem.",
                "#0f766e",
                "rgba(15, 118, 110, .16)",
                appointmentPoints,
                value => $"{value:N0} lịch"),
            BuildChartModel(
                "Xu hướng doanh thu",
                "Doanh thu đã thanh toán theo ngày để nhìn nhanh biến động kinh doanh.",
                "#2563eb",
                "rgba(37, 99, 235, .16)",
                revenuePoints,
                value => $"{value:N0} đ")
        ];
    }

    private static LineChartViewModel BuildChartModel(
        string title,
        string subtitle,
        string strokeColor,
        string fillColor,
        IReadOnlyList<LineChartPointViewModel> points,
        Func<decimal, string> formatLabel)
    {
        var maxValue = points.Count == 0 ? 0m : points.Max(x => x.Value);
        var midValue = maxValue / 2m;

        return new LineChartViewModel
        {
            Title = title,
            Subtitle = subtitle,
            StrokeColor = strokeColor,
            FillColor = fillColor,
            MaxLabel = formatLabel(maxValue),
            MidLabel = formatLabel(midValue),
            MinLabel = formatLabel(0),
            Points = points
        };
    }

    private static IReadOnlyDictionary<string, (DateTime? From, DateTime? To)> BuildRangePresets(DateTime today)
    {
        var currentDate = today.Date;
        var startOfWeek = currentDate.AddDays(-(((int)currentDate.DayOfWeek + 6) % 7));
        var endOfWeek = startOfWeek.AddDays(6);
        var startOfLastWeek = startOfWeek.AddDays(-7);
        var endOfLastWeek = startOfWeek.AddDays(-1);
        var startOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);
        var endOfLastMonth = startOfMonth.AddDays(-1);

        return new Dictionary<string, (DateTime? From, DateTime? To)>(StringComparer.OrdinalIgnoreCase)
        {
            ["today"] = (currentDate, currentDate),
            ["last7"] = (currentDate.AddDays(-6), currentDate),
            ["last30"] = (currentDate.AddDays(-29), currentDate),
            ["thisweek"] = (startOfWeek, endOfWeek),
            ["lastweek"] = (startOfLastWeek, endOfLastWeek),
            ["thismonth"] = (startOfMonth, endOfMonth),
            ["lastmonth"] = (startOfLastMonth, endOfLastMonth)
        };
    }

    private static IReadOnlyList<RangePresetOptionViewModel> BuildRangePresetOptions()
    {
        return
        [
            new() { Value = "all", Label = "Tất cả" },
            new() { Value = "today", Label = "Hôm nay" },
            new() { Value = "thisweek", Label = "Tuần này" },
            new() { Value = "lastweek", Label = "Tuần trước" },
            new() { Value = "last7", Label = "7 ngày qua" },
            new() { Value = "last30", Label = "30 ngày qua" },
            new() { Value = "thismonth", Label = "Tháng này" },
            new() { Value = "lastmonth", Label = "Tháng trước" },
            new() { Value = "custom", Label = "Tùy chỉnh" }
        ];
    }
}
