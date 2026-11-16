# Epistemic model

## Principles

1. **Claims and evidence are different records.** A hypothesis/decision never
   contains its own evidence; evidence records point at claims (`supports`,
   `contradicts`).
2. **Standing is derived, not declared:** `Unassessed | Supported | Contested |
   Refuted`, computed from evidence links (`Strict.Standing`). The declared
   `status` is the owner's judgement; disagreement between the two is a validation
   finding (5 cases in the corpus).
3. **Confidence is qualitative** (`very-low … very-high | unstated`). Nothing in the
   corpus calibrates numbers; numeric confidence is rejected until something does.
4. **Source reliability ≠ conclusion confidence.** Evidence carries
   `evidence_type` and source fields; claims carry confidence. They are separate
   fields and are not combined automatically.
5. **Time.** Evidence carries `retrieved`/`source_date`; `stale-evidence` flags
   evidence older than a configurable horizon (180 days in the prototype).
6. **Supersession is history, not deletion.** Superseded records stay; supports
   from superseded records are flagged (3 cases).
7. **Validity is structural.** A well-formed record is not a true record. Authority
   comes from review and location (security-model.md), never from syntax.

## Adversarial cases (specified, not yet executed with agents)

| Case | Expected behaviour | Mechanism |
|---|---|---|
| Contradictory sources | both shown, claim marked contested | derived standing |
| Outdated evidence | flagged stale | `retrieved` + horizon |
| Unsupported conclusion | cannot be `supported` | strict `Standing` |
| Misleading authority (record claims to be canonical) | ignored; authority from location/review | trust labels in packages |
| Changing requirements | prior versions reachable via supersession | lifecycle |
| Unverified AI-generated claim | marked by provenance (`author_agent`), status not above `proposed` without human review | policy rule |
| Conflicting organisational assumptions | both recorded as hypotheses with owners | taxonomy |
