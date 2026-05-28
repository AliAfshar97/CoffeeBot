using BaleManagerSystem.Models;
using BaleManagerSystem.Models.ViewModels;
using BaleManagerSystem.Services;
using Microsoft.AspNetCore.Mvc;
using System.Numerics;
using System.Threading.Tasks;

namespace BaleManagerSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly BaleMessageService _baleService;

        private readonly SafirUserRepository _repo;

        public AdminController(
            BaleMessageService baleService,
            SafirUserRepository repo)
        {
            _baleService = baleService;
            _repo = repo;
        }
        // DASHBOARD
        public IActionResult Dashboard()
        {
            return View();
        }

        // ================= REGISTER =================

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            var users =
                await _repo.GetUsersAsync();

            ViewBag.Users = users;

            return View(new RegisterUserViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterUserViewModel model)
        {
            var users =
                    await _repo.GetUsersAsync();

            if (!ModelState.IsValid)
            {
                ViewBag.Users = users;

                return View(model);
            }

            if (users != null)
            {
                var isExistPhoneNumber = users.Where(u => u.PhoneNumber == model.PhoneNumber).Count() > 0;

                if (isExistPhoneNumber)
                {
                    ViewBag.Message =
                       "شماره تلفن همراه کاربر از قبل وارد شده است.";

                    return View(users);
                }
            }
            try
            {
                await _repo.SaveUserAsync(model.PhoneNumber);

                ViewBag.Message =
                    "شماره همراه با موفقیت اضافه شد.";
            }
            catch (Exception)
            {
                ViewBag.Message =
                    "ثبت کاربر با خطا مواجه شد.";
            }
            
            var allUsers =
                await _repo.GetUsersAsync();

            ViewBag.Users = allUsers;

            ModelState.Clear();

            return View(new RegisterUserViewModel());
        }

        // ================= SEND =================

        [HttpGet]
        public IActionResult Send()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Send(BroadcastRequest request)
        {
            var users =
                await _repo.GetAllPhonesAsync();

            int success = 0;
            int failed = 0;

            foreach (var phone in users)
            {
                try
                {
                    var result =
                        await _baleService.SendMessageAsync(
                            phone,
                            request.Message);

                    if (result)
                    {
                        success++;

                        await _repo.SaveLogAsync(
                            phone,
                            request.Message,
                            true);
                    }
                    else
                    {
                        failed++;

                        await _repo.SaveLogAsync(
                            phone,
                            request.Message,
                            false,
                            "خطا در ارسال");
                    }
                }
                catch (Exception ex)
                {
                    failed++;

                    await _repo.SaveLogAsync(
                        phone,
                        request.Message,
                        false,
                        ex.Message);
                }
            }

            //return Ok(new
            //{
            //    Success = success,
            //    Failed = failed
            //});
            return View();
        }

        // ================= LOGS =================

        public async Task<IActionResult> Logs()
        {
            var getLog = await _repo.GetLogsAsync();

            return View(getLog);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            await _repo.DeleteUserAsync(id);

            return RedirectToAction(nameof(Register));
        }
    }
}