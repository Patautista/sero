using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddColumn_DifficultyLevel_ToTable_Meaning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tags_Cards_CardId",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Tags_CardId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "CardId",
                table: "Tags");

            migrationBuilder.AddColumn<string>(
                name: "DifficultyLevel",
                table: "Meanings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UserCardStates_CardId",
                table: "UserCardStates",
                column: "CardId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserCardStates_Cards_CardId",
                table: "UserCardStates",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCardStates_Cards_CardId",
                table: "UserCardStates");

            migrationBuilder.DropIndex(
                name: "IX_UserCardStates_CardId",
                table: "UserCardStates");

            migrationBuilder.DropColumn(
                name: "DifficultyLevel",
                table: "Meanings");

            migrationBuilder.AddColumn<int>(
                name: "CardId",
                table: "Tags",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_CardId",
                table: "Tags",
                column: "CardId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_Cards_CardId",
                table: "Tags",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id");
        }
    }
}
