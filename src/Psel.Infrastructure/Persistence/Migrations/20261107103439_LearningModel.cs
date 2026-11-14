using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Psel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LearningModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LearningDomains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningDomains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearningModules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DomainId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 600, nullable: false),
                    Level = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningModules_LearningDomains_DomainId",
                        column: x => x.DomainId,
                        principalTable: "LearningDomains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Competencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ModuleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 140, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 800, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Competencies_LearningModules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "LearningModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearningMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ModuleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 140, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    BodyMarkdown = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningMaterials_LearningModules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "LearningModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearningModulePrerequisites",
                columns: table => new
                {
                    ModuleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PrerequisiteModuleId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningModulePrerequisites", x => new { x.ModuleId, x.PrerequisiteModuleId });
                    table.ForeignKey(
                        name: "FK_LearningModulePrerequisites_LearningModules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "LearningModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LearningModulePrerequisites_LearningModules_PrerequisiteModuleId",
                        column: x => x.PrerequisiteModuleId,
                        principalTable: "LearningModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LearningObjectives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ModuleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningObjectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningObjectives_LearningModules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "LearningModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Competencies_ModuleId_SortOrder",
                table: "Competencies",
                columns: new[] { "ModuleId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Competencies_Slug",
                table: "Competencies",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningDomains_Slug",
                table: "LearningDomains",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningMaterials_ModuleId_SortOrder",
                table: "LearningMaterials",
                columns: new[] { "ModuleId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningMaterials_Slug",
                table: "LearningMaterials",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningModulePrerequisites_PrerequisiteModuleId",
                table: "LearningModulePrerequisites",
                column: "PrerequisiteModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningModules_DomainId_SortOrder",
                table: "LearningModules",
                columns: new[] { "DomainId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningModules_Slug",
                table: "LearningModules",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningObjectives_ModuleId_SortOrder",
                table: "LearningObjectives",
                columns: new[] { "ModuleId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Competencies");

            migrationBuilder.DropTable(
                name: "LearningMaterials");

            migrationBuilder.DropTable(
                name: "LearningModulePrerequisites");

            migrationBuilder.DropTable(
                name: "LearningObjectives");

            migrationBuilder.DropTable(
                name: "LearningModules");

            migrationBuilder.DropTable(
                name: "LearningDomains");
        }
    }
}
