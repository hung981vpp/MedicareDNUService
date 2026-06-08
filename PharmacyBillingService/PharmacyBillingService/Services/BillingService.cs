using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PharmacyBillingService.Data;
using PharmacyBillingService.DTOs;
using PharmacyBillingService.Events;
using PharmacyBillingService.Models;

namespace PharmacyBillingService.Services
{
    public interface IBillingService
    {
        Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createDto);
        Task<List<InvoiceDto>> GetAllInvoicesAsync(string? status);
        Task<InvoiceDto?> GetInvoiceByIdAsync(int id);
        Task<List<InvoiceDto>> GetInvoicesByPatientIdAsync(int patientId);
        Task<InvoiceDto> PayInvoiceAsync(int invoiceId, PayInvoiceDto payDto, int userId);
        Task<InvoiceDto> RefundInvoiceAsync(int invoiceId, RefundInvoiceDto refundDto, int userId);
        Task<InvoiceDto?> PayInvoiceFromSePayWebhookAsync(SePayWebhookDto webhookDto);
        Task<List<PaymentWebhookLogDto>> GetPaymentWebhookLogsAsync(string? status);
        Task<bool> CancelInvoiceAsync(int invoiceId);
    }

    public class BillingService : IBillingService
    {
        private readonly PharmacyDbContext _context;
        private readonly IEventPublisher _eventPublisher;

        public BillingService(PharmacyDbContext context, IEventPublisher eventPublisher)
        {
            _context = context;
            _eventPublisher = eventPublisher;
        }

        public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createDto)
        {
            if (createDto.PrescriptionId is null && createDto.AppointmentId is null)
            {
                throw new ArgumentException("Hoa don can co ma lich kham hoac ma don thuoc.");
            }

            var existingInvoice = await _context.Invoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i =>
                    i.Status != "Cancelled"
                    && ((createDto.PrescriptionId != null && i.PrescriptionId == createDto.PrescriptionId)
                        || (createDto.PrescriptionId == null
                            && createDto.AppointmentId != null
                            && i.AppointmentId == createDto.AppointmentId
                            && i.PrescriptionId == null)));

            if (existingInvoice != null)
            {
                return MapToInvoiceDto(existingInvoice);
            }

            int patientId;
            int? appointmentId = createDto.AppointmentId;
            decimal medicineTotal = 0;

            if (createDto.PrescriptionId is not null)
            {
                var prescription = await _context.Prescriptions
                    .Include(p => p.PrescriptionItems)
                    .FirstOrDefaultAsync(p => p.PrescriptionId == createDto.PrescriptionId.Value);

                if (prescription == null)
                {
                    throw new ArgumentException($"Khong tim thay don thuoc co ID = {createDto.PrescriptionId}");
                }

                patientId = prescription.PatientId;
                appointmentId = prescription.AppointmentId ?? appointmentId;
                medicineTotal = prescription.PrescriptionItems.Sum(pi => pi.TotalPrice);
            }
            else
            {
                if (createDto.PatientId is null)
                {
                    throw new ArgumentException("Hoa don chi co lich kham can PatientId.");
                }

                patientId = createDto.PatientId.Value;
            }

            var invoice = new Invoice
            {
                PatientId = patientId,
                AppointmentId = appointmentId,
                PrescriptionId = createDto.PrescriptionId,
                ExaminationFee = createDto.ExaminationFee,
                MedicineTotal = medicineTotal,
                TotalAmount = createDto.ExaminationFee + medicineTotal,
                Status = "Unpaid",
                CreatedAt = DateTime.UtcNow
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            return MapToInvoiceDto(invoice);
        }

        public async Task<List<InvoiceDto>> GetAllInvoicesAsync(string? status)
        {
            var query = _context.Invoices.Include(i => i.Payments).AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(i => i.Status == status);
            }

            var invoices = await query.OrderByDescending(i => i.CreatedAt).ToListAsync();
            return invoices.Select(MapToInvoiceDto).ToList();
        }

        public async Task<InvoiceDto?> GetInvoiceByIdAsync(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            return invoice == null ? null : MapToInvoiceDto(invoice);
        }

        public async Task<List<InvoiceDto>> GetInvoicesByPatientIdAsync(int patientId)
        {
            var invoices = await _context.Invoices
                .Include(i => i.Payments)
                .Where(i => i.PatientId == patientId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return invoices.Select(MapToInvoiceDto).ToList();
        }

        public async Task<InvoiceDto> PayInvoiceAsync(int invoiceId, PayInvoiceDto payDto, int userId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null)
            {
                throw new ArgumentException("Khong tim thay hoa don yeu cau.");
            }

            if (invoice.Status == "Cancelled" || invoice.Status == "Refunded")
            {
                throw new InvalidOperationException("Khong duoc thanh toan hoa don da huy hoac da hoan tien.");
            }

            var balanceDue = GetBalanceDue(invoice);
            if (balanceDue <= 0)
            {
                await transaction.CommitAsync();
                return MapToInvoiceDto(invoice);
            }

            var amount = payDto.Amount.GetValueOrDefault(balanceDue);
            if (amount <= 0 || amount > balanceDue)
            {
                throw new InvalidOperationException($"So tien thanh toan phai lon hon 0 va khong vuot qua so con no {balanceDue:N0}.");
            }

            var paymentMethod = NormalizePaymentMethod(payDto.PaymentMethod, payDto.Method);
            var payment = new Payment
            {
                InvoiceId = invoiceId,
                Amount = amount,
                PaymentMethod = paymentMethod,
                PaymentStatus = "Success",
                PaidBy = userId,
                PaidAt = DateTime.UtcNow,
                Note = BuildPaymentNote(payDto)
            };

            _context.Payments.Add(payment);
            invoice.Payments.Add(payment);
            ApplyInvoicePaymentStatus(invoice);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            if (invoice.Status == "Paid")
            {
                await PublishInvoicePaidAsync(invoice, paymentMethod);
                await UpdatePrescriptionReadyStateAsync(invoice.PrescriptionId);
            }

            return MapToInvoiceDto(invoice);
        }

        public async Task<InvoiceDto> RefundInvoiceAsync(int invoiceId, RefundInvoiceDto refundDto, int userId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null)
            {
                throw new ArgumentException("Khong tim thay hoa don yeu cau.");
            }

            if (invoice.Status == "Cancelled")
            {
                throw new InvalidOperationException("Khong hoan tien hoa don da huy.");
            }

            var refundable = GetPaidAmount(invoice) - GetRefundedAmount(invoice);
            if (refundDto.Amount <= 0 || refundDto.Amount > refundable)
            {
                throw new InvalidOperationException($"So tien hoan phai lon hon 0 va khong vuot qua {refundable:N0}.");
            }

            var refund = new Payment
            {
                InvoiceId = invoice.InvoiceId,
                Amount = refundDto.Amount,
                PaymentMethod = "Refund",
                PaymentStatus = "Refund",
                PaidBy = userId,
                PaidAt = DateTime.UtcNow,
                Note = string.IsNullOrWhiteSpace(refundDto.Reason) ? "Hoan tien hoa don" : refundDto.Reason.Trim()
            };

            _context.Payments.Add(refund);
            invoice.Payments.Add(refund);
            ApplyInvoicePaymentStatus(invoice);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapToInvoiceDto(invoice);
        }

        public async Task<InvoiceDto?> PayInvoiceFromSePayWebhookAsync(SePayWebhookDto webhookDto)
        {
            var providerTransactionId = webhookDto.Id?.ToString();
            var existingLog = await _context.PaymentWebhookLogs
                .FirstOrDefaultAsync(l => l.Provider == "SePay"
                    && providerTransactionId != null
                    && l.ProviderTransactionId == providerTransactionId);

            if (existingLog?.Status == "Success" && existingLog.InvoiceId is not null)
            {
                return await GetInvoiceByIdAsync(existingLog.InvoiceId.Value);
            }

            var log = existingLog ?? new PaymentWebhookLog
            {
                Provider = "SePay",
                ProviderTransactionId = providerTransactionId,
                ReferenceCode = webhookDto.ReferenceCode,
                Amount = webhookDto.TransferAmount,
                RawPayload = JsonSerializer.Serialize(webhookDto),
                Status = "Received",
                ReceivedAt = DateTime.UtcNow
            };

            if (existingLog == null)
            {
                _context.PaymentWebhookLogs.Add(log);
                await _context.SaveChangesAsync();
            }

            try
            {
                if (!string.Equals(webhookDto.TransferType, "in", StringComparison.OrdinalIgnoreCase))
                {
                    log.Status = "Ignored";
                    log.ProcessedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return null;
                }

                var invoiceId = ExtractInvoiceId(webhookDto.Content)
                    ?? ExtractInvoiceId(webhookDto.Code)
                    ?? ExtractInvoiceId(webhookDto.Description);

                if (invoiceId is null)
                {
                    throw new InvalidOperationException("Khong tim thay ma hoa don trong noi dung giao dich SePay.");
                }

                log.InvoiceId = invoiceId.Value;

                var invoice = await _context.Invoices
                    .Include(i => i.Payments)
                    .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId.Value);

                if (invoice == null)
                {
                    throw new ArgumentException("Khong tim thay hoa don yeu cau.");
                }

                if (invoice.Status == "Cancelled" || invoice.Status == "Refunded")
                {
                    throw new InvalidOperationException("Khong duoc thanh toan hoa don da huy hoac da hoan tien.");
                }

                var sePayKey = BuildSePayKey(webhookDto);
                if (!string.IsNullOrWhiteSpace(sePayKey)
                    && invoice.Payments.Any(p => p.PaymentStatus == "Success"
                        && p.Note != null
                        && p.Note.Contains(sePayKey, StringComparison.OrdinalIgnoreCase)))
                {
                    log.Status = "Success";
                    log.ProcessedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return MapToInvoiceDto(invoice);
                }

                var balanceDue = GetBalanceDue(invoice);
                if (webhookDto.TransferAmount <= 0 || webhookDto.TransferAmount > balanceDue)
                {
                    throw new InvalidOperationException($"So tien thanh toan khong hop le. So con no {balanceDue:N0}.");
                }

                var paidAt = ParseSePayDate(webhookDto.TransactionDate) ?? DateTime.UtcNow;
                var payment = new Payment
                {
                    InvoiceId = invoice.InvoiceId,
                    Amount = webhookDto.TransferAmount,
                    PaymentMethod = "BankTransfer",
                    PaymentStatus = "Success",
                    PaidBy = 0,
                    PaidAt = paidAt,
                    Note = BuildSePayNote(webhookDto, sePayKey)
                };

                _context.Payments.Add(payment);
                invoice.Payments.Add(payment);
                ApplyInvoicePaymentStatus(invoice, paidAt);
                log.Status = "Success";
                log.ProcessedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                if (invoice.Status == "Paid")
                {
                    await PublishInvoicePaidAsync(invoice, "BankTransfer");
                    await UpdatePrescriptionReadyStateAsync(invoice.PrescriptionId);
                }

                return MapToInvoiceDto(invoice);
            }
            catch (Exception ex)
            {
                log.Status = "Failed";
                log.FailureReason = ex.Message.Length <= 255 ? ex.Message : ex.Message[..255];
                log.ProcessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                throw;
            }
        }

        public async Task<List<PaymentWebhookLogDto>> GetPaymentWebhookLogsAsync(string? status)
        {
            var query = _context.PaymentWebhookLogs.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(l => l.Status == status);
            }

            var logs = await query.OrderByDescending(l => l.ReceivedAt).Take(200).ToListAsync();
            return logs.Select(l => new PaymentWebhookLogDto
            {
                PaymentWebhookLogId = l.PaymentWebhookLogId,
                Provider = l.Provider,
                ProviderTransactionId = l.ProviderTransactionId,
                ReferenceCode = l.ReferenceCode,
                InvoiceId = l.InvoiceId,
                Amount = l.Amount,
                Status = l.Status,
                FailureReason = l.FailureReason,
                ReceivedAt = l.ReceivedAt,
                ProcessedAt = l.ProcessedAt
            }).ToList();
        }

        public async Task<bool> CancelInvoiceAsync(int invoiceId)
        {
            var invoice = await _context.Invoices.Include(i => i.Payments).FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
            if (invoice == null) return false;

            if (GetPaidAmount(invoice) > GetRefundedAmount(invoice))
            {
                throw new InvalidOperationException("Hoa don da co thanh toan can hoan tien truoc khi huy.");
            }

            invoice.Status = "Cancelled";
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task UpdatePrescriptionReadyStateAsync(int? prescriptionId)
        {
            if (prescriptionId is null) return;

            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId.Value);

            if (prescription == null || prescription.Status == "Dispensed") return;

            var stock = await BuildPrescriptionAvailabilityAsync(prescription);
            prescription.Status = stock.AllAvailable
                ? "ReadyToDispense"
                : stock.AnyAvailable ? "PartiallyAvailable" : "OutOfStock";

            await _context.SaveChangesAsync();
        }

        private async Task<(bool AllAvailable, bool AnyAvailable)> BuildPrescriptionAvailabilityAsync(Prescription prescription)
        {
            var today = DateTime.UtcNow.Date;
            var allAvailable = true;
            var anyAvailable = false;

            foreach (var item in prescription.PrescriptionItems)
            {
                var quantity = await _context.MedicineBatches
                    .Where(b => b.MedicineId == item.MedicineId
                        && b.Status == "Active"
                        && b.Quantity > 0
                        && (b.ExpiryDate == null || b.ExpiryDate > today))
                    .SumAsync(b => b.Quantity);

                if (quantity >= item.Quantity)
                {
                    anyAvailable = true;
                }
                else
                {
                    allAvailable = false;
                }
            }

            return (allAvailable, anyAvailable);
        }

        private async Task PublishInvoicePaidAsync(Invoice invoice, string paymentMethod)
        {
            await _eventPublisher.PublishAsync("invoice.paid", new InvoicePaidEvent
            {
                InvoiceId = invoice.InvoiceId,
                PatientId = invoice.PatientId,
                TotalAmount = invoice.TotalAmount,
                PaymentMethod = paymentMethod,
                PaidAt = invoice.PaidAt ?? DateTime.UtcNow
            });
        }

        private static InvoiceDto MapToInvoiceDto(Invoice invoice)
        {
            var paidAmount = GetPaidAmount(invoice);
            var refundedAmount = GetRefundedAmount(invoice);
            var balanceDue = Math.Max(0, invoice.TotalAmount - paidAmount + refundedAmount);

            return new InvoiceDto
            {
                InvoiceId = invoice.InvoiceId,
                PatientId = invoice.PatientId,
                AppointmentId = invoice.AppointmentId,
                PrescriptionId = invoice.PrescriptionId,
                ExaminationFee = invoice.ExaminationFee,
                MedicineTotal = invoice.MedicineTotal,
                TotalAmount = invoice.TotalAmount,
                PaidAmount = paidAmount,
                RefundedAmount = refundedAmount,
                BalanceDue = balanceDue,
                Status = invoice.Status,
                CreatedAt = invoice.CreatedAt,
                PaidAt = invoice.PaidAt,
                Payments = invoice.Payments.OrderBy(p => p.PaidAt).Select(p => new PaymentDto
                {
                    PaymentId = p.PaymentId,
                    InvoiceId = p.InvoiceId,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    PaymentStatus = p.PaymentStatus,
                    PaidBy = p.PaidBy,
                    PaidAt = p.PaidAt,
                    Note = p.Note
                }).ToList()
            };
        }

        private static void ApplyInvoicePaymentStatus(Invoice invoice, DateTime? paidAt = null)
        {
            var paidAmount = GetPaidAmount(invoice);
            var refundedAmount = GetRefundedAmount(invoice);
            var netPaid = paidAmount - refundedAmount;

            if (paidAmount <= 0)
            {
                invoice.Status = "Unpaid";
                invoice.PaidAt = null;
                return;
            }

            if (netPaid <= 0)
            {
                invoice.Status = "Refunded";
                return;
            }

            if (refundedAmount > 0)
            {
                invoice.Status = "PartiallyRefunded";
                return;
            }

            invoice.Status = netPaid >= invoice.TotalAmount ? "Paid" : "PartialPaid";
            if (invoice.Status == "Paid")
            {
                invoice.PaidAt ??= paidAt ?? DateTime.UtcNow;
            }
        }

        private static decimal GetPaidAmount(Invoice invoice)
        {
            return invoice.Payments
                .Where(p => p.PaymentStatus == "Success")
                .Sum(p => p.Amount);
        }

        private static decimal GetRefundedAmount(Invoice invoice)
        {
            return invoice.Payments
                .Where(p => p.PaymentStatus == "Refund" || p.PaymentMethod == "Refund")
                .Sum(p => p.Amount);
        }

        private static decimal GetBalanceDue(Invoice invoice)
        {
            return Math.Max(0, invoice.TotalAmount - GetPaidAmount(invoice) + GetRefundedAmount(invoice));
        }

        private static string NormalizePaymentMethod(string? paymentMethod, string? method)
        {
            var value = string.IsNullOrWhiteSpace(paymentMethod) ? method : paymentMethod;
            value = value?.Trim();
            if (string.IsNullOrWhiteSpace(value)) return "Cash";

            return value.Equals("Banking", StringComparison.OrdinalIgnoreCase)
                ? "BankTransfer"
                : value;
        }

        private static string BuildPaymentNote(PayInvoiceDto payDto)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(payDto.Note)) parts.Add(payDto.Note.Trim());
            if (!string.IsNullOrWhiteSpace(payDto.PaymentContent)) parts.Add($"Content: {payDto.PaymentContent.Trim()}");
            if (!string.IsNullOrWhiteSpace(payDto.BankCode)) parts.Add($"Bank: {payDto.BankCode.Trim()}");
            if (!string.IsNullOrWhiteSpace(payDto.BankAccountNumber)) parts.Add($"Account: {payDto.BankAccountNumber.Trim()}");

            var note = parts.Count == 0 ? "Thanh toan hoa don" : string.Join(" | ", parts);
            return note.Length <= 255 ? note : note[..255];
        }

        private static int? ExtractInvoiceId(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var match = Regex.Match(value, @"\bMEDDNU\s*(\d+)\b", RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out var invoiceId) ? invoiceId : null;
        }

        private static string BuildSePayKey(SePayWebhookDto webhookDto)
        {
            if (webhookDto.Id is not null) return $"SePay:{webhookDto.Id}";
            if (!string.IsNullOrWhiteSpace(webhookDto.ReferenceCode)) return $"SePayRef:{webhookDto.ReferenceCode.Trim()}";
            return string.Empty;
        }

        private static string BuildSePayNote(SePayWebhookDto webhookDto, string sePayKey)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(sePayKey)) parts.Add(sePayKey);
            if (!string.IsNullOrWhiteSpace(webhookDto.Content)) parts.Add($"Content: {webhookDto.Content.Trim()}");
            if (!string.IsNullOrWhiteSpace(webhookDto.Gateway)) parts.Add($"Bank: {webhookDto.Gateway.Trim()}");
            if (!string.IsNullOrWhiteSpace(webhookDto.AccountNumber)) parts.Add($"Account: {webhookDto.AccountNumber.Trim()}");
            if (!string.IsNullOrWhiteSpace(webhookDto.ReferenceCode)) parts.Add($"Ref: {webhookDto.ReferenceCode.Trim()}");

            var note = parts.Count == 0 ? "SePay webhook" : string.Join(" | ", parts);
            return note.Length <= 255 ? note : note[..255];
        }

        private static DateTime? ParseSePayDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return DateTime.TryParse(value, out var parsed)
                ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                : null;
        }
    }
}
