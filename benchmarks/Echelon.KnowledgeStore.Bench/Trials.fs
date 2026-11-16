module Echelon.KnowledgeStore.Bench.Trials

open System
open System.IO
open System.Text.Json
open Echelon.KnowledgeStore
open Echelon.KnowledgeStore.Bench.Corpus

/// An end-to-end agent question with a deterministic gold answer.
type Question =
    {
        Id: string
        Kind: string
        Batch: int
        Text: string
        Target: string option
        Gold: string list
        GoldText: string option
    }

let private bare (k: string) = k.Split(':') |> Array.last

let private evidenceOf (g: KnowledgeGraph) k =
    Graph.incoming g k
    |> List.filter (fun e -> e.Kind = Supports || e.Kind = Contradicts)
    |> List.map (_.Source >> bare)
    |> List.distinct
    |> List.sort

let private current (g: KnowledgeGraph) (start: string) =
    let rec go seen k =
        match
            Graph.outgoing g k
            |> List.filter (fun x -> x.Kind = SupersededBy)
            |> List.map _.Target
        with
        | n :: _ when not (Set.contains n seen) -> go (Set.add n seen) n
        | _ ->
            match
                Graph.incoming g k
                |> List.filter (fun x -> x.Kind = Supersedes)
                |> List.map _.Source
            with
            | n :: _ when not (Set.contains n seen) -> go (Set.add n seen) n
            | _ -> k

    go (Set.singleton start) start

let private unsupportedHypotheses (g: KnowledgeGraph) (repo: string) =
    g.Records
    |> Map.filter (fun k r -> r.Provenance.Repository = repo && r.Kind = Hypothesis)
    |> Map.keys
    |> Seq.filter (fun k -> Graph.incoming g k |> List.forall (fun e -> e.Kind <> Supports))
    |> Seq.map bare
    |> Seq.sort
    |> List.ofSeq

/// The fixed question set (16 questions, two batches). Gold derives from the
/// authored typed metadata of the public corpus at the audited commits.
let questions (g: KnowledgeGraph) : Question list =
    let rec_ k = Map.find k g.Records

    let evidence i batch k =
        {
            Id = $"Q{i:D2}"
            Kind = "find-evidence"
            Batch = batch
            Text =
                $"In the {(rec_ k).Provenance.Repository} repository, list every record that provides supporting OR contradicting evidence for {bare k}. Include evidence declared in either direction (in {bare k}'s own metadata or in the evidence record's metadata). Answer with record ids."
            Target = Some k
            Gold = evidenceOf g k
            GoldText = None
        }

    let impact i batch k =
        {
            Id = $"Q{i:D2}"
            Kind = "change-impact"
            Batch = batch
            Text =
                $"In the {(rec_ k).Provenance.Repository} repository, record {bare k} is about to change. List every record that must be reviewed as a consequence, transitively: records that depend on or are derived from a changed record, and records whose claims a changed evidence record supports or contradicts. Use only typed metadata relations (depends_on, derived_from, supports, contradicts, supporting_evidence, contradicting_evidence), not related_documents or prose mentions. Answer with record ids."
            Target = Some k
            Gold = Graph.impactOf g 6 k |> Map.keys |> Seq.map bare |> Seq.sort |> List.ofSeq
            GoldText = None
        }

    let currentQ i batch k =
        {
            Id = $"Q{i:D2}"
            Kind = "current-authority"
            Batch = batch
            Text =
                $"In the {(rec_ k).Provenance.Repository} repository, {bare k} may have been superseded. Which record is the current authoritative replacement at the end of its supersession chain? If it is not superseded, answer {bare k}. Answer with one record id."
            Target = Some k
            Gold = [ bare (current g k) ]
            GoldText = None
        }

    let lookup i batch k (text: string) =
        {
            Id = $"Q{i:D2}"
            Kind = "lookup-by-description"
            Batch = batch
            Text = text + " Answer with one record id."
            Target = Some k
            Gold = [ bare k ]
            GoldText = None
        }

    let epistemic i batch k =
        let r = rec_ k
        let kinds = Graph.incoming g k |> List.map _.Kind
        let contested = List.contains Supports kinds && List.contains Contradicts kinds

        let status = defaultArg r.Status String.Empty
        let contestedText = if contested then "yes" else "no"

        {
            Id = $"Q{i:D2}"
            Kind = "epistemic-status"
            Batch = batch
            Text =
                $"In the {r.Provenance.Repository} repository, what are the recorded status and confidence of {bare k}, and is it contested (has at least one supporting AND at least one contradicting evidence record)? Answer as the string \"status=<status>; confidence=<confidence>; contested=<yes|no>\"."
            Target = Some k
            Gold = []
            GoldText = Some $"status={status}; confidence={Confidence.name r.Confidence}; contested={contestedText}"
        }

    let missing i batch repo =
        {
            Id = $"Q{i:D2}"
            Kind = "missing-evidence"
            Batch = batch
            Text =
                $"In the {repo} repository, list every hypothesis record (id prefix HY-) that has NO supporting evidence record (no evidence declares it supports the hypothesis, and the hypothesis lists no supporting_evidence). Answer with record ids; answer an empty list if there are none."
            Target = None
            Gold = unsupportedHypotheses g repo
            GoldText = None
        }

    [
        evidence 1 1 "praxis:HY-FE-2026-B006"
        impact 2 2 "praxis:DF-ROS-2026-A047"
        currentQ 3 1 "praxis:DF-ROS-2026-A031"
        lookup
            4
            2
            "fides:DF-FIDES-2026-0003"
            "In the fides repository, which decision record settled what kind of GitHub integration (an app versus an OAuth client) the sign-in uses, and how its tokens expire?"
        epistemic 5 1 "praxis:HY-FE-2026-B006"
        missing 6 2 "percepta"
        evidence 7 1 "praxis:DF-ROS-2026-A028"
        impact 8 2 "forma:EX-FORMA-2026-0001"
        evidence 9 2 "praxis:HY-ROS-2026-A028"
        currentQ 10 2 "fides:DF-FIDES-2026-0013"
        lookup
            11
            1
            "arca:DF-ARCA-2026-0012"
            "In the arca repository, which decision record explains how records that are otherwise immutable can still be erased, and what Git history limitation remains?"
        impact 12 1 "praxis:EX-ROS-2026-A021"
        evidence 13 2 "praxis:DF-ROS-2026-A027"
        lookup
            14
            1
            "signal:DF-SIGNAL-2026-0016"
            "In the signal repository, which decision record covers running the survey page on Safari-on-iPhone and Firefox outside the per-pull-request checks?"
        epistemic 15 2 "forma:HY-FORMA-2026-0002"
        missing 16 1 "forma"
    ]

let private taskKind (q: Question) =
    match q.Kind with
    | "change-impact" -> Some ImpactAnalysis
    | "current-authority" -> Some DecisionHistory
    | "find-evidence"
    | "epistemic-status" -> Some EvidenceReview
    | _ -> None

/// Compiled context for a question, produced without access to the gold answer:
/// the target is resolved from the question text (explicit id, else BM25).
let compiled (count: string -> int) (g: KnowledgeGraph) (index: Bm25Index) (q: Question) =
    let explicitId =
        KnowledgeRecord.idPattern.Matches q.Text
        |> Seq.map _.Value
        |> Seq.tryPick (fun id -> g.Records |> Map.tryFindKey (fun _ r -> r.Id = id))

    let repoOf (text: string) =
        defaultRepositories
        |> List.tryFind (fun r -> text.Contains $"the {r} repository")

    match q.Kind, explicitId with
    | "missing-evidence", _ ->
        let repo = repoOf q.Text |> Option.defaultValue ""

        let hyps =
            g.Records
            |> Map.filter (fun _ r -> r.Provenance.Repository = repo && r.Kind = Hypothesis)
            |> Map.keys
            |> List.ofSeq

        $"# Context: hypotheses in {repo} with their evidence links\n\n"
        + (hyps |> List.map (Context.card g) |> String.concat "\n\n")
    | _, Some k ->
        match taskKind q with
        | Some task -> (Context.compile count 12_000 g task k).Text
        | None -> Context.card g k
    | _, None ->
        let repo = repoOf q.Text

        let seeds =
            Retrieval.search index 50 q.Text
            |> List.map (fun (c, _) -> c.Key)
            |> List.distinct
            |> List.filter (fun k -> repo |> Option.forall (fun r -> k.StartsWith(r + ":")))
            |> List.truncate 5

        "# Context: candidate records (ranked by relevance)\n\n"
        + (seeds |> List.map (Context.card g) |> String.concat "\n\n")

/// Writes the materials for each condition and the question/gold files.
let prepare (count: string -> int) (files: SourceFile list) (records: KnowledgeRecord list) (outDir: string) =
    let g = Graph.build false records

    let index =
        records
        |> List.collect (fun r -> Retrieval.sections (Graph.key r) r.Title r.Body)
        |> Retrieval.build

    let qs = questions g

    let write (path: string) (text: string) =
        Directory.CreateDirectory(Path.GetDirectoryName path |> string) |> ignore
        File.WriteAllText(path, text)

    for f in files do
        write (Path.Combine(outDir, "md", "corpus", f.Repository, f.Path)) f.Text
        write (Path.Combine(outDir, "md-index", "corpus", f.Repository, f.Path)) f.Text
        // Plain Markdown: front matter removed, so relations survive only in prose.
        let plain =
            match f.Parsed with
            | Ok d -> d.Body
            | Error _ -> f.Text

        write (Path.Combine(outDir, "plain", "corpus", f.Repository, f.Path)) plain

    let cards =
        records
        |> List.sortBy Graph.key
        |> List.map (Graph.key >> Context.card g)
        |> String.concat "\n\n"

    write
        (Path.Combine(outDir, "md-index", "corpus", "INDEX.md"))
        ("# Record index (generated): one card per record, with derived inverse links\n\n"
         + cards
         + "\n")

    let core = records |> List.map Codec.core
    write (Path.Combine(outDir, "jsonl", "corpus", "records.jsonl")) (Codec.toJsonl core + "\n")
    write (Path.Combine(outDir, "dsl", "corpus", "records.eks")) (Dsl.write core + "\n")

    for q in qs do
        write (Path.Combine(outDir, "compiled", "corpus", $"{q.Id}.md")) (compiled count g index q + "\n")

    let opts = JsonSerializerOptions(WriteIndented = true)

    for batch in [ 1; 2 ] do
        let text =
            qs
            |> List.filter (fun q -> q.Batch = batch)
            |> List.map (fun q -> $"{q.Id} [{q.Kind}]: {q.Text}")
            |> String.concat "\n\n"

        write (Path.Combine(outDir, $"questions-batch{batch}.md")) (text + "\n")

    write (Path.Combine(outDir, "gold.json")) (JsonSerializer.Serialize(qs, opts))
    qs

type QuestionScore =
    {
        Condition: string
        Question: string
        Kind: string
        Score: float
        Exact: bool
    }

let private norm (s: string) =
    s.Trim().Trim('`', '"').ToLowerInvariant()

/// Scores one answer: set F1 for id lists, exact match for single ids, and the
/// fraction of the three fields correct for epistemic strings.
let scoreAnswer (q: Question) (answer: JsonElement option) : float * bool =
    match answer, q.GoldText with
    | None, _ -> 0.0, false
    | Some a, Some gold ->
        let text =
            match a.ValueKind with
            | JsonValueKind.String -> a.GetString() |> string
            | _ -> a.GetRawText()

        let parts (s: string) =
            s.Split(';') |> Array.map norm |> Array.filter ((<>) "") |> Set.ofArray

        let g = parts gold
        let got = parts text
        let hit = Set.intersect g got |> Set.count |> float
        hit / float g.Count, hit = float g.Count
    | Some a, None ->
        let ids =
            match a.ValueKind with
            | JsonValueKind.Array -> a.EnumerateArray() |> Seq.map (fun e -> e.ToString()) |> List.ofSeq
            | JsonValueKind.String -> [ a.GetString() |> string ]
            | _ -> []
            |> List.map (fun s -> (norm s).Split(':') |> Array.last)
            |> Set.ofList

        let gold = q.Gold |> List.map norm |> Set.ofList

        if gold.IsEmpty && ids.IsEmpty then
            1.0, true
        else
            let tp = Set.intersect gold ids |> Set.count |> float
            let p = if ids.IsEmpty then 0.0 else tp / float ids.Count
            let r = if gold.IsEmpty then 0.0 else tp / float gold.Count
            let f1 = if p + r = 0.0 then 0.0 else 2.0 * p * r / (p + r)
            f1, (ids = gold)

/// Reads `answers/<condition>-b<batch>[-<run>].json` files and scores them.
let score (qs: Question list) (answersDir: string) : QuestionScore list =
    Directory.EnumerateFiles(answersDir, "*.json")
    |> Seq.sort
    |> Seq.collect (fun path ->
        let condition = Path.GetFileNameWithoutExtension path |> string
        use doc = JsonDocument.Parse(File.ReadAllText path)

        let lookup (id: string) =
            match doc.RootElement.TryGetProperty id with
            | true, v ->
                match v.ValueKind with
                | JsonValueKind.Object ->
                    match v.TryGetProperty "answer" with
                    | true, a -> Some(a.Clone())
                    | _ -> None
                | _ -> Some(v.Clone())
            | _ -> None

        let batch =
            if condition.Contains "-b1" then 1
            elif condition.Contains "-b2" then 2
            else 0

        qs
        |> List.filter (fun q -> q.Batch = batch)
        |> List.map (fun q ->
            let s, exact = scoreAnswer q (lookup q.Id)

            {
                Condition = condition
                Question = q.Id
                Kind = q.Kind
                Score = s
                Exact = exact
            }))
    |> List.ofSeq
