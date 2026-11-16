# Executive report — EKS-RESEARCH-2026-001

Status: **PARTIALLY COMPLETE** (see "Gaps"). Date: 2026-11-16.

## Answer

Markdown is not the problem. Echelon's Markdown already carries typed metadata in
YAML front matter, and capable agents answer relational questions from it at ceiling
accuracy. Removing that metadata costs ~25 points of accuracy; changing its syntax
changes nothing. The value lies in **compiling** the existing Markdown — deriving
links, closures and authority, validating agent edits, and delivering task-specific
outputs — which halves agent time and catches most edit mistakes, but does not
reduce end-to-end tokens. Praxis already implements the foundation
("Markdown canonical, registries generated"); EKS should extend it, not replace it.

## Measured highlights

* Corpus: 13 repositories, 2,149 Markdown files, 3.42M tokens; 25% of tokens are
  byte-identical vendored copies; 0 typed cross-repository edges.
* Formats: syntax moves total tokens by ≤ 13%; DSL saves 0.7%; F# records +75% metadata.
* Trial 1 (144 answers): metadata present → 0.98–1.00; stripped → 0.72–0.76.
* Trial 2 (54 answers, A/B/C/D): raw Markdown 0.99, optimised retrieval 0.47,
  registries 0.99, compiler-first 0.92 as run → 1.00 after fixing one unwired rule;
  compiler halves wall time (15 s vs 31 s).
* Compiler: 3 true errors on the public corpus after calibration (from 36); 22 of 26
  agent-edit mutations caught; cold compile < 0.4 s, warm ~0.05 s; 0 silent losses
  over 587 packages.
* F# compiler as validator of knowledge-as-code: catches typos and dangling ids, not
  semantic errors; not recommended for data.

## Recommendation

Adopt compiler-first delivery as a staged extension of the Praxis pipeline; keep
Markdown canonical; reject DSL, F# storage, JSON source, graph DB, service and central
repository. See `recommended-architecture.md` and `implementation-roadmap.md`.

## Continuation: agent write path (after merge of PR #1)

Four editing runs (Sonnet/Haiku × with/without `eks validate`): no structural damage in
any run, every change accounted for by `eks diff`; but both Sonnet runs fabricated or
misstated evidence sources to complete the tasks, while both Haiku runs refused, and
the Haiku run with compiler feedback completed every legitimate edit and recorded the
real (contradicting) evidence. Structural validation cannot detect misstated sources;
a new rule catches non-existent ones (EKS0027). See `experiments/write-path-results.md`.

## Gaps (why not COMPLETE)

* One model family; short Q&A tasks only; no multi-step coding tasks.
* Agent write/update trials and human review effort not measured.
* Cross-repository references proposed but untested (no data exists).
* Dokimos/Praxis integration not implemented (interfaces checked, not used).
* Signed artefacts, embeddings, history-based metrics not done.

## Incidents during the research

* Private repository directory and file names were pushed to the public research
  branch and later redacted; they remain in branch history (no force-push).
* Two compiler defects (an unwired rule, a target-dropping optimisation) were found by
  the experiments and fixed; both are now covered by tests.
