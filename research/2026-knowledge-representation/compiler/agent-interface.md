# Agent interface

Implemented: `eks` CLI (`tools/Echelon.KnowledgeStore.Cli`) over an F# library.

```
eks validate | compile --out DIR | verify --artifacts DIR
eks query ID | explain ID | context ID | evidence ID | impact ID | obligations ID
eks diff BASE_ROOT HEAD_ROOT
  --root DIR --repos a,b --private a,b --audience public|private --as-of YYYY-MM-DD --allow-incomplete
```

Exit codes: 0 ok · 1 not found/usage · 2 stale artefacts · 3 incomplete output ·
4 compilation errors. Agents can rely on non-zero codes instead of parsing text.

## CLI vs library vs service

| Option | Latency per query (measured) | Fit for agents |
|---|---|---|
| CLI, compiling on each call | ~1–2.5 s process start + 0.15–0.4 s compile | works with every coding agent (they all run shell commands); no state |
| CLI over precompiled artefacts (`--artifacts`) | same start-up; answers from compiled outputs after a freshness check | recommended |
| F# library in-process | ~0.4–0.5 ms per package after compile | for Praxis or other F# tools |
| Persistent service | not built | no measured need: compile is sub-second and packages are sub-millisecond |

`eks query` with a missing or ambiguous id returns exit 1 and lists the qualified
candidates (`repo:ID`); agents never get a silent best guess.

Not implemented from the proposed interface: `eks context task TASK-…` (no task
records exist in the corpus; packages are keyed by record), `eks obligations task`
(per-record only).
