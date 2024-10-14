using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataSharedLibrary.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChapterContents_NovelContents_NovelContentBookId",
                table: "ChapterContents");

            migrationBuilder.DropIndex(
                name: "IX_ChapterContents_NovelContentBookId",
                table: "ChapterContents");

            migrationBuilder.DropColumn(
                name: "NovelContentBookId",
                table: "ChapterContents");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterDetailContents_ChapterId",
                table: "ChapterDetailContents",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterContents_BookId",
                table: "ChapterContents",
                column: "BookId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChapterContents_NovelContents_BookId",
                table: "ChapterContents",
                column: "BookId",
                principalTable: "NovelContents",
                principalColumn: "BookId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChapterDetailContents_ChapterContents_ChapterId",
                table: "ChapterDetailContents",
                column: "ChapterId",
                principalTable: "ChapterContents",
                principalColumn: "ChapterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChapterContents_NovelContents_BookId",
                table: "ChapterContents");

            migrationBuilder.DropForeignKey(
                name: "FK_ChapterDetailContents_ChapterContents_ChapterId",
                table: "ChapterDetailContents");

            migrationBuilder.DropIndex(
                name: "IX_ChapterDetailContents_ChapterId",
                table: "ChapterDetailContents");

            migrationBuilder.DropIndex(
                name: "IX_ChapterContents_BookId",
                table: "ChapterContents");

            migrationBuilder.AddColumn<string>(
                name: "NovelContentBookId",
                table: "ChapterContents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChapterContents_NovelContentBookId",
                table: "ChapterContents",
                column: "NovelContentBookId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChapterContents_NovelContents_NovelContentBookId",
                table: "ChapterContents",
                column: "NovelContentBookId",
                principalTable: "NovelContents",
                principalColumn: "BookId");
        }
    }
}
