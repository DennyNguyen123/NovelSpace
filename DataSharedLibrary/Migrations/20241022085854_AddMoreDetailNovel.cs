using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataSharedLibrary.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreDetailNovel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "NovelContents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortDesc",
                table: "NovelContents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "NovelContents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "NovelContents",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "NovelContents");

            migrationBuilder.DropColumn(
                name: "ShortDesc",
                table: "NovelContents");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "NovelContents");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "NovelContents");
        }
    }
}
