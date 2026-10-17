using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KpiEvaluation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfessionIdSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProfessionIdSnapshot",
                table: "employee_evaluation",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE evaluation
                SET ProfessionIdSnapshot = employee.ProfessionId
                FROM employee_evaluation evaluation
                INNER JOIN employee ON employee.Id = evaluation.EmployeeId;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_employee_evaluation_ProfessionIdSnapshot",
                table: "employee_evaluation",
                column: "ProfessionIdSnapshot");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_employee_evaluation_ProfessionIdSnapshot",
                table: "employee_evaluation");

            migrationBuilder.DropColumn(
                name: "ProfessionIdSnapshot",
                table: "employee_evaluation");
        }
    }
}
