using Chores.Infrastructure.Captcha;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chores.Infrastructure.Persistence.Configurations;

internal sealed class UsedCaptchaChallengeConfiguration : IEntityTypeConfiguration<UsedCaptchaChallenge>
{
    public void Configure(EntityTypeBuilder<UsedCaptchaChallenge> builder)
    {
        builder.ToTable("UsedCaptchaChallenges");
        builder.HasKey(c => c.Challenge);
        builder.Property(c => c.Challenge).HasMaxLength(256);
        builder.HasIndex(c => c.ExpiresAt);
    }
}
