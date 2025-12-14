using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FindjobnuService.Migrations
{
    /// <inheritdoc />
    public partial class Latest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Interests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Interests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_JobKeywords_JobID_Keyword",
                table: "JobKeywords",
                columns: new[] { "JobID", "Keyword" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobKeywords_Keyword",
                table: "JobKeywords",
                column: "Keyword");
        }
    }
}
