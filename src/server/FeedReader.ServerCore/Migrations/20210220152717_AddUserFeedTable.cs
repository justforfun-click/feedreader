using Microsoft.EntityFrameworkCore.Migrations;

namespace FeedReader.ServerCore.Migrations
{
    public partial class AddUserFeedTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserFeeds",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    FeedId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFeeds", x => new { x.UserId, x.FeedId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserFeeds_FeedId",
                table: "UserFeeds",
                column: "FeedId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFeeds_UserId",
                table: "UserFeeds",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserFeeds");
        }
    }
}
