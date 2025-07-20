# C# Coding Standards and Conventions

This document provides guidelines for GitHub Copilot when working on C# projects. These instructions help maintain consistency with established codebase standards, conventions, and architectural patterns.

## Code Standards & Conventions

### C# Language Features

- **Target Framework**: .NET 9.0 (or latest available)
- **Language Version**: Latest C# features including records, pattern matching, and nullable reference types
- **Nullable Context**: Enabled throughout the project
- **var Usage**: Prefer `var` for local variables when type is apparent: `var result = CreateValidObject();`

### Formatting & Style

- **Indentation**: 4 spaces (no tabs)
- **Brace Style**: Allman style (opening braces on new lines)
- **Line Endings**: LF (Unix-style)
- **Encoding**: UTF-8
- **Final Newlines**: Do not insert final newlines in files

### Naming Conventions

- **Classes**: PascalCase (`UserService`, `DataProcessor`)
- **Interfaces**: PascalCase with `I` prefix (`IUserService`, `IDataProcessor`)
- **Methods**: PascalCase (`ProcessDataAsync`, `CreateValidUser`)
- **Properties**: PascalCase (`UserName`, `IsValid`)
- **Fields**: camelCase with underscore prefix (`_userService`, `_configuration`)
- **Parameters**: camelCase (`userData`, `cancellationToken`)
- **Local Variables**: camelCase (`mockService`, `result`)

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
public async Task<Result> ProcessAsync(Data input,
                                       CancellationToken cancellationToken = default)
```

## Testing Standards

### Test Organization

- **Test Namespace**: All test classes must use root namespace for the tests project. For example, `NumTic.Tests` (never area-specific namespaces like `NumTic.Tests.Game`)
- **Test File Organization**: Create test files on a **per-class basis** - all behavior for a class should be tested in a single corresponding test file (e.g., `GameState` → `GameStateTests.
- **No Regions**: Do not use `#region`/`#endregion` directives
- **Local Variables Only**: No class-level test subjects or setup methods
- **Test Categories**: Use `[Category]` attributes for grouping (`[Category("Integration")]`)
- **Non-Parallelizable**: Mark test classes with `[NonParallelizable]` if needed
 
### Test Naming Conventions
- **Use generic concepts** rather than specific exception types in test names
- **Good**: `PlayTurnAsyncThrowsForInvalidGameState`
- **Bad**: `PlayTurnAsyncThrowsArgumentOutOfRangeExceptionForNullGameState`
- Focus on the **behavior being tested** rather than implementation details
- Keep names **descriptive but concise**

### Test Structure

```csharp
[Test]
public async Task MethodNameWithScenarioShouldExpectedBehavior()
{
    // Arrange: Create local instances.
    var mockService = Substitute.For<IDataService>();
    var processor = new DataProcessor(mockService);
    var input = CreateValidInput();

    // Act & Assert: Use Assert.ThatAsync for async operations.
    await Assert.ThatAsync(async () => await processor.ProcessAsync(input),
        Throws.Nothing);
}
```

### Assertion Standards

- **Never use `Assert.Throws`**: Always use `Assert.That` or `Assert.ThatAsync`
- **Async Assertions**: Use `Assert.ThatAsync` for async operations
- **Exception Testing**: Use `Throws.ArgumentNullException.With.Property(...)`
- **Console Redirection**: Redirect `System.Console.Out` in tests that involve console output

### Mocking with NSubstitute

- Use NSubstitute for all test doubles
- Create mocks locally in each test method
- Use `Substitute.For<IInterface>()` for interface mocks
- Verify interactions with `.Received()` and `.DidNotReceive()`

```csharp
// Setup mock behavior.
mockService.GetDataAsync(Arg.Any<CancellationToken>())
    .Returns(expectedData);

// Verify interactions.
await mockService.Received().ProcessDataAsync(
    Arg.Any<string>(),
    Arg.Is<int>(x => x > 0),
    Arg.Any<CancellationToken>());
```

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
namespace Company.Project.Contracts;

/// <summary>
///   Defines the contract for data processing operations.
/// </summary>
///
public interface IDataProcessor
{
    /// <summary>
    ///   Processes the provided data asynchronously.
    /// </summary>
    ///
    /// <param name="data">The data to process.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    ///
    /// <returns>The processed result.</returns>
    ///
    /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when operation is cancelled.</exception>
    ///
    Task<ProcessResult> ProcessAsync(InputData data, CancellationToken cancellationToken = default);
}
```

### Class Implementation

```csharp
namespace Company.Project.Services;

/// <summary>
///   A service implementation for data processing operations.
/// </summary>
///
public class DataProcessor : IDataProcessor
{
    /// <summary>The service used for data operations.</summary>
    private readonly IDataService _dataService;

    /// <summary>
    ///   Initializes a new instance of the <see cref="DataProcessor"/> class.
    /// </summary>
    ///
    /// <param name="dataService">The service for data operations.</param>
    ///
    public DataProcessor(IDataService dataService)
    {
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
    }

    /// <summary>
    ///   Processes the <paramref name="data"/> asynchronously.
    /// </summary>
    ///
    /// <param name="data">The data to process.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    ///
    /// <returns>The processed result.</returns>
    ///
    /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when operation is cancelled.</exception>
    ///
    public async Task<ProcessResult> ProcessAsync(InputData data,
                                                  CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data, nameof(data));
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
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using NSubstitute;
using Company.Project.Core;
using Company.Project.Contracts;
```

### Namespace Organization

- Use file-scoped namespaces: `namespace Company.Project.Services;`
- Match folder structure to namespace hierarchy
- Keep related classes in the same namespace

## Git Commit Guidelines

When making changes:

- Focus on single, atomic changes
- Write clear commit messages describing what changed and why
- Include relevant test updates with implementation changes
- Ensure all tests pass before committing

## Summary

When working on C# codebases:

1. Follow the established patterns for architecture and naming
2. Always write comprehensive XML documentation
3. Use local variables in tests, never class members
4. Prefer `Assert.ThatAsync` over older assertion methods
5. Support cancellation tokens in all async operations
6. Follow the comment standards (full sentences, periods, blank lines)
7. Maintain clean separation of concerns and interface-based abstractions

These guidelines ensure consistency and maintainability across C# projects.
