using GameOfLife.Api.Models;

namespace GameOfLife.Api.Persistence;

public interface IBoardRepository
{
    Task<Board?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Board> AddAsync(Board board, CancellationToken cancellationToken = default);
    Task UpdateAsync(Board board, CancellationToken cancellationToken = default);
}
