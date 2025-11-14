using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddColumn_CreatedIn_ToTable_Cards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_Decks_DeckTableId",
                table: "Cards");

            migrationBuilder.DropIndex(
                name: "IX_Cards_DeckTableId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "DeckTableId",
                table: "Cards");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedIn",
                table: "Cards",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Cards_DeckId",
                table: "Cards",
                column: "DeckId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_Decks_DeckId",
                table: "Cards",
                column: "DeckId",
                principalTable: "Decks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_Decks_DeckId",
                table: "Cards");

            migrationBuilder.DropIndex(
                name: "IX_Cards_DeckId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "CreatedIn",
                table: "Cards");

            migrationBuilder.AddColumn<int>(
                name: "DeckTableId",
                table: "Cards",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cards_DeckTableId",
                table: "Cards",
                column: "DeckTableId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_Decks_DeckTableId",
                table: "Cards",
                column: "DeckTableId",
                principalTable: "Decks",
                principalColumn: "Id");
        }
    }
}
