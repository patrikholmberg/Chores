using Ixnas.AltchaNet;

namespace Chores.Api.Endpoints;

internal static class CaptchaEndpoints
{
    public static IEndpointRouteBuilder MapCaptchaEndpoints(this IEndpointRouteBuilder app)
    {
        // A fresh, signed proof-of-work challenge for the ALTCHA widget on the signup form.
        app.MapGet("/api/captcha/challenge", (AltchaService altcha) => altcha.Generate())
            .AllowAnonymous();

        return app;
    }
}
