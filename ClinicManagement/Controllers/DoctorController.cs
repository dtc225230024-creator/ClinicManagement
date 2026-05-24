using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Controllers;

[Authorize(Roles = "Doctor")]
public class DoctorController(ClinicStore store) : Controller
{
    public IActionResult Schedule(DateTime? date, string? tab, string? sort, int page = 1, int pageSize = 10)
    {
        var activeTab = string.Equals(tab, "completed", StringComparison.OrdinalIgnoreCase)
            ? "completed"
            : "current";

        IEnumerable<AppointmentListItem> items = store.GetAppointmentItems().Where(x =>
            activeTab == "completed"
                ? x.Appointment.Status == AppointmentStatus.Completed
                : x.Appointment.Status == AppointmentStatus.Scheduled);
        var restrictedDoctorId = GetRestrictedDoctorId();

        if (IsDoctorRestricted() && restrictedDoctorId is null)
        {
            return Forbid();
        }

        if (restrictedDoctorId is not null)
        {
            items = items.Where(x => x.Doctor.DoctorId == restrictedDoctorId.Value);
        }

        if (date is not null)
        {
            items = items.Where(x => x.Appointment.AppointmentDate.Date == date.Value.Date);
        }

        var model = new DoctorScheduleViewModel
        {
            ActiveTab = activeTab,
            Date = date,
            Sort = string.IsNullOrWhiteSpace(sort)
                ? activeTab == "completed" ? "date_desc" : "date"
                : sort,
            Page = page,
            PageSize = pageSize
        };

        items = model.Sort switch
        {
            "patient" => items.OrderBy(x => x.Patient.FullName).ThenBy(x => x.Appointment.AppointmentDate).ThenBy(x => x.Appointment.TimeSlot),
            "patient_desc" => items.OrderByDescending(x => x.Patient.FullName).ThenByDescending(x => x.Appointment.AppointmentDate).ThenByDescending(x => x.Appointment.TimeSlot),
            "time" => items.OrderBy(x => x.Appointment.TimeSlot).ThenBy(x => x.Appointment.AppointmentDate).ThenBy(x => x.Appointment.AppointmentId),
            "time_desc" => items.OrderByDescending(x => x.Appointment.TimeSlot).ThenByDescending(x => x.Appointment.AppointmentDate).ThenByDescending(x => x.Appointment.AppointmentId),
            "recorded" => items.OrderBy(x => x.MedicalRecord is null ? 1 : 0).ThenBy(x => x.MedicalRecord?.CreatedAt ?? DateTime.MaxValue).ThenByDescending(x => x.Appointment.AppointmentDate).ThenByDescending(x => x.Appointment.TimeSlot),
            "recorded_desc" => items.OrderBy(x => x.MedicalRecord is null ? 1 : 0).ThenByDescending(x => x.MedicalRecord?.CreatedAt ?? DateTime.MinValue).ThenByDescending(x => x.Appointment.AppointmentDate).ThenByDescending(x => x.Appointment.TimeSlot),
            "date_desc" => items.OrderByDescending(x => x.Appointment.AppointmentDate).ThenByDescending(x => x.Appointment.TimeSlot).ThenByDescending(x => x.Appointment.AppointmentId),
            _ => items.OrderBy(x => x.Appointment.AppointmentDate).ThenBy(x => x.Appointment.TimeSlot).ThenBy(x => x.Appointment.AppointmentId)
        };

        model.Items = PagingHelper.ApplyPaging(items, model);
        return View(model);
    }

    [HttpGet]
    public IActionResult Details(int appointmentId)
    {
        var item = FindAccessibleAppointment(appointmentId);
        if (item is null)
        {
            TempData["Error"] = "Không tìm thấy lịch khám hoặc bạn không có quyền truy cập.";
            return RedirectToAction(nameof(Schedule));
        }

        if (item.Appointment.Status == AppointmentStatus.Cancelled)
        {
            TempData["Error"] = "Không thể xem chi tiết lịch khám đã hủy.";
            return RedirectToAction(nameof(Schedule));
        }

        return View(item);
    }

    [HttpGet]
    public IActionResult MedicalRecord(int appointmentId)
    {
        var item = FindAccessibleAppointment(appointmentId);
        if (item is null)
        {
            TempData["Error"] = "Không tìm thấy lịch khám hoặc bạn không có quyền truy cập.";
            return RedirectToAction(nameof(Schedule));
        }

        if (item.Appointment.Status == AppointmentStatus.Cancelled)
        {
            TempData["Error"] = "Không thể cập nhật kết quả cho lịch khám đã hủy.";
            return RedirectToAction(nameof(Schedule));
        }

        return View(BuildMedicalRecordModel(item));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MedicalRecord(MedicalRecordViewModel model)
    {
        var item = FindAccessibleAppointment(model.Record.AppointmentId);
        if (item is null)
        {
            TempData["Error"] = "Không tìm thấy lịch khám hoặc bạn không có quyền truy cập.";
            return RedirectToAction(nameof(Schedule));
        }

        if (item.Appointment.Status == AppointmentStatus.Cancelled)
        {
            TempData["Error"] = "Không thể cập nhật kết quả cho lịch khám đã hủy.";
            return RedirectToAction(nameof(Schedule));
        }

        try
        {
            store.SaveMedicalRecord(model.Record, model.SelectedServiceIds);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(BuildMedicalRecordModel(item, model.Record, model.SelectedServiceIds));
        }

        TempData["Message"] = "Đã lưu kết quả khám bệnh và cập nhật dịch vụ sử dụng.";
        return RedirectToAction(nameof(Schedule), new
        {
            tab = "completed",
            date = item.Appointment.AppointmentDate.ToString("yyyy-MM-dd")
        });
    }

    private int? GetRestrictedDoctorId()
    {
        if (User.IsInRole("Doctor") && !User.IsInRole("Admin"))
        {
            var doctorIdClaim = User.FindFirst("DoctorId")?.Value;
            if (int.TryParse(doctorIdClaim, out var doctorId))
            {
                return doctorId;
            }
        }

        return null;
    }

    private AppointmentListItem? FindAccessibleAppointment(int appointmentId)
    {
        var item = store.GetAppointmentItems().FirstOrDefault(x => x.Appointment.AppointmentId == appointmentId);
        var restrictedDoctorId = GetRestrictedDoctorId();

        if (IsDoctorRestricted() && restrictedDoctorId is null)
        {
            return null;
        }

        if (item is null)
        {
            return null;
        }

        if (restrictedDoctorId is not null && item.Doctor.DoctorId != restrictedDoctorId.Value)
        {
            return null;
        }

        return item;
    }

    private MedicalRecordViewModel BuildMedicalRecordModel(AppointmentListItem item, MedicalRecord? record = null, int[]? selectedServiceIds = null)
    {
        List<InvoiceDetail> invoiceDetails = item.Invoice is null
            ? []
            : store.GetInvoiceDetails(item.Invoice.InvoiceId).ToList();

        return new MedicalRecordViewModel
        {
            Appointment = item,
            Record = record ?? item.MedicalRecord ?? new MedicalRecord { AppointmentId = item.Appointment.AppointmentId },
            Services = store.Services.Where(s => s.IsActive).OrderBy(s => s.ServiceName),
            SelectedServiceIds = selectedServiceIds ?? invoiceDetails.Select(x => x.ServiceId).ToArray()
        };
    }

    private bool IsDoctorRestricted() => User.IsInRole("Doctor") && !User.IsInRole("Admin");
}
