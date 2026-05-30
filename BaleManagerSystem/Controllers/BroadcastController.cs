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
    public class BroadcastController : ControllerBase
    {
        private readonly BroadcastService _broadcast;

        public BroadcastController(BroadcastService broadcast)
        {
            _broadcast = broadcast;
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
        }
}
