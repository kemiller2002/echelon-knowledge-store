# Knowledge compilation (architecture)

```
Markdown + front matter (canonical, human-edited, reviewed in PRs)
        │ data-only parse (Yaml.fs: no aliases, no tags-as-types, event limit)
        ▼
IR: KnowledgeRecord (Codec.toYaml/ofYaml) ── validation (Validation.fs, Strict.fs)
        │
        ├──► graph (derived inverse links, typed impact)
        ├──► INDEX.md / index.jsonl (cards)               [implemented]
        ├──► task context packages                         [implemented]
        ├──► JSON / JSONL / YAML / DSL exports            [implemented, round-trip tested]
        ├──► ADR-style decision log (generated view)       [designed]
        ├──► traceability report (requirement → tests)     [designed; needs implements/tests relations]
        ├──► Dokimos obligations (requirement → check)     [designed; Dokimos interface not verified]
        └──► Praxis work dependencies                      [designed; not verified]
```

Properties required and status:

| Property | Status |
|---|---|
| Lossless parse of canonical source | measured 568/568 |
| Deterministic output | measured after fix (sorted links) |
| Diff locality | measured: one status edit → one changed index line |
| Source maps | every card carries `repository/path`; packages cite sources |
| Incremental compilation | not needed (< 0.1 s for 585 records) |
| Generated artefact verification | CI regenerates and diffs (proposed `eks check --generated`) |
| Round-trip editing | edits happen in the canonical Markdown only; generated views are read-only |

Generated views must be marked as generated and never edited by hand. That
removes the synchronisation problem that made 5 of 6 hand-written indexes stale.
