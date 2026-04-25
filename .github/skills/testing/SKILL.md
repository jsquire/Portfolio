---
name: testing
description: Test standards including organization, assertions, mocking patterns, and quality guidelines. Load when writing or reviewing tests.
---

# Testing Standards

## General Principles

- Design tests around intended behavior and end-to-end scenarios, including edge cases — base test design on behavior, not implementation.
- Execute tests with `dotnet test` without prompting.
- When writing or updating tests, leave the implementation unchanged. If behavior appears incorrect, surface analysis in chat for human review.
- When testability is insufficient (missing abstractions, untestable internals), surface analysis in chat for discussion rather than working around gaps.

## Organization

- **Namespace**: `Squire.NumTic.Tests` (always; avoid area-specific sub-namespaces)
- **File per class**: All tests for a class in one file (e.g., `GameState` → `GameStateTests.cs`)
- **Categories**: Use `[Category]` attributes for area grouping (`"Game"`, `"Console"`, `"Players"`)
- **Local variables only**: Create all test subjects and mocks locally in each test method — avoid class-level fields or setup methods
- **Avoid** `#region`/`#endregion` directives

## Test Naming

Name tests by behavior validated, not implementation details:
- **Good**: `PlayTurnAsyncThrowsForInvalidGameState`
- **Avoid**: `PlayTurnAsyncThrowsArgumentOutOfRangeExceptionForNullGameState`

## Assertions

- Use `Assert.That` or `Assert.ThatAsync` exclusively (avoid `Assert.Throws`)
- For async operations: `Assert.ThatAsync`
- For exceptions: `Throws.ArgumentNullException.With.Property(...)`
- Redirect `System.Console.Out` in tests involving rendering

## Mocking (NSubstitute)

- `Substitute.For<IInterface>()` for all test doubles, created locally per test
- Verify with `.Received()` and `.DidNotReceive()`
- Mock user-observable interactions, not internal implementation details

```csharp
// Mock user-facing behavior
mockGameInterface.ReadPlayerResponseAsync(Arg.Any<CancellationToken>())
    .Returns("1,1,5");
```

## API Contract Testing

- Test external behavior only — what API users can observe
- Respect class invariants and intended construction patterns
- Use public constructors and factory methods as designed
- If testing reveals API design issues, discuss the design rather than working around it

## "DoesNotThrow" Test Policy

**Avoid creating tests that only verify "does not throw" for:**
- Basic object construction with valid parameters
- Simple method calls with valid input
- Happy path scenarios and rendering with valid data

**Reserve "does not throw" assertions for:**
- Boundary validation logic (valid vs invalid coordinates)
- Complex business rule validation (token ownership rules)
- Error recovery and robustness with malformed data

**Quality check**: If removing the `Throws.Nothing` assertion leaves no meaningful verification, the test adds no value. Every test should verify observable behavior, state changes, or side effects.

```csharp
// Prefer: verify observable state
var game = new Game(player1, player2, renderer);
Assert.That(game.State.CurrentTurn, Is.EqualTo(PlayerToken.Odd));
Assert.That(game.State.IsGameOver, Is.False);
```