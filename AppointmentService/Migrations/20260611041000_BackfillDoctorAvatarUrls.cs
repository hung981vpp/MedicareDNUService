using AppointmentService.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentService.Migrations
{
    [DbContext(typeof(AppointmentDbContext))]
    [Migration("20260611041000_BackfillDoctorAvatarUrls")]
    public partial class BackfillDoctorAvatarUrls : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Doctors"
                SET "AvatarUrl" = CASE
                    WHEN lower(trim("Gender")) IN ('female', 'nữ', 'nu') THEN 'https://images.unsplash.com/photo-1559839734-2b71ea197ec2?auto=format&fit=crop&w=600&q=80'
                    WHEN lower(trim("Gender")) IN ('male', 'nam') THEN 'https://images.unsplash.com/photo-1622253692010-333f2da6031d?auto=format&fit=crop&w=600&q=80'
                    ELSE 'https://images.unsplash.com/photo-1582750433449-648ed127bb54?auto=format&fit=crop&w=600&q=80'
                END
                WHERE "AvatarUrl" IS NULL OR trim("AvatarUrl") = '';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
