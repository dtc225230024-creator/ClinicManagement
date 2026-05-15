using ClinicManagement.Models;
using ClinicManagement.ViewModels;

namespace ClinicManagement.Services;

public class UserManualService
{
    public const string CurrentVersion = "2026.05.14";

    public bool ShouldShow(UserAccount user)
    {
        return !user.MustChangePassword &&
               !string.Equals(user.ManualSeenVersion, CurrentVersion, StringComparison.OrdinalIgnoreCase);
    }

    public UserManualViewModel Build(UserRole role, bool showOnLoad)
    {
        var model = role switch
        {
            UserRole.Admin => BuildAdminManual(),
            UserRole.Receptionist => BuildReceptionManual(),
            UserRole.Doctor => BuildDoctorManual(),
            _ => BuildReceptionManual()
        };

        model.Version = CurrentVersion;
        model.ShowOnLoad = showOnLoad;
        return model;
    }

    private static UserManualViewModel BuildAdminManual()
    {
        return new UserManualViewModel
        {
            RoleName = "Admin",
            Title = "Thông tin sử dụng cho quản trị viên",
            Summary = "Tài khoản Admin quản lý cấu hình hệ thống, tài khoản người dùng, danh mục và theo dõi số liệu tổng quan.",
            Permissions =
            [
                "Quản lý tài khoản, vai trò, trạng thái khóa/mở và reset mật khẩu tạm thời.",
                "Liên kết tài khoản bác sĩ với hồ sơ bác sĩ tương ứng.",
                "Quản lý bác sĩ, chuyên khoa, dịch vụ khám, lịch làm việc và luật gợi ý.",
                "Xem lịch khám toàn hệ thống, hồ sơ khám và báo cáo thống kê."
            ],
            Features =
            [
                "Tài khoản: tạo user mới, sửa vai trò, reset mật khẩu, khóa/mở tài khoản.",
                "Bác sĩ: thêm/sửa bác sĩ, liên kết tài khoản và quản lý ca làm việc.",
                "Luật gợi ý: thêm cụm triệu chứng, gán chuyên khoa và điều chỉnh điểm ưu tiên.",
                "Thống kê: lọc nhanh khoảng thời gian, xem biểu đồ và bảng dữ liệu chi tiết."
            ],
            Workflows =
            [
                Workflow("Tạo tài khoản bác sĩ",
                [
                    "Mở Bác sĩ và tạo hồ sơ bác sĩ nếu chưa có.",
                    "Mở Tài khoản, thêm user vai trò Doctor.",
                    "Chọn đúng bác sĩ liên kết, lưu tài khoản.",
                    "Thông báo mật khẩu tạm thời cho bác sĩ; hệ thống sẽ bắt đổi mật khẩu lần đầu."
                ]),
                Workflow("Cập nhật luật gợi ý",
                [
                    "Mở Luật gợi ý.",
                    "Tìm cụm triệu chứng hiện có để tránh trùng lặp.",
                    "Thêm hoặc sửa cụm triệu chứng, chọn chuyên khoa và điểm ưu tiên.",
                    "Lưu thay đổi; danh sách gợi ý sẽ được cập nhật tự động."
                ])
            ],
            Notes =
            [
                "Không khóa admin cuối cùng đang hoạt động.",
                "Không ngưng chuyên khoa/bác sĩ nếu đang có ràng buộc nghiệp vụ chưa xử lý.",
                "Reset mật khẩu sẽ bắt người dùng đổi mật khẩu khi đăng nhập lại."
            ]
        };
    }

    private static UserManualViewModel BuildReceptionManual()
    {
        return new UserManualViewModel
        {
            RoleName = "Lễ tân",
            Title = "Thông tin sử dụng cho lễ tân",
            Summary = "Tài khoản lễ tân tiếp nhận bệnh nhân, đặt lịch khám, đổi/hủy lịch và thanh toán sau khi bác sĩ hoàn tất khám.",
            Permissions =
            [
                "Quản lý hồ sơ bệnh nhân.",
                "Đặt lịch khám theo gợi ý chuyên khoa, khung giờ và bác sĩ.",
                "Đổi lịch, hủy lịch khi lịch còn hợp lệ.",
                "Lập hóa đơn, chọn dịch vụ, thanh toán và in hóa đơn."
            ],
            Features =
            [
                "Bệnh nhân: tìm kiếm, tạo mới và cập nhật hồ sơ.",
                "Đặt lịch khám: nhập nhu cầu, chọn chuyên khoa, chọn bệnh nhân, chọn khung giờ và bác sĩ.",
                "Lịch khám: lọc, sắp xếp, đổi lịch, hủy lịch và mở hóa đơn.",
                "Hóa đơn: chọn dịch vụ, lưu thanh toán và in phiếu."
            ],
            Workflows =
            [
                Workflow("Đặt lịch khám",
                [
                    "Nhập nhu cầu khám để hệ thống gợi ý chuyên khoa.",
                    "Chọn chuyên khoa phù hợp và sang bước tra cứu bệnh nhân.",
                    "Tìm hồ sơ bệnh nhân; nếu chưa có thì tạo hồ sơ mới và quay lại luồng đặt lịch.",
                    "Chọn ngày, khung giờ, bác sĩ gợi ý và xác nhận đặt lịch."
                ]),
                Workflow("Thanh toán sau khám",
                [
                    "Mở Lịch khám và tìm lịch đã hoàn tất khám.",
                    "Mở Hóa đơn, chọn dịch vụ đã sử dụng.",
                    "Lưu thanh toán, sau đó in hóa đơn nếu cần."
                ])
            ],
            Notes =
            [
                "Lễ tân là người quyết định chuyên khoa cuối cùng; kết quả gợi ý chỉ có vai trò tham khảo.",
                "Chỉ thanh toán khi bác sĩ đã nhập kết quả khám.",
                "Không hủy lịch đã có hóa đơn."
            ]
        };
    }

    private static UserManualViewModel BuildDoctorManual()
    {
        return new UserManualViewModel
        {
            RoleName = "Bác sĩ",
            Title = "Thông tin sử dụng cho bác sĩ",
            Summary = "Tài khoản bác sĩ xem lịch cá nhân, mở chi tiết lịch khám và nhập kết quả khám cho bệnh nhân.",
            Permissions =
            [
                "Xem lịch khám cá nhân theo tài khoản đã liên kết với hồ sơ bác sĩ.",
                "Xem thông tin bệnh nhân và nhu cầu khám trong lịch được phân công.",
                "Nhập triệu chứng, chẩn đoán, kết quả khám và ghi chú.",
                "Tra cứu hồ sơ khám trong phạm vi được phép."
            ],
            Features =
            [
                "Lịch cá nhân: tách tab lịch hiện tại và lịch đã hoàn thành.",
                "Chi tiết lịch: xem bệnh nhân, chuyên khoa, thời gian và lý do khám.",
                "Kết quả khám: lưu thông tin khám và chuyển lịch sang trạng thái đã hoàn tất.",
                "Hồ sơ khám: tìm lại lịch sử khám khi cần đối chiếu."
            ],
            Workflows =
            [
                Workflow("Nhập kết quả khám",
                [
                    "Mở Lịch cá nhân.",
                    "Chọn lịch đang chờ khám.",
                    "Mở form kết quả khám.",
                    "Nhập đầy đủ triệu chứng, chẩn đoán, kết quả và ghi chú nếu có.",
                    "Lưu kết quả để lễ tân có thể thực hiện thanh toán."
                ])
            ],
            Notes =
            [
                "Bác sĩ chỉ thấy lịch gắn với tài khoản của mình.",
                "Sau khi lưu kết quả, lịch được tính là đã hoàn thành.",
                "Nếu không thấy lịch cá nhân, cần báo Admin kiểm tra liên kết tài khoản - bác sĩ."
            ]
        };
    }

    private static UserManualWorkflowViewModel Workflow(string title, IReadOnlyList<string> steps)
    {
        return new UserManualWorkflowViewModel
        {
            Title = title,
            Steps = steps
        };
    }
}
