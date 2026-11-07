using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KpiEvaluation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamManagersAndEvaluationWeights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FirstManagerUserId",
                table: "profession",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecondManagerUserId",
                table: "profession",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EvaluatorManagerLevelSnapshot",
                table: "employee_evaluation",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_profession_FirstManagerUserId",
                table: "profession",
                column: "FirstManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_profession_SecondManagerUserId",
                table: "profession",
                column: "SecondManagerUserId");

            migrationBuilder.AddCheckConstraint(
                name: "ck_evaluator_manager_level",
                table: "employee_evaluation",
                sql: "[EvaluatorManagerLevelSnapshot] IS NULL OR [EvaluatorManagerLevelSnapshot] IN (1, 2)");

            migrationBuilder.AddForeignKey(
                name: "FK_profession_app_user_FirstManagerUserId",
                table: "profession",
                column: "FirstManagerUserId",
                principalTable: "app_user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_profession_app_user_SecondManagerUserId",
                table: "profession",
                column: "SecondManagerUserId",
                principalTable: "app_user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                UPDATE evaluation
                SET EvaluatorManagerLevelSnapshot = CASE
                    WHEN employee.FirstManagerUserId = evaluation.EvaluatorUserId THEN 1
                    WHEN employee.SecondManagerUserId = evaluation.EvaluatorUserId THEN 2
                    END
                FROM employee_evaluation AS evaluation
                INNER JOIN employee ON employee.Id = evaluation.EmployeeId
                WHERE evaluation.EvaluatorUserId IS NOT NULL
                    AND (employee.FirstManagerUserId = evaluation.EvaluatorUserId OR employee.SecondManagerUserId = evaluation.EvaluatorUserId)
                    AND NOT (ISNULL(employee.FirstManagerUserId, 0) = evaluation.EvaluatorUserId
                        AND ISNULL(employee.SecondManagerUserId, 0) = evaluation.EvaluatorUserId);

                WITH manager_pairs AS (
                    SELECT DISTINCT ProfessionId, FirstManagerUserId, SecondManagerUserId FROM employee
                ), uniform_teams AS (
                    SELECT ProfessionId FROM manager_pairs GROUP BY ProfessionId HAVING COUNT(*) = 1
                )
                UPDATE profession
                SET FirstManagerUserId = manager_pairs.FirstManagerUserId,
                    SecondManagerUserId = manager_pairs.SecondManagerUserId
                FROM profession
                INNER JOIN uniform_teams ON uniform_teams.ProfessionId = profession.Id
                INNER JOIN manager_pairs ON manager_pairs.ProfessionId = profession.Id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_profession_app_user_FirstManagerUserId",
                table: "profession");

            migrationBuilder.DropForeignKey(
                name: "FK_profession_app_user_SecondManagerUserId",
                table: "profession");

            migrationBuilder.DropIndex(
                name: "IX_profession_FirstManagerUserId",
                table: "profession");

            migrationBuilder.DropIndex(
                name: "IX_profession_SecondManagerUserId",
                table: "profession");

            migrationBuilder.DropCheckConstraint(
                name: "ck_evaluator_manager_level",
                table: "employee_evaluation");

            migrationBuilder.DropColumn(
                name: "FirstManagerUserId",
                table: "profession");

            migrationBuilder.DropColumn(
                name: "SecondManagerUserId",
                table: "profession");

            migrationBuilder.DropColumn(
                name: "EvaluatorManagerLevelSnapshot",
                table: "employee_evaluation");
        }
    }
}
