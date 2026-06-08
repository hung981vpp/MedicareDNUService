using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PharmacyBillingService.Data;
using PharmacyBillingService.DTOs;
using PharmacyBillingService.Events;
using PharmacyBillingService.Models;

namespace PharmacyBillingService.Services
{
    public interface IPrescriptionService
    {
        Task<PrescriptionDto> ProcessPrescriptionCreatedEventAsync(PrescriptionCreatedEvent ev);
        Task<List<PrescriptionDto>> GetAllPrescriptionsAsync(string? status);
        Task<PrescriptionDto?> GetPrescriptionByIdAsync(int id);
        Task<List<PrescriptionDto>> GetPrescriptionsByPatientIdAsync(int patientId);
        Task<PrescriptionStockCheckDto> CheckPrescriptionStockAsync(int prescriptionId);
        Task<PrescriptionDto> ApprovePrescriptionAsync(int prescriptionId);
        Task<bool> DispensePrescriptionAsync(int prescriptionId, int userId);
    }

    public class PrescriptionService : IPrescriptionService
    {
        private readonly PharmacyDbContext _context;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<PrescriptionService> _logger;

        public PrescriptionService(PharmacyDbContext context, IEventPublisher eventPublisher, ILogger<PrescriptionService> logger)
        {
            _context = context;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task<PrescriptionDto> ProcessPrescriptionCreatedEventAsync(PrescriptionCreatedEvent ev)
        {
            var eventKey = BuildEventKey(ev);
            var processedEvent = await _context.ProcessedEvents.FirstOrDefaultAsync(e => e.EventKey == eventKey);
            if (processedEvent?.Status == "Success")
            {
                var existingSuccess = await _context.Prescriptions
                    .Include(p => p.PrescriptionItems)
                    .FirstOrDefaultAsync(p => p.PrescriptionId == ev.PrescriptionId);

                if (existingSuccess != null)
                {
                    await EnsureInvoiceCreatedAsync(existingSuccess);
                    return await MapToPrescriptionDtoAsync(existingSuccess);
                }
            }

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            if (processedEvent == null)
            {
                processedEvent = new ProcessedEvent
                {
                    EventKey = eventKey,
                    EventType = string.IsNullOrWhiteSpace(ev.EventType) ? ev.EventName : ev.EventType,
                    Source = ev.Source,
                    PayloadHash = ComputePayloadHash(ev),
                    Status = "Processing",
                    ReceivedAt = DateTime.UtcNow
                };
                _context.ProcessedEvents.Add(processedEvent);
                await _context.SaveChangesAsync();
            }

            var existing = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .FirstOrDefaultAsync(p => p.PrescriptionId == ev.PrescriptionId);

            if (existing != null)
            {
                _logger.LogWarning("Duplicate prescription.created ignored for prescription {Id}.", ev.PrescriptionId);
                await EnsureInvoiceCreatedAsync(existing);
                processedEvent.Status = "Success";
                processedEvent.ProcessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                return await MapToPrescriptionDtoAsync(existing);
            }

            var prescription = new Prescription
            {
                PrescriptionId = ev.PrescriptionId,
                PatientId = ev.PatientId,
                DoctorId = ev.DoctorId,
                AppointmentId = ev.AppointmentId,
                MedicalRecordId = ev.MedicalRecordId,
                CreatedAt = ev.CreatedAt != default ? ev.CreatedAt : DateTime.UtcNow,
                Status = "Pending"
            };

            var itemsToCreate = new List<PrescriptionItem>();
            var allAvailable = true;
            var anyAvailable = false;

            foreach (var item in ev.Items)
            {
                var medicine = await _context.Medicines.FindAsync(item.MedicineId);
                if (medicine == null)
                {
                    throw new ArgumentException($"Khong tim thay thuoc co ID = {item.MedicineId} trong he thong.");
                }

                var priceSnapshot = medicine.Price;
                itemsToCreate.Add(new PrescriptionItem
                {
                    PrescriptionId = ev.PrescriptionId,
                    MedicineId = item.MedicineId,
                    MedicineName = medicine.MedicineName,
                    Quantity = item.Quantity,
                    Dosage = item.Dosage,
                    UnitPrice = priceSnapshot,
                    TotalPrice = item.Quantity * priceSnapshot
                });

                var availableQuantity = await GetAvailableBatchQuantityAsync(item.MedicineId);
                if (availableQuantity >= item.Quantity)
                {
                    anyAvailable = true;
                }
                else
                {
                    allAvailable = false;
                }
            }

            prescription.Status = allAvailable ? "Pending" : anyAvailable ? "PartiallyAvailable" : "OutOfStock";

            _context.Prescriptions.Add(prescription);
            _context.PrescriptionItems.AddRange(itemsToCreate);
            await _context.SaveChangesAsync();

            var savedPrescription = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .FirstAsync(p => p.PrescriptionId == ev.PrescriptionId);

            await EnsureInvoiceCreatedAsync(savedPrescription);
            processedEvent.Status = "Success";
            processedEvent.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return await MapToPrescriptionDtoAsync(savedPrescription);
        }

        private async Task EnsureInvoiceCreatedAsync(Prescription prescription)
        {
            var existingInvoice = await _context.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.PrescriptionId == prescription.PrescriptionId && i.Status != "Cancelled");

            if (existingInvoice != null) return;

            var medicineTotal = prescription.PrescriptionItems.Sum(i => i.TotalPrice);
            const decimal defaultExaminationFee = 150000m;

            _context.Invoices.Add(new Invoice
            {
                PatientId = prescription.PatientId,
                AppointmentId = prescription.AppointmentId,
                PrescriptionId = prescription.PrescriptionId,
                ExaminationFee = defaultExaminationFee,
                MedicineTotal = medicineTotal,
                TotalAmount = defaultExaminationFee + medicineTotal,
                Status = "Unpaid",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task<List<PrescriptionDto>> GetAllPrescriptionsAsync(string? status)
        {
            var query = _context.Prescriptions.Include(p => p.PrescriptionItems).AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.Status == status);
            }

            var prescriptions = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            var result = new List<PrescriptionDto>();
            foreach (var prescription in prescriptions)
            {
                result.Add(await MapToPrescriptionDtoAsync(prescription));
            }

            return result;
        }

        public async Task<PrescriptionDto?> GetPrescriptionByIdAsync(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);

            return prescription == null ? null : await MapToPrescriptionDtoAsync(prescription);
        }

        public async Task<List<PrescriptionDto>> GetPrescriptionsByPatientIdAsync(int patientId)
        {
            var prescriptions = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .Where(p => p.PatientId == patientId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var result = new List<PrescriptionDto>();
            foreach (var prescription in prescriptions)
            {
                result.Add(await MapToPrescriptionDtoAsync(prescription));
            }

            return result;
        }

        public async Task<PrescriptionStockCheckDto> CheckPrescriptionStockAsync(int prescriptionId)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId);

            if (prescription == null)
            {
                throw new ArgumentException("Khong tim thay don thuoc yeu cau.");
            }

            return await BuildStockCheckAsync(prescription);
        }

        public async Task<PrescriptionDto> ApprovePrescriptionAsync(int prescriptionId)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId);

            if (prescription == null)
            {
                throw new ArgumentException("Khong tim thay don thuoc yeu cau.");
            }

            if (prescription.Status == "Dispensed")
            {
                return await MapToPrescriptionDtoAsync(prescription);
            }

            var stockCheck = await BuildStockCheckAsync(prescription);
            if (!stockCheck.AllAvailable)
            {
                prescription.Status = stockCheck.AnyAvailable ? "PartiallyAvailable" : "OutOfStock";
                await _context.SaveChangesAsync();
                throw new InvalidOperationException("Chua du thuoc de duyet don.");
            }

            if (stockCheck.InvoiceStatus != "Paid")
            {
                prescription.Status = "Pending";
                await _context.SaveChangesAsync();
                throw new InvalidOperationException("Hoa don lien quan chua duoc thanh toan du.");
            }

            prescription.Status = "ReadyToDispense";
            await _context.SaveChangesAsync();
            return await MapToPrescriptionDtoAsync(prescription);
        }

        public async Task<bool> DispensePrescriptionAsync(int prescriptionId, int userId)
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId);

            if (prescription == null)
            {
                throw new ArgumentException("Khong tim thay don thuoc yeu cau.");
            }

            if (prescription.Status == "Dispensed")
            {
                throw new InvalidOperationException("Don thuoc da duoc xuat tu truoc.");
            }

            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.PrescriptionId == prescriptionId && i.Status != "Cancelled");
            if (invoice == null)
            {
                throw new InvalidOperationException("Chua co hoa don cho don thuoc nay.");
            }

            if (invoice.Status != "Paid")
            {
                throw new InvalidOperationException("Hoa don lien quan chua duoc thanh toan du.");
            }

            var stockCheck = await BuildStockCheckAsync(prescription);
            if (!stockCheck.AllAvailable)
            {
                prescription.Status = stockCheck.AnyAvailable ? "PartiallyAvailable" : "OutOfStock";
                await _context.SaveChangesAsync();
                throw new InvalidOperationException("Khong du ton kho de phat thuoc.");
            }

            var today = DateTime.UtcNow.Date;
            foreach (var item in prescription.PrescriptionItems)
            {
                var medicine = await _context.Medicines.FindAsync(item.MedicineId);
                if (medicine == null)
                {
                    throw new InvalidOperationException($"Thuoc '{item.MedicineName}' khong ton tai.");
                }

                var beforeTotalQty = medicine.StockQuantity;
                var remaining = item.Quantity;
                var batches = await _context.MedicineBatches
                    .Where(b => b.MedicineId == item.MedicineId
                        && b.Status == "Active"
                        && b.Quantity > 0
                        && (b.ExpiryDate == null || b.ExpiryDate > today))
                    .OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue)
                    .ThenBy(b => b.BatchId)
                    .ToListAsync();

                foreach (var batch in batches)
                {
                    if (remaining <= 0) break;

                    var exportQty = Math.Min(batch.Quantity, remaining);
                    var beforeBatchQty = batch.Quantity;
                    batch.Quantity -= exportQty;
                    batch.UpdatedAt = DateTime.UtcNow;
                    if (batch.Quantity == 0) batch.Status = "OutOfStock";
                    remaining -= exportQty;

                    _context.StockTransactions.Add(new StockTransaction
                    {
                        MedicineId = medicine.MedicineId,
                        BatchId = batch.BatchId,
                        Type = "Export",
                        Quantity = exportQty,
                        BeforeQuantity = beforeBatchQty,
                        AfterQuantity = batch.Quantity,
                        Reason = $"Xuat thuoc theo don #{prescriptionId}",
                        CreatedBy = userId,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                if (remaining > 0)
                {
                    throw new InvalidOperationException($"Khong du ton kho cho thuoc '{item.MedicineName}'.");
                }

                await _context.SaveChangesAsync();
                medicine.StockQuantity = await _context.MedicineBatches
                    .Where(b => b.MedicineId == medicine.MedicineId && b.Status != "Inactive")
                    .SumAsync(b => b.Quantity);
                medicine.Status = medicine.StockQuantity == 0 ? "OutOfStock" : "Active";
                medicine.UpdatedAt = DateTime.UtcNow;

                await _eventPublisher.PublishAsync("medicine.stock.updated", new MedicineStockUpdatedEvent
                {
                    MedicineId = medicine.MedicineId,
                    BeforeQuantity = beforeTotalQty,
                    AfterQuantity = medicine.StockQuantity,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            prescription.Status = "Dispensed";
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            await _eventPublisher.PublishAsync("medicine.dispensed", new MedicineDispensedEvent
            {
                PrescriptionId = prescriptionId,
                PatientId = prescription.PatientId,
                DispensedAt = DateTime.UtcNow
            });

            return true;
        }

        private static PrescriptionDto MapToPrescriptionDto(Prescription p)
        {
            return new PrescriptionDto
            {
                PrescriptionId = p.PrescriptionId,
                PatientId = p.PatientId,
                DoctorId = p.DoctorId,
                AppointmentId = p.AppointmentId,
                MedicalRecordId = p.MedicalRecordId,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                PrescriptionItems = p.PrescriptionItems.Select(pi => new PrescriptionItemDto
                {
                    PrescriptionItemId = pi.PrescriptionItemId,
                    PrescriptionId = pi.PrescriptionId,
                    MedicineId = pi.MedicineId,
                    MedicineName = pi.MedicineName,
                    Quantity = pi.Quantity,
                    Dosage = pi.Dosage,
                    UnitPrice = pi.UnitPrice,
                    TotalPrice = pi.TotalPrice
                }).ToList()
            };
        }

        private async Task<PrescriptionDto> MapToPrescriptionDtoAsync(Prescription prescription)
        {
            var dto = MapToPrescriptionDto(prescription);
            var stockCheck = await BuildStockCheckAsync(prescription);

            dto.StockStatus = stockCheck.AllAvailable ? "Available" : stockCheck.AnyAvailable ? "PartiallyAvailable" : "OutOfStock";
            dto.InvoiceStatus = stockCheck.InvoiceStatus;
            dto.CanApprove = stockCheck.CanApprove;
            dto.CanDispense = stockCheck.CanDispense;

            foreach (var item in dto.PrescriptionItems)
            {
                var stockItem = stockCheck.Items.FirstOrDefault(i => i.PrescriptionItemId == item.PrescriptionItemId);
                if (stockItem == null) continue;

                item.CurrentStock = stockItem.CurrentStock;
                item.IsAvailable = stockItem.IsAvailable;
                item.ShortageQuantity = stockItem.ShortageQuantity;
                item.StockStatus = stockItem.StockStatus;
            }

            return dto;
        }

        private async Task<PrescriptionStockCheckDto> BuildStockCheckAsync(Prescription prescription)
        {
            var invoice = await _context.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.PrescriptionId == prescription.PrescriptionId && i.Status != "Cancelled");

            var result = new PrescriptionStockCheckDto
            {
                PrescriptionId = prescription.PrescriptionId,
                PatientId = prescription.PatientId,
                PrescriptionStatus = prescription.Status,
                InvoiceStatus = invoice?.Status
            };

            foreach (var item in prescription.PrescriptionItems)
            {
                var medicine = await _context.Medicines.AsNoTracking().FirstOrDefaultAsync(m => m.MedicineId == item.MedicineId);
                var currentStock = medicine == null ? 0 : await GetAvailableBatchQuantityAsync(item.MedicineId);
                var shortage = Math.Max(0, item.Quantity - currentStock);
                var isAvailable = medicine != null && medicine.Status != "Inactive" && currentStock >= item.Quantity;

                result.Items.Add(new PrescriptionStockItemDto
                {
                    PrescriptionItemId = item.PrescriptionItemId,
                    MedicineId = item.MedicineId,
                    MedicineName = item.MedicineName,
                    RequiredQuantity = item.Quantity,
                    CurrentStock = currentStock,
                    ShortageQuantity = shortage,
                    IsAvailable = isAvailable,
                    StockStatus = isAvailable ? "Available" : "Shortage"
                });
            }

            result.AllAvailable = result.Items.Count > 0 && result.Items.All(i => i.IsAvailable);
            result.AnyAvailable = result.Items.Any(i => i.IsAvailable);
            result.CanApprove = prescription.Status != "Dispensed" && result.AllAvailable && invoice?.Status == "Paid";
            result.CanDispense = prescription.Status != "Dispensed" && result.AllAvailable && invoice?.Status == "Paid";

            return result;
        }

        private async Task<int> GetAvailableBatchQuantityAsync(int medicineId)
        {
            var today = DateTime.UtcNow.Date;
            return await _context.MedicineBatches
                .AsNoTracking()
                .Where(b => b.MedicineId == medicineId
                    && b.Status == "Active"
                    && b.Quantity > 0
                    && (b.ExpiryDate == null || b.ExpiryDate > today))
                .SumAsync(b => b.Quantity);
        }

        private static string BuildEventKey(PrescriptionCreatedEvent ev)
        {
            if (!string.IsNullOrWhiteSpace(ev.EventCode)) return ev.EventCode.Trim();
            var source = string.IsNullOrWhiteSpace(ev.Source) ? "unknown" : ev.Source.Trim();
            var eventType = string.IsNullOrWhiteSpace(ev.EventType) ? ev.EventName : ev.EventType;
            return $"{source}:{eventType}:{ev.PrescriptionId}";
        }

        private static string ComputePayloadHash(PrescriptionCreatedEvent ev)
        {
            var json = JsonSerializer.Serialize(ev);
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
            return Convert.ToHexString(bytes);
        }
    }
}
