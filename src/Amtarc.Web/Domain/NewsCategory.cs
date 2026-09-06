using NpgsqlTypes;

namespace Amtarc.Web.Domain;

/// <summary>
/// News item category. Backed by a native PostgreSQL enum type <c>"NewsCategory"</c> whose
/// labels are the uppercase French names carried over from the previous (Prisma) schema.
/// </summary>
public enum NewsCategory
{
    [PgName("CONCOURS")]
    Concours,

    [PgName("TRAVAUX")]
    Travaux,

    [PgName("EVENEMENT")]
    Evenement,
}
