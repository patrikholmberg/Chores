namespace Chores.Api.Auth;

internal sealed class AuthOptions
{
    public const string Section = "Auth";

    /// <summary>The public origin of the web app, which is the token issuer. Defaults to the request host when unset.</summary>
    public string? Issuer { get; set; }

    /// <summary>Base64 PFX used to sign tokens. Required outside Development.</summary>
    public string? SigningCertificate { get; set; }

    public string? SigningCertificatePassword { get; set; }

    /// <summary>Base64 PFX used to encrypt tokens. Required outside Development.</summary>
    public string? EncryptionCertificate { get; set; }

    public string? EncryptionCertificatePassword { get; set; }
}
