using System.Text.Json;

namespace GameOfLife.Api.Services;

public class GameEngine : IGameEngine
{
    public HashSet<(int row, int col)> NextGeneration(HashSet<(int row, int col)> liveCells)
    {
        var candidates = liveCells
            .SelectMany(GetNeighbors) // find all potential cells that could change state
            .Concat(liveCells) // include currently live cells to ensure they are evaluated
            .ToHashSet();

        // check each candidate for change cell
        return candidates.Where(cell =>
        {
            int neighborCount = GetNeighbors(cell).Count(l => liveCells.Contains(l)); // get count of neighbors for cell under evaluation
            return liveCells.Contains(cell)
                ? neighborCount == 2 || neighborCount == 3  // survival or death
                : neighborCount == 3;                        // birth or death
        }).ToHashSet();
    }

    // Get the 8 neighboring cells for a given cell
    private static IEnumerable<(int row, int col)> GetNeighbors((int row, int col) cell)
    {
        for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
        for (int colOffset = -1; colOffset <= 1; colOffset++)
        {
            if (rowOffset == 0 && colOffset == 0) continue;
            yield return (cell.row + rowOffset, cell.col + colOffset);
        }
    }

    // Serialize the set of live cells to a JSON string
    public string Serialize(HashSet<(int row, int col)> liveCells) =>
        JsonSerializer.Serialize(
            liveCells.OrderBy(c => c.row).ThenBy(c => c.col)
                     .Select(c => new[] { c.row, c.col })
                     .ToArray());
}
