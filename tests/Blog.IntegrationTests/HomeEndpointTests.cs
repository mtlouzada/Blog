using System.Net;
using Blog.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Blog.IntegrationTests;

public class HomeEndpointTests : IntegrationTestBase
{
    public HomeEndpointTests(BlogApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Raiz_deve_responder_com_o_ambiente_configurado()
    {
        var response = await Client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.ReadRawAsync()).Should().Contain("test");
    }
}
