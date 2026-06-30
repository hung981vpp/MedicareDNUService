using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PharmacyBillingService.Data;
using PharmacyBillingService.DTOs;
using PharmacyBillingService.Models;

namespace PharmacyBillingService.Services
{
    public interface IInventorySlipService
    {
        /// <summary>Y tá tạo phiếu yêu cầu nhập kho (trạng thái Pending)</summary>
        Task<InventorySlipDto> CreateSlipAsync(CreateInventorySlipDto dto, int userId);

        /// <summary>Admin duyệt phiếu → cộng tồn kho + ghi StockCard</summary>
        Task<InventorySlipDto> ApproveSlipAsync(int slipId, ApproveInventorySlipDto dto, int adminUserId);

        /// <summary>Admin từ chối phiếu → trả về cho Y tá sửa</summary>
        Task<InventorySlipDto> RejectSlipAsync(int slipId, RejectInventorySlipDto dto, int adminUserId);

        /// <summary>Y tá/Admin hủy phiếu ở trạng thái Pending/Rejected</summary>
        Task<InventorySlipDto> VoidSlipAsync(int slipId, int userId);

        /// <summary>Lấy danh sách tất cả phiếu (có lọc trạng thái)</summary>
        Task<List<InventorySlipDto>> GetSlipsAsync(string? status = null, int? createdBy = null);

        /// <summary>Lấy chi tiết 1 phiếu theo ID</summary>
        Task<InventorySlipDto> GetSlipByIdAsync(int slipId);
    }

    public class InventorySlipService : IInventorySlipService
    {
        private readonly PharmacyDbContext _context;
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<InventorySlipService> _logger;

        public InventorySlipService(
            PharmacyDbContext context,
            IInventoryService inventoryService,
            ILogger<InventorySlipService> logger)
        {
            _context = context;
            _inventoryService = inventoryService;
            _logger = logger;
        }

        public async Task<InventorySlipDto> CreateSlipAsync(CreateInventorySlipDto dto, int userId)
        {
            // Validate all medicine IDs exist
            var medicineIds = dto.Items.Select(i => i.MedicineId).Distinct().ToList();
            var existingMedicines = await _context.Medicines
                .Where(m => medicineIds.Contains(m.MedicineId))
                .Select(m => m.MedicineId)
                .ToListAsync();

            var missingIds = medicineIds.Except(existingMedicines).ToList();
            if (missingIds.Any())
            {
                throw new ArgumentException($"Không tìm thấy thuốc với mã: {string.Join(", ", missingIds)}");
            }

            // Generate slip code
            var today = DateTime.UtcNow;
            var prefix = "NK";
            var dateStr = today.ToString("yyyyMMdd");
            var todayCount = await _context.InventorySlips
                .CountAsync(s => s.CreatedAt.Date == today.Date && s.SlipType == "Import");
            var slipCode = $"{prefix}-{dateStr}-{(todayCount + 1):D3}";

            var slip = new InventorySlip
            {
                SlipCode = slipCode,
                SlipType = "Import",
                Status = "Pending",
                SupplierName = dto.SupplierName?.Trim(),
                InvoiceImageUrl = dto.InvoiceImageUrl?.Trim(),
                Note = dto.Note?.Trim(),
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                Items = dto.Items.Select(item => new InventorySlipItem
                {
                    MedicineId = item.MedicineId,
                    BatchNumber = item.BatchNumber.Trim(),
                    ExpiryDate = item.ExpiryDate,
                    Quantity = item.Quantity,
                    ImportPrice = item.ImportPrice,
                    Note = item.Note?.Trim()
                }).ToList()
            };

            _context.InventorySlips.Add(slip);
            await _context.SaveChangesAsync();

            _logger.LogInformation("InventorySlip {SlipCode} created by User {UserId} with {ItemCount} items",
                slipCode, userId, dto.Items.Count);

            return await GetSlipByIdAsync(slip.SlipId);
        }

        public async Task<InventorySlipDto> ApproveSlipAsync(int slipId, ApproveInventorySlipDto dto, int adminUserId)
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            var slip = await _context.InventorySlips
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.SlipId == slipId);

            if (slip == null)
                throw new ArgumentException("Không tìm thấy phiếu nhập kho");

            if (slip.Status != "Pending")
                throw new InvalidOperationException($"Chỉ có thể duyệt phiếu ở trạng thái 'Chờ duyệt'. Trạng thái hiện tại: {slip.Status}");

            // Process each item: create/update batch and record stock transaction
            foreach (var item in slip.Items)
            {
                var importDto = new StockImportDto
                {
                    MedicineId = item.MedicineId,
                    Quantity = item.Quantity,
                    BatchNumber = item.BatchNumber,
                    ExpiryDate = item.ExpiryDate,
                    ImportPrice = item.ImportPrice,
                    Reason = $"Duyệt phiếu {slip.SlipCode} - {dto.Note ?? "Nhập kho"}"
                };

                await _inventoryService.ImportStockAsync(importDto, adminUserId);
            }

            // Update slip status
            slip.Status = "Approved";
            slip.ApprovedBy = adminUserId;
            slip.ApprovedAt = DateTime.UtcNow;
            slip.UpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(dto.Note))
            {
                slip.Note = (slip.Note ?? "") + $"\n[Admin] {dto.Note}";
            }

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            _logger.LogInformation("InventorySlip {SlipCode} APPROVED by Admin {UserId}", slip.SlipCode, adminUserId);

            return await GetSlipByIdAsync(slip.SlipId);
        }

        public async Task<InventorySlipDto> RejectSlipAsync(int slipId, RejectInventorySlipDto dto, int adminUserId)
        {
            var slip = await _context.InventorySlips.FindAsync(slipId);

            if (slip == null)
                throw new ArgumentException("Không tìm thấy phiếu nhập kho");

            if (slip.Status != "Pending")
                throw new InvalidOperationException($"Chỉ có thể từ chối phiếu ở trạng thái 'Chờ duyệt'. Trạng thái hiện tại: {slip.Status}");

            slip.Status = "Rejected";
            slip.RejectReason = dto.RejectReason.Trim();
            slip.ApprovedBy = adminUserId;
            slip.ApprovedAt = DateTime.UtcNow;
            slip.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogWarning("InventorySlip {SlipCode} REJECTED by Admin {UserId}. Reason: {Reason}",
                slip.SlipCode, adminUserId, dto.RejectReason);

            return await GetSlipByIdAsync(slip.SlipId);
        }

        public async Task<InventorySlipDto> VoidSlipAsync(int slipId, int userId)
        {
            var slip = await _context.InventorySlips.FindAsync(slipId);

            if (slip == null)
                throw new ArgumentException("Không tìm thấy phiếu nhập kho");

            if (slip.Status != "Pending" && slip.Status != "Rejected")
                throw new InvalidOperationException($"Chỉ có thể hủy phiếu ở trạng thái 'Chờ duyệt' hoặc 'Từ chối'. Trạng thái hiện tại: {slip.Status}");

            slip.Status = "Voided";
            slip.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("InventorySlip {SlipCode} VOIDED by User {UserId}", slip.SlipCode, userId);

            return await GetSlipByIdAsync(slip.SlipId);
        }

        public async Task<List<InventorySlipDto>> GetSlipsAsync(string? status = null, int? createdBy = null)
        {
            var query = _context.InventorySlips
                .Include(s => s.Items)
                    .ThenInclude(i => i.Medicine)
                .Include(s => s.Creator)
                .Include(s => s.Approver)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(s => s.Status == status);

            if (createdBy.HasValue)
                query = query.Where(s => s.CreatedBy == createdBy.Value);

            var slips = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return slips.Select(MapToDto).ToList();
        }

        public async Task<InventorySlipDto> GetSlipByIdAsync(int slipId)
        {
            var slip = await _context.InventorySlips
                .Include(s => s.Items)
                    .ThenInclude(i => i.Medicine)
                .Include(s => s.Creator)
                .Include(s => s.Approver)
                .FirstOrDefaultAsync(s => s.SlipId == slipId);

            if (slip == null)
                throw new ArgumentException("Không tìm thấy phiếu nhập kho");

            return MapToDto(slip);
        }

        private static InventorySlipDto MapToDto(InventorySlip slip)
        {
            return new InventorySlipDto
            {
                SlipId = slip.SlipId,
                SlipCode = slip.SlipCode,
                SlipType = slip.SlipType,
                Status = slip.Status,
                SupplierName = slip.SupplierName,
                InvoiceImageUrl = slip.InvoiceImageUrl,
                Note = slip.Note,
                RejectReason = slip.RejectReason,
                CreatedBy = slip.CreatedBy,
                CreatedByName = slip.Creator?.FullName ?? "N/A",
                ApprovedBy = slip.ApprovedBy,
                ApprovedByName = slip.Approver?.FullName,
                CreatedAt = slip.CreatedAt,
                ApprovedAt = slip.ApprovedAt,
                UpdatedAt = slip.UpdatedAt,
                TotalItems = slip.Items.Count,
                TotalQuantity = slip.Items.Sum(i => i.Quantity),
                Items = slip.Items.Select(i => new InventorySlipItemDto
                {
                    SlipItemId = i.SlipItemId,
                    MedicineId = i.MedicineId,
                    MedicineName = i.Medicine?.MedicineName ?? "N/A",
                    MedicineUnit = i.Medicine?.Unit,
                    BatchNumber = i.BatchNumber,
                    ExpiryDate = i.ExpiryDate,
                    Quantity = i.Quantity,
                    ImportPrice = i.ImportPrice,
                    Note = i.Note
                }).ToList()
            };
        }
    }
}
