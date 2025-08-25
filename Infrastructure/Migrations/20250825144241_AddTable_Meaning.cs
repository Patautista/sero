using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTable_Meaning : Migration
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

            migrationBuilder.DropForeignKey(
                name: "FK_Tags_Sentences_SentenceId",
                table: "Tags");

            migrationBuilder.DropTable(
                name: "CardTags");

            migrationBuilder.RenameColumn(
                name: "SentenceId",
                table: "Tags",
                newName: "CardId");

            migrationBuilder.RenameIndex(
                name: "IX_Tags_SentenceId",
                table: "Tags",
                newName: "IX_Tags_CardId");

            migrationBuilder.CreateTable(
                name: "Meaning",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meaning", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeaningTags",
                columns: table => new
                {
                    MeaningId = table.Column<int>(type: "INTEGER", nullable: false),
                    TagName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeaningTags", x => new { x.MeaningId, x.TagName });
                    table.ForeignKey(
                        name: "FK_MeaningTags_Meaning_MeaningId",
                        column: x => x.MeaningId,
                        principalTable: "Meaning",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeaningTags_Tags_TagName",
                        column: x => x.TagName,
                        principalTable: "Tags",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_MeaningId",
                table: "Cards",
                column: "MeaningId");

            migrationBuilder.CreateIndex(
                name: "IX_MeaningTags_TagName",
                table: "MeaningTags",
                column: "TagName");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_Meaning_MeaningId",
                table: "Cards",
                column: "MeaningId",
                principalTable: "Meaning",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_Cards_CardId",
                table: "Tags",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_Meaning_MeaningId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_Cards_Sentences_NativeSentenceId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_Cards_Sentences_TargetSentenceId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_Tags_Cards_CardId",
                table: "Tags");

            migrationBuilder.DropTable(
                name: "MeaningTags");

            migrationBuilder.DropTable(
                name: "Meaning");

            migrationBuilder.DropIndex(
                name: "IX_Cards_MeaningId",
                table: "Cards");

            migrationBuilder.RenameColumn(
                name: "CardId",
                table: "Tags",
                newName: "SentenceId");

            migrationBuilder.RenameIndex(
                name: "IX_Tags_CardId",
                table: "Tags",
                newName: "IX_Tags_SentenceId");

            migrationBuilder.CreateTable(
                name: "CardTags",
                columns: table => new
                {
                    CardId = table.Column<int>(type: "INTEGER", nullable: false),
                    TagName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardTags", x => new { x.CardId, x.TagName });
                    table.ForeignKey(
                        name: "FK_CardTags_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CardTags_Tags_TagName",
                        column: x => x.TagName,
                        principalTable: "Tags",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardTags_TagName",
                table: "CardTags",
                column: "TagName");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_Sentences_NativeSentenceId",
                table: "Cards",
                column: "NativeSentenceId",
                principalTable: "Sentences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_Sentences_TargetSentenceId",
                table: "Cards",
                column: "TargetSentenceId",
                principalTable: "Sentences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_Sentences_SentenceId",
                table: "Tags",
                column: "SentenceId",
                principalTable: "Sentences",
                principalColumn: "Id");
        }
    }
}
