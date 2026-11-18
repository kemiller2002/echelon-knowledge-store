# Economic and performance analysis (Phase 7)

Mission EKS-RESEARCH-2026-003 · 2026-11-18. Reproduce with
`python3 -I experiments/clarity-edf/analyze.py experiments/clarity-edf` and then
`python3 -I experiments/clarity-edf/economics.py experiments/clarity-edf`
(`economics.json`).

## What was measured and what was assumed

| Quantity | Status |
|---|---|
| Wall time per run, tool uses | **measured** (harness task notifications, `raw/round*/usage.json`) |
| Verified outcomes, false completion claims, scope violations, test weakening | **measured** (deterministic oracle, `grade.py`) |
| Requester questions (proxy for human interventions) | **measured** (agents' `REPORT.json`) |
| Model inference cost in currency | **unavailable.** The harness exposes `subagent_tokens`, which behaves like final context size: a fixed ~65k-token floor plus 1–14k task-dependent tokens. It is not billed usage, and it is not converted to currency here. |
| Human minutes per question, rework minutes per escaped false completion, value of an agent minute | **assumed** (three scenarios below) |
| Maintenance overhead, escaped defects in production | **not measured** (no production history in scope) |

Following the Praxis rule, unavailable data is never recorded as zero.

## Model

total = agentMinutes × k + questions × Q + falseCompletions × R, and
cost per verified outcome = total ÷ verified.

| Scenario | Q (min/question) | R (min/escaped false completion) | k (human-min per agent wall-min) |
|---|---|---|---|
| low | 2 | 30 | 0.05 |
| central | 5 | 60 | 0.10 |
| high | 10 | 240 | 0.20 |

## Results: human-equivalent minutes per verified outcome

| Arm | R1 low | R1 central | R1 high | R2 low | R2 central | R2 high |
|---|---|---|---|---|---|---|
| A baseline | 1.02 | 2.54 | 5.07 | 6.82 | 14.04 | 52.07 |
| B Praxis discipline | 1.03 | 2.56 | 5.11 | 1.69 | 4.22 | 8.44 |
| C Clarity | 1.70 | 4.23 | 8.46 | 1.36 | 3.39 | 6.78 |
| D EDF | 1.37 | 3.41 | 6.81 | 11.05 | 23.84 | 77.69 |
| E Clarity+EDF | 1.37 | 3.42 | 6.83 | 1.70 | 4.23 | 8.47 |
| N neutral | 0.69 | 1.71 | 3.43 | 1.64 | 4.07 | 8.15 |
| F adaptive (derived) | 1.36 | 3.39 | 6.79 | 8.03 | 17.07 | 58.14 |

## Interpretation

1. **Agent time is not the cost driver.**
   - Analysis arms cost 30–124% more wall time, but at any plausible k that is under a
     minute of human-equivalent time per task.
   - The terms that dominate are human attention: questions, and above all rework after
     a false completion claim.
   - Optimising tokens or wall time alone would optimise the wrong term.
2. **Round 1 (ceiling):**
   - The cheapest arm per verified outcome is the one that asked the fewest questions
     (N: 2 questions; C: 5).
   - **Caution:** most questions were *appropriate*. On T6 every arm correctly asked
     before deleting a function the accounting export uses. Counting an appropriate
     escalation as a cost penalises good behaviour.
   - The model needs a correctness-weighted question cost, and this program has no data
     to set it. The round-1 ranking is therefore **not decision-grade**.
3. **Round 2 (headroom):**
   - The arms with a false completion claim (A and D on T4) are 3–9× more expensive per
     verified outcome than B, C and E, under every scenario.
   - **This rests on one task and one false claim per arm. It is a direction for the
     next experiment, not a finding.**
   - It points at the same lever as EKS mission 002 (0 vs 8 false completion claims with
     compiled packages) and the verification audit (V3: verified completion is not
     deployed): **preventing false "complete" claims is where the money is.**
4. **Cost savings vs reliability.**
   - Nothing here is a cost saving in absolute terms. Every intervention adds agent time.
   - Any benefit is a reliability improvement that becomes a cost improvement only when
     an escaped false completion is expensive (R large).
   - Where R is small (throwaway prototypes), baseline is cheapest.

## Brief metrics: availability

| Metric | Available? |
|---|---|
| Time to verified completion | measured (mean wall per arm; agent runs only, excludes review) |
| Human interventions per task | proxy measured (requester questions) |
| Human attention minutes per task | **assumed** (no instrumentation exists in Praxis; see roadmap item R3) |
| First-pass verification rate | measured (= verified rate; there were no retries) |
| Regression rate | measured: 0 (no run broke a visible or hidden pre-existing test, except the T4 cases above) |
| False-positive verification rate | measured: false completion claims 2/72 runs (both Haiku, T4) |
| Rework frequency | not observable in single-shot runs |
| Token usage | proxy only (see above) |
| Decision-quality failures | 1 mechanism identified (EDF "premature" ranking, D-T4) |
| Successful autonomous completion rate | measured: 68/72 runs verified (2 of the 4 misses are harness-blocked) |

## What Echelon must instrument to answer this properly

1. **Human-attention minutes per work item.**
   - Today Praxis records agent telemetry only. Add a `human.attention` step type with
     start and stop times.
   - Unavailable stays unavailable: never estimated as zero.
2. **An escaped-defect link.**
   - When a completed work item is later reopened or a regression is traced to it, record
     the link.
   - Without it, R is a guess.
3. **Billed usage.** Ingest the provider's usage report, which Praxis's `UsageCost`
   already models (provider-reported vs estimated), instead of a harness proxy.
