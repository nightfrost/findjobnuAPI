using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace findjobnuAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryID);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CityName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobIndexPostingsExtended",
                columns: table => new
                {
                    JobID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyURL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobUrl = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Published = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BannerPicture = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    FooterPicture = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Keywords = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobIndexPostingsExtended", x => x.JobID);
                });

            migrationBuilder.CreateTable(
                name: "UserProfile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SavedJobPosts = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Keywords = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfile", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobCategories",
                columns: table => new
                {
                    JobID = table.Column<int>(type: "int", nullable: false),
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobCategories", x => new { x.JobID, x.CategoryID });
                    table.ForeignKey(
                        name: "FK_JobCategories_Categories_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "Categories",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobCategories_JobIndexPostingsExtended_JobID",
                        column: x => x.JobID,
                        principalTable: "JobIndexPostingsExtended",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserProfileId = table.Column<int>(type: "int", nullable: false),
                    BasicInfo_Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BasicInfo_About = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BasicInfo_Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BasicInfo_Company = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BasicInfo_JobTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BasicInfo_LinkedinUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BasicInfo_OpenToWork = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkProfiles_UserProfile_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Accomplishments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accomplishments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accomplishments_WorkProfiles_WorkProfileId",
                        column: x => x.WorkProfileId,
                        principalTable: "WorkProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Occupation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contacts_WorkProfiles_WorkProfileId",
                        column: x => x.WorkProfileId,
                        principalTable: "WorkProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Educations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Institution = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Degree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FromDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LinkedinUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Educations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Educations_WorkProfiles_WorkProfileId",
                        column: x => x.WorkProfileId,
                        principalTable: "WorkProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Experiences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PositionTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Company = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FromDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Duration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LinkedinUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Experiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Experiences_WorkProfiles_WorkProfileId",
                        column: x => x.WorkProfileId,
                        principalTable: "WorkProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Interests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Interests_WorkProfiles_WorkProfileId",
                        column: x => x.WorkProfileId,
                        principalTable: "WorkProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Proficiency = table.Column<int>(type: "int", nullable: false),
                    WorkProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Skills_WorkProfiles_WorkProfileId",
                        column: x => x.WorkProfileId,
                        principalTable: "WorkProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accomplishments_WorkProfileId",
                table: "Accomplishments",
                column: "WorkProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_WorkProfileId",
                table: "Contacts",
                column: "WorkProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Educations_WorkProfileId",
                table: "Educations",
                column: "WorkProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Experiences_WorkProfileId",
                table: "Experiences",
                column: "WorkProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Interests_WorkProfileId",
                table: "Interests",
                column: "WorkProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_JobCategories_CategoryID",
                table: "JobCategories",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_JobIndexPostingsExtended_JobUrl",
                table: "JobIndexPostingsExtended",
                column: "JobUrl",
                unique: true,
                filter: "[JobUrl] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_WorkProfileId",
                table: "Skills",
                column: "WorkProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfile_UserId",
                table: "UserProfile",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkProfiles_UserProfileId",
                table: "WorkProfiles",
                column: "UserProfileId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accomplishments");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropTable(
                name: "Educations");

            migrationBuilder.DropTable(
                name: "Experiences");

            migrationBuilder.DropTable(
                name: "Interests");

            migrationBuilder.DropTable(
                name: "JobCategories");

            migrationBuilder.DropTable(
                name: "Skills");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "JobIndexPostingsExtended");

            migrationBuilder.DropTable(
                name: "WorkProfiles");

            migrationBuilder.DropTable(
                name: "UserProfile");
        }
    }
}
