# Cold and warm compilation

Source: `results/compiler.md` (`compiler` command), medians of 5 runs after a JIT
warm-up, single process, research container.

| Scenario | Files | Median |
|---|---|---|
| cold — public corpus (11 repos) | 1,615 | 363 ms |
| cold — all 13 repositories | 1,931 | 152 ms |
| parse only, public | 1,615 | 87 ms |
| warm, no change | 1,615 | 55 ms |
| warm, 1 file changed | 1,615 | 45 ms |
| warm, 10% changed | 1,615 | 66 ms |

The cold figures vary between 150 and 370 ms across runs: tiered JIT recompiles hot
paths during the first runs, which the warm-up does not fully absorb. Either way the
cost is below a second. Warm outputs were byte-identical to cold outputs in every
scenario.

Partial evaluation cost is included in every figure: closure, evidence, current
authority and dependency closure are precomputed for every record on every compile.

Process start-up of the `eks` CLI (.NET) adds ~1–2 s per invocation; a full
`eks validate` over the public corpus took 2.4 s wall time.
