using GameOfLife.Api.Services;

namespace GameOfLife.Tests;

public class GameEngineTests
{
    private readonly GameEngine _engine = new();

    [Fact]
    // specific test case found via google
    public void Block_Stable_DoesNotChange()
    {
        var block = new HashSet<(int, int)> { (1,1), (1,2), (2,1), (2,2) };

        var next = _engine.NextGeneration(block);

        Assert.Equal(block, next);
    }

    [Fact]
    // specific test case found via google
    public void Blinker_Oscillator_AlternatesCorrectly()
    {
        // should oscillate between vertical and horizontal line of 3 cells
        var vertical = new HashSet<(int, int)> { (0,1), (1,1), (2,1) };
        var horizontal = new HashSet<(int, int)> { (1,0), (1,1), (1,2) };

        var gen1 = _engine.NextGeneration(vertical);
        Assert.Equal(horizontal, gen1);

        var gen2 = _engine.NextGeneration(gen1);
        Assert.Equal(vertical, gen2);
    }

    [Fact]
    public void EmptyBoard_RemainsEmpty()
    {
        var next = _engine.NextGeneration([]);

        Assert.Empty(next);
    }

    [Fact]
    public void SingleLiveCell_Dies_Underpopulation()
    {
        var next = _engine.NextGeneration([(0, 0)]);

        Assert.Empty(next);
    }

    [Fact]
    public void LiveCell_WithTwoNeighbors_Survives()
    {
        var cells = new HashSet<(int, int)> { (0,0), (0,1), (1,0) };

        var next = _engine.NextGeneration(cells);

        Assert.Contains((0, 0), next);
    }

    [Fact]
    public void LiveCell_WithFourOrMoreNeighbors_Dies_Overpopulation()
    {
        // center cell (1,1) has 4 live neighbors
        var cells = new HashSet<(int, int)> { (0,0), (0,1), (0,2), (1,0), (1,1) };

        var next = _engine.NextGeneration(cells);

        Assert.DoesNotContain((1, 1), next);
    }

    [Fact]
    public void DeadCell_WithExactlyThreeNeighbors_BecomesAlive()
    {
        var cells = new HashSet<(int, int)> { (0,0), (0,1), (1,0) };

        var next = _engine.NextGeneration(cells);

        Assert.Contains((1, 1), next); // (1,1) has 3 live neighbors — born
    }

    [Fact]
    public void DeadCell_WithTwoNeighbors_RemainsDeadAsBornRequiresThree()
    {
        var cells = new HashSet<(int, int)> { (0,0), (1,0) };

        var next = _engine.NextGeneration(cells);

        Assert.DoesNotContain((0, 1), next);
    }

    [Fact]
    public void Serialize_SameBoardTwice_ReturnsSameResult()
    {
        var cells = new HashSet<(int, int)> { (0,0), (1,1) };

        Assert.Equal(_engine.Serialize(cells), _engine.Serialize(cells));
    }

    [Fact]
    public void Serialize_DifferentBoards_ReturnDifferentResults()
    {
        var cells1 = new HashSet<(int, int)> { (0,0), (1,1) };
        var cells2 = new HashSet<(int, int)> { (0,1), (1,0) };

        Assert.NotEqual(_engine.Serialize(cells1), _engine.Serialize(cells2));
    }

    [Fact]
    public void Glider_TravelsDiagonally()
    {
        var glider = new HashSet<(int, int)> { (0,1), (1,2), (2,0), (2,1), (2,2) };

        // advance 4 generations — glider completes one cycle, shifted one step diagonally
        var cells = glider;
        for (int i = 0; i < 4; i++)
            cells = _engine.NextGeneration(cells);

        Assert.Equal(5, cells.Count);
        Assert.Contains((1,2), cells);
        Assert.Contains((2,3), cells);
        Assert.Contains((3,1), cells);
        Assert.Contains((3,2), cells);
        Assert.Contains((3,3), cells);
    }
}
