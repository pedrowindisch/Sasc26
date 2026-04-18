using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sasc26.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificateAndCreditHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreditHours",
                table: "TimeSlots",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CertificateConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TemplateMessage = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IssuedCertificates",
                columns: table => new
                {
                    ValidationCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Course = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Phase = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TotalHours = table.Column<decimal>(type: "TEXT", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssuedCertificates", x => x.ValidationCode);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssuedCertificates_Email",
                table: "IssuedCertificates",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificateConfigs");

            migrationBuilder.DropTable(
                name: "IssuedCertificates");

            migrationBuilder.DropColumn(
                name: "CreditHours",
                table: "TimeSlots");
        }
    }
}
