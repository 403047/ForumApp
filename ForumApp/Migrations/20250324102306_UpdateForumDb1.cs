using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForumApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdateForumDb1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_Posts_PostId1",
                table: "Ratings");

            migrationBuilder.DropIndex(
                name: "IX_Ratings_PostId1",
                table: "Ratings");

            migrationBuilder.DropColumn(
                name: "PostId1",
                table: "Ratings");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "TotalRating",
                table: "Posts");

            migrationBuilder.CreateTable(
                name: "PostPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostPrices_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostPrices_PostId",
                table: "PostPrices",
                column: "PostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostPrices");

            migrationBuilder.AddColumn<int>(
                name: "PostId1",
                table: "Ratings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Posts",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalRating",
                table: "Posts",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_PostId1",
                table: "Ratings",
                column: "PostId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_Posts_PostId1",
                table: "Ratings",
                column: "PostId1",
                principalTable: "Posts",
                principalColumn: "Id");
        }
    }
}
