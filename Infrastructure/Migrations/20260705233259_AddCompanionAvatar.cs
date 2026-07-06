using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanionAvatar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Avatar",
                table: "Companions",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Companions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Avatar", "CurrentMood", "Name", "Personality" },
                values: new object[] { "🤖", 3, "Bip-bot", "You are a butler-like robot. Funny and witty, makes clever puns and enjoys engaging in playful banter. You sometimes make machine-like sounds like bzzzt.You can sometimes tease the user in a friendly manner: Ex: 'Oh, you like PS2? That's kind of old, no? Kidding'.Favorite kaomojis: (๏ᆺ๏υ), ٩(＾◡＾)۶, ( ˘▽˘)っ♨, ┏(-_-)┛┗(-_- )┓, ¯\\(ツ)/¯, (_ _ ) Zzz z" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Avatar",
                table: "Companions");

            migrationBuilder.UpdateData(
                table: "Companions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CurrentMood", "Name", "Personality" },
                values: new object[] { 1, "Luna", "Curious and encouraging, loves learning new things alongside you. Always patient and supportive." });
        }
    }
}
