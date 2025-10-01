using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeckTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Cards_CardTableId",
                table: "Events");

            migrationBuilder.DropTable(
                name: "CurriculumSections");

            migrationBuilder.DropTable(
                name: "Curricula");

            migrationBuilder.DropIndex(
                name: "IX_Events_CardTableId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CardTableId",
                table: "Events");

            migrationBuilder.AddColumn<int>(
                name: "DeckId",
                table: "Cards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeckTableId",
                table: "Cards",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Decks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    TargetLanguage = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Decks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_CardId",
                table: "Events",
                column: "CardId");

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
                name: "FK_Cards_Decks_DeckTableId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_Cards_CardId",
                table: "Events");

            migrationBuilder.DropTable(
                name: "Decks");

            migrationBuilder.DropIndex(
                name: "IX_Events_CardId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Cards_DeckTableId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "DeckId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "DeckTableId",
                table: "Cards");

            migrationBuilder.AddColumn<int>(
                name: "CardTableId",
                table: "Events",
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
                    CurriculumTableId = table.Column<int>(type: "INTEGER", nullable: true),
                    DifficultySpecificationJson = table.Column<string>(type: "TEXT", nullable: false),
                    RequiredExp = table.Column<int>(type: "INTEGER", nullable: false),
                    TagsSpecificationJson = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumSections_Curricula_CurriculumTableId",
                        column: x => x.CurriculumTableId,
                        principalTable: "Curricula",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_CardTableId",
                table: "Events",
                column: "CardTableId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumSections_CurriculumTableId",
                table: "CurriculumSections",
                column: "CurriculumTableId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Cards_CardTableId",
                table: "Events",
                column: "CardTableId",
                principalTable: "Cards",
                principalColumn: "Id");
        }
    }
}
