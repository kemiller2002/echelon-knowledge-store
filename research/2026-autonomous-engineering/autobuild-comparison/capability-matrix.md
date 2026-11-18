# Capability matrix: Obvious AutoBuild vs Echelon Foundry

Mission EKS-RESEARCH-2026-003 · 2026-11-18. Machine-readable sources:
`data/autobuild-capabilities.json` (AutoBuild), `data/baseline-group-a.json` and
`data/baseline-group-b.json` (Echelon).

**AutoBuild status vocabulary:**
- **D**: documented by the vendor.
- **Dm**: demonstrated publicly, in the vendor's own public PRs.
- **I**: inferred.
- **U**: unknown.

No AutoBuild capability is independently verified.

**Echelon status vocabulary:**
- **Impl+used**: implemented and in use.
- **Impl**: implemented but not enabled or not used.
- **Partial**: partly implemented.
- **Design**: designed or documented only.
- **None**: absent.

| # | Capability | AutoBuild | Echelon today (evidence) | Gap and verdict |
|---|---|---|---|---|
| 1 | Requirements / spec generation | D: agent-drafted "Blueprint", three-phase spec, approval before build | Partial: Praxis `RQ-` records and requirement preservation gate; EKS compiles typed requirement packages (32/32 vs 24/32 on a hard fixture, EV-AE-017). No agent-drafted, approved spec flow. | Adopt an approved-spec step **only** if it reuses EKS packages; do not add a second requirements store |
| 2 | Agent orchestration | Dm: orchestrator, initiative > feature > executable, dependency ordering | Partial: Praxis work groups, local worker delegation, `execution start --worktree`; Ordo decision events. No hosted orchestrator. | Improve later; orchestration is not the bottleneck the evidence shows |
| 3 | Parallel execution and isolation | D: per-repo cloud sandboxes (technology U) | Impl: claim fencing generations and leases (`Claim.fs`), worktrees, local containment. No sandboxing. | Keep claims; sandboxing belongs to the hosting platform, not Echelon code |
| 4 | Independent review | D: rubric reviewer 0–100, path routing; independence from author U | Impl: Praxis deterministic independent review (`Autonomy/Review.fs`); Dokimos completion verification. Independence real only in attested mode. | Echelon is stronger in design; enable it (see 5) |
| 5 | Verification and evidence | Dm: screenshots, tests and results attached to PRs; auto-fix and retest | **Impl, not used:** claim-bound validation results, signed receipts, protected policy, pinned keys, enabled in 0/13 repos (EV-AE-005). Two acceptance defects fixed in this program (EV-AE-001, EV-AE-003). | **Echelon's main differentiator, currently undeployed.** Highest-priority adoption |
| 6 | Repository governance | Dm: `.obvious/config.yml` policy contract; human-merge and human-PR gates; repo config cannot weaken platform rules | Impl+used: `ros.json`, protected classes, policy-weakening detection (REL-GATE-004), owner-signed run authorization | Parity or better in design. Adoption gaps: stale vendored governance copies (QRI) |
| 7 | Skills and knowledge | Dm: `SKILL.md`, `SKILL_CORE.md`, default-branch-only loading | Impl+used: `AGENTS.md`/`CLAUDE.md` contracts; Partial: EKS compiler (research) | Copy AutoBuild's "load skills only from the default branch" rule; it blocks unmerged-branch instruction injection |
| 8 | Self-improvement | D (marketing only): nightly memory consolidation without approval; rule proposals need approval; un-apply | Design → Partial: governed proposal contract (praxis#302) with counterfactual, held-out and regression gates and a human-approval guard. No persistence or runner yet. | Echelon's design is stricter (AutoBuild consolidates memory without approval, and evaluates rules not at all, as far as documented). Build the runner before claiming learning |
| 9 | Cost and human-attention accounting | D: spend per task, project and workspace; budgets; Cost/PR, Autonomy % | Partial: Praxis `UsageCost` separates provider-reported from estimated cost; step telemetry. **No human-attention measure.** | Adopt human-attention minutes per task; Echelon cannot answer its central question without it |
| 10 | Human approval workflows | Dm: Blueprint approval, inbox, merge gates, per-use secret approval | Impl: `RunAuthorization`, `RequiresOwner` decisions, merge requirements | Parity; Echelon lacks the inbox (UI) |
| 11 | Model independence | D: "Fusion" per-step routing; pinning; listed Claude, GPT and GLM models | Partial: provider-agnostic identity (Codex, Claude, Gemini, Copilot); no routing | Low priority; routing evidence (round 2) shows model choice dominates protocol choice |
| 12 | Product interface and DX | Dm: Slack/ticket intake, build drawer, cockpit, inbox, mobile | None/Partial: CLI first; `praxis web serve` / `hub serve`; Forma patterns exist (work-queue, lane-board, provenance-trail) | Largest visible gap; see the control-plane plan in `implementation-roadmap.md` |
| 13 | Security boundaries | D: SOC 2 / ISO claims; per-repo sandboxes; protected secrets | Partial: Tutela posture gate; Fides auth with self-reported high-severity gaps (EV-AE-021); "Aegis" is operational recovery, not security | Do not compete here; rely on the hosting platform's isolation |
| 14 | Deployment and maintenance | I: preview environments; scheduled and triggered maintenance PRs | None for agents | Defer |

## What AutoBuild does that Echelon should not adopt

- **Memory consolidation without approval** (capability 8). It conflicts with
  provenance and with "never silently modify governance".
- **"96% merge rate" as a quality metric.** Merge rate measures reviewer acceptance, not
  correctness. Storybook's review bots found blocking bugs in some merged-path AutoBuild
  PRs (`data/autobuild-research-notes.md` §5). Storybook's "87 of 143 merged" counts open
  PRs as unmerged, so it does not contradict the 96% decided-merge figure.

## What AutoBuild has that Echelon underestimated

- **Throughput evidence.** Over 5,700 public PRs, with the monthly rate growing from 258
  (August) to 2,633 (October).
- **Productised attention routing:** an inbox and a build drawer.
- **Default-branch-only skill loading.**
- **Spend controls.**

None of these needs Echelon's verification depth, and all of them are what users see.
