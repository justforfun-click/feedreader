using Microsoft.EntityFrameworkCore.Migrations;

namespace FeedReader.ServerCore.Migrations
{
    public partial class RenameUserFavoritesToUserFeedItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserFavorites_FeedItems_FeedItemId",
                table: "UserFavorites");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserFavorites",
                table: "UserFavorites");

            migrationBuilder.RenameTable(
                name: "UserFavorites",
                newName: "UserFeedItems");

            migrationBuilder.RenameIndex(
                name: "IX_UserFavorites_UserId",
                table: "UserFeedItems",
                newName: "IX_UserFeedItems_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserFavorites_FeedItemId",
                table: "UserFeedItems",
                newName: "IX_UserFeedItems_FeedItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserFeedItems",
                table: "UserFeedItems",
                columns: new[] { "UserId", "FavoriteItemIdHash" });

            migrationBuilder.AddForeignKey(
                name: "FK_UserFeedItems_FeedItems_FeedItemId",
                table: "UserFeedItems",
                column: "FeedItemId",
                principalTable: "FeedItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserFeedItems_FeedItems_FeedItemId",
                table: "UserFeedItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserFeedItems",
                table: "UserFeedItems");

            migrationBuilder.RenameTable(
                name: "UserFeedItems",
                newName: "UserFavorites");

            migrationBuilder.RenameIndex(
                name: "IX_UserFeedItems_UserId",
                table: "UserFavorites",
                newName: "IX_UserFavorites_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserFeedItems_FeedItemId",
                table: "UserFavorites",
                newName: "IX_UserFavorites_FeedItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserFavorites",
                table: "UserFavorites",
                columns: new[] { "UserId", "FavoriteItemIdHash" });

            migrationBuilder.AddForeignKey(
                name: "FK_UserFavorites_FeedItems_FeedItemId",
                table: "UserFavorites",
                column: "FeedItemId",
                principalTable: "FeedItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
