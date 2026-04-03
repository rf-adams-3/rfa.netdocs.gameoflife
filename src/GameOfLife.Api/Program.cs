using GameOfLife.Api.Models;
using GameOfLife.Api.Persistence;
using GameOfLife.Api.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GameOfLifeSettings>(
    builder.Configuration.GetSection(GameOfLifeSettings.SectionName));

builder.Services.AddDbContext<GameOfLifeDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IBoardRepository, BoardRepository>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddSingleton<IGameEngine, GameEngine>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Conway's Game of Life API", Version = "v1" });
});

var app = builder.Build();

// Apply migrations on startup (creates the DB if it doesn't exist).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GameOfLifeDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
    else // for in-memory or other non-relational providers, ensure DB is created
        db.Database.EnsureCreated();
}

app.Use(async (context, next) =>
{
    // Scalar otherwise caches responses, which causes incorrect results in the UI after the first request
    context.Response.Headers.CacheControl = "no-store";
    await next();
});

app.UseSwagger(options => options.RouteTemplate = "openapi/{documentName}.json");
app.MapScalarApiReference();
app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
app.MapControllers();

app.Run();

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }
