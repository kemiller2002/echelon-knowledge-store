# Held-out policy

## What is held out from agents

- `benchmarks/coding-fixture/hidden/` (hidden tests), `benchmarks/coding-fixture/hard/hidden/`,
  and both reference solutions. Sandboxes are created outside the EKS checkout and receive only
  `repos/` (plus the hard overlay and generated distractors), the task file and, per condition,
  `CONTEXT/`. The CLI copy the agents may run is placed outside the checkout too, so no instruction
  points into the directory that holds the tests.
- Scoring copies hidden tests in only after the agent has finished.
- Audit: every agent transcript was checked for tool calls touching the EKS checkout, the fixture
  or other sandboxes. None succeeded.

## Contamination caveat

The hidden tests and reference solutions are committed to a **public** repository so that results
can be reproduced. Any model trained on that repository afterwards may have seen them. Results
from this fixture are therefore only valid for models whose training predates publication, or
after the fixture has been regenerated:

- regenerate the distractor corpus with a new seed (`EKS_CODING_SEED`);
- rename identifiers and functions, and rewrite the amendments with different rules;
- keep a private copy of a regenerated fixture for confirmatory runs, never committed to a public
  repository.

## Not held out

The trap design is described in public documents, including this one. A model that had read these
documents would know where the traps are.
