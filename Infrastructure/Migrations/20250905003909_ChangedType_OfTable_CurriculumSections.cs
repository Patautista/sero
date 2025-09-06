using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangedType_OfTable_CurriculumSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PropertySpecificationJson",
                table: "CurriculumSections",
                newName: "TagsSpecificationJson");

            migrationBuilder.AddColumn<int>(
                name: "CurriculumTableId",
                table: "CurriculumSections",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SentenceSpecificationJson",
                table: "CurriculumSections",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumSections_CurriculumTableId",
                table: "CurriculumSections",
                column: "CurriculumTableId");

            migrationBuilder.AddForeignKey(
                name: "FK_CurriculumSections_Curricula_CurriculumTableId",
                table: "CurriculumSections",
                column: "CurriculumTableId",
                principalTable: "Curricula",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CurriculumSections_Curricula_CurriculumTableId",
                table: "CurriculumSections");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumSections_CurriculumTableId",
                table: "CurriculumSections");

            migrationBuilder.DropColumn(
                name: "CurriculumTableId",
                table: "CurriculumSections");

            migrationBuilder.DropColumn(
                name: "SentenceSpecificationJson",
                table: "CurriculumSections");

            migrationBuilder.RenameColumn(
                name: "TagsSpecificationJson",
                table: "CurriculumSections",
                newName: "PropertySpecificationJson");
        }
    }
}
