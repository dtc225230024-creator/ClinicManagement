using System.Data;
using ClinicManagement.Models;
using ClinicManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Data;

public static class ClinicDbInitializer
{
    private const string InitialMigrationId = "20260420184901_InitialCreate";
    private const string EfProductVersion = "8.0.13";
    private const int TargetDoctorCount = 24;
    private const int TargetPatientCount = 100;
    private const int TargetAppointmentCount = 200;
    private const string TemporaryPassword = "Clinic@2026!";
    private const string AnalyticsDemoReasonPrefix = "Báo cáo demo";

    private static readonly (string Name, string Description)[] DepartmentSeeds =
    [
        ("Nội tổng quát", "Khám bệnh nội khoa và sức khỏe tổng quát"),
        ("Hô hấp", "Ho, khó thở, viêm phổi, hen"),
        ("Tiêu hóa", "Đau bụng, buồn nôn, rối loạn tiêu hóa"),
        ("Mắt", "Đau mắt, mờ mắt, viêm kết mạc"),
        ("Tai mũi họng", "Đau họng, nghẹt mũi, ù tai"),
        ("Da liễu", "Ngứa da, nổi mẩn, mụn, viêm da, dị ứng da"),
        ("Cơ xương khớp", "Đau khớp, đau lưng, chấn thương, hạn chế vận động"),
        ("Tim mạch", "Đau ngực, hồi hộp, huyết áp, khó thở khi gắng sức"),
        ("Sản phụ khoa", "Khám thai, rối loạn kinh nguyệt, đau vùng chậu, viêm phụ khoa")
    ];

    private static readonly (string Name, decimal Price, string Description)[] ServiceSeeds =
    [
        ("Khám tổng quát", 150_000m, "Phí khám ban đầu"),
        ("Xét nghiệm máu", 220_000m, "Công thức máu cơ bản"),
        ("Siêu âm", 300_000m, "Siêu âm ổ bụng"),
        ("Nội soi tai mũi họng", 250_000m, "Kiểm tra tai mũi họng"),
        ("Khám da liễu", 180_000m, "Thăm khám các vấn đề da, tóc, móng"),
        ("Chụp X-quang xương khớp", 260_000m, "Đánh giá tổn thương xương khớp cơ bản"),
        ("Khám tim mạch", 220_000m, "Thăm khám tim mạch và huyết áp"),
        ("Điện tâm đồ", 180_000m, "Ghi điện tim cơ bản"),
        ("Khám sản phụ khoa", 220_000m, "Thăm khám sản phụ khoa cơ bản")
    ];

    private static readonly (string FullName, string Gender, string Phone, string Email, string DepartmentName)[] CoreDoctorSeeds =
    [
        ("BS. Nguyễn Minh An", "Nam", "0901000001", "an@clinic.local", "Nội tổng quát"),
        ("BS. Trần Thu Hà", "Nữ", "0901000002", "ha@clinic.local", "Hô hấp"),
        ("BS. Lê Quốc Bảo", "Nam", "0901000003", "bao@clinic.local", "Tiêu hóa"),
        ("BS. Phạm Ngọc Linh", "Nữ", "0901000004", "linh@clinic.local", "Mắt"),
        ("BS. Đỗ Hải Yến", "Nữ", "0901000005", "yen@clinic.local", "Da liễu"),
        ("BS. Vũ Đức Sơn", "Nam", "0901000006", "son@clinic.local", "Cơ xương khớp"),
        ("BS. Nguyễn Gia Hưng", "Nam", "0901000007", "hung@clinic.local", "Tim mạch"),
        ("BS. Trần Mai Phương", "Nữ", "0901000008", "phuong@clinic.local", "Sản phụ khoa")
    ];

    private static readonly string[] LastNames =
    [
        "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Vũ", "Phan", "Đặng", "Bùi", "Đỗ"
    ];

    private static readonly string[] MiddleNames =
    [
        "Minh", "Thu", "Quốc", "Ngọc", "Thanh", "Gia", "Khánh", "Anh", "Hoài", "Đức"
    ];

    private static readonly string[] GivenNames =
    [
        "An", "Bình", "Châu", "Dung", "Giang", "Hà", "Hùng", "Linh", "Mai", "Nam",
        "Ngân", "Phong", "Quỳnh", "Sơn", "Thảo", "Trang", "Tuấn", "Vy", "Yến", "Khoa"
    ];

    private static readonly string[] DiverseLastNames =
    [
        "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Vũ", "Phan", "Đặng", "Bùi", "Đỗ",
        "Hồ", "Dương", "Võ", "Tạ", "Lý"
    ];

    private static readonly string[] DiverseMiddleNames =
    [
        "Minh", "Thu", "Quốc", "Ngọc", "Thanh", "Gia", "Khánh", "Anh", "Hoài", "Đức",
        "Quang", "Hải", "Bảo", "Nhật", "Thiên", "Phương", "Trường", "Hữu"
    ];

    private static readonly string[] DiverseGivenNames =
    [
        "An", "Bảo", "Châu", "Dung", "Giang", "Hà", "Hân", "Hiếu", "Hùng", "Khánh",
        "Lan", "Linh", "Mai", "Nam", "Ngân", "Nhi", "Phong", "Phúc", "Quân", "Quỳnh",
        "Sơn", "Thảo", "Trang", "Trâm", "Tuấn", "Vy", "Yến", "Khoa", "Tú", "Uyên"
    ];

    private static readonly string[] Addresses =
    [
        "Thái Nguyên", "Bắc Ninh", "Hà Nội", "Hải Phòng", "Nam Định",
        "Vĩnh Phúc", "Bắc Giang", "Tuyên Quang", "Phú Thọ", "Lạng Sơn"
    ];

    private static readonly string[] ReasonTemplates =
    [
        "Khám sức khỏe tổng quát",
        "Đau họng kéo dài",
        "Ho và khó thở",
        "Đau bụng và rối loạn tiêu hóa",
        "Khám mắt định kỳ",
        "Tái khám theo chỉ định",
        "Đau đầu, mệt mỏi",
        "Kiểm tra huyết áp",
        "Khám dị ứng theo mùa",
        "Tư vấn điều trị"
    ];

    private static readonly (string Symptoms, string Diagnosis, string Result, string Note)[] MedicalRecordTemplates =
    [
        ("Ho khan, đau họng", "Viêm họng cấp", "Niêm mạc họng sung huyết", "Uống thuốc đủ liều và tái khám sau 5 ngày."),
        ("Đau bụng âm ỉ", "Rối loạn tiêu hóa", "Bụng mềm, không phản ứng thành bụng", "Ăn uống thanh đạm, theo dõi thêm."),
        ("Mệt mỏi, chóng mặt", "Thiếu máu nhẹ", "Mạch và huyết áp ổn định", "Bổ sung dinh dưỡng và xét nghiệm lại."),
        ("Khó thở khi gắng sức", "Viêm phế quản", "Phổi thông khí giảm nhẹ", "Theo dõi triệu chứng hô hấp tại nhà."),
        ("Mờ mắt, khô mắt", "Viêm kết mạc", "Mắt đỏ nhẹ, không có dị vật", "Hạn chế dùng thiết bị điện tử kéo dài.")
    ];

    private static readonly string[] TimeSlots =
    [
        "08:00-08:30", "08:30-09:00", "09:00-09:30", "09:30-10:00",
        "10:00-10:30", "10:30-11:00", "11:00-11:30", "13:30-14:00",
        "14:00-14:30", "14:30-15:00", "15:00-15:30", "15:30-16:00"
    ];

    private static readonly int[] AnalyticsTrendPattern =
    [
        2, 5, 1, 6, 3, 8, 2, 7, 4, 9
    ];

    public static void Initialize(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHashService>();

        BaselineEnsureCreatedDatabase(db);
        db.Database.Migrate();

        EnsureDepartments(db);
        EnsureAiSymptomRules(db);
        EnsureServices(db);
        EnsureDoctors(db);
        EnsureUsers(db, passwordHasher);
        EnsureUserDoctorLinkIndex(db);
        EnsurePatients(db);
        EnsureDemoDisplayNames(db);
        EnsureSchedules(db);
        EnsureAppointments(db);
        EnsureVietnameseDemoDataText(db);
        EnsureAnalyticsDemoData(db);
        EnsureMedicalRecordsAndInvoices(db);
    }

    private static void EnsureDepartments(ClinicDbContext db)
    {
        var existing = db.Departments.ToDictionary(x => x.DepartmentName, StringComparer.OrdinalIgnoreCase);
        foreach (var seed in DepartmentSeeds)
        {
            if (existing.TryGetValue(seed.Name, out var department))
            {
                department.Description ??= seed.Description;
                department.IsActive = true;
                continue;
            }

            db.Departments.Add(new Department
            {
                DepartmentName = seed.Name,
                Description = seed.Description,
                IsActive = true
            });
        }

        db.SaveChanges();
    }

    private static void EnsureAiSymptomRules(ClinicDbContext db)
    {
        var departments = db.Departments
            .ToDictionary(x => VietnameseTextNormalizer.Normalize(x.DepartmentName), x => x.DepartmentId);
        var existingRules = db.AiSymptomRules
            .ToDictionary(x => (x.DepartmentId, x.NormalizedTerm), x => x);
        var now = DateTime.Now;

        foreach (var seed in AiSymptomRuleSeedCatalog.Rules)
        {
            if (!departments.TryGetValue(VietnameseTextNormalizer.Normalize(seed.DepartmentName), out var departmentId))
            {
                continue;
            }

            foreach (var term in seed.Terms.Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var normalizedTerm = VietnameseTextNormalizer.Normalize(term);
                if (string.IsNullOrWhiteSpace(normalizedTerm))
                {
                    continue;
                }

                var key = (departmentId, normalizedTerm);
                if (existingRules.TryGetValue(key, out var rule))
                {
                    rule.Term = term;
                    rule.Score = Math.Max(rule.Score, seed.Score);
                    rule.IsActive = true;
                    rule.UpdatedAt = now;
                    continue;
                }

                rule = new AiSymptomRule
                {
                    DepartmentId = departmentId,
                    Term = term,
                    NormalizedTerm = normalizedTerm,
                    Score = seed.Score,
                    IsActive = true,
                    CreatedAt = now
                };
                db.AiSymptomRules.Add(rule);
                existingRules[key] = rule;
            }
        }

        db.SaveChanges();
    }

    private static void EnsureServices(ClinicDbContext db)
    {
        var existing = db.Services.ToDictionary(x => x.ServiceName, StringComparer.OrdinalIgnoreCase);
        foreach (var seed in ServiceSeeds)
        {
            if (existing.TryGetValue(seed.Name, out var service))
            {
                service.Description ??= seed.Description;
                if (service.Price <= 0)
                {
                    service.Price = seed.Price;
                }
                service.IsActive = true;
                continue;
            }

            db.Services.Add(new ClinicService
            {
                ServiceName = seed.Name,
                Price = seed.Price,
                Description = seed.Description,
                IsActive = true
            });
        }

        db.SaveChanges();
    }

    private static void EnsureDoctors(ClinicDbContext db)
    {
        var departments = db.Departments
            .Where(x => x.IsActive)
            .OrderBy(x => x.DepartmentId)
            .ToList();

        foreach (var seed in CoreDoctorSeeds)
        {
            if (db.Doctors.Any(x => x.Email == seed.Email))
            {
                continue;
            }

            var departmentId = departments.First(x => x.DepartmentName == seed.DepartmentName).DepartmentId;
            db.Doctors.Add(new DoctorProfile
            {
                FullName = seed.FullName,
                Gender = seed.Gender,
                Phone = seed.Phone,
                Email = seed.Email,
                DepartmentId = departmentId,
                IsActive = true
            });
        }

        db.SaveChanges();

        var usedPhones = db.Doctors
            .Select(x => x.Phone)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var usedEmails = db.Doctors
            .Select(x => x.Email)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var index = 1;
        var currentCount = db.Doctors.Count();
        while (currentCount < TargetDoctorCount)
        {
            var phone = NextUniqueNumber(usedPhones, "0902000000");
            var email = $"doctor{index:00}@clinic.local";
            while (usedEmails.Contains(email))
            {
                index++;
                email = $"doctor{index:00}@clinic.local";
            }

            var fullName = BuildPersonName(index, "BS.");
            var department = departments[(db.Doctors.Count() + index) % departments.Count];
            db.Doctors.Add(new DoctorProfile
            {
                FullName = fullName,
                Gender = index % 2 == 0 ? "Nam" : "Nữ",
                Phone = phone,
                Email = email,
                DepartmentId = department.DepartmentId,
                IsActive = true
            });

            usedPhones.Add(phone);
            usedEmails.Add(email);
            currentCount++;
            index++;
        }

        db.SaveChanges();
    }

    private static void EnsureUsers(ClinicDbContext db, PasswordHashService passwordHasher)
    {
        var primaryDoctor = db.Doctors.OrderBy(x => x.DoctorId).First();
        EnsureUser(db, passwordHasher, "admin", UserRole.Admin, null, mustChangePassword: false);
        EnsureUser(db, passwordHasher, "letan", UserRole.Receptionist, null, mustChangePassword: false);
        EnsureUser(db, passwordHasher, "bacsi", UserRole.Doctor, primaryDoctor.DoctorId, mustChangePassword: false);
        EnsureUser(db, passwordHasher, "letan02", UserRole.Receptionist, null, mustChangePassword: true);
        EnsureUser(db, passwordHasher, "letan03", UserRole.Receptionist, null, mustChangePassword: true);
        EnsureUser(db, passwordHasher, "letan04", UserRole.Receptionist, null, mustChangePassword: true);
        EnsureDoctorAccounts(db, passwordHasher, primaryDoctor.DoctorId);
        db.SaveChanges();
    }

    private static void EnsureUser(ClinicDbContext db, PasswordHashService passwordHasher, string username, UserRole role, int? doctorId, bool mustChangePassword)
    {
        var user = db.Users.FirstOrDefault(x => x.Username == username);
        if (user is null)
        {
            db.Users.Add(new UserAccount
            {
                Username = username,
                Password = passwordHasher.Hash(TemporaryPassword),
                Role = role,
                DoctorId = doctorId,
                IsActive = true,
                MustChangePassword = mustChangePassword,
                CreatedAt = DateTime.Now
            });
            return;
        }

        user.Role = role;
        user.IsActive = true;
        if (role == UserRole.Doctor && user.DoctorId is null)
        {
            user.DoctorId = doctorId;
        }
    }

    private static void EnsureDoctorAccounts(ClinicDbContext db, PasswordHashService passwordHasher, int primaryDoctorId)
    {
        var doctorsWithAccounts = db.Users
            .Where(x => x.Role == UserRole.Doctor && x.DoctorId != null)
            .Select(x => x.DoctorId!.Value)
            .ToHashSet();

        var existingUsernames = db.Users
            .Select(x => x.Username)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var doctor in db.Doctors.Where(x => x.IsActive).OrderBy(x => x.DoctorId))
        {
            if (doctor.DoctorId == primaryDoctorId || doctorsWithAccounts.Contains(doctor.DoctorId))
            {
                continue;
            }

            var username = BuildUniqueUsername(existingUsernames, $"bacsi{doctor.DoctorId:00}");
            db.Users.Add(new UserAccount
            {
                Username = username,
                Password = passwordHasher.Hash(TemporaryPassword),
                Role = UserRole.Doctor,
                DoctorId = doctor.DoctorId,
                IsActive = true,
                MustChangePassword = true,
                CreatedAt = DateTime.Now
            });
            existingUsernames.Add(username);
        }
    }

    private static void EnsurePatients(ClinicDbContext db)
    {
        var usedPhones = db.Patients
            .Select(x => x.Phone)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var usedIdentities = db.Patients
            .Select(x => x.IdentityNumber)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var index = 1;
        var currentCount = db.Patients.Count();
        while (currentCount < TargetPatientCount)
        {
            var phone = NextUniqueNumber(usedPhones, "0988000000");
            var identity = NextUniqueNumber(usedIdentities, "120000000000");
            db.Patients.Add(new Patient
            {
                FullName = BuildPersonName(index, null),
                DateOfBirth = DateTime.Today.AddYears(-18 - (index % 45)).AddDays(-(index * 11 % 365)),
                Gender = index % 2 == 0 ? "Nam" : "Nữ",
                Phone = phone,
                Address = Addresses[index % Addresses.Length],
                IdentityNumber = identity
            });
            usedPhones.Add(phone);
            usedIdentities.Add(identity);
            currentCount++;
            index++;
        }

        db.SaveChanges();
    }

    private static void EnsureSchedules(ClinicDbContext db)
    {
        var existingKeys = db.WorkSchedules
            .Where(x => x.IsActive)
            .AsEnumerable()
            .Select(x => $"{x.DoctorId}-{x.WorkDate.Date:yyyyMMdd}")
            .ToHashSet(StringComparer.Ordinal);

        var doctors = db.Doctors
            .Where(x => x.IsActive)
            .OrderBy(x => x.DoctorId)
            .ToList();

        for (var offset = -10; offset <= 21; offset++)
        {
            var workDate = DateTime.Today.AddDays(offset).Date;
            foreach (var doctor in doctors)
            {
                var key = $"{doctor.DoctorId}-{workDate:yyyyMMdd}";
                if (existingKeys.Contains(key))
                {
                    continue;
                }

                db.WorkSchedules.Add(new WorkSchedule
                {
                    DoctorId = doctor.DoctorId,
                    WorkDate = workDate,
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(16, 30, 0),
                    IsActive = true
                });
                existingKeys.Add(key);
            }
        }

        db.SaveChanges();
    }

    private static void EnsureAppointments(ClinicDbContext db)
    {
        var doctors = db.Doctors
            .Where(x => x.IsActive)
            .OrderBy(x => x.DoctorId)
            .Select(x => new { x.DoctorId, x.DepartmentId })
            .ToList();
        var patientIds = db.Patients
            .OrderBy(x => x.PatientId)
            .Select(x => x.PatientId)
            .ToList();
        var existingKeys = db.Appointments
            .AsEnumerable()
            .Select(x => $"{x.DoctorId}-{x.AppointmentDate.Date:yyyyMMdd}-{x.TimeSlot}")
            .ToHashSet(StringComparer.Ordinal);

        var currentCount = db.Appointments.Count();
        var seedIndex = 0;
        while (currentCount < TargetAppointmentCount)
        {
            var doctor = doctors[seedIndex % doctors.Count];
            var patientId = patientIds[(seedIndex * 3) % patientIds.Count];
            var date = DateTime.Today.AddDays((seedIndex % 32) - 10).Date;
            var timeSlot = TimeSlots[(seedIndex / doctors.Count) % TimeSlots.Length];
            var key = $"{doctor.DoctorId}-{date:yyyyMMdd}-{timeSlot}";
            if (existingKeys.Contains(key))
            {
                seedIndex++;
                continue;
            }

            var status = date < DateTime.Today
                ? seedIndex % 6 == 0 ? AppointmentStatus.Cancelled : AppointmentStatus.Completed
                : seedIndex % 7 == 0 ? AppointmentStatus.Cancelled : AppointmentStatus.Scheduled;

            db.Appointments.Add(new Appointment
            {
                PatientId = patientId,
                DoctorId = doctor.DoctorId,
                DepartmentId = doctor.DepartmentId,
                AppointmentDate = date,
                TimeSlot = timeSlot,
                Reason = ReasonTemplates[seedIndex % ReasonTemplates.Length],
                Status = status,
                CreatedAt = date.AddDays(-1)
            });

            existingKeys.Add(key);
            currentCount++;
            seedIndex++;
        }

        db.SaveChanges();
    }

    private static void EnsureAnalyticsDemoData(ClinicDbContext db)
    {
        var desiredDemoCount = AnalyticsTrendPattern.Sum();
        var existingDemoCount = db.Appointments.Count(x =>
            x.Reason != null &&
            x.Reason.StartsWith(AnalyticsDemoReasonPrefix));
        if (existingDemoCount >= desiredDemoCount)
        {
            return;
        }

        var doctors = db.Doctors
            .Where(x => x.IsActive)
            .OrderBy(x => x.DoctorId)
            .Select(x => new { x.DoctorId, x.DepartmentId })
            .ToList();
        var patientIds = db.Patients
            .OrderBy(x => x.PatientId)
            .Select(x => x.PatientId)
            .ToList();
        if (doctors.Count == 0 || patientIds.Count == 0)
        {
            return;
        }

        var existingKeys = db.Appointments
            .AsEnumerable()
            .Select(x => $"{x.DoctorId}-{x.AppointmentDate.Date:yyyyMMdd}-{x.TimeSlot}")
            .ToHashSet(StringComparer.Ordinal);

        var patientCursor = 0;
        var doctorCursor = 0;
        for (var index = 0; index < AnalyticsTrendPattern.Length; index++)
        {
            var targetCount = AnalyticsTrendPattern[index];
            var date = DateTime.Today.AddDays(index - AnalyticsTrendPattern.Length).Date;
            var existingForDay = db.Appointments.Count(x =>
                x.AppointmentDate.Date == date &&
                x.Reason != null &&
                x.Reason.StartsWith(AnalyticsDemoReasonPrefix));

            var guard = 0;
            while (existingForDay < targetCount && guard < doctors.Count * TimeSlots.Length * 2)
            {
                var doctor = doctors[(doctorCursor + guard) % doctors.Count];
                var timeSlot = TimeSlots[(index + existingForDay + guard) % TimeSlots.Length];
                var key = $"{doctor.DoctorId}-{date:yyyyMMdd}-{timeSlot}";
                if (existingKeys.Contains(key))
                {
                    guard++;
                    continue;
                }

                var patientId = patientIds[patientCursor % patientIds.Count];
                var status = (existingForDay + index) % 6 == 0
                    ? AppointmentStatus.Cancelled
                    : AppointmentStatus.Completed;

                db.Appointments.Add(new Appointment
                {
                    PatientId = patientId,
                    DoctorId = doctor.DoctorId,
                    DepartmentId = doctor.DepartmentId,
                    AppointmentDate = date,
                    TimeSlot = timeSlot,
                    Reason = $"{AnalyticsDemoReasonPrefix} {index + 1}",
                    Status = status,
                    CreatedAt = date.AddDays(-1).AddHours(9)
                });

                existingKeys.Add(key);
                existingForDay++;
                patientCursor++;
                doctorCursor++;
            }
        }

        db.SaveChanges();
    }

    private static void EnsureVietnameseDemoDataText(ClinicDbContext db)
    {
        var changed = false;
        var oldAnalyticsPrefix = "Bao cao demo";

        foreach (var appointment in db.Appointments.Where(x =>
                     x.Reason != null &&
                     x.Reason.StartsWith(oldAnalyticsPrefix)))
        {
            appointment.Reason = AnalyticsDemoReasonPrefix + appointment.Reason![oldAnalyticsPrefix.Length..];
            changed = true;
        }

        foreach (var patient in db.Patients.Where(IsGeneratedPatient).OrderBy(x => x.PatientId).ToList())
        {
            if (HasVietnameseDiacritics(patient.FullName))
            {
                continue;
            }

            patient.FullName = BuildPersonName(1200 + patient.PatientId, null);
            changed = true;
        }

        foreach (var doctor in db.Doctors.Where(IsGeneratedDoctor).OrderBy(x => x.DoctorId).ToList())
        {
            if (HasVietnameseDiacritics(doctor.FullName))
            {
                continue;
            }

            doctor.FullName = BuildPersonName(200 + doctor.DoctorId, "BS.");
            changed = true;
        }

        if (changed)
        {
            db.SaveChanges();
        }
    }

    private static void EnsureMedicalRecordsAndInvoices(ClinicDbContext db)
    {
        var services = db.Services
            .Where(x => x.IsActive)
            .OrderBy(x => x.ServiceId)
            .ToList();
        if (services.Count == 0)
        {
            return;
        }

        var completedAppointments = db.Appointments
            .Where(x => x.Status == AppointmentStatus.Completed)
            .OrderBy(x => x.AppointmentDate)
            .ThenBy(x => x.AppointmentId)
            .ToList();

        var recordAppointmentIds = db.MedicalRecords
            .Select(x => x.AppointmentId)
            .ToHashSet();
        var invoiceAppointmentIds = db.Invoices
            .Select(x => x.AppointmentId)
            .ToHashSet();

        for (var index = 0; index < completedAppointments.Count; index++)
        {
            var appointment = completedAppointments[index];
            if (!recordAppointmentIds.Contains(appointment.AppointmentId))
            {
                var template = MedicalRecordTemplates[index % MedicalRecordTemplates.Length];
                db.MedicalRecords.Add(new MedicalRecord
                {
                    AppointmentId = appointment.AppointmentId,
                    Symptoms = template.Symptoms,
                    Diagnosis = template.Diagnosis,
                    ExaminationResult = template.Result,
                    Note = template.Note,
                    CreatedAt = appointment.AppointmentDate.AddHours(appointment.TimeSlot.StartsWith("08") ? 8 : 14)
                });
            }

            if (!invoiceAppointmentIds.Contains(appointment.AppointmentId))
            {
                var selectedServices = services
                    .Skip(index % services.Count)
                    .Take(index % 2 == 0 ? 1 : 2)
                    .ToList();
                if (selectedServices.Count == 0)
                {
                    selectedServices.Add(services[0]);
                }

                var invoice = new Invoice
                {
                    AppointmentId = appointment.AppointmentId,
                    PaymentStatus = index % 4 == 0 ? PaymentStatus.Unpaid : PaymentStatus.Paid,
                    TotalAmount = selectedServices.Sum(x => x.Price),
                    CreatedAt = appointment.AppointmentDate.AddHours(17)
                };
                db.Invoices.Add(invoice);
                db.SaveChanges();

                db.InvoiceDetails.AddRange(selectedServices.Select(service => new InvoiceDetail
                {
                    InvoiceId = invoice.InvoiceId,
                    ServiceId = service.ServiceId,
                    ServiceName = service.ServiceName,
                    UnitPrice = service.Price,
                    Quantity = 1,
                    LineTotal = service.Price
                }));
            }
        }

        db.SaveChanges();
    }

    private static void EnsureDemoDisplayNames(ClinicDbContext db)
    {
        EnsureGeneratedDoctorNames(db);
        EnsureGeneratedPatientNames(db);
        db.SaveChanges();
    }

    private static void EnsureGeneratedDoctorNames(ClinicDbContext db)
    {
        var doctors = db.Doctors.OrderBy(x => x.DoctorId).ToList();
        var usedNames = doctors
            .Where(x => !IsGeneratedDoctor(x))
            .Select(x => x.FullName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var generatedDoctors = doctors.Where(IsGeneratedDoctor).ToList();
        for (var index = 0; index < generatedDoctors.Count; index++)
        {
            generatedDoctors[index].FullName = BuildUniquePersonName(200 + index, "BS.", usedNames);
        }
    }

    private static void EnsureGeneratedPatientNames(ClinicDbContext db)
    {
        var patients = db.Patients.OrderBy(x => x.PatientId).ToList();
        var usedNames = patients
            .Where(x => !IsGeneratedPatient(x))
            .Select(x => x.FullName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var generatedPatients = patients.Where(IsGeneratedPatient).ToList();
        for (var index = 0; index < generatedPatients.Count; index++)
        {
            generatedPatients[index].FullName = BuildUniquePersonName(1200 + index, null, usedNames);
        }
    }

    private static bool IsGeneratedDoctor(DoctorProfile doctor)
    {
        return !string.IsNullOrWhiteSpace(doctor.Email) &&
               doctor.Email.StartsWith("doctor", StringComparison.OrdinalIgnoreCase) &&
               doctor.Email.EndsWith("@clinic.local", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGeneratedPatient(Patient patient)
    {
        return !string.IsNullOrWhiteSpace(patient.Phone) &&
               patient.Phone.StartsWith("0988", StringComparison.OrdinalIgnoreCase) &&
               !string.IsNullOrWhiteSpace(patient.IdentityNumber) &&
               patient.IdentityNumber.StartsWith("120", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildUniquePersonName(int seed, string? title, HashSet<string> usedNames)
    {
        var totalCombinations = DiverseLastNames.Length * DiverseMiddleNames.Length * DiverseGivenNames.Length;
        for (var offset = 0; offset < totalCombinations; offset++)
        {
            var candidate = BuildPersonName(seed + offset, title);
            if (usedNames.Add(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Khong the tao them ten demo duy nhat.");
    }

    private static string BuildPersonName(int index, string? title)
    {
        var totalCombinations = DiverseLastNames.Length * DiverseMiddleNames.Length * DiverseGivenNames.Length;
        var normalizedIndex = Math.Abs(index - 1);
        var permutedIndex = (normalizedIndex * 173 + 41) % totalCombinations;
        var baseName = string.Join(' ',
            DiverseLastNames[permutedIndex % DiverseLastNames.Length],
            DiverseMiddleNames[(permutedIndex / DiverseLastNames.Length) % DiverseMiddleNames.Length],
            DiverseGivenNames[(permutedIndex / (DiverseLastNames.Length * DiverseMiddleNames.Length)) % DiverseGivenNames.Length]);

        return string.IsNullOrWhiteSpace(title) ? baseName : $"{title} {baseName}";
    }

    private static bool HasVietnameseDiacritics(string value)
    {
        return value.Any(ch => "ăâđêôơưĂÂĐÊÔƠƯáàảãạắằẳẵặấầẩẫậéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵÁÀẢÃẠẮẰẲẴẶẤẦẨẪẬÉÈẺẼẸẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌỐỒỔỖỘỚỜỞỠỢÚÙỦŨỤỨỪỬỮỰÝỲỶỸỴ".Contains(ch));
    }

    private static string NextUniqueNumber(HashSet<string> usedValues, string seedValue)
    {
        var padding = seedValue.Length;
        long value = long.Parse(seedValue) + usedValues.Count + 1;
        var candidate = value.ToString().PadLeft(padding, '0');
        while (usedValues.Contains(candidate))
        {
            value++;
            candidate = value.ToString().PadLeft(padding, '0');
        }

        return candidate;
    }

    private static string BuildUniqueUsername(HashSet<string> existingUsernames, string baseUsername)
    {
        var candidate = baseUsername;
        var suffix = 1;
        while (existingUsernames.Contains(candidate))
        {
            candidate = $"{baseUsername}_{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static void BaselineEnsureCreatedDatabase(ClinicDbContext db)
    {
        if (!db.Database.CanConnect())
        {
            return;
        }

        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            connection.Open();
        }

        var usersTableExists = TableExists(connection, "Users");
        var historyTableExists = TableExists(connection, "__EFMigrationsHistory");

        if (!usersTableExists || historyTableExists)
        {
            if (shouldClose)
            {
                connection.Close();
            }

            return;
        }

        ExecuteNonQuery(connection, """
            CREATE TABLE `__EFMigrationsHistory` (
                `MigrationId` varchar(150) NOT NULL,
                `ProductVersion` varchar(32) NOT NULL,
                CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
            );
            """);

        ExecuteNonQuery(connection, $"""
            INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            VALUES ('{InitialMigrationId}', '{EfProductVersion}');
            """);

        if (shouldClose)
        {
            connection.Close();
        }
    }

    private static bool TableExists(System.Data.Common.DbConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = DATABASE() AND table_name = @tableName;
            """;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static void ExecuteNonQuery(System.Data.Common.DbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static void EnsureUserDoctorLinkIndex(ClinicDbContext db)
    {
        if (!db.Database.CanConnect())
        {
            return;
        }

        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            connection.Open();
        }

        if (!IndexExists(connection, "Users", "IX_Users_DoctorId") && !HasDuplicateDoctorLinks(connection))
        {
            ExecuteNonQuery(connection, "CREATE UNIQUE INDEX `IX_Users_DoctorId` ON `Users` (`DoctorId`);");
        }

        if (shouldClose)
        {
            connection.Close();
        }
    }

    private static bool IndexExists(System.Data.Common.DbConnection connection, string tableName, string indexName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.statistics
            WHERE table_schema = DATABASE()
              AND table_name = @tableName
              AND index_name = @indexName;
            """;

        var tableParameter = command.CreateParameter();
        tableParameter.ParameterName = "@tableName";
        tableParameter.Value = tableName;
        command.Parameters.Add(tableParameter);

        var indexParameter = command.CreateParameter();
        indexParameter.ParameterName = "@indexName";
        indexParameter.Value = indexName;
        command.Parameters.Add(indexParameter);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static bool HasDuplicateDoctorLinks(System.Data.Common.DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM (
                SELECT DoctorId
                FROM Users
                WHERE DoctorId IS NOT NULL
                GROUP BY DoctorId
                HAVING COUNT(*) > 1
            ) AS duplicates;
            """;

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }
}
