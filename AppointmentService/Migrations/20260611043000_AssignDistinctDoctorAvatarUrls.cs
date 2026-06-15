using AppointmentService.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentService.Migrations
{
    [DbContext(typeof(AppointmentDbContext))]
    [Migration("20260611043000_AssignDistinctDoctorAvatarUrls")]
    public partial class AssignDistinctDoctorAvatarUrls : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Doctors"
                SET "AvatarUrl" = CASE
                    WHEN lower(trim("Gender")) IN ('female', 'nữ', 'nu') THEN
                        CASE ("Id" % 5)
                            WHEN 0 THEN 'https://images.unsplash.com/photo-1559839734-2b71ea197ec2?auto=format&fit=crop&w=600&q=80'
                            WHEN 1 THEN 'https://images.unsplash.com/photo-1594824476967-48c8b964273f?auto=format&fit=crop&w=600&q=80'
                            WHEN 2 THEN 'https://images.unsplash.com/photo-1638202993928-7267aad84c31?auto=format&fit=crop&w=600&q=80'
                            WHEN 3 THEN 'https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?auto=format&fit=crop&w=600&q=80'
                            ELSE 'https://images.unsplash.com/photo-1580489944761-15a19d654956?auto=format&fit=crop&w=600&q=80'
                        END
                    WHEN lower(trim("Gender")) IN ('male', 'nam') THEN
                        CASE ("Id" % 5)
                            WHEN 0 THEN 'https://images.unsplash.com/photo-1622253692010-333f2da6031d?auto=format&fit=crop&w=600&q=80'
                            WHEN 1 THEN 'https://images.unsplash.com/photo-1582750433449-648ed127bb54?auto=format&fit=crop&w=600&q=80'
                            WHEN 2 THEN 'https://images.unsplash.com/photo-1537368910025-700350fe46c7?auto=format&fit=crop&w=600&q=80'
                            WHEN 3 THEN 'https://images.unsplash.com/photo-1612349317150-e413f6a5b16d?auto=format&fit=crop&w=600&q=80'
                            ELSE 'https://images.unsplash.com/photo-1651008376811-b90baee60c1f?auto=format&fit=crop&w=600&q=80'
                        END
                    ELSE
                        CASE ("Id" % 3)
                            WHEN 0 THEN 'https://images.unsplash.com/photo-1582750433449-648ed127bb54?auto=format&fit=crop&w=600&q=80'
                            WHEN 1 THEN 'https://images.unsplash.com/photo-1612349317150-e413f6a5b16d?auto=format&fit=crop&w=600&q=80'
                            ELSE 'https://images.unsplash.com/photo-1559839734-2b71ea197ec2?auto=format&fit=crop&w=600&q=80'
                        END
                END
                WHERE "AvatarUrl" IS NULL
                   OR trim("AvatarUrl") = ''
                   OR "AvatarUrl" IN (
                        'https://images.unsplash.com/photo-1622253692010-333f2da6031d?auto=format&fit=crop&w=600&q=80',
                        'https://images.unsplash.com/photo-1559839734-2b71ea197ec2?auto=format&fit=crop&w=600&q=80',
                        'https://images.unsplash.com/photo-1582750433449-648ed127bb54?auto=format&fit=crop&w=600&q=80'
                   );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
