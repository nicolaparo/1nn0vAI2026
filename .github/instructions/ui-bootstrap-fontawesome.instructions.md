---
applyTo: "**/*.razor,**/*.cshtml,**/*.html,**/*.css,**/*.scss,**/*.js,**/*.ts"
---

# UI Instructions

Use Bootstrap 5 and Font Awesome Free for UI work. Prefer standard, accessible components and minimal custom styling.

## Bootstrap 5

- Use standard Bootstrap 5 components, layout, spacing, and utility classes before writing custom CSS.
- Avoid custom CSS as much as possible. If custom CSS is necessary, keep it global, small, and reusable.
- Never use Bootstrap `-outline` variants such as `btn-outline-primary`, `btn-outline-secondary`, or similar outline styles.
- Prefer solid Bootstrap button variants such as `btn-primary`, `btn-secondary`, `btn-success`, `btn-danger`, `btn-warning`, `btn-info`, `btn-light`, and `btn-dark`.
- Keep markup semantic and accessible. Use correct button, link, form, label, table, and landmark elements.

## Font Awesome Free

- Use Font Awesome Free icons when icons are needed.
- Pair icon-only controls with accessible labels such as `aria-label` or visually hidden text.
- Do not use Pro-only icons or classes unless the project already includes a valid Pro setup.

## Reuse

- If a UI element is reused or likely to be reused, create or reuse a component instead of duplicating markup.
- Keep reusable components configurable only for real use cases. Prefer simple parameters over complex component frameworks.
