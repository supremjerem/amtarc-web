namespace Amtarc.Web.Domain;

/// <summary>
/// Entities that carry an <see cref="UpdatedAt"/> column maintained by
/// <c>UpdatedAtInterceptor</c> on every insert and update — the .NET equivalent of
/// Prisma's <c>@updatedAt</c>.
/// </summary>
public interface ITimestamped
{
    DateTime UpdatedAt { get; set; }
}
