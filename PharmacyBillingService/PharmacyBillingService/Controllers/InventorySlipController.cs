using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyBillingService.DTOs;
using PharmacyBillingService.Security;
using PharmacyBillingService.Services;

namespace PharmacyBillingService.Controllers
{
    [ApiController]
    [Route("api/inventory/slips")]
    public class InventorySlipController : ControllerBase
    {
        private readonly IInventorySlipService _slipService;

        public InventorySlipController(IInventorySlipService slipService)
        {
            _slipService = slipService;
        }

        /// <summary>
        /// Y tá tạo phiếu yêu cầu nhập kho (trạng thái Pending, chưa cộng tồn kho)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = RoleConstants.AdminOrNurse)]
        public async Task<IActionResult> CreateSlip([FromBody] CreateInventorySlipDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            try
            {
                var result = await _slipService.CreateSlipAsync(dto, userId.Value);
                return CreatedAtAction(nameof(GetSlipById), new { slipId = result.SlipId }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Admin duyệt phiếu → cộng tồn kho thực tế + ghi StockCard
        /// </summary>
        [HttpPost("{slipId}/approve")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> ApproveSlip(int slipId, [FromBody] ApproveInventorySlipDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            try
            {
                var result = await _slipService.ApproveSlipAsync(slipId, dto, userId.Value);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Admin từ chối phiếu → trả về cho Y tá sửa lại
        /// </summary>
        [HttpPost("{slipId}/reject")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> RejectSlip(int slipId, [FromBody] RejectInventorySlipDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            try
            {
                var result = await _slipService.RejectSlipAsync(slipId, dto, userId.Value);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Hủy phiếu ở trạng thái Pending hoặc Rejected
        /// </summary>
        [HttpPost("{slipId}/void")]
        [Authorize(Roles = RoleConstants.AdminOrNurse)]
        public async Task<IActionResult> VoidSlip(int slipId)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            try
            {
                var result = await _slipService.VoidSlipAsync(slipId, userId.Value);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách phiếu nhập kho (có lọc status, createdBy)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = RoleConstants.InventoryManagers)]
        public async Task<IActionResult> GetSlips([FromQuery] string? status, [FromQuery] int? createdBy)
        {
            var slips = await _slipService.GetSlipsAsync(status, createdBy);
            return Ok(slips);
        }

        /// <summary>
        /// Lấy chi tiết 1 phiếu nhập kho
        /// </summary>
        [HttpGet("{slipId}")]
        [Authorize(Roles = RoleConstants.InventoryManagers)]
        public async Task<IActionResult> GetSlipById(int slipId)
        {
            try
            {
                var result = await _slipService.GetSlipByIdAsync(slipId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách phiếu đang chờ duyệt (shortcut cho Admin)
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> GetPendingSlips()
        {
            var slips = await _slipService.GetSlipsAsync("Pending");
            return Ok(slips);
        }

        private int? GetCurrentUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdStr, out var userId) ? userId : null;
        }
    }
}
