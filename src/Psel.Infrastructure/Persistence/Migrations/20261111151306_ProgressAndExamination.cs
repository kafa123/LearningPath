using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Psel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProgressAndExamination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExaminationSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChallengeSlug = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    VariantId = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    VariantLabel = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    TimeLimitSeconds = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExaminationSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExaminationAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExaminationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExaminationAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExaminationAttempts_ExaminationSessions_ExaminationId",
                        column: x => x.ExaminationId,
                        principalTable: "ExaminationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExaminationAttempts_CreatedAtUtc",
                table: "ExaminationAttempts",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ExaminationAttempts_ExaminationId",
                table: "ExaminationAttempts",
                column: "ExaminationId");

            migrationBuilder.CreateIndex(
                name: "IX_ExaminationAttempts_SubmissionId",
                table: "ExaminationAttempts",
                column: "SubmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExaminationSessions_ChallengeSlug_StartedAtUtc",
                table: "ExaminationSessions",
                columns: new[] { "ChallengeSlug", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExaminationSessions_StartedAtUtc",
                table: "ExaminationSessions",
                column: "StartedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExaminationAttempts");

            migrationBuilder.DropTable(
                name: "ExaminationSessions");
        }
    }
}
