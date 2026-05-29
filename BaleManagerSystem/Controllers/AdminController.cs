using BaleManagerSystem.Models;
using BaleManagerSystem.Models.ViewModels;
using BaleManagerSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Numerics;
using System.Threading.Tasks;

namespace BaleManagerSystem.Controllers
{
    [Authorize]
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
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalUsers =
                await _repo.GetTotalUsersCountAsync();

            ViewBag.Success =
                await _repo.GetSuccessLogsCountAsync();

            ViewBag.Failed =
                await _repo.GetFailedLogsCountAsync();

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
                await _repo.SaveUserAsync(model.PhoneNumber, model.Username);

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
        public async Task<IActionResult> Send()
        {
            var vm =
             new BroadcastPageViewModel
             {
                 Users =
                     await _repo.GetUsersAsync()
             };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Send(BroadcastPageViewModel vm)
        {
            vm.Users =
                await _repo.GetUsersAsync();

            if (vm.SelectedPhones == null
                || !vm.SelectedPhones.Any())
            {
                ViewBag.Error =
                    "انتخاب یک شماره الزامی است.";

                return View(vm);
            }

            int success = 0;
            int failed = 0;

            foreach (var phone in vm.SelectedPhones)
            {
                try
                {
                    var result =
                        await _baleService.SendMessageAsync(
                            phone,
                            vm.Message);

                    if (result)
                    {
                        success++;

                        await _repo.SaveLogAsync(
                            phone,
                            vm.Message,
                            true);
                    }
                    else
                    {
                        failed++;

                        await _repo.SaveLogAsync(
                            phone,
                            vm.Message,
                            false,
                            "Send failed");
                    }
                }
                catch (Exception ex)
                {
                    failed++;

                    await _repo.SaveLogAsync(
                        phone,
                        vm.Message,
                        false,
                        ex.Message);
                }
            }

            ViewBag.Success = success;

            ViewBag.Failed = failed;

            return View(vm);
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