using System.Text.Json;

namespace GameOfLife.Api.Services;

public class GameEngine : IGameEngine
{
    public bool[][] NextGeneration(bool[][] cells)
    {
        var rowCount = cells.Length;
        var colCount = cells[0].Length;
        var next = new bool[rowCount][];

        for (var row = 0; row < rowCount; row++)
        {
            next[row] = new bool[colCount];
            for (var col = 0; col < colCount; col++)
            {
                var liveNeighbors = CountLiveNeighbors(cells, row, col, rowCount, colCount);
                next[row][col] = cells[row][col]
                    ? liveNeighbors == 2 || liveNeighbors == 3  // survival
                    : liveNeighbors == 3;                        // birth
            }
        }

        return next;
    }

    private static int CountLiveNeighbors(bool[][] cells, int row, int col, int rowCount, int colCount)
    {
        var count = 0;
        for (var rowOffset = -1; rowOffset <= 1; rowOffset++)
        for (var colOffset = -1; colOffset <= 1; colOffset++)
        {
            if (rowOffset == 0 && colOffset == 0) continue;
            var neighborRow = row + rowOffset;
            var neighborCol = col + colOffset;

            // check if neighbor is alive, and also within bounds of the board
            if (neighborRow >= 0 && neighborRow < rowCount && neighborCol >= 0 && neighborCol < colCount && cells[neighborRow][neighborCol])
                count++;
        }
        return count;
    }

    public string Serialize(bool[][] cells) => JsonSerializer.Serialize(cells);
}
