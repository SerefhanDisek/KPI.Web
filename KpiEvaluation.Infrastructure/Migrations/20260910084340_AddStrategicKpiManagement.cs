using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KpiEvaluation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStrategicKpiManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "kpi_criterion",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyGoalId",
                table: "kpi_criterion",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataSource",
                table: "kpi_criterion",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAchievable",
                table: "kpi_criterion",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMeasurable",
                table: "kpi_criterion",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRelevant",
                table: "kpi_criterion",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSpecific",
                table: "kpi_criterion",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTimeBound",
                table: "kpi_criterion",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MeasurementType",
                table: "kpi_criterion",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReviewFrequency",
                table: "kpi_criterion",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TargetDirection",
                table: "kpi_criterion",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetValue",
                table: "kpi_criterion",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "kpi_criterion",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ValidFrom",
                table: "kpi_criterion",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ValidTo",
                table: "kpi_criterion",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightPercent",
                table: "kpi_criterion",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualValue",
                table: "employee_evaluation_detail",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MeasurementTypeSnapshot",
                table: "employee_evaluation_detail",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TargetDirectionSnapshot",
                table: "employee_evaluation_detail",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetValueSnapshot",
                table: "employee_evaluation_detail",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitSnapshot",
                table: "employee_evaluation_detail",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightPercentSnapshot",
                table: "employee_evaluation_detail",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightedScore",
                table: "employee_evaluation_detail",
                type: "decimal(8,4)",
                precision: 8,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DevelopmentAction",
                table: "employee_evaluation",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeFeedback",
                table: "employee_evaluation",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FollowUpDate",
                table: "employee_evaluation",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerFeedback",
                table: "employee_evaluation",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "employee_evaluation",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightedScore",
                table: "employee_evaluation",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Preserve the meaning of existing evaluations while introducing weighted scoring.
            migrationBuilder.Sql("UPDATE employee_evaluation SET WeightedScore = AverageScore, Status = 1");
            migrationBuilder.Sql("UPDATE kpi_criterion SET ReviewFrequency = 2");
            migrationBuilder.Sql(@"UPDATE d SET WeightedScore = CAST(d.Score AS decimal(8,4)) / counts.ItemCount
FROM employee_evaluation_detail d
JOIN (SELECT EmployeeEvaluationId, COUNT(*) ItemCount FROM employee_evaluation_detail GROUP BY EmployeeEvaluationId) counts
  ON counts.EmployeeEvaluationId = d.EmployeeEvaluationId");

            migrationBuilder.CreateTable(
                name: "company_goal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EvaluationPeriodId = table.Column<int>(type: "int", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_goal", x => x.Id);
                    table.CheckConstraint("ck_company_goal_priority", "[Priority] > 0");
                    table.ForeignKey(
                        name: "FK_company_goal_evaluation_period_EvaluationPeriodId",
                        column: x => x.EvaluationPeriodId,
                        principalTable: "evaluation_period",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_kpi_criterion_CompanyGoalId",
                table: "kpi_criterion",
                column: "CompanyGoalId");

            migrationBuilder.AddCheckConstraint(
                name: "ck_kpi_validity",
                table: "kpi_criterion",
                sql: "[ValidTo] IS NULL OR [ValidFrom] IS NULL OR [ValidTo] >= [ValidFrom]");

            migrationBuilder.AddCheckConstraint(
                name: "ck_kpi_weight",
                table: "kpi_criterion",
                sql: "[WeightPercent] BETWEEN 0 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "ck_weighted_score",
                table: "employee_evaluation",
                sql: "[WeightedScore] BETWEEN 1 AND 5");

            migrationBuilder.CreateIndex(
                name: "IX_company_goal_EvaluationPeriodId",
                table: "company_goal",
                column: "EvaluationPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_company_goal_Name_EvaluationPeriodId",
                table: "company_goal",
                columns: new[] { "Name", "EvaluationPeriodId" },
                unique: true,
                filter: "[EvaluationPeriodId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_kpi_criterion_company_goal_CompanyGoalId",
                table: "kpi_criterion",
                column: "CompanyGoalId",
                principalTable: "company_goal",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_kpi_criterion_company_goal_CompanyGoalId",
                table: "kpi_criterion");

            migrationBuilder.DropTable(
                name: "company_goal");

            migrationBuilder.DropIndex(
                name: "IX_kpi_criterion_CompanyGoalId",
                table: "kpi_criterion");

            migrationBuilder.DropCheckConstraint(
                name: "ck_kpi_validity",
                table: "kpi_criterion");

            migrationBuilder.DropCheckConstraint(
                name: "ck_kpi_weight",
                table: "kpi_criterion");

            migrationBuilder.DropCheckConstraint(
                name: "ck_weighted_score",
                table: "employee_evaluation");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "kpi_criterion");

            migrationBuilder.DropColumn(
                name: "CompanyGoalId",
                table: "kpi_criterion");

            migrationBuilder.DropColumn(
                name: "DataSource",
                table: "kpi_criterion");

            migrationBuilder.DropColumn(
                name: "IsAchievable",
                table: "kpi_criterion");

            migrationBuilder.DropColumn(
                name: "IsMeasurable",
                table: "kpi_criterion");

            migrationBuilder.DropColumn(
                name: "IsRelevant",
                table: "kpi_criterion");

            migrationBuilder.DropColumn(
                name: "IsSpecific",
                table: "kpi_criterion");

            migrationBuilder.DropColumn(
                name: "IsTimeBound",
                table: "kpi_criterion");

            migrationBuilder.DropColumn(
                name: "MeasurementType",
                table: "kpi_criterion");

            migrationBuilder.DropColumn(
                name: "ReviewFrequency",
                table: "kpi_criterion");

            migrationBuilder.DropColumn(
                name: "TargetDirection",
                table: "kpi_criterion");

            migrationBuilder.DropColumn(
                name: "TargetValue",
                table: "kpi_criterion");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "kpi_criterion");

            migrationBuilder.DropColumn(
                name: "ValidFrom",
                table: "kpi_criterion");

            migrationBuilder.DropColumn(
                name: "ValidTo",
                table: "kpi_criterion");

            migrationBuilder.DropColumn(
                name: "WeightPercent",
                table: "kpi_criterion");

            migrationBuilder.DropColumn(
                name: "ActualValue",
                table: "employee_evaluation_detail");

            migrationBuilder.DropColumn(
                name: "MeasurementTypeSnapshot",
                table: "employee_evaluation_detail");

            migrationBuilder.DropColumn(
                name: "TargetDirectionSnapshot",
                table: "employee_evaluation_detail");

            migrationBuilder.DropColumn(
                name: "TargetValueSnapshot",
                table: "employee_evaluation_detail");

            migrationBuilder.DropColumn(
                name: "UnitSnapshot",
                table: "employee_evaluation_detail");

            migrationBuilder.DropColumn(
                name: "WeightPercentSnapshot",
                table: "employee_evaluation_detail");

            migrationBuilder.DropColumn(
                name: "WeightedScore",
                table: "employee_evaluation_detail");

            migrationBuilder.DropColumn(
                name: "DevelopmentAction",
                table: "employee_evaluation");

            migrationBuilder.DropColumn(
                name: "EmployeeFeedback",
                table: "employee_evaluation");

            migrationBuilder.DropColumn(
                name: "FollowUpDate",
                table: "employee_evaluation");

            migrationBuilder.DropColumn(
                name: "ManagerFeedback",
                table: "employee_evaluation");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "employee_evaluation");

            migrationBuilder.DropColumn(
                name: "WeightedScore",
                table: "employee_evaluation");
        }
    }
}
