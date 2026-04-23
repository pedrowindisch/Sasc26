using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sasc26.Migrations
{
    /// <inheritdoc />
    public partial class AddRetroactiveCheckIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RetroactiveCheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AttendeeEmail = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    LectureId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Justification = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetroactiveCheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetroactiveCheckIns_Attendees_AttendeeEmail",
                        column: x => x.AttendeeEmail,
                        principalTable: "Attendees",
                        principalColumn: "Email",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RetroactiveCheckIns_Lectures_LectureId",
                        column: x => x.LectureId,
                        principalTable: "Lectures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RetroactiveCheckIns_AttendeeEmail",
                table: "RetroactiveCheckIns",
                column: "AttendeeEmail");

            migrationBuilder.CreateIndex(
                name: "IX_RetroactiveCheckIns_LectureId",
                table: "RetroactiveCheckIns",
                column: "LectureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RetroactiveCheckIns");
        }
    }
}
