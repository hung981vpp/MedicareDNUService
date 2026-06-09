using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyBillingService.DTOs;
using PharmacyBillingService.Security;
using PharmacyBillingService.Services;

namespace PharmacyBillingService.Controllers
{
    [ApiController]
    [Route("api/medicines")]
    public class MedicinesController : ControllerBase
    {
        private readonly IMedicineService _medicineService;

        public MedicinesController(IMedicineService medicineService)
        {
            _medicineService = medicineService;
        }

        [HttpGet]
        [Authorize(Roles = RoleConstants.DoctorOrStaff)]
        public async Task<IActionResult> GetAllMedicines(
            [FromQuery] string? name,
            [FromQuery] string? activeIngredient,
            [FromQuery] string? medicineType,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            return Ok(await _medicineService.GetAllMedicinesAsync(name, activeIngredient, medicineType, status, page, pageSize));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = RoleConstants.DoctorOrStaff)]
        public async Task<IActionResult> GetMedicineById(int id)
        {
            var medicine = await _medicineService.GetMedicineByIdAsync(id);
            return medicine == null ? NotFound(new { Message = "Khong tim thay thuoc yeu cau." }) : Ok(medicine);
        }

        [HttpPost]
        [Authorize(Roles = RoleConstants.InventoryManagers)]
        public async Task<IActionResult> CreateMedicine([FromBody] CreateMedicineDto createDto)
        {
            try
            {
                var result = await _medicineService.CreateMedicineAsync(createDto);
                return CreatedAtAction(nameof(GetMedicineById), new { id = result.MedicineId }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = RoleConstants.InventoryManagers)]
        public async Task<IActionResult> UpdateMedicine(int id, [FromBody] UpdateMedicineDto updateDto)
        {
            try
            {
                var result = await _medicineService.UpdateMedicineAsync(id, updateDto);
                return result == null ? NotFound(new { Message = "Khong tim thay thuoc yeu cau." }) : Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = RoleConstants.InventoryManagers)]
        public async Task<IActionResult> DeleteMedicine(int id)
        {
            try
            {
                var success = await _medicineService.DeleteMedicineAsync(id);
                return success ? Ok(new { Message = "Da tam ngung thuoc thanh cong." }) : NotFound(new { Message = "Khong tim thay thuoc yeu cau." });
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { Message = "Khong the xoa thuoc do co rang buoc du lieu. Vui long tam ngung thuoc.", Detail = ex.Message });
            }
        }

        [HttpGet("low-stock")]
        [Authorize(Roles = RoleConstants.InventoryManagers)]
        public async Task<IActionResult> GetLowStock()
        {
            return Ok(await _medicineService.GetLowStockMedicinesAsync());
        }

        [HttpGet("expired")]
        [Authorize(Roles = RoleConstants.InventoryManagers)]
        public async Task<IActionResult> GetExpired()
        {
            return Ok(await _medicineService.GetExpiredMedicinesAsync());
        }

        [HttpGet("expiring-soon")]
        [Authorize(Roles = RoleConstants.InventoryManagers)]
        public async Task<IActionResult> GetExpiringSoon([FromQuery] int days = 30)
        {
            return Ok(await _medicineService.GetExpiringSoonMedicinesAsync(days));
        }
    }
}
