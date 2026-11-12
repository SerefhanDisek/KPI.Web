using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KpiEvaluation.Infrastructure;

public sealed class TeamManagerAssignmentConfiguration : IEntityTypeConfiguration<TeamManagerAssignment>
{
    public void Configure(EntityTypeBuilder<TeamManagerAssignment> b)
    {
        b.ToTable("team_manager_assignment", t =>
        {
            t.HasCheckConstraint("ck_team_manager_weight", "[WeightPercent] > 0 AND [WeightPercent] <= 100");
            t.HasCheckConstraint("ck_team_manager_position", "[Position] > 0");
        });
        b.HasKey(x => new { x.ProfessionId, x.ManagerUserId });
        b.HasIndex(x => new { x.ProfessionId, x.Position }).IsUnique();
        b.Property(x => x.WeightPercent).HasPrecision(5, 2);
        b.HasOne(x => x.Profession).WithMany(x => x.ManagerAssignments).HasForeignKey(x => x.ProfessionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ManagerUser).WithMany().HasForeignKey(x => x.ManagerUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
