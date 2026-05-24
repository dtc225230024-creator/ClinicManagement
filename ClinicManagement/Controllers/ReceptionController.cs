using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace ClinicManagement.Controllers;

[Authorize]
public class ReceptionController(ClinicStore store, AiSchedulingService ai) : Controller
{
    [Authorize(Roles = "Receptionist")]
    public IActionResult Patients(string? q, string? gender, string? sort, int page = 1, int pageSize = 10)
    {
        var model = new PatientDirectoryViewModel
        {
            Query = q,
            Gender = gender,
            Sort = string.IsNullOrWhiteSpace(sort) ? "name" : sort,
            Page = page,
            PageSize = pageSize,
            Genders = store.Patients
                .Select(x => x.Gender)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .Cast<string>()
                .ToList()
        };

        IEnumerable<Patient> patients = SearchPatients(q);

        if (!string.IsNullOrWhiteSpace(gender))
        {
            patients = patients.Where(x => string.Equals(x.Gender, gender, StringComparison.OrdinalIgnoreCase));
        }

        patients = model.Sort switch
        {
            "id" => patients.OrderBy(x => x.PatientId),
            "id_desc" => patients.OrderByDescending(x => x.PatientId),
            "dob" => patients.OrderBy(x => x.DateOfBirth).ThenBy(x => x.FullName),
            "dob_desc" => patients.OrderByDescending(x => x.DateOfBirth).ThenBy(x => x.FullName),
            "phone" => patients.OrderBy(x => x.Phone).ThenBy(x => x.FullName),
            "phone_desc" => patients.OrderByDescending(x => x.Phone).ThenBy(x => x.FullName),
            "name_desc" => patients.OrderByDescending(x => x.FullName).ThenBy(x => x.PatientId),
            _ => patients.OrderBy(x => x.FullName).ThenBy(x => x.PatientId)
        };

        model.Items = PagingHelper.ApplyPaging(patients, model);
        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = "Receptionist")]
    public IActionResult EditPatient(int? id, string? returnUrl)
    {
        var patient = id is null ? new Patient() : store.Patients.First(x => x.PatientId == id);
        SetPatientReturnNavigation(returnUrl);
        return View(patient);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Receptionist")]
    public IActionResult EditPatient(Patient patient, string? returnUrl)
    {
        SetPatientReturnNavigation(returnUrl);
        if (!ModelState.IsValid)
        {
            return View(patient);
        }

        try
        {
            store.SavePatient(patient);
            TempData["Message"] = "Đã lưu hồ sơ bệnh nhân.";

            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(AppendPatientToReturnUrl(returnUrl!, patient));
            }

            return RedirectToAction(nameof(Patients));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(patient);
        }
    }

    [HttpGet]
    [Authorize(Roles = "Receptionist")]
    public IActionResult SuggestDepartments(string? reason)
    {
        var normalizedReason = reason ?? string.Empty;
        var scores = ai.ScoreDepartments(normalizedReason);
        var departments = ai.SuggestDepartments(normalizedReason)
            .Select((department, index) => new
            {
                id = department.DepartmentId,
                name = department.DepartmentName,
                description = string.IsNullOrWhiteSpace(department.Description)
                    ? "Chuyên khoa đang hoạt động và có lịch làm việc."
                    : department.Description,
                rank = index + 1,
                score = scores.GetValueOrDefault(department.DepartmentId),
                isSuggested = scores.ContainsKey(department.DepartmentId)
            });

        return Json(new
        {
            hasReason = !string.IsNullOrWhiteSpace(normalizedReason),
            departments
        });
    }

    [HttpGet]
    [Authorize(Roles = "Receptionist")]
    public IActionResult CreateAppointment(
        string? reason,
        int? departmentId,
        int? patientId,
        DateTime? desiredDate,
        string? patientSearch,
        string? selectedTimeSlot,
        string? selectedSuggestionKey,
        int step = 1,
        bool patientSearchPerformed = false)
    {
        return View(BuildAppointmentModel(
            reason,
            departmentId,
            patientId,
            desiredDate,
            patientSearch,
            selectedTimeSlot,
            selectedSuggestionKey,
            step,
            patientSearchPerformed));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Receptionist")]
    public IActionResult CreateAppointment(AppointmentCreateViewModel model, string command)
    {
        command = string.IsNullOrWhiteSpace(command)
            ? model.Step switch
            {
                1 => "next-step-1",
                2 => "search-patient",
                _ => "refresh-step-3"
            }
            : command;

        if (command == "back-step-1")
        {
            return View(BuildAppointmentModel(
                model.Reason,
                model.DepartmentId,
                model.PatientId,
                model.DesiredDate,
                model.PatientSearch,
                model.SelectedTimeSlot,
                model.SelectedSuggestionKey,
                1,
                model.PatientSearchPerformed));
        }

        if (command == "next-step-1")
        {
            if (!ValidateAppointmentStep1(model))
            {
                return View(BuildAppointmentModel(
                    model.Reason,
                    model.DepartmentId,
                    model.PatientId,
                    model.DesiredDate,
                    model.PatientSearch,
                    model.SelectedTimeSlot,
                    model.SelectedSuggestionKey,
                    1,
                    model.PatientSearchPerformed));
            }

            return View(BuildAppointmentModel(
                model.Reason,
                model.DepartmentId,
                model.PatientId,
                model.DesiredDate,
                model.PatientSearch,
                model.SelectedTimeSlot,
                model.SelectedSuggestionKey,
                2,
                model.PatientSearchPerformed));
        }

        if (command == "search-patient")
        {
            ValidateAppointmentStep1(model);
            if (string.IsNullOrWhiteSpace(model.PatientSearch))
            {
                ModelState.AddModelError(nameof(model.PatientSearch), "Vui lòng nhập tên hoặc số điện thoại để tra cứu.");
            }

            return View(BuildAppointmentModel(
                model.Reason,
                model.DepartmentId,
                model.PatientId,
                model.DesiredDate,
                model.PatientSearch,
                model.SelectedTimeSlot,
                model.SelectedSuggestionKey,
                2,
                true));
        }

        if (command == "next-step-2")
        {
            ValidateAppointmentStep1(model);
            if (model.PatientId is null)
            {
                ModelState.AddModelError(nameof(model.PatientId), "Vui lòng chọn hồ sơ bệnh nhân.");
            }

            if (!ModelState.IsValid)
            {
                return View(BuildAppointmentModel(
                    model.Reason,
                    model.DepartmentId,
                    model.PatientId,
                    model.DesiredDate,
                    model.PatientSearch,
                    model.SelectedTimeSlot,
                    model.SelectedSuggestionKey,
                    2,
                    model.PatientSearchPerformed || !string.IsNullOrWhiteSpace(model.PatientSearch)));
            }

            return View(BuildAppointmentModel(
                model.Reason,
                model.DepartmentId,
                model.PatientId,
                model.DesiredDate,
                model.PatientSearch,
                model.SelectedTimeSlot,
                model.SelectedSuggestionKey,
                3,
                model.PatientSearchPerformed || !string.IsNullOrWhiteSpace(model.PatientSearch) || model.PatientId is not null));
        }

        if (command == "back-step-2")
        {
            return View(BuildAppointmentModel(
                model.Reason,
                model.DepartmentId,
                model.PatientId,
                model.DesiredDate,
                model.PatientSearch,
                model.SelectedTimeSlot,
                model.SelectedSuggestionKey,
                2,
                model.PatientSearchPerformed || !string.IsNullOrWhiteSpace(model.PatientSearch) || model.PatientId is not null));
        }

        if (command == "refresh-step-3")
        {
            ValidateAppointmentStep1(model);
            if (model.PatientId is null)
            {
                ModelState.AddModelError(nameof(model.PatientId), "Vui lòng chọn hồ sơ bệnh nhân.");
            }

            return View(BuildAppointmentModel(
                model.Reason,
                model.DepartmentId,
                model.PatientId,
                model.DesiredDate,
                model.PatientSearch,
                model.SelectedTimeSlot,
                model.SelectedSuggestionKey,
                3,
                model.PatientSearchPerformed || !string.IsNullOrWhiteSpace(model.PatientSearch) || model.PatientId is not null));
        }

        var selectedSuggestion = ResolveSelectedSuggestion(model);
        if (!ValidateAppointmentStep1(model) || model.PatientId is null || string.IsNullOrWhiteSpace(model.SelectedTimeSlot) || selectedSuggestion is null)
        {
            if (model.PatientId is null)
            {
                ModelState.AddModelError(nameof(model.PatientId), "Vui lòng chọn hồ sơ bệnh nhân.");
            }

            if (string.IsNullOrWhiteSpace(model.SelectedTimeSlot))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng chọn khoảng thời gian khám để hệ thống gợi ý lịch.");
            }
            else if (selectedSuggestion is null)
            {
                ModelState.AddModelError(string.Empty, "Vui lòng chọn một lịch gợi ý hoàn chỉnh.");
            }

            return View(BuildAppointmentModel(
                model.Reason,
                model.DepartmentId,
                model.PatientId,
                model.DesiredDate,
                model.PatientSearch,
                model.SelectedTimeSlot,
                model.SelectedSuggestionKey,
                3,
                model.PatientSearchPerformed || !string.IsNullOrWhiteSpace(model.PatientSearch) || model.PatientId is not null));
        }

        try
        {
            store.CreateAppointment(
                model.PatientId.Value,
                selectedSuggestion.DoctorId,
                selectedSuggestion.Date,
                selectedSuggestion.TimeSlot,
                model.Reason);
            TempData["Message"] = "Đã đặt lịch khám thành công.";
            return RedirectToAction(nameof(Appointments));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(BuildAppointmentModel(
                model.Reason,
                model.DepartmentId,
                model.PatientId,
                model.DesiredDate,
                model.PatientSearch,
                model.SelectedTimeSlot,
                model.SelectedSuggestionKey,
                3,
                model.PatientSearchPerformed || !string.IsNullOrWhiteSpace(model.PatientSearch) || model.PatientId is not null));
        }
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public IActionResult Appointments(string? q, DateTime? date, AppointmentStatus? status, int? departmentId, string? payment, string? sort, int page = 1, int pageSize = 10)
    {
        var normalizedPayment = NormalizePaymentFilter(payment);
        var model = new AppointmentDirectoryViewModel
        {
            Query = q,
            Date = date,
            Status = status,
            DepartmentId = departmentId,
            Payment = normalizedPayment,
            Sort = string.IsNullOrWhiteSpace(sort) ? "date_desc" : sort,
            Page = page,
            PageSize = pageSize,
            CanManageReception = User.IsInRole("Receptionist"),
            Departments = store.Departments
                .Where(x => x.IsActive || x.DepartmentId == departmentId)
                .OrderBy(x => x.DepartmentName)
                .ToList()
        };

        IEnumerable<AppointmentListItem> items = store.GetAppointmentItems();

        if (!string.IsNullOrWhiteSpace(q))
        {
            items = items.Where(x =>
                x.Appointment.AppointmentId.ToString() == q ||
                x.Patient.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                x.Doctor.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                x.Department.DepartmentName.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        if (date is not null)
        {
            items = items.Where(x => x.Appointment.AppointmentDate.Date == date.Value.Date);
        }

        if (status is not null)
        {
            items = items.Where(x => x.Appointment.Status == status);
        }

        if (departmentId is not null)
        {
            items = items.Where(x => x.Department.DepartmentId == departmentId.Value);
        }

        items = normalizedPayment switch
        {
            "waiting" => items.Where(NeedsPayment),
            "unpaid-invoice" => items.Where(x => x.Invoice?.PaymentStatus == PaymentStatus.Unpaid),
            "no-invoice" => items.Where(x => x.Appointment.Status == AppointmentStatus.Completed && x.Invoice is null),
            "paid" => items.Where(x => x.Invoice?.PaymentStatus == PaymentStatus.Paid),
            _ => items
        };

        items = model.Sort switch
        {
            "date" => items.OrderBy(x => x.Appointment.AppointmentDate).ThenBy(x => x.Appointment.TimeSlot).ThenBy(x => x.Appointment.AppointmentId),
            "patient" => items.OrderBy(x => x.Patient.FullName).ThenBy(x => x.Appointment.AppointmentDate).ThenBy(x => x.Appointment.TimeSlot),
            "patient_desc" => items.OrderByDescending(x => x.Patient.FullName).ThenByDescending(x => x.Appointment.AppointmentDate).ThenByDescending(x => x.Appointment.TimeSlot),
            "doctor" => items.OrderBy(x => x.Doctor.FullName).ThenBy(x => x.Appointment.AppointmentDate).ThenBy(x => x.Appointment.TimeSlot),
            "doctor_desc" => items.OrderByDescending(x => x.Doctor.FullName).ThenByDescending(x => x.Appointment.AppointmentDate).ThenByDescending(x => x.Appointment.TimeSlot),
            "recorded" => items.OrderBy(x => x.MedicalRecord is null ? 1 : 0).ThenBy(x => x.MedicalRecord?.CreatedAt ?? DateTime.MaxValue).ThenByDescending(x => x.Appointment.AppointmentDate).ThenByDescending(x => x.Appointment.TimeSlot),
            "recorded_desc" => items.OrderBy(x => x.MedicalRecord is null ? 1 : 0).ThenByDescending(x => x.MedicalRecord?.CreatedAt ?? DateTime.MinValue).ThenByDescending(x => x.Appointment.AppointmentDate).ThenByDescending(x => x.Appointment.TimeSlot),
            "status" => items.OrderBy(x => x.Appointment.Status).ThenByDescending(x => x.Appointment.AppointmentDate).ThenByDescending(x => x.Appointment.TimeSlot),
            "status_desc" => items.OrderByDescending(x => x.Appointment.Status).ThenByDescending(x => x.Appointment.AppointmentDate).ThenByDescending(x => x.Appointment.TimeSlot),
            _ => items.OrderByDescending(x => x.Appointment.AppointmentDate).ThenByDescending(x => x.Appointment.TimeSlot).ThenByDescending(x => x.Appointment.AppointmentId)
        };

        model.Items = PagingHelper.ApplyPaging(items, model);
        return View(model);
    }

    private static string? NormalizePaymentFilter(string? payment)
    {
        var normalized = payment?.Trim().ToLowerInvariant();
        return normalized is "waiting" or "unpaid-invoice" or "no-invoice" or "paid" ? normalized : null;
    }

    private static bool NeedsPayment(AppointmentListItem item)
    {
        return item.Appointment.Status == AppointmentStatus.Completed
               && (item.Invoice is null || item.Invoice.PaymentStatus == PaymentStatus.Unpaid);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Receptionist")]
    public IActionResult CancelAppointment(int id)
    {
        try
        {
            store.CancelAppointment(id);
            TempData["Message"] = "Đã hủy lịch khám.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Appointments));
    }

    [HttpGet]
    [Authorize(Roles = "Receptionist")]
    public IActionResult Reschedule(int id)
    {
        var appointment = store.GetAppointmentItems().First(x => x.Appointment.AppointmentId == id);
        if (appointment.Appointment.Status != AppointmentStatus.Scheduled)
        {
            TempData["Error"] = "Chỉ có thể đổi lịch đang chờ khám.";
            return RedirectToAction(nameof(Appointments));
        }

        return View(BuildRescheduleModel(id, appointment.Appointment.AppointmentDate, null));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Receptionist")]
    public IActionResult Reschedule(RescheduleAppointmentViewModel model, string command)
    {
        if (command == "suggest")
        {
            return View(BuildRescheduleModel(model.AppointmentId, model.DesiredDate, model.SelectedSuggestionKey));
        }

        var selectedSuggestion = ResolveSelectedRescheduleSuggestion(model);
        if (selectedSuggestion is null)
        {
            ModelState.AddModelError(string.Empty, "Vui lòng chọn một gợi ý đổi lịch hoàn chỉnh.");
            return View(BuildRescheduleModel(model.AppointmentId, model.DesiredDate, model.SelectedSuggestionKey));
        }

        try
        {
            store.RescheduleAppointment(
                model.AppointmentId,
                selectedSuggestion.DoctorId,
                selectedSuggestion.Date,
                selectedSuggestion.TimeSlot);
            TempData["Message"] = "Đã đổi lịch khám.";
            return RedirectToAction(nameof(Appointments));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(BuildRescheduleModel(model.AppointmentId, model.DesiredDate, model.SelectedSuggestionKey));
        }
    }

    [HttpGet]
    [Authorize(Roles = "Receptionist")]
    public IActionResult Invoice(int appointmentId)
    {
        try
        {
            return View(BuildInvoiceModel(appointmentId));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Appointments));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Receptionist")]
    public IActionResult Invoice(InvoiceViewModel model)
    {
        try
        {
            store.SaveInvoice(model.AppointmentId, model.SelectedServiceIds);
            TempData["Message"] = "Đã lưu hóa đơn và xác nhận trạng thái thanh toán.";
            return RedirectToAction(nameof(Invoice), new { appointmentId = model.AppointmentId });
        }
        catch (InvalidOperationException ex)
        {
            try
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(BuildInvoiceModel(model.AppointmentId, model.SelectedServiceIds));
            }
            catch (InvalidOperationException)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Appointments));
            }
        }
    }

    [HttpGet]
    [Authorize(Roles = "Receptionist")]
    public IActionResult PrintInvoice(int appointmentId)
    {
        try
        {
            return View(BuildPrintInvoiceModel(appointmentId));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Invoice), new { appointmentId });
        }
    }

    private AppointmentCreateViewModel BuildAppointmentModel(
        string? reason,
        int? departmentId,
        int? patientId,
        DateTime? desiredDate,
        string? patientSearch,
        string? selectedTimeSlot,
        string? selectedSuggestionKey)
    {
        return BuildAppointmentModel(
            reason,
            departmentId,
            patientId,
            desiredDate,
            patientSearch,
            selectedTimeSlot,
            selectedSuggestionKey,
            1,
            false);
    }

    private AppointmentCreateViewModel BuildAppointmentModel(
        string? reason,
        int? departmentId,
        int? patientId,
        DateTime? desiredDate,
        string? patientSearch,
        string? selectedTimeSlot,
        string? selectedSuggestionKey,
        int step,
        bool patientSearchPerformed)
    {
        var normalizedStep = Math.Clamp(step, 1, 3);
        var requestedDate = desiredDate?.Date ?? DateTime.Today;
        var actualDate = requestedDate < DateTime.Today ? DateTime.Today : requestedDate;
        var departments = ai.SuggestDepartments(reason ?? string.Empty).ToList();
        var aiSuggestedDepartmentIds = ai.GetSuggestedDepartmentIds(reason ?? string.Empty);
        var selectedDepartmentId = departmentId;
        var selectedDepartment = selectedDepartmentId is null
            ? null
            : departments.FirstOrDefault(x => x.DepartmentId == selectedDepartmentId.Value)
                ?? store.Departments.FirstOrDefault(x => x.DepartmentId == selectedDepartmentId.Value);
        var selectedPatient = patientId is null
            ? null
            : store.Patients.FirstOrDefault(x => x.PatientId == patientId.Value);

        var patients = normalizedStep >= 2
            ? SearchPatients(patientSearch, patientId, patientSearchPerformed)
            : [];

        var timeSlots = normalizedStep >= 3 && selectedDepartmentId is not null
            ? ai.SuggestTimeSlots(actualDate, selectedDepartmentId).ToList()
            : [];

        var normalizedTimeSlot = timeSlots.Any(x => x.TimeSlot == selectedTimeSlot && x.AvailableDoctorCount > 0)
            ? selectedTimeSlot
            : null;

        var suggestions = selectedDepartmentId is null || normalizedStep < 3 || string.IsNullOrWhiteSpace(normalizedTimeSlot)
            ? []
            : ai.SuggestAppointments(
                    selectedDepartmentId.Value,
                    actualDate,
                    preferredTimeSlot: normalizedTimeSlot,
                    includeNearbyDates: false)
                .ToList();
        var normalizedSelection = suggestions.Any(x => x.SuggestionKey == selectedSuggestionKey)
            ? selectedSuggestionKey
            : null;

        return new AppointmentCreateViewModel
        {
            Step = normalizedStep,
            Reason = reason ?? string.Empty,
            DepartmentId = selectedDepartmentId,
            SelectedDepartment = selectedDepartment,
            PatientId = patientId,
            SelectedPatient = selectedPatient,
            DesiredDate = actualDate,
            SelectedTimeSlot = normalizedTimeSlot,
            SelectedSuggestionKey = normalizedSelection,
            PatientSearch = patientSearch ?? string.Empty,
            PatientSearchPerformed = patientSearchPerformed,
            CreatePatientReturnUrl = BuildCreatePatientReturnUrl(
                reason,
                selectedDepartmentId,
                actualDate,
                patientSearch),
            AiSuggestedDepartmentIds = aiSuggestedDepartmentIds,
            Departments = departments,
            Patients = patients,
            AvailabilityDays = normalizedStep >= 3 && selectedDepartmentId is not null
                ? ai.GetAvailabilityDays(DateTime.Today, selectedDepartmentId.Value, actualDate, 7)
                : [],
            SuggestedTimeSlots = timeSlots,
            AppointmentSuggestions = suggestions
        };
    }

    private RescheduleAppointmentViewModel BuildRescheduleModel(int appointmentId, DateTime desiredDate, string? selectedSuggestionKey)
    {
        var appointment = store.GetAppointmentItems().First(x => x.Appointment.AppointmentId == appointmentId);
        var suggestions = ai.SuggestAppointments(appointment.Department.DepartmentId, desiredDate.Date, appointmentId).ToList();
        var normalizedSelection = suggestions.Any(x => x.SuggestionKey == selectedSuggestionKey)
            ? selectedSuggestionKey
            : null;

        return new RescheduleAppointmentViewModel
        {
            AppointmentId = appointmentId,
            DepartmentId = appointment.Department.DepartmentId,
            Appointment = appointment,
            DesiredDate = desiredDate.Date,
            SelectedSuggestionKey = normalizedSelection,
            AppointmentSuggestions = suggestions
        };
    }

    private InvoiceViewModel BuildInvoiceModel(int appointmentId, int[]? selectedServiceIds = null)
    {
        var appointment = store.GetAppointmentItems().First(x => x.Appointment.AppointmentId == appointmentId);
        var invoice = store.Invoices.FirstOrDefault(x => x.AppointmentId == appointmentId);
        if (appointment.Appointment.Status == AppointmentStatus.Cancelled)
        {
            throw new InvalidOperationException("Không thể mở hóa đơn cho lịch khám đã hủy.");
        }

        if (appointment.Appointment.Status == AppointmentStatus.Scheduled && invoice is null)
        {
            throw new InvalidOperationException("Chỉ có thể thanh toán sau khi bác sĩ hoàn tất khám.");
        }

        List<InvoiceDetail> existingDetails = invoice is null ? [] : store.GetInvoiceDetails(invoice.InvoiceId).ToList();
        var selectedIds = selectedServiceIds ?? existingDetails.Select(x => x.ServiceId).ToArray();

        return new InvoiceViewModel
        {
            AppointmentId = appointmentId,
            Appointment = appointment,
            Invoice = invoice,
            ExistingDetails = existingDetails,
            Services = store.Services.Where(s => s.IsActive),
            SelectedServiceIds = selectedIds,
            TotalAmount = invoice?.TotalAmount ?? existingDetails.Sum(x => x.LineTotal)
        };
    }

    private InvoicePrintViewModel BuildPrintInvoiceModel(int appointmentId)
    {
        var appointment = store.GetAppointmentItems().First(x => x.Appointment.AppointmentId == appointmentId);
        var invoice = store.Invoices.FirstOrDefault(x => x.AppointmentId == appointmentId);
        if (invoice is null)
        {
            throw new InvalidOperationException("Lịch khám này chưa có hóa đơn để in.");
        }

        var details = store.GetInvoiceDetails(invoice.InvoiceId).ToList();
        if (details.Count == 0)
        {
            throw new InvalidOperationException("Hóa đơn hiện chưa có chi tiết dịch vụ.");
        }

        return new InvoicePrintViewModel
        {
            Appointment = appointment,
            Invoice = invoice,
            Details = details,
            PrintedAt = DateTime.Now
        };
    }

    private IEnumerable<Patient> SearchPatients(string? q, int? selectedPatientId = null, bool searchPerformed = false)
    {
        var patients = store.Patients.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            patients = patients.Where(p =>
                p.PatientId.ToString() == q ||
                p.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.Phone.Contains(q, StringComparison.OrdinalIgnoreCase));
            return patients.OrderBy(p => p.FullName).ToList();
        }

        if (selectedPatientId is not null)
        {
            return patients.Where(p => p.PatientId == selectedPatientId.Value)
                .OrderBy(p => p.FullName)
                .ToList();
        }

        if (!searchPerformed)
        {
            return [];
        }

        return patients
            .OrderByDescending(p => p.PatientId)
            .Take(12)
            .OrderBy(p => p.FullName)
            .ToList();
    }

    private AppointmentSuggestion? ResolveSelectedSuggestion(AppointmentCreateViewModel model)
    {
        if (model.DepartmentId is null ||
            string.IsNullOrWhiteSpace(model.SelectedTimeSlot) ||
            string.IsNullOrWhiteSpace(model.SelectedSuggestionKey))
        {
            return null;
        }

        return ai.SuggestAppointments(
                model.DepartmentId.Value,
                model.DesiredDate,
                preferredTimeSlot: model.SelectedTimeSlot,
                includeNearbyDates: false)
            .FirstOrDefault(x => x.SuggestionKey == model.SelectedSuggestionKey);
    }

    private AppointmentSuggestion? ResolveSelectedRescheduleSuggestion(RescheduleAppointmentViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.SelectedSuggestionKey))
        {
            return null;
        }

        return ai.SuggestAppointments(model.DepartmentId, model.DesiredDate, model.AppointmentId)
            .FirstOrDefault(x => x.SuggestionKey == model.SelectedSuggestionKey);
    }

    private bool ValidateAppointmentStep1(AppointmentCreateViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Reason))
        {
            ModelState.AddModelError(nameof(model.Reason), "Vui lòng nhập nhu cầu khám.");
        }

        if (model.DepartmentId is null)
        {
            ModelState.AddModelError(nameof(model.DepartmentId), "Vui lòng chọn chuyên khoa phù hợp.");
        }

        return ModelState.IsValid;
    }

    private void SetPatientReturnNavigation(string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.BackUrl = Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Action(nameof(Patients));
    }

    private string BuildCreatePatientReturnUrl(string? reason, int? departmentId, DateTime desiredDate, string? patientSearch)
    {
        var returnRouteValues = new Dictionary<string, string?>
        {
            ["step"] = "2",
            ["patientSearchPerformed"] = "true",
            ["desiredDate"] = desiredDate.ToString("yyyy-MM-dd")
        };

        if (!string.IsNullOrWhiteSpace(reason))
        {
            returnRouteValues["reason"] = reason;
        }

        if (departmentId is not null)
        {
            returnRouteValues["departmentId"] = departmentId.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(patientSearch))
        {
            returnRouteValues["patientSearch"] = patientSearch;
        }

        var returnUrl = QueryHelpers.AddQueryString(
            Url.Action(nameof(CreateAppointment)) ?? "/Reception/CreateAppointment",
            returnRouteValues);

        return QueryHelpers.AddQueryString(
            Url.Action(nameof(EditPatient)) ?? "/Reception/EditPatient",
            "returnUrl",
            returnUrl);
    }

    private string AppendPatientToReturnUrl(string returnUrl, Patient patient)
    {
        var uriParts = returnUrl.Split('?', 2);
        var path = uriParts[0];
        var query = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (uriParts.Length == 2)
        {
            foreach (var item in QueryHelpers.ParseQuery(uriParts[1]))
            {
                query[item.Key] = item.Value.ToString();
            }
        }

        query["patientId"] = patient.PatientId.ToString();
        query["patientSearch"] = patient.FullName;
        query["patientSearchPerformed"] = "true";

        return QueryHelpers.AddQueryString(path, query);
    }
}
