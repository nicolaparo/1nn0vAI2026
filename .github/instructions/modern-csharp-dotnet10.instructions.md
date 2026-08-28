---
applyTo: "**/*.cs"
---

# Modern C# and .NET 10 Instructions

Use modern C# targeting .NET 10 and C# 14 where it improves clarity, safety, or performance. Prefer stable platform patterns over novelty for its own sake.

## Code Style

- Be a senior C# developer: clear names, small focused types, explicit dependencies, and simple control flow.
- Prefer simplicity first. Avoid speculative abstractions, over-engineering, and clever code.
- Follow SOLID, KISS, and DRY pragmatically. Extract shared behavior when duplication is real, but do not introduce abstractions for one-off code.
- Never use an `_` prefix for members, fields, parameters, local variables, or captured variables.
- Use camelCase for private fields and local variables without leading underscores.
- Prefer nullable reference type correctness, pattern matching, collection expressions, primary constructors, and other modern language features when they make code simpler.
- Keep public APIs intentional and minimal. Do not expose mutable state unless required.

## Async and Tasks

- Use `async` and `await` for asynchronous work. Prefer async all the way through the call stack.
- Never block on asynchronous code with `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`.
- Never fire and forget tasks. Every `Task` must be awaited, returned, composed with `Task.WhenAll`/`Task.WhenAny`, or handed to an explicit background-processing abstraction that observes exceptions and cancellation.
- Async methods must end with the `Async` suffix.
- Prefer `Task` or `Task<T>` for async APIs. Use `ValueTask` or `ValueTask<T>` only when there is a measured or well-understood allocation benefit.
- Accept and propagate `CancellationToken` for I/O, long-running operations, and request-scoped work.
- Avoid `async void` except for framework-required event handlers. Event-handler bodies should delegate to `Task`-returning methods ending in `Async` whenever possible.
- Do not swallow exceptions from asynchronous work. Handle expected failures explicitly and let unexpected failures surface through normal error handling.

## .NET 10 Guidance

- Prefer the `dotnet` CLI for creating, wiring, restoring, building, running, testing, formatting, and publishing .NET solutions and projects.
- Do not manually create project files, solution files, generated template files, package references, or project references when a `dotnet` CLI command can do it reliably.
- Use dependency injection, options, logging, and configuration through the built-in .NET abstractions.
- Prefer source-generated or platform-provided features when they reduce boilerplate without hiding behavior.
- Use minimal APIs, endpoint filters, hosted services, and typed clients only when they fit the application shape.
- Keep performance improvements readable. Use spans, pooling, or low-allocation patterns only where they are justified by the code path.
