# Integration roadmap (staged)

No stage changes Praxis or Dokimos without their owners' review and passing validation gates. No
governance rule is changed to make an experiment pass.

## Stage 0: consolidate EKS (this repository; ready now)

- Merge PR #4: work-item packages, manifests, derived completeness, change manifest, fixed source
  rule, parse-cache key, privacy checks, benchmarks, fixtures.
- Add a planted-violation regression test for every mandatory rule (only some rules have one).
  This addresses "rules that exist but do not apply".
- Owner decision on history rewrite for the privacy incident (`../security/privacy-review.md`).

Exit: CI green; every mandatory rule has a planted-violation test.

## Stage 1: advisory packages on real work (no Praxis change)

- Agents working in Praxis-managed repositories run `eks work-item <repo> <WI>` before starting and
  read the package; INCOMPLETE packages are surfaced to the person who assigned the work.
- Attach `eks work-item-manifest` and, for knowledge edits, `eks change-manifest` output to completion
  evidence as a free-text evidence type, which Praxis already accepts (`../architecture/praxis-integration.md` §6).
- Measure on real work items: share with structured links, share INCOMPLETE and why, and the
  defect rate of completed items (reopened items, failed verification) against comparable items
  without packages.

Exit: real-repository evidence that packages reduce missed requirements, or a documented reason they
do not. This is the main open question of Phase 2.

## Stage 2: typed links in Praxis (Praxis change, owner review)

- Propose a typed `requirementIds` / `decisionIds` field on work items. Today only the
  completion packet has typed ids (`praxis-integration.md` §4.3). EKS would read it as `structured`
  links, ahead of prose.
- Read the live item (`.ros/context/current.json`: `evidence[].path`, `telemetryExecutionIds`) as
  additional link sources.

Exit: links on new work items mostly structured; package INCOMPLETE rate caused by "no links" falls.

## Stage 3: verification obligations (Dokimos input, owner review)

- Add `verified_by` (test identities) to requirement records where owners agree, ideally per
  acceptance criterion rather than per requirement.
- Emit `dokimos.verification-obligations/1` in CI; run `dokimos verification coverage`.
- Ask Dokimos to publish a JSON Schema for the obligations contract and to check `planDigest`
  (`dokimos-integration.md` Verdict).

Exit: obligations accepted without refusals; binding reviews in place; Dokimos verdicts, not EKS,
feed Praxis readiness.

## Stage 4: gating (only with Stage 1 evidence)

- Optionally make Praxis completion readiness require a COMPLETE package manifest for items that cite
  requirements. Do this only if Stage 1 shows fewer missed requirements on real work, and keep an
  explicit, recorded override path.

## Continuing research (parallel)

- Regenerate the fixture privately (held-out policy) and rerun with more repetitions and a third model.
- Add long-running tasks (category J) and real-repository tasks.
- Evidence integrity with less legible traps; measure the title-only rule's false-positive rate.
