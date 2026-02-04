using Infrastructure.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    [Route("api/admin")]
    [ApiController]
    public class AdminController(IAdminService adminService) : ControllerBase
    {
        /// <summary>
        /// Seed the database with test data (3 hotels with 6 rooms each)
        /// </summary>
        [HttpPost("seed")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Seed(CancellationToken cancellationToken)
        {
            await adminService.Seed(cancellationToken);

            return NoContent();
        }

        /// <summary>
        /// Reset the database by removing all data
        /// </summary>
        [HttpPost("reset")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Reset(CancellationToken cancellationToken)
        {
            await adminService.Reset(cancellationToken);

            return NoContent();
        }
    }
}
