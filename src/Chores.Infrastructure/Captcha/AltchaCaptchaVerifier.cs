using Chores.Application.Common.Identity;
using Ixnas.AltchaNet;

namespace Chores.Infrastructure.Captcha;

internal sealed class AltchaCaptchaVerifier(AltchaService altcha) : ICaptchaVerifier
{
    public async Task<bool> VerifyAsync(string? solution, CancellationToken cancellationToken) =>
        !string.IsNullOrWhiteSpace(solution) && (await altcha.Validate(solution, cancellationToken)).IsValid;
}
