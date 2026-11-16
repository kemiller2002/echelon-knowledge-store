# Literature: Context Engineering, Input Format Effects, and How Coding Agents Consume Repository Knowledge

Mission EKS-RESEARCH-2026-001, workstream F. Compiled 2026-11-16.

Conventions. **[Cited]** is a fact reported by a retrieved source. **[Inference]** is my reasoning. Source IDs map to `evidence/sources-agents.csv`. "Snippet only" or "abstract only" means I did not read the full text. Official-docs claims were read on 2026-11-16 and describe product behavior as of that date; these docs change frequently.

## 0. Bottom line

1. **Format effects are real but model- and task-specific, and no format wins everywhere.** Measured spreads range from a few points to tens of points (section 3). The best-supported statement is that format can swing accuracy, not that Markdown, JSON or YAML is best.
2. **The only independent evaluation of repository instruction files found (ETH Zurich, Feb 2026) reports no success-rate gain from LLM-generated context files and about +4% from developer-written ones, with cost up about 20%** (SRC-AG-026). Context files are not free.
3. **All major coding agents consume Markdown (plus light YAML front matter) and look files up with tools.** None natively parses a custom schema. The cost of teaching a new format has not been measured directly in anything I retrieved (section 6).
4. **Long context degrades even simple tasks**, so selecting fewer, relevant tokens is supported across sources (section 2).

## 1. Context engineering: definitions and practice

- **[Cited]** Anthropic defines context as the set of tokens included when sampling and context engineering as curating those tokens across system prompt, tools and message history, iteratively. Goal: "the smallest possible set of high-signal tokens". Tactics: just-in-time retrieval via lightweight identifiers, compaction, structured note-taking to a file such as NOTES.md, and sub-agents returning condensed summaries, often 1,000-2,000 tokens (SRC-AG-012).
- **[Cited]** Same source recommends delineating prompt sections with XML tags or Markdown headers, and says exact formatting matters less as models improve (SRC-AG-012). Anthropic's prompting guide says XML tags help Claude parse prompts mixing instructions, context, examples and inputs, and suggests wrapping multiple documents in `<document>` tags with source metadata (SRC-AG-052). No quantitative claim accompanies this guidance in the text retrieved.
- **[Cited]** Tool definitions are a context cost: a five-server MCP setup with 58 tools consumed about 55K tokens before any work; a tool-search approach cut upfront cost to roughly 500 tokens and raised MCP-eval accuracy (Opus 4: 49% to 74%; Opus 4.5: 79.5% to 88.1%) (SRC-AG-013, Anthropic-measured).
- **[Inference]** The same logic applies to knowledge: always-loaded instruction text is a fixed cost paid every turn whether or not it is relevant.

## 2. Context rot and long-context degradation

- **Lost in the Middle** **[Cited]**: on multi-document QA and key-value retrieval, performance is best when relevant information is at the beginning or end and drops in the middle, even for long-context models (SRC-AG-001).
- **RULER** **[Cited]**: simple needle tests are "superficial"; across 17 models and 13 tasks, nearly all scored near-perfectly on basic needle tests but degraded as length and complexity grew; only about half of models claiming 32K+ kept satisfactory performance at 32K (SRC-AG-010).
- **NoLiMa** **[Cited]**: with no literal overlap between question and needle, 11 of 13 models fell below 50% of their short-context baseline at 32K; GPT-4o dropped from 99.3% to 69.7%; reasoning models and chain-of-thought also struggled (SRC-AG-011).
- **Chroma "Context Rot"** **[Cited, vendor technical report, July 2025]**: 18 models; performance fell with input length in every experiment; lower needle-question similarity degraded faster; distractors compounded the effect; no notable variation by needle position across 11 positions (this differs from the 2023 "lost in the middle" pattern); shuffled haystacks beat coherent ones; on LongMemEval-derived prompts, focused ~300-token prompts consistently beat full ~113k-token prompts, with the largest Claude gap driven by abstentions. Authors caveat that tasks are simple and mechanisms unexplained (SRC-AG-002).
- **[Inference]** Position-in-context effects have weakened in newer models per Chroma, but length, distractors and low lexical overlap remain degrading factors. This supports compiling small task-specific contexts and keeping records self-contained, without needing to engineer ordering.

## 3. Effect of input FORMAT on accuracy

### 3.1 Prompt-format studies (general tasks)

- **He et al. 2024, "Does Prompt Formatting Have Any Impact on LLM Performance?"** (SRC-AG-003) **[Cited]**. Plain text, Markdown, JSON and YAML templates with the same content; GPT-3.5-turbo and GPT-4 variants. Abstract: GPT-3.5-turbo varied by up to 40% on a code translation task; GPT-4 more robust. From Table 1 (best-worst spread in points):
  - MMLU: GPT-3.5-0613 JSON 59.7 vs Markdown 50.0 (9.7); GPT-4-1106 Markdown 81.2 vs JSON 73.9 (7.3); GPT-4-32k Markdown 81.3 vs JSON 77.8 (3.5).
  - HumanEval: GPT-3.5-0613 JSON 59.8 vs plain 40.2 (19.6); GPT-4-1106 Markdown 86.6 vs plain 82.9 (3.7); GPT-4-32k plain 76.2 vs JSON 21.95 (54.3, attributed to the model writing reasoning without producing code under the JSON template).
  - NER Finance: GPT-3.5 plain 37.2 vs YAML 24.6; GPT-4-1106 YAML 53.8 vs plain 49.3.
  - FIND: GPT-3.5 plain 15.9 vs Markdown 5.2; GPT-4-1106 Markdown 20.7 vs plain 20.1 (0.6).
  - Cross-model: GPT-3.5 tends to prefer JSON, GPT-4 Markdown; best format differs per model and task; transfer of best format across model families is low (IoU often below 0.2). Larger models are more robust (GPT-4-1106 intra-model variance below 0.036 on all benchmarks).
  - Caveat: models are 2023-era GPT models; no Claude or current-generation model is covered.
- **Sclar et al. 2023** **[Cited, abstract only]**: formatting changes alone produced differences of up to 76 accuracy points on LLaMA-2-13B (SRC-AG-008). This is an upper bound from few-shot prompt-format perturbations (spacing, separators), not a Markdown-vs-JSON contrast.
- **Tam et al. 2024, "Let Me Speak Freely?"** **[Cited, abstract only]**: constraining *outputs* to JSON/XML degraded reasoning, with stricter constraints causing larger drops (SRC-AG-009). This concerns output format; the page gave no effect sizes. StructEval (SRC-AG-058, snippet only) reports even strong models near 75% on generating structured outputs across 18 formats; also output-focused.

### 3.2 Table / structured-data format studies

- **Sui et al. 2023 "Table Meets LLM"** **[Cited, abstract]** (SRC-AG-004): GPT-3.5/GPT-4 performance varies with table format, content order, role prompting and partition marks; chosen settings gave gains of +0.84 to +5.68 points on five benchmarks. A search-result summary (snippet only, not verified) says delimiter-separated formats trailed HTML by 6.76 points.
- **Singha et al. 2023** **[Cited]** (SRC-AG-005): GPT-3 (text-davinci-003), eight table formats. Fact-finding average pass@1: DFLoader (pandas code) 79.79%, JSON 77.93%, TSV 75.8%, CSV 75.78%, HTML 71.4%, Markdown 67.32%. Transformation F1: DFLoader 98.55%, JSON 94.89%, CSV 89.55%, Markdown 36.11%. Markdown was worst on most tasks, and the authors note this contradicts earlier work favoring HTML. Noise operations moved results by tens of points (e.g. transposing a JSON table: +20.86 on navigation, -65.00 on column lookup).
- **Improving Agents, table lookups** **[Cited, practitioner blog, single model]** (SRC-AG-006): GPT-4.1-nano, 1,000 synthetic employee records, 1,000 questions, 11 formats. Accuracy/tokens: Markdown key-value 60.7%/52,104; XML 56.0%/76,114; INI 55.7%/48,100; YAML 54.7%/55,395; HTML 53.6%/75,204; JSON 52.3%/66,396; Markdown table 51.9%/25,140; natural language 49.6%/43,411; JSONL 45.0%; CSV 44.3%/19,524; pipe-delimited 41.1%. Spread best-worst is 19.6 points; YAML vs JSON is +2.4 points on a single model and sample (no confidence interval seen).
- **Improving Agents, nested data** **[Cited, practitioner blog]** (SRC-AG-007): 1,000 questions per format per model; synthetic Terraform-like config; data sized to push accuracy to roughly 40-60%; substring-match grading.

  | Model | JSON | YAML | XML | Markdown |
  |---|---|---|---|---|
  | GPT-5 Nano | 50.3% | 62.1% | 44.4% | 54.3% |
  | Llama 3.2 3B | 52.7% | 49.1% | 50.7% | 48.0% |
  | Gemini 2.5 Flash Lite | 43.1% | 51.9% | 33.8% | 48.2% |

  Best format differs by model (YAML for two, JSON for Llama, whose spread of 4.7 points is small); XML is worst or near-worst on two of three. Notably the table-lookup post above ranks Markdown key-value first and the nested post ranks YAML first, so the two blog studies do not agree on a winner.
- **[Inference]** Across the retrieved studies: (a) spreads of roughly 5-20 points between formats are common on older or small models; (b) the more capable GPT-4-class model had smaller spreads in He et al.; (c) the rank order of formats flips across models, tasks and studies. Evidence that a *current frontier* coding model is sensitive to YAML vs Markdown vs JSON for knowledge records is absent from the retrieved set. EKS should measure this on its own models rather than import a ranking.

### 3.3 Token efficiency of formats

- **[Cited]** In the Improving Agents table test, CSV used 19,524 tokens and Markdown key-value 52,104 (about 2.7x) for the same data (SRC-AG-006). In the nested test Markdown used the fewest tokens on all three models (e.g. 38,357 vs JSON 57,933 for GPT-5 Nano) and XML the most (SRC-AG-007).
- **[Cited, snippet only]** An o200k_base comparison found CSV 300, minified JSON 525, YAML 649, XML 1,088 tokens on one dataset (SRC-AG-055). A separate GPT-5-tokenizer test ranked TSV, CSV, Markdown table, minified JSON, YAML, XML from cheapest to costliest and reported Llama 3.2, Mistral and Qwen3 gave nearly the same ranking (SRC-AG-057). A dev.to test using Anthropic's `count_tokens` reported averages of 3,252 (JSON), 2,208 (YAML), 1,514 (Markdown) tokens (SRC-AG-056). These are blog-level, small-sample, and I could not verify them.
- **[Inference]** Token ordering looks consistent across tokenizers (verbose delimiters and closing tags cost tokens), so XML/JSON carry a 1.3-2x token tax over Markdown/YAML for the same content, but efficiency and accuracy do not move together (CSV is cheapest and among the least accurate in SRC-AG-006).

## 4. Context compaction

- **[Cited]** Claude API compaction/context editing: tool-result clearing (default trigger 100,000 input tokens, keeps 3 most recent tool uses), thinking-block clearing, and a memory tool so the model can save content before clearing; client SDK compaction summarizes at a threshold, in one example shrinking about 100k tokens to 2-3k. Clearing invalidates prompt-cache prefixes (SRC-AG-014).
- **[Cited, vendor-measured]** Context editing alone improved an internal agentic-search evaluation by 29%, with the memory tool 39%, and cut tokens 84% in a 100-turn task; no methodology (SRC-AG-015).
- **[Cited]** Claude Code re-attaches the latest invocation of each skill after a summary, keeping the first 5,000 tokens of each within a combined 25,000-token budget (SRC-AG-017).
- **[Inference]** Compaction is lossy and happens mid-session; knowledge that must survive should live in files the agent can re-read, which is what instruction files and EKS compiled context already are.

## 5. How major coding agents consume repository knowledge (official docs)

### 5.1 Claude Code (SRC-AG-016, SRC-AG-017, SRC-AG-018)
- **[Cited]** CLAUDE.md files (user, project, org scopes) load at session start; docs say Claude "treats them as context, not enforced configuration" and recommend a PreToolUse hook to block actions deterministically. Target under 200 lines per file; longer files "consume more context and reduce adherence". Contradictory instructions may be resolved arbitrarily.
- **[Cited]** `@path` imports expand into context (max depth four) and "don't reduce its context cost". Subdirectory CLAUDE.md files load on demand when Claude reads files there. `.claude/rules/*.md` can be scoped with a `paths` YAML front-matter field.
- **[Cited]** AGENTS.md is readable directly as project instructions (v2.1.277+), or via `@AGENTS.md` import from CLAUDE.md. Auto memory is notes Claude writes itself, the first 200 lines or 25KB loaded each session.
- **[Cited]** Skills: a directory with SKILL.md, YAML front matter (all fields optional, `description` recommended, truncated at 1,536 characters in the listing); only descriptions load at startup and the body loads on invocation; keep SKILL.md under 500 lines; skills follow the open Agent Skills standard. Malformed YAML does not stop loading: the skill loads with no fields set.
- **[Cited]** Subagents get their own context window and, for most, the CLAUDE.md hierarchy; the built-in Explore and Plan subagents skip CLAUDE.md; custom subagents can have persistent memory directories.
- **[Cited, secondary]** Retrieval is by grep/glob and file reads rather than an embedding index (SRC-AG-031, summarizing public statements; not confirmed against a primary Anthropic source in this session). Anthropic's context-engineering post independently describes just-in-time loading via file paths and tools such as grep-style exploration as an approach (SRC-AG-012).

### 5.2 OpenAI Codex (SRC-AG-019)
- **[Cited]** Builds an instruction chain once per run: global `~/.codex` (AGENTS.override.md or AGENTS.md), then each directory from project root down to the cwd, at most one file per directory, concatenated root to leaf so closer files appear later and override. Stops adding files at `project_doc_max_bytes` (default 32 KiB).

### 5.3 GitHub Copilot (SRC-AG-021, SRC-AG-022)
- **[Cited]** `.github/copilot-instructions.md` (repo-wide, natural-language Markdown); `.github/instructions/*.instructions.md` with `applyTo` glob front matter; AGENTS.md (nearest file wins) or a root CLAUDE.md/GEMINI.md. Cloud-agent onboarding suggests keeping instructions to no more than 2 pages and not task-specific. Personal, repository, and organization instructions are all supplied, so conflicts should be avoided.
- **[Cited]** Copilot builds a semantic code search index of repositories (first index of a large repo up to about 60 s; later updates typically within seconds) used by Chat and the cloud agent.

### 5.4 Cursor (SRC-AG-023, SRC-AG-024)
- **[Cited]** Project rules are `.mdc` files in `.cursor/rules` with front matter (`description`, `globs`, `alwaysApply`); a plain `.md` there is ignored. Four application modes: always, intelligent (by description), file-glob, manual. AGENTS.md is supported as a plain Markdown alternative. Docs: keep rules under 500 lines, reference files rather than copying them. `@file` mentions are read by tools, not inlined.
- **[Cited]** The indexing docs retrieved describe a local "Instant Grep" index supporting regex, and an explore subagent with its own context returning summaries. They state codebase embeddings are not stored for that feature; I did not retrieve the page describing Cursor's older embedding-based semantic search.

### 5.5 Gemini CLI (SRC-AG-025)
- **[Cited]** GEMINI.md files load in three layers (global, workspace/parents, just-in-time on tool access) and are concatenated into each prompt; `@file.md` imports; `context.fileName` can be set to a list such as AGENTS.md, CONTEXT.md, GEMINI.md. The page notes Gemini CLI was replaced by Antigravity CLI on June 18, 2026, so check before relying on it.

### 5.6 AGENTS.md convention (SRC-AG-020)
- **[Cited]** "A README for agents": plain Markdown, no required fields, any headings; nested files with the closest taking precedence; site claims over 60,000 open-source projects and lists many supporting tools; stewarded by the Agentic AI Foundation under the Linux Foundation.

### 5.7 Cross-agent synthesis
- **[Inference]** Convergent design: Markdown body, optional YAML front matter used only for routing (globs/paths/description), hierarchical discovery by directory, concatenation into context, size caps (32 KiB Codex; 200 lines Claude guidance; 500 lines Cursor; 2 pages Copilot), and tool-based lookup for the rest. Front matter serves as routing metadata, not as data the model is expected to reason over. EKS can therefore deliver compiled context as Markdown files at paths these agents already discover, with typed structure retained upstream.

## 6. Independent evidence on instruction files

- **Gloaguen et al., "Evaluating AGENTS.md" (ETH Zurich, arXiv Feb 2026)** **[Cited]** (SRC-AG-026; HTML version and search summaries). Benchmarks: SWE-bench Lite (300 tasks, 11 Python repos, none with developer context files) and AGENTbench (138 instances, 12 repos, all with developer-written files). Agents: Claude Code (Sonnet-4.5), Codex (GPT-5.2, GPT-5.1 mini), Qwen Code (Qwen3-30B-coder). Results:
  - LLM-generated context files: -0.5% success on SWE-bench Lite and -2% on AGENTbench; cost +20% and +23%; steps +2.45 and +3.92.
  - Developer-written files: about +4% on average (improving all agents except Claude Code), steps +3.34, cost up to +19%.
  - GPT-5.2-generated ablation: +2% (Lite), -3% (AGENTbench). Removing repo documentation made LLM-generated files help by 2.7% on average (suggesting redundancy with existing docs).
  - Agents followed instructions in the files; repository overviews were not helpful; authors suggest human-written files describe minimal requirements and non-standard practices. Per-agent figures are only in a figure I could not read; Python-heavy benchmarks limit generalization.
- **Chatlatanagulchai et al., "Agent READMEs"** **[Cited]** (SRC-AG-027): 2,303 context files from 1,925 repos; content is mostly functional (test procedures in 75.9%, implementation details 70.8%, architecture 68.1%); security in 14.8% and performance in 14.5%; files change like config code through frequent small additions and are hard to read. Descriptive, not an effect on success.
- **Galster et al., configuring agentic coding tools** (SRC-AG-028; snippet only): reports context files dominate adoption versus skills/subagents/hooks/MCP; I could not confirm percentages.
- **Anthropic** **[Cited]**: its own guidance (SRC-AG-016) says shorter and more specific instructions are followed more consistently; this is guidance, not a measurement.
- **[Inference]** The ETH result is about generic repository overviews and generated summaries. It does not test curated, task-specific knowledge such as the specific requirement and ADR a task depends on; EKS's claim is different (targeted, typed, compiled). That makes the ETH result a warning about volume and redundancy rather than evidence against EKS, but also removes any prior that more knowledge helps.

## 7. Cost of teaching custom formats

- **[Cited] No direct measurement found.** I found no retrieved study measuring accuracy or token cost of teaching a coding agent a new knowledge-file format via instructions.
- **[Cited, snippet/abstract only]** Rare DSLs are hard to learn from a handful of in-context examples: grammar prompting notes few-shot generation of DSL strings "remains challenging" (SRC-AG-053); a semantic-parsing study reports learning rare DSLs from few demonstrations is challenging and recommends designing DSLs to resemble popular programming languages (SRC-AG-054). Both are about generating DSL output, not reading structured input.
- **[Cited]** Fixed token costs of in-context specification are real: tool definitions of 55K+ tokens in a 58-tool setup (SRC-AG-013); tool-use examples raised complex-parameter accuracy from 72% to 90% but add tokens (same source); always-loaded instructions raise cost about 20% in SRC-AG-026 (though that cost came from extra exploration, not only prompt length).
- **[Cited]** Agents natively handle Markdown, YAML front matter (skills, rules, Copilot `applyTo`), JSON and XML tags (SRC-AG-016, SRC-AG-017, SRC-AG-021, SRC-AG-023, SRC-AG-052). Front matter malformed YAML silently drops metadata in Claude Code skills (SRC-AG-017), a concrete failure mode of structure.
- **[Inference]** A format that is a strict subset of Markdown+YAML front matter (known to the models from pretraining and from agent docs) should have near-zero teaching cost; a bespoke syntax would require in-prompt specification or examples each session, and the DSL literature suggests that is error-prone. This is untested for reading (as opposed to writing) structured records.

## 8. Gaps

- No current-generation (2025-2026) model evaluation of Markdown vs YAML vs JSON vs XML for knowledge-record retrieval/reasoning was found beyond small practitioner blogs.
- No measurement of format-teaching cost.
- ETH AGENTS.md study: per-agent numbers unread; secondary summaries used for context.
- Several format-token claims are snippet-level only.

## Implications for EKS (falsifiable hypotheses)

H-C1. With a current frontier coding model, delivering the same compiled knowledge as Markdown-with-YAML-front-matter versus a stricter typed serialization (JSON or XML-tagged) yields task-success differences within a pre-registered equivalence margin (for example +/-3 points); falsified if the difference exceeds the margin on the EKS task set. (Basis: SRC-AG-003 shows GPT-4-class spreads of 0.6-8.8 points on most benchmarks but large outliers; SRC-AG-007 shows model-dependent flips.)
H-C2. The ranking of formats is not stable across models: for at least one of the evaluated models the best format differs from the best for another model. Falsified if one format is best (within noise) on every model and task. (Basis: SRC-AG-003, SRC-AG-007.)
H-C3. For the same record content, XML-tagged and JSON serializations use at least 1.3x the tokens of Markdown/YAML on the target tokenizers, with no accuracy gain that offsets it; falsified if token ratios are below 1.3x or accuracy gain is significant. (Basis: SRC-AG-006, SRC-AG-007, SRC-AG-055, SRC-AG-057 [snippet].)
H-C4. A task-specific compiled context of the records a task needs outperforms the same agent given a generic repository overview file of equal or greater length, in task success and in cost per success; falsified otherwise. (Basis: SRC-AG-026 found overviews unhelpful; SRC-AG-002 focused prompts beat full.)
H-C5. Hand-curated, minimal, task-relevant context files improve success relative to no context file by a measurable margin on EKS tasks that depend on non-obvious project decisions (not on generic code navigation); falsified if the margin is not distinguishable from zero. (Basis: SRC-AG-026 developer-written +4%.)
H-C6. Compiled context delivered through agent-native mechanisms (an AGENTS.md/CLAUDE.md or skill at a discoverable path, plain Markdown plus front matter) achieves at least the success of a bespoke-format arm that needs an in-prompt format explanation, at lower token cost; falsified if the bespoke-format arm wins after counting the explanation tokens. (Basis: SRC-AG-053, SRC-AG-054 on DSL difficulty; SRC-AG-013 on fixed spec cost; [Inference] on reading vs writing.)
H-C7. Success degrades as compiled-context length grows with distractor records at fixed required records; falsified if success is flat across a 5x increase in distractor tokens for the target models. (Basis: SRC-AG-002, SRC-AG-010, SRC-AG-011.)
H-C8. Instruction-file content that is redundant with repository documentation increases cost without increasing success; falsified if redundant-content arms match non-redundant arms on cost per success. (Basis: SRC-AG-026.)
