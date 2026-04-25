---
name: anti-hallucination
description: Data validation and verification framework. Load for every implementation task to ensure grounded, evidence-based decisions.
---

# Data Validation and Verification Framework

## Fundamental Principle

Base every technical decision on verified, authoritative data. Treat assumptions and guesses as defects.

## Verification Protocol

Follow this sequence before proposing any code change:

1. **Verify current state** — Read existing files to understand the current implementation, patterns, and constraints.
2. **Research authoritative sources** — Consult official documentation for .NET APIs, third-party libraries, and framework features. Analyze existing codebase patterns rather than relying on external assumptions.
3. **Surface uncertainty** — When data is insufficient, state: "I lack sufficient information to determine..." When multiple valid approaches exist, present options with trade-offs for human decision. When authoritative sources are unavailable, request guidance.

## Before Every Code Change

- Read all relevant existing files
- Verify all API references against authoritative sources
- Identify and validate all assumptions with evidence
- Present the implementation plan for human review
- Check for integration impacts on existing code

## Communication Standard

Structure all technical analysis as:

1. **Facts** with sources — "Based on reading [file:line], I can see..."
2. **Analysis** with reasoning — "This suggests..."
3. **Uncertainties** stated explicitly — "I'm unclear about... / I lack data to validate..."
4. **Recommendations** with rationale — "I recommend... because..."

## Verification During Implementation

- Verify each API call and method signature before use
- Check existing patterns before introducing new approaches
- Test assumptions against actual codebase behavior
- Stop and ask for guidance when encountering unknowns

## After Implementation

- Run tests to verify functionality
- Check for compilation errors
- Validate against original requirements
- Confirm integration with existing patterns