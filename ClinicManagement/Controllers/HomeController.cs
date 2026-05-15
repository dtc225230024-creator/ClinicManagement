using System.Diagnostics;
using System.Security.Claims;
using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Controllers;

[Authorize]
public class HomeController(ClinicStore store) : Controller
{
    public IActionResult Index()
    {
        var appointments = store.GetAppointmentItems()
            .OrderBy(x => x.Appointment.AppointmentDate)
            .ThenBy(x => x.Appointment.TimeSlot)
            .ToList();

        DashboardViewModel model;
        if (User.IsInRole("Admin"))
        {
            model = BuildAdminDashboard(appointments);
        }
        else if (User.IsInRole("Receptionist"))
        {
            model = BuildReceptionDashboard(appointments);
        }
        else
        {
            var doctorId = GetDoctorId();
            if (doctorId is null)
            {
                return Forbid();
            }

            model = BuildDoctorDashboard(appointments, doctorId.Value);
        }

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MarkManualSeen(string version)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return Forbid();
        }

        if (!string.Equals(version, UserManualService.CurrentVersion, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest();
        }

        store.MarkUserManualSeen(userId, UserManualService.CurrentVersion);
        return Ok();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private DashboardViewModel BuildAdminDashboard(IReadOnlyList<AppointmentListItem> appointments)
    {
        var today = DateTime.Today;
        var waitingPaymentCount = CountWaitingPayments(appointments);
        var upcoming = appointments
            .Where(x => x.Appointment.Status == AppointmentStatus.Scheduled && x.Appointment.AppointmentDate.Date >= today)
            .Take(8)
            .ToList();

        return new DashboardViewModel
        {
            Eyebrow = "Hệ thống quản lý phòng khám",
            Title = "Tổng quan điều hành",
            Subtitle = "Theo dõi toàn bộ tài khoản, lịch khám, thanh toán và doanh thu trong một màn hình.",
            AppointmentSectionTitle = "Lịch khám sắp tới toàn hệ thống",
            EmptyMessage = "Chưa có lịch khám sắp tới trong hệ thống.",
            Metrics =
            [
                Metric("Tài khoản đang hoạt động", store.Users.Count(x => x.IsActive).ToString(), "Bao gồm admin, lễ tân và bác sĩ."),
                Metric("Bác sĩ hoạt động", store.Doctors.Count(x => x.IsActive).ToString(), "Chỉ tính các bác sĩ đang nhận lịch."),
                Metric("Bệnh nhân", store.Patients.Count.ToString(), "Tổng hồ sơ bệnh nhân hiện có."),
                Metric("Lịch hôm nay", appointments.Count(x => x.Appointment.AppointmentDate.Date == today && x.Appointment.Status != AppointmentStatus.Cancelled).ToString(), "Bao gồm đang chờ và đã hoàn tất."),
                Metric("Cần thanh toán", waitingPaymentCount.ToString(), "Lịch đã khám xong nhưng chưa thanh toán xong."),
                Metric("Doanh thu hôm nay", store.Invoices.Where(x => x.CreatedAt.Date == today && x.PaymentStatus == PaymentStatus.Paid).Sum(x => x.TotalAmount).ToString("N0") + " đ", "Tổng giá trị hóa đơn đã thanh toán.")
            ],
            Actions =
            [
                Action("Xem lịch khám", "Reception", "Appointments", "btn btn-primary"),
                Action("Quản lý tài khoản", "Admin", "Users"),
                Action("Xem báo cáo", "Reports", "Index")
            ],
            Appointments = upcoming,
            ShowDoctorColumn = true,
            ShowDepartmentColumn = true,
            ShowStatusColumn = true,
            ShowPaymentColumn = true
        };
    }

    private DashboardViewModel BuildReceptionDashboard(IReadOnlyList<AppointmentListItem> appointments)
    {
        var today = DateTime.Today;
        var waitingPaymentCount = CountWaitingPayments(appointments);
        var queue = appointments
            .Where(x => x.Appointment.Status != AppointmentStatus.Cancelled)
            .OrderBy(x => GetReceptionPriority(x, today))
            .ThenBy(x => x.Appointment.AppointmentDate)
            .ThenBy(x => x.Appointment.TimeSlot)
            .Take(8)
            .ToList();

        return new DashboardViewModel
        {
            Eyebrow = "Lễ tân",
            Title = "Bảng điều phối tiếp nhận",
            Subtitle = "Tập trung vào bệnh nhân trong ngày, lịch cần xử lý và các ca đang chờ thanh toán.",
            AppointmentSectionTitle = "Lịch cần lễ tân xử lý",
            EmptyMessage = "Chưa có lịch khám nào cần lễ tân xử lý.",
            Metrics =
            [
                Metric("Bệnh nhân", store.Patients.Count.ToString(), "Tổng hồ sơ có thể đặt lịch ngay."),
                Metric("Lịch hôm nay", appointments.Count(x => x.Appointment.AppointmentDate.Date == today && x.Appointment.Status != AppointmentStatus.Cancelled).ToString(), "Tất cả lịch chưa hủy trong ngày."),
                Metric("Đang chờ khám", appointments.Count(x => x.Appointment.Status == AppointmentStatus.Scheduled).ToString(), "Bao gồm cả hôm nay và lịch sắp tới."),
                Metric("Cần thanh toán", waitingPaymentCount.ToString(), "Các lịch đã khám xong nhưng chưa thanh toán xong."),
                Metric("Đã thanh toán hôm nay", store.Invoices.Count(x => x.CreatedAt.Date == today && x.PaymentStatus == PaymentStatus.Paid).ToString(), "Số hóa đơn đã chốt trong ngày.")
            ],
            Actions =
            [
            Action("Đặt lịch khám", "Reception", "CreateAppointment", "btn btn-primary"),
                Action("Quản lý bệnh nhân", "Reception", "Patients"),
                Action("Xem lịch khám", "Reception", "Appointments")
            ],
            Appointments = queue,
            ShowDoctorColumn = true,
            ShowDepartmentColumn = true,
            ShowStatusColumn = true,
            ShowPaymentColumn = true
        };
    }

    private DashboardViewModel BuildDoctorDashboard(IReadOnlyList<AppointmentListItem> appointments, int doctorId)
    {
        var today = DateTime.Today;
        var doctorAppointments = appointments
            .Where(x => x.Doctor.DoctorId == doctorId)
            .ToList();
        var doctorName = doctorAppointments.FirstOrDefault()?.Doctor.FullName
            ?? store.Doctors.FirstOrDefault(x => x.DoctorId == doctorId)?.FullName
            ?? User.Identity?.Name
            ?? "Bác sĩ";
        var queue = doctorAppointments
            .Where(x => x.Appointment.Status == AppointmentStatus.Scheduled && x.Appointment.AppointmentDate.Date >= today)
            .Take(8)
            .ToList();

        return new DashboardViewModel
        {
            Eyebrow = "Bác sĩ",
            Title = "Lịch làm việc của " + doctorName,
            Subtitle = "Theo dõi lịch cần khám, kết quả đã ghi và các bệnh nhân sẽ đến trong 7 ngày tới.",
            AppointmentSectionTitle = "Lịch cần khám sắp tới",
            EmptyMessage = "Bạn chưa có lịch khám nào cần xử lý.",
            Metrics =
            [
                Metric("Lịch hôm nay", doctorAppointments.Count(x => x.Appointment.Status == AppointmentStatus.Scheduled && x.Appointment.AppointmentDate.Date == today).ToString(), "Các ca khám bác sĩ cần xử lý trong ngày."),
                Metric("Đang chờ khám", doctorAppointments.Count(x => x.Appointment.Status == AppointmentStatus.Scheduled).ToString(), "Tổng số lịch chưa hoàn tất và chưa hủy."),
                Metric("Đã khám hôm nay", doctorAppointments.Count(x => x.Appointment.Status == AppointmentStatus.Completed && x.Appointment.AppointmentDate.Date == today).ToString(), "Số ca đã cập nhật kết quả trong ngày."),
                Metric("Lịch 7 ngày tới", doctorAppointments.Count(x => x.Appointment.Status == AppointmentStatus.Scheduled && x.Appointment.AppointmentDate.Date >= today && x.Appointment.AppointmentDate.Date <= today.AddDays(7)).ToString(), "Giúp bác sĩ nhìn nhanh tải lượng bệnh nhân sắp tới.")
            ],
            Actions =
            [
                Action("Mở lịch cá nhân", "Doctor", "Schedule", "btn btn-primary"),
                Action("Xem hồ sơ khám", "Records", "Index")
            ],
            Appointments = queue,
            ShowReasonColumn = true
        };
    }

    private int? GetDoctorId()
    {
        var doctorIdValue = User.FindFirstValue("DoctorId");
        return int.TryParse(doctorIdValue, out var doctorId) ? doctorId : null;
    }

    private static int GetReceptionPriority(AppointmentListItem item, DateTime today)
    {
        if (NeedsPayment(item))
        {
            return 0;
        }

        if (item.Appointment.AppointmentDate.Date == today)
        {
            return 1;
        }

        if (item.Appointment.Status == AppointmentStatus.Scheduled)
        {
            return 2;
        }

        return 3;
    }

    private static int CountWaitingPayments(IEnumerable<AppointmentListItem> appointments)
    {
        return appointments.Count(NeedsPayment);
    }

    private static bool NeedsPayment(AppointmentListItem item)
    {
        return item.Appointment.Status == AppointmentStatus.Completed &&
               (item.Invoice is null || item.Invoice.PaymentStatus == PaymentStatus.Unpaid);
    }

    private static DashboardMetricViewModel Metric(string label, string value, string? hint = null)
    {
        return new DashboardMetricViewModel
        {
            Label = label,
            Value = value,
            Hint = hint
        };
    }

    private static DashboardActionViewModel Action(string label, string controller, string action, string buttonClass = "btn btn-outline-primary")
    {
        return new DashboardActionViewModel
        {
            Label = label,
            Controller = controller,
            Action = action,
            ButtonClass = buttonClass
        };
    }
}
