using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace GameOfLife.Api.Models;

public class Board
{
    public Guid Id { get; set; }
    public string CellsJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [NotMapped]
    public HashSet<(int row, int col)> Cells
    {
        get => JsonSerializer.Deserialize<int[][]>(CellsJson)!
            .Select(p => (p[0], p[1]))
            .ToHashSet();
        set => CellsJson = JsonSerializer.Serialize(
            value.OrderBy(c => c.row).ThenBy(c => c.col)
                 .Select(c => new[] { c.row, c.col })
                 .ToArray());
    }
}
