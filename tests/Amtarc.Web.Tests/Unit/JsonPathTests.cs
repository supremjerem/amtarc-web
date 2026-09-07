using System.Text.Json.Nodes;
using Amtarc.Web.Services;

namespace Amtarc.Web.Tests.Unit;

/// <summary>
/// The admin form is a flat list of dotted paths; these are the rules that map it onto the
/// nested section document. Ported from the previous site's <c>getAtPath</c> / <c>setAtPath</c>.
/// </summary>
public sealed class JsonPathTests
{
    [Fact]
    public void Get_ReadsANestedValue()
    {
        var document = JsonNode.Parse("""{"ctaPrimary":{"label":"Nous écrire"}}""");

        JsonPath.Get(document, "ctaPrimary.label")!.GetValue<string>().Should().Be("Nous écrire");
    }

    [Theory]
    [InlineData("absent")]
    [InlineData("ctaPrimary.absent")]
    [InlineData("absent.deeper")]
    public void Get_ReturnsNull_ForAPathThatIsNotThere(string path)
    {
        var document = JsonNode.Parse("""{"ctaPrimary":{"label":"Nous écrire"}}""");

        JsonPath.Get(document, path).Should().BeNull();
    }

    [Fact]
    public void Set_WritesANestedValue_LeavingItsSiblingsAlone()
    {
        var document = JsonNode.Parse("""{"cta":{"href":"#contact","label":"Écrire"}}""")!.AsObject();

        JsonPath.Set(document, "cta.label", "Nous écrire");

        document["cta"]!["label"]!.GetValue<string>().Should().Be("Nous écrire");
        document["cta"]!["href"]!.GetValue<string>().Should().Be("#contact");
    }

    [Fact]
    public void Set_CreatesTheIntermediateObjects_WhenTheBranchIsMissing()
    {
        // A section saved before a field was added has no branch for it yet.
        var document = new JsonObject();

        JsonPath.Set(document, "newsletter.cta.label", "S'inscrire");

        document["newsletter"]!["cta"]!["label"]!.GetValue<string>().Should().Be("S'inscrire");
    }

    [Fact]
    public void Set_ReplacesAScalarStandingWhereAnObjectBelongs()
    {
        var document = JsonNode.Parse("""{"cta":"ancienne valeur"}""")!.AsObject();

        JsonPath.Set(document, "cta.label", "Écrire");

        document["cta"]!["label"]!.GetValue<string>().Should().Be("Écrire");
    }

    [Fact]
    public void Set_WritesAtTheRoot_ForASingleSegmentPath()
    {
        var document = JsonNode.Parse("""{"title":"Ancien"}""")!.AsObject();

        JsonPath.Set(document, "title", "Nouveau");

        document["title"]!.GetValue<string>().Should().Be("Nouveau");
    }

    [Fact]
    public void Set_StoresAnArrayWhole()
    {
        var document = new JsonObject();

        JsonPath.Set(document, "address.lines", new JsonArray("Stand de Chapas", "82290 Meauzac"));

        document["address"]!["lines"]!.AsArray().Should().HaveCount(2);
    }
}
