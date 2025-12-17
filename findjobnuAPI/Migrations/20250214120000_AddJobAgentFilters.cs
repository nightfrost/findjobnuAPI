using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FindjobnuService.Migrations
{
    /// <inheritdoc />
    public partial class AddJobAgentFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IncludeKeywords",
                table: "JobAgents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredCategoryIds",
                table: "JobAgents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredLocations",
                table: "JobAgents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncludeKeywords",
                table: "JobAgents");

            migrationBuilder.DropColumn(
                name: "PreferredCategoryIds",
                table: "JobAgents");

            migrationBuilder.DropColumn(
                name: "PreferredLocations",
                table: "JobAgents");
        }
    }
}
