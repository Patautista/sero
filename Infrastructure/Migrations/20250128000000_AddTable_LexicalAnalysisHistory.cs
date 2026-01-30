using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTable_LexicalAnalysisHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LexicalAnalysisHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LexicalAnalysisCacheId = table.Column<int>(type: "INTEGER", nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LexicalAnalysisHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LexicalAnalysisHistory_LexicalAnalyses_LexicalAnalysisCacheId",
                        column: x => x.LexicalAnalysisCacheId,
                        principalTable: "LexicalAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LexicalAnalysisHistory_AnalyzedAt",
                table: "LexicalAnalysisHistory",
                column: "AnalyzedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LexicalAnalysisHistory_LexicalAnalysisCacheId",
                table: "LexicalAnalysisHistory",
                column: "LexicalAnalysisCacheId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LexicalAnalysisHistory");
        }
    }
}
