using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMistakeChallengeLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SkillsJson",
                table: "UserProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveCorrectCount",
                table: "LanguageMistakes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "LanguageMistakes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "LanguageMistakes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Companions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CurrentMood", "Name" },
                values: new object[] { 1, "Blip" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SkillsJson",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "ConsecutiveCorrectCount",
                table: "LanguageMistakes");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "LanguageMistakes");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "LanguageMistakes");

            migrationBuilder.UpdateData(
                table: "Companions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CurrentMood", "Name" },
                values: new object[] { 3, "Bip-bot" });
        }
    }
}
