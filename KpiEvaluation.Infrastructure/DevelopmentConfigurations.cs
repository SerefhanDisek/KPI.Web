using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KpiEvaluation.Infrastructure;

public sealed class CompetencyMatrixEntryConfiguration : IEntityTypeConfiguration<CompetencyMatrixEntry>
{
    public void Configure(EntityTypeBuilder<CompetencyMatrixEntry> b)
    {
        b.ToTable("competency_matrix_entry"); b.HasKey(x => x.Id);
        b.Property(x => x.Seniority).HasMaxLength(100).IsRequired();
        b.Property(x => x.Competency).HasMaxLength(200).IsRequired();
        b.Property(x => x.ExpectedLevel).HasMaxLength(500).IsRequired();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.ProfessionId, x.Seniority, x.Competency }).IsUnique();
        b.HasOne(x => x.Profession).WithMany().HasForeignKey(x => x.ProfessionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DevelopmentItemConfiguration : IEntityTypeConfiguration<DevelopmentItem>
{
    public void Configure(EntityTypeBuilder<DevelopmentItem> b)
    {
        b.ToTable("development_item", t => t.HasCheckConstraint("ck_development_progress", "[Progress] BETWEEN 0 AND 100")); b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired(); b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Progress).HasPrecision(5,2); b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.EmployeeId, x.IsActive, x.TargetDate });
        b.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DevelopmentCheckInConfiguration : IEntityTypeConfiguration<DevelopmentCheckIn>
{
    public void Configure(EntityTypeBuilder<DevelopmentCheckIn> b)
    {
        b.ToTable("development_check_in", t => t.HasCheckConstraint("ck_check_in_progress", "[Progress] BETWEEN 0 AND 100")); b.HasKey(x => x.Id);
        b.Property(x => x.Note).HasMaxLength(2000).IsRequired(); b.Property(x => x.Progress).HasPrecision(5,2);
        b.HasIndex(x => new { x.DevelopmentItemId, x.CreatedAt, x.Id });
        b.HasOne(x => x.DevelopmentItem).WithMany(x => x.CheckIns).HasForeignKey(x => x.DevelopmentItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
