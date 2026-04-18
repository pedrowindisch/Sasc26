using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sasc26.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attendees",
                columns: table => new
                {
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Course = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Shift = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Phase = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendees", x => x.Email);
                });

            migrationBuilder.CreateTable(
                name: "TimeSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Shift = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeSlots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Volunteers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Course = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Shift = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Semester = table.Column<int>(type: "INTEGER", nullable: false),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Volunteers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lectures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TimeSlotId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Speaker = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Keyword1 = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Keyword2 = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Keyword3 = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsPreRegistrationEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lectures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lectures_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalTable: "TimeSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerCheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VolunteerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TimeSlotId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerCheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VolunteerCheckIns_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalTable: "TimeSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VolunteerCheckIns_Volunteers_VolunteerId",
                        column: x => x.VolunteerId,
                        principalTable: "Volunteers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AttendeeEmail = table.Column<string>(type: "TEXT", nullable: false),
                    LectureId = table.Column<int>(type: "INTEGER", nullable: true),
                    OtpCode = table.Column<string>(type: "TEXT", maxLength: 6, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SesFallback = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckIns_Attendees_AttendeeEmail",
                        column: x => x.AttendeeEmail,
                        principalTable: "Attendees",
                        principalColumn: "Email",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheckIns_Lectures_LectureId",
                        column: x => x.LectureId,
                        principalTable: "Lectures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PreRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LectureId = table.Column<int>(type: "INTEGER", nullable: false),
                    AttendeeEmail = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OtpCode = table.Column<string>(type: "TEXT", maxLength: 6, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreRegistrations_Lectures_LectureId",
                        column: x => x.LectureId,
                        principalTable: "Lectures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_AttendeeEmail",
                table: "CheckIns",
                column: "AttendeeEmail");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_LectureId",
                table: "CheckIns",
                column: "LectureId");

            migrationBuilder.CreateIndex(
                name: "IX_Lectures_TimeSlotId",
                table: "Lectures",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_PreRegistrations_LectureId_AttendeeEmail",
                table: "PreRegistrations",
                columns: new[] { "LectureId", "AttendeeEmail" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerCheckIns_TimeSlotId",
                table: "VolunteerCheckIns",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerCheckIns_VolunteerId_TimeSlotId",
                table: "VolunteerCheckIns",
                columns: new[] { "VolunteerId", "TimeSlotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Volunteers_Email",
                table: "Volunteers",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckIns");

            migrationBuilder.DropTable(
                name: "PreRegistrations");

            migrationBuilder.DropTable(
                name: "VolunteerCheckIns");

            migrationBuilder.DropTable(
                name: "Attendees");

            migrationBuilder.DropTable(
                name: "Lectures");

            migrationBuilder.DropTable(
                name: "Volunteers");

            migrationBuilder.DropTable(
                name: "TimeSlots");
        }
    }
}
