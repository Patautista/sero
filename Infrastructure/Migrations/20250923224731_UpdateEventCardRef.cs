using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEventCardRef : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sentences_Meanings_Meanings",
                table: "Sentences");

            migrationBuilder.DropIndex(
                name: "IX_Sentences_Meanings",
                table: "Sentences");

            migrationBuilder.DropColumn(
                name: "Meanings",
                table: "Sentences");

            migrationBuilder.AddColumn<int>(
                name: "CardTableId",
                table: "Events",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sentences_MeaningId",
                table: "Sentences",
                column: "MeaningId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_CardTableId",
                table: "Events",
                column: "CardTableId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Cards_CardTableId",
                table: "Events",
                column: "CardTableId",
                principalTable: "Cards",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sentences_Meanings_MeaningId",
                table: "Sentences",
                column: "MeaningId",
                principalTable: "Meanings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Cards_CardTableId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Sentences_Meanings_MeaningId",
                table: "Sentences");

            migrationBuilder.DropIndex(
                name: "IX_Sentences_MeaningId",
                table: "Sentences");

            migrationBuilder.DropIndex(
                name: "IX_Events_CardTableId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CardTableId",
                table: "Events");

            migrationBuilder.AddColumn<int>(
                name: "Meanings",
                table: "Sentences",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sentences_Meanings",
                table: "Sentences",
                column: "Meanings");

            migrationBuilder.AddForeignKey(
                name: "FK_Sentences_Meanings_Meanings",
                table: "Sentences",
                column: "Meanings",
                principalTable: "Meanings",
                principalColumn: "Id");
        }
    }
}
