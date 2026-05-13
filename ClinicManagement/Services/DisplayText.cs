using ClinicManagement.Models;

namespace ClinicManagement.Services;

public static class DisplayText
{
    public static string UserRole(UserRole role) => role switch
    {
        Models.UserRole.Admin => "Admin",
        Models.UserRole.Receptionist => "Lễ tân",
        Models.UserRole.Doctor => "Bác sĩ",
        _ => role.ToString()
    };

    public static string AppointmentStatus(AppointmentStatus status) => status switch
    {
        Models.AppointmentStatus.Scheduled => "Đang chờ",
        Models.AppointmentStatus.Completed => "Hoàn tất",
        Models.AppointmentStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };

    public static string PaymentStatus(PaymentStatus status) => status switch
    {
        Models.PaymentStatus.Paid => "Đã thanh toán",
        Models.PaymentStatus.Unpaid => "Chưa thanh toán",
        _ => status.ToString()
    };
}
