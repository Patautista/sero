using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EventsCardRel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Cards_CardTableId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_CardTableId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CardTableId",
                table: "Events");

            migrationBuilder.CreateIndex(
                name: "IX_Events_CardId",
                table: "Events",
                column: "CardId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Cards_CardId",
                table: "Events",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Cards_CardId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_CardId",
                table: "Events");

            migrationBuilder.AddColumn<int>(
                name: "CardTableId",
                table: "Events",
                type: "INTEGER",
                nullable: true);

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
        }
    }
}
