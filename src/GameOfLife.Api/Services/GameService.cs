using GameOfLife.Api.Models;
using GameOfLife.Api.Persistence;
using Microsoft.Extensions.Options;

namespace GameOfLife.Api.Services;

public class GameService : IGameService
{
    private readonly IBoardRepository _repository;
    private readonly IGameEngine _engine;
    private readonly GameOfLifeSettings _settings;
    private readonly ILogger<GameService> _logger;

    public GameService(IBoardRepository repository, IGameEngine engine, IOptions<GameOfLifeSettings> settings, ILogger<GameService> logger)
    {
        _repository = repository;
        _engine = engine;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<Board> CreateBoardAsync(bool[][] cells, CancellationToken cancellationToken = default)
    {
        ValidateGrid(cells);

        var board = new Board
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        board.Cells = cells;

        await _repository.AddAsync(board, cancellationToken);
        _logger.LogInformation("Board {BoardId} created ({Rows}x{Cols})", board.Id, cells.Length, cells[0].Length);
        return board;
    }

    public async Task<Board?> AdvanceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var board = await _repository.GetByIdAsync(id, cancellationToken);
        if (board is null)
        {
            _logger.LogWarning("Board {BoardId} not found", id);
            return null;
        }

        board.Cells = _engine.NextGeneration(board.Cells);
        await _repository.UpdateAsync(board, cancellationToken);
        _logger.LogDebug("Board {BoardId} advanced 1 generation", id);
        return board;
    }

    public async Task<Board?> AdvanceNStatesAsync(Guid id, int n, CancellationToken cancellationToken = default)
    {
        if (n <= 0)
            throw new ArgumentException($"N must be greater than 0, but was {n}.", nameof(n));
        if (n > _settings.MaxGenerations)
            throw new ArgumentException($"N cannot exceed {_settings.MaxGenerations}, but was {n}.", nameof(n));

        var board = await _repository.GetByIdAsync(id, cancellationToken);
        if (board is null)
        {
            _logger.LogWarning("Board {BoardId} not found", id);
            return null;
        }

        var cells = board.Cells;
        for (int i = 0; i < n; i++)
            cells = _engine.NextGeneration(cells);

        board.Cells = cells;
        await _repository.UpdateAsync(board, cancellationToken);
        _logger.LogDebug("Board {BoardId} advanced {N} generations", id, n);
        return board;
    }

    public async Task<Board?> AdvanceToFinalAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var board = await _repository.GetByIdAsync(id, cancellationToken);
        if (board is null)
        {
            _logger.LogWarning("Board {BoardId} not found", id);
            return null;
        }

        var cells = board.Cells;
        for (int i = 0; i < _settings.MaxFinalStateIterations; i++)
        {
            var next = _engine.NextGeneration(cells);

            // board is stable if next generation is the same as current
            if (_engine.Serialize(cells) == _engine.Serialize(next))
            {
                board.Cells = next;
                await _repository.UpdateAsync(board, cancellationToken);
                _logger.LogInformation("Board {BoardId} reached a stable state after {Iterations} iterations", id, i + 1);
                return board;
            }

            cells = next;
        }

        _logger.LogWarning("Board {BoardId} did not reach a stable state within {MaxIterations} iterations", id, _settings.MaxFinalStateIterations);
        throw new InvalidOperationException(
            $"A stable state was not reached within {_settings.MaxFinalStateIterations} iterations.");
    }

    private void ValidateGrid(bool[][] cells)
    {
        if (cells.Length == 0)
            throw new ArgumentException("The board must have at least one row.");

        if (cells.Length > _settings.MaxGridRows)
            throw new ArgumentException(
                $"The board cannot exceed {_settings.MaxGridRows} rows, but {cells.Length} were provided.");

        int cols = cells[0].Length;

        if (cols == 0)
            throw new ArgumentException("The board must have at least one column.");

        if (cols > _settings.MaxGridCols)
            throw new ArgumentException(
                $"The board cannot exceed {_settings.MaxGridCols} columns, but {cols} were provided.");

        for (int r = 1; r < cells.Length; r++)
        {
            if (cells[r].Length != cols)
                throw new ArgumentException(
                    $"All rows must have the same number of columns. Row 0 has {cols} columns, but row {r} has {cells[r].Length}.");
        }
    }
}
