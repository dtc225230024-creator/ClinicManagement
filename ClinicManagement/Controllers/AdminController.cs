using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClinicManagement.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(ClinicStore store, AiSchedulingService ai) : Controller
{
    public IActionResult Users(string? q, UserRole? role, string? status, string? passwordState, string? sort, int page = 1, int pageSize = 10)
    {
        var doctors = store.Doctors.ToDictionary(x => x.DoctorId);
        var departments = store.Departments.ToDictionary(x => x.DepartmentId);
        var model = new UserDirectoryViewModel
        {
            Query = q,
            Role = role,
            Status = status,
            PasswordState = passwordState,
            Sort = string.IsNullOrWhiteSpace(sort) ? "created_desc" : sort,
            Page = page,
            PageSize = pageSize
        };

        IEnumerable<UserListItem> items = store.Users
            .Select(user =>
            {
                var doctor = user.DoctorId is not null && doctors.TryGetValue(user.DoctorId.Value, out var linkedDoctor)
                    ? linkedDoctor
                    : null;
                var department = doctor is not null && departments.TryGetValue(doctor.DepartmentId, out var linkedDepartment)
                    ? linkedDepartment
                    : null;

                return new UserListItem(user, doctor, department);
            });

        if (!string.IsNullOrWhiteSpace(q))
        {
            items = items.Where(item =>
                item.User.UserId.ToString() == q ||
                item.User.Username.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                item.User.Role.ToString().Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (item.Doctor?.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (item.Department?.DepartmentName.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (role is not null)
        {
            items = items.Where(item => item.User.Role == role.Value);
        }

        if (status == "active")
        {
            items = items.Where(item => item.User.IsActive);
        }
        else if (status == "inactive")
        {
            items = items.Where(item => !item.User.IsActive);
        }

        if (passwordState == "temporary")
        {
            items = items.Where(item => item.User.MustChangePassword);
        }
        else if (passwordState == "stable")
        {
            items = items.Where(item => !item.User.MustChangePassword);
        }

        items = model.Sort switch
        {
            "username" => items.OrderBy(item => item.User.Username).ThenBy(item => item.User.UserId),
            "username_desc" => items.OrderByDescending(item => item.User.Username).ThenByDescending(item => item.User.UserId),
            "role" => items.OrderBy(item => item.User.Role).ThenBy(item => item.User.Username),
            "role_desc" => items.OrderByDescending(item => item.User.Role).ThenBy(item => item.User.Username),
            "status" => items.OrderBy(item => item.User.IsActive).ThenBy(item => item.User.Username),
            "status_desc" => items.OrderByDescending(item => item.User.IsActive).ThenBy(item => item.User.Username),
            "created" => items.OrderBy(item => item.User.CreatedAt).ThenBy(item => item.User.UserId),
            _ => items.OrderByDescending(item => item.User.CreatedAt).ThenByDescending(item => item.User.UserId)
        };

        model.Items = PagingHelper.ApplyPaging(items, model);
        return View(model);
    }

    [HttpGet]
    public IActionResult EditUser(int? id, int? doctorId = null)
    {
        UserAccount user;
        if (id is not null)
        {
            user = store.Users.First(x => x.UserId == id);
        }
        else if (doctorId is not null)
        {
            user = new UserAccount
            {
                Role = UserRole.Doctor,
                DoctorId = doctorId,
                IsActive = true,
                Username = store.SuggestDoctorUsername(doctorId.Value)
            };
        }
        else
        {
            user = new UserAccount
            {
                IsActive = true
            };
        }

        user.Password = string.Empty;
        PopulateAssignableDoctors(user.UserId == 0 ? null : user.UserId, user.DoctorId);
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditUser(UserAccount user)
    {
        PopulateAssignableDoctors(user.UserId == 0 ? null : user.UserId, user.DoctorId);

        ModelState.Remove(nameof(UserAccount.Password));
        user.Password = string.Empty;

        if (!ModelState.IsValid)
        {
            return View(user);
        }

        try
        {
            var isNewUser = user.UserId == 0;
            var temporaryPassword = store.SaveUser(user);
            if (isNewUser)
            {
                TempData["TemporaryUsername"] = user.Username;
                TempData["TemporaryPassword"] = temporaryPassword;
                TempData["Message"] = "Đã tạo tài khoản với mật khẩu tạm thời ngẫu nhiên.";
            }
            else
            {
                TempData["Message"] = "Đã lưu tài khoản.";
            }

            return RedirectToAction(nameof(Users));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(user);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleUser(int id)
    {
        try
        {
            store.ToggleUser(id);
            TempData["Message"] = "Đã cập nhật trạng thái tài khoản.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResetPassword(int id)
    {
        try
        {
            var temporaryPassword = store.ResetPassword(id);
            var user = store.GetUser(id);
            TempData["TemporaryUsername"] = user.Username;
            TempData["TemporaryPassword"] = temporaryPassword;
            TempData["Message"] = "Đã đặt lại mật khẩu tạm thời ngẫu nhiên.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Users));
    }

    public IActionResult Doctors(string? q, int? departmentId, string? status, string? accountState, string? sort, int page = 1, int pageSize = 10)
    {
        var usersByDoctorId = store.Users
            .Where(x => x.DoctorId is not null)
            .ToDictionary(x => x.DoctorId!.Value);
        var departments = store.Departments.ToDictionary(x => x.DepartmentId);
        var model = new DoctorDirectoryViewModel
        {
            Query = q,
            DepartmentId = departmentId,
            Status = status,
            AccountState = accountState,
            Sort = string.IsNullOrWhiteSpace(sort) ? "name" : sort,
            Page = page,
            PageSize = pageSize,
            Departments = store.Departments
                .Where(x => x.IsActive || x.DepartmentId == departmentId)
                .OrderBy(x => x.DepartmentName)
                .ToList()
        };

        IEnumerable<DoctorListItem> items = store.Doctors.Select(d =>
            new DoctorListItem(
                d,
                departments[d.DepartmentId],
                usersByDoctorId.GetValueOrDefault(d.DoctorId)));

        if (!string.IsNullOrWhiteSpace(q))
        {
            items = items.Where(item =>
                item.Doctor.DoctorId.ToString() == q ||
                item.Doctor.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (item.Doctor.Phone?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (item.Doctor.Email?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                item.Department.DepartmentName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (item.LinkedUser?.Username.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (departmentId is not null)
        {
            items = items.Where(item => item.Department.DepartmentId == departmentId.Value);
        }

        if (status == "active")
        {
            items = items.Where(item => item.Doctor.IsActive);
        }
        else if (status == "inactive")
        {
            items = items.Where(item => !item.Doctor.IsActive);
        }

        if (accountState == "linked")
        {
            items = items.Where(item => item.LinkedUser is not null);
        }
        else if (accountState == "unlinked")
        {
            items = items.Where(item => item.LinkedUser is null);
        }
        else if (accountState == "temporary")
        {
            items = items.Where(item => item.LinkedUser?.MustChangePassword == true);
        }

        items = model.Sort switch
        {
            "department" => items.OrderBy(item => item.Department.DepartmentName).ThenBy(item => item.Doctor.FullName),
            "department_desc" => items.OrderByDescending(item => item.Department.DepartmentName).ThenBy(item => item.Doctor.FullName),
            "status" => items.OrderBy(item => item.Doctor.IsActive).ThenBy(item => item.Doctor.FullName),
            "status_desc" => items.OrderByDescending(item => item.Doctor.IsActive).ThenBy(item => item.Doctor.FullName),
            "account" => items.OrderBy(item => item.LinkedUser is null ? 1 : 0).ThenBy(item => item.LinkedUser?.Username),
            "account_desc" => items.OrderByDescending(item => item.LinkedUser is null ? 1 : 0).ThenBy(item => item.LinkedUser?.Username),
            "name_desc" => items.OrderByDescending(item => item.Doctor.FullName).ThenBy(item => item.Doctor.DoctorId),
            _ => items.OrderBy(item => item.Doctor.FullName).ThenBy(item => item.Doctor.DoctorId)
        };

        model.Items = PagingHelper.ApplyPaging(items, model);
        return View(model);
    }

    public IActionResult Departments(string? q, string? status, string? sort, int page = 1, int pageSize = 10)
    {
        var model = new DepartmentDirectoryViewModel
        {
            Query = q,
            Status = status,
            Sort = string.IsNullOrWhiteSpace(sort) ? "name" : sort,
            Page = page,
            PageSize = pageSize
        };

        IEnumerable<Department> items = store.Departments;

        if (!string.IsNullOrWhiteSpace(q))
        {
            items = items.Where(item =>
                item.DepartmentId.ToString() == q ||
                item.DepartmentName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (item.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (status == "active")
        {
            items = items.Where(item => item.IsActive);
        }
        else if (status == "inactive")
        {
            items = items.Where(item => !item.IsActive);
        }

        items = model.Sort switch
        {
            "id" => items.OrderBy(item => item.DepartmentId),
            "id_desc" => items.OrderByDescending(item => item.DepartmentId),
            "status" => items.OrderBy(item => item.IsActive).ThenBy(item => item.DepartmentName),
            "status_desc" => items.OrderByDescending(item => item.IsActive).ThenBy(item => item.DepartmentName),
            "name_desc" => items.OrderByDescending(item => item.DepartmentName).ThenBy(item => item.DepartmentId),
            _ => items.OrderBy(item => item.DepartmentName).ThenBy(item => item.DepartmentId)
        };

        model.Items = PagingHelper.ApplyPaging(items, model);
        return View(model);
    }

    [HttpGet]
    public IActionResult EditDepartment(int? id)
    {
        var department = id is null ? new Department() : store.Departments.First(x => x.DepartmentId == id);
        return View(department);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditDepartment(Department department)
    {
        if (!ModelState.IsValid)
        {
            return View(department);
        }

        try
        {
            store.SaveDepartment(department);
            TempData["Message"] = "Đã lưu chuyên khoa.";
            return RedirectToAction(nameof(Departments));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(department);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleDepartment(int id)
    {
        try
        {
            store.ToggleDepartment(id);
            TempData["Message"] = "Đã cập nhật trạng thái chuyên khoa.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Departments));
    }

    public IActionResult AiRules(string? q, int? departmentId, string? status, string? sort, int page = 1, int pageSize = 20)
    {
        var departments = store.Departments.ToDictionary(x => x.DepartmentId);
        var model = new AiSymptomRuleDirectoryViewModel
        {
            Query = q,
            DepartmentId = departmentId,
            Status = status,
            Sort = string.IsNullOrWhiteSpace(sort) ? "department" : sort,
            Page = page,
            PageSize = pageSize,
            Departments = store.Departments
                .Where(x => x.IsActive || x.DepartmentId == departmentId)
                .OrderBy(x => x.DepartmentName)
                .ToList()
        };

        IEnumerable<AiSymptomRuleListItem> items = store.AiSymptomRules
            .Where(rule => departments.ContainsKey(rule.DepartmentId))
            .Select(rule => new AiSymptomRuleListItem(rule, departments[rule.DepartmentId]));

        if (!string.IsNullOrWhiteSpace(q))
        {
            items = items.Where(item =>
                item.Rule.AiSymptomRuleId.ToString() == q ||
                item.Rule.Term.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                item.Rule.NormalizedTerm.Contains(VietnameseTextNormalizer.Normalize(q), StringComparison.OrdinalIgnoreCase) ||
                item.Department.DepartmentName.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        if (departmentId is not null)
        {
            items = items.Where(item => item.Department.DepartmentId == departmentId.Value);
        }

        if (status == "active")
        {
            items = items.Where(item => item.Rule.IsActive);
        }
        else if (status == "inactive")
        {
            items = items.Where(item => !item.Rule.IsActive);
        }

        items = model.Sort switch
        {
            "term" => items.OrderBy(item => item.Rule.Term).ThenBy(item => item.Department.DepartmentName),
            "term_desc" => items.OrderByDescending(item => item.Rule.Term).ThenBy(item => item.Department.DepartmentName),
            "score" => items.OrderBy(item => item.Rule.Score).ThenBy(item => item.Rule.Term),
            "score_desc" => items.OrderByDescending(item => item.Rule.Score).ThenBy(item => item.Rule.Term),
            "status" => items.OrderBy(item => item.Rule.IsActive).ThenBy(item => item.Rule.Term),
            "status_desc" => items.OrderByDescending(item => item.Rule.IsActive).ThenBy(item => item.Rule.Term),
            "department_desc" => items.OrderByDescending(item => item.Department.DepartmentName).ThenBy(item => item.Rule.Term),
            _ => items.OrderBy(item => item.Department.DepartmentName).ThenBy(item => item.Rule.Term)
        };

        model.Items = PagingHelper.ApplyPaging(items, model);
        return View(model);
    }

    [HttpGet]
    public IActionResult EditAiRule(int? id, int? departmentId = null)
    {
        ViewBag.Departments = store.Departments.Where(d => d.IsActive);
        var rule = id is null
            ? new AiSymptomRule
            {
                DepartmentId = departmentId ?? store.Departments.First(d => d.IsActive).DepartmentId,
                Score = 10,
                IsActive = true
            }
            : store.AiSymptomRules.First(x => x.AiSymptomRuleId == id);

        return View(rule);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditAiRule(AiSymptomRule rule)
    {
        ViewBag.Departments = store.Departments.Where(d => d.IsActive);
        ModelState.Remove(nameof(AiSymptomRule.NormalizedTerm));
        rule.NormalizedTerm = VietnameseTextNormalizer.Normalize(rule.Term);

        if (!ModelState.IsValid)
        {
            return View(rule);
        }

        try
        {
            store.SaveAiSymptomRule(rule);
            ai.ClearSymptomRuleCache();
            TempData["Message"] = "Đã lưu luật gợi ý.";
            return RedirectToAction(nameof(AiRules), new { departmentId = rule.DepartmentId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(rule);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleAiRule(int id)
    {
        store.ToggleAiSymptomRule(id);
        ai.ClearSymptomRuleCache();
        TempData["Message"] = "Đã cập nhật trạng thái luật gợi ý.";
        return RedirectToAction(nameof(AiRules));
    }

    public IActionResult Services(string? q, string? status, string? sort, int page = 1, int pageSize = 10)
    {
        var model = new ServiceDirectoryViewModel
        {
            Query = q,
            Status = status,
            Sort = string.IsNullOrWhiteSpace(sort) ? "name" : sort,
            Page = page,
            PageSize = pageSize
        };

        IEnumerable<ClinicService> items = store.Services;

        if (!string.IsNullOrWhiteSpace(q))
        {
            items = items.Where(item =>
                item.ServiceId.ToString() == q ||
                item.ServiceName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (item.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (status == "active")
        {
            items = items.Where(item => item.IsActive);
        }
        else if (status == "inactive")
        {
            items = items.Where(item => !item.IsActive);
        }

        items = model.Sort switch
        {
            "price" => items.OrderBy(item => item.Price).ThenBy(item => item.ServiceName),
            "price_desc" => items.OrderByDescending(item => item.Price).ThenBy(item => item.ServiceName),
            "status" => items.OrderBy(item => item.IsActive).ThenBy(item => item.ServiceName),
            "status_desc" => items.OrderByDescending(item => item.IsActive).ThenBy(item => item.ServiceName),
            "name_desc" => items.OrderByDescending(item => item.ServiceName).ThenBy(item => item.ServiceId),
            _ => items.OrderBy(item => item.ServiceName).ThenBy(item => item.ServiceId)
        };

        model.Items = PagingHelper.ApplyPaging(items, model);
        return View(model);
    }

    [HttpGet]
    public IActionResult EditService(int? id)
    {
        var service = id is null ? new ClinicService() : store.Services.First(x => x.ServiceId == id);
        return View(service);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditService(ClinicService service)
    {
        if (!ModelState.IsValid)
        {
            return View(service);
        }

        try
        {
            store.SaveService(service);
            TempData["Message"] = "Đã lưu dịch vụ khám.";
            return RedirectToAction(nameof(Services));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(service);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleService(int id)
    {
        store.ToggleService(id);
        TempData["Message"] = "Đã cập nhật trạng thái dịch vụ.";
        return RedirectToAction(nameof(Services));
    }

    [HttpGet]
    public IActionResult EditDoctor(int? id)
    {
        ViewBag.Departments = store.Departments.Where(d => d.IsActive);
        var doctor = id is null ? new DoctorProfile() : store.Doctors.First(x => x.DoctorId == id);
        return View(doctor);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditDoctor(DoctorProfile doctor)
    {
        ViewBag.Departments = store.Departments.Where(d => d.IsActive);
        if (!ModelState.IsValid)
        {
            return View(doctor);
        }

        try
        {
            store.SaveDoctor(doctor);
            TempData["Message"] = "Đã lưu thông tin bác sĩ.";
            return RedirectToAction(nameof(Doctors));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(doctor);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleDoctor(int id)
    {
        try
        {
            store.ToggleDoctor(id);
            TempData["Message"] = "Đã cập nhật trạng thái bác sĩ.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Doctors));
    }

    [HttpGet]
    public IActionResult Schedule(int id, string? sort, int page = 1, int pageSize = 10)
    {
        var doctor = store.Doctors.First(x => x.DoctorId == id);
        var model = new ScheduleEditViewModel
        {
            DoctorId = id,
            Doctor = doctor,
            Sort = string.IsNullOrWhiteSpace(sort) ? "date" : sort,
            Page = page,
            PageSize = pageSize
        };

        IEnumerable<WorkSchedule> schedules = store.Schedules.Where(s => s.DoctorId == id && s.IsActive);
        schedules = model.Sort switch
        {
            "date_desc" => schedules.OrderByDescending(s => s.WorkDate).ThenByDescending(s => s.StartTime),
            "start" => schedules.OrderBy(s => s.StartTime).ThenBy(s => s.WorkDate),
            "start_desc" => schedules.OrderByDescending(s => s.StartTime).ThenByDescending(s => s.WorkDate),
            "end" => schedules.OrderBy(s => s.EndTime).ThenBy(s => s.WorkDate),
            "end_desc" => schedules.OrderByDescending(s => s.EndTime).ThenByDescending(s => s.WorkDate),
            _ => schedules.OrderBy(s => s.WorkDate).ThenBy(s => s.StartTime)
        };

        model.Items = PagingHelper.ApplyPaging(schedules, model);
        model.Schedules = model.Items;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddSchedule(ScheduleEditViewModel model)
    {
        try
        {
            store.AddSchedule(new WorkSchedule
            {
                DoctorId = model.DoctorId,
                WorkDate = model.WorkDate,
                StartTime = model.StartTime,
                EndTime = model.EndTime
            });
            TempData["Message"] = "Đã thêm ca làm việc.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Schedule), new { id = model.DoctorId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveSchedule(int id, int doctorId)
    {
        try
        {
            store.RemoveSchedule(id);
            TempData["Message"] = "Đã ẩn ca làm việc.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Schedule), new { id = doctorId });
    }

    private void PopulateAssignableDoctors(int? userId, int? selectedDoctorId)
    {
        var departments = store.Departments.ToDictionary(x => x.DepartmentId, x => x.DepartmentName);
        var assignedDoctorIds = store.Users
            .Where(x => x.UserId != userId && x.DoctorId != null)
            .Select(x => x.DoctorId!.Value)
            .ToHashSet();

        ViewBag.AssignableDoctors = store.GetAssignableDoctors(userId, selectedDoctorId)
            .Select(doctor =>
            {
                var label = $"#{doctor.DoctorId} - {doctor.FullName} - {departments[doctor.DepartmentId]}";
                if (!doctor.IsActive)
                {
                    label += " - ngưng hoạt động";
                }
                else if (assignedDoctorIds.Contains(doctor.DoctorId))
                {
                    label += " - đã liên kết với tài khoản khác";
                }

                return new SelectListItem(label, doctor.DoctorId.ToString());
            })
            .ToList();
    }
}
