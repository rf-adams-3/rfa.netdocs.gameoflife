namespace GameOfLife.Api.Models;

public class GameOfLifeSettings
{
    public const string SectionName = "GameOfLife";

    public int MaxGridRows { get; set; } = 1000;
    public int MaxGridCols { get; set; } = 1000;
    public int MaxGenerations { get; set; } = 10000;
    public int MaxFinalStateIterations { get; set; } = 10000;
}
