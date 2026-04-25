---
name: numtic-architecture
description: Numeric Tic-Tac-Toe project structure, dependency graph, and domain conventions. Load when working on project structure or cross-cutting changes.
---

# Numeric Tic-Tac-Toe Architecture

## Solution Layout

```
NumericTicTacToe.sln
├── src/
│   ├── Game/       (NumTic.Game.csproj)     — Core logic, no external dependencies
│   ├── Console/    (NumTic.Console.csproj)  — Console app, depends on Game + Spectre.Console
│   ├── AI/         (NumTic.AI.csproj)       — OpenAI player, depends on Game + OpenAI SDK
│   └── Mcp/        (NumTic.Mcp.csproj)      — MCP server, depends on Game + ModelContextProtocol
├── tests/          (NumTic.Tests.csproj)     — References all src projects
└── benchmark/      (NumTic.Benchmark.csproj) — Performance benchmarks
```

## Dependency Rules

- `Game` has zero project dependencies (pure domain logic)
- `Console`, `AI`, `Mcp` each depend only on `Game`
- `Tests` references all projects
- Central package management via `Directory.Packages.props`
- Shared build configuration via `Directory.Build.props` (`net10.0`, nullable, implicit usings)

## Key Domain Types

- `GameState` — Board state, turn tracking, win detection
- `Move` — Player action (player token, position index, number token)
- `PlayerToken` — `Odd` or `Even` player identity
- `IPlayer` — Player abstraction (`ConsolePlayer`, `OpenAIPlayer`, `BotPlayer`)
- `IGameInterface` — UI abstraction for rendering and input
- `Game` — Orchestrates gameplay between two players