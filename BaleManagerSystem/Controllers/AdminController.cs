using BaleManagerSystem.Models;
using BaleManagerSystem.Services;
using Microsoft.AspNetCore.Mvc;
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
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string phone)
        {
            await _repo.SaveUserAsync(phone);

            ViewBag.Message = "User Registered";

            return  View();
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
                            "Send failed");
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
    }
}