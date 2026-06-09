using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PharmacyBillingService.Data;
using PharmacyBillingService.DTOs;
using PharmacyBillingService.Models;

namespace PharmacyBillingService.Services
{
    public interface IMedicineService
    {
        Task<List<MedicineDto>> GetAllMedicinesAsync(string? searchName, string? searchActiveIngredient, string? medicineType, string? status, int page = 1, int pageSize = 20);
        Task<MedicineDto?> GetMedicineByIdAsync(int id);
        Task<MedicineDto> CreateMedicineAsync(CreateMedicineDto createDto);
        Task<MedicineDto?> UpdateMedicineAsync(int id, UpdateMedicineDto updateDto);
        Task<bool> DeleteMedicineAsync(int id);
        Task<List<MedicineDto>> GetLowStockMedicinesAsync();
        Task<List<MedicineDto>> GetExpiredMedicinesAsync();
        Task<List<MedicineDto>> GetExpiringSoonMedicinesAsync(int days);
    }

    public class MedicineService : IMedicineService
    {
        private readonly PharmacyDbContext _context;

        public MedicineService(PharmacyDbContext context)
        {
            _context = context;
        }

        public async Task<List<MedicineDto>> GetAllMedicinesAsync(string? searchName, string? searchActiveIngredient, string? medicineType, string? status, int page = 1, int pageSize = 20)
        {
            var query = _context.Medicines.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                var normalizedName = searchName.Trim().ToLower();
                query = query.Where(m => m.MedicineName.ToLower().StartsWith(normalizedName));
            }

            if (!string.IsNullOrWhiteSpace(searchActiveIngredient))
            {
                var normalizedActiveIngredient = searchActiveIngredient.Trim().ToLower();
                query = query.Where(m => m.ActiveIngredient != null && m.ActiveIngredient.ToLower().Contains(normalizedActiveIngredient));
            }

            if (!string.IsNullOrWhiteSpace(medicineType))
            {
                var normalizedMedicineType = medicineType.Trim().ToLower();
                query = query.Where(m => m.MedicineType.ToLower() == normalizedMedicineType);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = status.Trim().ToLower();
                query = query.Where(m => m.Status.ToLower() == normalizedStatus);
            }

            var medicines = await query
                .OrderBy(m => m.MedicineName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return medicines.Select(MapToMedicineDto).ToList();
        }

        public async Task<MedicineDto?> GetMedicineByIdAsync(int id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null) return null;
            return MapToMedicineDto(medicine);
        }

        public async Task<MedicineDto> CreateMedicineAsync(CreateMedicineDto createDto)
        {
            var medicineName = RequiredText(createDto.MedicineName, "Ten thuoc la bat buoc.");
            var activeIngredient = RequiredText(createDto.ActiveIngredient, "Hoat chat la bat buoc.");
            var medicineType = RequiredText(createDto.MedicineType, "Loai thuoc la bat buoc.");
            var unit = RequiredText(createDto.Unit, "Don vi tinh la bat buoc.");
            var status = NormalizeStatus(createDto.Status, createDto.StockQuantity);
            
            var medicine = new Medicine
            {
                MedicineName = medicineName,
                ActiveIngredient = activeIngredient,
                MedicineType = medicineType,
                Unit = unit,
                Price = createDto.Price,
                StockQuantity = createDto.StockQuantity,
                MinStockLevel = createDto.MinStockLevel,
                ExpiryDate = createDto.ExpiryDate,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            _context.Medicines.Add(medicine);
            await _context.SaveChangesAsync();

            if (medicine.StockQuantity > 0)
            {
                _context.MedicineBatches.Add(new MedicineBatch
                {
                    MedicineId = medicine.MedicineId,
                    BatchNumber = $"INIT-{medicine.MedicineId}",
                    ExpiryDate = medicine.ExpiryDate,
                    Quantity = medicine.StockQuantity,
                    InitialQuantity = medicine.StockQuantity,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return MapToMedicineDto(medicine);
        }

        public async Task<MedicineDto?> UpdateMedicineAsync(int id, UpdateMedicineDto updateDto)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null) return null;

            medicine.MedicineName = RequiredText(updateDto.MedicineName, "Ten thuoc la bat buoc.");
            medicine.ActiveIngredient = RequiredText(updateDto.ActiveIngredient, "Hoat chat la bat buoc.");
            medicine.MedicineType = RequiredText(updateDto.MedicineType, "Loai thuoc la bat buoc.");
            medicine.Unit = RequiredText(updateDto.Unit, "Don vi tinh la bat buoc.");
            medicine.Price = updateDto.Price;
            medicine.StockQuantity = updateDto.StockQuantity;
            medicine.MinStockLevel = updateDto.MinStockLevel;
            medicine.ExpiryDate = updateDto.ExpiryDate;
            medicine.Status = NormalizeStatus(updateDto.Status, updateDto.StockQuantity);
            medicine.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToMedicineDto(medicine);
        }

        public async Task<bool> DeleteMedicineAsync(int id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null) return false;

            medicine.Status = "Inactive";
            medicine.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<MedicineDto>> GetLowStockMedicinesAsync()
        {
            // BR15: Khi tồn kho dưới MinStockLevel, hệ thống cần cảnh báo
            var medicines = await _context.Medicines
                .Where(m => m.StockQuantity <= m.MinStockLevel && m.Status != "Inactive")
                .ToListAsync();

            return medicines.Select(MapToMedicineDto).ToList();
        }

        public async Task<List<MedicineDto>> GetExpiredMedicinesAsync()
        {
            var today = DateTime.UtcNow.Date;
            var medicines = await _context.Medicines
                .Where(m => m.ExpiryDate != null && m.ExpiryDate <= today && m.Status != "Inactive")
                .ToListAsync();

            return medicines.Select(MapToMedicineDto).ToList();
        }

        public async Task<List<MedicineDto>> GetExpiringSoonMedicinesAsync(int days)
        {
            var today = DateTime.UtcNow.Date;
            var until = today.AddDays(Math.Max(1, days));

            var medicineIds = await _context.MedicineBatches
                .Where(b => b.Status == "Active"
                    && b.Quantity > 0
                    && b.ExpiryDate != null
                    && b.ExpiryDate > today
                    && b.ExpiryDate <= until)
                .Select(b => b.MedicineId)
                .Distinct()
                .ToListAsync();

            var medicines = await _context.Medicines
                .Where(m => medicineIds.Contains(m.MedicineId) && m.Status != "Inactive")
                .ToListAsync();

            return medicines.Select(MapToMedicineDto).ToList();
        }

        private static MedicineDto MapToMedicineDto(Medicine medicine)
        {
            return new MedicineDto
            {
                MedicineId = medicine.MedicineId,
                MedicineName = medicine.MedicineName,
                ActiveIngredient = medicine.ActiveIngredient,
                MedicineType = medicine.MedicineType,
                Unit = medicine.Unit,
                Price = medicine.Price,
                StockQuantity = medicine.StockQuantity,
                MinStockLevel = medicine.MinStockLevel,
                ExpiryDate = medicine.ExpiryDate,
                Status = medicine.Status,
                CreatedAt = medicine.CreatedAt,
                UpdatedAt = medicine.UpdatedAt
            };
        }

        private static string RequiredText(string? value, string message)
        {
            var text = value?.Trim();
            if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException(message);
            return text;
        }

        private static string NormalizeStatus(string? status, int stockQuantity)
        {
            var value = string.IsNullOrWhiteSpace(status) ? "Active" : status.Trim();
            if (!string.Equals(value, "Active", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "Inactive", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(value, "OutOfStock", StringComparison.OrdinalIgnoreCase)) return stockQuantity == 0 ? "OutOfStock" : "Active";
                throw new InvalidOperationException("Trang thai khong hop le. Chi chap nhan Active, Inactive hoac OutOfStock.");
            }

            if (string.Equals(value, "Inactive", StringComparison.OrdinalIgnoreCase)) return "Inactive";
            return stockQuantity == 0 ? "OutOfStock" : "Active";
        }
    }
}
