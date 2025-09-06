using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovedColumns_NativeSentence_And_TargetSentence_FromTable_Cards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_Sentences_NativeSentenceId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_Cards_Sentences_TargetSentenceId",
                table: "Cards");

            migrationBuilder.DropIndex(
                name: "IX_Cards_NativeSentenceId",
                table: "Cards");

            migrationBuilder.DropIndex(
                name: "IX_Cards_TargetSentenceId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "NativeSentenceId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "TargetSentenceId",
                table: "Cards");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NativeSentenceId",
                table: "Cards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TargetSentenceId",
                table: "Cards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Cards_NativeSentenceId",
                table: "Cards",
                column: "NativeSentenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_TargetSentenceId",
                table: "Cards",
                column: "TargetSentenceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_Sentences_NativeSentenceId",
                table: "Cards",
                column: "NativeSentenceId",
                principalTable: "Sentences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_Sentences_TargetSentenceId",
                table: "Cards",
                column: "TargetSentenceId",
                principalTable: "Sentences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
