using System.Text.Json.Nodes;

namespace Amtarc.Web.Services;

/// <summary>
/// Reads and writes a dotted path such as <c>ctaPrimary.label</c> inside a section's JSON.
/// Port of <c>getAtPath</c> / <c>setAtPath</c> from the previous site's admin-content module:
/// the admin form is a flat list of paths, and this is what maps it onto the nested document.
/// </summary>
public static class JsonPath
{
    public static JsonNode? Get(JsonNode? source, string path)
    {
        var current = source;

        foreach (var segment in path.Split('.'))
        {
            if (current is not JsonObject obj || !obj.TryGetPropertyValue(segment, out current))
            {
                return null;
            }
        }

        return current;
    }

    /// <summary>
    /// Sets <paramref name="value"/> at <paramref name="path"/>, creating intermediate objects as
    /// needed. Mutates <paramref name="target"/>.
    /// </summary>
    public static void Set(JsonObject target, string path, JsonNode? value)
    {
        var segments = path.Split('.');
        var current = target;

        for (var i = 0; i < segments.Length - 1; i++)
        {
            var segment = segments[i];

            // A path segment that holds a scalar today is replaced by an object: the field config
            // is the authority on the shape, not whatever happens to be stored.
            if (current[segment] is not JsonObject next)
            {
                next = [];
                current[segment] = next;
            }

            current = next;
        }

        current[segments[^1]] = value;
    }
}
