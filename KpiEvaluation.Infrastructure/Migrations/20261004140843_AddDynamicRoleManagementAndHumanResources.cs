using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KpiEvaluation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicRoleManagementAndHumanResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_role_permission",
                table: "role_permission");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "role_permission",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "app_user",
                newName: "RoleId");

            migrationBuilder.CreateTable(
                name: "app_role",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_role", x => x.Id);
                });

            migrationBuilder.Sql("""
                SET IDENTITY_INSERT app_role ON;
                INSERT INTO app_role (Id, Name, Code, IsSystem, IsActive, CreatedAt)
                VALUES
                    (1, N'Sistem Yöneticisi', N'administrator', 1, 1, SYSUTCDATETIME()),
                    (2, N'1. Amir', N'first-manager', 1, 1, SYSUTCDATETIME()),
                    (3, N'2. Amir', N'second-manager', 1, 1, SYSUTCDATETIME()),
                    (4, N'Çalışan', N'employee', 1, 1, SYSUTCDATETIME()),
                    (5, N'İnsan Kaynakları', N'human-resources', 1, 1, SYSUTCDATETIME());
                SET IDENTITY_INSERT app_role OFF;

                UPDATE app_user SET RoleId = RoleId + 1;
                UPDATE role_permission SET RoleId = RoleId + 1;
                """);

            migrationBuilder.AddPrimaryKey(
                name: "PK_role_permission",
                table: "role_permission",
                columns: new[] { "RoleId", "Permission" });

            migrationBuilder.CreateIndex(
                name: "IX_app_user_RoleId",
                table: "app_user",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_app_role_Code",
                table: "app_role",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_app_role_Name",
                table: "app_role",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_app_user_app_role_RoleId",
                table: "app_user",
                column: "RoleId",
                principalTable: "app_role",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_role_permission_app_role_RoleId",
                table: "role_permission",
                column: "RoleId",
                principalTable: "app_role",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_app_user_app_role_RoleId",
                table: "app_user");

            migrationBuilder.DropForeignKey(
                name: "FK_role_permission_app_role_RoleId",
                table: "role_permission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_role_permission",
                table: "role_permission");

            migrationBuilder.Sql("""
                UPDATE app_user SET RoleId = RoleId - 1;
                UPDATE role_permission SET RoleId = RoleId - 1;
                """);

            migrationBuilder.AddPrimaryKey(
                name: "PK_role_permission",
                table: "role_permission",
                columns: new[] { "RoleId", "Permission" });

            migrationBuilder.DropTable(
                name: "app_role");

            migrationBuilder.DropIndex(
                name: "IX_app_user_RoleId",
                table: "app_user");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "role_permission",
                newName: "Role");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "app_user",
                newName: "Role");
        }
    }
}
