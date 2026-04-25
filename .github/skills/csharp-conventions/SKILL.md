---
name: csharp-conventions
description: C# code standards including naming, formatting, member organization, architecture patterns, and performance guidelines. Load when writing or modifying C# code.
---

# C# Code Conventions

## Language & Framework

- **Target Framework**: Latest available long-term support version of .NET (e.g., .NET 10 or later)
- **Language Version**: Latest C# features including records, pattern matching, nullable reference types
- **Nullable Context**: Enabled throughout
- **var Usage**: Always prefer `var`

## Formatting

- 4-space indentation, Allman brace style, LF line endings, UTF-8 encoding
- Omit final newlines in files

## Naming

| Element | Convention | Example |
|---|---|---|
| Classes | PascalCase | `GameState`, `ConsolePlayer` |
| Interfaces | `I` prefix + PascalCase | `IPlayer`, `IGameInterface` |
| Methods | PascalCase | `PlayTurnAsync` |
| Properties | PascalCase | `CurrentTurn` |
| Constants | PascalCase | `StackAllocThreshold`, `DefaultTokensPerRow` |
| Readonly Fields | PascalCase | `SerializerOptions`, `Interface` |
| Mutable Fields | `_` prefix + camelCase | `_movesSinceLastRandom`, `_counter` |
| Parameters/Locals | camelCase | `gameState`, `cancellationToken` |

**Key rule**: The `_` prefix is reserved for mutable fields only. Constants, `static readonly`, and instance `readonly` fields use PascalCase.

## Member Organization

Order within a class: **Constants → Fields → Properties → Constructors → Methods → Nested Types**

Within each section, order by visibility: **private → protected → internal → public**

Static vs instance rules:
- Constants, fields, properties: static members before instance members
- Methods: instance methods before static methods
- Blank line between static and instance groups

```csharp
public class ExampleClass
{
    // Constants (static first, by visibility)
    private const int DefaultValue = 10;
    public const string Version = "1.0";

    // Fields (static first, by visibility, then instance by visibility)
    private static readonly object Lock = new();
    public static readonly string DefaultName = "Default";

    private readonly IService Service;
    public readonly int Id;

    // Properties (static first, by visibility, then instance by visibility)
    private static int StaticCounter { get; set; }
    public static string GlobalSetting { get; set; }

    private int _counter;
    public string Name { get; set; }

    // Constructors
    public ExampleClass() { }
    public ExampleClass(IService service) { }

    // Methods (instance first, by visibility, then static by visibility)
    private void HelperMethod() { }
    public void DoSomething() { }

    private static void StaticHelperMethod() { }
    public static void StaticMethod() { }

    // Nested Types
    private class NestedClass { }
    public enum Status { }
}
```

## Architecture Patterns

- **Dependency injection**: Constructor injection for all dependencies; interface-based abstractions
- **Async**: All I/O operations async; always accept `CancellationToken` with default value; suffix methods with `Async`
- **Parameters**: Validate with `ArgumentNullException.ThrowIfNull(param, nameof(param))`
- **Cancellation**: Call `cancellationToken.ThrowIfCancellationRequested()` at appropriate points
- **Exceptions**: Use standard .NET types (`ArgumentNullException`, `ArgumentOutOfRangeException`, `InvalidOperationException`)

## Performance

- Respect allocation concerns — Gen0 collections have real costs
- Use `Span<T>`, `Memory<T>`, `ArrayPool<T>`, `stackalloc` where appropriate
- Avoid unnecessary LINQ chains creating intermediate collections
- Measure actual performance impact when evaluating trade-offs
- Use `ConfigureAwait(false)` in library code
- Avoid `async void` except for event handlers
- Use `using` statements for disposable resources; prefer `IReadOnlyCollection`/`IReadOnlyDictionary` for immutable collections

## Using Statements

Group: System → third-party → project namespaces, with blank lines between groups. Alphabetize namespaces within each group. Use file-scoped namespaces.