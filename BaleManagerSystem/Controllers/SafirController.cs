using BaleManagerSystem.Models;
using BaleManagerSystem.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BaleManagerSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SafirController : ControllerBase
    {
        private readonly BaleMessageService _baleService;

        private readonly SafirUserRepository _repo;

        public SafirController(
            BaleMessageService baleService,
            SafirUserRepository repo)
        {
            _baleService = baleService;
            _repo = repo;
        }

        // ================= REGISTER USER =================
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            string phone)
        {
            await _repo.SaveUserAsync(phone);

            return Ok("User Registered");
        }

        // ================= SEND BROADCAST =================
        [HttpPost("send")]
        public async Task<IActionResult> Send(
            [FromBody] BroadcastRequest request)
        {
            var users =
                await _repo.GetAllPhonesAsync();

            int success = 0;
            int failed = 0;

            foreach (var phone in users)
            {
                //try
                //{
                //    var result =
                //        await _baleService.SendMessageAsync(
                //            phone,
                //            request.Message);

                //    if (result)
                //    {
                //        success++;

                //        await _repo.SaveLogAsync(
                //            phone,
                //            request.Message,
                //            true);
                //    }
                //    else
                //    {
                //        failed++;

                //        await _repo.SaveLogAsync(
                //            phone,
                //            request.Message,
                //            false,
                //            "Send failed");
                //    }
                //}
                //catch (Exception ex)
                //{
                //    failed++;

                //    await _repo.SaveLogAsync(
                //        phone,
                //        request.Message,
                //        false,
                //        ex.Message);
                //}
            }

            return Ok(new
            {
                Success = success,
                Failed = failed
            });
        }
    }
}
