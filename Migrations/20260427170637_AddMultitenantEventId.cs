using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sasc26.Migrations
{
    /// <inheritdoc />
    public partial class AddMultitenantEventId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, create the Events table so we can reference it in foreign keys
            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Subtitle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    AllowedEmailDomain = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    InstagramUrl = table.Column<string>(type: "TEXT", nullable: false),
                    TshirtPresaleUrl = table.Column<string>(type: "TEXT", nullable: false),
                    AdminEmailsJson = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PrimaryColor = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AccentColor = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    BackgroundColor = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TextColor = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    LogoImage = table.Column<byte[]>(type: "BLOB", nullable: true),
                    LogoContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PostCheckinButtonsJson = table.Column<string>(type: "TEXT", nullable: false),
                    AdminEmails = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            // Seed a default event with Id=1 so existing rows with EventId=0 can reference it
            // We'll update existing rows to EventId=1 after adding the column
            migrationBuilder.InsertData(
                table: "Events",
                columns: new[] { "Id", "Slug", "Name", "Subtitle", "AllowedEmailDomain", "InstagramUrl", "TshirtPresaleUrl", "AdminEmailsJson", "IsActive", "CreatedAt", "PrimaryColor", "AccentColor", "BackgroundColor", "TextColor", "LogoContentType", "PostCheckinButtonsJson", "AdminEmails" },
                values: new object[] { 1, "default", "Default Event", "", "furb.br", "", "", "[]", true, DateTime.UtcNow, "#113D76", "#2EBDEF", "#f5f5f5", "#1a1a1a", "", "[]", "[]" });

            // Temporarily disable foreign key checks while adding columns
            migrationBuilder.Sql("PRAGMA foreign_keys = OFF;");

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "Volunteers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "VolunteerCheckIns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "TimeSlots",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "ThankYouConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "RetroactiveCheckIns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "PreRegistrations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "MagicCheckInSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "Lectures",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "LectureFeedbacks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "IssuedCertificates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "FormSubmissions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "CheckIns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "CertificateConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "Banners",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "Attendees",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            // Re-enable foreign key checks
            migrationBuilder.Sql("PRAGMA foreign_keys = ON;");

            migrationBuilder.CreateIndex(
                name: "IX_Volunteers_EventId",
                table: "Volunteers",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerCheckIns_EventId",
                table: "VolunteerCheckIns",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_EventId",
                table: "TimeSlots",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_ThankYouConfigs_EventId",
                table: "ThankYouConfigs",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RetroactiveCheckIns_EventId",
                table: "RetroactiveCheckIns",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_PreRegistrations_EventId",
                table: "PreRegistrations",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_MagicCheckInSessions_EventId",
                table: "MagicCheckInSessions",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Lectures_EventId",
                table: "Lectures",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_LectureFeedbacks_EventId",
                table: "LectureFeedbacks",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_IssuedCertificates_EventId",
                table: "IssuedCertificates",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_EventId",
                table: "FormSubmissions",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_EventId",
                table: "CheckIns",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateConfigs_EventId",
                table: "CertificateConfigs",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Banners_EventId",
                table: "Banners",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendees_EventId_Email",
                table: "Attendees",
                columns: new[] { "EventId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_Slug",
                table: "Events",
                column: "Slug",
                unique: true);

            // Temporarily disable foreign keys again before adding FK constraints
            // to avoid issues with existing data referencing the default event
            migrationBuilder.Sql("PRAGMA foreign_keys = OFF;");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendees_Events_EventId",
                table: "Attendees",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Banners_Events_EventId",
                table: "Banners",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CertificateConfigs_Events_EventId",
                table: "CertificateConfigs",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CheckIns_Events_EventId",
                table: "CheckIns",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FormSubmissions_Events_EventId",
                table: "FormSubmissions",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IssuedCertificates_Events_EventId",
                table: "IssuedCertificates",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LectureFeedbacks_Events_EventId",
                table: "LectureFeedbacks",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Lectures_Events_EventId",
                table: "Lectures",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MagicCheckInSessions_Events_EventId",
                table: "MagicCheckInSessions",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PreRegistrations_Events_EventId",
                table: "PreRegistrations",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RetroactiveCheckIns_Events_EventId",
                table: "RetroactiveCheckIns",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ThankYouConfigs_Events_EventId",
                table: "ThankYouConfigs",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TimeSlots_Events_EventId",
                table: "TimeSlots",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VolunteerCheckIns_Events_EventId",
                table: "VolunteerCheckIns",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Volunteers_Events_EventId",
                table: "Volunteers",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("PRAGMA foreign_keys = ON;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendees_Events_EventId",
                table: "Attendees");

            migrationBuilder.DropForeignKey(
                name: "FK_Banners_Events_EventId",
                table: "Banners");

            migrationBuilder.DropForeignKey(
                name: "FK_CertificateConfigs_Events_EventId",
                table: "CertificateConfigs");

            migrationBuilder.DropForeignKey(
                name: "FK_CheckIns_Events_EventId",
                table: "CheckIns");

            migrationBuilder.DropForeignKey(
                name: "FK_FormSubmissions_Events_EventId",
                table: "FormSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_IssuedCertificates_Events_EventId",
                table: "IssuedCertificates");

            migrationBuilder.DropForeignKey(
                name: "FK_LectureFeedbacks_Events_EventId",
                table: "LectureFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_Lectures_Events_EventId",
                table: "Lectures");

            migrationBuilder.DropForeignKey(
                name: "FK_MagicCheckInSessions_Events_EventId",
                table: "MagicCheckInSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_PreRegistrations_Events_EventId",
                table: "PreRegistrations");

            migrationBuilder.DropForeignKey(
                name: "FK_RetroactiveCheckIns_Events_EventId",
                table: "RetroactiveCheckIns");

            migrationBuilder.DropForeignKey(
                name: "FK_ThankYouConfigs_Events_EventId",
                table: "ThankYouConfigs");

            migrationBuilder.DropForeignKey(
                name: "FK_TimeSlots_Events_EventId",
                table: "TimeSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_VolunteerCheckIns_Events_EventId",
                table: "VolunteerCheckIns");

            migrationBuilder.DropForeignKey(
                name: "FK_Volunteers_Events_EventId",
                table: "Volunteers");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Volunteers_EventId",
                table: "Volunteers");

            migrationBuilder.DropIndex(
                name: "IX_VolunteerCheckIns_EventId",
                table: "VolunteerCheckIns");

            migrationBuilder.DropIndex(
                name: "IX_TimeSlots_EventId",
                table: "TimeSlots");

            migrationBuilder.DropIndex(
                name: "IX_ThankYouConfigs_EventId",
                table: "ThankYouConfigs");

            migrationBuilder.DropIndex(
                name: "IX_RetroactiveCheckIns_EventId",
                table: "RetroactiveCheckIns");

            migrationBuilder.DropIndex(
                name: "IX_PreRegistrations_EventId",
                table: "PreRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_MagicCheckInSessions_EventId",
                table: "MagicCheckInSessions");

            migrationBuilder.DropIndex(
                name: "IX_Lectures_EventId",
                table: "Lectures");

            migrationBuilder.DropIndex(
                name: "IX_LectureFeedbacks_EventId",
                table: "LectureFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_IssuedCertificates_EventId",
                table: "IssuedCertificates");

            migrationBuilder.DropIndex(
                name: "IX_FormSubmissions_EventId",
                table: "FormSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_CheckIns_EventId",
                table: "CheckIns");

            migrationBuilder.DropIndex(
                name: "IX_CertificateConfigs_EventId",
                table: "CertificateConfigs");

            migrationBuilder.DropIndex(
                name: "IX_Banners_EventId",
                table: "Banners");

            migrationBuilder.DropIndex(
                name: "IX_Attendees_EventId_Email",
                table: "Attendees");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "VolunteerCheckIns");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "TimeSlots");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "ThankYouConfigs");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "RetroactiveCheckIns");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "PreRegistrations");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "MagicCheckInSessions");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Lectures");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "LectureFeedbacks");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "IssuedCertificates");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "FormSubmissions");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "CheckIns");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "CertificateConfigs");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Attendees");
        }
    }
}
