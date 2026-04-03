namespace GameOfLife.Api.Services;

public interface IGameEngine
{
    bool[][] NextGeneration(bool[][] cells);
    string Serialize(bool[][] cells);
}
