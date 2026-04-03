namespace GameOfLife.Api.Services;

public interface IGameEngine
{
    HashSet<(int row, int col)> NextGeneration(HashSet<(int row, int col)> liveCells);
    string Serialize(HashSet<(int row, int col)> liveCells);
}
