using System.Security.Claims;
using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Controllers;

public class AuthController(ClinicStore store) : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (IsPasswordChangeRequired())
            {
                return RedirectToAction(nameof(ChangePassword), new { required = 1 });
            }

            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = store.Authenticate(model.Username, model.Password);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
            return View(model);
        }

        await HttpContext.SignInAsync("ClinicCookie", BuildPrincipal(user));
        if (user.MustChangePassword)
        {
            return RedirectToAction(nameof(ChangePassword), new { required = 1 });
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("ClinicCookie");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Logout(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction(nameof(Login), new { ReturnUrl = returnUrl });
        }

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Denied() => View();

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword(bool required = false)
    {
        return View(new ChangePasswordViewModel
        {
            IsRequiredBySystem = required || IsPasswordChangeRequired()
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        model.IsRequiredBySystem = IsPasswordChangeRequired();
        if (string.Equals(model.CurrentPassword, model.NewPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "Mật khẩu mới phải khác mật khẩu hiện tại.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return Forbid();
        }

        try
        {
            store.ChangePassword(userId, model.CurrentPassword, model.NewPassword);
            await HttpContext.SignInAsync("ClinicCookie", BuildPrincipal(store.GetUser(userId)));
            TempData["Message"] = "Đã đổi mật khẩu.";
            return RedirectToAction("Index", "Home");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    private ClaimsPrincipal BuildPrincipal(UserAccount user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("MustChangePassword", user.MustChangePassword.ToString())
        };

        if (user.DoctorId is not null)
        {
            claims.Add(new Claim("DoctorId", user.DoctorId.Value.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "ClinicCookie"));
    }

    private bool IsPasswordChangeRequired()
    {
        return bool.TryParse(User.FindFirstValue("MustChangePassword"), out var required) && required;
    }
}
