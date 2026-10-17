using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KpiEvaluation.Infrastructure;

public sealed class KpiCriterionProfessionConfiguration : IEntityTypeConfiguration<KpiCriterionProfession>
{
    public void Configure(EntityTypeBuilder<KpiCriterionProfession> builder)
    {
        builder.ToTable("kpi_criterion_profession");
        builder.HasKey(x => new { x.KpiCriterionId, x.ProfessionId });
        builder.HasOne(x => x.KpiCriterion).WithMany(x => x.Professions).HasForeignKey(x => x.KpiCriterionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Profession).WithMany().HasForeignKey(x => x.ProfessionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class EmployeeLeaveConfiguration : IEntityTypeConfiguration<EmployeeLeave>
{
    public void Configure(EntityTypeBuilder<EmployeeLeave> builder)
    {
        builder.ToTable("employee_leave", table => table.HasCheckConstraint("ck_leave_dates", "[EndDate] >= [StartDate]"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LeaveType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(2000);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => new { x.EmployeeId, x.StartDate, x.EndDate });
        builder.HasIndex(x => new { x.StartDate, x.Id });
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.UpdatedByUser).WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
