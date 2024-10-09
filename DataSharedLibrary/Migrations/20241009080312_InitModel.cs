using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataSharedLibrary.Migrations
{
    /// <inheritdoc />
    public partial class InitModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChapterDetailContents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: true),
                    BookId = table.Column<string>(type: "TEXT", nullable: true),
                    ChapterId = table.Column<string>(type: "TEXT", nullable: true),
                    Content = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterDetailContents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NovelContents",
                columns: table => new
                {
                    BookId = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    ImageBase64 = table.Column<string>(type: "TEXT", nullable: true),
                    BookName = table.Column<string>(type: "TEXT", nullable: true),
                    Author = table.Column<string>(type: "TEXT", nullable: true),
                    URL = table.Column<string>(type: "TEXT", nullable: true),
                    MaxChapterCount = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NovelContents", x => x.BookId);
                });

            migrationBuilder.CreateTable(
                name: "ChapterContents",
                columns: table => new
                {
                    ChapterId = table.Column<string>(type: "TEXT", nullable: false),
                    BookId = table.Column<string>(type: "TEXT", nullable: true),
                    IndexChapter = table.Column<int>(type: "INTEGER", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    URL = table.Column<string>(type: "TEXT", nullable: true),
                    Content = table.Column<string>(type: "TEXT", nullable: true),
                    NovelContentBookId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterContents", x => x.ChapterId);
                    table.ForeignKey(
                        name: "FK_ChapterContents_NovelContents_NovelContentBookId",
                        column: x => x.NovelContentBookId,
                        principalTable: "NovelContents",
                        principalColumn: "BookId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChapterContents_NovelContentBookId",
                table: "ChapterContents",
                column: "NovelContentBookId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChapterContents");

            migrationBuilder.DropTable(
                name: "ChapterDetailContents");

            migrationBuilder.DropTable(
                name: "NovelContents");
        }
    }
}
