using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KpiEvaluation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "evaluation_period",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluation_period", x => x.Id);
                    table.CheckConstraint("ck_period_dates", "[EndDate] >= [StartDate]");
                });

            migrationBuilder.CreateTable(
                name: "profession",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profession", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "employee",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProfessionId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_profession_ProfessionId",
                        column: x => x.ProfessionId,
                        principalTable: "profession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "kpi_criterion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfessionId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsCommon = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kpi_criterion", x => x.Id);
                    table.CheckConstraint("ck_kpi_order", "[DisplayOrder] > 0");
                    table.CheckConstraint("ck_kpi_scope", "([IsCommon] = 1 AND [ProfessionId] IS NULL) OR ([IsCommon] = 0 AND [ProfessionId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_kpi_criterion_profession_ProfessionId",
                        column: x => x.ProfessionId,
                        principalTable: "profession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_evaluation",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    EvaluationPeriodId = table.Column<int>(type: "int", nullable: false),
                    EmployeeNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProfessionNameSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PeriodNameSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EvaluationDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    TotalScore = table.Column<int>(type: "int", nullable: false),
                    AverageScore = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    GeneralNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_evaluation", x => x.Id);
                    table.CheckConstraint("ck_average_score", "[AverageScore] BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_total_score", "[TotalScore] > 0");
                    table.ForeignKey(
                        name: "FK_employee_evaluation_employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_evaluation_evaluation_period_EvaluationPeriodId",
                        column: x => x.EvaluationPeriodId,
                        principalTable: "evaluation_period",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_evaluation_detail",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeEvaluationId = table.Column<long>(type: "bigint", nullable: false),
                    KpiCriterionId = table.Column<int>(type: "int", nullable: false),
                    KpiTitleSnapshot = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsCommonSnapshot = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrderSnapshot = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_evaluation_detail", x => x.Id);
                    table.CheckConstraint("ck_score", "[Score] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_employee_evaluation_detail_employee_evaluation_EmployeeEvaluationId",
                        column: x => x.EmployeeEvaluationId,
                        principalTable: "employee_evaluation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_evaluation_detail_kpi_criterion_KpiCriterionId",
                        column: x => x.KpiCriterionId,
                        principalTable: "kpi_criterion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employee_FullName",
                table: "employee",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_employee_ProfessionId",
                table: "employee",
                column: "ProfessionId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_evaluation_EmployeeId_EvaluationPeriodId",
                table: "employee_evaluation",
                columns: new[] { "EmployeeId", "EvaluationPeriodId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_evaluation_EvaluationPeriodId",
                table: "employee_evaluation",
                column: "EvaluationPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_evaluation_detail_EmployeeEvaluationId_KpiCriterionId",
                table: "employee_evaluation_detail",
                columns: new[] { "EmployeeEvaluationId", "KpiCriterionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_evaluation_detail_KpiCriterionId",
                table: "employee_evaluation_detail",
                column: "KpiCriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_period_Name",
                table: "evaluation_period",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_kpi_criterion_ProfessionId_IsCommon_IsActive",
                table: "kpi_criterion",
                columns: new[] { "ProfessionId", "IsCommon", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_profession_Name",
                table: "profession",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_evaluation_detail");

            migrationBuilder.DropTable(
                name: "employee_evaluation");

            migrationBuilder.DropTable(
                name: "kpi_criterion");

            migrationBuilder.DropTable(
                name: "employee");

            migrationBuilder.DropTable(
                name: "evaluation_period");

            migrationBuilder.DropTable(
                name: "profession");
        }
    }
}
