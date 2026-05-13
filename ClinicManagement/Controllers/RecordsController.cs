using ClinicManagement.Services;
using ClinicManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Controllers;

[Authorize(Roles = "Admin,Receptionist,Doctor")]
public class RecordsController(ClinicStore store) : Controller
{
    public IActionResult Index(string? q, DateTime? fromDate, DateTime? toDate, int? doctorId, int? departmentId, string? sort, int page = 1, int pageSize = 10)
    {
        var restrictedDoctorId = GetRestrictedDoctorId();
        var model = new MedicalHistoryViewModel
        {
            Query = q,
            FromDate = fromDate,
            ToDate = toDate,
            DoctorId = doctorId,
            DepartmentId = departmentId,
            Sort = string.IsNullOrWhiteSpace(sort) ? "date_desc" : sort,
            Page = page,
            PageSize = pageSize
        };

        IEnumerable<AppointmentListItem> items = store.GetAppointmentItems().Where(x => x.MedicalRecord is not null);

        if (restrictedDoctorId is not null)
        {
            items = items.Where(x => x.Doctor.DoctorId == restrictedDoctorId.Value);
            doctorId = restrictedDoctorId;
            model.DoctorId = restrictedDoctorId;
        }
        else if (doctorId is not null)
        {
            items = items.Where(x => x.Doctor.DoctorId == doctorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            items = items.Where(x =>
                x.Appointment.AppointmentId.ToString() == q ||
                x.Patient.PatientId.ToString() == q ||
                x.Patient.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                x.Patient.Phone.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (x.Patient.IdentityNumber?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                x.Doctor.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                x.Department.DepartmentName.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        if (fromDate is not null)
        {
            items = items.Where(x => x.Appointment.AppointmentDate.Date >= fromDate.Value.Date);
        }

        if (toDate is not null)
        {
            items = items.Where(x => x.Appointment.AppointmentDate.Date <= toDate.Value.Date);
        }

        if (departmentId is not null)
        {
            items = items.Where(x => x.Department.DepartmentId == departmentId.Value);
        }

        items = model.Sort switch
        {
            "date" => items.OrderBy(x => x.Appointment.AppointmentDate).ThenBy(x => x.Appointment.TimeSlot),
            "patient" => items.OrderBy(x => x.Patient.FullName).ThenByDescending(x => x.Appointment.AppointmentDate),
            "patient_desc" => items.OrderByDescending(x => x.Patient.FullName).ThenByDescending(x => x.Appointment.AppointmentDate),
            "doctor" => items.OrderBy(x => x.Doctor.FullName).ThenByDescending(x => x.Appointment.AppointmentDate),
            "doctor_desc" => items.OrderByDescending(x => x.Doctor.FullName).ThenByDescending(x => x.Appointment.AppointmentDate),
            "diagnosis" => items.OrderBy(x => x.MedicalRecord!.Diagnosis).ThenByDescending(x => x.Appointment.AppointmentDate),
            "diagnosis_desc" => items.OrderByDescending(x => x.MedicalRecord!.Diagnosis).ThenByDescending(x => x.Appointment.AppointmentDate),
            _ => items.OrderByDescending(x => x.Appointment.AppointmentDate).ThenByDescending(x => x.Appointment.TimeSlot)
        };

        var filteredItems = items.ToList();
        model.IsDoctorRestricted = restrictedDoctorId is not null;
        model.Doctors = store.Doctors.Where(d => d.IsActive).OrderBy(d => d.FullName);
        model.Departments = store.Departments
            .Where(x => x.IsActive || x.DepartmentId == departmentId)
            .OrderBy(x => x.DepartmentName)
            .ToList();
        model.DisplayedFromDate = filteredItems.Count == 0 ? null : filteredItems.Min(x => x.Appointment.AppointmentDate.Date);
        model.DisplayedToDate = filteredItems.Count == 0 ? null : filteredItems.Max(x => x.Appointment.AppointmentDate.Date);
        model.FromDate ??= model.DisplayedFromDate;
        model.ToDate ??= model.DisplayedToDate;
        model.Results = PagingHelper.ApplyPaging(filteredItems, model);

        return View(model);
    }

    public IActionResult Details(int id)
    {
        var item = store.GetAppointmentItems().FirstOrDefault(x => x.Appointment.AppointmentId == id && x.MedicalRecord is not null);
        if (item is null)
        {
            TempData["Error"] = "Không tìm thấy hồ sơ khám bệnh.";
            return RedirectToAction(nameof(Index));
        }

        var restrictedDoctorId = GetRestrictedDoctorId();
        if (restrictedDoctorId is not null && item.Doctor.DoctorId != restrictedDoctorId.Value)
        {
            return Forbid();
        }

        return View(item);
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
}


