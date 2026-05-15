using System.ComponentModel.DataAnnotations;
using ClinicManagement.Models;

namespace ClinicManagement.ViewModels;

public abstract class ListPageViewModel
{
    public string Sort { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)Math.Max(PageSize, 1)));
    public int StartItem => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int EndItem => TotalCount == 0 ? 0 : Math.Min(Page * PageSize, TotalCount);
    public IReadOnlyList<int> PageSizeOptions => [10, 20, 50];

    public IReadOnlyList<int> GetVisiblePages()
    {
        if (TotalPages <= 1)
        {
            return [];
        }

        var start = Math.Max(1, Page - 2);
        var end = Math.Min(TotalPages, start + 4);
        start = Math.Max(1, end - 4);

        return Enumerable.Range(start, end - start + 1).ToList();
    }
}

public static class PagingHelper
{
    public static IReadOnlyList<T> ApplyPaging<T>(IEnumerable<T> source, ListPageViewModel model)
    {
        model.PageSize = model.PageSize switch
        {
            <= 0 => 10,
            > 100 => 100,
            _ => model.PageSize
        };

        model.TotalCount = source.Count();
        var totalPages = Math.Max(1, (int)Math.Ceiling(model.TotalCount / (double)model.PageSize));
        model.Page = Math.Clamp(model.Page, 1, totalPages);

        return source
            .Skip((model.Page - 1) * model.PageSize)
            .Take(model.PageSize)
            .ToList();
    }
}

public class PagerViewModel
{
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public int StartItem { get; set; }
    public int EndItem { get; set; }
    public IReadOnlyList<int> Pages { get; set; } = [];
    public IDictionary<string, string?> RouteValues { get; set; } = new Dictionary<string, string?>();
}

public class SortHeaderViewModel
{
    public string Label { get; set; } = string.Empty;
    public string AscendingSort { get; set; } = string.Empty;
    public string DescendingSort { get; set; } = string.Empty;
    public string? CurrentSort { get; set; }
    public IDictionary<string, string?> RouteValues { get; set; } = new Dictionary<string, string?>();
}

public class UserDirectoryViewModel : ListPageViewModel
{
    public string? Query { get; set; }
    public UserRole? Role { get; set; }
    public string? Status { get; set; }
    public string? PasswordState { get; set; }
    public IReadOnlyList<UserListItem> Items { get; set; } = [];
}

public class DoctorDirectoryViewModel : ListPageViewModel
{
    public string? Query { get; set; }
    public int? DepartmentId { get; set; }
    public string? Status { get; set; }
    public string? AccountState { get; set; }
    public IEnumerable<Department> Departments { get; set; } = [];
    public IReadOnlyList<DoctorListItem> Items { get; set; } = [];
}

public class DepartmentDirectoryViewModel : ListPageViewModel
{
    public string? Query { get; set; }
    public string? Status { get; set; }
    public IReadOnlyList<Department> Items { get; set; } = [];
}

public class ServiceDirectoryViewModel : ListPageViewModel
{
    public string? Query { get; set; }
    public string? Status { get; set; }
    public IReadOnlyList<ClinicService> Items { get; set; } = [];
}

public class AiSymptomRuleDirectoryViewModel : ListPageViewModel
{
    public string? Query { get; set; }
    public int? DepartmentId { get; set; }
    public string? Status { get; set; }
    public IEnumerable<Department> Departments { get; set; } = [];
    public IReadOnlyList<AiSymptomRuleListItem> Items { get; set; } = [];
}

public class PatientDirectoryViewModel : ListPageViewModel
{
    public string? Query { get; set; }
    public string? Gender { get; set; }
    public IEnumerable<string> Genders { get; set; } = [];
    public IReadOnlyList<Patient> Items { get; set; } = [];
}

public class AppointmentDirectoryViewModel : ListPageViewModel
{
    public string? Query { get; set; }

    [DataType(DataType.Date)]
    public DateTime? Date { get; set; }

    public AppointmentStatus? Status { get; set; }
    public int? DepartmentId { get; set; }
    public IEnumerable<Department> Departments { get; set; } = [];
    public bool CanManageReception { get; set; }
    public IReadOnlyList<AppointmentListItem> Items { get; set; } = [];
}

public class DoctorScheduleViewModel : ListPageViewModel
{
    public string ActiveTab { get; set; } = "current";

    [DataType(DataType.Date)]
    public DateTime? Date { get; set; }

    public IReadOnlyList<AppointmentListItem> Items { get; set; } = [];
}

public class DoctorListItem
{
    public DoctorListItem(DoctorProfile doctor, Department department, UserAccount? linkedUser)
    {
        Doctor = doctor;
        Department = department;
        LinkedUser = linkedUser;
    }

    public DoctorProfile Doctor { get; }
    public Department Department { get; }
    public UserAccount? LinkedUser { get; }
}

public class UserListItem
{
    public UserListItem(UserAccount user, DoctorProfile? doctor, Department? department)
    {
        User = user;
        Doctor = doctor;
        Department = department;
    }

    public UserAccount User { get; }
    public DoctorProfile? Doctor { get; }
    public Department? Department { get; }
}

public record AiSymptomRuleListItem(AiSymptomRule Rule, Department Department);

public record AppointmentListItem(
    Appointment Appointment,
    Patient Patient,
    DoctorProfile Doctor,
    Department Department,
    MedicalRecord? MedicalRecord,
    Invoice? Invoice);

public class DashboardMetricViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Hint { get; set; }
}

public class DashboardActionViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ButtonClass { get; set; } = "btn btn-outline-primary";
}

public class DashboardViewModel
{
    public string Eyebrow { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string AppointmentSectionTitle { get; set; } = string.Empty;
    public string EmptyMessage { get; set; } = string.Empty;
    public IEnumerable<DashboardMetricViewModel> Metrics { get; set; } = [];
    public IEnumerable<DashboardActionViewModel> Actions { get; set; } = [];
    public IEnumerable<AppointmentListItem> Appointments { get; set; } = [];
    public bool ShowDoctorColumn { get; set; }
    public bool ShowDepartmentColumn { get; set; }
    public bool ShowStatusColumn { get; set; }
    public bool ShowReasonColumn { get; set; }
    public bool ShowPaymentColumn { get; set; }
}

public class UserManualViewModel
{
    public string Version { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public bool ShowOnLoad { get; set; }
    public IReadOnlyList<string> Permissions { get; set; } = [];
    public IReadOnlyList<string> Features { get; set; } = [];
    public IReadOnlyList<UserManualWorkflowViewModel> Workflows { get; set; } = [];
    public IReadOnlyList<string> Notes { get; set; } = [];
}

public class UserManualWorkflowViewModel
{
    public string Title { get; set; } = string.Empty;
    public IReadOnlyList<string> Steps { get; set; } = [];
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class ChangePasswordViewModel
{
    public bool IsRequiredBySystem { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "Mật khẩu mới phải có ít nhất 10 ký tự")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9])\S+$", ErrorMessage = "Mật khẩu mới phải có chữ hoa, chữ thường, chữ số, ký tự đặc biệt và không chứa khoảng trắng")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
public class ScheduleEditViewModel : ListPageViewModel
{
    public int DoctorId { get; set; }
    public DoctorProfile? Doctor { get; set; }
    public IEnumerable<WorkSchedule> Schedules { get; set; } = [];
    public IReadOnlyList<WorkSchedule> Items { get; set; } = [];

    [DataType(DataType.Date)]
    public DateTime WorkDate { get; set; } = DateTime.Today;

    [DataType(DataType.Time)]
    public TimeSpan StartTime { get; set; } = new(8, 0, 0);

    [DataType(DataType.Time)]
    public TimeSpan EndTime { get; set; } = new(16, 30, 0);
}

public class AppointmentCreateViewModel
{
    public int Step { get; set; } = 1;

    [Required(ErrorMessage = "Vui lòng nhập nhu cầu khám")]
    public string Reason { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }
    public int? PatientId { get; set; }

    [DataType(DataType.Date)]
    public DateTime DesiredDate { get; set; } = DateTime.Today;

    public string? SelectedTimeSlot { get; set; }
    public string? SelectedSuggestionKey { get; set; }

    public string PatientSearch { get; set; } = string.Empty;
    public bool PatientSearchPerformed { get; set; }
    public Department? SelectedDepartment { get; set; }
    public Patient? SelectedPatient { get; set; }
    public string? CreatePatientReturnUrl { get; set; }
    public IReadOnlySet<int> AiSuggestedDepartmentIds { get; set; } = new HashSet<int>();
    public IEnumerable<Department> Departments { get; set; } = [];
    public IEnumerable<Patient> Patients { get; set; } = [];
    public IReadOnlyList<AvailabilityDayViewModel> AvailabilityDays { get; set; } = [];
    public IEnumerable<TimeSlotSuggestion> SuggestedTimeSlots { get; set; } = [];
    public IEnumerable<AppointmentSuggestion> AppointmentSuggestions { get; set; } = [];
}

public class AvailabilityDayViewModel
{
    public DateTime Date { get; set; }
    public bool IsSelected { get; set; }
    public int AvailableDoctorSlotCount { get; set; }
    public int BusyDoctorSlotCount { get; set; }
    public int TotalDoctorSlotCount { get; set; }
    public int AvailableTimeSlotCount { get; set; }
    public int TotalTimeSlotCount { get; set; }
    public int AvailabilityPercent { get; set; }
    public string LoadLevel { get; set; } = "full";
    public string Summary { get; set; } = string.Empty;
    public IReadOnlyList<AvailabilitySessionViewModel> Sessions { get; set; } = [];
}

public class AvailabilitySessionViewModel
{
    public string Label { get; set; } = string.Empty;
    public int AvailableDoctorSlotCount { get; set; }
    public int BusyDoctorSlotCount { get; set; }
    public int TotalDoctorSlotCount { get; set; }
    public int AvailableTimeSlotCount { get; set; }
    public int TotalTimeSlotCount { get; set; }
    public int AvailabilityPercent { get; set; }
    public string LoadLevel { get; set; } = "full";
}

public class TimeSlotSuggestion
{
    public string TimeSlot { get; set; } = string.Empty;
    public int AvailableDoctorCount { get; set; }
    public int BusyDoctorCount { get; set; }
    public int TotalDoctorCount { get; set; }
    public int AvailabilityPercent { get; set; }
    public string LoadLevel { get; set; } = "full";
    public int Score { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}

public class AppointmentSuggestion
{
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public int Score { get; set; }
    public string SuggestionKey => $"{DoctorId}|{Date:yyyy-MM-dd}|{TimeSlot}";
}

public class RescheduleAppointmentViewModel
{
    public int AppointmentId { get; set; }
    public int DepartmentId { get; set; }
    public AppointmentListItem? Appointment { get; set; }

    [DataType(DataType.Date)]
    public DateTime DesiredDate { get; set; } = DateTime.Today.AddDays(1);

    public string? SelectedSuggestionKey { get; set; }
    public IEnumerable<AppointmentSuggestion> AppointmentSuggestions { get; set; } = [];
}

public class MedicalRecordViewModel
{
    public AppointmentListItem? Appointment { get; set; }
    public MedicalRecord Record { get; set; } = new();
}

public class InvoiceViewModel
{
    public int AppointmentId { get; set; }
    public AppointmentListItem? Appointment { get; set; }
    public Invoice? Invoice { get; set; }
    public IEnumerable<InvoiceDetail> ExistingDetails { get; set; } = [];
    public IEnumerable<ClinicService> Services { get; set; } = [];
    public int[] SelectedServiceIds { get; set; } = [];
    public decimal TotalAmount { get; set; }
}

public class InvoicePrintViewModel
{
    public AppointmentListItem? Appointment { get; set; }
    public Invoice? Invoice { get; set; }
    public IEnumerable<InvoiceDetail> Details { get; set; } = [];
    public DateTime PrintedAt { get; set; } = DateTime.Now;
    public decimal TotalAmount => Details.Sum(x => x.LineTotal);
    public string InvoiceCode => Invoice is null ? string.Empty : $"HD-{Invoice.InvoiceId:00000}";
}

public class ReportDepartmentSummaryViewModel
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int AppointmentCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public decimal Revenue { get; set; }
}

public class ReportsViewModel : ListPageViewModel
{
    [DataType(DataType.Date)]
    public DateTime? FromDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ToDate { get; set; }

    public int? DepartmentId { get; set; }
    public IEnumerable<Department> Departments { get; set; } = [];
    public IEnumerable<ReportDepartmentSummaryViewModel> DepartmentSummaries { get; set; } = [];
    public IEnumerable<AppointmentListItem> Results { get; set; } = [];
    public int TotalAppointments { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public int WaitingPaymentCount { get; set; }
    public decimal Revenue { get; set; }
    public string RangePreset { get; set; } = "all";
    public IEnumerable<RangePresetOptionViewModel> RangePresets { get; set; } = [];
    public DateTime? DisplayedFromDate { get; set; }
    public DateTime? DisplayedToDate { get; set; }
    public IEnumerable<LineChartViewModel> TrendCharts { get; set; } = [];
}

public class RangePresetOptionViewModel
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class MedicalHistoryViewModel : ListPageViewModel
{
    public string? Query { get; set; }

    [DataType(DataType.Date)]
    public DateTime? FromDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ToDate { get; set; }

    public int? DoctorId { get; set; }
    public int? DepartmentId { get; set; }
    public bool IsDoctorRestricted { get; set; }
    public IEnumerable<DoctorProfile> Doctors { get; set; } = [];
    public IEnumerable<Department> Departments { get; set; } = [];
    public DateTime? DisplayedFromDate { get; set; }
    public DateTime? DisplayedToDate { get; set; }
    public IReadOnlyList<AppointmentListItem> Results { get; set; } = [];
}

public class LineChartPointViewModel
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Tooltip { get; set; } = string.Empty;
}

public class LineChartViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string StrokeColor { get; set; } = "#0f766e";
    public string FillColor { get; set; } = "rgba(15, 118, 110, .16)";
    public string MaxLabel { get; set; } = "0";
    public string MidLabel { get; set; } = "0";
    public string MinLabel { get; set; } = "0";
    public IReadOnlyList<LineChartPointViewModel> Points { get; set; } = [];
}


