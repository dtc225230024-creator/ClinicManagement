using ClinicManagement.Data;
using ClinicManagement.Models;
using ClinicManagement.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Services;

public class ClinicStore(ClinicDbContext db, PasswordHashService passwordHasher)
{
    public IReadOnlyList<UserAccount> Users => db.Users.AsNoTracking().OrderBy(x => x.UserId).ToList();
    public IReadOnlyList<Department> Departments => db.Departments.AsNoTracking().OrderBy(x => x.DepartmentId).ToList();
    public IReadOnlyList<AiSymptomRule> AiSymptomRules => db.AiSymptomRules.AsNoTracking().OrderBy(x => x.AiSymptomRuleId).ToList();
    public IReadOnlyList<DoctorProfile> Doctors => db.Doctors.AsNoTracking().OrderBy(x => x.DoctorId).ToList();
    public IReadOnlyList<WorkSchedule> Schedules => db.WorkSchedules.AsNoTracking().OrderBy(x => x.WorkDate).ThenBy(x => x.StartTime).ToList();
    public IReadOnlyList<Patient> Patients => db.Patients.AsNoTracking().OrderBy(x => x.FullName).ToList();
    public IReadOnlyList<Appointment> Appointments => db.Appointments.AsNoTracking().OrderBy(x => x.AppointmentDate).ThenBy(x => x.TimeSlot).ToList();
    public IReadOnlyList<MedicalRecord> Records => db.MedicalRecords.AsNoTracking().ToList();
    public IReadOnlyList<ClinicService> Services => db.Services.AsNoTracking().OrderBy(x => x.ServiceName).ToList();
    public IReadOnlyList<Invoice> Invoices => db.Invoices.AsNoTracking().ToList();
    public IReadOnlyList<InvoiceDetail> InvoiceDetails => db.InvoiceDetails.AsNoTracking().ToList();

    public UserAccount? GetUserForSession(int userId)
    {
        return db.Users
            .AsNoTracking()
            .FirstOrDefault(x => x.UserId == userId);
    }

    public UserAccount? Authenticate(string username, string password)
    {
        var user = db.Users.FirstOrDefault(u =>
            u.IsActive &&
            u.Username.ToLower() == username.ToLower());

        if (user is null || !passwordHasher.Verify(password, user.Password))
        {
            return null;
        }

        if (passwordHasher.NeedsUpgrade(user.Password))
        {
            user.Password = passwordHasher.Hash(password);
            db.SaveChanges();
        }

        return user;
    }

    public UserAccount GetUser(int userId)
    {
        return db.Users
            .AsNoTracking()
            .First(x => x.UserId == userId);
    }

    public void MarkUserManualSeen(int userId, string version)
    {
        var user = db.Users.First(x => x.UserId == userId);
        user.ManualSeenVersion = version;
        user.ManualSeenAt = DateTime.Now;
        db.SaveChanges();
    }

    public IReadOnlyList<DoctorProfile> GetAssignableDoctors(int? userId = null, int? selectedDoctorId = null)
    {
        var assignedDoctorIds = db.Users
            .AsNoTracking()
            .Where(x => x.UserId != userId && x.DoctorId != null)
            .Select(x => x.DoctorId!.Value)
            .ToHashSet();

        return db.Doctors
            .AsNoTracking()
            .Where(x => x.DoctorId == selectedDoctorId || (x.IsActive && !assignedDoctorIds.Contains(x.DoctorId)))
            .OrderBy(x => x.FullName)
            .ToList();
    }

    public string SuggestDoctorUsername(int doctorId)
    {
        var baseUsername = $"bacsi{doctorId:00}";
        var existingUsernames = db.Users
            .AsNoTracking()
            .Select(x => x.Username)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidate = baseUsername;
        var suffix = 1;
        while (existingUsernames.Contains(candidate))
        {
            candidate = $"{baseUsername}_{suffix}";
            suffix++;
        }

        return candidate;
    }

    public string? SaveUser(UserAccount user)
    {
        if (db.Users.Any(u => u.UserId != user.UserId && u.Username.ToLower() == user.Username.ToLower()))
        {
            throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");
        }

        var existingUser = user.UserId == 0
            ? null
            : db.Users.First(x => x.UserId == user.UserId);

        if (existingUser is not null &&
            existingUser.Role == UserRole.Admin &&
            existingUser.IsActive &&
            (!user.IsActive || user.Role != UserRole.Admin))
        {
            EnsureActiveAdminRemains(existingUser.UserId);
        }

        ValidateUserDoctorLink(user);

        if (user.UserId == 0)
        {

            var temporaryPassword = PasswordPolicy.GenerateTemporaryPassword();
            user.CreatedAt = DateTime.Now;
            user.Password = passwordHasher.Hash(temporaryPassword);
            user.MustChangePassword = true;
            db.Users.Add(user);
            db.SaveChanges();
            return temporaryPassword;
        }
        else
        {
            existingUser!.Username = user.Username;
            existingUser.Role = user.Role;
            existingUser.IsActive = user.IsActive;
            existingUser.DoctorId = user.DoctorId;
        }

        db.SaveChanges();
        return null;
    }

    public void ChangePassword(int userId, string currentPassword, string newPassword)
    {
        var user = db.Users.First(x => x.UserId == userId);
        if (!passwordHasher.Verify(currentPassword, user.Password))
        {
            throw new InvalidOperationException("Mật khẩu hiện tại không đúng.");
        }

        var passwordValidationMessage = PasswordPolicy.ValidateNewPassword(newPassword, currentPassword, user.Username);
        if (passwordValidationMessage is not null)
        {
            throw new InvalidOperationException(passwordValidationMessage);
        }

        user.Password = passwordHasher.Hash(newPassword);
        user.MustChangePassword = false;
        db.SaveChanges();
    }

    public string ResetPassword(int userId)
    {
        var user = db.Users.First(x => x.UserId == userId);
        var temporaryPassword = PasswordPolicy.GenerateTemporaryPassword();
        user.Password = passwordHasher.Hash(temporaryPassword);
        user.MustChangePassword = true;
        db.SaveChanges();
        return temporaryPassword;
    }

    public void ToggleUser(int id)
    {
        var user = db.Users.First(x => x.UserId == id);
        if (user.Role == UserRole.Admin && user.IsActive)
        {
            EnsureActiveAdminRemains(user.UserId);
        }

        user.IsActive = !user.IsActive;
        db.SaveChanges();
    }

    public void SaveDoctor(DoctorProfile doctor)
    {
        EnsureDoctorDepartmentIsActive(doctor.DepartmentId);

        if (doctor.DoctorId == 0)
        {
            db.Doctors.Add(doctor);
        }
        else
        {
            var existing = db.Doctors.First(x => x.DoctorId == doctor.DoctorId);
            if (existing.IsActive && !doctor.IsActive)
            {
                EnsureDoctorCanBeDeactivated(existing.DoctorId);
            }

            var departmentChanged = existing.DepartmentId != doctor.DepartmentId;
            existing.FullName = doctor.FullName;
            existing.Gender = doctor.Gender;
            existing.Phone = doctor.Phone;
            existing.Email = doctor.Email;
            existing.DepartmentId = doctor.DepartmentId;
            existing.IsActive = doctor.IsActive;

            if (departmentChanged)
            {
                UpdateScheduledAppointmentsDepartment(existing.DoctorId, doctor.DepartmentId);
            }
        }

        db.SaveChanges();
    }

    public void ToggleDoctor(int id)
    {
        var doctor = db.Doctors.First(x => x.DoctorId == id);
        if (doctor.IsActive)
        {
            EnsureDoctorCanBeDeactivated(id);
        }

        doctor.IsActive = !doctor.IsActive;
        db.SaveChanges();
    }

    public void SaveDepartment(Department department)
    {
        if (db.Departments.Any(d =>
            d.DepartmentId != department.DepartmentId &&
            d.DepartmentName.ToLower() == department.DepartmentName.ToLower()))
        {
            throw new InvalidOperationException("Tên chuyên khoa đã tồn tại.");
        }

        if (department.DepartmentId == 0)
        {
            db.Departments.Add(department);
        }
        else
        {
            var existing = db.Departments.First(x => x.DepartmentId == department.DepartmentId);
            if (existing.IsActive && !department.IsActive)
            {
                EnsureDepartmentCanBeDeactivated(existing.DepartmentId);
            }

            existing.DepartmentName = department.DepartmentName;
            existing.Description = department.Description;
            existing.IsActive = department.IsActive;
        }

        db.SaveChanges();
    }

    public void ToggleDepartment(int id)
    {
        var department = db.Departments.First(x => x.DepartmentId == id);

        if (department.IsActive && !CanDepartmentBeDeactivated(id))
        {
            throw new InvalidOperationException("Không thể ngưng chuyên khoa đang có bác sĩ hoạt động.");
        }

        department.IsActive = !department.IsActive;
        db.SaveChanges();
    }

    public void SaveAiSymptomRule(AiSymptomRule rule)
    {
        rule.Term = rule.Term.Trim();
        rule.NormalizedTerm = VietnameseTextNormalizer.Normalize(rule.Term);

        if (string.IsNullOrWhiteSpace(rule.NormalizedTerm))
        {
            throw new InvalidOperationException("Cụm triệu chứng không được để trống.");
        }

        if (rule.Score is < 1 or > 100)
        {
            throw new InvalidOperationException("Điểm ưu tiên phải nằm trong khoảng 1 đến 100.");
        }

        var department = db.Departments.FirstOrDefault(x => x.DepartmentId == rule.DepartmentId);
        if (department is null)
        {
            throw new InvalidOperationException("Chuyên khoa không tồn tại.");
        }

        if (!department.IsActive)
        {
                throw new InvalidOperationException("Không thể gán luật gợi ý vào chuyên khoa đã tạm ngưng.");
        }

        if (db.AiSymptomRules.Any(x =>
            x.AiSymptomRuleId != rule.AiSymptomRuleId &&
            x.DepartmentId == rule.DepartmentId &&
            x.NormalizedTerm == rule.NormalizedTerm))
        {
            throw new InvalidOperationException("Cụm triệu chứng này đã tồn tại trong chuyên khoa đã chọn.");
        }

        if (rule.AiSymptomRuleId == 0)
        {
            rule.CreatedAt = DateTime.Now;
            db.AiSymptomRules.Add(rule);
        }
        else
        {
            var existing = db.AiSymptomRules.First(x => x.AiSymptomRuleId == rule.AiSymptomRuleId);
            existing.DepartmentId = rule.DepartmentId;
            existing.Term = rule.Term;
            existing.NormalizedTerm = rule.NormalizedTerm;
            existing.Score = rule.Score;
            existing.IsActive = rule.IsActive;
            existing.UpdatedAt = DateTime.Now;
        }

        db.SaveChanges();
    }

    public void ToggleAiSymptomRule(int id)
    {
        var rule = db.AiSymptomRules.First(x => x.AiSymptomRuleId == id);
        rule.IsActive = !rule.IsActive;
        rule.UpdatedAt = DateTime.Now;
        db.SaveChanges();
    }

    public void SaveService(ClinicService service)
    {
        if (db.Services.Any(s =>
            s.ServiceId != service.ServiceId &&
            s.ServiceName.ToLower() == service.ServiceName.ToLower()))
        {
            throw new InvalidOperationException("Tên dịch vụ đã tồn tại.");
        }

        if (service.ServiceId == 0)
        {
            db.Services.Add(service);
        }
        else
        {
            var existing = db.Services.First(x => x.ServiceId == service.ServiceId);
            existing.ServiceName = service.ServiceName;
            existing.Price = service.Price;
            existing.Description = service.Description;
            existing.IsActive = service.IsActive;
        }

        db.SaveChanges();
    }

    public void ToggleService(int id)
    {
        var service = db.Services.First(x => x.ServiceId == id);
        service.IsActive = !service.IsActive;
        db.SaveChanges();
    }

    public void AddSchedule(WorkSchedule schedule)
    {
        if (schedule.EndTime <= schedule.StartTime)
        {
            throw new InvalidOperationException("Giờ kết thúc phải lớn hơn giờ bắt đầu.");
        }

        var hasOverlap = db.WorkSchedules.Any(x =>
            x.DoctorId == schedule.DoctorId &&
            x.WorkDate.Date == schedule.WorkDate.Date &&
            x.IsActive &&
            schedule.StartTime < x.EndTime &&
            schedule.EndTime > x.StartTime);

        if (hasOverlap)
        {
            throw new InvalidOperationException("Ca làm việc bị trùng với lịch hiện có.");
        }

        db.WorkSchedules.Add(schedule);
        db.SaveChanges();
    }

    public void RemoveSchedule(int id)
    {
        var schedule = db.WorkSchedules.First(x => x.ScheduleId == id);
        if (schedule.IsActive)
        {
            EnsureScheduleCanBeRemoved(schedule);
        }

        schedule.IsActive = false;
        db.SaveChanges();
    }

    public void SavePatient(Patient patient)
    {
        if (db.Patients.Any(p => p.PatientId != patient.PatientId && p.Phone == patient.Phone))
        {
            throw new InvalidOperationException("Số điện thoại bệnh nhân đã tồn tại.");
        }

        if (patient.PatientId == 0)
        {
            db.Patients.Add(patient);
        }
        else
        {
            var existing = db.Patients.First(x => x.PatientId == patient.PatientId);
            existing.FullName = patient.FullName;
            existing.DateOfBirth = patient.DateOfBirth;
            existing.Gender = patient.Gender;
            existing.Phone = patient.Phone;
            existing.Address = patient.Address;
            existing.IdentityNumber = patient.IdentityNumber;
        }

        db.SaveChanges();
    }

    public Appointment CreateAppointment(int patientId, int doctorId, DateTime date, string timeSlot, string reason)
    {
        var doctor = db.Doctors.First(x => x.DoctorId == doctorId);
        if (!doctor.IsActive)
        {
            throw new InvalidOperationException("Bác sĩ hiện không hoạt động.");
        }

        if (!IsSlotWithinSchedule(doctorId, date, timeSlot))
        {
            throw new InvalidOperationException("Khung giờ đã chọn không nằm trong lịch làm việc của bác sĩ.");
        }

        if (!IsSlotAvailable(doctorId, date, timeSlot))
        {
            throw new InvalidOperationException("Bác sĩ đã có lịch khám ở khung giờ này.");
        }

        var appointment = new Appointment
        {
            PatientId = patientId,
            DoctorId = doctorId,
            DepartmentId = doctor.DepartmentId,
            AppointmentDate = date.Date,
            TimeSlot = timeSlot,
            Reason = reason,
            Status = AppointmentStatus.Scheduled,
            CreatedAt = DateTime.Now
        };

        db.Appointments.Add(appointment);
        db.SaveChanges();
        return appointment;
    }

    public void RescheduleAppointment(int appointmentId, int doctorId, DateTime date, string timeSlot)
    {
        var appointment = db.Appointments.First(x => x.AppointmentId == appointmentId);
        if (appointment.Status != AppointmentStatus.Scheduled)
        {
            throw new InvalidOperationException("Chỉ có thể đổi lịch đang chờ khám.");
        }

        var doctor = db.Doctors.First(x => x.DoctorId == doctorId);
        if (!doctor.IsActive)
        {
            throw new InvalidOperationException("Bác sĩ hiện không hoạt động.");
        }

        if (!IsSlotWithinSchedule(doctorId, date, timeSlot))
        {
            throw new InvalidOperationException("Khung giờ mới không nằm trong lịch làm việc của bác sĩ.");
        }

        if (!IsSlotAvailable(doctorId, date, timeSlot, appointmentId))
        {
            throw new InvalidOperationException("Khung giờ mới không còn trống.");
        }

        appointment.DoctorId = doctorId;
        appointment.DepartmentId = doctor.DepartmentId;
        appointment.AppointmentDate = date.Date;
        appointment.TimeSlot = timeSlot;
        db.SaveChanges();
    }

    public void CancelAppointment(int appointmentId)
    {
        var appointment = db.Appointments.First(x => x.AppointmentId == appointmentId);
        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            throw new InvalidOperationException("Lịch khám này đã được hủy trước đó.");
        }

        if (db.Invoices.Any(x => x.AppointmentId == appointmentId))
        {
            throw new InvalidOperationException("Không thể hủy lịch đã có hóa đơn.");
        }

        if (appointment.Status == AppointmentStatus.Completed)
        {
            throw new InvalidOperationException("Không thể hủy lịch đã hoàn tất.");
        }

        appointment.Status = AppointmentStatus.Cancelled;
        db.SaveChanges();
    }

    public void SaveMedicalRecord(MedicalRecord record)
    {
        var appointment = db.Appointments.First(x => x.AppointmentId == record.AppointmentId);
        var existing = db.MedicalRecords.FirstOrDefault(x => x.AppointmentId == record.AppointmentId);

        if (existing is null)
        {
            record.CreatedAt = DateTime.Now;
            db.MedicalRecords.Add(record);
        }
        else
        {
            existing.Symptoms = record.Symptoms;
            existing.Diagnosis = record.Diagnosis;
            existing.ExaminationResult = record.ExaminationResult;
            existing.Note = record.Note;
        }

        appointment.Status = AppointmentStatus.Completed;
        db.SaveChanges();
    }

    public Invoice SaveInvoice(int appointmentId, IEnumerable<int> serviceIds)
    {
        var appointment = db.Appointments.First(x => x.AppointmentId == appointmentId);
        if (appointment.Status == AppointmentStatus.Scheduled)
        {
            throw new InvalidOperationException("Chỉ có thể thanh toán sau khi bác sĩ hoàn tất khám.");
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            throw new InvalidOperationException("Không thể lập hóa đơn cho lịch khám đã hủy.");
        }

        var ids = serviceIds.ToList();
        if (ids.Count == 0)
        {
            throw new InvalidOperationException("Vui lòng chọn ít nhất một dịch vụ khám.");
        }

        var selectedServices = db.Services
            .Where(s => ids.Contains(s.ServiceId) && s.IsActive)
            .ToList();

        if (selectedServices.Count != ids.Distinct().Count())
        {
            throw new InvalidOperationException("Một hoặc nhiều dịch vụ đã ngưng áp dụng.");
        }

        var invoice = db.Invoices.FirstOrDefault(x => x.AppointmentId == appointmentId);

        if (invoice is null)
        {
            invoice = new Invoice
            {
                AppointmentId = appointmentId,
                CreatedAt = DateTime.Now
            };
            db.Invoices.Add(invoice);
            db.SaveChanges();
        }

        var existingDetails = db.InvoiceDetails.Where(x => x.InvoiceId == invoice.InvoiceId).ToList();
        db.InvoiceDetails.RemoveRange(existingDetails);

        var details = selectedServices.Select(service => new InvoiceDetail
        {
            InvoiceId = invoice.InvoiceId,
            ServiceId = service.ServiceId,
            ServiceName = service.ServiceName,
            UnitPrice = service.Price,
            Quantity = 1,
            LineTotal = service.Price
        }).ToList();

        db.InvoiceDetails.AddRange(details);
        invoice.TotalAmount = details.Sum(x => x.LineTotal);
        invoice.PaymentStatus = PaymentStatus.Paid;
        db.SaveChanges();
        return invoice;
    }

    public IEnumerable<InvoiceDetail> GetInvoiceDetails(int invoiceId)
    {
        return db.InvoiceDetails
            .AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId)
            .OrderBy(x => x.InvoiceDetailId)
            .ToList();
    }

    public bool IsSlotAvailable(int doctorId, DateTime date, string timeSlot, int? ignoredAppointmentId = null)
    {
        return !db.Appointments.Any(a =>
            a.AppointmentId != ignoredAppointmentId &&
            a.DoctorId == doctorId &&
            a.AppointmentDate.Date == date.Date &&
            a.TimeSlot == timeSlot &&
            a.Status != AppointmentStatus.Cancelled);
    }

    public bool IsSlotWithinSchedule(int doctorId, DateTime date, string timeSlot)
    {
        if (!TryParseSlot(timeSlot, out var startTime, out var endTime))
        {
            return false;
        }

        return db.WorkSchedules.Any(s =>
            s.DoctorId == doctorId &&
            s.IsActive &&
            s.WorkDate.Date == date.Date &&
            startTime >= s.StartTime &&
            endTime <= s.EndTime);
    }

    public IEnumerable<AppointmentListItem> GetAppointmentItems()
    {
        var appointments =
            from appointment in db.Appointments.AsNoTracking()
            join patient in db.Patients.AsNoTracking() on appointment.PatientId equals patient.PatientId
            join doctor in db.Doctors.AsNoTracking() on appointment.DoctorId equals doctor.DoctorId
            join department in db.Departments.AsNoTracking() on appointment.DepartmentId equals department.DepartmentId
            orderby appointment.AppointmentDate, appointment.TimeSlot
            select new { appointment, patient, doctor, department };

        var records = db.MedicalRecords.AsNoTracking().ToList();
        var invoices = db.Invoices.AsNoTracking().ToList();

        return appointments
            .AsEnumerable()
            .Select(x => new AppointmentListItem(
                x.appointment,
                x.patient,
                x.doctor,
                x.department,
                records.FirstOrDefault(r => r.AppointmentId == x.appointment.AppointmentId),
                invoices.FirstOrDefault(i => i.AppointmentId == x.appointment.AppointmentId)))
            .ToList();
    }

    private void ValidateUserDoctorLink(UserAccount user)
    {
        if (user.Role != UserRole.Doctor)
        {
            user.DoctorId = null;
            return;
        }

        if (user.DoctorId is null)
        {
            throw new InvalidOperationException("Vui lòng chọn bác sĩ liên kết cho tài khoản vai trò Bác sĩ.");
        }

        var doctor = db.Doctors.FirstOrDefault(x => x.DoctorId == user.DoctorId.Value);
        if (doctor is null)
        {
            throw new InvalidOperationException("Bác sĩ liên kết không tồn tại.");
        }

        if (!doctor.IsActive)
        {
            throw new InvalidOperationException("Không thể liên kết tài khoản với bác sĩ đã ngưng hoạt động.");
        }

        if (db.Users.Any(x => x.UserId != user.UserId && x.DoctorId == user.DoctorId.Value))
        {
            throw new InvalidOperationException("Bác sĩ này đã được liên kết với một tài khoản khác.");
        }
    }

    private void EnsureActiveAdminRemains(int userId)
    {
        if (!db.Users.Any(x => x.UserId != userId && x.Role == UserRole.Admin && x.IsActive))
        {
            throw new InvalidOperationException("Hệ thống phải duy trì ít nhất một tài khoản Admin đang hoạt động.");
        }
    }

    private void EnsureDoctorDepartmentIsActive(int departmentId)
    {
        var department = db.Departments.FirstOrDefault(x => x.DepartmentId == departmentId);
        if (department is null)
        {
            throw new InvalidOperationException("Chuyên khoa của bác sĩ không tồn tại.");
        }

        if (!department.IsActive)
        {
            throw new InvalidOperationException("Không thể gán bác sĩ vào chuyên khoa đã ngưng hoạt động.");
        }
    }

    private void EnsureDoctorCanBeDeactivated(int doctorId)
    {
        if (db.Users.Any(x => x.DoctorId == doctorId && x.IsActive))
        {
            throw new InvalidOperationException("Không thể ngưng hoạt động bác sĩ đang có tài khoản đang sử dụng.");
        }

        if (db.Appointments.Any(x => x.DoctorId == doctorId && x.Status == AppointmentStatus.Scheduled))
        {
            throw new InvalidOperationException("Không thể ngưng hoạt động bác sĩ đang có lịch khám chờ.");
        }
    }

    private void EnsureDepartmentCanBeDeactivated(int departmentId)
    {
        if (!CanDepartmentBeDeactivated(departmentId))
        {
            throw new InvalidOperationException("Không thể ngưng chuyên khoa đang có bác sĩ hoạt động.");
        }
    }

    private bool CanDepartmentBeDeactivated(int departmentId)
    {
        return !db.Doctors.Any(d => d.DepartmentId == departmentId && d.IsActive);
    }

    private void EnsureScheduleCanBeRemoved(WorkSchedule schedule)
    {
        var hasScheduledAppointments = db.Appointments
            .Where(x =>
                x.DoctorId == schedule.DoctorId &&
                x.AppointmentDate.Date == schedule.WorkDate.Date &&
                x.Status == AppointmentStatus.Scheduled)
            .AsEnumerable()
            .Any(x =>
                TryParseSlot(x.TimeSlot, out var startTime, out var endTime) &&
                startTime >= schedule.StartTime &&
                endTime <= schedule.EndTime);

        if (hasScheduledAppointments)
        {
            throw new InvalidOperationException("Không thể ẩn ca làm việc đang có lịch khám chờ.");
        }
    }

    private void UpdateScheduledAppointmentsDepartment(int doctorId, int departmentId)
    {
        var scheduledAppointments = db.Appointments
            .Where(x => x.DoctorId == doctorId && x.Status == AppointmentStatus.Scheduled)
            .ToList();

        foreach (var appointment in scheduledAppointments)
        {
            appointment.DepartmentId = departmentId;
        }
    }

    private static bool TryParseSlot(string timeSlot, out TimeSpan startTime, out TimeSpan endTime)
    {
        startTime = TimeSpan.Zero;
        endTime = TimeSpan.Zero;

        var parts = timeSlot.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2 &&
               TimeSpan.TryParse(parts[0], out startTime) &&
               TimeSpan.TryParse(parts[1], out endTime) &&
               endTime > startTime;
    }
}

