using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KokoAnalytics.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsPathsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Paths",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PathUrl = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paths", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Paths_PathUrl",
                table: "Paths",
                column: "PathUrl",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Paths");
        }
    }
}
