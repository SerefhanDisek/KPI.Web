using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KpiEvaluation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorizationDraftWorkflowAndPageDesigner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "employee_evaluation",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByUserId",
                table: "employee_evaluation",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EvaluatorUserId",
                table: "employee_evaluation",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaximumScore",
                table: "employee_evaluation",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "ScorePercentage",
                table: "employee_evaluation",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "FirstManagerUserId",
                table: "employee",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecondManagerUserId",
                table: "employee",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "app_user",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_user", x => x.Id);
                    table.ForeignKey(
                        name: "FK_app_user_employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "page_definition",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LayoutJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredPermission = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_page_definition", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "role_permission",
                columns: table => new
                {
                    Role = table.Column<int>(type: "int", nullable: false),
                    Permission = table.Column<int>(type: "int", nullable: false),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permission", x => new { x.Role, x.Permission });
                });

            migrationBuilder.CreateIndex(
                name: "IX_employee_evaluation_ApprovedByUserId",
                table: "employee_evaluation",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_evaluation_EvaluatorUserId",
                table: "employee_evaluation",
                column: "EvaluatorUserId");

            migrationBuilder.Sql("""
                UPDATE evaluation
                SET MaximumScore = CASE WHEN counts.ItemCount > 0 THEN counts.ItemCount * 5 ELSE 1 END,
                    ScorePercentage = ROUND(evaluation.WeightedScore * 20, 2),
                    Status = 4,
                    ApprovedAt = COALESCE(evaluation.UpdatedAt, evaluation.EvaluationDate)
                FROM employee_evaluation evaluation
                OUTER APPLY (
                    SELECT COUNT(*) AS ItemCount
                    FROM employee_evaluation_detail detail
                    WHERE detail.EmployeeEvaluationId = evaluation.Id
                ) counts;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_maximum_score",
                table: "employee_evaluation",
                sql: "[MaximumScore] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_score_percentage",
                table: "employee_evaluation",
                sql: "[ScorePercentage] BETWEEN 0 AND 100");

            migrationBuilder.CreateIndex(
                name: "IX_employee_FirstManagerUserId",
                table: "employee",
                column: "FirstManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_SecondManagerUserId",
                table: "employee",
                column: "SecondManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_app_user_EmployeeId",
                table: "app_user",
                column: "EmployeeId",
                unique: true,
                filter: "[EmployeeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_app_user_UserName",
                table: "app_user",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_page_definition_Slug",
                table: "page_definition",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_employee_app_user_FirstManagerUserId",
                table: "employee",
                column: "FirstManagerUserId",
                principalTable: "app_user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_employee_app_user_SecondManagerUserId",
                table: "employee",
                column: "SecondManagerUserId",
                principalTable: "app_user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_employee_evaluation_app_user_ApprovedByUserId",
                table: "employee_evaluation",
                column: "ApprovedByUserId",
                principalTable: "app_user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_employee_evaluation_app_user_EvaluatorUserId",
                table: "employee_evaluation",
                column: "EvaluatorUserId",
                principalTable: "app_user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employee_app_user_FirstManagerUserId",
                table: "employee");

            migrationBuilder.DropForeignKey(
                name: "FK_employee_app_user_SecondManagerUserId",
                table: "employee");

            migrationBuilder.DropForeignKey(
                name: "FK_employee_evaluation_app_user_ApprovedByUserId",
                table: "employee_evaluation");

            migrationBuilder.DropForeignKey(
                name: "FK_employee_evaluation_app_user_EvaluatorUserId",
                table: "employee_evaluation");

            migrationBuilder.DropTable(
                name: "app_user");

            migrationBuilder.DropTable(
                name: "page_definition");

            migrationBuilder.DropTable(
                name: "role_permission");

            migrationBuilder.DropIndex(
                name: "IX_employee_evaluation_ApprovedByUserId",
                table: "employee_evaluation");

            migrationBuilder.DropIndex(
                name: "IX_employee_evaluation_EvaluatorUserId",
                table: "employee_evaluation");

            migrationBuilder.DropCheckConstraint(
                name: "ck_maximum_score",
                table: "employee_evaluation");

            migrationBuilder.DropCheckConstraint(
                name: "ck_score_percentage",
                table: "employee_evaluation");

            migrationBuilder.DropIndex(
                name: "IX_employee_FirstManagerUserId",
                table: "employee");

            migrationBuilder.DropIndex(
                name: "IX_employee_SecondManagerUserId",
                table: "employee");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "employee_evaluation");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "employee_evaluation");

            migrationBuilder.DropColumn(
                name: "EvaluatorUserId",
                table: "employee_evaluation");

            migrationBuilder.DropColumn(
                name: "MaximumScore",
                table: "employee_evaluation");

            migrationBuilder.DropColumn(
                name: "ScorePercentage",
                table: "employee_evaluation");

            migrationBuilder.DropColumn(
                name: "FirstManagerUserId",
                table: "employee");

            migrationBuilder.DropColumn(
                name: "SecondManagerUserId",
                table: "employee");
        }
    }
}
