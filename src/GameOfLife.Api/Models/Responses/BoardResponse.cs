namespace GameOfLife.Api.Models.Responses;

public record BoardResponse(Guid Id, int[][] Cells, DateTime UpdatedAt);
