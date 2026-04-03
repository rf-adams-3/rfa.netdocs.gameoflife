using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameOfLife.Api.Models.Requests;
using GameOfLife.Api.Models.Responses;
using GameOfLife.Api.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameOfLife.Tests;

public class BoardsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public BoardsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = CreateClient();
    }

    private HttpClient CreateClient(Dictionary<string, string?>? configOverrides = null)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            if (configOverrides is not null)
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(configOverrides));

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<GameOfLifeDbContext>));
                if (descriptor is not null)
                    services.Remove(descriptor);

                // use a unique DB name per CreateClient() call so each client gets an isolated database
                var dbName = $"IntegrationTest_{Guid.NewGuid()}";
                services.AddDbContext<GameOfLifeDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task CreateBoard_ValidRequest_Returns201WithBoardId()
    {
        var request = new CreateBoardRequest { Cells = [[1,2],[2,2],[3,2]] };

        var response = await _client.PostAsJsonAsync("/api/boards", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BoardResponse>(WebOptions);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal(3, body.Cells.Length);
    }

    [Fact]
    public async Task CreateBoard_InvalidPair_Returns400()
    {
        // cell with 1 elements instead of 2
        var json = """{"cells":[[1]]}""";
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/boards", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdvanceNext_UnknownId_Returns404()
    {
        var response = await _client.PostAsync($"/api/boards/{Guid.NewGuid()}/next", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdvanceNext_KnownId_Returns200WithNextGeneration()
    {
        // Vertical blinker
        var createRequest = new CreateBoardRequest { Cells = [[1,2],[2,2],[3,2]] };
        var created = await PostBoardAsync(createRequest);

        var response = await _client.PostAsync($"/api/boards/{created.Id}/next", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var next = await response.Content.ReadFromJsonAsync<BoardResponse>(WebOptions);
        Assert.NotNull(next);
        // should flip to horizontal blinker
        Assert.Equal(3, next.Cells.Length);
        Assert.Contains(next.Cells, c => c[0] == 2 && c[1] == 1);
        Assert.Contains(next.Cells, c => c[0] == 2 && c[1] == 2);
        Assert.Contains(next.Cells, c => c[0] == 2 && c[1] == 3);
    }

    [Fact]
    public async Task AdvanceNext_MutatesStoredState()
    {
        var createRequest = new CreateBoardRequest { Cells = [[1,2],[2,2],[3,2]] };
        var created = await PostBoardAsync(createRequest);

        // two advances should return to original orientation
        await _client.PostAsync($"/api/boards/{created.Id}/next", null);
        var secondResponse = await _client.PostAsync($"/api/boards/{created.Id}/next", null);
        var secondState = await secondResponse.Content.ReadFromJsonAsync<BoardResponse>(WebOptions);

        Assert.NotNull(secondState);
        Assert.Contains(secondState.Cells, c => c[0] == 1 && c[1] == 2);
        Assert.Contains(secondState.Cells, c => c[0] == 2 && c[1] == 2);
        Assert.Contains(secondState.Cells, c => c[0] == 3 && c[1] == 2);
    }

    [Fact]
    public async Task AdvanceN_ValidN_Returns200()
    {
        var created = await PostBoardAsync(new CreateBoardRequest { Cells = [[0,0],[0,1],[1,0],[1,1]] });

        var response = await _client.PostAsync($"/api/boards/{created.Id}/next/3", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdvanceN_NIsZero_Returns400()
    {
        var created = await PostBoardAsync(new CreateBoardRequest { Cells = [[0,0],[0,1],[1,0],[1,1]] });

        var response = await _client.PostAsync($"/api/boards/{created.Id}/next/0", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdvanceN_UnknownId_Returns404()
    {
        var response = await _client.PostAsync($"/api/boards/{Guid.NewGuid()}/next/5", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdvanceN_ExceedsMaxGenerations_Returns400()
    {
        var client = CreateClient(new Dictionary<string, string?>
        {
            ["GameOfLife:MaxGenerations"] = "2"
        });
        var response = await client.PostAsJsonAsync("/api/boards", new CreateBoardRequest { Cells = [[0,0]] });
        response.EnsureSuccessStatusCode();
        var created = (await response.Content.ReadFromJsonAsync<BoardResponse>(WebOptions))!;

        var advanceResponse = await client.PostAsync($"/api/boards/{created.Id}/next/3", null);

        Assert.Equal(HttpStatusCode.BadRequest, advanceResponse.StatusCode);
    }

    [Fact]
    public async Task AdvanceToFinal_Block_ReturnsStableBlock()
    {
        var createRequest = new CreateBoardRequest { Cells = [[1,1],[1,2],[2,1],[2,2]] };
        var created = await PostBoardAsync(createRequest);

        var response = await _client.PostAsync($"/api/boards/{created.Id}/final", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var final = await response.Content.ReadFromJsonAsync<BoardResponse>(WebOptions);
        Assert.NotNull(final);
        Assert.Equal(4, final.Cells.Length);
        Assert.Contains(final.Cells, c => c[0] == 1 && c[1] == 1);
        Assert.Contains(final.Cells, c => c[0] == 1 && c[1] == 2);
        Assert.Contains(final.Cells, c => c[0] == 2 && c[1] == 1);
        Assert.Contains(final.Cells, c => c[0] == 2 && c[1] == 2);
    }

    [Fact]
    public async Task AdvanceToFinal_UnknownId_Returns404()
    {
        var response = await _client.PostAsync($"/api/boards/{Guid.NewGuid()}/final", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdvanceToFinal_Oscillator_Returns422()
    {
        var client = CreateClient(new Dictionary<string, string?>
        {
            ["GameOfLife:MaxFinalStateIterations"] = "5"
        });
        var response = await client.PostAsJsonAsync("/api/boards", new CreateBoardRequest { Cells = [[1,2],[2,2],[3,2]] });
        response.EnsureSuccessStatusCode();
        var created = (await response.Content.ReadFromJsonAsync<BoardResponse>(WebOptions))!;

        var finalResponse = await client.PostAsync($"/api/boards/{created.Id}/final", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, finalResponse.StatusCode);
    }

    private async Task<BoardResponse> PostBoardAsync(CreateBoardRequest request)
    {
        var response = await _client.PostAsJsonAsync("/api/boards", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BoardResponse>(WebOptions))!;
    }
}
