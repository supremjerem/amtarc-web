using System.Text.Json.Nodes;

namespace Amtarc.Web.Services;

/// <summary>
/// Deep-merges an admin's stored section JSON over the built-in defaults. Port of the previous
/// site's <c>mergeSection</c>, and it keeps the same three rules — they are what make a stored
/// section survive the code's defaults changing under it:
///
/// <list type="bullet">
///   <item>the walk is driven by the <em>defaults'</em> shape, so a key added to the defaults
///     after a section was saved still renders, and a stored key the code no longer knows about
///     is dropped;</item>
///   <item>arrays are replaced whole, never merged element-wise — an admin who removes a line
///     from a list means to remove it;</item>
///   <item>anything else (scalars, type mismatches) is won by the stored value.</item>
/// </list>
/// </summary>
public static class JsonMerge
{
    public static JsonNode? Merge(JsonNode? defaults, JsonNode? stored)
    {
        if (stored is null)
        {
            return defaults?.DeepClone();
        }

        if (defaults is JsonObject defaultObject && stored is JsonObject storedObject)
        {
            var merged = new JsonObject();
            foreach (var (key, defaultValue) in defaultObject)
            {
                storedObject.TryGetPropertyValue(key, out var storedValue);
                merged[key] = Merge(defaultValue, storedValue);
            }

            return merged;
        }

        return stored.DeepClone();
    }
}
