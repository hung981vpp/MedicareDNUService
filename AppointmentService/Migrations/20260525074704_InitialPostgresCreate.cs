using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AppointmentService.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgresCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Specialties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Doctors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SpecialtyId = table.Column<int>(type: "integer", nullable: false),
                    ExamFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doctors_Specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalTable: "Specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    PatientNameSnapshot = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PatientPhoneSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DoctorId = table.Column<int>(type: "integer", nullable: false),
                    AppointmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SlotTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    QueueNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DoctorSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DoctorId = table.Column<int>(type: "integer", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorSchedules_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Specialties",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Tim mach" },
                    { 2, "Noi tong quat" }
                });

            migrationBuilder.InsertData(
                table: "Doctors",
                columns: new[] { "Id", "ExamFee", "Name", "SpecialtyId" },
                values: new object[,]
                {
                    { 1, 120000m, "Bac si Minh", 2 },
                    { 2, 150000m, "Bac si A", 1 }
                });

            migrationBuilder.InsertData(
                table: "Appointments",
                columns: new[] { "Id", "AppointmentDate", "CreatedAt", "DoctorId", "PatientId", "PatientNameSnapshot", "PatientPhoneSnapshot", "QueueNumber", "SlotTime", "Status" },
                values: new object[,]
                {
                    { 1, new DateOnly(2026, 5, 22), new DateTime(2026, 5, 22, 1, 30, 0, 0, DateTimeKind.Utc), 2, 10, "Nguyen Van B", "0987654321", 3, new TimeOnly(8, 30, 0), "Confirmed" },
                    { 2, new DateOnly(2026, 5, 22), new DateTime(2026, 5, 22, 2, 0, 0, 0, DateTimeKind.Utc), 1, 11, "Tran Thi C", "0912345678", 1, new TimeOnly(13, 30, 0), "Pending" }
                });

            migrationBuilder.InsertData(
                table: "DoctorSchedules",
                columns: new[] { "Id", "DoctorId", "EndTime", "StartTime", "WorkDate" },
                values: new object[,]
                {
                    { 1, 2, new TimeOnly(11, 30, 0), new TimeOnly(8, 0, 0), new DateOnly(2026, 5, 22) },
                    { 2, 1, new TimeOnly(16, 30, 0), new TimeOnly(13, 0, 0), new DateOnly(2026, 5, 22) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId_AppointmentDate_SlotTime",
                table: "Appointments",
                columns: new[] { "DoctorId", "AppointmentDate", "SlotTime" },
                unique: true,
                filter: "\"Status\" <> 'Cancelled'");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_SpecialtyId",
                table: "Doctors",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorSchedules_DoctorId_WorkDate_StartTime",
                table: "DoctorSchedules",
                columns: new[] { "DoctorId", "WorkDate", "StartTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Specialties_Name",
                table: "Specialties",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "DoctorSchedules");

            migrationBuilder.DropTable(
                name: "Doctors");

            migrationBuilder.DropTable(
                name: "Specialties");
        }
    }
}
