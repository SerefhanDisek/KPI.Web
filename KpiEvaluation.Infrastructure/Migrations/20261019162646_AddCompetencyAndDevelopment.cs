using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KpiEvaluation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetencyAndDevelopment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "competency_matrix_entry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfessionId = table.Column<int>(type: "int", nullable: false),
                    Seniority = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Competency = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExpectedLevel = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competency_matrix_entry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_competency_matrix_entry_profession_ProfessionId",
                        column: x => x.ProfessionId,
                        principalTable: "profession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "development_item",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Progress = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_development_item", x => x.Id);
                    table.CheckConstraint("ck_development_progress", "[Progress] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_development_item_employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "development_check_in",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DevelopmentItemId = table.Column<long>(type: "bigint", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Progress = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_development_check_in", x => x.Id);
                    table.CheckConstraint("ck_check_in_progress", "[Progress] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_development_check_in_app_user_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "app_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_development_check_in_development_item_DevelopmentItemId",
                        column: x => x.DevelopmentItemId,
                        principalTable: "development_item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_competency_matrix_entry_ProfessionId_Seniority_Competency",
                table: "competency_matrix_entry",
                columns: new[] { "ProfessionId", "Seniority", "Competency" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_development_check_in_CreatedByUserId",
                table: "development_check_in",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_development_check_in_DevelopmentItemId_CreatedAt_Id",
                table: "development_check_in",
                columns: new[] { "DevelopmentItemId", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_development_item_EmployeeId_IsActive_TargetDate",
                table: "development_item",
                columns: new[] { "EmployeeId", "IsActive", "TargetDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "competency_matrix_entry");

            migrationBuilder.DropTable(
                name: "development_check_in");

            migrationBuilder.DropTable(
                name: "development_item");
        }
    }
}
