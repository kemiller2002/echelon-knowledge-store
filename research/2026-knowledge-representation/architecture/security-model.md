# EKS Security Model: Threat Model, Trust Boundaries and Authority Model

Mission EKS-RESEARCH-2026-001, workstream I. Status: research draft for owner review. Date: 2026-11-16.

Conventions. Claims carry a tag. **[F]** is a fact retrieved from a cited source (`SRC-SEC-nnn`, see `../evidence/sources-security.csv`). **[L]** is a fact read from a local Echelon repository. **[I]** is my inference or design proposal, not established by a source. **[U]** is unverified or unknown. No quantitative claim here is mine; numbers quoted are the sources' own.

---

## 1. Summary

1. The central design rule: **a record gains no authority from being syntactically valid.** Authority is computed by the EKS compiler from three facts it observes itself: *location* (owning repo and path, from the checkout and GitHub's repo identity, never from the record's own fields), *review* (the content is on a protected default branch commit that arrived through required review), and *status* (honored only if the first two hold). [I]
2. The dominant threat is not parser exploitation; it is **instruction-bearing content reaching an agent with tools and private data**. OWASP ranks prompt injection LLM01 and treats indirect injection (external files or sites) as a distinct class [F, SRC-SEC-001]; Greshake et al. show retrieved data acting as an injection channel [F, SRC-SEC-003]; and a published case shows an agent reading a public GitHub issue and leaking private-repo data into a public PR [F, SRC-SEC-007]. EKS compiles exactly the kind of cross-repo context that makes this possible. [I]
3. The second dominant threat is **private-to-public disclosure through derived artifacts** (compiled packages, indices, graph exports committed or published somewhere with lower visibility than their inputs). Nothing in Aegis or Fides prevents this. It needs a label-propagation rule enforced by EKS itself. [I]
4. **What Aegis and Fides actually provide is narrow** (section 8). Aegis is a .NET operational-fault handling library with classification-aware redaction and quarantine primitives, not a security boundary or access-control system [L]. Fides is GitHub-backed sign-in (authentication) for Echelon web apps and explicitly "does not authorize application actions" [L]. Neither verifies content, signs records, enforces repo isolation, or defines service/agent identity. Repo isolation and review enforcement come from **GitHub** (branch protection or rulesets, CODEOWNERS, installation-token scope), not from Aegis or Fides.
5. Praxis/ROS provenance in Echelon repos is **self-reported** and its own documentation says it does not defend against a process that lies about itself [L, SRC-SEC-035]. EKS must therefore not treat `provenance` or `author` front-matter as evidence of origin.

---

## 2. System and assets (assumed from mission brief)

Source repos (~13+, public and private, one owner) hold records as Markdown with YAML front matter or JSON/JSONL. EKS (a separate repo) provides schemas, F# validators, a derived index/graph, and a compiler that emits task-specific context packages for agents. Git is the system of record; the index is a cache. [Mission brief; architecture otherwise **[I]**.]

| Asset | Why it matters |
|---|---|
| A1 Records in source repos | The knowledge; a corrupted record misleads agents and humans |
| A2 EKS schemas and validators | They define what is accepted; tampering silently changes trust |
| A3 Derived index/graph | Fast path to records; may contain content from private repos |
| A4 Compiled context packages | Directly consumed by LLM agents; highest injection impact; may mix classifications |
| A5 EKS compile credentials | Read access to many repos (likely private); a stolen token is cross-repo read |
| A6 Agent write credentials | Agents propose or make changes to records |
| A7 EKS trust configuration (manifest of repos, classifications, roots) | Defines which locations carry authority |
| A8 Provenance and audit trail | Needed for review and incident response |

---

## 3. Trust boundaries

```
 TB1                TB2                     TB3                 TB4              TB5            TB6
 author/agent --> source repo (PR) --> protected default --> EKS ingest/ --> index + package --> LLM agent --> tools/sinks
 (human, bot,       untrusted until        branch of owning    validate        store (derived,    (context =      (write to
  fork, issue)      reviewed               repo = reviewed     (pinned code)   untrusted cache)   untrusted data  repos, issues,
                                                                                                  + instructions)  network)

 TB7  EKS supply chain: NuGet / actions / parsers / schema releases  --> everything above
 TB8  Visibility: private repo content --> any artifact stored where a wider audience can read it
```

| Boundary | Crosses | Must hold |
|---|---|---|
| TB1 author to repo | Arbitrary text, from humans, agents, forks | Nothing is trusted. Content is data until review |
| TB2 PR to protected main | Review decision | Only reviewed content reaches the ref EKS reads. GitHub provides required reviews, code-owner review, status checks, optional signed-commit requirement, force-push restriction [F, SRC-SEC-014, SRC-SEC-016] |
| TB3 repo to EKS ingest | Git objects at a specific commit SHA | EKS reads a pinned commit SHA reachable from the protected branch, not a branch name or working tree. Repo identity comes from the GitHub API, not file contents [I] |
| TB4 EKS validator | Untrusted bytes into a parser | Parse as data only; hard resource limits; validator code and schemas come from a pinned EKS release, never from the repo under test [I] |
| TB5 index/package to agent | Text into an LLM context | Trust labels and segregation; no record text in the instruction channel; context cannot by itself authorize actions [F for need: SRC-SEC-001, SRC-SEC-006; mechanism I] |
| TB6 agent to sinks | Agent-initiated writes and network calls | Least privilege; sinks constrained after untrusted or higher-classification content is in context [F: SRC-SEC-001 least privilege and human approval; SRC-SEC-005, SRC-SEC-006 on design-level constraint] |
| TB7 supply chain | Code and schemas | Pinning by digest or full SHA; attested releases [F: SRC-SEC-018] |
| TB8 visibility | Classified content into an artifact with a wider audience | Output audience must not exceed the minimum visibility of any input [I] |

---

## 4. Authority model

### 4.1 Principle

> Valid syntax is a necessary condition for a record to be *considered*; it is never a sufficient condition for it to *carry authority*. [I]

Authority is derived, not declared. The compiler computes it; record fields such as `status: accepted`, `authority:`, `source:`, `author:`, `reviewed_by:` are **claims** and are compared against observed facts, never trusted in place of them.

### 4.2 Three derivation inputs

| Input | Observed how (proposal) | What is ignored |
|---|---|---|
| **Location**: owning repo + path | Repo identity from the GitHub API (immutable repo id and full name) plus the path relative to a *knowledge root declared in that repo's trusted manifest*. The record ID must carry the owning-repo namespace, and the namespace must match the repo [I] | Any `repo`, `source`, `owner`, `url` field in the record |
| **Review**: merged to protected main | Commit is reachable from the protected default branch; the branch protection or ruleset requiring PR review (and code-owner review for the knowledge paths) was active; the PR approver is not the author. Verified via GitHub API at compile time [I; the controls exist per SRC-SEC-014/015/016; the exact API sequence to prove "this blob arrived by reviewed PR" is **[U]** and must be prototyped] | `reviewed_by`, `approved`, signatures declared in front matter |
| **Status**: lifecycle state | Read from the record, but honored only when Location and Review hold. Allowed values and transitions come from the EKS schema for the type, not the record [I] | Any status on a record that fails Location or Review |

### 4.3 Authority tiers (proposed)

| Tier | Meaning | Included in packages | May influence tool use? |
|---|---|---|---|
| **A0 Rejected** | Fails validation, location, or limits | Never (quarantined; see section 8 on Aegis `Quarantine`) | No |
| **A1 Unreviewed** | Valid, in the right place, but on a PR branch, an unprotected branch, or merged without required review | Only on explicit request, labeled `unreviewed` | No |
| **A2 Reviewed-draft** | On protected main, status `draft` or `proposed` | Labeled `proposed`; excluded from default "what is decided" queries | No |
| **A3 Accepted knowledge** | On protected main, reviewed, status `accepted` (not superseded or retracted) | Yes, as **reference data** | No (see 4.4) |
| **A4 Instruction sources** | A very small, manifest-declared allowlist of files (for example a repo's `AGENTS.md`) delivered through the agent's existing instruction mechanism | Not via EKS records. EKS never promotes a record to A4 [I] | Yes, through the agent runtime's own channel |

### 4.4 Knowledge informs; it does not command

An accepted record may say "decision: use X." The compiled package presents that as a **cited fact with provenance**, in a structured data envelope, never as imperative text spliced into the system or developer instruction channel. No record can: grant itself or another record authority, edit EKS trust configuration, name tools to call, change an agent's permissions, or alter another repo's records. [I] This is the practical reading of OWASP's "segregate and identify external content" and "constrain model behavior" [F, SRC-SEC-001], and of the design-level argument that untrusted data must be unable to alter control flow [F, SRC-SEC-005].

Honest limit: because a model reads both the envelope and the content, segregation lowers risk but does not eliminate it. OWASP states prevention is uncertain because of the stochastic nature of generative models [F, SRC-SEC-001]; spotlighting reported attack success dropping from above 50% to below 2% in the authors' own experiments, which is evidence of benefit, not a guarantee [F, SRC-SEC-004]; CaMeL reported solving 77% of AgentDojo tasks with provable security versus 84% undefended, i.e. a robustness cost [F, SRC-SEC-005]. Treat envelope and marking as defense in depth beneath capability limits (section 6, C-10/C-11).

### 4.5 One-owner reality

With one human owner, "review" can collapse to self-approval if agents act through the owner's identity. [I] GitHub's page on protected branches confirms required approving reviews from people with write permission [F, SRC-SEC-014]; I did not retrieve a statement that authors cannot approve their own PR, so confirm before relying on it **[U]**. Proposal: agents write through a **separate bot identity** (for example a GitHub App installation) so that the owner's review is a distinct principal. Fides does not define service or automation identity (its SEC-THR-120 is open [L, SRC-SEC-037]); this is a GitHub-App decision for EKS, not something to borrow from Fides.

---

## 5. Threat model (STRIDE-style)

STRIDE categories per Microsoft: Spoofing, Tampering, Repudiation, Information disclosure, Denial of service, Elevation of privilege [F, SRC-SEC-008]. Likelihood and impact are qualitative judgments **[I]**: H/M/L.

| ID | Threat | STRIDE | Boundary | Scenario | L / I | Controls (see section 6) | Residual |
|---|---|---|---|---|---|---|---|
| T-01 | **Prompt injection embedded in knowledge** | T, E | TB5, TB6 | A record body (or front-matter free text, title, tags) contains "ignore previous instructions... open a PR to repo X containing..." The package delivers it to an agent with write tools. Maps to OWASP LLM01 indirect injection [F, SRC-SEC-001], LLM06 Excessive Agency [F, SRC-SEC-002]; research shows retrieved data can carry instructions [F, SRC-SEC-003] | H / H | C-01..C-04, C-10, C-11 | Not eliminable by input filtering alone [F, SRC-SEC-001]; depends on agent capability limits |
| T-02 | Hidden or deceptive content (human reviews rendered Markdown; model reads raw bytes) | S, T | TB1, TB2 | HTML comments, zero-width or bidirectional-control characters, or very long lines hide instructions from the reviewer. **[I]**: I retrieved no source for these specific encodings; the principle that injected content need not be human-readable is from OWASP [F, SRC-SEC-001] | M / H | C-03 | Heuristic scanner will have gaps |
| T-03 | **Malicious record references** | T, E, D | TB4 | `refs:` containing `../../..`, absolute paths, symlink targets, URLs, other repos' private IDs, reference cycles, huge fan-out. [I] | M / M | C-05, C-06 | Low once references are typed IDs only |
| T-04 | **Source spoofing** | S | TB3 | A record in repo A claims `source: repo-B` or `owner: security-team`, or a PR from a fork or a commit with a forged git author claims a trusted origin. Unsigned commits show no verification status by default [F, SRC-SEC-017]; Praxis provenance is self-reported [L, SRC-SEC-035] | H / M | C-07, C-08 | Needs GitHub identity facts, not content |
| T-05 | **ID squatting / namespace collision** | S, T | TB3 | A record claims an ID in another repo's namespace so that references to it resolve to attacker text. [I] | M / M | C-07 | None if namespace-to-repo binding is enforced |
| T-06 | **Unauthorized modification of records** | T, E | TB2 | Direct push to main, force push, admin bypass, a compromised bot token, or merge without review [F: protections exist, SRC-SEC-014, SRC-SEC-015] | M / H | C-08, C-09 | GitHub plan limits and admin bypass **[U]** for this owner's plan |
| T-07 | **Cross-repo leakage** (agent) | I | TB5, TB6 | An agent with a package of private-repo knowledge plus untrusted input plus ability to write externally leaks data; the "lethal trifecta" [F, SRC-SEC-006]; demonstrated with GitHub MCP, a malicious public issue, and a PR into a public repo [F, SRC-SEC-007] | H / H | C-12, C-10, C-11 | Needs runtime policy in the agent harness, outside EKS |
| T-08 | **Private-to-public disclosure via derived artifacts** | I | TB8 | EKS compiles an index or package from private repos and it is committed to a public repo, published as a Pages site, uploaded as a public release asset, or shown in public CI logs. Making a repo public exposes its code and Actions logs to everyone [F, SRC-SEC-020]; push protection is pattern-based and not a classification control [F, SRC-SEC-019] | M / H | C-12, C-13 | Visibility changes after the fact (a private repo later made public) need a re-check |
| T-09 | **Visibility flip of a source repo** | I | TB8 | A repo goes private to public with sensitive records or history; or a public repo's record refers to private IDs. [F: effects of visibility change SRC-SEC-020; reference-leak mechanics I] | L / H | C-13 | Process control |
| T-10 | **Dependency poisoning** | T, E | TB7 | A compromised NuGet package (YAML/Markdown parser), a GitHub Action referenced by tag, or an `#r "nuget:"` directive in a script. Pinning an action to a full commit SHA is GitHub's guidance, since tags can move [F, SRC-SEC-018]; `#r` carries "the same security considerations as any other form of package reference" [F, SRC-SEC-013]; OWASP LLM03 Supply Chain [F, SRC-SEC-002]. Fides's own model notes third-party actions pinned by tag in some workflows [L, SRC-SEC-037] | M / H | C-14 | Standard supply-chain residual |
| T-11 | **Cross-repo reference poisoning** (mutable refs) | T | TB3 | Record in repo A references `repo-B/rec-7` by branch; B later edits or deletes it; A's package silently changes meaning. [I] | M / M | C-06 | Requires pinned refs in packages |
| T-12 | **False provenance** | S, R | TB3 | `provenance:` or `created_by:` lies about the actor or model. Praxis/ROS records are explicitly self-reported and do not defend against a process that lies [L, SRC-SEC-035] | H / M | C-07, C-15 | Cryptographic attestation not available today **[U]** |
| T-13 | **Untrusted AI-generated content treated as knowledge** | T | TB1, TB2 | An agent writes plausible but wrong or poisoned records; later agents ingest them (LLM04 Data poisoning, LLM09 Misinformation [F, SRC-SEC-002]; information ecosystem contamination [F, SRC-SEC-003]). Feedback loop: agent-authored records become inputs for the next agent. | H / M | C-02, C-09, C-16 | Review load on the human owner |
| T-14 | **Executable DSL / code execution on ingest** | E | TB4 | An `.fsx` in a knowledge directory is loaded or evaluated. `dotnet fsi` "reads the code and executes it"; scripts can `#load` other scripts and `#r "nuget:..."` [F, SRC-SEC-013]. | L (if banned) / H | C-17 | None if never executed |
| T-15 | **YAML deserialization to native objects** | E | TB4 | A parser configured to instantiate arbitrary types via tags. OWASP: prefer pure data formats and safe loaders; for .NET, do not let the data stream choose the target type [F, SRC-SEC-009]. Whether a specific F#/.NET YAML library is safe by default is **[U]** (not researched here) | M / H | C-17, C-18 | Verify the chosen library |
| T-16 | **Parser DoS: alias expansion ("billion laughs"), deep nesting, huge files** | D | TB4 | Small YAML expands to very large structures. YAML has anchors and aliases and processors "should reject" unidentified aliases [F, SRC-SEC-011]; alias-expansion DoS advisories recur across YAML parsers, and fixes add limits [F, SRC-SEC-012 (snippets only)]; the billion-laughs name and entity-expansion mitigations are documented for XML [F, SRC-SEC-010]. OWASP LLM10 Unbounded Consumption [F, SRC-SEC-002] | M / M | C-18, C-19 | Library-specific limits **[U]** |
| T-17 | **Front-matter / format confusion** | T | TB4 | YAML 1.1 vs 1.2 typing differences make the same file parse differently in two tools; YAML 1.2 "removed many of the problematic implicit typing recommendations" [F, SRC-SEC-011]. A validator and a downstream consumer disagree, so a record validates under one reading and means something else under another [I] | M / M | C-18 | Single parser; semantic round-trip tests |
| T-18 | **Schema downgrade attack** | T, E | TB4 | A record declares an old `schema:` version lacking a mandatory security field (classification, status rules) to bypass newer constraints. [I] | M / H | C-20 | Needs a per-type minimum schema floor |
| T-19 | **Unauthorized schema or validator change** | T, E | TB7 | A PR (possibly from a source repo, or an agent) edits EKS schemas or validators to widen acceptance; or CI runs the validator from the branch under test. [I] Workflow `pull_request_target` and untrusted checkouts are a documented risk [F, SRC-SEC-018] | M / H | C-14, C-21 | Needs EKS repo protection and attested releases |
| T-20 | **Stale or superseded record served as current** | T | TB5 | Index lags source; a retracted or superseded record is still compiled in. [I] | M / M | C-22 | Eventual consistency window |
| T-21 | **Credential exposure** (compile token reads many repos) | I, E | TB3 | EKS compile token leaks via logs or an agent session; compromise gives cross-repo read. Secrets must not reach logs (a rule Fides enforces for its own tokens [L, SRC-SEC-037]). | L / H | C-23 | Operational |
| T-22 | **Repudiation of changes** | R | TB2 | A record is rewritten and history lost (force push, squash of reviewed commits). | L / M | C-08, C-24 | Needs force-push block |
| T-23 | **Information disclosure through errors and search** | I | TB5 | Validator error messages, index search snippets, or "did you mean" leak titles or content of records the caller may not see. [I] | M / M | C-12, C-25 | Needs caller-scoped queries |

---

## 6. Required controls

Controls marked **REQUIRED** are what I consider prerequisites to compiling any package for an agent that has write tools.

### Content and injection

- **C-01 REQUIRED. Data-only envelope.** Packages are structured (JSON or similar) with per-record `{id, repo, path, commit, blob_hash, authority_tier, classification, status}` plus content in a clearly delimited field. Record text never enters the system/developer channel. [I; basis SRC-SEC-001, SRC-SEC-004]
- **C-02 REQUIRED. Trust labeling in the package.** Each record is labeled with tier (A1 unreviewed, A2 proposed, A3 accepted) and origin kind (human, agent, unknown, derived from observed identity, not self-report). Default packages contain A3 only. [I]
- **C-03. Content hygiene validation at ingest.** Reject or flag: control and format characters (zero-width, bidi, tag characters) outside an allowlist; raw HTML including comments in Markdown bodies unless the schema permits it; excessive line length; embedded base64 blobs. Heuristic scan for instruction-like phrases is advisory only (flag for human review, never an authority input). [I; no source retrieved for specific encodings]
- **C-04. Spotlighting or datamarking** of record text in the package as defense in depth [F: SRC-SEC-004; benefit reported by its authors only].
- **C-10 REQUIRED. Capability limits on consumers.** The agent harness restricts tools, applying least privilege, and requires human approval for high-risk actions, per OWASP [F, SRC-SEC-001]. EKS emits, with each package, a **context risk descriptor** (max classification, lowest tier present, whether any untrusted tier is present) that the harness can use to gate tools. EKS cannot enforce this itself. [I; gap]
- **C-11. Constrain post-ingest actions.** Once a session holds untrusted or higher-classification content, deny writes to lower-classification sinks (public repos, issues, PRs, outbound network). This is the lethal-trifecta break [F: SRC-SEC-006; design-level view SRC-SEC-005]. Harness responsibility; gap.

### References and identity

- **C-05 REQUIRED. References are typed IDs, never paths or URLs.** Strict ASCII ID grammar and length limit; resolved only through the index; reject path separators, `..`, schemes, and non-normalized Unicode. Reject git symlink entries (mode 120000) in knowledge roots and do not follow them. Cap reference depth, fan-out, and cycle count. [I]
- **C-06. Cross-repo references pinned.** Package references carry `repo@commit` plus content hash; "latest" is resolved at compile time and recorded. [I]
- **C-07 REQUIRED. Namespace bound to repo.** An ID prefix must equal the namespace the EKS manifest assigns to the repo the record was read from; mismatches are rejected, not reinterpreted. Front-matter `source`/`owner` fields are ignored for authority. [I]
- **C-15. Observed provenance.** Record origin kind is derived from GitHub facts (the bot identity that opened the PR, the human who approved), stored as EKS-computed metadata, not copied from record fields. Praxis `provenance` blocks may be displayed as self-reported context only [L, SRC-SEC-035]. Signed commits can add evidence where adopted: GitHub shows Verified only for signed commits with checkable signatures, and rebase-and-merge produces commits GitHub cannot sign for the author [F, SRC-SEC-017].

### Source integrity and review

- **C-08 REQUIRED. Protected default branches** on every source repo: required PR review, required code-owner review for knowledge roots, required status checks (EKS validation), force-push and deletion blocked [F: capabilities in SRC-SEC-014, SRC-SEC-015, SRC-SEC-016]. CODEOWNERS is read from the PR's base branch and should itself be code-owner-protected [F, SRC-SEC-016]. **[U]**: availability on this owner's plan and private repos. Note the pages say rulesets are available on GitHub Team and Enterprise plans [F, SRC-SEC-015], and the protected-branches page did not state plan requirements [F, SRC-SEC-014].
- **C-09 REQUIRED. Compile only from verified commits.** At compile time, resolve each repo's default branch to a SHA, check protection state via API, and refuse (fail closed) if protection is absent or the repo is not in the manifest. Record `{repo, commit}` in the package. [I]
- **C-16. Agent-authored records need human promotion.** Agents may open PRs creating `proposed` records; only the review step (human, different principal from the author) moves them to `accepted`. An agent may not approve a record that another agent wrote and may not change `status` to `accepted` in its own PR (a status-change rule enforced by CI check). [I]
- **C-24. Immutable history.** Force-push blocked; reviewed commits retained; corrections by new commits (supersede or retract), not rewriting. [F: force-push restriction exists, SRC-SEC-014; policy I]

### Parsing

- **C-17 REQUIRED. No code execution on ingest.** No `.fsx` or any script under knowledge roots is executed, loaded, or compiled by EKS. Validators and schemas are compiled F# in a pinned EKS release. If scripting is ever wanted for migrations, run it only from EKS-owned, reviewed locations. [F: .fsx executes with `#load`, `#r nuget`, SRC-SEC-013; policy I]
- **C-18 REQUIRED. Data-only, restricted YAML.** Parse front matter as a restricted subset: reject anchors, aliases, merge keys, custom or local tags, multiple documents, and non-string-keyed maps unless a schema needs them; fix one YAML version (1.2 core-ish); never instantiate application types from data [F basis: SRC-SEC-009, SRC-SEC-011; I for the subset]. Consider JSON or JSONL for machine-written records to remove YAML's surface. [I]
- **C-19 REQUIRED. Resource limits**: file size, front-matter size, nesting depth, node count, key count, string length, files per repo, records per package, parse timeout. Over-limit input becomes a rejected record, not a crash. [I; need motivated by SRC-SEC-010, SRC-SEC-012]

### Schemas

- **C-20. Schema floor.** The registry declares per record type a `minimum_schema` and a list of migration-window versions; security-relevant fields (classification, status semantics) are required in every accepted version; the version a record declares picks a *validator*, but cannot lower the floor. [I]
- **C-21. Validator provenance.** CI for source repos runs the validator at a pinned EKS release SHA or digest, never code from the PR; EKS's own repo has protected main, code-owner review on `schemas/` and validators, and attested releases. [I; Fides's Sigstore-attested release assets are a precedent in this ecosystem, SRC-SEC-037, [L]; GitHub guidance on SHA pinning SRC-SEC-018 [F]]
- **C-14 REQUIRED. Supply chain hygiene.** Pin Actions to full commit SHA, restrict `GITHUB_TOKEN` to least privilege, avoid `pull_request_target`/untrusted checkout, pin NuGet versions and lock files, review parser upgrades [F: SRC-SEC-018, SRC-SEC-013]. Fides's own threat model records a gap here (tag pinning) so do not copy its workflows blindly [L, SRC-SEC-037].

### Leakage

- **C-12 REQUIRED. Classification labels and propagation.** Each source repo has a classification in the trusted manifest (`public`, `private`, optionally finer: `internal`, `sensitive`), cross-checked with the repo's real GitHub visibility at compile time (a mismatch is an error). Each record may carry a classification that can only *raise* the repo's. A package or index's classification is the **maximum** of its inputs. [I] Aegis's `ContextValue` vocabulary (Public/Internal/Sensitive/Secret) is a candidate naming scheme [L, SRC-SEC-034].
- **C-13 REQUIRED. Output sink rule ("no write down").** The compiler refuses to write or publish an artifact to any destination whose audience is wider than the artifact's classification. Default destinations: ephemeral build workspace or a private EKS-store repo. Derived indices from private inputs are never committed to a public repo; CI in public repos must not run EKS compile against private inputs; Actions logs from jobs touching private data are treated as public if the repo is public [F: logs visible on going public, SRC-SEC-020]. A scheduled recheck flags any repo whose visibility changed. [I]
- **C-25. Caller-scoped queries.** Index queries and error messages are filtered to what the requesting principal's GitHub access permits (GitHub enforces repository read; Fides states repository read/write is decided by GitHub [L, SRC-SEC-038]). Where the EKS index is shared, the index must not return titles or snippets from repos the caller cannot read. [I]
- **C-23. Credentials.** Separate read-only compile credential (GitHub App installation token scoped to the needed repos) from agent write credentials; no tokens in logs or packages; short-lived tokens. [I; Fides's redaction invariant is a precedent, [L, SRC-SEC-037]]

### Freshness

- **C-22. Status resolved at compile time from source commit**, not from the index; superseded and retracted records are excluded or flagged. The index is a cache rebuilt from git. [I]

---

## 7. Residual risks that controls cannot remove

1. Models can still be steered by A3 content from a compromised-but-reviewed record (reviewer fatigue; a plausible malicious PR). Mitigate with capability limits (C-10, C-11), not with more validation. [I]
2. No cryptographic attestation of who authored what is available in the Echelon stack today. Signed commits and bot identity improve it; `provenance` front matter does not. [L, F]
3. EKS cannot control what the agent harness does after receiving a package. The context risk descriptor (C-10) is advisory until a harness enforces it. [I]
4. One owner: review independence relies on separate bot and human principals. [I]
5. Plan-dependent GitHub features (rulesets, private-repo protections, push rulesets) are unverified for this owner. **[U]**

---

## 8. What Aegis and Fides verifiably provide (from local repos, verified 2026-11-16)

### Aegis (`/home/user/kemiller2002/aegis`)

| Claim | Evidence |
|---|---|
| Purpose: "handling and recovering from unexpected operational failure in .NET applications" (the GitHub call that times out, the stored file that will not parse). Packages: `Aegis.Core`, `Aegis.Store.GitHub` (one immutable file per event in a GitHub repo), `Aegis.Integration.GitHub` [L, SRC-SEC-033] | README |
| Fault values are classified `Public / Internal / Sensitive / Secret`; redaction runs before an event reaches any sink; secret values never persisted [L, SRC-SEC-034] | `Types.fs`, `Redaction.fs` |
| Containment primitives: circuit state, **quarantine of unprocessable data** keyed by source id, dead-letter [L, SRC-SEC-034] | `Containment.fs` |
| Projects a fault into a Tutela evidence record; "Tutela remains responsible for deciding invariant state" [L, SRC-SEC-034] | `Tutela.fs` |
| `aegis-boundaries.json` declares *fault* boundaries (storage, API integration, serialization, data integrity) for the Aegis repo itself [L] | file |
| Docs state Aegis is for "the GitHub outage, not the business rule" and not for outcomes the domain models [L, SRC-SEC-033] | `docs/aegis/README.md` |

**Not provided by Aegis:** access control, repo isolation, content validation or injection detection, signing or attestation, prevention of private-to-public output, identity. The name suggests a security boundary system; the repository does not implement one. Note a mismatch: the Dokimos README says "Security remains owned by Aegis/Tutors" [L, SRC-SEC-039], but Aegis's own README describes fault handling and feeds evidence into Tutela, the security-engineering discipline [L]. Treat Tutela (not Aegis) as the model for how EKS should record threats, invariants and evidence (`asset, trust boundary, threat, invariant, control, evidence, unknown` [L, SRC-SEC-039]).

**Usable by EKS:** (a) a .NET fault model for ingest and compile failures (e.g. unreadable repo, parse failure, rate limit) so failures are recorded rather than swallowed; (b) `Quarantine` as the data structure for rejected records (tier A0); (c) the classification vocabulary and "redact before persist" pattern for fault context; (d) `Store.GitHub` as a place for fault events, if desired. Aegis `GitHubStore.query` returns `Store.Malformed` listing unreadable records rather than silently skipping [L, SRC-SEC-033], which is the right failure posture. None of this enforces C-01 to C-25.

### Fides (`/home/user/kemiller2002/fides`)

| Claim | Evidence |
|---|---|
| "Fides is the common Echelon authentication and single sign-on (SSO) identity boundary", starting with a GitHub App and expiring user-to-server tokens; parts: stateless Exchange (holds client secret) and F# Client library for Limen/Forma WebAssembly apps. "Nothing is deployed from this repository." [L, SRC-SEC-036] | README |
| "Fides authenticates. It does not authorize application actions." Repository read/write is enforced by GitHub; capabilities finer than a repository are advisory unless a server-side enforcement point exists; permission boundaries should be separate repositories [L, SRC-SEC-038] | AUTHORIZATION-OWNERSHIP |
| Service actions: enforcement point is "Installation token scope"; no service-identity model (SEC-THR-120 open; DF-FIDES-2026-0014) [L, SRC-SEC-037, SRC-SEC-038] | threat model |
| Evidence is from unit, simulated-provider, emulated-host and wasm tests; "nothing has been run against live GitHub or a deployed exchange" [L, SRC-SEC-037] | threat model |
| Multi-tenant and role models are undecided (SEC-THR-108, SEC-THR-110 unknown) [L, SRC-SEC-037] | threat model |
| Sigstore-attested release assets, pinned Arca feed with checksums (partial mitigation of supply-chain compromise) [L, SRC-SEC-037] | threat model |

**Not provided by Fides:** authorization or policy engine (none planned [L]), agent or service identity, repo-level data isolation beyond what GitHub enforces, content signing, review verification. It is also a browser-session sign-in library, so it addresses human users of Echelon web apps, not the headless EKS compiler or agents. [I]

**Usable by EKS:** (a) a precedent for the discipline that GitHub is the enforcement point for repo access and the rule "default-deny, fail closed on unknown input" (`DecisionUnavailable` → deny) [L, SRC-SEC-038]; (b) if EKS ever offers a human-facing web viewer over private knowledge, Fides gives sign-in and the GitHub token whose reach defines what that user may read; EKS would then need its own authorization (C-25); (c) release attestation practice (Sigstore) as a model for C-21.

### Praxis / ROS (present in both repos)

Praxis provenance is self-reported and not authentication, by its own statement [L, SRC-SEC-035]; use it for audit narrative, never as an authority input (C-15).

### Gap table

| Needed control | Aegis | Fides | Provided by | Gap |
|---|---|---|---|---|
| Repo-level read isolation | No | Delegates to GitHub | GitHub | None beyond GitHub config |
| Reviewed-merge enforcement | No | No | GitHub branch protection/rulesets/CODEOWNERS [F] | Plan availability **[U]**; EKS must verify state via API (C-09) |
| Agent/service identity | No | Not defined (open) | GitHub App installation tokens (EKS to adopt) | Gap, EKS decision |
| Content validation, injection hygiene | No | No | EKS (to build) | Gap |
| Classification and no-write-down | Vocabulary only | No | EKS (to build) | Gap |
| Quarantine of rejected records | Primitive exists | No | Aegis `Containment` | Integration work |
| Fault capture, redaction of fault context | Yes | Its own for tokens | Aegis | Covers faults only |
| Release/schema attestation | No | Precedent only | Sigstore / GitHub (EKS to adopt) | Gap |
| Cryptographic authorship | No | No | Signed commits (GitHub) | Optional, partial [F] |
| Runtime capability limits on agents | No | No | Agent harness | Out of EKS; risk descriptor only |

---

## 9. Verification plan (suggested tests for the prototype)

1. Red-team corpus: records with embedded instructions, hidden Unicode, HTML comments; assert the package marks them and the harness's tool-gating blocks the demo exfiltration. [I]
2. Negative path tests: `../` reference, symlink in knowledge root, ID in another repo's namespace, front-matter claiming `status: accepted` on an unreviewed branch (must be A1), declared old schema without required security fields.
3. Parser limits: alias bomb, deep nesting, 100 MB file, many small files; the pipeline must reject within a time bound without exhausting memory (define the bound at prototype time).
4. Leak test: compile from a private fixture repo and attempt output to a public-destination fixture; the compiler must refuse. Flip fixture visibility and confirm recheck.
5. Review-proof test: a commit pushed to a non-protected branch, or merged with protection disabled, must not produce an A3 record.
6. Per the Tutela approach, record each of these as threat / invariant / control / evidence / unknown rather than asserting "secure" [L, SRC-SEC-039].

## 10. Open questions for the owner

- GitHub plan for the owner's org and whether branch protection/rulesets apply to private repos in practice **[U]**.
- Will agents write through a GitHub App bot, or the owner's own token? This decides whether independent review is real (section 4.5).
- Where may compiled packages live (private EKS-store repo vs ephemeral only)? C-13 depends on it.
- Which F#/.NET YAML and Markdown libraries will be used, and what alias and depth limits do they offer? **[U]** (not researched).
- Which agent harness will consume the risk descriptor, and who owns enforcing C-10/C-11?
