using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentService.Migrations
{
    /// <inheritdoc />
    public partial class CompleteN1AppointmentWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "Appointments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedInAt",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.DropIndex(
                name: "IX_Appointments_DoctorId_AppointmentDate_SlotTime",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId_AppointmentDate_SlotTime",
                table: "Appointments",
                columns: new[] { "DoctorId", "AppointmentDate", "SlotTime" },
                unique: true,
                filter: "\"Status\" NOT IN ('Cancelled', 'Expired', 'NoShow')");

            migrationBuilder.DropIndex(
                name: "IX_WaitingQueues_QueueDate_QueueNumber",
                table: "WaitingQueues");

            migrationBuilder.CreateIndex(
                name: "IX_WaitingQueues_DoctorId_QueueDate_QueueNumber",
                table: "WaitingQueues",
                columns: new[] { "DoctorId", "QueueDate", "QueueNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WaitingQueues_DoctorId_QueueDate_QueueNumber",
                table: "WaitingQueues");

            migrationBuilder.CreateIndex(
                name: "IX_WaitingQueues_QueueDate_QueueNumber",
                table: "WaitingQueues",
                columns: new[] { "QueueDate", "QueueNumber" },
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_Appointments_DoctorId_AppointmentDate_SlotTime",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId_AppointmentDate_SlotTime",
                table: "Appointments",
                columns: new[] { "DoctorId", "AppointmentDate", "SlotTime" },
                unique: true,
                filter: "\"Status\" <> 'Cancelled'");

            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CheckedInAt",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Appointments");
        }
    }
}
