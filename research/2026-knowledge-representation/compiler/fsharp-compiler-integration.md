# F# compiler integration (layer A)

## What the F# compiler gives the *engine* (used throughout the prototype)

* Discriminated unions with `TreatWarningsAsErrors`: an incomplete match over
  `RelationKind`, `Level`, `KnowledgeKind` or `Standing` is a build error, so adding a
  relation kind forces every consumer (propagation, rendering, codecs) to handle it.
  This caught real omissions during development.
* Records and private constructors (`KnowledgeId`, `NonEmpty`) make single-record
  invariants unrepresentable in the strict model (`Strict.fs`).
* Nullness checking (F# 9+/10) forced explicit handling of every .NET API that can
  return null (path APIs, JSON).
* Typed API contracts between pipeline stages; the CLI and benchmarks consume the
  same types, so model changes break callers at compile time.

## Knowledge expressed as F# (experiment)

`benchmarks/Echelon.KnowledgeStore.FcsProbe` generates an F# module from the
416 unique public records (ids as union cases, status/confidence as unions,
relations as `Id list`) and type-checks it with FSharp.Compiler.Service
43.12.403 (`ParseAndCheckFileInProject`). Nothing is compiled to an assembly or run.
Results (`experiments/results/fcs-probe.md`):

| Measure | Value |
|---|---|
| Generated source | 2,110 lines |
| Checker start-up (first check) | ~1.9 s |
| Type-check full corpus | 0.78 s cold, 0.07 s warm |
| Status typo / confidence typo / field typo | compile error (FS0039 / FS0039 / FS1129) |
| Relation id typo / lower-cased id | compile error (FS0039): referential integrity at compile time |
| "supported" despite contradicting evidence | accepted (not a type error) |
| Self-reference / cycles / asymmetric links | accepted (not type errors) |

## Limits

* The type system checks **shape and existence**, not meaning, freshness,
  authority or truth.
* Ids as union cases need one case per record: every new record changes the
  type, i.e. knowledge edits become schema edits and full re-checks.
* Reading F# safely requires the compiler service (≈ tens of MB of dependencies);
  executing it (`dotnet fsi`) runs arbitrary code — rejected.
* All type-level checks above are reproduced by the layer-B compiler on the
  Markdown source (EKS0022/0023/0024/0002) in ~40–60 ms warm, without making
  knowledge executable-looking code.

**Verdict:** use the F# compiler for the knowledge *engine and schema*, not as the
validator of knowledge *data*.
