using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sasc26.Migrations
{
    /// <inheritdoc />
    public partial class AddEventWidePreRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "LectureId",
                table: "PreRegistrations",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<bool>(
                name: "IsEventWidePreRegistration",
                table: "Events",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEventWidePreRegistration",
                table: "Events");

            migrationBuilder.AlterColumn<int>(
                name: "LectureId",
                table: "PreRegistrations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
