---
applyTo: "**/*.razor,**/*.razor.cs,**/*.cshtml,**/Components/**/*.cs"
---

# Blazor Instructions

Build Blazor code with simple, reusable components and standard framework patterns.

## Components

- Prefer reusable components over repeated markup or duplicated UI logic.
- Keep components focused on one responsibility. Move business logic to services instead of embedding it in markup-heavy components.
- Use component parameters, `EventCallback`, cascading values, and dependency injection using standard Blazor patterns.
- Prefer `OnInitializedAsync`, `OnParametersSetAsync`, and other async lifecycle methods when work is asynchronous.
- Async event handlers and component methods must use `async`/`await` and end with the `Async` suffix.
- Never use `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`, or fire-and-forget tasks in components.

## Styling

- Never use Blazor CSS isolation. Do not create or rely on `.razor.css` files.
- Put styles in global stylesheets and keep them minimal.
- Prefer Bootstrap 5 utilities and standard components over custom CSS.
- Add custom CSS only when Bootstrap 5 cannot express the design clearly.
