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

        private readonly BroadcastService _broadcast;

        private readonly IUserRepository _userRepository;

        private readonly IOrderRepository _orderRepository;

        public AdminController(
            BaleMessageService baleService,
            SafirUserRepository repo,
            BroadcastService broadcast,
            IUserRepository userRepository,
            IOrderRepository orderRepository)
        {
            _baleService = baleService;
            _repo = repo;
            _broadcast = broadcast;
            _userRepository = userRepository;
            _orderRepository = orderRepository;
        }
        // DASHBOARD
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalUsers =
                await _userRepository.GetUserCountAsync();

            ViewBag.TotalOrders =
                await _orderRepository.GetOrderCountAsync();

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
                 PhoneUsers =
                     await _repo.GetUsersAsync(),

                 TelegramUsers =
                     await _userRepository.GetAllChatIds()
             };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Send(
            BroadcastPageViewModel vm)
        {
            vm.PhoneUsers =
                await _repo.GetUsersAsync();

            vm.TelegramUsers =
                await _userRepository.GetAllChatIds();

            int success = 0;
            int failed = 0;

            if (vm.RecipientType == "Phone")
            {
                if (vm.SelectedPhones == null ||
                    !vm.SelectedPhones.Any())
                {
                    ViewBag.Error =
                        "حداقل یک شماره تلفن انتخاب کنید.";

                    return View(vm);
                }

                foreach (var phone in vm.SelectedPhones)
                {
                    try
                    {
                        string? fileId = null;

                        if (vm.Attachment != null)
                        {
                            fileId =
                                await _baleService
                                    .UploadFileAsync(
                                        vm.Attachment);
                        }

                        var result =
                            await _baleService.SendMessageAsync(
                                phone,
                                vm.Message,
                                fileId);

                        if (result)
                        {
                            success++;

                            await _repo.SaveLogAsync(
                            vm.Message,
                            true,
                            null,
                            null,
                            phone);
                        }
                        else
                        {
                            failed++;

                            await _repo.SaveLogAsync(
                                
                                vm.Message,
                                false,
                                "Send failed",
                                null,
                                phone);
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;

                        await _repo.SaveLogAsync(
                            vm.Message,
                            false,
                            ex.Message,
                            null,
                            phone);
                    }
                }
            }
            else
            {
                if (vm.SelectedChatIds == null ||
                    !vm.SelectedChatIds.Any())
                {
                    ViewBag.Error =
                        "حداقل یک ChatId انتخاب کنید.";

                    return View(vm);
                }

                foreach (var chatid in vm.SelectedChatIds)
                {
                    try
                    {
                        string? fileId = null;

                        if (vm.Attachment != null)
                        {
                            fileId =
                                await _baleService
                                    .UploadFileAsync(
                                        vm.Attachment);
                        }

                        var result = await _broadcast.SendToUsers(
                             vm.SelectedChatIds,
                             vm.Message,
                             fileId);

                        if (result.SuccessCount > 0)
                        {
                            success++;

                            await _repo.SaveLogAsync(
                            vm.Message,
                            true,
                            null,
                            chatid,
                            null);
                        }
                        else
                        {
                            failed++;

                            await _repo.SaveLogAsync(
                                vm.Message,
                                false,
                                "Send failed",
                                chatid,
                                null);
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;

                        await _repo.SaveLogAsync(
                            vm.Message,
                            false,
                            ex.Message,
                            chatid,
                            null);
                    }
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


        [HttpPost("selected")]
        public async Task<IActionResult> SendSelected(
            [FromBody] BroadcastRequest request)
        {
            await _broadcast.SendToUsers(
                request.ChatIds,
                request.Message);

            return Ok();
        }


        public async Task<IActionResult> Orders()
        {
            var data = await _orderRepository.GetOrdersAsync();

            return View(data);
        }
    }
}