using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PapirFly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_Category",
                table: "Articles",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_Name",
                table: "Articles",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Articles_Category",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "IX_Articles_Name",
                table: "Articles");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
