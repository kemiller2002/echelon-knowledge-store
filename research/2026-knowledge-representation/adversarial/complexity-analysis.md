---
title: Complexity and maintenance analysis of the EKS prototype, with the simplest alternative
mission: EKS-RESEARCH-2026-001
role: Agent N (independent adversarial reviewer)
date: 2026-11-16
status: review
---

# Complexity analysis

The question here is whether the proposed architecture is more complex than the problem needs.

I measured the size of the prototype by component, estimated what it would cost to maintain, and built and measured the simplest alternative I could find.

The objection numbers in brackets, for example [O8], refer to `architecture-objections.md`.

## 1. How I measured

- **Snapshot.** I counted the working tree at 2026-11-16, while other agents were still editing `AblationBench.fs` and `Program.fs`, so the benchmark figures may move slightly.
- **Columns.** Each file was counted with `wc -l` and `grep`:
  - *raw*: all lines;
  - *code*: lines that are neither blank nor `//` comments;
  - *lets*: `let` bindings, a rough count of functions and values;
  - *branch*: lines that start with `|`, `match`, `if`, `elif` or `when`, a rough proxy for decision points;
  - *test refs*: occurrences of `Module.` in `tests/`.

## 2. The prototype, component by component

### Core library (`src/Echelon.KnowledgeStore.Core`)

The "Needed by the recommendation?" column judges each component against the emerging recommendation: Markdown as source, an F# validator, an index, compiled packages, and no DSL.

| Component | File | Raw | Code | Lets | Branch | Test refs | Needed by the recommendation? |
|---|---|---|---|---|---|---|---|
| YAML reader | Yaml.fs | 111 | 86 | 11 | 51 | 3 | Yes. It is stricter than the corpus's own parser and drops 2 real records [O5, O6]. |
| Front-matter split | FrontMatter.fs | 48 | 37 | 6 | 3 | 4 | Yes |
| Identifiers | Identifiers.fs | 55 | 35 | 9 | 14 | 0 (own test file only) | No. It is dead code; nothing in src or benchmarks uses it [O22]. |
| Record model | Model.fs | 307 | 255 | 21 | 77 | 1 | Yes. It has the lossy confidence parse and the narrow relation vocabulary [O7, O18]. |
| Graph | Graph.fs | 212 | 160 | 37 | 26 | 9 | Yes. This is the real value: inverse links and traversal. |
| Validation | Validation.fs | 225 | 195 | 23 | 15 | 4 | Yes. It misses 13 of 25 agent mistakes [O8]. |
| Strict per-kind types | Strict.fs | 193 | 153 | 21 | 62 | 4 | Optional, for research |
| Multi-format codec | Codec.fs | 362 | 298 | 42 | 63 | 5 | No. The alternative formats were rejected. |
| DSL | Dsl.fs | 273 | 228 | 28 | 49 | 3 | No. Rejected. |
| F# source format | FSharpSource.fs | 72 | 57 | 12 | 5 | 1 | No. Rejected. |
| BM25 retrieval | Retrieval.fs | 148 | 121 | 20 | 8 | 3 | Doubtful. Grep reached 59/59 on lookup-by-description; the BM25 hybrid reached 43/59 (`retrieval.md`). |
| Context compiler | Context.fs | 212 | 175 | 27 | 46 | 3 | Only if compiled packages are kept [O12, O13] |
| **Core total** | 12 files | **2,218** | **1,800** | 257 | 419 | | |

The core breaks down as follows:

| Grouping | Code lines | Share of core |
|---|---|---|
| Rejected-format code (Codec, Dsl, FSharpSource) | 583 | 32% |
| Dead code (Identifiers) | 35 | 2% |
| Optional research code (Strict, Retrieval) | 274 | 15% |
| **The minimum the recommendation needs:** Yaml, FrontMatter, Model, Graph, Validation, Context | **908** | 50% |

### Benchmarks (`benchmarks/Echelon.KnowledgeStore.Bench`)

| File | Raw | Code |
|---|---|---|
| Program.fs | 514 | 443 |
| RetrievalBench.fs | 366 | 307 |
| Trials.fs | 366 | 305 |
| AblationBench.fs | 307 | 257 |
| Baseline.fs | 252 | 227 |
| Formats.fs | 240 | 204 |
| DependencyBench.fs | 172 | 142 |
| Corpus.fs | 147 | 121 |
| Tokens.fs | 38 | 25 |
| Report.fs | 35 | 24 |
| **Total** | **2,437** | **2,055** |

The benchmark harness is larger than the core. That is appropriate for research, but none of it is product code.

### Tests (`tests/Echelon.KnowledgeStore.Tests`)

- 389 raw lines, 313 code lines, 28 `[<Fact>]`/`[<Property>]` cases.
- The ratio of test code to core code is **0.17**.
- No test covers:
  - the agent-mistake mutations in O8;
  - a parser that disagrees with the corpus tooling;
  - lossless round-trip of the original text.

### Supporting infrastructure

- 3 CI workflows (137 lines).
- A `global.json` that pins .NET SDK 10.0.400.
- One external package, YamlDotNet 18.1.0.
- A llama tokenizer model checked into `benchmarks/out/tok/`.

### Proposed but not yet built

- **Security model.** `architecture/security-model.md` lists 23 threats and 25 controls, several marked REQUIRED:
  - classification propagation;
  - a no-write-down sink rule;
  - caller-scoped queries;
  - separate compile credentials;
  - a schema floor.
- **Versioning research.** `migration/versioning-research.md` proposes immutable schema versions with a registry, compatibility modes and migration windows.
- **Index across repositories and compiled packages.** These need a build location and credentials outside each repository's CI [O16].

## 3. Estimated maintenance burden

These estimates come from concrete obligations visible in the code and corpus. They are not productivity figures from the literature. Where I give hours, they are labelled assumptions.

| Obligation | Why it recurs | Evidence | Assumed cost |
|---|---|---|---|
| **Keeping two validators in step.** Praxis `ArtifactPolicy` (F#, 166 lines) and the JSON schemas in 10 repositories already define the status and confidence vocabularies. EKS defines its own. | Each change to the Praxis vocabulary must be mirrored, or the two disagree. They already disagree: EKS does not accept `medium-high`, does not know `tests_hypotheses`, and has no status enums. | O1, O7, O8, O18 | About 1–2 h per vocabulary change, plus defects whenever the mirroring is missed |
| **Agreement between parsers** | EKS (YamlDotNet, strict) rejects files that Praxis accepts. Every new agent-written edge case, such as an unquoted colon or a block-mapping error, has to be triaged twice. | 2 real records dropped [O5, O6] | Small but permanent |
| **A rule for every new convention** | Each new relation field or record kind needs model, graph, validation and context recipe changes. Model.fs alone has 77 branch lines. | Model.fs, Context.fs | About 0.5–1 day per new kind (assumption) |
| **SDK and dependency upgrades** | .NET is pinned in `global.json`, YamlDotNet must be bumped, and `TreatWarningsAsErrors` is on. | `Directory.Build.props` | About 1–2 h per quarter (assumption) |
| **Rebuilding the index across repositories** | The index needs credentials for several repositories, private/public separation (C-12, C-13) and a freshness SLO (T-20, C-22). Today none of this exists, and the per-repository registries need none of it. | security-model.md | The largest new operational surface. It needs an owner, and there is one owner (§4.5). |
| **Maintaining the compiled-package recipes** | Each task type has its own recipe. A router is needed in practice, and it is not built or measured. | Trials.fs:172 [O13] | Unknown. It may need an LLM router. |
| **Dead and rejected code** | 618 lines (583 for rejected formats and 35 dead) still compile under `TreatWarningsAsErrors` and must keep compiling through every upgrade. | §2 | Remove it now to avoid the cost |

**Qualitative conclusion.** The architecture as proposed adds:

- a second validator;
- a second parser;
- an index across repositories with its own security perimeter;
- a compiler with per-task recipes.

All of this is for one owner and about 570 typed records that grow by roughly 150–250 a month (by `created:` date: July 90, August 6, September 257, October 155, November so far 22).

## 4. The simplest alternative, built and measured

### 4.1 What I built

**Alternative A: extend the existing pipeline; build no new store.**

I wrote a 108-line Python linter, `simple_lint.py`, reproduced in Appendix A. It uses PyYAML with a duplicate-key loader and falls back to a lenient line parser, like the permissive Praxis reader. It does five things:

1. checks closed status and confidence vocabularies, using Praxis's own sets plus `medium-high`;
2. detects misspelled relation fields that are close to known relation names (`difflib`);
3. detects malformed (lower-cased) ids, dangling references, self-supersession, duplicate ids and superseded records with no replacement;
4. checks evidence symmetry and supersession symmetry, and flags `supported` status with no support or with net contradiction;
5. emits `INDEX.md` with derived inverse links (`supported-by`, `contradicted-by`).

A 34-line traversal script, `fe.py`, adds supersession-chain following and 6-hop change impact.

### 4.2 Results on the same corpus and gold as EKS

| Measure | EKS prototype | Alternative A | Source |
|---|---|---|---|
| Implementation size | 1,800 core code lines (908 minimum) + YamlDotNet + .NET SDK | **108 + 34 lines** of Python + PyYAML | wc |
| find-evidence, exact match against the EKS gold | 56/56 (by definition) | **56/56** | `python3 fe.py` |
| change-impact, exact | 66/66 (by definition) | **66/66** | same |
| current-authority, exact | 12/12 (by definition) | **12/12** | same |
| Evidence-link asymmetries found | 28 | **28** | `python3 simple_lint.py` |
| Real dangling references found | 1 (+8 false positives) | **1** (+4 YAML errors reported against their own files) | same |
| Agent mistakes blocked, of 25 | 7 + 1 indirect | **17** | O8 table |
| Agent mistakes undetected, of 25 | 13 | **4** | O8 table |
| Time to build the index | 63 ms compile (`ablation.md`) | 0.62 s read + 0.04 s lint and index | measured |
| Records silently dropped by the parser | 2 real research records | 0 (reported and recovered leniently) | O5 |

### 4.3 Recommended form of Alternative A

Do not adopt the Python script itself. Port the same ideas into what already exists:

1. **`praxis registry build`.** Add `supported_by`, `contradicted_by` and `impacted_by` fields, computed by inverse and transitive traversal, to the existing `registries/*.json`. Estimate: about 40–80 lines of F# in `Praxis.Domain/Artifacts`.
2. **`ArtifactPolicy.validate`.** Add these rules, about 40–60 lines of F#:
   - evidence symmetry, or better, declare one direction canonical and derive the other;
   - detection of misspelled relation fields;
   - a check on id case;
   - "supported with net contradiction";
   - "review-due": evidence newer than the claim's `updated` date.
3. **AGENTS.md "Start here".** Point agents at `registries/*.json`. This is baseline finding C5, and it is an organisational fix, not code.
4. **Links between repositories.** Allow `repo:ID` in the existing fields. Validate them against the other repository's *published* registry (a release asset or a raw URL), so no private compile credential is needed. Derive hard dependencies from manifests and lock files instead of authoring them [O10].
5. **The two repositories Praxis does not govern** (echelon-organization-administration and clarity-framework): add a link checker such as lychee or markdown-link-check to their CI. That covers 160 of the 205 broken links.
6. **Duplication.** Stop copy-installing framework material, or exclude `.sde/`, `framework/`, `templates/` and `.visual-engineering/` from agent retrieval. That covers 578 extra copies [O9].

**Rough cost.** About 150–250 lines of F# inside an existing, tested, CI-gated codebase, plus convention changes. There is no new repository, store, security perimeter or schema registry.

### 4.4 What Alternative A loses, and how much

| Lost capability | Measured value of that capability | Assessment |
|---|---|---|
| Compiled task-specific context packages | Trial accuracy: compiled 16/16 vs md 16/16, so no difference. Tokens: Sonnet −2.7%, Haiku **+9.1%**. Wall time on Sonnet 22 s vs 44 s, tool calls 7 vs 8.5 (one run each). | **Small and unproven.** The only signal is lower latency in a single run pair. It could be recovered later as a `praxis context <id> --task impact` command printing registry cards, about 50 lines. |
| BM25 / hybrid retrieval | Grep scored recall 1.0 (59/59) on lookup-by-description; the hybrid reached 43/59 (`retrieval.md`). | Nothing is lost. Grep is better here. |
| Strict per-kind F# types (Strict.fs) | Research insight, for example "20 requirements lack acceptance criteria". Not used at runtime. | Keep it as a research report, or as advisory `ArtifactPolicy` rules. |
| Multi-format codec, DSL, F# source | DSL saves 0.7% of full-record tokens; F# source costs 1.73× on metadata [O19]. | Nothing is lost. |
| A unified graph across the organisation, with caller-scoped queries | No measured task needed it. Typed edges between repositories: 0 authored. Dependency questions are answered by manifests or keywords (M3 hard precision 0.77; M2 recall 0.98, which is biased upward [O11]). | **This is the real loss** if questions across repositories become frequent. It could be added later as a read-only merge of the published per-repository registries. |
| One validator with F# types for every repository | Already available through the Praxis F# validator in 10 repositories. | Nothing is lost. |
| Doorstop-style suspect links | Neither design has them. | Not a differentiator. Adding them to Alternative A costs the same as adding them to EKS. |

**Estimate.** Alternative A captures all of the measured *accuracy* value:

- the trials show front matter at the ceiling;
- it reproduces 134/134 of the typed retrieval gold;
- it beats the EKS validator on agent mistakes.

It loses at most the latency benefit of compiled packages, measured once at about 2× wall time and 1.5 tool calls per session on Sonnet, with no token benefit. It also defers a graph across repositories, which no measured task required. I estimate it at **under 15% of the build and maintenance cost** of the proposed architecture: about 200 lines of F# in existing code against 900–1,800 lines plus a new operational surface. That estimate is mine; it is reasoned from the line counts in §2 and the obligations in §3, not measured.

### 4.5 When the full architecture would be warranted

These conditions would reverse the conclusion. They should be pre-registered:

1. On a harder trial set without wording leakage, with at least 60 questions and at least 3 runs [O2, O4], compiled packages beat registry plus inverse links by at least 10 points of exact match, or by at least 25% of *net* task tokens.
2. Questions across repositories make up a measured, material share of real agent work. For example, at least 20% of logged agent questions over 2 weeks need more than one repository.
3. The knowledge corpus has to be served to principals who do not share the owner's GitHub permissions. Only then does the security perimeter in `security-model.md` become necessary.

## Appendix A: `simple_lint.py`, the reviewer's baseline

This is not meant for production. It shows how small the measured value is to obtain. It ran with Python 3 and PyYAML 6.0.3 against `/home/user/kemiller2002`, read-only.

```python
"""Simplest-alternative linter: front matter + JSON-Schema-like enums + reference graph checks + generated index.
Adversarial baseline for EKS. Lenient YAML (falls back to a line parser, as the Praxis tool accepts these files)."""
import re, sys, glob, os, difflib, collections, yaml

EXC = re.compile(r'/(archive|templates|tests|fixtures|vendor|node_modules|\.git|bin|obj)/')
ID = re.compile(r'^[A-Z][A-Z0-9]{1,9}(?:-[A-Z0-9]{1,12}){0,4}-[A-Z]?\d{2,4}[A-Z]?$')
REL = ['supports', 'contradicts', 'depends_on', 'derived_from', 'supersedes', 'superseded_by',
       'supporting_evidence', 'contradicting_evidence', 'tests_hypotheses', 'related_documents']
STATUS = {'DF': {'draft','review','accepted','superseded','withdrawn','proposed','deprecated'},
          'EV': {'draft','review','accepted','superseded','withdrawn'},
          'EX': {'proposed','active','blocked','completed','cancelled'},
          'HY': {'proposed','active','supported','rejected','superseded','withdrawn'}}
CONF = {'very-low','low','medium','medium-high','high','very-high'}

class DupLoader(yaml.SafeLoader): pass
def _map(loader, node, deep=False):
    keys = [loader.construct_object(k) for k, _ in node.value]
    d = [k for k, n in collections.Counter(keys).items() if n > 1]
    if d: raise yaml.constructor.ConstructorError(None, None, f'duplicate key {d}', node.start_mark)
    return loader.construct_mapping(node, deep)
DupLoader.add_constructor(yaml.resolver.BaseResolver.DEFAULT_MAPPING_TAG, _map)

def lenient(text):  # top-level `key: value` and `- item` lists, like a permissive front-matter reader
    out, key = {}, None
    for line in text.split('\n'):
        m = re.match(r'^([A-Za-z_][\w-]*):\s*(.*)$', line)
        if m:
            key, v = m.group(1), m.group(2).strip()
            out[key] = [x.strip().strip('\'"') for x in v.strip('[]').split(',') if x.strip()] if v.startswith('[') else (v.strip('\'"') if v else [])
        elif key and re.match(r'^\s+-\s+', line) and isinstance(out.get(key), list):
            out[key].append(line.split('-', 1)[1].strip().strip('\'"'))
    return out

def load(root, repos, override=None):
    recs, out = {}, []
    files = []
    for r in repos:
        for p in sorted(glob.glob(f'{root}/{r}/**/*.md', recursive=True)):
            if not EXC.search(p): files.append((r, p, open(p, errors='ignore').read()))
    files += (override or [])
    seen = {}
    for r, p, t in files:
        if override and any(o[1] == p for o in override) and (r, p, t) not in override: continue
        if not t.startswith('---\n'): continue
        end = t.find('\n---', 4)
        if end < 0: out.append(('B', 'unterminated-front-matter', p)); continue
        fm_text = t[4:end]
        try: fm = yaml.load(fm_text, Loader=DupLoader) or {}
        except yaml.YAMLError as e:
            out.append(('B', 'yaml-error', f'{p}: {str(e).splitlines()[0]}'))
            fm = lenient(fm_text)
        if not isinstance(fm, dict) or 'id' not in fm: continue
        i = str(fm['id'])
        if (r, i) in recs: out.append(('B', 'duplicate-id', f'{r}:{i}'))
        recs[(r, i)] = fm
    return recs, out

def lst(v):
    if v is None: return []
    return [str(x) for x in v] if isinstance(v, list) else [str(v)]

def lint(recs, out):
    ids = {k for k in recs}
    known = set(REL) | {'id','title','status','confidence','created','updated','author_agent','created_by_agent','source_uri','retrieved'}
    inc = collections.defaultdict(lambda: collections.defaultdict(set))
    for (r, i), fm in recs.items():
        pre = i.split('-')[0]
        st, cf = fm.get('status'), fm.get('confidence')
        if pre in STATUS and st is not None and str(st) not in STATUS[pre]: out.append(('B', 'status-enum', f'{i}: {st}'))
        if cf is not None and str(cf) not in CONF: out.append(('B', 'confidence-enum', f'{i}: {cf}'))
        for k in fm:
            if k not in known and difflib.get_close_matches(k.lower(), REL, 1, 0.75):
                out.append(('B', 'unknown-relation-field', f'{i}: {k}'))
        for f in REL:
            for t in lst(fm.get(f)):
                if '/' in t or t.endswith('.md'): continue
                if not ID.match(t):
                    if ID.match(t.upper()): out.append(('B', 'malformed-reference', f'{i}.{f}: {t}'))
                    continue
                if t == i and f.startswith('supersed'): out.append(('B', 'self-supersession', i))
                if (r, t) not in ids: out.append(('B', 'dangling-reference', f'{i}.{f}: {t}')); continue
                if f in ('supports', 'contradicts'): inc[(r, t)][f].add(i)
                if f == 'supporting_evidence': inc[(r, i)]['supports'].add(t)
                if f == 'contradicting_evidence': inc[(r, i)]['contradicts'].add(t)
        if str(st) == 'superseded' and not lst(fm.get('superseded_by')): out.append(('B', 'superseded-without-replacement', i))
        for t in lst(fm.get('superseded_by')):
            o = recs.get((r, t))
            if o is not None and i not in lst(o.get('supersedes')): out.append(('A', 'supersession-asymmetry', f'{i}->{t}'))
        if lst(fm.get('superseded_by')) and str(st) not in ('superseded', 'deprecated', 'withdrawn'):
            out.append(('A', 'replacement-but-active', i))
    for (r, i), fm in recs.items():
        for t in lst(fm.get('supporting_evidence')):
            if (r, t) in recs and i not in lst(recs[(r, t)].get('supports')): out.append(('A', 'evidence-link-asymmetry', f'{t}->{i}'))
        for t in lst(fm.get('contradicting_evidence')):
            if (r, t) in recs and i not in lst(recs[(r, t)].get('contradicts')): out.append(('A', 'evidence-link-asymmetry', f'{t}-x->{i}'))
        e = inc[(r, i)]
        if str(fm.get('status')) == 'supported' and not e['supports']: out.append(('B', 'unsupported-claim', i))
        if str(fm.get('status')) == 'supported' and len(e['contradicts']) > len(e['supports']): out.append(('A', 'supported-but-mostly-contradicted', i))
    return out, inc

def index(recs, inc):
    lines = ['# Record index (generated; do not edit)']
    for (r, i), fm in sorted(recs.items()):
        e = inc[(r, i)]
        lines.append(f"- {r}:{i} [{fm.get('status','')}/{fm.get('confidence','')}] {fm.get('title','')}"
                     + (f" | supported-by: {', '.join(sorted(e['supports']))}" if e['supports'] else '')
                     + (f" | contradicted-by: {', '.join(sorted(e['contradicts']))}" if e['contradicts'] else ''))
    return '\n'.join(lines) + '\n'

if __name__ == '__main__':
    repos = ['aegis','arca','dokimos','echelon-diagnostic-framework','fides','forma','mercatus','percepta','praxis','signal']
    recs, out = load('/home/user/kemiller2002', repos)
    out, inc = lint(recs, out)
    c = collections.Counter((s, r) for s, r, _ in out)
    print('records', len(recs)); [print(s, r, n) for (s, r), n in sorted(c.items())]
    if '--index' in sys.argv: open('INDEX.md', 'w').write(index(recs, inc))
```

### Traversal used for the 134/134 reproduction (core of `fe.py`)

```python
L = s.lst  # recs, inc from simple_lint.load/lint over the 11 public repositories
def ev(r, i): e = inc[(r, i)]; return {f'{r}:{x}' for x in e['supports'] | e['contradicts']}
def cur(r, i, seen=None):                     # follow superseded_by, else incoming supersedes
    seen = seen or {i}
    nxt = [t for t in L(recs.get((r, i), {}).get('superseded_by')) if t not in seen]
    nxt += [j for (rr, j), fm in recs.items() if rr == r and i in L(fm.get('supersedes')) and j not in seen]
    return cur(r, nxt[0], seen | {nxt[0]}) if nxt else f'{r}:{i}'
rev = collections.defaultdict(set)            # change to key -> records to review
for (r, i), fm in recs.items():
    for f in ('depends_on', 'derived_from'):
        for t in L(fm.get(f)): rev[(r, t)].add((r, i))
    for f in ('supports', 'contradicts'):
        for t in L(fm.get(f)): rev[(r, i)].add((r, t))
    for f in ('supporting_evidence', 'contradicting_evidence'):
        for t in L(fm.get(f)): rev[(r, t)].add((r, i))
def impact(r, i):                             # 6 hops, as Graph.impactOf g 6
    seen, fr = set(), {(r, i)}
    for _ in range(6):
        fr = {y for x in fr for y in rev[x]} - seen - {(r, i)}; seen |= fr
    return {f'{a}:{b}' for a, b in seen}
```

Result against `experiments/results/retrieval-tasks.json`:

- find-evidence: 56/56 exact;
- change-impact: 66/66 exact;
- current-authority: 12/12 exact.
