using Amtarc.Web.Services;

namespace Amtarc.Web.Tests.Unit;

public sealed class SlugGeneratorTests
{
    [Theory]
    [InlineData("Bourse aux armes", "bourse-aux-armes")]
    [InlineData("Concours interne de fin de saison", "concours-interne-de-fin-de-saison")]
    [InlineData("Résultats du concours d'été", "resultats-du-concours-d-ete")]
    [InlineData("  Trim   &   collapse  ", "trim-collapse")]
    [InlineData("Déjà-vu: l'accent!", "deja-vu-l-accent")]
    [InlineData("ÉÈÊ ÀÂ ÔÖ ÜÛ Ç", "eee-aa-oo-uu-c")]
    public void Slugify_StripsDiacriticsLowercasesAndHyphenates(string input, string expected)
    {
        SlugGenerator.Slugify(input).Should().Be(expected);
    }

    [Fact]
    public void Slugify_DropsLeadingAndTrailingSeparators()
    {
        SlugGenerator.Slugify("--hello world--").Should().Be("hello-world");
    }

    [Fact]
    public void Slugify_KeepsDigits()
    {
        SlugGenerator.Slugify("Certificat médical 2025 / 2026").Should().Be("certificat-medical-2025-2026");
    }
}
