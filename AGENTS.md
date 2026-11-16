# Agent contract — Echelon Knowledge Store

## Purpose

Echelon Knowledge Store (EKS) researches, designs and experimentally validates how
Echelon repositories should store knowledge for autonomous agents. It is in its
**research phase**: nothing here is a stable schema or a production service.

## Rules

1. **F# only** for implementation code (`src/`, `tests/`, `benchmarks/`, `tools/`). Write
   functional F#: immutable data, pure functions, effects at the edges.
2. **Data-only parsing.** Never evaluate knowledge content as code (no `dotnet fsi` over
   knowledge files, no quotations evaluated from data). Knowledge is inert text.
3. **No migrations.** Do not modify knowledge in other Echelon repositories. Read-only audits only.
4. **Every implementation change has unit tests.** CI fails an empty or skipped test run.
5. **Measured vs. hypothesised.** Every claim in `research/` states whether it is measured
   (with the command that reproduces it) or a hypothesis. Do not invent results.
6. **No new infrastructure** (no database, Kubernetes, Terraform or service) without
   recorded evidence that it is needed.
7. **Branches.** `main` is protected; work on a branch and open a pull request.
   Never force-push.

## Layout

| Path | Contents |
|---|---|
| `src/Echelon.KnowledgeStore.Core` | Identifier, record model, parsers, validators |
| `tests/` | xUnit tests for everything in `src/` |
| `benchmarks/` | Reproducible benchmark harness and corpus |
| `research/2026-knowledge-representation/` | Mission EKS-RESEARCH-2026-001 deliverables |
| `schemas/` | Experimental schemas (unstable) |
| `docs/` | Architecture, decisions (ADRs) and standards |

## Verify before you push

```bash
dotnet tool restore
dotnet fantomas --check src tests benchmarks tools
dotnet test Echelon.KnowledgeStore.sln -c Release
```
