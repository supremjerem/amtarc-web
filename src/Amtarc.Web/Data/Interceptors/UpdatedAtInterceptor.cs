using Amtarc.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Amtarc.Web.Data.Interceptors;

/// <summary>
/// Stamps <see cref="ITimestamped.UpdatedAt"/> on every insert and update — the .NET
/// equivalent of Prisma's <c>@updatedAt</c>, which EF Core has no built-in support for.
/// </summary>
public sealed class UpdatedAtInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = DateTime.UtcNow;

        foreach (EntityEntry entry in context.ChangeTracker.Entries())
        {
            if (entry is { Entity: ITimestamped timestamped, State: EntityState.Added or EntityState.Modified })
            {
                timestamped.UpdatedAt = now;
            }
        }
    }
}
