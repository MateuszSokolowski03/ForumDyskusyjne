using Microsoft.EntityFrameworkCore.Migrations;

namespace ForumDyskusyjne.Migrations
{
    /// <inheritdoc />
    public partial class AddEditedByToMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "edited_by",
                table: "message",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "edited_by",
                table: "message");
        }
    }
}
