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
- Give every `Assert.That` a descriptive message so a failure in a multi-assert test identifies which check failed; interpolate context where it helps (e.g. `$"Token set for {player} should not be null"`)

## Documentation & Layout

- Every fixture, test, and helper carries an XML `<summary>` (fields use the single-line form; everything else uses the multi-line block). The fixture summary reads `The suite of tests for the <see cref="Type"/> class.`

- Test-method summaries name the member under test in a uniform voice: `Verifies functionality of the {Member} method.` (or `... property.` / `... constructor.`). Multiple tests of the same member share the identical summary; the specific scenario is carried by the test method name, not the summary.

- Document `<param>` for each parameter of a parameterized test. Never add `<returns>` to an async test method: it returns a void-like `Task`, which is not documented (see the documentation skill for the `Task` vs `Task<T>` rule).

- Separate a test into logical groups with single blank lines (setup, the act, the assertion block), keeping tightly-coupled statements together. This is a readability judgment, not a mechanical one-blank-per-statement rule.

- Hoist fixed test data and constants to the top of the method.

- Keep a test's calls on a single line rather than wrapping arguments onto continuation lines. For a multi-line raw string argument, place the opening `"""` on its own line below the call, with the closing `"""` at the delimiter's indentation.

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