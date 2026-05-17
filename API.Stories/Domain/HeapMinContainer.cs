namespace API.Stories.Domain;

public sealed record HeapMinContainer<T>(int Capacity)
{
    private readonly PriorityQueue<T, int> queue = new();

    public void Add(T e, int p)
    {
        lock (queue)
        {
            queue.Enqueue(e, p);
            if (queue.Count >= Capacity)
                queue.Dequeue();
        }
    }

    public IEnumerable<T> AsEnumerable()
    {
        lock (queue)
        {
            while (queue.Count > 0)
                yield return queue.Dequeue();
        }
    }
}