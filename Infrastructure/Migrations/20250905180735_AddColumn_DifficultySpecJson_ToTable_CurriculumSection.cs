using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddColumn_DifficultySpecJson_ToTable_CurriculumSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SentenceSpecificationJson",
                table: "CurriculumSections",
                newName: "DifficultySpecificationJson");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DifficultySpecificationJson",
                table: "CurriculumSections",
                newName: "SentenceSpecificationJson");
        }
    }
}
