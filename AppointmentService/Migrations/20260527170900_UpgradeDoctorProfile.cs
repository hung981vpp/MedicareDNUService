using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentService.Migrations
{
    /// <inheritdoc />
    public partial class UpgradeDoctorProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Doctors",
                newName: "FullName");

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "Doctors",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Doctors",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "Doctors",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Degree",
                table: "Doctors",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Doctors",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Doctors",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ExperienceYears",
                table: "Doctors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Doctors",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Doctors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Doctors",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RoomNumber",
                table: "Doctors",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Doctors",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Doctors",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AvatarUrl", "CreatedAt", "DateOfBirth", "Degree", "Description", "Email", "ExamFee", "ExperienceYears", "FullName", "Gender", "IsActive", "Phone", "RoomNumber", "SpecialtyId", "UpdatedAt", "UserId" },
                values: new object[] { "https://images.unsplash.com/photo-1622253692010-333f2da6031d?auto=format&fit=crop&w=600&q=80", new DateTime(2026, 5, 22, 1, 0, 0, 0, DateTimeKind.Utc), new DateOnly(1985, 3, 12), "Thac si, Bac si CKI", "Chuyen kham va dieu tri benh ly tim mach, tang huyet ap va roi loan nhip tim.", "nguyenvana@clinic.test", 150000m, 12, "Bac si Nguyen Van A", "Male", true, "0901000001", "A-201", 1, null, null });

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AvatarUrl", "CreatedAt", "DateOfBirth", "Degree", "Description", "Email", "ExamFee", "ExperienceYears", "FullName", "Gender", "IsActive", "Phone", "RoomNumber", "SpecialtyId", "UpdatedAt", "UserId" },
                values: new object[] { "https://images.unsplash.com/photo-1559839734-2b71ea197ec2?auto=format&fit=crop&w=600&q=80", new DateTime(2026, 5, 22, 1, 5, 0, 0, DateTimeKind.Utc), new DateOnly(1988, 7, 24), "Bac si CKII", "Phu trach tham kham nhi khoa, tu van dinh duong va theo doi suc khoe tre em.", "tranthib@clinic.test", 120000m, 10, "Bac si Tran Thi B", "Female", true, "0901000002", "B-102", 2, null, null });

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Nhi khoa");

            migrationBuilder.InsertData(
                table: "Specialties",
                columns: new[] { "Id", "Name" },
                values: new object[] { 3, "Da lieu" });

            migrationBuilder.InsertData(
                table: "Doctors",
                columns: new[] { "Id", "AvatarUrl", "CreatedAt", "DateOfBirth", "Degree", "Description", "Email", "ExamFee", "ExperienceYears", "FullName", "Gender", "IsActive", "Phone", "RoomNumber", "SpecialtyId", "UpdatedAt", "UserId" },
                values: new object[] { 3, "https://images.unsplash.com/photo-1582750433449-648ed127bb54?auto=format&fit=crop&w=600&q=80", new DateTime(2026, 5, 22, 1, 10, 0, 0, DateTimeKind.Utc), new DateOnly(1990, 11, 5), "Bac si Da lieu", "Tu van va dieu tri cac van de ve da, mun, di ung da va cham soc da.", "levanc@clinic.test", 100000m, 8, "Bac si Le Van C", "Male", true, "0901000003", "C-305", 3, null, null });

            migrationBuilder.InsertData(
                table: "DoctorSchedules",
                columns: new[] { "Id", "DoctorId", "EndTime", "StartTime", "WorkDate" },
                values: new object[] { 3, 3, new TimeOnly(10, 0, 0), new TimeOnly(8, 0, 0), new DateOnly(2026, 5, 22) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DoctorSchedules",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "Degree",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "ExperienceYears",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "RoomNumber",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Doctors");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "Doctors",
                newName: "Name");

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ExamFee", "Name", "SpecialtyId" },
                values: new object[] { 120000m, "Bac si Minh", 2 });

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ExamFee", "Name", "SpecialtyId" },
                values: new object[] { 150000m, "Bac si A", 1 });

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Noi tong quat");
        }
    }
}
