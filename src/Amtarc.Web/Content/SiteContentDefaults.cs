namespace Amtarc.Web.Content;

/// <summary>
/// Every piece of built-in site copy. The four editable sections (<see cref="Hero"/>,
/// <see cref="Announcements"/>, <see cref="PracticalInfo"/>, <see cref="Contact"/>) are the
/// fallback an admin's stored JSON is merged over; the rest is code-only.
///
/// Visitor-facing strings are French — the club's audience — while identifiers and comments
/// stay English.
/// </summary>
public static class SiteContentDefaults
{
    public static readonly IReadOnlyList<NavLink> NavLinks =
    [
        new("#club", "Le club"),
        new("#disciplines", "Disciplines"),
        new("#tsv", "TSV"),
        new("#actus", "Actualités"),
        new("#infos", "Infos pratiques"),
    ];

    public static readonly IReadOnlyList<SocialLink> SocialLinks =
    [
        new(SocialPlatform.Facebook, "Facebook",
            "https://fr-fr.facebook.com/TSV-Amtarc-953914188035594/", External: true, ComingSoon: false),
        new(SocialPlatform.Instagram, "Instagram",
            "https://www.instagram.com/ipsc.amtarc/", External: true, ComingSoon: false),
        new(SocialPlatform.TikTok, "TikTok", "#", External: false, ComingSoon: true),
        new(SocialPlatform.YouTube, "YouTube", "#", External: false, ComingSoon: true),
    ];

    public static readonly HeroContent Hero = new()
    {
        Badge = "CLUB DE TIR · MEAUZAC · TARN-ET-GARONNE",
        Wordmark = "AMTARC",
        Title = "La précision\nne s'improvise pas.",
        Paragraph =
            "Association meauzacaise de tireurs aux armes rayées et de chasse. Du 10 mètres au Tir "
            + "Sportif de Vitesse, la passion du geste juste depuis le stand de Chapas.",
        CtaPrimary = new("#disciplines", "Découvrir le club"),
        CtaSecondary = new("#tsv", "La section TSV →"),
        ScrollHint = "DÉFILER",
    };

    public static readonly AnnouncementsContent Announcements = new()
    {
        Kicker = "À NE PAS MANQUER",
        Heading = "Les messages du club.",
        MoreLink = new("#actus", "Toutes les actus →"),
        MedicalCertificate = new()
        {
            Badge = "OBLIGATOIRE",
            Title = "Certificat médical 2025 / 2026",
            Body =
                "Un certificat médical est requis pour tous les licenciés. Téléchargez le modèle "
                + "officiel, faites-le compléter et signer par votre médecin avant la reprise.",
            Cta = new("#contact", "Télécharger le modèle ↓"),
        },
        Membership = new()
        {
            Kicker = "SAISON",
            Title = "Adhésions 2026 / 2027",
            Body =
                "Les inscriptions ouvrent chaque année à la rentrée. Dossier, cotisation et pièces "
                + "à préparer en amont.",
            Cta = new("#infos", "Voir les conditions →"),
        },
        Merch = new()
        {
            Kicker = "BOUTIQUE",
            Title = "Aux couleurs de l'AMTARC",
            Body =
                "Vestes, polos, sweats, t-shirts et casquettes au logo du club. Livraison gratuite "
                + "au club.",
            Cta = new("#contact", "Commander →"),
        },
        Newsletter = new()
        {
            Title = "« Chapas News » — la lettre du club",
            Body =
                "Concours, travaux, bourse aux armes : toute la vie du stand, saison après saison.",
            Cta = new("#actus", "Lire →"),
        },
    };

    public static readonly IReadOnlyList<ClubStat> ClubStats =
    [
        new(5, "disciplines de tir", StatVariant.Light),
        new(10, "alvéoles TSV", StatVariant.Gold),
        new(200, "distance maximale", StatVariant.Light, Suffix: " m"),
        new(3, "créneaux / semaine", StatVariant.Dark),
    ];

    public static readonly ClubIntro ClubIntro = new()
    {
        Kicker = "LE CLUB DE CHAPAS",
        Heading = "Un stand complet,\nune communauté sérieuse.",
        Paragraph =
            "Niché au lieu-dit Chapas à Meauzac, l'AMTARC réunit tireurs sportifs, passionnés de "
            + "poudre noire et compétiteurs TSV autour d'une même exigence : la sécurité et la "
            + "précision. Cinq disciplines, des installations dédiées et un encadrement fédéral.",
        Features = ["Encadrement fédéral", "Moniteurs & arbitres diplômés", "École de tir"],
    };

    public static readonly SectionHeading DisciplinesHeading =
        new("NOS DISCIPLINES", "Cinq façons de viser juste.");

    public static readonly IReadOnlyList<DisciplineCard> Disciplines =
    [
        new()
        {
            Id = "tsv",
            Size = DisciplineSize.Hero,
            Badge = "DISCIPLINE PHARE",
            Title = "Tir Sportif de Vitesse",
            Body =
                "Né aux États-Unis (IPSC), le TSV enchaîne des parcours chronométrés au gros "
                + "calibre, arme au holster. Puissance, mouvement et sang-froid.",
            Href = "#tsv",
            Cta = "Explorer la section TSV →",
        },
        new()
        {
            Id = "10m",
            Size = DisciplineSize.Small,
            Overline = "ISSF",
            Title = "10 mètres",
            Body = "Carabine & pistolet à air comprimé. Le geste de base, à la perfection.",
        },
        new()
        {
            Id = "25m",
            Size = DisciplineSize.Small,
            Overline = "ISSF",
            Title = "25 mètres",
            Body = "Pistolet vitesse et précision. Rythme et régularité.",
        },
        new()
        {
            Id = "50-100m",
            Size = DisciplineSize.Wide,
            Overline = "CARABINE",
            Title = "50 – 100 mètres",
            Body =
                "Le tir à distance, où la lecture du vent et la maîtrise du souffle font la "
                + "différence.",
            BigLabel = "100m",
        },
        new()
        {
            Id = "200m",
            Size = DisciplineSize.Small,
            Overline = "LONGUE DISTANCE",
            Title = "200 mètres",
            Body = "Notre plus longue ligne de tir. L'exigence absolue.",
        },
        new()
        {
            Id = "black-powder",
            Size = DisciplineSize.Full,
            Overline = "TRADITION",
            Title = "Poudre noire",
            Body =
                "Armes anciennes et répliques historiques : le tir dans sa forme la plus "
                + "authentique, entre fumée, rituel et patience.",
            Emoji = "🔥",
        },
    ];

    public static readonly TsvShowcaseContent TsvShowcase = new()
    {
        Badge = "★ NOTRE FIERTÉ",
        Heading = "Le TSV,\nnotre signature.",
        Paragraphs =
        [
            "Le Tir Sportif de Vitesse propose des actions chronométrées au pistolet de gros "
            + "calibre — jamais inférieur à 9 mm — arme portée dans un étui à la ceinture. Une "
            + "discipline spectaculaire, née de l'IPSC américain.",
            "Le club dispose d'une dizaine d'alvéoles de tailles variées, bordées de buttes de "
            + "terre qui garantissent la sécurité. Un plateau technique rare, encadré par des "
            + "moniteurs et arbitres fédéraux.",
        ],
        Stats =
        [
            new("≥9 mm", "gros calibre"),
            new("HOLSTER", "départ à la ceinture"),
            new("CHRONO", "classement au temps"),
        ],
        CtaPrimary = new("#infos", "Découvrir l'entraînement"),
        CtaSecondary = new("#contact", "Venir essayer"),
    };

    public static readonly SectionHeading NewsHeading =
        new("VIE DU CLUB", "Ce qui se passe à Chapas.");

    public static readonly PracticalInfoContent PracticalInfo = new()
    {
        HoursKicker = "HORAIRES D'OUVERTURE",
        Hours =
        [
            new("Lundi", "14 h 00 – 18 h 00"),
            new("Mercredi", "14 h 00 – 18 h 00"),
            new("Samedi", "14 h 00 – 18 h 00"),
        ],
        HoursNote =
            "Saison sportive du 1ᵉʳ septembre au 31 août. Inscription obligatoire au cahier de "
            + "présence à chaque venue.",
        MembershipKicker = "DOSSIER D'ADHÉSION",
        MembershipHeading = "À prévoir pour votre licence",
        MembershipChecklist =
        [
            "Certificat médical",
            "Photos d'identité",
            "Pièce d'identité",
            "Extrait casier judiciaire 3",
            "Justificatif de domicile",
            "Cotisation annuelle",
        ],
        MembershipNote =
            "Un droit d'entrée est demandé à tout nouvel adhérent. La cotisation est fixée par le "
            + "bureau avant chaque saison.",
        MembershipCta = new("#contact", "Nous contacter pour adhérer →"),
    };

    public static readonly ContactContent Contact = new()
    {
        Heading = "Venez tirer avec nous.",
        Paragraph =
            "Débutant curieux ou tireur confirmé, poussez la porte du stand de Chapas. On vous "
            + "explique tout — de la sécurité au premier plomb.",
        Email = "contact@amtarc.fr",
        Address = new()
        {
            Kicker = "ADRESSE",
            Lines = ["AMTARC — lieu-dit « Chapas »", "82290 Meauzac, Tarn-et-Garonne"],
        },
        Hours = new()
        {
            Kicker = "OUVERTURE",
            Lines = ["Lundi · Mercredi · Samedi", "14 h 00 – 18 h 00"],
        },
    };

    public static readonly FooterContent Footer = new()
    {
        Tagline =
            "Association meauzacaise de tireurs aux armes rayées et de chasse. Stand de tir de "
            + "Chapas, Meauzac (82).",
        Columns =
        [
            new("DISCIPLINES",
            [
                new("#disciplines", "10 mètres"),
                new("#disciplines", "25 mètres"),
                new("#disciplines", "50 – 200 mètres"),
                new("#disciplines", "Poudre noire"),
                new("#tsv", "Tir Sportif de Vitesse"),
            ]),
            new("LE CLUB",
            [
                new("#club", "Présentation"),
                new("#infos", "Horaires"),
                new("#infos", "Adhésion"),
                new("#actus", "Actualités"),
            ]),
            new("FÉDÉRATIONS",
            [
                new("http://www.fftir.org", "Fédération Française de Tir", External: true),
                new("http://www.liguetirmidipyrenees.fr/", "Ligue Midi-Pyrénées", External: true),
                new("mailto:contact@amtarc.fr", "contact@amtarc.fr"),
            ]),
        ],
        Location = "Meauzac · Tarn-et-Garonne · Occitanie",
    };
}
