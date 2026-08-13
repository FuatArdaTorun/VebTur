namespace VebTur.Application.Admin;

/// <summary>
/// Reconciles an entity collection (e.g. a hotel's images, room types, or supervisors) against
/// a submitted DTO list using each row's Id: a DTO with <c>Id: null</c> becomes a new entity; a
/// DTO with a matching Id updates that entity; any existing entity whose Id is absent from the
/// DTO list is removed. Deliberately generic and dependency-free (no EF Core, no Domain types)
/// so add/update/remove semantics can be unit-tested as pure logic — this exact shape of code
/// (silently dropping or duplicating a row) is an easy place to introduce a subtle bug by hand.
/// </summary>
public static class HotelAggregateReconciler
{
    public static void Reconcile<TEntity, TDto>(
        ICollection<TEntity> existing,
        IReadOnlyList<TDto> incoming,
        Func<TEntity, Guid> getEntityId,
        Func<TDto, Guid?> getDtoId,
        Action<TEntity, TDto> applyToExisting,
        Func<TDto, TEntity> createFromDto)
    {
        var incomingIds = incoming
            .Select(getDtoId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        foreach (var entity in existing.Where(e => !incomingIds.Contains(getEntityId(e))).ToList())
        {
            existing.Remove(entity);
        }

        foreach (var dto in incoming)
        {
            var dtoId = getDtoId(dto);
            var match = dtoId.HasValue ? existing.FirstOrDefault(e => getEntityId(e) == dtoId.Value) : default;

            if (match is not null)
            {
                applyToExisting(match, dto);
            }
            else
            {
                existing.Add(createFromDto(dto));
            }
        }
    }
}
