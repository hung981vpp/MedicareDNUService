using MedicalAPI.Domain.Constants;
using MedicalAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalAPI.Infrastructure.Data;

public static class MedicalDbSeeder
{
    public static void Seed(MedicalDbContext db)
    {
        db.Database.EnsureCreated();
        EnsureAppointmentSnapshotReason(db);
        EnsureMedicalRecordDiagnosisSpecialty(db);
        EnsureVisitAppointmentUniqueness(db);

        if (db.Patients.Any())
        {
            return;
        }

        var patients = new List<Patient>
        {
            CreatePatient(1, "BN001", "Nguyễn Văn A", new DateOnly(2003, 8, 20), "Nam", "0987654321", "benhnhan@example.com", "Số 12, đường Nguyễn Trãi, Hà Nội", "012345678901", "O", "Dị ứng Penicillin", "Từng bị viêm xoang"),
            CreatePatient(2, "BN002", "Trần Thị Bình", new DateOnly(1995, 4, 12), "Nữ", "0901111222", "0901111222@example.com", "Hà Nội", null, "A", "Chưa ghi nhận", "Không ghi nhận dị ứng"),
            CreatePatient(3, "BN003", "Phạm Quốc Cường", new DateOnly(1988, 11, 3), "Nam", "0903333444", "0903333444@example.com", "Hà Nội", null, "B", "Dị ứng hải sản", "Dị ứng hải sản"),
            CreatePatient(4, "BN004", "Vũ Minh Đức", new DateOnly(1979, 7, 25), "Nam", "0905555666", "0905555666@example.com", "Hà Nội", null, "AB", "Chưa ghi nhận", "Tăng huyết áp"),
            CreatePatient(5, "BN005", "Hoàng Ngọc Anh", new DateOnly(2001, 1, 9), "Nữ", "0907777888", "0907777888@example.com", "Hà Nội", null, "O", "Chưa ghi nhận", "Viêm xoang mạn tính"),
            CreatePatient(6, "BN006", "Đặng Thu Hà", new DateOnly(1992, 9, 18), "Nữ", "0909999000", "0909999000@example.com", "Hà Nội", null, "A", "Chưa ghi nhận", "Không có bệnh nền")
        };

        db.Patients.AddRange(patients);
        db.AppointmentSnapshots.Add(new AppointmentSnapshot
        {
            Id = 1,
            AppointmentId = 1,
            PatientId = 1,
            PatientNameSnapshot = "Nguyễn Văn A",
            DoctorId = 2,
            DoctorNameSnapshot = "BS. Trần Minh",
            SpecialtyId = 1,
            SpecialtyNameSnapshot = "Nội tổng quát",
            ScheduledAt = DateTime.UtcNow.Date.AddHours(10),
            QueueNumber = 12,
            Status = "Đã xác nhận",
            ConfirmedAt = DateTime.UtcNow.AddHours(-1)
        });
        db.Visits.Add(new Visit
        {
            Id = 1,
            VisitCode = "LK001",
            AppointmentId = 1,
            PatientId = 1,
            DoctorId = 2,
            ChiefComplaint = "Sốt nhẹ, đau họng và nghẹt mũi",
            Status = MedicalStatuses.InProgress,
            StartedAt = DateTime.UtcNow.AddMinutes(-20)
        });

        db.SaveChanges();
        ResetIdentitySequences(db);
    }

    private static void EnsureAppointmentSnapshotReason(MedicalDbContext db)
    {
        db.Database.ExecuteSqlRaw(
            """
            ALTER TABLE "AppointmentSnapshots"
            ADD COLUMN IF NOT EXISTS "Reason" character varying(500);
            """);
    }

    private static void EnsureMedicalRecordDiagnosisSpecialty(MedicalDbContext db)
    {
        db.Database.ExecuteSqlRaw(
            """
            ALTER TABLE "MedicalRecords"
            ADD COLUMN IF NOT EXISTS "DiagnosisSpecialty" character varying(100);
            """);
    }

    private static Patient CreatePatient(
        int id,
        string patientCode,
        string fullName,
        DateOnly dateOfBirth,
        string gender,
        string phoneNumber,
        string email,
        string address,
        string? citizenId,
        string bloodType,
        string allergyNote,
        string medicalHistory)
    {
        return new Patient
        {
            Id = id,
            PatientCode = patientCode,
            FullName = fullName,
            DateOfBirth = dateOfBirth,
            Gender = gender,
            PhoneNumber = phoneNumber,
            Email = email,
            Address = address,
            CitizenId = citizenId,
            BloodType = bloodType,
            AllergyNote = allergyNote,
            MedicalHistory = medicalHistory
        };
    }

    private static void ResetIdentitySequences(MedicalDbContext db)
    {
        var resetSqlStatements = new[]
        {
            """SELECT setval(pg_get_serial_sequence('"Patients"', 'Id'), COALESCE((SELECT MAX("Id") FROM "Patients"), 1), (SELECT COUNT(*) FROM "Patients") > 0);""",
            """SELECT setval(pg_get_serial_sequence('"AppointmentSnapshots"', 'Id'), COALESCE((SELECT MAX("Id") FROM "AppointmentSnapshots"), 1), (SELECT COUNT(*) FROM "AppointmentSnapshots") > 0);""",
            """SELECT setval(pg_get_serial_sequence('"Visits"', 'Id'), COALESCE((SELECT MAX("Id") FROM "Visits"), 1), (SELECT COUNT(*) FROM "Visits") > 0);""",
            """SELECT setval(pg_get_serial_sequence('"MedicalRecords"', 'Id'), COALESCE((SELECT MAX("Id") FROM "MedicalRecords"), 1), (SELECT COUNT(*) FROM "MedicalRecords") > 0);""",
            """SELECT setval(pg_get_serial_sequence('"Prescriptions"', 'Id'), COALESCE((SELECT MAX("Id") FROM "Prescriptions"), 1), (SELECT COUNT(*) FROM "Prescriptions") > 0);""",
            """SELECT setval(pg_get_serial_sequence('"PrescriptionItems"', 'Id'), COALESCE((SELECT MAX("Id") FROM "PrescriptionItems"), 1), (SELECT COUNT(*) FROM "PrescriptionItems") > 0);""",
            """SELECT setval(pg_get_serial_sequence('"ClinicalOrders"', 'Id'), COALESCE((SELECT MAX("Id") FROM "ClinicalOrders"), 1), (SELECT COUNT(*) FROM "ClinicalOrders") > 0);""",
            """SELECT setval(pg_get_serial_sequence('"InboxEvents"', 'Id'), COALESCE((SELECT MAX("Id") FROM "InboxEvents"), 1), (SELECT COUNT(*) FROM "InboxEvents") > 0);""",
            """SELECT setval(pg_get_serial_sequence('"OutboxEvents"', 'Id'), COALESCE((SELECT MAX("Id") FROM "OutboxEvents"), 1), (SELECT COUNT(*) FROM "OutboxEvents") > 0);"""
        };

        foreach (var sql in resetSqlStatements)
        {
            db.Database.ExecuteSqlRaw(sql);
        }
    }

    private static void EnsureVisitAppointmentUniqueness(MedicalDbContext db)
    {
        db.Database.ExecuteSqlRaw(
            """
            WITH ranked_visits AS (
                SELECT
                    v."Id",
                    ROW_NUMBER() OVER (
                        PARTITION BY v."AppointmentId"
                        ORDER BY
                            CASE WHEN EXISTS (
                                SELECT 1
                                FROM "MedicalRecords" mr
                                WHERE mr."VisitId" = v."Id"
                            ) THEN 0 ELSE 1 END,
                            v."Id"
                    ) AS duplicate_rank
                FROM "Visits" v
                WHERE v."AppointmentId" IS NOT NULL
            )
            DELETE FROM "Visits" v
            USING ranked_visits ranked
            WHERE v."Id" = ranked."Id"
              AND ranked.duplicate_rank > 1
              AND NOT EXISTS (
                  SELECT 1
                  FROM "MedicalRecords" mr
                  WHERE mr."VisitId" = v."Id"
              );

            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM "Visits"
                    WHERE "AppointmentId" IS NOT NULL
                    GROUP BY "AppointmentId"
                    HAVING COUNT(*) > 1
                ) THEN
                    RAISE EXCEPTION 'Không thể tạo unique index: còn Visit trùng AppointmentId có dữ liệu bệnh án.';
                END IF;
            END $$;

            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM pg_class index_class
                    JOIN pg_index index_info ON index_info.indexrelid = index_class.oid
                    WHERE index_class.relname = 'IX_Visits_AppointmentId'
                      AND NOT index_info.indisunique
                ) THEN
                    DROP INDEX "IX_Visits_AppointmentId";
                END IF;
            END $$;

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Visits_AppointmentId"
                ON "Visits" ("AppointmentId")
                WHERE "AppointmentId" IS NOT NULL;
            """);
    }
}
