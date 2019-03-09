using Microsoft.EntityFrameworkCore.Migrations;

namespace LocusPocusBot.Migrations
{
    public partial class AddCounts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "MesianoCount",
                table: "Chats",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "PovoCount",
                table: "Chats",
                nullable: false,
                defaultValue: 0u);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MesianoCount",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "PovoCount",
                table: "Chats");
        }
    }
}
