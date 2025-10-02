using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeckTable1 : Migration
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
