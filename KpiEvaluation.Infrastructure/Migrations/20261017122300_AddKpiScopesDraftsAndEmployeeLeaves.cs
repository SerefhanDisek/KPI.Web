using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KpiEvaluation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKpiScopesDraftsAndEmployeeLeaves : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_score",
                table: "employee_evaluation_detail");

            migrationBuilder.DropCheckConstraint(
                name: "ck_average_score",
                table: "employee_evaluation");

            migrationBuilder.DropCheckConstraint(
                name: "ck_total_score",
                table: "employee_evaluation");

            migrationBuilder.DropCheckConstraint(
                name: "ck_weighted_score",
                table: "employee_evaluation");

            migrationBuilder.CreateTable(
                name: "employee_leave",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LeaveType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_leave", x => x.Id);
                    table.CheckConstraint("ck_leave_dates", "[EndDate] >= [StartDate]");
                    table.ForeignKey(
                        name: "FK_employee_leave_app_user_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "app_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_leave_app_user_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "app_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_leave_employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "kpi_criterion_profession",
                columns: table => new
                {
                    KpiCriterionId = table.Column<int>(type: "int", nullable: false),
                    ProfessionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kpi_criterion_profession", x => new { x.KpiCriterionId, x.ProfessionId });
                    table.ForeignKey(
                        name: "FK_kpi_criterion_profession_kpi_criterion_KpiCriterionId",
                        column: x => x.KpiCriterionId,
                        principalTable: "kpi_criterion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_kpi_criterion_profession_profession_ProfessionId",
                        column: x => x.ProfessionId,
                        principalTable: "profession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_score",
                table: "employee_evaluation_detail",
                sql: "[Score] BETWEEN 0 AND 5");

            migrationBuilder.Sql("""
                INSERT INTO kpi_criterion_profession (KpiCriterionId, ProfessionId)
                SELECT Id, ProfessionId
                FROM kpi_criterion
                WHERE IsCommon = 0 AND ProfessionId IS NOT NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_average_score",
                table: "employee_evaluation",
                sql: "([Status] = 0 AND [AverageScore] BETWEEN 0 AND 5) OR [AverageScore] BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "ck_total_score",
                table: "employee_evaluation",
                sql: "([Status] = 0 AND [TotalScore] >= 0) OR [TotalScore] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_weighted_score",
                table: "employee_evaluation",
                sql: "([Status] = 0 AND [WeightedScore] BETWEEN 0 AND 5) OR [WeightedScore] BETWEEN 1 AND 5");

            migrationBuilder.CreateIndex(
                name: "IX_employee_leave_CreatedByUserId",
                table: "employee_leave",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_leave_EmployeeId_StartDate_EndDate",
                table: "employee_leave",
                columns: new[] { "EmployeeId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_leave_StartDate_Id",
                table: "employee_leave",
                columns: new[] { "StartDate", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_leave_UpdatedByUserId",
                table: "employee_leave",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_kpi_criterion_profession_ProfessionId",
                table: "kpi_criterion_profession",
                column: "ProfessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_leave");

            migrationBuilder.DropTable(
                name: "kpi_criterion_profession");

            migrationBuilder.DropCheckConstraint(
                name: "ck_score",
                table: "employee_evaluation_detail");

            migrationBuilder.DropCheckConstraint(
                name: "ck_average_score",
                table: "employee_evaluation");

            migrationBuilder.DropCheckConstraint(
                name: "ck_total_score",
                table: "employee_evaluation");

            migrationBuilder.DropCheckConstraint(
                name: "ck_weighted_score",
                table: "employee_evaluation");

            migrationBuilder.AddCheckConstraint(
                name: "ck_score",
                table: "employee_evaluation_detail",
                sql: "[Score] BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "ck_average_score",
                table: "employee_evaluation",
                sql: "[AverageScore] BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "ck_total_score",
                table: "employee_evaluation",
                sql: "[TotalScore] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_weighted_score",
                table: "employee_evaluation",
                sql: "[WeightedScore] BETWEEN 1 AND 5");
        }
    }
}
