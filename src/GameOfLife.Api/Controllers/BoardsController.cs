using GameOfLife.Api.Models;
using GameOfLife.Api.Models.Requests;
using GameOfLife.Api.Models.Responses;
using GameOfLife.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameOfLife.Api.Controllers;

[ApiController]
[Route("api/boards")]
[Produces("application/json")]
public class BoardsController : ControllerBase
{
    private readonly IGameService _gameService;

    public BoardsController(IGameService gameService) => _gameService = gameService;

    [HttpPost]
    [ProducesResponseType(typeof(BoardResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BoardResponse>> CreateBoard(
        [FromBody] CreateBoardRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var cells = ToBoolJagged(request.Cells);
            var board = await _gameService.CreateBoardAsync(cells, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, ToResponse(board));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Problem(detail: ex.Message, statusCode: 400));
        }
    }

    [HttpPost("{id:guid}/next")]
    [ProducesResponseType(typeof(BoardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BoardResponse>> AdvanceNext(
        Guid id,
        CancellationToken cancellationToken)
    {
        var board = await _gameService.AdvanceAsync(id, cancellationToken);
        if (board is null) return NotFound();
        return Ok(ToResponse(board));
    }

    [HttpPost("{id:guid}/next/{n:int}")]
    [ProducesResponseType(typeof(BoardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BoardResponse>> AdvanceN(
        Guid id,
        int n,
        CancellationToken cancellationToken)
    {
        try
        {
            var board = await _gameService.AdvanceNStatesAsync(id, n, cancellationToken);
            if (board is null) return NotFound();
            return Ok(ToResponse(board));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Problem(detail: ex.Message, statusCode: 400));
        }
    }

    [HttpPost("{id:guid}/final")]
    [ProducesResponseType(typeof(BoardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BoardResponse>> AdvanceToFinal(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var board = await _gameService.AdvanceToFinalAsync(id, cancellationToken);
            if (board is null) return NotFound();
            return Ok(ToResponse(board));
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(Problem(detail: ex.Message, statusCode: 422));
        }
    }

    private static bool[][] ToBoolJagged(int[][] cells) =>
        cells.Select(row => row.Select(cell => cell != 0).ToArray()).ToArray();

    private static BoardResponse ToResponse(Board board) =>
        new(board.Id, board.Cells.Select(row => row.Select(c => c ? 1 : 0).ToArray()).ToArray(), board.UpdatedAt);
}
