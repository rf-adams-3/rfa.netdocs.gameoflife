using GameOfLife.Api.Models;

namespace GameOfLife.Api.Persistence;

public class BoardRepository : IBoardRepository
{
    private readonly GameOfLifeDbContext _db;

    public BoardRepository(GameOfLifeDbContext db) => _db = db;

    public async Task<Board?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.Boards.FindAsync([id], cancellationToken);

    public async Task<Board> AddAsync(Board board, CancellationToken cancellationToken = default)
    {
        _db.Boards.Add(board);
        await _db.SaveChangesAsync(cancellationToken);
        return board;
    }

    public async Task UpdateAsync(Board board, CancellationToken cancellationToken = default)
    {
        board.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
