using MedicalAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalAPI.Infrastructure.Data;

public sealed class MedicalDbContext(DbContextOptions<MedicalDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<AppointmentSnapshot> AppointmentSnapshots => Set<AppointmentSnapshot>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<ClinicalOrder> ClinicalOrders => Set<ClinicalOrder>();
    public DbSet<InboxEvent> InboxEvents => Set<InboxEvent>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigurePatients(modelBuilder);
        ConfigureAppointmentSnapshots(modelBuilder);
        ConfigureVisits(modelBuilder);
        ConfigureMedicalRecords(modelBuilder);
        ConfigurePrescriptions(modelBuilder);
        ConfigurePrescriptionItems(modelBuilder);
        ConfigureClinicalOrders(modelBuilder);
        ConfigureInboxEvents(modelBuilder);
        ConfigureOutboxEvents(modelBuilder);
    }

    private static void ConfigurePatients(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Patient>();
        entity.HasIndex(p => p.PatientCode).IsUnique();
        entity.HasIndex(p => p.PhoneNumber);
        entity.Property(p => p.PatientCode).HasMaxLength(20);
        entity.Property(p => p.FullName).HasMaxLength(150);
        entity.Property(p => p.Gender).HasMaxLength(20);
        entity.Property(p => p.PhoneNumber).HasMaxLength(20);
        entity.Property(p => p.Email).HasMaxLength(150);
        entity.Property(p => p.Address).HasMaxLength(255);
        entity.Property(p => p.CitizenId).HasMaxLength(20);
        entity.Property(p => p.BloodType).HasMaxLength(10);
        entity.Property(p => p.Status).HasMaxLength(30);
    }

    private static void ConfigureAppointmentSnapshots(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AppointmentSnapshot>();
        entity.HasIndex(a => a.AppointmentId).IsUnique();
        entity.Property(a => a.PatientNameSnapshot).HasMaxLength(150);
        entity.Property(a => a.DoctorNameSnapshot).HasMaxLength(150);
        entity.Property(a => a.SpecialtyNameSnapshot).HasMaxLength(150);
        entity.Property(a => a.Status).HasMaxLength(30);
    }

    private static void ConfigureVisits(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Visit>();
        entity.HasIndex(v => v.VisitCode).IsUnique();
        entity.HasIndex(v => v.AppointmentId);
        entity.HasIndex(v => v.PatientId);
        entity.HasIndex(v => v.DoctorId);
        entity.Property(v => v.VisitCode).HasMaxLength(20);
        entity.Property(v => v.ChiefComplaint).HasMaxLength(500);
        entity.Property(v => v.Status).HasMaxLength(30);
        entity.Property(v => v.CancelReason).HasMaxLength(500);
        entity.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(v => v.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureMedicalRecords(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<MedicalRecord>();
        entity.HasIndex(r => r.MedicalRecordCode).IsUnique();
        entity.HasIndex(r => r.VisitId).IsUnique();
        entity.HasIndex(r => r.PatientId);
        entity.Property(r => r.MedicalRecordCode).HasMaxLength(20);
        entity.Property(r => r.DiagnosisCode).HasMaxLength(50);
        entity.Property(r => r.DiagnosisText).HasMaxLength(500);
        entity.Property(r => r.Status).HasMaxLength(30);
        entity.HasOne<Visit>()
            .WithOne()
            .HasForeignKey<MedicalRecord>(r => r.VisitId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(r => r.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurePrescriptions(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Prescription>();
        entity.HasIndex(p => p.PrescriptionCode).IsUnique();
        entity.HasIndex(p => p.MedicalRecordId);
        entity.HasIndex(p => p.PatientId);
        entity.Property(p => p.PrescriptionCode).HasMaxLength(20);
        entity.Property(p => p.Status).HasMaxLength(30);
        entity.Property(p => p.CancelReason).HasMaxLength(500);
        entity.HasOne<MedicalRecord>()
            .WithMany()
            .HasForeignKey(p => p.MedicalRecordId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurePrescriptionItems(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<PrescriptionItem>();
        entity.HasIndex(i => i.PrescriptionItemCode).IsUnique();
        entity.HasIndex(i => i.PrescriptionId);
        entity.Property(i => i.PrescriptionItemCode).HasMaxLength(30);
        entity.Property(i => i.MedicineNameSnapshot).HasMaxLength(200);
        entity.Property(i => i.UnitSnapshot).HasMaxLength(50);
        entity.Property(i => i.Dosage).HasMaxLength(100);
        entity.Property(i => i.Frequency).HasMaxLength(100);
        entity.Property(i => i.Quantity).HasPrecision(10, 2);
        entity.Property(i => i.UsageInstruction).HasMaxLength(500);
        entity.Property(i => i.Note).HasMaxLength(500);
        entity.HasOne<Prescription>()
            .WithMany()
            .HasForeignKey(i => i.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureClinicalOrders(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ClinicalOrder>();
        entity.HasIndex(o => o.ClinicalOrderCode).IsUnique();
        entity.HasIndex(o => o.MedicalRecordId);
        entity.Property(o => o.ClinicalOrderCode).HasMaxLength(20);
        entity.Property(o => o.OrderType).HasMaxLength(50);
        entity.Property(o => o.OrderName).HasMaxLength(200);
        entity.Property(o => o.Reason).HasMaxLength(500);
        entity.Property(o => o.Status).HasMaxLength(30);
        entity.HasOne<MedicalRecord>()
            .WithMany()
            .HasForeignKey(o => o.MedicalRecordId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureInboxEvents(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<InboxEvent>();
        entity.HasIndex(e => e.EventCode).IsUnique();
        entity.Property(e => e.EventCode).HasMaxLength(30);
        entity.Property(e => e.SourceService).HasMaxLength(100);
        entity.Property(e => e.EventType).HasMaxLength(100);
        entity.Property(e => e.Status).HasMaxLength(30);
    }

    private static void ConfigureOutboxEvents(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<OutboxEvent>();
        entity.HasIndex(e => e.EventCode).IsUnique();
        entity.HasIndex(e => e.Status);
        entity.Property(e => e.EventCode).HasMaxLength(30);
        entity.Property(e => e.EventType).HasMaxLength(100);
        entity.Property(e => e.AggregateType).HasMaxLength(100);
        entity.Property(e => e.Status).HasMaxLength(30);
    }
}
