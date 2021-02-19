using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FeedReader.ServerCore.Migrations
{
    public partial class FeedsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Feeds",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Uri = table.Column<string>(type: "text", nullable: false),
                    IconUri = table.Column<string>(type: "text", nullable: true),
                    WebSiteUri = table.Column<string>(type: "text", nullable: true),
                    RegistrationTimeInUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTimeInUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feeds", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feeds");
        }
    }
}
