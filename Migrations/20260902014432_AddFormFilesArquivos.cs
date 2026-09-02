using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sasc26.Migrations
{
    /// <inheritdoc />
    public partial class AddFormFilesArquivos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FormFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    PreRegistrationSubmissionId = table.Column<int>(type: "INTEGER", nullable: true),
                    FormSubmissionId = table.Column<int>(type: "INTEGER", nullable: true),
                    FieldLabel = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OriginalSize = table.Column<long>(type: "INTEGER", nullable: false),
                    CompressedData = table.Column<byte[]>(type: "BLOB", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormFiles_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormFiles_FormSubmissions_FormSubmissionId",
                        column: x => x.FormSubmissionId,
                        principalTable: "FormSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormFiles_PreRegistrationFormSubmissions_PreRegistrationSubmissionId",
                        column: x => x.PreRegistrationSubmissionId,
                        principalTable: "PreRegistrationFormSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormFiles_EventId",
                table: "FormFiles",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFiles_FormSubmissionId",
                table: "FormFiles",
                column: "FormSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFiles_PreRegistrationSubmissionId",
                table: "FormFiles",
                column: "PreRegistrationSubmissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FormFiles");
        }
    }
}
