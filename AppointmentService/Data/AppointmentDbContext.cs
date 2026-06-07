using AppointmentService.Constants;
using AppointmentService.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Data;

public sealed class AppointmentDbContext : DbContext
{
    public AppointmentDbContext(DbContextOptions<AppointmentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<Doctor> Doctors => Set<Doctor>();

    public DbSet<Specialty> Specialties => Set<Specialty>();

    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();

    public DbSet<QueueEntry> WaitingQueues => Set<QueueEntry>();

    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.ToTable("OutboxEvents");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Payload).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.OccurredAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.EventCode).IsUnique();
        });


        modelBuilder.Entity<Specialty>(entity =>
        {
            entity.ToTable("Specialties");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.ToTable("Doctors");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Degree).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ExamFee).HasPrecision(18, 2);
            entity.Property(x => x.Phone).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Gender).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.AvatarUrl).HasMaxLength(500).IsRequired();
            entity.Property(x => x.RoomNumber).HasMaxLength(30).IsRequired();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne<Specialty>()
                .WithMany()
                .HasForeignKey(x => x.SpecialtyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DoctorSchedule>(entity =>
        {
            entity.ToTable("DoctorSchedules");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SlotDurationMinutes).HasDefaultValue(30);
            entity.Property(x => x.IsAvailable).HasDefaultValue(true);
            entity.HasIndex(x => new { x.DoctorId, x.WorkDate, x.StartTime }).IsUnique();
            entity.HasOne<Doctor>()
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("Appointments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PatientNameSnapshot).HasMaxLength(120).IsRequired();
            entity.Property(x => x.PatientPhoneSnapshot).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.CancelReason).HasMaxLength(500);
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(x => new { x.DoctorId, x.AppointmentDate, x.SlotTime })
                .IsUnique()
                .HasFilter("\"Status\" NOT IN ('Cancelled', 'Expired', 'NoShow')");
            entity.HasOne<Doctor>()
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QueueEntry>(entity =>
        {
            entity.ToTable("WaitingQueues");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(x => x.AppointmentId).IsUnique();
            entity.HasIndex(x => new { x.DoctorId, x.QueueDate, x.QueueNumber }).IsUnique();
            entity.HasOne<Appointment>()
                .WithMany()
                .HasForeignKey(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Doctor>()
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Specialty>().HasData(
            new Specialty { Id = 1, Name = "Tim mach" },
            new Specialty { Id = 2, Name = "Nhi khoa" },
            new Specialty { Id = 3, Name = "Da lieu" });

        modelBuilder.Entity<Doctor>().HasData(
            new Doctor
            {
                Id = 1,
                UserId = null,
                FullName = "Bac si Nguyen Van A",
                SpecialtyId = 1,
                Degree = "Thac si, Bac si CKI",
                ExperienceYears = 12,
                ExamFee = 150000,
                Phone = "0901000001",
                Email = "nguyenvana@clinic.test",
                Gender = "Male",
                DateOfBirth = new DateOnly(1985, 3, 12),
                Description = "Chuyen kham va dieu tri benh ly tim mach, tang huyet ap va roi loan nhip tim.",
                AvatarUrl = "https://images.unsplash.com/photo-1622253692010-333f2da6031d?auto=format&fit=crop&w=600&q=80",
                RoomNumber = "A-201",
                IsActive = true,
                CreatedAt = new DateTime(2026, 5, 28, 1, 0, 0, DateTimeKind.Utc),
                UpdatedAt = null
            },
            new Doctor
            {
                Id = 2,
                UserId = null,
                FullName = "Bac si Tran Thi B",
                SpecialtyId = 2,
                Degree = "Bac si CKII",
                ExperienceYears = 10,
                ExamFee = 120000,
                Phone = "0901000002",
                Email = "tranthib@clinic.test",
                Gender = "Female",
                DateOfBirth = new DateOnly(1988, 7, 24),
                Description = "Phu trach tham kham nhi khoa, tu van dinh duong va theo doi suc khoe tre em.",
                AvatarUrl = "https://images.unsplash.com/photo-1559839734-2b71ea197ec2?auto=format&fit=crop&w=600&q=80",
                RoomNumber = "B-102",
                IsActive = true,
                CreatedAt = new DateTime(2026, 5, 28, 1, 5, 0, DateTimeKind.Utc),
                UpdatedAt = null
            },
            new Doctor
            {
                Id = 3,
                UserId = null,
                FullName = "Bac si Le Van C",
                SpecialtyId = 3,
                Degree = "Bac si Da lieu",
                ExperienceYears = 8,
                ExamFee = 100000,
                Phone = "0901000003",
                Email = "levanc@clinic.test",
                Gender = "Male",
                DateOfBirth = new DateOnly(1990, 11, 5),
                Description = "Tu van va dieu tri cac van de ve da, mun, di ung da va cham soc da.",
                AvatarUrl = "https://images.unsplash.com/photo-1582750433449-648ed127bb54?auto=format&fit=crop&w=600&q=80",
                RoomNumber = "C-305",
                IsActive = true,
                CreatedAt = new DateTime(2026, 5, 28, 1, 10, 0, DateTimeKind.Utc),
                UpdatedAt = null
            });

        modelBuilder.Entity<DoctorSchedule>().HasData(
            new DoctorSchedule
            {
                Id = 1,
                DoctorId = 2,
                WorkDate = new DateOnly(2026, 5, 28),
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(11, 30),
                SlotDurationMinutes = 30,
                IsAvailable = true
            },
            new DoctorSchedule
            {
                Id = 2,
                DoctorId = 1,
                WorkDate = new DateOnly(2026, 5, 28),
                StartTime = new TimeOnly(13, 0),
                EndTime = new TimeOnly(16, 30),
                SlotDurationMinutes = 30,
                IsAvailable = true
            },
            new DoctorSchedule
            {
                Id = 3,
                DoctorId = 3,
                WorkDate = new DateOnly(2026, 5, 28),
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(10, 0),
                SlotDurationMinutes = 30,
                IsAvailable = true
            });

        modelBuilder.Entity<Appointment>().HasData(
            new Appointment
            {
                Id = 1,
                PatientId = 10,
                PatientNameSnapshot = "Nguyen Van B",
                PatientPhoneSnapshot = "0987654321",
                DoctorId = 2,
                AppointmentDate = new DateOnly(2026, 5, 28),
                SlotTime = new TimeOnly(8, 30),
                Reason = "Kham tim mach dinh ky",
                Status = AppointmentStatus.Confirmed,
                QueueNumber = 3,
                CreatedAt = new DateTime(2026, 5, 28, 1, 30, 0, DateTimeKind.Utc),
                UpdatedAt = null
            },
            new Appointment
            {
                Id = 2,
                PatientId = 11,
                PatientNameSnapshot = "Tran Thi C",
                PatientPhoneSnapshot = "0912345678",
                DoctorId = 1,
                AppointmentDate = new DateOnly(2026, 5, 28),
                SlotTime = new TimeOnly(13, 30),
                Reason = "Sot va ho",
                Status = AppointmentStatus.Pending,
                QueueNumber = null,
                CreatedAt = new DateTime(2026, 5, 28, 2, 0, 0, DateTimeKind.Utc),
                UpdatedAt = null
            });

        modelBuilder.Entity<QueueEntry>().HasData(
            new QueueEntry
            {
                Id = 1,
                AppointmentId = 1,
                PatientId = 10,
                DoctorId = 2,
                QueueDate = new DateOnly(2026, 5, 28),
                QueueNumber = 3,
                Status = QueueStatus.Waiting,
                CreatedAt = new DateTime(2026, 5, 28, 1, 35, 0, DateTimeKind.Utc)
            });
    }
}
