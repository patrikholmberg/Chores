using Chores.Domain.Families;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chores.Infrastructure.Persistence.Configurations;

internal sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Username)
            .HasConversion(u => u.Value, value => Username.Create(value))
            .HasMaxLength(Username.MaxLength);
        builder.Property(m => m.DisplayName).HasMaxLength(Member.DisplayNameMaxLength);
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(16);
        builder.Property(m => m.Email).HasMaxLength(256);
        builder.Property(m => m.PreferredLanguage)
            .HasConversion(l => l.Code, code => PreferredLanguage.Create(code))
            .HasMaxLength(2);
        builder.Ignore(m => m.IsRemoved);
        builder.HasIndex(m => m.FamilyId);
    }
}
