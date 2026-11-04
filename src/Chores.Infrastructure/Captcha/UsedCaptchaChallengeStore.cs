using Chores.Infrastructure.Persistence;
using Ixnas.AltchaNet;
using Microsoft.EntityFrameworkCore;

namespace Chores.Infrastructure.Captcha;

/// <summary>Remembers accepted challenges in Postgres until they expire, so a solution cannot be replayed.</summary>
internal sealed class UsedCaptchaChallengeStore(ChoresDbContext db, TimeProvider time) : IAltchaChallengeStore
{
    public async Task Store(string challenge, DateTimeOffset expiryUtc, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        await db.UsedCaptchaChallenges.Where(c => c.ExpiresAt < now).ExecuteDeleteAsync(cancellationToken);

        db.UsedCaptchaChallenges.Add(new UsedCaptchaChallenge { Challenge = challenge, ExpiresAt = expiryUtc });
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> Exists(string challenge, CancellationToken cancellationToken) =>
        db.UsedCaptchaChallenges.AnyAsync(c => c.Challenge == challenge, cancellationToken);

    public Task Store(string challenge, DateTimeOffset expiryUtc) => Store(challenge, expiryUtc, CancellationToken.None);

    public Task<bool> Exists(string challenge) => Exists(challenge, CancellationToken.None);
}
