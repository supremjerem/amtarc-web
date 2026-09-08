using System.ComponentModel.DataAnnotations;

namespace Amtarc.Web.Options;

/// <summary>
/// Where uploaded images are written and how they are addressed publicly. Full validation
/// (refuse to boot on a bad value) arrives with the hardening phase.
/// </summary>
public sealed class UploadOptions
{
    public const string SectionName = "Upload";

    /// <summary>
    /// Absolute or content-root-relative directory. Deliberately outside <c>wwwroot</c>: uploads
    /// are data, and a directory the static-file middleware serves wholesale is a place where a
    /// file that slips past validation becomes reachable.
    /// </summary>
    [Required(ErrorMessage = "Upload:Directory is required.")]
    public string Directory { get; set; } = "App_Data/uploads";

    /// <summary>5 MiB, the limit the previous site enforced.</summary>
    [Range(1024, 100L * 1024 * 1024,
        ErrorMessage = "Upload:MaxBytes must be between 1 KiB and 100 MiB.")]
    public long MaxBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>
    /// Origin prefixed to stored image URLs. Empty means site-relative, which is what a
    /// single-origin deployment wants; the previous site needed an absolute URL only because the
    /// API answered on a different origin than the pages.
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
}
