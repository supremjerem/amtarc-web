using System.Text.Json.Nodes;
using Amtarc.Web.Content;
using Amtarc.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Amtarc.Web.Pages.Admin.Content;

/// <summary>
/// Edits one section. The form is a flat list of dotted paths taken from
/// <see cref="SectionFieldConfig"/>; on save each one is written back into the section's JSON.
/// Everything works without JavaScript, including adding and removing schedule rows — those are
/// ordinary posts that re-render the form.
/// </summary>
public class EditModel(ISiteContentService siteContent) : PageModel
{
    public SectionConfig Section { get; private set; } = null!;

    /// <summary>
    /// Path → raw value, for every field except the schedule rows. Read straight out of the form
    /// rather than model-bound: the names are built from the field config at render time, and the
    /// dictionary binder does not handle a post that carries none of them.
    /// </summary>
    public Dictionary<string, string> Fields { get; private set; } = [];

    [BindProperty]
    public List<HourRowInput> HourRows { get; set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string key, CancellationToken cancellationToken)
    {
        if (!TryLoadSection(key))
        {
            return NotFound();
        }

        Populate(await siteContent.GetSectionAsync(key, cancellationToken));
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string key, CancellationToken cancellationToken)
    {
        if (!TryLoadSection(key))
        {
            return NotFound();
        }

        ReadPostedFields();

        // Start from what the section currently renders, so fields this form does not expose keep
        // their value instead of being dropped.
        var document = await siteContent.GetSectionAsync(key, cancellationToken);

        foreach (var field in Section.Fields)
        {
            JsonPath.Set(document, field.Path, ValueFor(field));
        }

        await siteContent.UpsertAsync(key, document, cancellationToken);

        StatusMessage = $"« {Section.Title} » enregistrée.";
        return RedirectToPage("Index");
    }

    public async Task<IActionResult> OnPostResetAsync(string key, CancellationToken cancellationToken)
    {
        if (!TryLoadSection(key))
        {
            return NotFound();
        }

        await siteContent.ResetAsync(key, cancellationToken);

        StatusMessage = $"« {Section.Title} » réinitialisée : la section affiche de nouveau le contenu par défaut.";
        return RedirectToPage("Index");
    }

    /// <summary>Adds an empty schedule row and redisplays; nothing is saved yet.</summary>
    public IActionResult OnPostAddRow(string key, string path)
    {
        if (!TryLoadSection(key))
        {
            return NotFound();
        }

        ReadPostedFields();

        HourRows.Add(new HourRowInput { Path = path });
        return Page();
    }

    public IActionResult OnPostRemoveRow(string key, int index)
    {
        if (!TryLoadSection(key))
        {
            return NotFound();
        }

        ReadPostedFields();

        if (index >= 0 && index < HourRows.Count)
        {
            HourRows.RemoveAt(index);
        }

        return Page();
    }

    /// <summary>The schedule rows belonging to one field, with their index in the bound list.</summary>
    public IEnumerable<(int Index, HourRowInput Row)> RowsFor(string path) =>
        HourRows.Index().Where(entry => entry.Item.Path == path);

    private bool TryLoadSection(string key)
    {
        var section = SectionFieldConfig.Find(key);
        if (section is null)
        {
            return false;
        }

        Section = section;
        return true;
    }

    /// <summary>Pulls the <c>Fields[some.path]</c> entries out of the posted form.</summary>
    private void ReadPostedFields()
    {
        const string Prefix = "Fields[";

        foreach (var (name, value) in Request.Form)
        {
            if (name.StartsWith(Prefix, StringComparison.Ordinal)
                && name.EndsWith(']'))
            {
                Fields[name[Prefix.Length..^1]] = value.ToString();
            }
        }
    }

    private void Populate(JsonObject document)
    {
        foreach (var field in Section.Fields)
        {
            var value = JsonPath.Get(document, field.Path);

            switch (field.Type)
            {
                case FieldType.Hours:
                    foreach (var row in value?.AsArray() ?? [])
                    {
                        HourRows.Add(new HourRowInput
                        {
                            Path = field.Path,
                            Day = row?["day"]?.GetValue<string>() ?? string.Empty,
                            Time = row?["time"]?.GetValue<string>() ?? string.Empty,
                        });
                    }

                    break;

                case FieldType.Lines:
                    Fields[field.Path] = string.Join(
                        '\n',
                        (value?.AsArray() ?? []).Select(line => line?.GetValue<string>() ?? string.Empty));
                    break;

                default:
                    Fields[field.Path] = value?.GetValue<string>() ?? string.Empty;
                    break;
            }
        }
    }

    private JsonNode? ValueFor(FieldConfig field)
    {
        switch (field.Type)
        {
            case FieldType.Hours:
                var rows = new JsonArray();
                foreach (var (_, row) in RowsFor(field.Path))
                {
                    // A row the admin left completely blank is a row they did not mean to add.
                    if (string.IsNullOrWhiteSpace(row.Day) && string.IsNullOrWhiteSpace(row.Time))
                    {
                        continue;
                    }

                    rows.Add(new JsonObject
                    {
                        ["day"] = row.Day?.Trim() ?? string.Empty,
                        ["time"] = row.Time?.Trim() ?? string.Empty,
                    });
                }

                return rows;

            case FieldType.Lines:
                var lines = new JsonArray();
                var raw = Fields.GetValueOrDefault(field.Path) ?? string.Empty;
                foreach (var line in raw.ReplaceLineEndings("\n").Split('\n'))
                {
                    // Blank lines are formatting in the textarea, not entries in the list.
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        lines.Add(line.Trim());
                    }
                }

                return lines;

            default:
                return Fields.GetValueOrDefault(field.Path) ?? string.Empty;
        }
    }
}

public sealed class HourRowInput
{
    /// <summary>Which <see cref="FieldConfig.Path"/> this row belongs to.</summary>
    public string Path { get; set; } = string.Empty;

    public string? Day { get; set; }

    public string? Time { get; set; }
}
