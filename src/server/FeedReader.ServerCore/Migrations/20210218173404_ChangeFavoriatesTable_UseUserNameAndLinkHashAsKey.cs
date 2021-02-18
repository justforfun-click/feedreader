using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace FeedReader.ServerCore.Migrations
{
    public partial class ChangeFavoriatesTable_UseUserNameAndLinkHashAsKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserFavorites",
                table: "UserFavorites");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "UserFavorites");

            migrationBuilder.RenameColumn(
                name: "FavoriateItemIdHash",
                table: "UserFavorites",
                newName: "FavoriteItemIdHash");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserFavorites",
                table: "UserFavorites",
                columns: new[] { "UserId", "FavoriteItemIdHash" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserFavorites",
                table: "UserFavorites");

            migrationBuilder.RenameColumn(
                name: "FavoriteItemIdHash",
                table: "UserFavorites",
                newName: "FavoriateItemIdHash");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "UserFavorites",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserFavorites",
                table: "UserFavorites",
                column: "Id");
        }
    }
}
