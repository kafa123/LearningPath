using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Psel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RubricReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RubricReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChallengeSlug = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    Band = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RubricReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RubricReviews_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RubricCriterionScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RubricReviewId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CriterionId = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RubricCriterionScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RubricCriterionScores_RubricReviews_RubricReviewId",
                        column: x => x.RubricReviewId,
                        principalTable: "RubricReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RubricCriterionScores_RubricReviewId_SortOrder",
                table: "RubricCriterionScores",
                columns: new[] { "RubricReviewId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_RubricReviews_SubmissionId",
                table: "RubricReviews",
                column: "SubmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RubricReviews_UpdatedAtUtc",
                table: "RubricReviews",
                column: "UpdatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RubricCriterionScores");

            migrationBuilder.DropTable(
                name: "RubricReviews");
        }
    }
}
