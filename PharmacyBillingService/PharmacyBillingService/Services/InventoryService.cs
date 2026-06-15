using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PharmacyBillingService.Data;
using PharmacyBillingService.DTOs;
using PharmacyBillingService.Events;
using PharmacyBillingService.Models;

namespace PharmacyBillingService.Services
{
    public interface IInventoryService
    {
        Task<StockTransactionDto> ImportStockAsync(StockImportDto importDto, int userId);
        Task<StockTransactionDto> AdjustStockAsync(StockAdjustDto adjustDto, int userId);
        Task<List<StockTransactionDto>> GetTransactionsAsync();
        Task<List<StockTransactionDto>> GetTransactionsByMedicineIdAsync(int medicineId);
        Task<List<MedicineBatchDto>> GetBatchesAsync();
        Task<List<MedicineBatchDto>> GetBatchesByMedicineIdAsync(int medicineId);
        Task SyncMedicineStockAsync(int medicineId);
    }

    public class InventoryService : IInventoryService
    {
        private readonly PharmacyDbContext _context;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(PharmacyDbContext context, IEventPublisher eventPublisher, ILogger<InventoryService> logger)
        {
            _context = context;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task<StockTransactionDto> ImportStockAsync(StockImportDto importDto, int userId)
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            var medicine = await _context.Medicines.FindAsync(importDto.MedicineId);
            if (medicine == null)
            {
                throw new ArgumentException("Khong tim thay thuoc yeu cau");
            }

            var batchNumber = string.IsNullOrWhiteSpace(importDto.BatchNumber)
                ? $"AUTO-{DateTime.UtcNow:yyyyMMddHHmmss}-{medicine.MedicineId}"
                : importDto.BatchNumber.Trim();

            var batch = await _context.MedicineBatches
                .FirstOrDefaultAsync(b => b.MedicineId == medicine.MedicineId && b.BatchNumber == batchNumber);

            if (batch == null)
            {
                batch = new MedicineBatch
                {
                    MedicineId = medicine.MedicineId,
                    BatchNumber = batchNumber,
                    ExpiryDate = importDto.ExpiryDate ?? medicine.ExpiryDate,
                    Quantity = 0,
                    InitialQuantity = 0,
                    ImportPrice = importDto.ImportPrice,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };
                _context.MedicineBatches.Add(batch);
            }

            var beforeBatchQty = batch.Quantity;
            batch.Quantity += importDto.Quantity;
            batch.InitialQuantity += importDto.Quantity;
            batch.UpdatedAt = DateTime.UtcNow;
            if (batch.Quantity > 0 && batch.Status == "OutOfStock") batch.Status = "Active";

            var beforeTotalQty = medicine.StockQuantity;
            await _context.SaveChangesAsync();
            await SyncMedicineStockAsync(medicine.MedicineId);
            await _context.Entry(medicine).ReloadAsync();

            var transaction = new StockTransaction
            {
                MedicineId = medicine.MedicineId,
                BatchId = batch.BatchId,
                Type = "Import",
                Quantity = importDto.Quantity,
                BeforeQuantity = beforeBatchQty,
                AfterQuantity = batch.Quantity,
                Reason = importDto.Reason ?? "Nhap them thuoc vao kho",
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.StockTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            await PublishStockUpdatedAsync(medicine.MedicineId, beforeTotalQty, medicine.StockQuantity);
            WarnLowStock(medicine);

            return MapToTransactionDto(transaction, medicine.MedicineName, batch.BatchNumber);
        }

        public async Task<StockTransactionDto> AdjustStockAsync(StockAdjustDto adjustDto, int userId)
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            var medicine = await _context.Medicines.FindAsync(adjustDto.MedicineId);
            if (medicine == null)
            {
                throw new ArgumentException("Khong tim thay thuoc yeu cau");
            }

            MedicineBatch? batch = null;
            if (adjustDto.BatchId is not null)
            {
                batch = await _context.MedicineBatches
                    .FirstOrDefaultAsync(b => b.BatchId == adjustDto.BatchId && b.MedicineId == medicine.MedicineId);

                if (batch == null)
                {
                    throw new ArgumentException("Khong tim thay lo thuoc yeu cau");
                }
            }

            var beforeTotalQty = medicine.StockQuantity;
            int beforeQty;
            int afterQty = adjustDto.NewQuantity;

            if (batch != null)
            {
                beforeQty = batch.Quantity;
                batch.Quantity = afterQty;
                batch.Status = afterQty == 0 ? "OutOfStock" : "Active";
                batch.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                beforeQty = medicine.StockQuantity;
                medicine.StockQuantity = afterQty;
                medicine.Status = afterQty == 0 ? "OutOfStock" : "Active";
                medicine.UpdatedAt = DateTime.UtcNow;
                batch = await SyncDefaultBatchForTotalAsync(medicine, afterQty);
            }

            if (beforeQty == afterQty)
            {
                throw new InvalidOperationException("So luong ton kho moi bang ton kho cu. Khong co gi thay doi.");
            }

            if (afterQty < 0)
            {
                throw new InvalidOperationException("Khong cho phep ton kho am.");
            }

            var transaction = new StockTransaction
            {
                MedicineId = medicine.MedicineId,
                BatchId = batch?.BatchId,
                Batch = batch,
                Type = "Adjust",
                Quantity = Math.Abs(afterQty - beforeQty),
                BeforeQuantity = beforeQty,
                AfterQuantity = afterQty,
                Reason = adjustDto.Reason ?? $"Dieu chinh kho boi nguoi dung {userId}",
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.StockTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            if (batch != null)
            {
                await SyncMedicineStockAsync(medicine.MedicineId);
                await _context.Entry(medicine).ReloadAsync();
            }
            else
            {
                await _context.SaveChangesAsync();
            }

            await dbTransaction.CommitAsync();

            await PublishStockUpdatedAsync(medicine.MedicineId, beforeTotalQty, medicine.StockQuantity);
            WarnLowStock(medicine);

            return MapToTransactionDto(transaction, medicine.MedicineName, batch?.BatchNumber);
        }

        public async Task<List<StockTransactionDto>> GetTransactionsAsync()
        {
            var transactions = await _context.StockTransactions
                .Include(t => t.Medicine)
                .Include(t => t.Batch)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return transactions.Select(t => MapToTransactionDto(t, t.Medicine?.MedicineName ?? "N/A", t.Batch?.BatchNumber)).ToList();
        }

        public async Task<List<StockTransactionDto>> GetTransactionsByMedicineIdAsync(int medicineId)
        {
            var transactions = await _context.StockTransactions
                .Include(t => t.Medicine)
                .Include(t => t.Batch)
                .Where(t => t.MedicineId == medicineId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return transactions.Select(t => MapToTransactionDto(t, t.Medicine?.MedicineName ?? "N/A", t.Batch?.BatchNumber)).ToList();
        }

        public async Task<List<MedicineBatchDto>> GetBatchesAsync()
        {
            var batches = await _context.MedicineBatches
                .Include(b => b.Medicine)
                .OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue)
                .ThenBy(b => b.BatchNumber)
                .ToListAsync();

            return batches.Select(MapToBatchDto).ToList();
        }

        public async Task<List<MedicineBatchDto>> GetBatchesByMedicineIdAsync(int medicineId)
        {
            var batches = await _context.MedicineBatches
                .Include(b => b.Medicine)
                .Where(b => b.MedicineId == medicineId)
                .OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue)
                .ThenBy(b => b.BatchNumber)
                .ToListAsync();

            return batches.Select(MapToBatchDto).ToList();
        }

        public async Task SyncMedicineStockAsync(int medicineId)
        {
            var medicine = await _context.Medicines.FindAsync(medicineId);
            if (medicine == null) return;

            var quantity = await _context.MedicineBatches
                .Where(b => b.MedicineId == medicineId && b.Status != "Inactive")
                .SumAsync(b => b.Quantity);

            medicine.StockQuantity = quantity;
            medicine.Status = quantity == 0 ? "OutOfStock" : "Active";
            medicine.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        private async Task PublishStockUpdatedAsync(int medicineId, int beforeQty, int afterQty)
        {
            await _eventPublisher.PublishAsync("medicine.stock.updated", new MedicineStockUpdatedEvent
            {
                MedicineId = medicineId,
                BeforeQuantity = beforeQty,
                AfterQuantity = afterQty,
                UpdatedAt = DateTime.UtcNow
            });
        }

        private async Task<MedicineBatch?> SyncDefaultBatchForTotalAsync(Medicine medicine, int totalQuantity)
        {
            var defaultBatchNumber = $"INIT-{medicine.MedicineId}";
            var defaultBatch = await _context.MedicineBatches
                .FirstOrDefaultAsync(b => b.MedicineId == medicine.MedicineId && b.BatchNumber == defaultBatchNumber);

            if (defaultBatch == null)
            {
                var hasAnyBatch = await _context.MedicineBatches.AnyAsync(b => b.MedicineId == medicine.MedicineId);
                if (hasAnyBatch) return null;

                defaultBatch = new MedicineBatch
                {
                    MedicineId = medicine.MedicineId,
                    BatchNumber = defaultBatchNumber,
                    ExpiryDate = medicine.ExpiryDate,
                    Quantity = totalQuantity,
                    InitialQuantity = totalQuantity,
                    Status = totalQuantity == 0 ? "OutOfStock" : "Active",
                    CreatedAt = DateTime.UtcNow
                };
                _context.MedicineBatches.Add(defaultBatch);
                return defaultBatch;
            }

            defaultBatch.ExpiryDate = medicine.ExpiryDate;
            defaultBatch.Quantity = totalQuantity;
            defaultBatch.InitialQuantity = Math.Max(defaultBatch.InitialQuantity, totalQuantity);
            defaultBatch.Status = totalQuantity == 0 ? "OutOfStock" : "Active";
            defaultBatch.UpdatedAt = DateTime.UtcNow;
            return defaultBatch;
        }

        private void WarnLowStock(Medicine medicine)
        {
            if (medicine.StockQuantity <= medicine.MinStockLevel)
            {
                _logger.LogWarning("LOW STOCK: Medicine {Name} has {Qty}, min {Min}", medicine.MedicineName, medicine.StockQuantity, medicine.MinStockLevel);
            }
        }

        private static StockTransactionDto MapToTransactionDto(StockTransaction t, string medicineName, string? batchNumber)
        {
            return new StockTransactionDto
            {
                TransactionId = t.TransactionId,
                MedicineId = t.MedicineId,
                BatchId = t.BatchId,
                MedicineName = medicineName,
                BatchNumber = batchNumber,
                Type = t.Type,
                Quantity = t.Quantity,
                BeforeQuantity = t.BeforeQuantity,
                AfterQuantity = t.AfterQuantity,
                Reason = t.Reason,
                CreatedBy = t.CreatedBy,
                CreatedAt = t.CreatedAt
            };
        }

        private static MedicineBatchDto MapToBatchDto(MedicineBatch batch)
        {
            return new MedicineBatchDto
            {
                BatchId = batch.BatchId,
                MedicineId = batch.MedicineId,
                MedicineName = batch.Medicine?.MedicineName ?? string.Empty,
                BatchNumber = batch.BatchNumber,
                ExpiryDate = batch.ExpiryDate,
                Quantity = batch.Quantity,
                InitialQuantity = batch.InitialQuantity,
                ImportPrice = batch.ImportPrice,
                Status = batch.Status,
                CreatedAt = batch.CreatedAt,
                UpdatedAt = batch.UpdatedAt
            };
        }
    }
}
