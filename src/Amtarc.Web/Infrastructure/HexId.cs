namespace Amtarc.Web.Infrastructure;

/// <summary>
/// Unique ids for inline SVG <c>&lt;defs&gt;</c>. SVG def ids are global to the document, so the
/// honeycomb pattern — rendered several times per page — needs a fresh one each time. This is the
/// server-rendered stand-in for React's <c>useId</c>.
/// </summary>
public static class HexId
{
    private static int _counter;

    public static string Next(string prefix) =>
        $"{prefix}-{Interlocked.Increment(ref _counter)}";
}
