namespace Amtarc.Web.Content;

public enum FieldType
{
    /// <summary>Single-line string.</summary>
    Text,

    /// <summary>Multi-line string.</summary>
    Textarea,

    /// <summary>A string array edited as one line per entry.</summary>
    Lines,

    /// <summary>An array of day/time pairs edited as repeatable rows.</summary>
    Hours,
}

public sealed record FieldConfig(string Path, string Label, FieldType Type);

public sealed record SectionConfig(
    string Key, string Title, string Description, IReadOnlyList<FieldConfig> Fields);

/// <summary>
/// Which fields of each editable section the back-office exposes, and how each one is edited.
/// Port of <c>SECTION_CONFIGS</c> from the previous site. Labels are French because an admin
/// reads them; the paths are the JSON keys and stay as they are stored.
/// </summary>
public static class SectionFieldConfig
{
    public static readonly IReadOnlyList<SectionConfig> All =
    [
        new(SectionKey.Hero, "Bandeau d'accueil", "Titre, texte et boutons du haut de page.",
        [
            new("badge", "Surtitre (badge)", FieldType.Text),
            new("title", "Titre", FieldType.Textarea),
            new("paragraph", "Paragraphe", FieldType.Textarea),
            new("ctaPrimary.label", "Bouton principal", FieldType.Text),
            new("ctaSecondary.label", "Bouton secondaire", FieldType.Text),
        ]),

        new(SectionKey.Announcements, "Messages du club",
            "Les quatre encarts d'annonces sous le bandeau.",
        [
            new("kicker", "Surtitre de section", FieldType.Text),
            new("heading", "Titre de section", FieldType.Text),
            new("medicalCertificate.badge", "Encart 1 — badge", FieldType.Text),
            new("medicalCertificate.title", "Encart 1 — titre", FieldType.Text),
            new("medicalCertificate.body", "Encart 1 — texte", FieldType.Textarea),
            new("medicalCertificate.cta.label", "Encart 1 — bouton", FieldType.Text),
            new("membership.kicker", "Encart 2 — surtitre", FieldType.Text),
            new("membership.title", "Encart 2 — titre", FieldType.Text),
            new("membership.body", "Encart 2 — texte", FieldType.Textarea),
            new("membership.cta.label", "Encart 2 — lien", FieldType.Text),
            new("merch.kicker", "Encart 3 — surtitre", FieldType.Text),
            new("merch.title", "Encart 3 — titre", FieldType.Text),
            new("merch.body", "Encart 3 — texte", FieldType.Textarea),
            new("merch.cta.label", "Encart 3 — lien", FieldType.Text),
            new("newsletter.title", "Bandeau lettre — titre", FieldType.Text),
            new("newsletter.body", "Bandeau lettre — texte", FieldType.Text),
            new("newsletter.cta.label", "Bandeau lettre — lien", FieldType.Text),
        ]),

        new(SectionKey.PracticalInfo, "Infos pratiques",
            "Horaires, dossier d'adhésion et notes.",
        [
            new("hoursKicker", "Surtitre horaires", FieldType.Text),
            new("hours", "Horaires", FieldType.Hours),
            new("hoursNote", "Note horaires", FieldType.Textarea),
            new("membershipKicker", "Surtitre adhésion", FieldType.Text),
            new("membershipHeading", "Titre adhésion", FieldType.Text),
            new("membershipChecklist", "Pièces à fournir (une par ligne)", FieldType.Lines),
            new("membershipNote", "Note adhésion", FieldType.Textarea),
            new("membershipCta.label", "Bouton adhésion", FieldType.Text),
        ]),

        new(SectionKey.Contact, "Contact", "Coordonnées et texte de la section contact.",
        [
            new("heading", "Titre", FieldType.Text),
            new("paragraph", "Paragraphe", FieldType.Textarea),
            new("email", "Email", FieldType.Text),
            new("address.lines", "Adresse (une ligne par ligne)", FieldType.Lines),
            new("hours.lines", "Ouverture (une ligne par ligne)", FieldType.Lines),
        ]),
    ];

    public static SectionConfig? Find(string? key) =>
        All.FirstOrDefault(section => section.Key == key);
}
