namespace API.Stories.Application.Ports;

public interface ICacheService
{
    Task<T> GetAsync<T>(string key)
        where T : class;

    Task<T> SetAsync<T>(T element, string key);
}