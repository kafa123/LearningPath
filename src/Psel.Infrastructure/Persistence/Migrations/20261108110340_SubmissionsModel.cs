using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Psel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SubmissionsModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChallengeSlug = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LanguageId = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    SourceFilesJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DurationMs = table.Column<long>(type: "INTEGER", nullable: true),
                    ExitCode = table.Column<int>(type: "INTEGER", nullable: true),
                    StandardOutput = table.Column<string>(type: "TEXT", nullable: false),
                    StandardError = table.Column<string>(type: "TEXT", nullable: false),
                    FailureMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    OutputTruncated = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ChallengeSlug_LanguageId",
                table: "Submissions",
                columns: new[] { "ChallengeSlug", "LanguageId" });

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_CreatedAtUtc",
                table: "Submissions",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Submissions");
        }
    }
}
