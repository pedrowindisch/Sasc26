using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sasc26.Migrations
{
    /// <inheritdoc />
    public partial class AddEventCourses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventCourses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NumberOfSemesters = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventCourses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventCourses_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventCourses_EventId_Name",
                table: "EventCourses",
                columns: new[] { "EventId", "Name" },
                unique: true);

            // Seed default courses for existing events
            migrationBuilder.Sql(@"
                INSERT INTO EventCourses (EventId, Name, NumberOfSemesters)
                SELECT e.Id, 'Ciência da Computação', 8 FROM Events e
                WHERE NOT EXISTS (SELECT 1 FROM EventCourses ec WHERE ec.EventId = e.Id AND ec.Name = 'Ciência da Computação');
                INSERT INTO EventCourses (EventId, Name, NumberOfSemesters)
                SELECT e.Id, 'Ciência de Dados', 8 FROM Events e
                WHERE NOT EXISTS (SELECT 1 FROM EventCourses ec WHERE ec.EventId = e.Id AND ec.Name = 'Ciência de Dados');
                INSERT INTO EventCourses (EventId, Name, NumberOfSemesters)
                SELECT e.Id, 'Sistemas de Informação', 8 FROM Events e
                WHERE NOT EXISTS (SELECT 1 FROM EventCourses ec WHERE ec.EventId = e.Id AND ec.Name = 'Sistemas de Informação');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventCourses");
        }
    }
}
