using GameOfLife.Api.Services;

namespace GameOfLife.Tests;

public class GameEngineTests
{
    private readonly GameEngine _engine = new();

    [Fact]
    // specific test case found via google
    public void Block_StillLife_DoesNotChange()
    {
        // 2x2 square — canonical still life
        bool[][] block =
        [
            [false, false, false, false],
            [false, true,  true,  false],
            [false, true,  true,  false],
            [false, false, false, false],
        ];

        var next = _engine.NextGeneration(block);

        Assert.Equal(block, next);
    }

    [Fact]
    // specific test case found via google
    public void Blinker_Oscillator_AlternatesCorrectly()
    {
        // should oscillate between vertical and horizontal line of 3 cells
        bool[][] vertical =
        [
            [false, false, false, false, false],
            [false, false, true,  false, false],
            [false, false, true,  false, false],
            [false, false, true,  false, false],
            [false, false, false, false, false],
        ];

        bool[][] horizontal =
        [
            [false, false, false, false, false],
            [false, false, false, false, false],
            [false, true,  true,  true,  false],
            [false, false, false, false, false],
            [false, false, false, false, false],
        ];

        var gen1 = _engine.NextGeneration(vertical);
        Assert.Equal(horizontal, gen1);

        var gen2 = _engine.NextGeneration(gen1);
        Assert.Equal(vertical, gen2);
    }

    [Fact]
    public void EmptyBoard_RemainsEmpty()
    {
        bool[][] empty =
        [
            [false, false, false],
            [false, false, false],
            [false, false, false],
        ];

        var next = _engine.NextGeneration(empty);

        Assert.Equal(empty, next);
    }

    [Fact]
    public void SingleLiveCell_Dies_Underpopulation()
    {
        bool[][] single =
        [
            [false, false, false],
            [false, true,  false],
            [false, false, false],
        ];

        bool[][] expected =
        [
            [false, false, false],
            [false, false, false],
            [false, false, false],
        ];

        var next = _engine.NextGeneration(single);

        Assert.Equal(expected, next);
    }

    [Fact]
    public void LiveCell_WithTwoNeighbors_Survives()
    {
        bool[][] cells =
        [
            [false, false, false, false],
            [false, true,  true,  false],
            [false, true,  false, false],
            [false, false, false, false],
        ];

        var next = _engine.NextGeneration(cells);

        Assert.True(next[1][1]);
    }

    [Fact]
    public void LiveCell_WithFourOrMoreNeighbors_Dies_Overpopulation()
    {
        bool[][] cells =
        [
            [true,  true,  true],
            [true,  true,  false],
            [false, false, false],
        ];

        var next = _engine.NextGeneration(cells);

        Assert.False(next[1][1]);
    }

    [Fact]
    public void DeadCell_WithExactlyThreeNeighbors_BecomesAlive()
    {
        bool[][] cells =
        [
            [true,  true,  false],
            [true,  false, false],
            [false, false, false],
        ];

        var next = _engine.NextGeneration(cells);

        Assert.True(next[1][1]); 
    }

    [Fact]
    public void DeadCell_WithTwoNeighbors_RemainsDeadAsBornRequiresThree()
    {
        bool[][] cells =
        [
            [true,  false, false],
            [true,  false, false],
            [false, false, false],
        ];

        var next = _engine.NextGeneration(cells);

        Assert.False(next[1][1]);
    }

    [Fact]
    public void Serialize_SameBoardTwice_ReturnsSameHash()
    {
        bool[][] cells = [[true, false], [false, true]];

        Assert.Equal(_engine.Serialize(cells), _engine.Serialize(cells));
    }

    [Fact]
    public void Serialize_DifferentBoards_ReturnDifferentHashes()
    {
        bool[][] cells1 = [[true, false], [false, true]];
        bool[][] cells2 = [[false, true], [true, false]];

        Assert.NotEqual(_engine.Serialize(cells1), _engine.Serialize(cells2));
    }

    [Fact]
    public void SingleRowBoard_ProcessedCorrectly()
    {
        bool[][] cells = [[true, true, true]];

        var next = _engine.NextGeneration(cells);

        Assert.False(next[0][0]);
        Assert.True(next[0][1]);
        Assert.False(next[0][2]);
    }

    [Fact]
    public void SingleColumnBoard_ProcessedCorrectly()
    {
        bool[][] cells = [[true], [true], [true]];

        var next = _engine.NextGeneration(cells);

        Assert.False(next[0][0]);
        Assert.True(next[1][0]);
        Assert.False(next[2][0]);
    }

    [Fact]
    public void SingleCellBoard_LiveCell_Dies_Underpopulation()
    {
        bool[][] cells = [[true]];

        var next = _engine.NextGeneration(cells);

        Assert.False(next[0][0]);
    }

    [Fact]
    public void SingleCellBoard_DeadCell_RemainsDeadAsBornRequiresThree()
    {
        bool[][] cells = [[false]];

        var next = _engine.NextGeneration(cells);

        Assert.False(next[0][0]);
    }
}
