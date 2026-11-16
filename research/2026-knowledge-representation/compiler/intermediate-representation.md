# Intermediate representation (compiler)

The compiler reuses the research IR (`../architecture/intermediate-representation.md`):
inert `Yaml` tree ↔ `KnowledgeRecord`. Compiler-specific additions:

| Element | Purpose |
|---|---|
| `SourceFile { Repository; Path; Text; Hash }` | identity and content hash of every input; basis of incremental builds, staleness and provenance |
| `ParsedUnit { File; Record option; Diagnostics }` | per-file result; cached by `(repository/path, hash)`; parse failures are units with diagnostics, never drops |
| `KnowledgeGraph` | typed resolved edges + unresolved relations (kept for diagnostics) |
| `Precomputed` | partially evaluated queries: `Impact` (closure with hop distance), `Evidence` (supporting/contradicting), `Current` (authority policy), `Dependencies` (closure) |
| `Diagnostic { Code; Level; Record; Location(repo, path, line, field); Message; Remediation }` | compiler-style findings |
| `Item { Key; Section; Full }` + `LedgerEntry` | what an output contains and every transformation applied to it |

Source maps: every card prints `repository/path` and the first 12 hex digits of
the source file's SHA-256; diagnostics carry file and line.
