using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Psel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssessmentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "Submissions",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Run");

            migrationBuilder.CreateTable(
                name: "Assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChallengeSlug = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LanguageId = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Purpose = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Outcome = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    PassedTests = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTests = table.Column<int>(type: "INTEGER", nullable: false),
                    PublicPassed = table.Column<int>(type: "INTEGER", nullable: false),
                    PublicTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    HiddenPassed = table.Column<int>(type: "INTEGER", nullable: false),
                    HiddenTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentTestResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    TestId = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    Visibility = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentTestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentTestResults_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_ChallengeSlug_CreatedAtUtc",
                table: "Assessments",
                columns: new[] { "ChallengeSlug", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_CreatedAtUtc",
                table: "Assessments",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_SubmissionId",
                table: "Assessments",
                column: "SubmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentTestResults_AssessmentId_SortOrder",
                table: "AssessmentTestResults",
                columns: new[] { "AssessmentId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentTestResults");

            migrationBuilder.DropTable(
                name: "Assessments");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "Submissions");
        }
    }
}
