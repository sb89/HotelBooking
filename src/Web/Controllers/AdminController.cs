using Infrastructure.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    [Route("api/admin")]
    [ApiController]
    public class AdminController(IAdminService adminService) : ControllerBase
    {
        [HttpPost("seed")]
        public async Task<IActionResult> Seed(CancellationToken cancellationToken)
        {
            await adminService.Seed(cancellationToken);
            
            return NoContent();
        }
        
        [HttpPost("reset")]
        public async Task<IActionResult> Reset(CancellationToken cancellationToken)
        {
            await adminService.Reset(cancellationToken);
            
            return NoContent();
        }
    }
}
