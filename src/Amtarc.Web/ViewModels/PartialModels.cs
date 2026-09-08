namespace Amtarc.Web.ViewModels;

/// <summary>Backing model for the honeycomb texture partial.</summary>
public sealed record HexagonPatternModel
{
    public string Color { get; init; } = "#ff9d00";

    /// <summary>
    /// The texture's opacity as a Tailwind class rather than an inline style, so the
    /// content-security policy can forbid inline styles outright. Call sites pass a literal so
    /// Tailwind's scanner finds the class.
    /// </summary>
    public string OpacityClass { get; init; } = "opacity-[0.09]";

    public string CssClass { get; init; } = "absolute inset-0 h-full w-full pointer-events-none";
}

public enum PillButtonVariant
{
    Gold,
    GoldLarge,
    Dark,
    Outline,
}

/// <summary>Backing model for the pill-shaped call-to-action partial.</summary>
public sealed record PillButtonModel
{
    public required string Href { get; init; }
    public required string Label { get; init; }
    public PillButtonVariant Variant { get; init; } = PillButtonVariant.Dark;
    public bool External { get; init; }

    public string VariantClasses => Variant switch
    {
        PillButtonVariant.Gold =>
            "bg-gold-gradient text-ink font-extrabold shadow-btn-gold hover:brightness-[1.04] hover:-translate-y-0.5",
        PillButtonVariant.GoldLarge =>
            "bg-gold-gradient text-ink font-extrabold shadow-btn-gold-lg hover:brightness-[1.04] hover:-translate-y-0.5",
        PillButtonVariant.Outline =>
            "bg-white/75 border border-ink/15 text-ink font-bold hover:bg-white",
        _ => "bg-brand text-white font-bold hover:bg-brand-black hover:-translate-y-0.5",
    };
}

public enum SocialLinksVariant
{
    Contact,
    Footer,
}
