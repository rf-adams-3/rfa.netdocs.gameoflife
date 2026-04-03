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

// use IClassFixture to ensure an isolated DB per test run, but reuse the DB and app setup for all tests in this class
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
                // use a unique DB name per CreateClient() call so each client gets an isolated database
                // use in-memory DB to avoid external dependencies
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<GameOfLifeDbContext>));
                if (descriptor is not null)
                    services.Remove(descriptor);

                var dbName = $"IntegrationTest_{Guid.NewGuid()}";
                services.AddDbContext<GameOfLifeDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task CreateBoard_ValidRequest_Returns201WithBoardId()
    {
        var request = new CreateBoardRequest { Cells = [[0, 1], [1, 0]] };

        var response = await _client.PostAsJsonAsync("/api/boards", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BoardResponse>(WebOptions);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal(2, body.Cells.Length);
    }

    [Fact]
    public async Task CreateBoard_EmptyCells_Returns400()
    {
        var request = new CreateBoardRequest { Cells = [] };

        var response = await _client.PostAsJsonAsync("/api/boards", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBoard_JaggedGrid_Returns400()
    {
        // Manually construct JSON to bypass C# type system
        var json = """{"cells":[[1,0],[1]]}""";
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
        var createRequest = new CreateBoardRequest
        {
            Cells =
            [
                [0, 0, 0, 0, 0],
                [0, 0, 1, 0, 0],
                [0, 0, 1, 0, 0],
                [0, 0, 1, 0, 0],
                [0, 0, 0, 0, 0],
            ]
        };
        var created = await PostBoardAsync(createRequest);

        var response = await _client.PostAsync($"/api/boards/{created.Id}/next", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var next = await response.Content.ReadFromJsonAsync<BoardResponse>(WebOptions);
        Assert.NotNull(next);
        // Blinker should have flipped to horizontal
        Assert.Equal(1, next.Cells[2][1]);
        Assert.Equal(1, next.Cells[2][2]);
        Assert.Equal(1, next.Cells[2][3]);
    }

    [Fact]
    public async Task AdvanceNext_MutatesStoredState()
    {
        var createRequest = new CreateBoardRequest
        {
            Cells =
            [
                [0, 0, 0, 0, 0],
                [0, 0, 1, 0, 0],
                [0, 0, 1, 0, 0],
                [0, 0, 1, 0, 0],
                [0, 0, 0, 0, 0],
            ]
        };
        var created = await PostBoardAsync(createRequest);

        await _client.PostAsync($"/api/boards/{created.Id}/next", null);
        var secondResponse = await _client.PostAsync($"/api/boards/{created.Id}/next", null);
        var secondState = await secondResponse.Content.ReadFromJsonAsync<BoardResponse>(WebOptions);

        Assert.NotNull(secondState);
        Assert.Equal(1, secondState.Cells[1][2]);
        Assert.Equal(1, secondState.Cells[2][2]);
        Assert.Equal(1, secondState.Cells[3][2]);
    }

    [Fact]
    public async Task AdvanceN_ValidN_Returns200()
    {
        var created = await PostBoardAsync(new CreateBoardRequest { Cells = [[1, 0], [0, 1]] });

        var response = await _client.PostAsync($"/api/boards/{created.Id}/next/3", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdvanceN_NIsZero_Returns400()
    {
        var created = await PostBoardAsync(new CreateBoardRequest { Cells = [[1, 0], [0, 1]] });

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
        var createRequest = new CreateBoardRequest { Cells = [[1, 0], [0, 1]] };
        var response = await client.PostAsJsonAsync("/api/boards", createRequest);
        response.EnsureSuccessStatusCode();
        var created = (await response.Content.ReadFromJsonAsync<BoardResponse>(WebOptions))!;

        var advanceResponse = await client.PostAsync($"/api/boards/{created.Id}/next/3", null);

        Assert.Equal(HttpStatusCode.BadRequest, advanceResponse.StatusCode);
    }

    [Fact]
    public async Task AdvanceToFinal_Block_ReturnsStableBlock()
    {
        var createRequest = new CreateBoardRequest
        {
            Cells =
            [
                [0, 0, 0, 0],
                [0, 1, 1, 0],
                [0, 1, 1, 0],
                [0, 0, 0, 0],
            ]
        };
        var created = await PostBoardAsync(createRequest);

        var response = await _client.PostAsync($"/api/boards/{created.Id}/final", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var final = await response.Content.ReadFromJsonAsync<BoardResponse>(WebOptions);
        Assert.NotNull(final);
        Assert.Equal(1, final.Cells[1][1]);
        Assert.Equal(1, final.Cells[1][2]);
        Assert.Equal(1, final.Cells[2][1]);
        Assert.Equal(1, final.Cells[2][2]);
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
        var createRequest = new CreateBoardRequest
        {
            Cells =
            [
                [0, 0, 0, 0, 0],
                [0, 0, 1, 0, 0],
                [0, 0, 1, 0, 0],
                [0, 0, 1, 0, 0],
                [0, 0, 0, 0, 0],
            ]
        };
        var response = await client.PostAsJsonAsync("/api/boards", createRequest);
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
