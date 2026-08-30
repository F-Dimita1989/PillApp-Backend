using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PillApp.Api.Dtos;

namespace PillApp.Api.Tests;

public class ApiIntegrationTests : IClassFixture<PillAppWebApplicationFactory>, IAsyncLifetime
{
    private readonly PillAppWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(PillAppWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.EnsureSeededAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ok", payload.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Root_ListsPublicEndpoints()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PillApp.Api", payload.GetProperty("service").GetString());
    }

    [Fact]
    public async Task Keepalive_WithoutSecret_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/keepalive-db");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Keepalive_WithWrongSecret_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/keepalive-db");
        request.Headers.Add("X-KEEPALIVE", "secret-sbagliato");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_ReturnsPaginatedResults()
    {
        var response = await _client.GetAsync("/api/farmaci/search?q=Paracetamolo&limit=10&offset=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<FarmacoSearchResultDto>();
        Assert.NotNull(body);
        Assert.Equal(10, body.Limit);
        Assert.Equal(0, body.Offset);
        Assert.True(body.Total >= 25);
        Assert.Equal(10, body.Items.Count);
    }

    [Fact]
    public async Task Search_SecondPage_ReturnsRemainingItems()
    {
        var response = await _client.GetAsync("/api/farmaci/search?q=Paracetamolo&limit=10&offset=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<FarmacoSearchResultDto>();
        Assert.NotNull(body);
        Assert.Equal(10, body.Offset);
        Assert.Equal(10, body.Items.Count);
    }

    [Fact]
    public async Task Search_WithTooShortTerm_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/farmaci/search?q=pa");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_SetsCacheControlHeader()
    {
        var response = await _client.GetAsync("/api/farmaci/search?q=Paracetamolo");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.CacheControl);
        Assert.True(response.Headers.CacheControl!.Public);
    }

    [Fact]
    public async Task GetByAic_WithValidCode_ReturnsDrug()
    {
        var response = await _client.GetAsync("/api/farmaci/023076001");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<FarmacoLookupDto>();
        Assert.NotNull(body);
        Assert.Equal("023076001", body.Aic);
        Assert.Equal("Analgesici", body.DescrizioneGruppo);
    }

    [Fact]
    public async Task GetByAic_WithUnknownCode_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/farmaci/000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task WriteVerbs_AreNotExposed(string method)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), "/api/farmaci/023076001")
        {
            Content = JsonContent.Create(new { denominazioneConfezione = "Tentativo di scrittura" })
        };

        var response = await _client.SendAsync(request);

        Assert.True(
            response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed,
            $"{method} ha restituito {(int)response.StatusCode}: l'API deve essere di sola lettura.");
    }
}
