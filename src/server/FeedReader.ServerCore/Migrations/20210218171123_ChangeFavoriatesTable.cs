using Microsoft.EntityFrameworkCore.Migrations;

namespace FeedReader.ServerCore.Migrations
{
    public partial class ChangeFavoriatesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Md5",
                table: "UserFavorites");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserFavorites",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FavoriateItemIdHash",
                table: "UserFavorites",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FavoriateItemIdHash",
                table: "UserFavorites");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserFavorites",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AddColumn<string>(
                name: "Md5",
                table: "UserFavorites",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }
    }
}
