using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sasc26.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificateCustomization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "BackgroundImage",
                table: "CertificateConfigs",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BackgroundImageContentType",
                table: "CertificateConfigs",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BodyColor",
                table: "CertificateConfigs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BorderColor",
                table: "CertificateConfigs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TitleColor",
                table: "CertificateConfigs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackgroundImage",
                table: "CertificateConfigs");

            migrationBuilder.DropColumn(
                name: "BackgroundImageContentType",
                table: "CertificateConfigs");

            migrationBuilder.DropColumn(
                name: "BodyColor",
                table: "CertificateConfigs");

            migrationBuilder.DropColumn(
                name: "BorderColor",
                table: "CertificateConfigs");

            migrationBuilder.DropColumn(
                name: "TitleColor",
                table: "CertificateConfigs");
        }
    }
}
