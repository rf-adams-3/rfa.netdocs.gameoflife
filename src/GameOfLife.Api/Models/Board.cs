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
    public bool[][] Cells
    {
        get => JsonSerializer.Deserialize<bool[][]>(CellsJson)!;
        set => CellsJson = JsonSerializer.Serialize(value);
    }
}
