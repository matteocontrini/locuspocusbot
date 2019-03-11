using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LocusPocusBot.Migrations
{
    public partial class CreateLogsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MesianoCount",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "PovoCount",
                table: "Chats");

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChatId = table.Column<long>(nullable: false),
                    At = table.Column<DateTime>(nullable: false),
                    RequestType = table.Column<int>(nullable: false),
                    Department = table.Column<string>(nullable: false),
                    AvailabilityType = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Logs_Chats_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Logs_ChatId",
                table: "Logs",
                column: "ChatId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Logs");

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
    }
}
