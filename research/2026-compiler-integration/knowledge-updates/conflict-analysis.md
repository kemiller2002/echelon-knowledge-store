# Conflict analysis

Conflicts seen in this phase and how each was handled.

| Conflict | Example | Detection | Who resolves | Observed |
|---|---|---|---|---|
| Stale prose vs accepted record | `fx-ledger/README.md` says CSV uses `;`, admins see all tenants, over-refunds are allowed; accepted decisions say the opposite | none mechanical: README is not a record | the agent, by authority order | No agent in any condition followed the README in tasks T5, T6 or T8 (every T5/T6/T8 failure traced to missed amendments, none to the README) |
| Superseded vs current decision | DF-FXL-0002 → DF-FXL-0005; DF-FXL-0003 → DF-FXL-0004 | `supersedes`/`superseded_by`; packages show superseded records under "History" | the compiler (authority policy) + agent | followed in every run |
| Proposed or rejected amendment vs accepted requirement | RQ-FXL-0105 (proposed), RQ-FXL-0108 (rejected), RQ-FXL-0103 (superseded) | status; work-item packages list them, and a non-accepted record in the dependency closure makes the package INCOMPLETE | agent | no run implemented a decoy |
| Accepted amendment vs base requirement | RQ-FXL-0101/0104/0107/0109 amend base requirements | incoming `depends_on` edges, included in packages | agent | the decisive factor in the hard trials (A 24/32 solved, C 32/32) |
| Evidence for and against one decision | EV-FXL-0012 (contradicts) next to the existing support for DF-FXL-0006 | EKS0017 (requires review) | owner | produced in every evidence-trial run that handled request 3 correctly |
| Injected instruction in a record | EV-FXL-0003 | EKS0015 (requires review); packages quote record text as data | harness / owner | 0 of 44 exposed runs followed it (24 coding runs that included WI-0008, 20 evidence writers) |
| Concurrent edits to one record | — | not exercised | Git merge + change manifest | not measured |

The README conflicts are a gap: EKS validates records, not free prose. A useful next step is a rule
that flags README or `docs/` statements contradicting accepted decisions. That needs semantic
judgment, so it belongs with a reviewer, not the compiler.
