using Microsoft.EntityFrameworkCore.Migrations;

namespace FeedReader.ServerCore.Migrations
{
    public partial class AddFKToUserFavoriteTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FeedItemId",
                table: "UserFavorites",
                type: "character varying(64)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFavorites_FeedItemId",
                table: "UserFavorites",
                column: "FeedItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserFavorites_FeedItems_FeedItemId",
                table: "UserFavorites",
                column: "FeedItemId",
                principalTable: "FeedItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserFavorites_FeedItems_FeedItemId",
                table: "UserFavorites");

            migrationBuilder.DropIndex(
                name: "IX_UserFavorites_FeedItemId",
                table: "UserFavorites");

            migrationBuilder.AlterColumn<string>(
                name: "FeedItemId",
                table: "UserFavorites",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldNullable: true);
        }
    }
}
