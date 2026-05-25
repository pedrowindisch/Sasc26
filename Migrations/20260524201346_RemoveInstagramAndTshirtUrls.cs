using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sasc26.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInstagramAndTshirtUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migrate InstagramUrl and TshirtPresaleUrl into PostCheckinButtonsJson
            // Step 1: normalize empty PostCheckinButtonsJson
            migrationBuilder.Sql("""
                UPDATE Events SET PostCheckinButtonsJson = '[]'
                WHERE PostCheckinButtonsJson IS NULL OR PostCheckinButtonsJson = '';
            """);

            // Step 2: append Instagram button if InstagramUrl is non-empty
            migrationBuilder.Sql("""
                UPDATE Events
                SET PostCheckinButtonsJson = json_insert(
                    PostCheckinButtonsJson,
                    '$[#]',
                    json_object('label', '@dascfurb no Instagram', 'url', InstagramUrl, 'enabled', 1)
                )
                WHERE InstagramUrl IS NOT NULL AND InstagramUrl != '';
            """);

            // Step 3: append T-shirt presale button if TshirtPresaleUrl is non-empty
            migrationBuilder.Sql("""
                UPDATE Events
                SET PostCheckinButtonsJson = json_insert(
                    PostCheckinButtonsJson,
                    '$[#]',
                    json_object('label', 'Pre-venda de Camisetas', 'url', TshirtPresaleUrl, 'enabled', 1)
                )
                WHERE TshirtPresaleUrl IS NOT NULL AND TshirtPresaleUrl != '';
            """);

            migrationBuilder.DropColumn(
                name: "InstagramUrl",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "TshirtPresaleUrl",
                table: "Events");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InstagramUrl",
                table: "Events",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TshirtPresaleUrl",
                table: "Events",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // Restore InstagramUrl from PostCheckinButtonsJson
            migrationBuilder.Sql("""
                UPDATE Events SET InstagramUrl = json_extract(PostCheckinButtonsJson, '$[0].url')
                WHERE json_extract(PostCheckinButtonsJson, '$[0].label') LIKE '%Instagram%'
                   OR json_extract(PostCheckinButtonsJson, '$[0].label') LIKE '%instagram%';
            """);

            // Restore TshirtPresaleUrl from PostCheckinButtonsJson
            migrationBuilder.Sql("""
                UPDATE Events SET TshirtPresaleUrl = json_extract(PostCheckinButtonsJson, '$[0].url')
                WHERE json_extract(PostCheckinButtonsJson, '$[0].label') LIKE '%camiseta%'
                   OR json_extract(PostCheckinButtonsJson, '$[1].label') LIKE '%camiseta%';
            """);
        }
    }
}
