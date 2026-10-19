using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KpiEvaluation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeparateManagerEvaluations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_employee_evaluation_EmployeeId_EvaluationPeriodId",
                table: "employee_evaluation");

            migrationBuilder.CreateIndex(
                name: "IX_employee_evaluation_EmployeeId_EvaluationPeriodId_EvaluatorUserId",
                table: "employee_evaluation",
                columns: new[] { "EmployeeId", "EvaluationPeriodId", "EvaluatorUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM employee_evaluation GROUP BY EmployeeId, EvaluationPeriodId HAVING COUNT(*) > 1) THROW 51000, 'Birden fazla amir kaydi var; eski tek kayit kisitina veri kaybi olmadan donulemez.', 1;");

            migrationBuilder.DropIndex(
                name: "IX_employee_evaluation_EmployeeId_EvaluationPeriodId_EvaluatorUserId",
                table: "employee_evaluation");

            migrationBuilder.CreateIndex(
                name: "IX_employee_evaluation_EmployeeId_EvaluationPeriodId",
                table: "employee_evaluation",
                columns: new[] { "EmployeeId", "EvaluationPeriodId" },
                unique: true);
        }
    }
}
