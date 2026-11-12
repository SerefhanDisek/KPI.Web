using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KpiEvaluation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurableTeamManagerWeights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_evaluator_manager_level",
                table: "employee_evaluation");

            migrationBuilder.CreateTable(
                name: "team_manager_assignment",
                columns: table => new
                {
                    ProfessionId = table.Column<int>(type: "int", nullable: false),
                    ManagerUserId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    WeightPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_manager_assignment", x => new { x.ProfessionId, x.ManagerUserId });
                    table.CheckConstraint("ck_team_manager_position", "[Position] > 0");
                    table.CheckConstraint("ck_team_manager_weight", "[WeightPercent] > 0 AND [WeightPercent] <= 100");
                    table.ForeignKey(
                        name: "FK_team_manager_assignment_app_user_ManagerUserId",
                        column: x => x.ManagerUserId,
                        principalTable: "app_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_team_manager_assignment_profession_ProfessionId",
                        column: x => x.ProfessionId,
                        principalTable: "profession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_evaluator_manager_level",
                table: "employee_evaluation",
                sql: "[EvaluatorManagerLevelSnapshot] IS NULL OR [EvaluatorManagerLevelSnapshot] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_team_manager_assignment_ManagerUserId",
                table: "team_manager_assignment",
                column: "ManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_team_manager_assignment_ProfessionId_Position",
                table: "team_manager_assignment",
                columns: new[] { "ProfessionId", "Position" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO team_manager_assignment (ProfessionId, ManagerUserId, Position, WeightPercent)
                SELECT Id, FirstManagerUserId, 1, CASE WHEN SecondManagerUserId IS NULL THEN 100 ELSE 70 END
                FROM profession WHERE FirstManagerUserId IS NOT NULL
                    AND (SecondManagerUserId IS NULL OR FirstManagerUserId <> SecondManagerUserId);
                INSERT INTO team_manager_assignment (ProfessionId, ManagerUserId, Position, WeightPercent)
                SELECT Id, SecondManagerUserId, CASE WHEN FirstManagerUserId IS NULL THEN 1 ELSE 2 END,
                    CASE WHEN FirstManagerUserId IS NULL THEN 100 ELSE 30 END
                FROM profession WHERE SecondManagerUserId IS NOT NULL
                    AND (FirstManagerUserId IS NULL OR FirstManagerUserId <> SecondManagerUserId);

                UPDATE evaluation SET ScorePercentage = ROUND(scores.Percentage, 2)
                FROM employee_evaluation evaluation
                INNER JOIN (
                    SELECT EmployeeEvaluationId,
                        CASE WHEN SUM(WeightPercentSnapshot) > 0
                            THEN SUM(Score * WeightPercentSnapshot) / SUM(WeightPercentSnapshot) * 20
                            ELSE SUM(CAST(Score AS decimal(18, 6))) / COUNT(*) * 20 END AS Percentage
                    FROM employee_evaluation_detail GROUP BY EmployeeEvaluationId
                ) scores ON scores.EmployeeEvaluationId = evaluation.Id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "team_manager_assignment");

            migrationBuilder.DropCheckConstraint(
                name: "ck_evaluator_manager_level",
                table: "employee_evaluation");

            migrationBuilder.AddCheckConstraint(
                name: "ck_evaluator_manager_level",
                table: "employee_evaluation",
                sql: "[EvaluatorManagerLevelSnapshot] IS NULL OR [EvaluatorManagerLevelSnapshot] IN (1, 2)");
        }
    }
}
