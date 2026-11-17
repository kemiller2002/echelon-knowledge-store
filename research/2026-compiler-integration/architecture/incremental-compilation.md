# Incremental compilation

Harness: `benchmarks/Echelon.KnowledgeStore.Bench/IncrementalBench.fs` (`dotnet run … -- incremental`).
Raw results: `../experiments/raw/incremental-compilation.json`. Corpus: the public repositories of the
workspace (the same loader as Phase 1's compiler benchmark). Run twice: once while 16 trial agents were
building concurrently, once on an idle machine after the source-rule fix. The numbers below are from
the idle run; the first run (under load) gave medians of 153.0 ms (full) and 62.0 ms (incremental)
with identical staleness. Only the idle run's raw output is kept (the file was overwritten).

## Method

25 target records (requirements, hypotheses, decisions with at least three neighbours) chosen by a
seeded hash. A deterministic sequence of 60 edits (seed 20261116) touches their neighbourhoods:
body edits (10), status changes (13), deleted neighbours (12), new evidence supporting a target (11),
new requirements depending on a target (7), and new records colliding with a target's id (7).
After every step each strategy produces the 25 task packages, and they are compared with a full
compile from scratch. A package that differs is **stale**. The header's corpus-wide inputs hash is
excluded from the comparison: a memo can refresh it cheaply, and it would otherwise mark every
memoised package stale trivially.

## Results (measured)

| Strategy | Stale packages (of 1,525) | Steps with any stale package (of 61) | Median ms per step | Total ms |
|---|---|---|---|---|
| Full compile every time | 0 | 0 | 158.9 | 9,647 |
| Incremental (parse cache keyed by file hash + parse configuration) | **0** | **0** | **62.3** | 3,980 |
| Package memo keyed by the hashes of the package's item files (a common shortcut) | 508 | 60 | 0.8 | 1,345 |
| On-demand (compile only the target's outgoing closure) | 1,336 | 61 | 234.2 | 14,683 |
| Hybrid (parse cache + package memo keyed on everything the package reads) | 0 | 0 | 106.6 | 6,794 |

Configuration change with a warm cache (schema version raised, files unchanged): incremental output
equals a fresh compile (true). Before this phase's fix it would not have (regression test
`parse cache is not reused across parse-relevant configuration`).

## Findings

1. **The parse cache is the only optimisation that pays.** It is 2.5× faster than a full compile by median step time (2.4× by total time)
   and was never stale. Parsing dominates; graph building, diagnostics and package assembly are
   cheap by comparison.
2. **Package memoisation by item files is unsafe.** It went stale on the first step: a new
   evidence record in *another* file adds an incoming edge, which changes the package, but no item
   file changed. A fast cache that returns stale requirements is unacceptable, so this strategy is
   rejected.
3. **On-demand compilation is wrong by construction.** Following only outgoing references misses
   incoming edges (supporting evidence, dependent requirements, amendments), so packages were wrong
   before any edit. It was also slower, because each target re-parses its closure. Rejected. The
   hard coding trials show the cost of missing incoming edges in agent outcomes, not only in
   package text (`../experiments/compiler-results.md`).
4. **A correct memo is slower than not memoising.** Keying on everything a package reads (item
   files, every edge touching an item, diagnostics on items, rule runs, configuration) was never
   stale, but computing the key costs more than assembling the package (106.6 vs 62.3 ms).
   Rejected until packages become expensive to build.
5. **A stale-cache defect existed.** Parsed units were reused on file hash alone, although parsing
   also depends on the compiler version and the supported schema. The cache key now includes both.
   The benchmark's design (asking "what does parsing read?") found it, not a failing run.

## Recommendation

Use the parse cache (incremental) for interactive and CI compilation. Keep full compilation as the
reference in CI (`eks verify` compares artefacts with sources and refuses stale ones). Do not
memoise packages. If a persistent cache is added later, its key must be (file hash, compiler
version, parse configuration), and a full-compile equivalence check should run in CI.
