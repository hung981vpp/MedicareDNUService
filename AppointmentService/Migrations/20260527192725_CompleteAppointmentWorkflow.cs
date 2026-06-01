using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppointmentService.Migrations
{
    /// <inheritdoc />
    public partial class CompleteAppointmentWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "DoctorSchedules",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "SlotDurationMinutes",
                table: "DoctorSchedules",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AlterColumn<int>(
                name: "QueueNumber",
                table: "Appointments",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "Appointments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WaitingQueues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppointmentId = table.Column<int>(type: "integer", nullable: false),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    DoctorId = table.Column<int>(type: "integer", nullable: false),
                    QueueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    QueueNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitingQueues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaitingQueues_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WaitingQueues_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Appointments",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AppointmentDate", "CreatedAt", "Reason", "UpdatedAt" },
                values: new object[] { new DateOnly(2026, 5, 28), new DateTime(2026, 5, 28, 1, 30, 0, 0, DateTimeKind.Utc), "Kham tim mach dinh ky", null });

            migrationBuilder.UpdateData(
                table: "Appointments",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AppointmentDate", "CreatedAt", "QueueNumber", "Reason", "UpdatedAt" },
                values: new object[] { new DateOnly(2026, 5, 28), new DateTime(2026, 5, 28, 2, 0, 0, 0, DateTimeKind.Utc), null, "Sot va ho", null });

            migrationBuilder.UpdateData(
                table: "DoctorSchedules",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "IsAvailable", "SlotDurationMinutes", "WorkDate" },
                values: new object[] { true, 30, new DateOnly(2026, 5, 28) });

            migrationBuilder.UpdateData(
                table: "DoctorSchedules",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "IsAvailable", "SlotDurationMinutes", "WorkDate" },
                values: new object[] { true, 30, new DateOnly(2026, 5, 28) });

            migrationBuilder.UpdateData(
                table: "DoctorSchedules",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "IsAvailable", "SlotDurationMinutes", "WorkDate" },
                values: new object[] { true, 30, new DateOnly(2026, 5, 28) });

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 28, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 28, 1, 5, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 28, 1, 10, 0, 0, DateTimeKind.Utc));

            migrationBuilder.InsertData(
                table: "WaitingQueues",
                columns: new[] { "Id", "AppointmentId", "CreatedAt", "DoctorId", "PatientId", "QueueDate", "QueueNumber", "Status" },
                values: new object[] { 1, 1, new DateTime(2026, 5, 28, 1, 35, 0, 0, DateTimeKind.Utc), 2, 10, new DateOnly(2026, 5, 28), 3, "Waiting" });

            migrationBuilder.CreateIndex(
                name: "IX_WaitingQueues_AppointmentId",
                table: "WaitingQueues",
                column: "AppointmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WaitingQueues_DoctorId",
                table: "WaitingQueues",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitingQueues_QueueDate_QueueNumber",
                table: "WaitingQueues",
                columns: new[] { "QueueDate", "QueueNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WaitingQueues");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "DoctorSchedules");

            migrationBuilder.DropColumn(
                name: "SlotDurationMinutes",
                table: "DoctorSchedules");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Appointments");

            migrationBuilder.AlterColumn<int>(
                name: "QueueNumber",
                table: "Appointments",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Appointments",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AppointmentDate", "CreatedAt" },
                values: new object[] { new DateOnly(2026, 5, 22), new DateTime(2026, 5, 22, 1, 30, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Appointments",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AppointmentDate", "CreatedAt", "QueueNumber" },
                values: new object[] { new DateOnly(2026, 5, 22), new DateTime(2026, 5, 22, 2, 0, 0, 0, DateTimeKind.Utc), 1 });

            migrationBuilder.UpdateData(
                table: "DoctorSchedules",
                keyColumn: "Id",
                keyValue: 1,
                column: "WorkDate",
                value: new DateOnly(2026, 5, 22));

            migrationBuilder.UpdateData(
                table: "DoctorSchedules",
                keyColumn: "Id",
                keyValue: 2,
                column: "WorkDate",
                value: new DateOnly(2026, 5, 22));

            migrationBuilder.UpdateData(
                table: "DoctorSchedules",
                keyColumn: "Id",
                keyValue: 3,
                column: "WorkDate",
                value: new DateOnly(2026, 5, 22));

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 22, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 22, 1, 5, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 22, 1, 10, 0, 0, DateTimeKind.Utc));
        }
    }
}
