module Echelon.KnowledgeStore.Tests.Fixtures

open Echelon.KnowledgeStore

/// Parses a Markdown record or fails the test.
let record (repo: string) (path: string) (text: string) =
    match FrontMatter.split text |> Result.bind (KnowledgeRecord.ofDocument repo path) with
    | Ok r -> r
    | Error e -> failwith e

let hypothesis =
    record
        "praxis"
        "research/hypotheses/HY-T-2026-0001--x.md"
        """---
id: HY-T-2026-0001
title: Typed links help
status: proposed
confidence: very-low
supporting_evidence: [EV-T-2026-0001]
contradicting_evidence: [EV-T-2026-0002]
supersedes: []
superseded_by: []
---

# Hypothesis

Typed links help agents. See EV-T-2026-0001.
"""

let evidenceFor =
    record
        "praxis"
        "research/evidence/EV-T-2026-0001--a.md"
        """---
id: EV-T-2026-0001
title: Observation A
status: accepted
source_title: Trial A
supports: [HY-T-2026-0001]
---

Body A.
"""

let evidenceAgainst =
    record
        "praxis"
        "research/evidence/EV-T-2026-0002--b.md"
        """---
id: EV-T-2026-0002
title: Observation B
status: accepted
derived_from: [EV-T-2026-0001]
---

Body B with a --- line

---

after a thematic break.
"""

let decisionOld =
    record
        "praxis"
        "research/decisions/DF-T-2026-0001--old.md"
        """---
id: DF-T-2026-0001
title: Old decision
status: superseded
superseded_by: [DF-T-2026-0002]
---

## Rationale

Because.
"""

let decisionNew =
    record
        "praxis"
        "research/decisions/DF-T-2026-0002--new.md"
        """---
id: DF-T-2026-0002
title: New decision
status: accepted
supersedes: [DF-T-2026-0001]
depends_on: [RQ-T-2026-0001]
---

## Rationale

Better.
"""

let requirement =
    record
        "praxis"
        "research/requirements/RQ-T-2026-0001--req.md"
        """---
id: RQ-T-2026-0001
title: A requirement
status: accepted
---

## Acceptance

- It works.
- It is tested.
"""

let all =
    [
        hypothesis
        evidenceFor
        evidenceAgainst
        decisionOld
        decisionNew
        requirement
    ]

let graph = Graph.build false all
