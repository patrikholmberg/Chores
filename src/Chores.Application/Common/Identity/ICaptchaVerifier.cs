namespace Chores.Application.Common.Identity;

/// <summary>Checks a CAPTCHA solution issued by this system. A solution is accepted at most once.</summary>
public interface ICaptchaVerifier
{
    Task<bool> VerifyAsync(string? solution, CancellationToken cancellationToken);
}
