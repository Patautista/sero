using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SupportServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PingController : ControllerBase
    {
        [HttpGet()]
        public async Task<bool> GetPing()
        {
            return true;
        }
    }
}
