using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sasc26.Migrations
{
    /// <inheritdoc />
    public partial class AddPreRegistrationConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PreRegistrationConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    IsFormEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    FormTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FormDescription = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    FormButtonText = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FormFields = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreRegistrationConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreRegistrationConfigs_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreRegistrationFormSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AttendeeEmail = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FormData = table.Column<string>(type: "TEXT", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreRegistrationFormSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreRegistrationFormSubmissions_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreRegistrationConfigs_EventId",
                table: "PreRegistrationConfigs",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreRegistrationFormSubmissions_EventId",
                table: "PreRegistrationFormSubmissions",
                column: "EventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreRegistrationConfigs");

            migrationBuilder.DropTable(
                name: "PreRegistrationFormSubmissions");
        }
    }
}
