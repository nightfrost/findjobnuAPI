using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FindjobnuService.Migrations
{
    /// <inheritdoc />
    public partial class RemoveJobIndexPostKeywords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Keywords",
                table: "JobIndexPostingsExtended");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Keywords",
                table: "JobIndexPostingsExtended",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
