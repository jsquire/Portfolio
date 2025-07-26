# Copilot Instructions for Numeric Tic-Tac-Toe

This document provides guidelines for GitHub Copilot when working on the Numeric Tic-Tac-Toe project. These instructions help maintain consistency with the existing codebase standards, conventions, and architectural patterns.

## Project Overview

Numeric Tic-Tac-Toe is a strategic variant of classic Tic-Tac-Toe that uses numbers instead of X's and O's. Players use odd (1,3,5,7,9) or even (2,4,6,8) numbers and win by creating lines that sum to exactly 15. The project demonstrates clean architecture principles with extensible rendering and player implementations.

## General Instructions for Copilot Responses

- Provide concise, focused explanations without unnecessary elaboration.
- When analyzing images, first describe exactly what is visually present before conducting any web-based research.
- Cross-check all details with both the image and external sources to ensure accuracy.
- Clearly explain your reasoning by verifying claims step by step.
- Avoid hallucinations at all costs—only respond with grounded, verifiable information.

## Code Standards & Conventions

### .NET Performance and Memory Allocation Guidelines

**CRITICAL: Never dismiss allocation concerns as "negligible" or assume garbage collection is "effortless."**

Modern .NET performance best practices emphasize **allocation reduction** as a primary optimization strategy:

- **Gen0 collections have real costs** - CPU overhead and pause times that accumulate
- **Allocation rate often matters more than allocation size** - frequent small allocations can be more problematic than occasional large ones
- **Use allocation-reducing patterns** when appropriate:
  - `Span<T>` and `Memory<T>` for temporary data
  - `ArrayPool<T>` for reusable buffers
  - `stackalloc` for small, short-lived arrays
  - Avoid unnecessary LINQ chains that create intermediate collections

**When evaluating allocation trade-offs:**
- Consider the allocation frequency and context
- Measure actual performance impact when in doubt
- Acknowledge that allocation reduction is a legitimate optimization concern
- Balance allocation concerns with code maintainability appropriately

**Never suggest that garbage collection "handles allocations effortlessly" - this contradicts fundamental .NET

### C# Language Features
- **Target Framework**: .NET 9.0
- **Language Version**: Latest C# features including records, pattern matching, and nullable reference types
- **Nullable Context**: Enabled throughout the project
- **var Usage**: Prefer `var` for local variables when type is apparent: `var gameState = CreateValidGameState();`

### Formatting & Style
- **Indentation**: 4 spaces (no tabs)
- **Brace Style**: Allman style (opening braces on new lines)
- **Line Endings**: LF (Unix-style)
- **Encoding**: UTF-8
- **Final Newlines**: Do not insert final newlines in files

### Naming Conventions
- **Classes**: PascalCase (`GameState`, `ConsolePlayer`)
- **Interfaces**: PascalCase with `I` prefix (`IPlayer`, `IGameInterface`)
- **Methods**: PascalCase (`PlayTurnAsync`, `CreateValidGameState`)
- **Properties**: PascalCase (`CurrentTurn`, `PlayerToken`)
- **Fields**: camelCase with underscore prefix (`_players`, `_interface`)
- **Parameters**: camelCase (`gameState`, `cancellationToken`)
- **Local Variables**: camelCase (`mockGameInterface`, `player`)

### Member Organization
Members within a class should be organized in the following sections:

1. **Constants**
2. **Fields**
3. **Properties**
4. **Constructors**
5. **Methods**
6. **Nested Types**

Within each section, organize by visibility from least to most restrictive:
- `private`
- `protected`
- `internal`
- `public`

For constants, fields, and properties: static members come before instance members.
For methods: instance methods come before static methods.

**Example Organization:**
```csharp
public class ExampleClass
{
    // Constants (static first, by visibility)
    private const int DefaultValue = 10;
    public const string Version = "1.0";

    // Fields (static first, by visibility, then instance by visibility)
    // Should have a blank line between static and instance fields.
    private static readonly object _lock = new();
    public static readonly string DefaultName = "Default";

    private readonly IService _service;
    public readonly int Id;

    // Properties (static first, by visibility, then instance by visibility)
    // Should have a blank line between static and instance properties.
    private static int StaticCounter { get; set; }
    public static string GlobalSetting { get; set; }

    private int _counter;
    public string Name { get; set; }

    // Constructors
    public ExampleClass() { }
    public ExampleClass(IService service) { }

    // Methods (instance first, by visibility, then static by visibility)
    // Should have a blank line between static and instance fields.
    private void HelperMethod() { }
    public void DoSomething() { }

    private static void StaticHelperMethod() { }
    public static void StaticMethod() { }

    // Nested Types
    private class NestedClass { }
    public enum Status { }
}
```

### Comments & Documentation

- **XML Documentation**: Required for all public members
- **Summary Tags**: Use `<summary>` for all public APIs
- **Parameter Documentation**: Use `<param>` for all parameters
- **Exception Documentation**: Use `<exception>` for documented exceptions
- **See Also References**: Use `<seealso>` for related documentation
- **No Inheritdoc**: Never use `/// <inheritdoc />`; always write explicit documentation following the documentation conventions
- **Inline Comments**: **CRITICAL:** Must be full sentences ending with periods, followed by blank lines

**Inline Comment Examples:**
```csharp
// ❌ WRONG: Missing period and blank line
// Set up test data
var gameState = CreateValidGameState();

// ✅ CORRECT: Full sentence with period and blank line
// Set up test data for the validation scenario.

var gameState = CreateValidGameState();
```

```csharp
/// <summary>
///   Verifies that PlayTurnAsync validates the gameState parameter properly.
/// </summary>
///
[Test]
public async Task PlayTurnAsyncWithNullGameStateThrows()
{
    var mockGameInterface = Substitute.For<IGameInterface>();

    // This should throw an ArgumentNullException.

    await Assert.ThatAsync(async () => await player.PlayTurnAsync(null!),
        Throws.ArgumentNullException);
}
```

## Architecture & Design Patterns

### Project Structure
- **src/Game/**: Core game logic and contracts
- **src/Console/**: Console-specific implementations
- **tests/**: Unit tests organized by project structure
- **Contracts/**: Interface definitions and abstractions

### Dependency Injection
- Constructor injection for all dependencies
- Interface-based abstractions for extensibility
- No service locator or static dependencies

### Async Patterns
- All I/O operations must be async
- Use `async`/`await` consistently
- Always accept `CancellationToken` parameters with default values
- Method names ending in `Async` for async operations

```csharp
public async Task<Move> PlayTurnAsync(GameState gameState, 
                                      CancellationToken cancellationToken = default)
```

## Testing Standards

### General Test Guidlines
- You do not have to ask to execute tests, so long as the command being used is `dotnet test`.
- When writing or updating tests, DO NOT make changes to the implementation.  If the behavior being tested is incorrect, provide your analysis in chat clearly and succinctly for human analysis and review.

### Test Organization
- **Test Namespace**: All test classes must use `Squire.NumTic.Tests` namespace (never area-specific namespaces like `NumTic.Tests.Game`)
- **Test File Organization**: Create test files on a **per-class basis** - all behavior for a class should be tested in a single corresponding test file (e.g., `GameState` → `GameStateTests.cs`, `ConsolePlayer` → `ConsolePlayerTests.cs`)
- **Method Grouping**: Group tests by the method being tested, but keep all methods of a class in the same test file
- **Test Categories**: Use `[Category]` attributes on test classes for area grouping (`[Category("Game")]`, `[Category("Console")]`)
- **No Regions**: Do not use `#region`/`#endregion` directives
- **Local Variables Only**: No class-level test subjects or setup methods
- **Non-Parallelizable**: Mark test classes with `[NonParallelizable]` if needed

### Test Naming Conventions
- **Use generic concepts** rather than specific exception types in test names
- **Good**: `PlayTurnAsyncThrowsForInvalidGameState`
- **Bad**: `PlayTurnAsyncThrowsArgumentOutOfRangeExceptionForNullGameState`
- Focus on the **behavior being tested** rather than implementation details
- Keep names **descriptive but concise**

### Test Structure
```csharp
namespace Squire.NumTic.Tests;

[Category("Console")]
public class ConsolePlayerTests
{
    [Test]
    public async Task MethodNameWithScenarioShouldExpectedBehavior()
    {
        // Arrange: Create local instances.
        var mockGameInterface = Substitute.For<IGameInterface>();
        var player = new ConsolePlayer(mockGameInterface);
        var gameState = CreateValidGameState();

        // Act & Assert: Use Assert.ThatAsync for async operations.
        await Assert.ThatAsync(async () => await player.PlayTurnAsync(gameState),
            Throws.Nothing);
    }
}
```

### Assertion Standards
- **Never use `Assert.Throws`**: Always use `Assert.That` or `Assert.ThatAsync`
- **Async Assertions**: Use `Assert.ThatAsync` for async operations
- **Exception Testing**: Use `Throws.ArgumentNullException.With.Property(...)`
- **Console Redirection**: Redirect `System.Console.Out` in tests that involve rendering

### Mocking with NSubstitute
- Use NSubstitute for all test doubles
- Create mocks locally in each test method
- Use `Substitute.For<IInterface>()` for interface mocks
- Verify interactions with `.Received()` and `.DidNotReceive()`

```csharp
// Setup mock behavior.
mockGameInterface.ReadPlayerResponseAsnyc(Arg.Any<CancellationToken>())
    .Returns("1", "1", "1");

// Verify interactions.
await mockGameInterface.Received().RenderPlayerTextAsync(
    Arg.Any<TextType>(),
    Arg.Is<string>(s => s.Contains("Available tokens")),
    Arg.Any<CancellationToken>());
```

### ❌ **NEVER Create Tests That Only Verify "DoesNotThrow" For:**

1. **Basic Object Construction**
   - Constructor calls with valid parameters
   - Factory method calls with valid inputs
   - Example: `Assert.That(() => new MyClass(validParam), Throws.Nothing)`

2. **Simple Method Calls with Valid Input**
   - Methods called with expected, valid parameters
   - Basic CRUD operations with proper data
   - Example: `Assert.That(() => obj.SimpleMethod(validInput), Throws.Nothing)`

3. **Happy Path Scenarios**
   - Normal execution flows that should naturally work
   - Standard use cases without edge conditions
   - Example: `Assert.That(() => game.Reset(), Throws.Nothing)`

4. **Rendering/UI Operations with Valid Data**
   - Console output, file writing, or display operations with proper input
   - Example: `Assert.That(() => renderer.Render(validData), Throws.Nothing)`

### ✅ **ACCEPTABLE "DoesNotThrow" Tests Only When Testing:**

1. **Boundary Validation Logic**
   - Testing that validation methods correctly identify valid vs invalid boundaries
   - Example: Position validation that must distinguish between valid coordinates (1,1) and invalid ones (0,0)

2. **Complex Business Rule Validation**
   - Testing that business logic correctly permits valid operations
   - Example: Token ownership rules where specific tokens must be allowed for specific players

3. **Error Recovery and Robustness**
   - Testing that systems handle corrupted/invalid states gracefully without crashing
   - Example: Methods that should not crash even when given malformed data

4. **Integration with External Dependencies**
   - Testing that code properly handles external system interactions
   - Example: File system operations, network calls, or database interactions

### 🔍 **Test Quality Guidelines:**

1. **The "What Else" Test**
   - If removing the `Throws.Nothing` assertion leaves no meaningful verification, the entire test should be removed
   - Ask: "What specific behavior does this test verify beyond 'it doesn't crash'?"

2. **Behavior Over Implementation**
   - Tests should verify observable behavior, state changes, or side effects
   - Focus on WHAT the code does, not just that it runs

3. **Value-Add Principle**
   - Every test assertion should provide unique value
   - If a test only verifies that valid input doesn't throw, it adds no value over the compiler's type checking

### 📝 **Replacement Test Patterns:**

Instead of meaningless "DoesNotThrow" tests, create tests that verify:

```csharp
// ❌ BAD: Only tests that constructor doesn't throw
[Test]
public void ConstructorSucceedsWithValidInput()
{
    Assert.That(() => new Game(player1, player2, renderer), Throws.Nothing);
}

// ✅ GOOD: Tests that constructor creates correct initial state
[Test] 
public void ConstructorInitializesGameWithCorrectState()
{
    var game = new Game(player1, player2, renderer);
    
    Assert.That(game.State.CurrentTurn, Is.EqualTo(PlayerToken.Odd));
    Assert.That(game.State.IsGameOver, Is.False);
    Assert.That(game.State.Winner, Is.Null);
}

// ❌ BAD: Only tests that method doesn't throw
[Test]
public void ResetCompletesSuccessfully()
{
    Assert.That(() => game.Reset(), Throws.Nothing);
}

// ✅ GOOD: Tests what Reset actually does
[Test]
public void ResetRestoresInitialGameState()
{
    // Arrange: Modify game state
    game.State.SetBoardToken(1, 1, 5);
    game.State.CurrentTurn = PlayerToken.Even;
    
    // Act
    game.Reset();
    
    // Assert: Verify state was restored
    Assert.That(game.State.GetBoardToken(1, 1), Is.EqualTo(0));
    Assert.That(game.State.CurrentTurn, Is.EqualTo(PlayerToken.Odd));
}

## Error Handling

### Exception Conventions
- Use standard .NET exceptions (`ArgumentNullException`, `ArgumentOutOfRangeException`, `InvalidOperationException`)
- Always validate parameters and throw appropriate exceptions
- Use `ArgumentNullException.ThrowIfNull(parameter, nameof(parameter))`
- Include meaningful parameter names in exceptions

### Cancellation Support
- Always support cancellation tokens in async methods
- Call `cancellationToken.ThrowIfCancellationRequested()` at appropriate points
- Handle `OperationCanceledException` gracefully in calling code

## Performance Considerations

### Memory Management
- Use `using` statements for disposable resources
- Prefer `ReadOnlyCollection`/`IReadOnlyDictionary` for immutable collections
- Consider object pooling for frequently allocated objects

### Async Best Practices
- Use `ConfigureAwait(false)` in library code (not UI code)
- Avoid `async void` except for event handlers
- Don't block on async operations with `.Result` or `.Wait()`

## Code Examples

### Interface Implementation
```csharp
namespace Squire.NumTic.Contracts;

/// <summary>
///   Defines the contract for rendering game state to users.
/// </summary>
///
public interface IGameInterface
{
    /// <summary>
    ///   Renders the current game state for display.
    /// </summary>
    ///
    /// <param name="gameState">The current state of the game to render.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    ///
    /// <exception cref="ArgumentNullException">Thrown when gameState is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when operation is cancelled.</exception>
    ///
    Task RenderAsync(GameState gameState,
                     CancellationToken cancellationToken = default);
}
```

### Class Implementation
```csharp
namespace Squire.NumTic.Console;

/// <summary>
///   A console-based implementation of the game interface.
/// </summary>
///
public class ConsoleGameInterface : IGameInterface
{
    /// <summary>The text writer for output operations.</summary>
    private readonly TextWriter _output;

    /// <summary>
    ///   Initializes a new instance of the <see cref="ConsoleGameInterface"/> class.
    /// </summary>
    ///
    /// <param name="output">The text writer for output operations.</param>
    ///
    public ConsoleGameInterface(TextWriter? output = null)
    {
        _output = output ?? Console.Out;
    }

    /// <summary>
    ///   Renders the current game state for display.
    /// </summary>
    ///
    /// <param name="gameState">The current state of the game to render.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    ///
    /// <exception cref="ArgumentNullException">Thrown when gameState is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when operation is cancelled.</exception>
    ///
    public async Task RenderAsync(GameState gameState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gameState, nameof(gameState));
        cancellationToken.ThrowIfCancellationRequested();

        // Implementation details...
    }
}
```

## File Organization

### Using Statements
- Group system namespaces first
- Then third-party namespaces
- Finally project namespaces
- Single blank line between groups

```csharp
using System.Text;
using NUnit.Framework;
using NSubstitute;
using Squire.NumTic;
using Squire.NumTic.Contracts;
```

### Namespace Organization
- Use file-scoped namespaces: `namespace Squire.NumTic;`
- Match folder structure to namespace hierarchy
- Keep related classes in the same namespace

## Git Commit Guidelines

When making changes:
- Focus on single, atomic changes
- Write clear commit messages describing what changed and why
- Include relevant test updates with implementation changes
- Ensure all tests pass before committing

## Summary

When working on this codebase:
1. Follow the established patterns for architecture and naming
2. Always write comprehensive XML documentation
3. Use local variables in tests, never class members
4. Prefer `Assert.ThatAsync` over older assertion methods
5. Support cancellation tokens in all async operations
6. Follow the comment standards (full sentences, periods, blank lines)
7. Maintain the clean separation between game logic and UI concerns

These guidelines ensure consistency and maintainability across the entire Numeric Tic-Tac-Toe codebase.
