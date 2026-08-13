using VebTur.Application.Admin;

namespace VebTur.UnitTests.Admin;

public class HotelAggregateReconcilerTests
{
    private record FakeDto(Guid? Id, string Value);

    private class FakeEntity
    {
        public Guid Id { get; init; }
        public required string Value { get; set; }
    }

    private static void Reconcile(List<FakeEntity> existing, IReadOnlyList<FakeDto> incoming) =>
        HotelAggregateReconciler.Reconcile(
            existing,
            incoming,
            getEntityId: e => e.Id,
            getDtoId: d => d.Id,
            applyToExisting: (e, d) => e.Value = d.Value,
            createFromDto: d => new FakeEntity { Id = Guid.CreateVersion7(), Value = d.Value });

    [Fact]
    public void NewDto_WithNullId_IsAdded()
    {
        List<FakeEntity> existing = [];

        Reconcile(existing, [new FakeDto(null, "new-row")]);

        var added = Assert.Single(existing);
        Assert.Equal("new-row", added.Value);
        Assert.NotEqual(Guid.Empty, added.Id);
    }

    [Fact]
    public void ExistingDto_WithMatchingId_IsUpdatedInPlace()
    {
        var id = Guid.CreateVersion7();
        List<FakeEntity> existing = [new FakeEntity { Id = id, Value = "old" }];

        Reconcile(existing, [new FakeDto(id, "updated")]);

        var only = Assert.Single(existing);
        Assert.Same(existing[0], only);
        Assert.Equal(id, only.Id);
        Assert.Equal("updated", only.Value);
    }

    [Fact]
    public void ExistingEntity_AbsentFromIncomingList_IsRemoved()
    {
        var keepId = Guid.CreateVersion7();
        var removeId = Guid.CreateVersion7();
        List<FakeEntity> existing =
        [
            new FakeEntity { Id = keepId, Value = "keep" },
            new FakeEntity { Id = removeId, Value = "remove-me" },
        ];

        Reconcile(existing, [new FakeDto(keepId, "keep")]);

        var only = Assert.Single(existing);
        Assert.Equal(keepId, only.Id);
    }

    [Fact]
    public void EmptyIncomingList_RemovesAllExisting()
    {
        List<FakeEntity> existing =
        [
            new FakeEntity { Id = Guid.CreateVersion7(), Value = "a" },
            new FakeEntity { Id = Guid.CreateVersion7(), Value = "b" },
        ];

        Reconcile(existing, []);

        Assert.Empty(existing);
    }

    [Fact]
    public void MixedAddUpdateRemove_ProducesExpectedFinalSet()
    {
        var keepAndUpdateId = Guid.CreateVersion7();
        var removeId = Guid.CreateVersion7();
        List<FakeEntity> existing =
        [
            new FakeEntity { Id = keepAndUpdateId, Value = "old-value" },
            new FakeEntity { Id = removeId, Value = "goes-away" },
        ];

        Reconcile(existing,
        [
            new FakeDto(keepAndUpdateId, "new-value"),
            new FakeDto(null, "brand-new"),
        ]);

        Assert.Equal(2, existing.Count);
        Assert.Contains(existing, e => e.Id == keepAndUpdateId && e.Value == "new-value");
        Assert.Contains(existing, e => e.Value == "brand-new" && e.Id != keepAndUpdateId && e.Id != removeId);
        Assert.DoesNotContain(existing, e => e.Id == removeId);
    }

    [Fact]
    public void DtoWithUnknownId_IsTreatedAsNewRow()
    {
        // An Id that doesn't match any existing entity (e.g. a stale/foreign Id) should not
        // silently no-op — it must still result in a new row, not a dropped update.
        List<FakeEntity> existing = [new FakeEntity { Id = Guid.CreateVersion7(), Value = "unrelated" }];
        var unknownId = Guid.CreateVersion7();

        Reconcile(existing, [new FakeDto(unknownId, "orphaned-update")]);

        Assert.Single(existing);
        Assert.Equal("orphaned-update", existing[0].Value);
    }
}
