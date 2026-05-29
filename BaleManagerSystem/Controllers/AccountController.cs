using BaleManagerSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BaleManagerSystem.Controllers
{
    public class AccountController : Controller
    {
        // ================= LOGIN PAGE =================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        // ================= LOGIN POST =================

        [HttpPost]
        public async Task<IActionResult> Login(
            LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // DEMO LOGIN
            // Replace later with database check

            if (model.UserName != "admin"
                || model.Password != "12345678q@")
            {
                ModelState.AddModelError(
                    "",
                    "Invalid username or password.");

                return View(model);
            }

            var claims =
                new List<Claim>
                {
                    new Claim(
                        ClaimTypes.Name,
                        model.UserName),

                    new Claim(
                        ClaimTypes.Role,
                        "Admin")
                };

            var identity =
                new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults
                        .AuthenticationScheme);

            var principal =
                new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme,
                principal);

            return RedirectToAction(
                "Dashboard",
                "Admin");
        }


        // ================= LOGOUT =================

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme);

            return RedirectToAction(
                "Login");
        }
    }
}
