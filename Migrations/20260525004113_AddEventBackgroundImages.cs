using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sasc26.Migrations
{
    /// <inheritdoc />
    public partial class AddEventBackgroundImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "BackgroundImageDesktop",
                table: "Events",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BackgroundImageDesktopContentType",
                table: "Events",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "BackgroundImageMobile",
                table: "Events",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BackgroundImageMobileContentType",
                table: "Events",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackgroundImageDesktop",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "BackgroundImageDesktopContentType",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "BackgroundImageMobile",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "BackgroundImageMobileContentType",
                table: "Events");
        }
    }
}
