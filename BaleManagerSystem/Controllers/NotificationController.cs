using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BaleManagerSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly BaleMessageService _baleService;

        public NotificationController(BaleMessageService baleService)
        {
            _baleService = baleService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send()
        {
            var users = new List<string>
            {
                "989109550275",
                "989378488704"
            };

            await _baleService.SendBulkMessageAsync(
                users,
                "Test Bulk Message");

            return Ok("Messages Sent");
        }
    }
}
