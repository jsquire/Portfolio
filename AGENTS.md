# Portfolio Repository

A collection of personal projects demonstrating software engineering practices. Primary active project is **Numeric Tic-Tac-Toe** (`src/NumericTicTacToe/`).

## Build & Test

```shell
# Numeric Tic-Tac-Toe (primary project)
cd src/NumericTicTacToe
dotnet build NumericTicTacToe.sln
dotnet test NumericTicTacToe.sln
```

## Repository Structure

- `src/NumericTicTacToe/` — Strategic number-based Tic-Tac-Toe with AI players, MCP server, console UI
- `documents/` — Technical documents and references
- `resume/` — Professional resume materials

## Skills (Load On-Demand)

Skills provide focused instructions for specific task areas. Reference the relevant skill when working in that domain.

| Skill | Path | Use When |
|---|---|---|
| **Anti-Hallucination** | `.github/skills/anti-hallucination/` | Every implementation task (highest priority) |
| **C# Conventions** | `.github/skills/csharp-conventions/` | Writing or modifying C# code |
| **Testing** | `.github/skills/testing/` | Writing or reviewing tests |
| **Documentation** | `.github/skills/documentation/` | Writing XML docs or inline comments |

## Core Principles

- Base every technical decision on verified, authoritative data
- Surface uncertainty explicitly — state what you lack data to validate
- Present implementation plans for review before coding
- Provide file and line references when discussing existing code