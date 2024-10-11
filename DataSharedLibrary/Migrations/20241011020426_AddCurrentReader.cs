using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataSharedLibrary.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentReader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CurrentReader",
                columns: table => new
                {
                    BookId = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentChapter = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentLine = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentPosition = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentReader", x => x.BookId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurrentReader");
        }
    }
}
