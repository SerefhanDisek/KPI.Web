using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KpiEvaluation.Infrastructure;

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("app_user");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.MustChangePassword).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => x.UserName).IsUnique();
        builder.HasIndex(x => x.EmployeeId).IsUnique().HasFilter("[EmployeeId] IS NOT NULL");
        builder.HasOne(x => x.Role).WithMany(x => x.Users).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithOne(x => x.UserAccount).HasForeignKey<AppUser>(x => x.EmployeeId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        builder.ToTable("app_role");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permission");
        builder.HasKey(x => new { x.RoleId, x.Permission });
        builder.HasOne(x => x.Role).WithMany(x => x.Permissions).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PageDefinitionConfiguration : IEntityTypeConfiguration<PageDefinition>
{
    public void Configure(EntityTypeBuilder<PageDefinition> builder)
    {
        builder.ToTable("page_definition");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.LayoutJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}
