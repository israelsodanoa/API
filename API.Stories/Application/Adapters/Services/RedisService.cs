using System.Text.Json;
using API.Stories.Application.Ports;
using StackExchange.Redis;

namespace API.Stories.Application.Adapters.Services;

public sealed class RedisService(IConnectionMultiplexer connection) : ICacheService
{
    private readonly IConnectionMultiplexer __connection = connection;

    public async Task<T> GetAsync<T>(string key)
        where T : class
    {
        try
        {
            var db = __connection.GetDatabase();
            var value = await db.StringGetAsync(key);
            var str = value.ToString();

            var parsed = JsonSerializer.Deserialize<T>(str);
            return parsed;
        }
        catch
        {
            return null;
        }
    }

    public async Task<T> SetAsync<T>(T element, string key)
    {
        var value = JsonSerializer.Serialize(element);
        var db = __connection.GetDatabase();
        await db.StringSetAsync(key, value);
        return element;
    }
}