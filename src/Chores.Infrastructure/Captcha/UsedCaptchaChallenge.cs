namespace Chores.Infrastructure.Captcha;

/// <summary>A CAPTCHA challenge that has already been accepted once, kept until it expires to block replays.</summary>
internal sealed class UsedCaptchaChallenge
{
    public required string Challenge { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }
}
