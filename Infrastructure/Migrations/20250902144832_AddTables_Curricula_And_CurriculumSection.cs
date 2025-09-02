using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTables_Curricula_And_CurriculumSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCardStates_Cards_CardId",
                table: "UserCardStates");

            migrationBuilder.DropIndex(
                name: "IX_UserCardStates_CardId",
                table: "UserCardStates");

            migrationBuilder.AddColumn<int>(
                name: "UserCardStateId",
                table: "Cards",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Curricula",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Curricula", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CurriculumId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    PropertySpecificationJson = table.Column<string>(type: "TEXT", nullable: false),
                    RequiredExp = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumSections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_UserCardStateId",
                table: "Cards",
                column: "UserCardStateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_UserCardStates_UserCardStateId",
                table: "Cards",
                column: "UserCardStateId",
                principalTable: "UserCardStates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_UserCardStates_UserCardStateId",
                table: "Cards");

            migrationBuilder.DropTable(
                name: "Curricula");

            migrationBuilder.DropTable(
                name: "CurriculumSections");

            migrationBuilder.DropIndex(
                name: "IX_Cards_UserCardStateId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "UserCardStateId",
                table: "Cards");

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
    }
}
