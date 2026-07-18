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
- Within a method body, separate logical steps with single blank lines and keep tightly-coupled statements (a declaration and the statement that consumes it, a two-line swap) together; set control-flow blocks off with blank lines and precede a trailing `return` with one. This is a readability judgment, not a mechanical one-blank-per-statement rule.

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

Order within a class: **Constants → Fields → Events → Properties → Constructors → Methods → Nested Types**

Within each section, order by visibility **most-visible-first**: **public → internal → protected → private**

Static vs instance rules:
- Constants, fields, properties: static members before instance members
- Methods: instance methods before static methods
- Blank line between static and instance groups

```csharp
public class ExampleClass
{
    // Constants (static first, by visibility)
    public const string Version = "1.0";
    private const int DefaultValue = 10;

    // Fields (static first, by visibility, then instance; readonly before mutable)
    public static readonly string DefaultName = "Default";
    private static readonly object Lock = new();

    public readonly int Id;
    private readonly IService Service;

    // Properties (static first, by visibility, then instance by visibility)
    public static string GlobalSetting { get; set; }
    private static int StaticCounter { get; set; }

    public string Name { get; set; }
    private int _counter;

    // Constructors
    public ExampleClass() { }
    public ExampleClass(IService service) { }

    // Methods (instance first, by visibility, then static by visibility)
    public void DoSomething() { }
    private void HelperMethod() { }

    public static void StaticMethod() { }
    private static void StaticHelperMethod() { }

    // Nested Types
    public enum Status { }
    private class NestedClass { }
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

Order: System → third-party → project namespaces, all **contiguous** (no blank lines between these groups). Separate only a genuinely different directive kind (a `using static` or a `using X = ...` alias) with a blank line. Alphabetize within each group. Place using directives outside the file-scoped namespace. Omit a `using` for a namespace already in scope through an enclosing namespace (for example, a `Squire.Foo.Tests` file does not need `using Squire.Foo;`).