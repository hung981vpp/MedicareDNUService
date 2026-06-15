using Microsoft.EntityFrameworkCore;
using PharmacyBillingService.Models;
using PharmacyBillingService.Helpers;
using System;

namespace PharmacyBillingService.Data
{
    public class PharmacyDbContext : DbContext
    {
        public PharmacyDbContext(DbContextOptions<PharmacyDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Medicine> Medicines { get; set; } = null!;
        public DbSet<MedicineBatch> MedicineBatches { get; set; } = null!;
        public DbSet<Prescription> Prescriptions { get; set; } = null!;
        public DbSet<PrescriptionItem> PrescriptionItems { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<StockTransaction> StockTransactions { get; set; } = null!;
        public DbSet<ProcessedEvent> ProcessedEvents { get; set; } = null!;
        public DbSet<PaymentWebhookLog> PaymentWebhookLogs { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure unique index for User Email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique()
                .HasFilter("\"PhoneNumber\" IS NOT NULL");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.PatientId);

            modelBuilder.Entity<MedicineBatch>()
                .HasIndex(b => new { b.MedicineId, b.BatchNumber })
                .IsUnique();

            modelBuilder.Entity<MedicineBatch>()
                .HasOne(b => b.Medicine)
                .WithMany()
                .HasForeignKey(b => b.MedicineId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProcessedEvent>()
                .HasIndex(e => e.EventKey)
                .IsUnique();

            modelBuilder.Entity<PaymentWebhookLog>()
                .HasIndex(w => new { w.Provider, w.ProviderTransactionId })
                .IsUnique()
                .HasFilter("\"ProviderTransactionId\" IS NOT NULL");

            modelBuilder.Entity<PaymentWebhookLog>()
                .HasIndex(w => new { w.Provider, w.ReferenceCode })
                .HasFilter("\"ReferenceCode\" IS NOT NULL");

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.UserId);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead });

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure relations for PrescriptionItems
            modelBuilder.Entity<PrescriptionItem>()
                .HasOne(pi => pi.Prescription)
                .WithMany(p => p.PrescriptionItems)
                .HasForeignKey(pi => pi.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PrescriptionItem>()
                .HasOne(pi => pi.Medicine)
                .WithMany()
                .HasForeignKey(pi => pi.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure relations for Invoice
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Prescription)
                .WithMany()
                .HasForeignKey(i => i.PrescriptionId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.PrescriptionId)
                .IsUnique()
                .HasFilter("\"PrescriptionId\" IS NOT NULL AND \"Status\" <> 'Cancelled'");

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.AppointmentId)
                .IsUnique()
                .HasFilter("\"AppointmentId\" IS NOT NULL AND \"PrescriptionId\" IS NULL AND \"Status\" <> 'Cancelled'");

            // Configure relations for Payment
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure relations for StockTransaction
            modelBuilder.Entity<StockTransaction>()
                .HasOne(st => st.Medicine)
                .WithMany()
                .HasForeignKey(st => st.MedicineId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StockTransaction>()
                .HasOne(st => st.Batch)
                .WithMany()
                .HasForeignKey(st => st.BatchId)
                .OnDelete(DeleteBehavior.SetNull);

            // Seed default Users (N3 roles: Admin, Nurse, Pharmacist, Patient)
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    FullName = "System Administrator",
                    Email = "admin@clinic.com",
                    Username = "admin",
                    PasswordHash = PasswordHasher.HashPassword("Admin@123"),
                    Role = "Admin",
                    Status = "Active",
                    CreatedAt = new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    UserId = 2,
                    FullName = "Pharmacist Nguyen Van A",
                    Email = "pharmacist@clinic.com",
                    Username = "pharmacist",
                    PasswordHash = PasswordHasher.HashPassword("Pharmacist@123"),
                    Role = "Pharmacist",
                    Status = "Active",
                    CreatedAt = new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    UserId = 3,
                    FullName = "Nurse Tran Thi B",
                    Email = "nurse@clinic.com",
                    Username = "nurse",
                    PasswordHash = PasswordHasher.HashPassword("Nurse@123"),
                    Role = "Nurse",
                    Status = "Active",
                    CreatedAt = new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    UserId = 4,
                    FullName = "Patient Pham Van D",
                    Email = "patient@clinic.com",
                    Username = "patient",
                    PasswordHash = PasswordHasher.HashPassword("Patient@123"),
                    Role = "Patient",
                    PatientId = 1001,
                    Status = "Active",
                    CreatedAt = new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // Seed default Medicines
            modelBuilder.Entity<Medicine>().HasData(
                new Medicine
                {
                    MedicineId = 1,
                    MedicineName = "Paracetamol 500mg",
                    ActiveIngredient = "Paracetamol",
                    MedicineType = "Giảm đau - hạ sốt",
                    Unit = "Viên",
                    Price = 2000m,
                    StockQuantity = 100,
                    MinStockLevel = 10,
                    ExpiryDate = new DateTime(2028, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                    Status = "Active",
                    CreatedAt = new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc)
                },
                new Medicine
                {
                    MedicineId = 2,
                    MedicineName = "Vitamin C",
                    ActiveIngredient = "Ascorbic Acid",
                    MedicineType = "Vitamin - khoáng chất",
                    Unit = "Viên",
                    Price = 6000m,
                    StockQuantity = 50,
                    MinStockLevel = 10,
                    ExpiryDate = new DateTime(2027, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                    Status = "Active",
                    CreatedAt = new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc)
                },
                new Medicine
                {
                    MedicineId = 3,
                    MedicineName = "Amoxicillin 500mg",
                    ActiveIngredient = "Amoxicillin",
                    MedicineType = "Kháng sinh",
                    Unit = "Viên",
                    Price = 8000m,
                    StockQuantity = 120,
                    MinStockLevel = 15,
                    ExpiryDate = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc),
                    Status = "Active",
                    CreatedAt = new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            modelBuilder.Entity<MedicineBatch>().HasData(
                new MedicineBatch
                {
                    BatchId = 1,
                    MedicineId = 1,
                    BatchNumber = "SEED-PARA-2028",
                    ExpiryDate = new DateTime(2028, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                    Quantity = 100,
                    InitialQuantity = 100,
                    Status = "Active",
                    CreatedAt = new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc)
                },
                new MedicineBatch
                {
                    BatchId = 2,
                    MedicineId = 2,
                    BatchNumber = "SEED-VITC-2027",
                    ExpiryDate = new DateTime(2027, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                    Quantity = 50,
                    InitialQuantity = 50,
                    Status = "Active",
                    CreatedAt = new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc)
                },
                new MedicineBatch
                {
                    BatchId = 3,
                    MedicineId = 3,
                    BatchNumber = "SEED-AMOX-2026",
                    ExpiryDate = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc),
                    Quantity = 120,
                    InitialQuantity = 120,
                    Status = "Active",
                    CreatedAt = new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}
