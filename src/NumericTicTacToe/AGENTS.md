# Numeric Tic-Tac-Toe

A strategic variant of Tic-Tac-Toe where players use numbers instead of X/O. Odd player uses {1,3,5,7,9}, Even player uses {2,4,6,8}. Win by creating a line summing to exactly 15.

## Build & Test

```shell
dotnet build NumericTicTacToe.sln
dotnet test NumericTicTacToe.sln
```

## Project Structure

| Project | Path | Purpose |
|---|---|---|
| `NumTic.Game` | `src/Game/` | Core game logic, contracts, state management |
| `NumTic.Console` | `src/Console/` | Console UI with Spectre.Console rendering |
| `NumTic.AI` | `src/AI/` | OpenAI-powered AI player |
| `NumTic.Mcp` | `src/Mcp/` | Model Context Protocol server |
| `NumTic.Tests` | `tests/` | Unit and live tests |

## Namespaces

- `Squire.NumTic` — Core game types
- `Squire.NumTic.Contracts` — Interface definitions
- `Squire.NumTic.Console` — Console UI
- `Squire.NumTic.AI` — AI player
- `Squire.NumTic.Mcp` — MCP server
- `Squire.NumTic.Tests` — All tests (flat namespace, avoid sub-namespaces)

## Key Conventions

- Target: The latest long term support version of .NET, `LangVersion` latest, nullable enabled, central package management
- Architecture: constructor injection, interface-based abstractions, async I/O with `CancellationToken`
- Clean separation between game logic (`src/Game/`) and UI concerns (`src/Console/`, `src/Mcp/`)

## Skills

For shared conventions, reference the repository-root skills:

| Skill | Path | Use When |
|---|---|---|
| **Anti-Hallucination** | `../../.github/skills/anti-hallucination/` | Every implementation task |
| **C# Conventions** | `../../.github/skills/csharp-conventions/` | Writing C# code |
| **Testing** | `../../.github/skills/testing/` | Writing tests |
| **Documentation** | `../../.github/skills/documentation/` | Writing docs/comments |
| **NumTic Architecture** | `.github/skills/numtic-architecture/` | Project structure details |