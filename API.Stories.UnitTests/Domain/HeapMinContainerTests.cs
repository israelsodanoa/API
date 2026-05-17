using API.Stories.Domain;

namespace API.Stories.UnitTests.Domain;

public sealed class HeapMinContainerTests
{
    [Fact]
    public void AsEnumerable_ReturnsItemsInPriorityOrder_WhenCapacityNotReached()
    {
        var heap = new HeapMinContainer<string>(4);

        heap.Add("high", 40);
        heap.Add("low", 10);
        heap.Add("mid", 30);

        var items = heap.AsEnumerable().ToArray();

        Assert.Equal(new[] { "low", "mid", "high" }, items);
    }

    [Fact]
    public void Add_RemovesSmallestWhenCountReachesCapacity()
    {
        var heap = new HeapMinContainer<int>(3);

        heap.Add(1, 1);
        heap.Add(2, 2);
        heap.Add(3, 3);
        heap.Add(4, 4);

        var items = heap.AsEnumerable().ToArray();

        Assert.Equal(new[] { 3, 4 }, items);
    }

    [Fact]
    public void AsEnumerable_EmptiesContainer()
    {
        var heap = new HeapMinContainer<int>(5);
        heap.Add(1, 1);

        _ = heap.AsEnumerable().ToArray();
        var secondRead = heap.AsEnumerable().ToArray();

        Assert.Empty(secondRead);
    }
}
