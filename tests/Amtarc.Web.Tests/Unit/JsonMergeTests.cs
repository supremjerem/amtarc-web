using System.Text.Json.Nodes;
using Amtarc.Web.Services;

namespace Amtarc.Web.Tests.Unit;

/// <summary>
/// The merge rules are a contract with content an admin has already saved, so these cases are
/// ported from the previous site's <c>site-content.test.ts</c> one for one.
/// </summary>
public sealed class JsonMergeTests
{
    [Fact]
    public void Merge_ReturnsDefaults_WhenNothingStored()
    {
        var defaults = JsonNode.Parse("""{"title":"Défaut","cta":{"label":"Voir"}}""");

        var merged = JsonMerge.Merge(defaults, stored: null);

        merged!.ToJsonString().Should().Be(defaults!.ToJsonString());
    }

    [Fact]
    public void Merge_StoredScalarWins_FieldByField()
    {
        var defaults = JsonNode.Parse("""{"title":"Défaut","paragraph":"Inchangé"}""");
        var stored = JsonNode.Parse("""{"title":"Modifié"}""");

        var merged = JsonMerge.Merge(defaults, stored);

        merged!["title"]!.GetValue<string>().Should().Be("Modifié");
        merged["paragraph"]!.GetValue<string>().Should().Be("Inchangé");
    }

    [Fact]
    public void Merge_NestedObjects_KeepDefaultOnlyFields()
    {
        var defaults = JsonNode.Parse("""{"cta":{"href":"#contact","label":"Écrire"}}""");
        var stored = JsonNode.Parse("""{"cta":{"label":"Nous écrire"}}""");

        var merged = JsonMerge.Merge(defaults, stored);

        merged!["cta"]!["href"]!.GetValue<string>().Should().Be("#contact");
        merged["cta"]!["label"]!.GetValue<string>().Should().Be("Nous écrire");
    }

    [Fact]
    public void Merge_ReplacesArraysWhole_RatherThanElementWise()
    {
        var defaults = JsonNode.Parse("""{"lines":["un","deux","trois"]}""");
        var stored = JsonNode.Parse("""{"lines":["seul"]}""");

        var merged = JsonMerge.Merge(defaults, stored);

        merged!["lines"]!.AsArray().Select(n => n!.GetValue<string>()).Should().Equal("seul");
    }

    [Fact]
    public void Merge_DropsStoredKeysTheDefaultsNoLongerHave()
    {
        var defaults = JsonNode.Parse("""{"title":"Titre"}""");
        var stored = JsonNode.Parse("""{"title":"Autre","retired":"valeur orpheline"}""");

        var merged = JsonMerge.Merge(defaults, stored);

        merged!.AsObject().Should().ContainSingle().Which.Key.Should().Be("title");
    }

    [Fact]
    public void Merge_KeepsFieldsAddedToTheDefaultsAfterASectionWasSaved()
    {
        var defaults = JsonNode.Parse("""{"title":"Titre","addedLater":"nouveau"}""");
        var stored = JsonNode.Parse("""{"title":"Enregistré"}""");

        var merged = JsonMerge.Merge(defaults, stored);

        merged!["addedLater"]!.GetValue<string>().Should().Be("nouveau");
    }
}
