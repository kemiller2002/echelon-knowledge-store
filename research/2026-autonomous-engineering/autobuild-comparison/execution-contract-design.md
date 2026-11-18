# Typed execution contract (Phase 3)

Mission EKS-RESEARCH-2026-003 · 2026-11-18 · Status: **design**. One increment is
implemented (praxis#302); the rest is proposed.

## Finding: the contract already exists. Do not add a framework.

The baseline (`data/baseline-group-a.json`, EV-AE-018) shows that Praxis, Ordo and Dokimos
already model most of the execution lifecycle as F# discriminated unions and immutable
records:

| Concern | Existing type (owner) | Path |
|---|---|---|
| Work-item state | `LiveWorkState`, `WorkAction`, `WorkTransition.decide` (Praxis) | `src/Praxis.Domain/Work/Model.fs` |
| Exclusive execution / agent responsibility | `WorkClaim`, `ClaimStatus`, `ClaimFence` with fencing generation and lease (Praxis) | `src/Praxis.Domain/Work/Claim.fs` |
| Execution policy, versioned | `ExecutionPolicy` (has `PolicyVersion`), `ProtectedClass`, `RunAuthorization` (owner-signed, `PolicyDigest`) (Praxis) | `src/Praxis.Domain/Autonomy/Model.fs` |
| Evidence and verification outcome | `ValidationResult` (`CheckStatus`), `EvidenceObservation`, `CompletionPacket`, `CompletionVerdict` (Praxis) | `src/Praxis.Domain/Work/ValidationResult.fs`, `CompletionGate.fs` |
| Test outcomes | `Outcome = Passed / Failed / Skipped / NotExecuted`, now with `worst` (Dokimos) | `src/Dokimos.Core/Verification/TestResults.fs` |
| Requirement-level verification | `VerificationReport`, obligation statuses (Dokimos) | `src/Dokimos.Core/RequirementEvidence.fs`, `Verification/Coverage.fs` |
| Release authorization | `ReleaseReadinessEvidence`, `MergeReceipt` (Praxis) | `CompletionReadiness.fs`, `Autonomy/` |
| Decision provenance | `Actor`, `ArtifactProvenance` (Praxis); `DecisionEvents` JSONL, `EvidenceKind` observed / derived / model-inferred (Ordo) | `Provenance/`, Ordo `Evidence.fs` |
| Cost | `CostBasis`, `PriceTableRef` (provider-reported vs estimated) (Praxis) | `Telemetry/UsageCost.fs` |
| Knowledge dependencies | Typed records and relations, work-item packages, change manifests (EKS, research) | `echelon-knowledge-store/src/Echelon.KnowledgeStore.*` |

A new "Echelon execution contract" library would duplicate all of this. The brief's own
rule applies: reuse Praxis, Ordo and Dokimos.

## Where invalid states are still representable (the real gaps)

Each gap below was found by an actual defect or experiment in this program, not by
inspection alone.

1. **Plain-completion evidence is stringly typed.**
   - `--evidence TYPE=PATH` becomes `WorkEvidence = { Type: string; Path: string }`.
   - An outside path, a directory or a deleted file all type-check (V2 and V5 in
     `verification-audit.md`).
   - **Proposal:** a `RepositoryPath` smart constructor (normalised, repo-relative, no
     `..`, not absolute), and a `CitedEvidence = { Kind; Path: RepositoryPath; Sha256 }`
     captured at completion.
   - praxis#303 enforces the path rule at the I/O boundary; the type would make it
     unrepresentable.
2. **"Verified" has two meanings.**
   - `CompletionFacet.BehaviorVerified` is satisfied by presence, while
     `CompletionVerdict.Verified` requires a passing, claim-bound result.
   - **Proposal:** split the facet type into `TestsCited` (an attestation) and
     `BehaviorVerified of ValidationResult` (a proof), so presence can never be
     displayed as verification.
3. **Duplicate outcome merging.**
   - Dokimos had two merge rules for repeated test identities (V1).
   - **Done:** a single `TestResults.worst`. The behaviour-verification path keeps its
     stricter "differing outcomes = failure" rule. That is deliberate and documented in
     dokimos#71.
4. **Duplicated wire types.**
   - Receipt verification and manifest-obligation types exist in both Praxis and Dokimos.
   - Praxis re-implements Ordo semantics (`PRX-ARCH-001`, Partial) and pins Ordo.Core
     1.8.2, while Ordo is at 1.20.0.
   - **Proposal:** Dokimos consumes `Praxis.Contracts` for receipts instead of a copy.
     Upgrading the Ordo pin is a separate, owner-approved migration.
5. **Decision provenance for pre-implementation analysis is untyped.**
   - Clarity and EDF outputs are free text.
   - **Proposal (only if analysis is retained; see the experiment results):** reuse
     Ordo's `EvidenceKind` and record a fixed `FINAL` block (posture, action,
     probability) as a typed Ordo decision event. Do not add a Clarity type system.
6. **Governed learning had no type at all.**
   - **Done:** `Praxis.Domain.Learning.ImprovementProposal` (praxis#302):
     `Proposed → Evaluated → Approved → Applied → RolledBack`, with guard findings.

## Consistent, independently enforceable: what that requires

A contract is "independently enforceable" only if the party that evaluates it is not the
party that produced the evidence. In Praxis that is already defined: attested mode,
protected policy and pinned verifier keys (`verification-audit.md`).

The single most valuable contract change is therefore **not a type**. It is enabling
`requireVerifiedCompletion` with `trustedVerifierKeyIds` in one pilot repository through
a reviewed change.

## Not done

- No new types beyond `ImprovementProposal` were implemented. Items 1, 2, 4 and 5 are
  proposals.
- No cross-repository compatibility analysis was run for item 4.
