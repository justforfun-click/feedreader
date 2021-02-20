using Microsoft.EntityFrameworkCore.Migrations;

namespace FeedReader.ServerCore.Migrations
{
    public partial class AddGroupToUserFeedTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FeedId",
                table: "UserFeeds",
                type: "character varying(64)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserFeeds",
                type: "character varying(32)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Group",
                table: "UserFeeds",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserFeeds_Feeds_FeedId",
                table: "UserFeeds",
                column: "FeedId",
                principalTable: "Feeds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserFeeds_Users_UserId",
                table: "UserFeeds",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserFeeds_Feeds_FeedId",
                table: "UserFeeds");

            migrationBuilder.DropForeignKey(
                name: "FK_UserFeeds_Users_UserId",
                table: "UserFeeds");

            migrationBuilder.DropColumn(
                name: "Group",
                table: "UserFeeds");

            migrationBuilder.AlterColumn<string>(
                name: "FeedId",
                table: "UserFeeds",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserFeeds",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)");
        }
    }
}
