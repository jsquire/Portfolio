---
name: documentation
description: XML documentation and inline comment conventions for C# code. Load when writing documentation or comments.
---

# Documentation Conventions

## XML Documentation

Required for all members regardless of visibility. Follow this structure:

```csharp
/// <summary>
///   Processes the <paramref name="data"/> asynchronously.
/// </summary>
///
/// <param name="data">The data to process.</param>
/// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
///
/// <returns>The processed result.</returns>
///
/// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
///
```

**Required tags**: `<summary>` for all APIs, `<param>` for all parameters, `<exception>` for documented exceptions, `<seealso>` for related documentation.

### Summary form

Fields and constants use the single-line `/// <summary>…</summary>`. All other members, and every type including nested types, use the multi-line block with a trailing blank `///` line.

### `<returns>` and `Task`

Document `<returns>` only for a member that produces a value: a synchronous value-returning method, or a `Task<T>` (describe the produced value). Do not add `<returns>` to a `void` method or a non-generic `Task` (including every async test method), which produce no value.

### Descriptive Parameter Documentation

Parameter docs must describe the **purpose, behavior, or context** — avoid restating the parameter name or type.

```csharp
// ❌ BAD: Restates the name.
/// <param name="cancellationToken">The cancellation token.</param>
/// <param name="gameState">The game state.</param>

// ✅ GOOD: Describes purpose or behavior.
/// <param name="cancellationToken">A token that can be used to signal a request for cancellation.</param>
/// <param name="gameState">The current state of the game, including board positions and turn tracking.</param>
```

### `<inheritdoc />` Usage

Use `<inheritdoc />` for **well-known standard members** where the base documentation is sufficient and widely understood (e.g., `ToString()`, `Equals()`, `GetHashCode()`, `Dispose()`).

Prefer **explicit documentation** for domain-specific members where readers benefit from the locality of information — especially interface implementations with project-specific behavior.

```csharp
// ✅ GOOD: inheritdoc for well-known standard members.
/// <inheritdoc />
public override string ToString() => $"Move({Player}, {PositionIndex}, {Token})";

// ✅ GOOD: Explicit docs for domain-specific interface implementation.
/// <summary>
///   Plays a turn by prompting the console user for input and validating the response.
/// </summary>
///
public async Task<Move> PlayTurnAsync(GameState gameState, CancellationToken cancellationToken = default)
```

## Inline Comments

Inline comments must be **full sentences ending with periods**, followed by a **blank line** before the next code statement.

```csharp
// Set up test data for the validation scenario.

var gameState = CreateValidGameState();
```