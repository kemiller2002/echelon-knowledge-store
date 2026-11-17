# Semantic support and verification validity

The four categories are kept separate throughout this phase:

| Category | Question | Who can decide it | EKS's role |
|---|---|---|---|
| Structural validity | Does the record follow the schema? | the compiler, mechanically | decides (diagnostics) |
| Referential validity | Does the cited evidence exist? | the compiler, given the workspace | decides for local paths; reports external and unlocatable ones as unverified |
| Semantic support | Does the evidence support the claim? | a reader of the source: a person or a reviewing agent | **does not decide**; surfaces contested claims (EKS0017 when supports and contradicts coexist) and quotes record text as data |
| Verification validity | Did the relevant test pass, at the right revision? | Dokimos, from results at the candidate commit | **does not decide**; emits obligations and declared test bindings only |

## Why the compiler must not decide semantic support

Deciding "the survey supports the refund cap" needs reading and judging the source. A
compiler that did this would be an unaudited model call inside a deterministic build, so the
same inputs could give different outputs. The trial shows the judgment is usually right when
done by the agent that reads the source (20 of 20 runs judged requests 3–5 correctly), but
"usually" is not a property a compiler can promise.

What the compiler can do deterministically:

- Flag contradictions in the graph (EKS0017: a record with both supporting and contradicting
  evidence). In the trial, every correct handling of request 3 produced this review item, so
  it surfaced the disagreement to people.
- Flag agent-addressed instructions in record text (EKS0015). It fired on the injected
  EV-FXL-0003 in every run; no agent in any trial followed that instruction (0 of 44 runs that
  saw it: 24 coding runs including WI-0008 and 20 evidence writers).
- Refuse to treat the existence of a test file or a passing run as verification. Verification
  is Dokimos's decision (`../architecture/dokimos-integration.md`). EKS's obligations file lists
  declared bindings, and Dokimos reported them `not-run` until results exist.

## Verification validity in the trial

Request 4 asked for a verification record from a run whose relevant test failed; request 5
offered a passing run at a revision that predates the requirement. Both were refused in
20 of 20 runs. Under Dokimos the same cases would be mechanical: a failed bound test or a
result at a different candidate SHA leaves the obligation unverified. This is the strongest
argument for leaving verification to Dokimos: it does not depend on the agent noticing.
