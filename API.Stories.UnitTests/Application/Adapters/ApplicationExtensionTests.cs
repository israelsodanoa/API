using API.Stories.Application.Adapters;
using API.Stories.Application.Adapters.HttpClients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API.Stories.UnitTests.Application.Adapters;

public sealed class ApplicationExtensionTests
{
    [Fact]
    public void GetSettings_ReturnsBoundObject()
    {
        var data = new Dictionary<string, string>
        {
            [$"{nameof(HackerNewsHttpClientSettings)}:Host"] = "https://hn.test",
            [$"{nameof(HackerNewsHttpClientSettings)}:Retries"] = "5",
            [$"{nameof(HackerNewsHttpClientSettings)}:TimeoutMs"] = "15000"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();

        var settings = configuration.GetSettings<HackerNewsHttpClientSettings>();

        Assert.NotNull(settings);
        Assert.Equal("https://hn.test", settings.Host);
        Assert.Equal(5, settings.Retries);
        Assert.Equal(15000, settings.TimeoutMs);
    }

    [Fact]
    public void ConfigureApplication_ThrowsWhenRedisConnectionStringMissing()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(() => services.ConfigureApplication(configuration));
    }
}
