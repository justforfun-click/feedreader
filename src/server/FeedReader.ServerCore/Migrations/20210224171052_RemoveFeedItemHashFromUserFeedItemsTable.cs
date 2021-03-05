using Microsoft.EntityFrameworkCore.Migrations;

namespace FeedReader.ServerCore.Migrations
{
    public partial class RemoveFeedItemHashFromUserFeedItemsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserFeedItems_FeedItems_FeedItemId",
                table: "UserFeedItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserFeedItems",
                table: "UserFeedItems");

            migrationBuilder.DropColumn(
                name: "FavoriteItemIdHash",
                table: "UserFeedItems");

            migrationBuilder.AlterColumn<string>(
                name: "FeedItemId",
                table: "UserFeedItems",
                type: "character varying(64)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserFeedItems",
                table: "UserFeedItems",
                columns: new[] { "UserId", "FeedItemId" });

            migrationBuilder.AddForeignKey(
                name: "FK_UserFeedItems_FeedItems_FeedItemId",
                table: "UserFeedItems",
                column: "FeedItemId",
                principalTable: "FeedItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserFeedItems_FeedItems_FeedItemId",
                table: "UserFeedItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserFeedItems",
                table: "UserFeedItems");

            migrationBuilder.AlterColumn<string>(
                name: "FeedItemId",
                table: "UserFeedItems",
                type: "character varying(64)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)");

            migrationBuilder.AddColumn<string>(
                name: "FavoriteItemIdHash",
                table: "UserFeedItems",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

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
    }
}
