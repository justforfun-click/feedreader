using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FeedReader.ServerCore.Migrations
{
    public partial class AddUserTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ThirdPartyId = table.Column<string>(type: "text", nullable: true),
                    RegistrationTimeInUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_ThirdPartyId",
                table: "Users",
                column: "ThirdPartyId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
