using System.Diagnostics.CodeAnalysis;

namespace Chores.Domain.Families;

/// <summary>The UI and notification language of a Member: Swedish (<c>sv</c>) or English (<c>en</c>).</summary>
public sealed record PreferredLanguage
{
    public static readonly PreferredLanguage Swedish = new("sv");
    public static readonly PreferredLanguage English = new("en");

    private PreferredLanguage(string code) => Code = code;

    public string Code { get; }

    public static bool TryCreate(string? code, [NotNullWhen(true)] out PreferredLanguage? language)
    {
        language = code switch
        {
            "sv" => Swedish,
            "en" => English,
            _ => null,
        };
        return language is not null;
    }

    public static PreferredLanguage Create(string code) =>
        TryCreate(code, out var language)
            ? language
            : throw new ArgumentException($"'{code}' is not a supported language.", nameof(code));

    public override string ToString() => Code;
}
