using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        [Authorize(Roles = RoleConstants.AdminOrPharmacist)]
        public async Task<IActionResult> CreateMedicine([FromBody] CreateMedicineDto createDto)
        {
            var result = await _medicineService.CreateMedicineAsync(createDto);
            return CreatedAtAction(nameof(GetMedicineById), new { id = result.MedicineId }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = RoleConstants.AdminOrPharmacist)]
        public async Task<IActionResult> UpdateMedicine(int id, [FromBody] UpdateMedicineDto updateDto)
        {
            var result = await _medicineService.UpdateMedicineAsync(id, updateDto);
            return result == null ? NotFound(new { Message = "Khong tim thay thuoc yeu cau." }) : Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> DeleteMedicine(int id)
        {
            var success = await _medicineService.DeleteMedicineAsync(id);
            return success ? Ok(new { Message = "Xoa/Ngung ban thuoc thanh cong." }) : NotFound(new { Message = "Khong tim thay thuoc yeu cau." });
        }

        [HttpGet("low-stock")]
        [Authorize(Roles = RoleConstants.AdminOrPharmacist)]
        public async Task<IActionResult> GetLowStock()
        {
            return Ok(await _medicineService.GetLowStockMedicinesAsync());
        }

        [HttpGet("expired")]
        [Authorize(Roles = RoleConstants.AdminOrPharmacist)]
        public async Task<IActionResult> GetExpired()
        {
            return Ok(await _medicineService.GetExpiredMedicinesAsync());
        }

        [HttpGet("expiring-soon")]
        [Authorize(Roles = RoleConstants.AdminOrPharmacist)]
        public async Task<IActionResult> GetExpiringSoon([FromQuery] int days = 30)
        {
            return Ok(await _medicineService.GetExpiringSoonMedicinesAsync(days));
        }
    }
}
