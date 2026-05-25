using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sasc26.Migrations
{
    /// <inheritdoc />
    public partial class ChangeAttendeePkToComposite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("PRAGMA foreign_keys = OFF;");

            // Drop old FK constraints (metadata-only for SQLite)
            migrationBuilder.DropForeignKey(
                name: "FK_CheckIns_Attendees_AttendeeEmail",
                table: "CheckIns");

            migrationBuilder.DropForeignKey(
                name: "FK_RetroactiveCheckIns_Attendees_AttendeeEmail",
                table: "RetroactiveCheckIns");

            // Drop old indexes on CheckIns and RetroactiveCheckIns
            migrationBuilder.DropIndex(
                name: "IX_RetroactiveCheckIns_AttendeeEmail",
                table: "RetroactiveCheckIns");

            migrationBuilder.DropIndex(
                name: "IX_RetroactiveCheckIns_EventId",
                table: "RetroactiveCheckIns");

            migrationBuilder.DropIndex(
                name: "IX_CheckIns_AttendeeEmail",
                table: "CheckIns");

            migrationBuilder.DropIndex(
                name: "IX_CheckIns_EventId",
                table: "CheckIns");

            // Keep IX_Attendees_EventId_Email (unique index) - don't drop it.

            // Recreate Attendees with composite PK (EventId, Email)
            migrationBuilder.Sql(@"
                CREATE TABLE ""ef_temp_Attendees"" (
                    ""Email""    TEXT NOT NULL,
                    ""FullName"" TEXT NOT NULL,
                    ""Course""   TEXT NOT NULL,
                    ""Shift""    TEXT NOT NULL,
                    ""Phase""    INTEGER NOT NULL,
                    ""EventId""  INTEGER NOT NULL,
                    CONSTRAINT ""PK_Attendees"" PRIMARY KEY (""EventId"", ""Email""),
                    CONSTRAINT ""FK_Attendees_Events_EventId"" FOREIGN KEY (""EventId"") REFERENCES ""Events"" (""Id"") ON DELETE RESTRICT
                );
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""ef_temp_Attendees"" (""Email"", ""FullName"", ""Course"", ""Shift"", ""Phase"", ""EventId"")
                SELECT ""Email"", ""FullName"", ""Course"", ""Shift"", ""Phase"", ""EventId""
                FROM ""Attendees"";
            ");

            migrationBuilder.Sql(@"DROP TABLE ""Attendees"";");
            migrationBuilder.Sql(@"ALTER TABLE ""ef_temp_Attendees"" RENAME TO ""Attendees"";");

            // Recreate CheckIns with new FK (EventId, AttendeeEmail) -> Attendees(EventId, Email)
            migrationBuilder.Sql(@"
                CREATE TABLE ""ef_temp_CheckIns"" (
                    ""Id""             TEXT    NOT NULL CONSTRAINT ""PK_CheckIns"" PRIMARY KEY,
                    ""AttendeeEmail""  TEXT    NOT NULL,
                    ""CreatedAt""      TEXT    NOT NULL,
                    ""EventId""        INTEGER NOT NULL,
                    ""ExpiresAt""      TEXT    NOT NULL,
                    ""LectureId""      INTEGER NULL,
                    ""OtpCode""        TEXT    NOT NULL,
                    ""SesFallback""    INTEGER NOT NULL,
                    ""Status""         INTEGER NOT NULL,
                    ""VerifiedAt""     TEXT    NULL,
                    CONSTRAINT ""FK_CheckIns_Attendees_EventId_AttendeeEmail""
                        FOREIGN KEY (""EventId"", ""AttendeeEmail"")
                        REFERENCES ""Attendees"" (""EventId"", ""Email"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_CheckIns_Events_EventId""
                        FOREIGN KEY (""EventId"") REFERENCES ""Events"" (""Id"") ON DELETE RESTRICT,
                    CONSTRAINT ""FK_CheckIns_Lectures_LectureId""
                        FOREIGN KEY (""LectureId"") REFERENCES ""Lectures"" (""Id"") ON DELETE SET NULL
                );
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""ef_temp_CheckIns"" (""Id"", ""AttendeeEmail"", ""CreatedAt"", ""EventId"", ""ExpiresAt"", ""LectureId"", ""OtpCode"", ""SesFallback"", ""Status"", ""VerifiedAt"")
                SELECT ""Id"", ""AttendeeEmail"", ""CreatedAt"", ""EventId"", ""ExpiresAt"", ""LectureId"", ""OtpCode"", ""SesFallback"", ""Status"", ""VerifiedAt""
                FROM ""CheckIns"";
            ");

            migrationBuilder.Sql(@"DROP TABLE ""CheckIns"";");
            migrationBuilder.Sql(@"ALTER TABLE ""ef_temp_CheckIns"" RENAME TO ""CheckIns"";");

            // Recreate RetroactiveCheckIns with new FK
            migrationBuilder.Sql(@"
                CREATE TABLE ""ef_temp_RetroactiveCheckIns"" (
                    ""Id""             TEXT    NOT NULL CONSTRAINT ""PK_RetroactiveCheckIns"" PRIMARY KEY,
                    ""AttendeeEmail""  TEXT    NOT NULL,
                    ""EventId""        INTEGER NOT NULL,
                    ""Justification""  TEXT    NOT NULL,
                    ""LectureId""      INTEGER NOT NULL,
                    ""RequestedAt""    TEXT    NOT NULL,
                    ""ResolvedAt""     TEXT    NULL,
                    ""Status""         INTEGER NOT NULL,
                    CONSTRAINT ""FK_RetroactiveCheckIns_Attendees_EventId_AttendeeEmail""
                        FOREIGN KEY (""EventId"", ""AttendeeEmail"")
                        REFERENCES ""Attendees"" (""EventId"", ""Email"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_RetroactiveCheckIns_Events_EventId""
                        FOREIGN KEY (""EventId"") REFERENCES ""Events"" (""Id"") ON DELETE RESTRICT,
                    CONSTRAINT ""FK_RetroactiveCheckIns_Lectures_LectureId""
                        FOREIGN KEY (""LectureId"") REFERENCES ""Lectures"" (""Id"") ON DELETE CASCADE
                );
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""ef_temp_RetroactiveCheckIns"" (""Id"", ""AttendeeEmail"", ""EventId"", ""Justification"", ""LectureId"", ""RequestedAt"", ""ResolvedAt"", ""Status"")
                SELECT ""Id"", ""AttendeeEmail"", ""EventId"", ""Justification"", ""LectureId"", ""RequestedAt"", ""ResolvedAt"", ""Status""
                FROM ""RetroactiveCheckIns"";
            ");

            migrationBuilder.Sql(@"DROP TABLE ""RetroactiveCheckIns"";");
            migrationBuilder.Sql(@"ALTER TABLE ""ef_temp_RetroactiveCheckIns"" RENAME TO ""RetroactiveCheckIns"";");

            // Create new indexes
            migrationBuilder.CreateIndex(
                name: "IX_RetroactiveCheckIns_EventId_AttendeeEmail",
                table: "RetroactiveCheckIns",
                columns: new[] { "EventId", "AttendeeEmail" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_EventId_AttendeeEmail",
                table: "CheckIns",
                columns: new[] { "EventId", "AttendeeEmail" });

            migrationBuilder.Sql("PRAGMA foreign_keys = ON;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("PRAGMA foreign_keys = OFF;");

            migrationBuilder.DropForeignKey(
                name: "FK_CheckIns_Attendees_EventId_AttendeeEmail",
                table: "CheckIns");

            migrationBuilder.DropForeignKey(
                name: "FK_RetroactiveCheckIns_Attendees_EventId_AttendeeEmail",
                table: "RetroactiveCheckIns");

            migrationBuilder.DropIndex(
                name: "IX_RetroactiveCheckIns_EventId_AttendeeEmail",
                table: "RetroactiveCheckIns");

            migrationBuilder.DropIndex(
                name: "IX_CheckIns_EventId_AttendeeEmail",
                table: "CheckIns");

            // Recreate Attendees with Email-only PK
            migrationBuilder.Sql(@"
                CREATE TABLE ""ef_temp_Attendees"" (
                    ""Email""    TEXT NOT NULL CONSTRAINT ""PK_Attendees"" PRIMARY KEY,
                    ""FullName"" TEXT NOT NULL,
                    ""Course""   TEXT NOT NULL,
                    ""Shift""    TEXT NOT NULL,
                    ""Phase""    INTEGER NOT NULL,
                    ""EventId""  INTEGER NOT NULL,
                    CONSTRAINT ""FK_Attendees_Events_EventId"" FOREIGN KEY (""EventId"") REFERENCES ""Events"" (""Id"") ON DELETE RESTRICT
                );
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""ef_temp_Attendees"" (""Email"", ""FullName"", ""Course"", ""Shift"", ""Phase"", ""EventId"")
                SELECT ""Email"", ""FullName"", ""Course"", ""Shift"", ""Phase"", ""EventId""
                FROM ""Attendees"";
            ");

            migrationBuilder.Sql(@"DROP TABLE ""Attendees"";");
            migrationBuilder.Sql(@"ALTER TABLE ""ef_temp_Attendees"" RENAME TO ""Attendees"";");

            // Recreate CheckIns with old FK
            migrationBuilder.Sql(@"
                CREATE TABLE ""ef_temp_CheckIns"" (
                    ""Id""             TEXT    NOT NULL CONSTRAINT ""PK_CheckIns"" PRIMARY KEY,
                    ""AttendeeEmail""  TEXT    NOT NULL,
                    ""CreatedAt""      TEXT    NOT NULL,
                    ""EventId""        INTEGER NOT NULL,
                    ""ExpiresAt""      TEXT    NOT NULL,
                    ""LectureId""      INTEGER NULL,
                    ""OtpCode""        TEXT    NOT NULL,
                    ""SesFallback""    INTEGER NOT NULL,
                    ""Status""         INTEGER NOT NULL,
                    ""VerifiedAt""     TEXT    NULL,
                    CONSTRAINT ""FK_CheckIns_Attendees_AttendeeEmail""
                        FOREIGN KEY (""AttendeeEmail"") REFERENCES ""Attendees"" (""Email"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_CheckIns_Events_EventId""
                        FOREIGN KEY (""EventId"") REFERENCES ""Events"" (""Id"") ON DELETE RESTRICT,
                    CONSTRAINT ""FK_CheckIns_Lectures_LectureId""
                        FOREIGN KEY (""LectureId"") REFERENCES ""Lectures"" (""Id"") ON DELETE SET NULL
                );
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""ef_temp_CheckIns"" (""Id"", ""AttendeeEmail"", ""CreatedAt"", ""EventId"", ""ExpiresAt"", ""LectureId"", ""OtpCode"", ""SesFallback"", ""Status"", ""VerifiedAt"")
                SELECT ""Id"", ""AttendeeEmail"", ""CreatedAt"", ""EventId"", ""ExpiresAt"", ""LectureId"", ""OtpCode"", ""SesFallback"", ""Status"", ""VerifiedAt""
                FROM ""CheckIns"";
            ");

            migrationBuilder.Sql(@"DROP TABLE ""CheckIns"";");
            migrationBuilder.Sql(@"ALTER TABLE ""ef_temp_CheckIns"" RENAME TO ""CheckIns"";");

            // Recreate RetroactiveCheckIns with old FK
            migrationBuilder.Sql(@"
                CREATE TABLE ""ef_temp_RetroactiveCheckIns"" (
                    ""Id""             TEXT    NOT NULL CONSTRAINT ""PK_RetroactiveCheckIns"" PRIMARY KEY,
                    ""AttendeeEmail""  TEXT    NOT NULL,
                    ""EventId""        INTEGER NOT NULL,
                    ""Justification""  TEXT    NOT NULL,
                    ""LectureId""      INTEGER NOT NULL,
                    ""RequestedAt""    TEXT    NOT NULL,
                    ""ResolvedAt""     TEXT    NULL,
                    ""Status""         INTEGER NOT NULL,
                    CONSTRAINT ""FK_RetroactiveCheckIns_Attendees_AttendeeEmail""
                        FOREIGN KEY (""AttendeeEmail"") REFERENCES ""Attendees"" (""Email"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_RetroactiveCheckIns_Events_EventId""
                        FOREIGN KEY (""EventId"") REFERENCES ""Events"" (""Id"") ON DELETE RESTRICT,
                    CONSTRAINT ""FK_RetroactiveCheckIns_Lectures_LectureId""
                        FOREIGN KEY (""LectureId"") REFERENCES ""Lectures"" (""Id"") ON DELETE CASCADE
                );
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""ef_temp_RetroactiveCheckIns"" (""Id"", ""AttendeeEmail"", ""EventId"", ""Justification"", ""LectureId"", ""RequestedAt"", ""ResolvedAt"", ""Status"")
                SELECT ""Id"", ""AttendeeEmail"", ""EventId"", ""Justification"", ""LectureId"", ""RequestedAt"", ""ResolvedAt"", ""Status""
                FROM ""RetroactiveCheckIns"";
            ");

            migrationBuilder.Sql(@"DROP TABLE ""RetroactiveCheckIns"";");
            migrationBuilder.Sql(@"ALTER TABLE ""ef_temp_RetroactiveCheckIns"" RENAME TO ""RetroactiveCheckIns"";");

            migrationBuilder.Sql("PRAGMA foreign_keys = ON;");
        }
    }
}
