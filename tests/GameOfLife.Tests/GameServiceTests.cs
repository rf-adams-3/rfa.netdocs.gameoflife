using GameOfLife.Api.Models;
using GameOfLife.Api.Persistence;
using GameOfLife.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GameOfLife.Tests;

public class GameServiceTests
{
    private readonly Mock<IBoardRepository> _repoMock = new();
    private readonly Mock<IGameEngine> _engineMock = new();
    private readonly GameOfLifeSettings _settings = new();

    private GameService CreateService() =>
        new(_repoMock.Object, _engineMock.Object, Options.Create(_settings), NullLogger<GameService>.Instance);

    [Fact]
    public async Task CreateBoardAsync_ValidCells_PersistsAndReturnsBoardWithId()
    {
        bool[][] cells = [[true, false], [false, true]];
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Board b, CancellationToken _) => b);

        var result = await CreateService().CreateBoardAsync(cells);

        Assert.NotEqual(Guid.Empty, result.Id);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBoardAsync_EmptyRows_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().CreateBoardAsync([]));
    }

    [Fact]
    public async Task CreateBoardAsync_EmptyColumns_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().CreateBoardAsync([[]]));
    }

    [Fact]
    public async Task CreateBoardAsync_NonUniformGrid_ThrowsArgumentException()
    {
        bool[][] jagged = [[true, false], [true]];
        await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().CreateBoardAsync(jagged));
    }

    [Fact]
    public async Task CreateBoardAsync_ExceedsMaxRows_ThrowsArgumentException()
    {
        _settings.MaxGridRows = 2;
        bool[][] oversized = [[true], [false], [true]]; // 3 rows, limit is 2

        await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().CreateBoardAsync(oversized));
    }

    [Fact]
    public async Task CreateBoardAsync_ExceedsMaxCols_ThrowsArgumentException()
    {
        _settings.MaxGridCols = 2;
        bool[][] oversized = [[true, false, true]]; // 3 cols, limit is 2

        await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().CreateBoardAsync(oversized));
    }

    [Fact]
    public async Task AdvanceAsync_NonExistentId_ReturnsNull()
    {
        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Board?)null);

        var result = await CreateService().AdvanceAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task AdvanceAsync_ExistingId_AdvancesAndPersistsBoard()
    {
        var id = Guid.NewGuid();
        bool[][] initial = [[true, false], [false, true]];
        bool[][] next = [[false, false], [false, false]];
        var board = new Board { Id = id };
        board.Cells = initial;

        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(board);
        _engineMock.Setup(e => e.NextGeneration(It.IsAny<bool[][]>())).Returns(next);

        var result = await CreateService().AdvanceAsync(id);

        Assert.NotNull(result);
        Assert.Equal(next, result.Cells);
        _repoMock.Verify(r => r.UpdateAsync(board, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdvanceNStatesAsync_NonExistentId_ReturnsNull()
    {
        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Board?)null);

        var result = await CreateService().AdvanceNStatesAsync(Guid.NewGuid(), 3);

        Assert.Null(result);
    }

    [Fact]
    public async Task AdvanceNStatesAsync_NIsZero_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().AdvanceNStatesAsync(Guid.NewGuid(), 0));
    }

    [Fact]
    public async Task AdvanceNStatesAsync_NIsNegative_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().AdvanceNStatesAsync(Guid.NewGuid(), -1));
    }

    [Fact]
    public async Task AdvanceNStatesAsync_NExceedsMax_ThrowsArgumentException()
    {
        _settings.MaxGenerations = 100;

        await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().AdvanceNStatesAsync(Guid.NewGuid(), 101));
    }

    [Fact]
    public async Task AdvanceNStatesAsync_ValidN_CallsEngineNTimes()
    {
        var id = Guid.NewGuid();
        bool[][] cells = [[true, false]];
        var board = new Board { Id = id };
        board.Cells = cells;

        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(board);
        _engineMock.Setup(e => e.NextGeneration(It.IsAny<bool[][]>())).Returns(cells);

        await CreateService().AdvanceNStatesAsync(id, 5);

        _engineMock.Verify(e => e.NextGeneration(It.IsAny<bool[][]>()), Times.Exactly(5));
        _repoMock.Verify(r => r.UpdateAsync(board, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdvanceToFinalAsync_NonExistentId_ReturnsNull()
    {
        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Board?)null);

        var result = await CreateService().AdvanceToFinalAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task AdvanceToFinalAsync_StillLife_DetectsStableStateAndPersists()
    {
        var id = Guid.NewGuid();
        bool[][] stable = [[true, true], [true, true]];
        var board = new Board { Id = id };
        board.Cells = stable;

        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(board);
        _engineMock.Setup(e => e.NextGeneration(It.IsAny<bool[][]>())).Returns(stable);
        _engineMock.Setup(e => e.Serialize(It.IsAny<bool[][]>())).Returns("H0");

        var result = await CreateService().AdvanceToFinalAsync(id);

        Assert.NotNull(result);
        Assert.Equal(stable, result.Cells);
        _repoMock.Verify(r => r.UpdateAsync(board, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdvanceToFinalAsync_ExceedsMaxIterations_ThrowsInvalidOperationException()
    {
        _settings.MaxFinalStateIterations = 1;

        var id = Guid.NewGuid();
        bool[][] cells = [[true, false]];
        var board = new Board { Id = id };
        board.Cells = cells;

        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(board);
        _engineMock.Setup(e => e.NextGeneration(It.IsAny<bool[][]>())).Returns(cells);

        int counter = 0;
        _engineMock.Setup(e => e.Serialize(It.IsAny<bool[][]>()))
            .Returns(() => $"H{counter++}");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().AdvanceToFinalAsync(id));
    }

    [Fact]
    // specific test case found via google
    public async Task AdvanceToFinalAsync_Oscillator_ThrowsInvalidOperationException()
    {
        _settings.MaxFinalStateIterations = 10;

        var id = Guid.NewGuid();
        bool[][] stateA = [[true, false]];
        bool[][] stateB = [[false, true]];
        var board = new Board { Id = id };
        board.Cells = stateA;

        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(board);

        _engineMock.SetupSequence(e => e.NextGeneration(It.IsAny<bool[][]>()))
            .Returns(stateB).Returns(stateA).Returns(stateB).Returns(stateA)
            .Returns(stateB).Returns(stateA).Returns(stateB).Returns(stateA)
            .Returns(stateB).Returns(stateA);

        int counter = 0;
        _engineMock.Setup(e => e.Serialize(It.IsAny<bool[][]>()))
            .Returns(() => $"H{counter++}");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().AdvanceToFinalAsync(id));
    }
}
