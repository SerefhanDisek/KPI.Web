using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KpiEvaluation.Infrastructure;

public sealed class EvaluationEmployeeResponseConfiguration : IEntityTypeConfiguration<EvaluationEmployeeResponse>
{
    public void Configure(EntityTypeBuilder<EvaluationEmployeeResponse> b)
    {
        b.ToTable("evaluation_employee_response"); b.HasKey(x => x.Id);
        b.Property(x => x.Comment).HasMaxLength(2000).IsRequired();
        b.HasIndex(x => new { x.EvaluationId, x.CreatedAt, x.Id });
        b.HasOne(x => x.Evaluation).WithMany().HasForeignKey(x => x.EvaluationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> b)
    {
        b.ToTable("audit_entry"); b.HasKey(x => x.Id);
        b.Property(x => x.EntityType).HasMaxLength(150).IsRequired();
        b.Property(x => x.EntityId).HasMaxLength(200).IsRequired();
        b.Property(x => x.Action).HasMaxLength(20).IsRequired();
        b.Property(x => x.ChangedFieldsJson).HasMaxLength(4000).IsRequired();
        b.HasIndex(x => new { x.CreatedAt, x.Id });
        b.HasIndex(x => new { x.EntityType, x.EntityId });
    }
}
