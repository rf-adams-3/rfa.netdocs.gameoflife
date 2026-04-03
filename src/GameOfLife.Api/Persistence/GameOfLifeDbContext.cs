using GameOfLife.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GameOfLife.Api.Persistence;

public class GameOfLifeDbContext : DbContext
{
    public DbSet<Board> Boards => Set<Board>();

    public GameOfLifeDbContext(DbContextOptions<GameOfLifeDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Board>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.CellsJson).IsRequired();
            entity.Property(b => b.CreatedAt).IsRequired();
            entity.Property(b => b.UpdatedAt).IsRequired();
        });
    }
}
