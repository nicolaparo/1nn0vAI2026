---
name: codebase-memory-mcp
description: Use the installed DeusData codebase-memory-mcp knowledge graph for efficient, evidence-based codebase exploration, call tracing, impact analysis, and architecture discovery.
---

# Codebase Memory MCP Skill

Use the installed `codebase-memory-mcp` server as the primary tool for structural code discovery. It builds a local knowledge graph from the repository, including functions, classes, calls, imports, routes, implementations, and cross-service relationships. Prefer graph queries over broad file-by-file searching because they are faster, more precise, and preserve context.

## Start of Every Session

Before structural exploration:

1. Call `list_projects` to find the indexed project.
2. Call `index_status` for the matching project.
3. If no project matches the current repository, call `index_repository` with the repository's absolute Windows path. Use `fast` for focused exploration, `moderate` for filtered analysis, and `full` when similarity or semantic edges are needed.
4. After indexing, record the project name, generation/status, and any `parse_partial`, `skipped`, or deliberately excluded paths.

Do not claim that a symbol, caller, dependency, or file is absent until the relevant coverage has been checked. Indexing is local; never upload source code or expose secrets in queries or responses.

## Graph-First Discovery

Use the tools in this order:

1. `search_graph` to find functions, methods, classes, routes, variables, or exact qualified names.
2. `trace_path` to inspect callers or callees. Use `direction="both"` when cross-service or full context may matter.
3. `get_code_snippet` to read the exact source for a symbol found by `search_graph`.
4. `check_index_coverage` once for every file relied on or operated on. Include relevant scopes for negative or exhaustive claims.
5. Fall back to `search_code`, `rg`, or direct file reads for string literals, configuration, documentation, generated files, and coverage gaps.

Always resolve an exact qualified name with `search_graph` before calling `get_code_snippet`. Paginate `search_graph` results when `has_more` is true. Use `query_graph` for multi-hop relationships, aggregations, cross-service analysis, or complexity queries, and put a `LIMIT` in broad Cypher queries.

## Common Workflows

### Find a symbol

```text
search_graph(name_pattern=".*CustomerService.*")
get_code_snippet(qualified_name="<exact qualified name from the result>")
check_index_coverage(paths=["<repository-relative file path>"], project="<project>")
```

### Trace a change or bug

```text
search_graph(name_pattern=".*TargetName.*")
trace_path(function_name="<exact name>", direction="both", depth=3)
detect_changes()
```

Inspect the affected source and tests, then check coverage for every relevant path before drawing conclusions.

### Analyze architecture

Use `get_architecture` for a high-level overview. Use `query_graph` for relationships such as `CALLS`, `HTTP_CALLS`, `IMPORTS`, `IMPLEMENTS`, and `DATA_FLOWS`. Use `manage_adr` only when the user asks to record or update an architecture decision.

### Find dead or risky code

- Dead-code candidates: `search_graph(max_degree=0, exclude_entry_points=true)`.
- High fan-out: search outbound `CALLS` relationships with a minimum degree.
- High fan-in: search inbound `CALLS` relationships with a minimum degree.
- For performance hotspots, query function properties such as `transitive_loop_depth`, `linear_scan_in_loop`, `alloc_in_loop`, and recursion indicators.

Treat all candidates as findings to verify in source, not as proof by themselves.

## Coverage and Fallback Rules

`check_index_coverage` reports authoritative best-effort coverage metadata for exact paths and bounded scopes:

- `indexed_no_recorded_gap` means no recorded gap, not a completeness guarantee.
- `parse_partial` means constructs in the reported line ranges may be missing; read or search those ranges directly.
- `skipped` means the file was not indexed; use direct source search.
- `excluded` or `not_indexed` means the file was intentionally omitted by ignore or skip rules.

When graph results conflict with source, treat the source as authoritative and explain the coverage limitation. Do not use graph absence alone to justify deleting code or declaring an implementation unused.

## Query Guidance

- Use repository-relative paths in `check_index_coverage`.
- Use the exact project name returned by `list_projects` or `index_repository`.
- Keep queries bounded and select only the fields needed.
- Use `query_graph` with Cypher for actual relationship edges; degree filters in `search_graph` identify nodes by connectivity but do not return the edge details.
- Use `trace_path` with exact names and appropriate depth; outbound-only traces can miss cross-service callers.
- Run `detect_changes` when assessing the impact of the current Git worktree.

## Delegation

Before delegating codebase work, gather graph evidence in the parent context: project and index status, generation/freshness, exact symbols, candidate paths, trace direction and depth, pagination state, coverage results, direct-source fallback, and unresolved questions. Pass that evidence to the sub-agent. A sub-agent without MCP access must not claim to have queried the graph.

## Safety and Scope

Use the graph to understand and verify the existing code before editing. Make surgical changes only after locating all relevant callers, implementations, tests, configuration, and cross-service edges. Do not re-index unnecessarily, delete an indexed project, or change ignore rules unless the task requires it.

