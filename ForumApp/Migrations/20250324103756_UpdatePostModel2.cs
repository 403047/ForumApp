using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForumApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePostModel2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Content",
                table: "Posts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
