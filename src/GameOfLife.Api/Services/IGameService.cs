using GameOfLife.Api.Models;

namespace GameOfLife.Api.Services;

public interface IGameService
{
    Task<Board> CreateBoardAsync(bool[][] cells, CancellationToken cancellationToken = default);
    Task<Board?> AdvanceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Board?> AdvanceNStatesAsync(Guid id, int n, CancellationToken cancellationToken = default);
    Task<Board?> AdvanceToFinalAsync(Guid id, CancellationToken cancellationToken = default);
}
