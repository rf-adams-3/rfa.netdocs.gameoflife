# Conway's Game of Life API

A RESTful API implementation of [Conway's Game of Life](https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life) built with C# and .NET 8.0.

## Requirements

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- If you want to run migrations: `dotnet tool install --global dotnet-ef`

## Running the API

```bash
cd src/GameOfLife.Api
dotnet run
```

The SQLite database (`gameoflife.db`) is created automatically on first run — no manual setup required.

The API will be available at:
- **http://localhost:5244**
- **https://localhost:7176**

## Interactive API Docs (Scalar)

Navigate to **http://localhost:5244/scalar/v1** to explore and try all endpoints interactively.

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/boards` | Upload a new board state, returns a board ID |
| `POST` | `/api/boards/{id}/next` | Advance the board by one generation |
| `POST` | `/api/boards/{id}/next/{n}` | Advance the board by N generations |
| `POST` | `/api/boards/{id}/final` | Advance to a stable state (still life or oscillator) |

### Upload a board

```json
POST /api/boards
{
  "cells": [
    [0, 0, 0, 0, 0],
    [0, 0, 1, 0, 0],
    [0, 0, 1, 0, 0],
    [0, 0, 1, 0, 0],
    [0, 0, 0, 0, 0]
  ]
}
```

Cell values: `0` = dead, `1` = alive. All rows must have the same length.

Response includes a `id` field to use in following requests.

### Error responses

| Status | Meaning |
|--------|---------|
| `400` | Invalid board (empty, non-rectangular, size exceeds configured max rows/columns) |
| `404` | Board ID not found |
| `422` | Final state not reached within the maximum number of iterations |

## Configuration

Limits are configurable in `appsettings.json`:

```json
"GameOfLife": {
  "MaxGridRows": 1000,
  "MaxGridCols": 1000,
  "MaxGenerations": 10000,
  "MaxFinalStateIterations": 10000
}
```

## Running Tests

```bash
dotnet test
```

## Database Migrations


If you modify the `Board` entity and need to add a new migration:

```bash
cd src/GameOfLife.Api
dotnet ef migrations add <MigrationName>
```

To apply migrations manually (instead of relying on auto-apply at startup):

```bash
dotnet ef database update
```

To remove the last migration (before it has been applied):

```bash
dotnet ef migrations remove
```