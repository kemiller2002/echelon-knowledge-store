# Praxis integration: work item → compiled knowledge package

Mission `EKS-RESEARCH-2026-002`, workstream A. Status: research note, not a contract.

Scope: the verified Praxis interfaces EKS could use to compile a knowledge package for a
Praxis work item. Sources were read at Praxis commit
`12bf9951253fc15e6973c2d3f5b88076f4556245` (`<workspace>/praxis`) plus the
public Praxis-governed repositories `fides`, `signal`, `forma`, `percepta` and `dokimos`
under `<workspace>/`. Nothing was modified. Paths below are relative to the
named repository; `praxis:` means the Praxis checkout.

Labels used throughout (AGENTS.md rule 5):

- **[verified]**: read directly from source, schema, docs or data, with the location cited.
- **[measured]**: produced by the script in §2.3, which can be rerun.
- **[hypothesis]**: a recommendation or inference that has not been tested.

---

## 1. Work items: storage, shape and schemas

### 1.1 Two stores, and one optional prose file per item [verified]

Praxis keeps a work item in two JSON documents and, optionally, one Markdown file. Join them
on `id`.

| Store | Role | Citation |
|---|---|---|
| `.ros/work/queue.json` | Backlog or staging rows: title, description, tags, priority, source. This is the canonical backlog store. | `praxis:docs/work-protocol.md:850-852`; writer `praxis:src/Praxis.Infrastructure/Work/FileBacklogQueueRepository.fs:14,211-246` |
| `.ros/context/current.json` | The live, in-flight record: lifecycle state, completion `evidence[]` and telemetry links. Once an item starts, this record is authoritative. | `praxis:docs/work-protocol.md:861-867`; keys written at `praxis:src/Praxis.Infrastructure/Work/FileWorkContextRepository.fs:122-152,186-188` |
| `.ros/work/items/<ID>.md` | Optional free-form detail. `work show` includes it when it exists. | `praxis:docs/work-protocol.md:851-852`; `praxis:src/Praxis.Infrastructure/Work/FileWorkListRepository.fs:28` |
| `.ros/work/queue.md` | A generated human-readable projection. Do not read it as input. | `praxis:docs/work-protocol.md:850-851` |
| `.ros/events/events.jsonl` | Immutable events: `work.started`, `work.completed` (which carries `evidence`), `artifact.contributed`, `work.evidence.recorded` and others. | `praxis:docs/work-protocol.md:32` |

The backlog is "not a second work-item authority" (`praxis:docs/work-protocol.md:861`).
Praxis also says an external project-management system owns work-item truth
(`praxis:docs/work-adapter-contract.md:5`).

### 1.2 No JSON Schema governs queue or context items [verified]

None of the files the brief named describes a work item:

- `praxis:schemas/work-protocol.schema.json` validates the `ros.json` configuration, not
  items. Its required keys are `repository` and `workProtocol`
  (`work-protocol.schema.json:6,13-20`). It defines `semanticMapping` (enum `backlog|ready|active|review|blocked|complete`, line 19),
  `completionEvidence` (a map from work type to the list of required evidence types, line 20),
  `continuity` (line 23) and `qualityEvidence` (lines 29-42).
- `praxis:schemas/work-adapter-request.schema.json` and `work-adapter-result.schema.json`
  describe the external adapter envelope. `operation` is one of `getWorkItem`,
  `transitionWorkItem` or `publishRepositoryEvent` (request line 10), and `workItem` is just a
  string ID (line 14). `listWorkItems` is deferred (`docs/work-adapter-contract.md:15`).
- A search of `praxis:schemas/` and `docs/` for a queue schema (`grep -rl nextSeq`) finds
  only prose docs. The item shape is defined by the F# types and writers below.

### 1.3 Queue row (`.ros/work/queue.json`) [verified]

Document: `{schemaVersion: "1.0.0", repository, nextSeq, items: [...]}`
(`FileBacklogQueueRepository.fs:262-265`).

| Field | Type / enum | Source |
|---|---|---|
| `id` | string. Must match `^[A-Z][A-Z0-9_-]*-[A-Z0-9][A-Z0-9_-]*$` | `praxis:src/Praxis.Domain/Work/Identity.fs:7-8` |
| `title` | string | `Capture.fs:20-31` |
| `description` | string or null (free text) | `FileBacklogQueueRepository.fs:224-227` |
| `tags` | string[] | `FileBacklogQueueRepository.fs:229-231` |
| `priority` | `high` \| `medium` \| `low` (default `medium`) | `praxis:src/Praxis.Domain/Work/Capture.fs:60,117` |
| `status` | `captured` \| `ready` \| `blocked` \| `abandoned` from backlog triage, plus `complete`, which native completion writes | `praxis:src/Praxis.Domain/Work/Backlog.fs:4-25`; `FileBacklogQueueRepository.fs:304-342` |
| `attachments` | `[{id, seq, name, file, size, contentType, uploadedAt}]` | `FileBacklogQueueRepository.fs:574-585` |
| `createdAt`, `updatedAt`, `createdBy` | string | `FileBacklogQueueRepository.fs:235-237` |
| `createdByActor` | `{kind, id, provider, model, runtime}` (optional) | `FileBacklogQueueRepository.fs:238` |
| `source` | free string (default `manual`) | `Capture.fs:122`; set with `--source` (`praxis:src/Praxis.Cli/Program.fs:860`) |
| `sourceReference` | string or null, free text; set with `--source-reference` | `Program.fs:861`; `FileBacklogQueueRepository.fs:241-244` |
| `blockedReason`, `abandonedReason` | string (optional) | `FileBacklogQueueRepository.fs:179-180` |
| `reidentifiedFrom`, `reidentifiedReason` | string (optional) | `FileBacklogQueueRepository.fs:521-523` |
| `risk` | `praxis.work-risk/1` (optional), set by `work update --change-class ... --risk-level ...` | `FileBacklogQueueRepository.fs:471`; `$defs.workRisk` in `praxis:schemas/praxis-completion-readiness.schema.json` (around line 310) |

Measured value distributions in the Praxis queue (333 rows) [measured, one-off `python3 -c`
over `.ros/work/queue.json`]:

- `source`: 16 distinct free-text values, for example `manual` (250), `github-issue` (43),
  `agent-discovery` (9) and `REL-001 run-01` (5).
- `sourceReference`: null 246 times. Otherwise it holds GitHub issue URLs, input-document
  paths or run identifiers.

No public repository's queue row has a `risk` member.

### 1.4 Live item (`.ros/context/current.json` → `workItems[]`) [verified]

The domain type is `LiveWorkItem` (`praxis:src/Praxis.Domain/Work/Plan.fs:3-16`).
`WorkEvidence` is `{Type; Path}`.

| Field | Type / enum | Source |
|---|---|---|
| `id` | string | `FileWorkContextRepository.fs:122` |
| `type` | free string, keyed by `completionEvidence` (observed: `task`, `feature`, `mechanical`, `research`, `bug`, …) | `Plan.fs:9` |
| `state` | local state string | `Plan.fs:10` |
| `semanticState` | `ready` \| `active` \| `blocked` \| `complete` \| `abandoned` | `praxis:src/Praxis.Domain/Work/Model.fs:4-11` |
| `evidence` | `[{type, path}]`. `type` is free text, `path` is a repository path | `Plan.fs:3-5,12` |
| `blockReason`, `completedAt`, `updatedAt`, `abandonedAt`, `abandonedReason`, `conclusion` | string (optional) | `FileWorkContextRepository.fs:135-149` |
| `telemetryExecutionIds` | string[] (`EXE-…`) | `Plan.fs:16` |
| `latestCheckpoint` | object `{id, recordedAt, executionId, branch, commit, remote, summary, nextAction, …}` | `praxis:.ros/context/current.json:4643ff` (example below) |
| `completionReadiness` | `praxis.completion-readiness/1`. Written only when a quality gate applies | `praxis:docs/work-protocol.md:229-234` |

`work context [ID]` prints this view and adds `allowedActions`,
`requiredEvidenceForCompletion`, `qualityEvidenceForCompletion` and `continuity`
(`FileWorkContextRepository.fs:247,254,269`; `Program.fs:634-680`).

Measured evidence `type` frequencies, Praxis live items (357 items, 901 entries)
[measured]: `implementation` 425, `tests` 352, `documentation` 31, `verification` 24,
`research-record` 9, `decision` 7, `evidence` 3, `requirement` 2, `requirements` 1,
`experiment` 2, and so on. Evidence types are not a closed enum.

### 1.5 Fields that reference requirements, decisions, evidence or acceptance criteria [verified]

- **Queue row:** no dedicated field. Record IDs show up only inside free-text `description`
  or `title`, and occasionally inside `sourceReference`.
- **Live item:** no dedicated field. A record is reachable only when an `evidence[].path`
  points at the record's file, for example
  `{"type":"decision","path":"research/decisions/DF-ROS-2026-A047--….md"}`.
- **Acceptance criteria:** no field anywhere. Signal keeps them as Markdown in
  `.ros/work/items/<ID>.md`: 19 of 19 detail files have an "Acceptance criteria" section,
  and signal's `WI-0002.md` also has a "Reciprocal source requirement references" table.
  That file is prose by design (`docs/work-protocol.md:852`). [measured: `grep -il` over
  signal's `.ros/work/items`]
- **Only typed requirement and decision ID lists in Praxis:** the claim-based completion
  packet. `CompletionPacket.RequirementIds`, `DecisionIds` and `ConflictResolutionIds` are at
  `praxis:src/Praxis.Domain/Work/CompletionGate.fs:21-33`. The authority's
  `RequiredRequirementIds` and `RequiredDecisionIds` are at `CompletionGate.fs:43-45,67-69`.
  See §4.2.
- **Indirect provenance join:** a registry record's `provenance.contributions` is keyed by
  `EXE-…` execution IDs, which match a live item's `telemetryExecutionIds`. Each
  `artifact.contributed` event carries `artifact.id`, `contribution` (`EXE-…`) and a free-text
  `reason`, for example `"…(work item FEAT-AGENT-PROVENANCE)"`
  (`praxis:.ros/events/events.jsonl`, first `artifact.contributed` event).

### 1.6 Two real examples (verbatim, abbreviated with `…`) [verified]

**dokimos**: `.ros/work/queue.json:39`. The item ID equals a requirement ID, and the
description points at the requirement's location:

```json
{
  "id": "DOK-OPS-001",
  "title": "Canonical comparison wire contract",
  "description": "See requirements/OPERATIONAL-REQUIREMENTS.md#DOK-OPS-001",
  "tags": ["operational"],
  "priority": "high",
  "status": "complete",
  "attachments": [],
  "createdAt": "2026-09-29T14:05:00Z",
  "updatedAt": "2026-09-29T14:40:05Z",
  "createdBy": "claude",
  "source": "manual",
  "sourceReference": null
}
```

(`dokimos:requirements/OPERATIONAL-REQUIREMENTS.md:20` defines
`**DOK-OPS-001 — Canonical comparison wire contract.**`.)

**praxis**: queue row at `.ros/work/queue.json:3059` and live item at
`.ros/context/current.json:4643`. The decision, evidence and experiment records are reachable
only through `evidence[].path`:

```json
{ "id": "WI-0064",
  "title": "Evidence-based work groups for the advisory planner (plan groups/explain-group) and the frozen grouping A/B experiment protocol",
  "description": "Advisory, read-only work-group recommendations (…) Requirements: requirements/PLANNING-WORK-GROUPS.md.",
  "tags": ["planning", "architecture", "cli"], "priority": "high", "status": "complete",
  "source": "manual", "sourceReference": null, … }
```

```json
{ "id": "WI-0064", "type": "task", "state": "complete", "semanticState": "complete",
  "evidence": [
    {"type": "implementation", "path": "src/Ros.Domain/Planning/Grouping.fs"},
    {"type": "tests", "path": "tests/Ros.Tests/GroupingTests.fs"},
    {"type": "requirements", "path": "requirements/PLANNING-WORK-GROUPS.md"},
    {"type": "decision", "path": "research/decisions/DF-ROS-2026-A047--advisory-evidence-based-work-groups.md"},
    {"type": "evidence", "path": "research/evidence/EV-ROS-2026-A059--grouping-planner-baseline-and-cohort.md"},
    {"type": "experiment", "path": "research/experiments/EX-ROS-2026-A021--grouped-versus-independent-execution.md"}],
  "telemetryExecutionIds": ["EXE-20260930T102847967Z-f42c2262"],
  "latestCheckpoint": {"commit": "370e0a4d…", "summary": "Phase one done: … baseline recorded (EV-ROS-2026-A059); protocol, cohort and predictions frozen (EX-ROS-2026-A021); DF-ROS-2026-A047 in review", … } }
```

---

## 2. How work items link to knowledge records today

### 2.1 Mechanisms that exist [verified]

1. **Prose.** Record or requirement IDs written in `title`, `description`,
   `.ros/work/items/<ID>.md`, `blockReason` or `conclusion`.
2. **Evidence paths.** `evidence[].path` names a record file. The link is typed only by
   free-text `evidence[].type`, and the record ID appears only inside the path.
3. **ID equality.** The work item ID is itself a requirement ID (dokimos `DOK-OPS-*`).
4. **Execution join.** `telemetryExecutionIds` matches a record's
   `provenance.contributions` key (or `artifact.contributed.contribution`).
5. **Reverse prose tables.** Requirement documents list work items, for example
   `praxis:requirements/CODE-QUALITY-HARDENING.md:372`: `| PRX-QUAL-023 (…) | PRAXIS-QUAL-10 | GROUP-PRAXIS-COMPLETION-001 |`.
6. **Packet ID lists.** The claim-based `RequirementIds` and `DecisionIds` (§4.2). These do
   not appear in any public repository's history: there are 0 `work.completion.verified`
   events in `praxis:.ros/events/events.jsonl` [measured: `grep -c`].

### 2.2 Results [measured]

Unit: each distinct work-item ID, taken as the union of `queue.json` items and
`current.json` `workItems`.

Classification rules:

- **structured**: a record ID appears in `tags`, `sourceReference`, `attachments[].name` or
  `evidence[].path`, or an evidence path equals a registry record's `path`.
- **prose-only**: no structured link, but an ID appears in `title`, `description`,
  `blockedReason`, `abandonedReason`, `blockReason`, `conclusion` or `.ros/work/items/<ID>.md`.
- **none**: neither of the above.

Separate columns:

- **id=req**: the item's own ID is a defined requirement ID.
- **exe-join**: one of the item's `telemetryExecutionIds` is a contribution key in a record's
  `provenance`.

```
repo       items structured prose-only  none id=req exe-join
praxis       385         47         88   250      3       66
fides         46          1         37     8      0       10
signal       162         15         65    82      0       17
forma        117          9          7   101      0        8
percepta      27          5          3    19      0        2
dokimos       59          0          1    58     27        0
TOTAL        796         77        201   518     30      103
requirement-id vocabulary size: 2740
items with a record id, by structured field: {'evidence[].path': 74, 'sourceReference': 3}
```

What the numbers show:

- **77 of 796 items (9.7%)** link to at least one record ID in a structured field.
  **201 (25.3%)** link only in prose. **518 (65.1%)** do not link at all.
- Structured links come almost entirely from `evidence[].path` (74 items). `sourceReference`
  accounts for 3, and `tags` and attachments for none.
- **30 items** have an ID that is itself a requirement ID: 27 in dokimos (`DOK-OPS-*`) and 3
  in praxis. The 3 praxis cases (`PRAXIS-GROUP-07`, `PRAXIS-PLAN-05`,
  `PRAXIS-STATE-MERGE-01`) are work-item IDs listed in a requirement document's table, so
  they are reverse links, not requirements.
- **103 items** can be joined to a registry record through an execution ID.
- No item in any public repository carries `risk` or `completionReadiness`.
- No public repository configures `workProtocol.qualityEvidence`. Every `ros.json` has only
  `completionEvidence {default:[implementation,tests], research:[research-record], mechanical:[]}`
  (`praxis:ros.json:33`; `fides|signal|forma|percepta|dokimos:ros.json:22`).

### 2.3 Script (read-only, reproducible)

Run:

`python3 -I linkage.py <workspace> praxis fides signal forma percepta dokimos`

The script reads only the files named in its docstring and writes nothing.

Definitions:

- **Registry-record ID:** Praxis `ArtifactKinds` prefixes `RQ|DF|EV|HY|EX|JR|MS|RP|TH`
  followed by `-SCOPE-YYYY-N` (`praxis:src/Praxis.Domain/Artifacts/Model.fs:68-121`).
- **Requirement ID:** a token that starts a line (heading, bold, table cell or list item) in
  `requirements/`, `docs/requirements/`, `research/requirements/` or `input-documents/` of any
  of the six repositories. `WI-`, `GH-`, `EXE-`, `GROUP-`, `SHA-` and similar prefixes are
  removed, and so are the repository's own work-item IDs.

Known limitations [hypothesis about bias]:

- The definition-site heuristic can admit a few non-requirement tokens and miss requirements
  defined inside prose sentences.
- Excluding work-item IDs undercounts dokimos cross-references, because `DOK-OPS-*` IDs are
  both work-item and requirement IDs.
- Item text from private repositories was not considered.

```python
#!/usr/bin/env python3
"""Read-only measurement: how Praxis work items reference knowledge-record ids.

Usage: python3 -I linkage.py <workspace> praxis fides signal forma percepta dokimos
Reads only: <repo>/.ros/work/queue.json, <repo>/.ros/context/current.json,
<repo>/.ros/work/items/*.md, <repo>/registries/*.json and requirement sources.
Writes nothing.
"""
import collections, glob, json, os, re, sys

BASE, REPOS = sys.argv[1], sys.argv[2:]

# Registry-record ids (Praxis ArtifactKinds prefixes, src/Praxis.Domain/Artifacts/Model.fs:68-120)
REGISTRY_ID = re.compile(r"\b(?:RQ|DF|EV|HY|EX|JR|MS|RP|TH)-[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*-\d{4}-[A-Z]?\d+\b")
# Candidate requirement-id token (e.g. FID-IDN-001, ADM-001, PRX-QUAL-023, DOK-OPS-001)
REQ_TOKEN = r"[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*-[A-Z]?\d{1,4}"
# A requirement id is "defined" where it starts a line (heading, bold, table cell, list item)
DEF_SITE = re.compile(r"^\s*(?:#{1,6}\s+|\|\s*|[-*]\s+)?(?:\*\*|`)?(" + REQ_TOKEN + r")\b", re.M)
NOISE_PREFIXES = ("WI-", "GH-", "EXE-", "INPUT-", "SHA-", "UTF-", "ISO-", "RFC-", "WCAG-", "HTTP-", "TLS-", "AES-", "ES-", "V-", "GROUP-")
REQ_SOURCES = ["requirements/**/*", "docs/requirements/**/*", "research/requirements/**/*", "input-documents/**/*"]


def load(path, default):
    try:
        with open(path, encoding="utf-8") as fh:
            return json.load(fh)
    except FileNotFoundError:
        return default


def requirement_vocabulary(repo_dir):
    vocab = set()
    for pattern in REQ_SOURCES:
        for path in glob.glob(os.path.join(repo_dir, pattern), recursive=True):
            if not os.path.isfile(path) or not path.endswith((".md", ".txt")):
                continue
            with open(path, encoding="utf-8", errors="replace") as fh:
                for token in DEF_SITE.findall(fh.read()):
                    if not token.startswith(NOISE_PREFIXES) and not REGISTRY_ID.fullmatch(token):
                        vocab.add(token)
    return vocab


def strings(value):
    if value is None:
        return []
    if isinstance(value, str):
        return [value]
    if isinstance(value, list):
        return [s for v in value for s in strings(v)]
    if isinstance(value, dict):
        return [s for v in value.values() for s in strings(v)]
    return []


# Pass 1: vocabularies across all repos (a work item may cite another repo's ids)
req_vocab, work_ids, registry = set(), {}, {}
for repo in REPOS:
    d = os.path.join(BASE, repo)
    req_vocab |= requirement_vocabulary(d)
    q = load(os.path.join(d, ".ros/work/queue.json"), {"items": []})["items"]
    c = load(os.path.join(d, ".ros/context/current.json"), {"workItems": []})["workItems"]
    work_ids[repo] = {i["id"] for i in q} | {i["id"] for i in c}
    for f in glob.glob(os.path.join(d, "registries/*.json")):
        for rec in load(f, []):
            if isinstance(rec, dict) and "id" in rec:
                registry[(repo, rec["id"])] = rec


def record_ids(text, own_id, repo):
    found = set(REGISTRY_ID.findall(text))
    for token in re.findall(r"\b" + REQ_TOKEN + r"\b", text):
        if token in req_vocab and token != own_id and token not in work_ids[repo]:
            found.add(token)
    return found


totals, field_hits = collections.Counter(), collections.Counter()
print(f"{'repo':10} {'items':>5} {'structured':>10} {'prose-only':>10} {'none':>5} {'id=req':>6} {'exe-join':>8}")
for repo in REPOS:
    d = os.path.join(BASE, repo)
    q = {i["id"]: i for i in load(os.path.join(d, ".ros/work/queue.json"), {"items": []})["items"]}
    c = {i["id"]: i for i in load(os.path.join(d, ".ros/context/current.json"), {"workItems": []})["workItems"]}
    path_to_id = {rec.get("path"): rid for (r, rid), rec in registry.items() if r == repo and rec.get("path")}
    exe_to_records = collections.defaultdict(set)
    for (r, rid), rec in registry.items():
        if r == repo:
            for exe in ((rec.get("provenance") or {}).get("contributions") or {}):
                exe_to_records[exe].add(rid)
    counts = collections.Counter()
    for wid in sorted(work_ids[repo]):
        qi, ci = q.get(wid, {}), c.get(wid, {})
        by_field = {
            "tags": strings(qi.get("tags")),
            "sourceReference": strings(qi.get("sourceReference")),
            "attachments[].name": [a.get("name", "") for a in qi.get("attachments") or []],
            "evidence[].path": [e.get("path", "") for e in ci.get("evidence") or []],
        }
        structured_text = [t for texts in by_field.values() for t in texts]
        for field, texts in by_field.items():
            if any(record_ids(t, wid, repo) for t in texts):
                field_hits[field] += 1
        detail = os.path.join(d, ".ros/work/items", wid + ".md")
        prose_text = strings(qi.get("title")) + strings(qi.get("description")) \
            + strings(qi.get("blockedReason")) + strings(qi.get("abandonedReason")) \
            + strings(ci.get("blockReason")) + strings(ci.get("abandonedReason")) + strings(ci.get("conclusion"))
        if os.path.exists(detail):
            with open(detail, encoding="utf-8", errors="replace") as fh:
                prose_text.append(fh.read())
        s = set().union(*(record_ids(t, wid, repo) for t in structured_text)) if structured_text else set()
        s |= {path_to_id[e.get("path")] for e in ci.get("evidence") or [] if e.get("path") in path_to_id}
        p = set().union(*(record_ids(t, wid, repo) for t in prose_text)) if prose_text else set()
        counts["items"] += 1
        counts["structured" if s else "prose-only" if p else "none"] += 1
        if wid in req_vocab:
            counts["id=req"] += 1
        if any(exe in exe_to_records for exe in ci.get("telemetryExecutionIds") or []):
            counts["exe-join"] += 1
    totals.update(counts)
    print(f"{repo:10} {counts['items']:5} {counts['structured']:10} {counts['prose-only']:10} {counts['none']:5} {counts['id=req']:6} {counts['exe-join']:8}")
print(f"{'TOTAL':10} {totals['items']:5} {totals['structured']:10} {totals['prose-only']:10} {totals['none']:5} {totals['id=req']:6} {totals['exe-join']:8}")
print(f"requirement-id vocabulary size: {len(req_vocab)}")
print("items with a record id, by structured field:", dict(field_hits))
```

---

## 3. Praxis CLI surface for knowledge and work [verified]

**Implementation.** The CLI is a single F# binary. `docs/cli.md:12-16` says no Node.js or
npm is involved (`DF-ROS-2026-A049`). The source contains no `.js`, `.mjs` or `.cjs` CLI
code; the only `.js` file is web UI `src/Praxis.Application/Web/url-state.js`. Comments in
`Program.fs` (for example lines 488, 574 and 624) mention a historical `tools/ros_cli.mjs`, which
the F# code mirrors byte for byte. That file is not shipped. Dispatch is in
`praxis:src/Praxis.Cli/Program.fs:2808-2920`. Unless noted otherwise, every command below is
F#.

| Command | Reads | Writes | Citation |
|---|---|---|---|
| `praxis registry build [--dry-run]` | Front matter of `*.md` under `research/decisions`, `research/evidence`, `research/experiments`, `research/hypotheses`, `research/journals`, `missions`, `research/packages`, `research/theories`, `research/requirements` | `registries/{decisions,evidence,experiments,hypotheses,journals,missions,research-packages,theories,requirements}.json`, atomically under a registry lease | `Program.fs:114-133,2831-2832`; `praxis:src/Praxis.Application/Artifacts/Operations.fs:123-160`; `praxis:src/Praxis.Infrastructure/Artifacts/FileArtifactRepository.fs:19-37,85-91`; `Model.fs:68-121` |
| `praxis registry check` | Same inputs | nothing. Exit 1 with `registry is stale; run 'praxis registry build'` | `Program.fs:103-112,2833`; `Operations.fs:103-121` |
| `praxis artifacts validate [--json]` | Artifact front matter | nothing | `Program.fs:2829-2830` |
| `praxis validate [--json]` | Artifacts, registry staleness, work attribution, telemetry, checkpoints, step references, provenance warnings, work-group warnings, identity | nothing | `Program.fs:1952-1990,2005-2016,2822`; `docs/cli.md:265` |
| `praxis work context [ID] [--text]` | `.ros/context/current.json`, `ros.json`, checkpoints | nothing. Prints the live item plus `allowedActions`, `requiredEvidenceForCompletion`, `qualityEvidenceForCompletion` and `continuity` | `Program.fs:634-680,2847`; `docs/work-protocol.md:30` |
| `praxis work [list]`, `work ready`, `work show ID` | queue + context (live state wins) + `.ros/work/items/ID.md` | nothing | `Program.fs:2848-2860`; `FileWorkListRepository.fs:28` |
| `praxis add "…"` / `work capture` (`--priority --description --tag --source --source-reference`) | queue | `queue.json`, `queue.md` | `Program.fs:850-861,2861-2862` |
| `praxis work update` (incl. `--change-class --risk-level … --live-proof`) | queue | queue row (`title`, `description`, `tags`, `priority`, `risk`) | `Program.fs:2863`; `FileBacklogQueueRepository.fs:446-474`; `docs/work-protocol.md:134-160` |
| `praxis work attach`, `work reidentify`, `work backlog-transition`, `work backlog-validate` | queue | queue / attachments dir | `Program.fs:2838-2866` |
| `praxis work begin\|start\|resume\|block\|abandon\|complete\|done` | context, queue, `ros.json` | `current.json`, `events.jsonl`, `.ros/telemetry/executions/` | `Program.fs:2867-2871`; `docs/work-protocol.md:9-34` |
| `praxis work complete --evidence TYPE=PATH …` | the evidence files, `ros.json` `completionEvidence` / `qualityEvidence` | `evidence[]` on the live item and the `work.completed` event; `completionReadiness` when gated | `docs/work-protocol.md:73-253` |
| `praxis work evidence --id … --kind … [--reference REF]* [--decision ID]` | context | `work.evidence.recorded` event | `docs/cli.md:505-520` |
| `praxis work claim\|verify\|complete-verified\|finish\|completion-identity\|claims\|validate-run…` | claim store, authority file (`PRAXIS_COMPLETION_AUTHORITY`), Ordo contract (`PRAXIS_ORDO_CONTRACT`), receipts | claim refs, `praxis.completion-packet/1` + `dokimos.completion-authority/1` handed to `dokimos completion verify`, `work.completion.verified` event, metadata commit | `docs/work-claims.md:120-171,354-445`; `docs/cli.md:271` |
| `praxis work plan`, `work context-plan`, `work decide` | context | nothing (transition planners; `context-plan` is not a knowledge context) | `Program.fs:2835-2837`; `praxis:src/Praxis.Domain/Work/ContextPlan.fs:1-60` |
| `praxis requirements preservation verify\|sections` | baseline, disposition, approvals, source docs | `praxis.requirements-preservation/1` (stdout / `--output`) | `docs/cli.md:780-808`; `praxis:src/Praxis.Cli/RequirementsCommands.fs:147-148` |
| `praxis requirements manifest compile\|check` | `praxis.requirement-catalog/1` (+ optional implementation map, previous manifest) | `praxis.verification-manifest/1`, findings | `RequirementsCommands.fs:149-150`; `docs/requirement-verification-manifest.md:94-137` (not listed in `docs/cli.md:277`) |
| `praxis streams check`, `praxis plan …` | `ros.json` `integration` / `planner` | nothing | `docs/work-protocol.md:337-458` |
| `praxis adapter call\|publish` | adapter request / events | adapter store / `.ros/publications.json` | `docs/work-adapter-contract.md:31-41`; `docs/work-protocol.md:905-915` |

---

## 4. The completion gate

Praxis has three independent layers. A repository can turn each one on separately.

### 4.1 Evidence types (always on) [verified]

`work complete` refuses to run until every evidence type in `ros.json`
`completionEvidence[workType]` (or `default`) has been supplied with
`--evidence TYPE=PATH`. Praxis also checks that the paths exist
(`praxis:src/Praxis.Domain/Work/Model.fs:70-76` `MissingEvidence`; `docs/work-protocol.md:34`). In all six public
repositories the configuration is `default: [implementation, tests]`,
`research: [research-record]` and `mechanical: []`. **No layer requires a requirement,
decision or acceptance reference.**

### 4.2 Quality readiness (`praxis.completion-readiness/1`, opt-in) [verified]

- **Schema:** `praxis:schemas/praxis-completion-readiness.schema.json`. Required members are
  `schema`, `workItem`, `decision` (`ready` or `refused`), `policy`, `facets`, `sources` and
  `blockingReasons` (lines 7-15).
- **Facets:** `implementationComplete`, `behaviorVerified`, `architectureVerified`,
  `releaseReady`, `designDebtDeclared`, `verificationMatrixSatisfied`, plus optional
  `groupVerified`. Each facet is `{required, status: satisfied|not-satisfied|unavailable|not-required, evidence[], reasons[], blocking}`.
- **Sources:** each source's `evidenceType` is one of `dokimos-ratchet`, `ordo-boundary`,
  `design-debt`, `verification-matrix` or `release-readiness`.
- **`consumedEvidence`:** a list of `[{type, path, sha256}]`.
- **When it applies:** only when `workProtocol.qualityEvidence` is set, the item's `risk`
  carries obligations, or obligation evidence is supplied.
- **Facet meanings:** `implementation-complete` and `behavior-verified` are attestations that
  the `implementation` and `tests` evidence was supplied (`docs/work-protocol.md:200-202`).
- **Defaults:** without a policy nothing is evaluated. This is the "migration bridge" at
  `docs/work-protocol.md:129-132`. No public repository enables it (§2.2).

**Dokimos feeds this layer.** `dokimos ratchet check --json` produces the `dokimos-ratchet`
evidence (`dokimos.ratchet` 1.0.0), and Praxis only consumes the verdict
(`docs/work-protocol.md:67-93,203-210`). Dokimos can also emit
`praxis.verification-matrix/1` with
`dokimos verification coverage --praxis-matrix F --work-item ID`
(`dokimos:src/Dokimos.Core/Verification/Coverage.fs:326-328`;
`dokimos:docs/verification-compiler.md:60`). That output feeds the
`verification-matrix-satisfied` facet.

The Dokimos repository ships its own copy of `schemas/praxis-completion-readiness.schema.json`
(80 lines). It differs from Praxis's 597-line schema: its description cites only
PRX-QUAL-023. The copies have drifted [verified: `cmp`].

### 4.3 Verified completion (`praxis.completion-packet/1`, claim-based) [verified]

- **Schema location:** `dokimos:schemas/praxis-completion-packet.schema.json`. The schema is
  not in `praxis:schemas/`.
- **Required members (line 8):** `SchemaVersion`, `WorkItemId`, `ClaimToken`,
  `FencingGeneration`, `Commit`, `VerificationRevision`, `OrdoContractVersion`,
  `OrdoContractDigest`, `RequirementIds`, `DecisionIds`, `ConflictResolutionIds`,
  `Validations[{Name, Status: passed|failed|unavailable, EvidenceIds}]` and
  `Evidence[{Id, Digest, Revision}]`.
- **Authority:** the matching authority is `dokimos:schemas/dokimos-completion-authority.schema.json`.
  Its required members include `RequiredRequirementIds`, `RequiredDecisionIds`,
  `RequiredConflictResolutionIds` and `RequiredValidationNames` (line 8).
- **How Praxis builds it:** from an authority file (`docs/work-claims.md:151-160`) and an
  admitted Ordo `ecir/1` contract.
- **Where the requirement IDs come from:** "requirement IDs from the authority, else the
  contract; decisions … from the authority" (`docs/work-claims.md:417-418`).
- **Ordo checks:** a required ID that is absent from the contract is refused
  (`PRX.COMPLETION.UNKNOWN_CONTRACT_REFERENCE`, `docs/work-claims.md:496-505`).
- **Final judge:** Dokimos verifies the result with `dokimos completion verify` (consumer at
  `dokimos:src/Dokimos.Core/CompletionWire.fs:121-139`), and Praxis trusts only its receipt
  (`docs/work-claims.md:354-379`).

Dokimos is therefore the verifier of this gate, not just one input to it.

The requirement and decision IDs come from Ordo or the authority, never from the queue item.
No public repository has used this path: there are 0 `work.completion.verified` events
[measured].

---

## 5. Requirement catalog and verification manifest [verified]

| Document | Produced by | Consumed by |
|---|---|---|
| `praxis.requirement-catalog/1` (`praxis:schemas/praxis-requirement-catalog.schema.json`) | People, through requirements governance (`docs/requirement-verification-manifest.md:18-23`) | `praxis requirements manifest compile` |
| `praxis.verification-manifest/1` (`praxis:schemas/praxis-verification-manifest.schema.json`) | The compiler only. The output is deterministic, has a `contentHash` and no timestamps (lines 5, 66) | `praxis requirements manifest check`; `dokimos verification requirements --manifest F` (`dokimos:src/Dokimos.Cli/VerificationCommand.fs:292,391`; `dokimos:src/Dokimos.Core/Verification/RequirementEvidence.fs:10,144`) |

**Non-UI requirements are partly supported:**

- Requirement `type` may be `data-contract`, `state-transition`, `workflow-outcome` or
  `validation-rule` (catalog schema line 48). The allowed checks for those types are
  `Mapped`, `Validation`, `Rejects`, `Save`, `Reload`, `Update`, `Isolation`, `Activate` and
  `Outcome` (`praxis:src/Praxis.Domain/Requirements/RequirementCatalog.fs:253-263`).
- Every requirement must still name a declared `screen` (schema line 44; compiler check
  `RequirementCatalog.fs:330-331`). A screen's `route` may be `null` (schema line 21), so a
  non-UI requirement has to sit on a synthetic "screen".
- The V1-V5 levels describe rendered or interactive existence and lifecycle
  (`docs/requirement-verification-manifest.md:48-54`).
- The documented verifier, Dokimos, works from Playwright results and observations
  (`VerificationCommand.fs:391`).

It is therefore [hypothesis] usable for non-UI contract and state obligations only through
that workaround. It is not a general requirement registry.

**Adoption:** no public repository contains a catalog or manifest instance. The only
`praxis.verification-manifest/1` file is a Dokimos test fixture,
`dokimos:tests/Dokimos.Core.Tests/fixtures/requirements/verification-manifest.json`
[measured: `grep -rl` for the schema string].

**Related gate:** requirements preservation (`praxis.requirements-baseline/1`,
`-disposition/1`, `-approvals/1`, `-preservation/1`). It checks that a derived requirements
package keeps every authoritative obligation (`docs/requirements-preservation.md:1-8`).

---

## 6. Recommended EKS integration points (all [hypothesis])

Principle: EKS reads the files Praxis already writes and does not add a new work-item
store. It should emit its package beside Praxis's evidence and quality documents, not in
place of them.

### 6.1 Inputs EKS should read, in order of reliability

1. **`current.json` `workItems[]`:**
   - `id`, `type`, `semanticState`;
   - `evidence[].type` and `evidence[].path`. A path is a record link when it equals a
     registry `path`, or when its filename starts with a registry ID;
   - `telemetryExecutionIds`, joined to `registries/*.json` `provenance.contributions` keys
     and to `artifact.contributed` events in `.ros/events/events.jsonl`;
   - `latestCheckpoint.summary` / `nextAction` (prose).
2. **`queue.json` `items[]`:**
   - `id`, `title`, `description`, `tags`, `source`, `sourceReference`, `status`, `risk`;
   - `description`, `title` and `sourceReference` are scanned for IDs as **prose**
     (low-confidence edges).
3. **`.ros/work/items/<ID>.md`:** prose, including acceptance criteria where present
   (signal).
4. **Requirement IDs:** when the work-item `id` is itself a requirement ID defined in
   `requirements/**` (the dokimos pattern), treat it as a structured edge.
5. **Optional:** the completion authority's `RequiredRequirementIds` / `RequiredDecisionIds`,
   or an admitted Ordo contract, when present. These are the only typed ID lists Praxis has.

### 6.2 Proposed steps

- **Seed resolution.** Add `eks context --work-item ID` (or a pre-step in EKS's `Packages`
  module) that turns a Praxis work item into a set of seed record keys. Each edge is labelled
  `structured`, `id-equality`, `execution-join` or `prose`. The step then calls the existing
  per-record task package (`eks context ID`, Compiler `Packages.task`) for each seed. EKS
  already ships per-record packages
  (`echelon-knowledge-store:tools/Echelon.KnowledgeStore.Cli/Program.fs:18-22`), so this
  adds an adapter and does not duplicate them.
- **Freshness.** Precondition on `praxis registry check`, or re-derive registries from front
  matter exactly as `registry build` does, so packages never cite stale registries.
- **Output contract.** Record digests like `consumedEvidence` (`{type, path, sha256}`). That
  would let a future Praxis evidence type (for example `--evidence knowledge-package=PATH`)
  carry the package the same way `dokimos-ratchet` is carried today. Praxis accepts free-text
  evidence types (§1.4), so attaching the package as completion evidence needs no Praxis
  change. Gating on it would need a new Praxis readiness source.
- **Typed links for future work.** Propose, do not implement, a typed `links` member on the
  queue row, for example `{requirements: [], decisions: [], evidence: [], acceptance: []}`,
  written by `work capture`/`work update`. Praxis has no such member today. Today only
  `evidence[].path` and the packet ID lists are typed.

### 6.3 Missing today (gaps to record, not to patch in other repositories)

- No JSON Schema exists for queue rows or live items. The shape lives only in F#
  (`FileBacklogQueueRepository.fs`, `Plan.fs`, `FileWorkContextRepository.fs`).
- No structured requirement, decision or acceptance fields exist on work items. 90.3% of
  items have no structured record link (§2.2).
- `evidence[].type` is free text. `decision`, `requirement`, `requirements` and
  `research-record` are conventions, not an enum.
- Requirement IDs in `requirements/*.md` are not registry records. Only `RQ-*` files under
  `research/requirements` are registered (`Model.fs:116-121`). EKS must parse requirement
  documents itself.
- The packet's `RequirementIds` / `DecisionIds` path is unused in public history. It also
  depends on an authority file and an Ordo contract, so EKS cannot rely on it as an input yet.
- `listWorkItems` is deferred in the adapter contract (`docs/work-adapter-contract.md:15`),
  so EKS must read repository files rather than call an adapter.
- The Dokimos and Praxis copies of `praxis-completion-readiness.schema.json` have diverged.
  `requirements manifest compile|check` is missing from `docs/cli.md`.
