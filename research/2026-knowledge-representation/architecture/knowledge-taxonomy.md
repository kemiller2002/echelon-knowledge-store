# Knowledge taxonomy (candidate, tested against the corpus)

The mission's proposed list of 23 categories was **not** adopted as given. It was
compared with what Echelon repositories actually record (597 records, public and
private; `experiments/results/validation.md`).

## Observed kinds

| Kind | Records | Id prefixes / directories | Notes |
|---|---|---|---|
| Decision | 134 | DF-*, ADR-* · `research/decisions` | incl. architecture, product and governance decisions |
| Governance | 125 | GV-*, SDE-DOCTRINE-* · `docs/00-governance` | mostly vendored copies (29 cross-repo id collisions) |
| Document | 77 | various | records whose kind is not encoded; candidates for typing |
| Evidence | 71 | EV-* · `research/evidence` | two conventions: structured source fields, or links only |
| Method / Procedure | 70 | SDE-METHOD-* | procedural knowledge |
| Requirement | 56 | RQ-* · `requirements/` | plus thousands of requirements in tables/lists outside records |
| Hypothesis | 24 | HY-* | carry status, confidence, supporting/contradicting evidence |
| Experiment | 16 | EX-* | 4 lack a recorded hypothesis link |
| Journal | 12 | JR-* | episodic log |
| Research package | 12 | RP-* | aggregate / conclusion-bearing |
| Theory | 0 public | TH-* | template only |

## Proposed taxonomy (minimal)

Ten kinds cover every observed record. Each kind earns its place by a distinct
validation rule or retrieval behaviour:

| Kind | Distinct rule | Mission categories merged in |
|---|---|---|
| Requirement | needs acceptance criteria | requirement, constraint, capability, interface |
| Decision | needs grounds (rationale section, evidence or links); lifecycle with replacement | decision, rationale, policy, recommendation, supersession |
| Hypothesis | standing derived from evidence | hypothesis, claim, assumption, prediction |
| Evidence | needs a source (structured or linked); observed/retrieved date | evidence, observation, fact, result |
| Experiment | needs a hypothesis; outcome section | experiment |
| Conclusion (research package) | should cite evidence | conclusion |
| Governance | authority rules (see security model) | policy |
| Method | — | procedure |
| Journal | — (episodic) | — |
| Document | — (untyped fallback) | definition, risk, dependency, open question |

**Merged rather than separate:** *Fact* (a fact is evidence-backed — use Evidence
or a supported Hypothesis); *Rationale* (a section of a Decision); *Supersession*
(a relation, not a record); *Dependency* (a relation); *Assumption/Prediction/Claim*
(Hypothesis with an `epistemic_form` field if needed). *Risk*, *Definition* and
*Open question* are kept as optional subtypes of Document until a rule needs them;
none of them has enough records in the corpus to test.

## Status

EXPERIMENTAL. The taxonomy fits 100% of observed records, but no record type was
added by EKS. Whether Risk, Definition and Open question need first-class kinds is
an unresolved question.
