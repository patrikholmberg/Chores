namespace Chores.Infrastructure.Captcha;

public sealed class CaptchaOptions
{
    public const string Section = "Captcha";

    /// <summary>Base64 of the 64-byte HMAC key that signs challenges. Generated per process in Development when missing.</summary>
    public string? HmacKey { get; set; }

    public int ExpirySeconds { get; set; } = 600;
}
