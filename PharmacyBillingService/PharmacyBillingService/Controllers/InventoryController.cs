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
    [Route("api/inventory")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpPost("import")]
        [Authorize(Roles = RoleConstants.InventoryManagers)]
        public async Task<IActionResult> ImportStock([FromBody] StockImportDto importDto)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            try
            {
                var result = await _inventoryService.ImportStockAsync(importDto, userId.Value);
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

        [HttpPost("adjust")]
        [Authorize(Roles = RoleConstants.InventoryManagers)]
        public async Task<IActionResult> AdjustStock([FromBody] StockAdjustDto adjustDto)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            try
            {
                var result = await _inventoryService.AdjustStockAsync(adjustDto, userId.Value);
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

        [HttpGet("transactions")]
        [Authorize(Roles = RoleConstants.InventoryManagers)]
        public async Task<IActionResult> GetTransactions()
        {
            return Ok(await _inventoryService.GetTransactionsAsync());
        }

        [HttpGet("transactions/{medicineId}")]
        [Authorize(Roles = RoleConstants.InventoryManagers)]
        public async Task<IActionResult> GetTransactionsByMedicine(int medicineId)
        {
            return Ok(await _inventoryService.GetTransactionsByMedicineIdAsync(medicineId));
        }

        [HttpGet("batches")]
        [Authorize(Roles = RoleConstants.InventoryManagers)]
        public async Task<IActionResult> GetBatches()
        {
            return Ok(await _inventoryService.GetBatchesAsync());
        }

        [HttpGet("batches/{medicineId}")]
        [Authorize(Roles = RoleConstants.InventoryManagers)]
        public async Task<IActionResult> GetBatchesByMedicine(int medicineId)
        {
            return Ok(await _inventoryService.GetBatchesByMedicineIdAsync(medicineId));
        }

        private int? GetCurrentUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdStr, out var userId) ? userId : null;
        }
    }
}
