namespace GameOfLife.Api.Models.Requests;

public record CreateBoardRequest
{
    public required int[][] Cells { get; init; }
}
