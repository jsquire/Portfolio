---
applyTo: "**/*.cs"
---

# C# Essentials (Auto-Applied)

When working with C# files in this repository:

- Always prefer `var`; 4-space indentation; Allman brace style
- PascalCase for types, methods, properties, constants, readonly fields; `_camelCase` only for mutable fields; `I` prefix for interfaces
- Validate parameters with `ArgumentNullException.ThrowIfNull`; support `CancellationToken` in async methods
- Write XML documentation for all members regardless of visibility; use `<inheritdoc />` for well-known standard members (e.g. `ToString()`), prefer explicit docs elsewhere; inline comments as full sentences ending with periods, followed by blank lines
- File-scoped namespaces; group usings: System → third-party → project; alphabetize within each group
- Member order: Constants → Fields → Properties → Constructors → Methods → Nested Types; private → public visibility; static before instance (except methods: instance before static)

For detailed conventions, reference the skills in `.github/skills/`.