using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenamedTable_Meaning_To_Meanings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_Meaning_MeaningId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_MeaningTags_Meaning_MeaningId",
                table: "MeaningTags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Meaning",
                table: "Meaning");

            migrationBuilder.RenameTable(
                name: "Meaning",
                newName: "Meanings");

            migrationBuilder.AddColumn<int>(
                name: "Meanings",
                table: "Sentences",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Meanings",
                table: "Meanings",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Sentences_Meanings",
                table: "Sentences",
                column: "Meanings");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_Meanings_MeaningId",
                table: "Cards",
                column: "MeaningId",
                principalTable: "Meanings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MeaningTags_Meanings_MeaningId",
                table: "MeaningTags",
                column: "MeaningId",
                principalTable: "Meanings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sentences_Meanings_Meanings",
                table: "Sentences",
                column: "Meanings",
                principalTable: "Meanings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_Meanings_MeaningId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_MeaningTags_Meanings_MeaningId",
                table: "MeaningTags");

            migrationBuilder.DropForeignKey(
                name: "FK_Sentences_Meanings_Meanings",
                table: "Sentences");

            migrationBuilder.DropIndex(
                name: "IX_Sentences_Meanings",
                table: "Sentences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Meanings",
                table: "Meanings");

            migrationBuilder.DropColumn(
                name: "Meanings",
                table: "Sentences");

            migrationBuilder.RenameTable(
                name: "Meanings",
                newName: "Meaning");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Meaning",
                table: "Meaning",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_Meaning_MeaningId",
                table: "Cards",
                column: "MeaningId",
                principalTable: "Meaning",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MeaningTags_Meaning_MeaningId",
                table: "MeaningTags",
                column: "MeaningId",
                principalTable: "Meaning",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
