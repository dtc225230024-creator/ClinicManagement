using System.ComponentModel.DataAnnotations;

namespace ClinicManagement.Models;

public enum UserRole
{
    Admin,
    Receptionist,
    Doctor
}

public enum AppointmentStatus
{
    Scheduled,
    Completed,
    Cancelled
}

public enum PaymentStatus
{
    Unpaid,
    Paid
}

public class UserAccount
{
    public int UserId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    [StringLength(50, ErrorMessage = "Tên đăng nhập không được vượt quá 50 ký tự")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [StringLength(100, ErrorMessage = "Mật khẩu không được vượt quá 100 ký tự")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn vai trò")]
    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }

    public int? DoctorId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? ManualSeenVersion { get; set; }
    public DateTime? ManualSeenAt { get; set; }
}

public class Department
{
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên chuyên khoa")]
    [StringLength(100, ErrorMessage = "Tên chuyên khoa không được vượt quá 100 ký tự")]
    public string DepartmentName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class AiSymptomRule
{
    public int AiSymptomRuleId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn chuyên khoa")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập cụm triệu chứng")]
    [StringLength(120, ErrorMessage = "Cụm triệu chứng không được vượt quá 120 ký tự")]
    public string Term { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập cụm triệu chứng chuẩn hóa")]
    [StringLength(120, ErrorMessage = "Cụm triệu chứng chuẩn hóa không được vượt quá 120 ký tự")]
    public string NormalizedTerm { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "Điểm ưu tiên phải nằm trong khoảng 1 đến 100")]
    public int Score { get; set; } = 10;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}

public class DoctorProfile
{
    public int DoctorId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên bác sĩ")]
    [StringLength(100, ErrorMessage = "Họ tên bác sĩ không được vượt quá 100 ký tự")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(10, ErrorMessage = "Giới tính không được vượt quá 10 ký tự")]
    public string? Gender { get; set; }

    [StringLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn chuyên khoa")]
    public int DepartmentId { get; set; }

    public bool IsActive { get; set; } = true;
}

public class WorkSchedule
{
    public int ScheduleId { get; set; }

    public int DoctorId { get; set; }

    [DataType(DataType.Date)]
    public DateTime WorkDate { get; set; }

    [DataType(DataType.Time)]
    public TimeSpan StartTime { get; set; }

    [DataType(DataType.Time)]
    public TimeSpan EndTime { get; set; }

    public bool IsActive { get; set; } = true;
}

public class Patient
{
    public int PatientId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên bệnh nhân")]
    [StringLength(100, ErrorMessage = "Họ tên bệnh nhân không được vượt quá 100 ký tự")]
    public string FullName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(10, ErrorMessage = "Giới tính không được vượt quá 10 ký tự")]
    public string? Gender { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [StringLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự")]
    public string? Address { get; set; }

    [StringLength(20, ErrorMessage = "CCCD/CMND không được vượt quá 20 ký tự")]
    public string? IdentityNumber { get; set; }
}

public class Appointment
{
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public int DepartmentId { get; set; }

    [DataType(DataType.Date)]
    public DateTime AppointmentDate { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn khung giờ")]
    [StringLength(20, ErrorMessage = "Khung giờ không được vượt quá 20 ký tự")]
    public string TimeSlot { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class MedicalRecord
{
    public int RecordId { get; set; }
    public int AppointmentId { get; set; }
    public string? Symptoms { get; set; }
    public string? Diagnosis { get; set; }
    public string? ExaminationResult { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class ClinicService
{
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ")]
    [StringLength(100, ErrorMessage = "Tên dịch vụ không được vượt quá 100 ký tự")]
    public string ServiceName { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Giá dịch vụ không được âm")]
    public decimal Price { get; set; }

    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Invoice
{
    public int InvoiceId { get; set; }
    public int AppointmentId { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class InvoiceDetail
{
    public int InvoiceDetailId { get; set; }
    public int InvoiceId { get; set; }
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ")]
    [StringLength(100, ErrorMessage = "Tên dịch vụ không được vượt quá 100 ký tự")]
    public string ServiceName { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Đơn giá không được âm")]
    public decimal UnitPrice { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public int Quantity { get; set; } = 1;

    [Range(0, double.MaxValue, ErrorMessage = "Thành tiền không được âm")]
    public decimal LineTotal { get; set; }
}
