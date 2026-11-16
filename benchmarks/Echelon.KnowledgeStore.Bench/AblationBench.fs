module Echelon.KnowledgeStore.Bench.AblationBench

open System
open System.Diagnostics
open System.Text.RegularExpressions
open Echelon.KnowledgeStore
open Echelon.KnowledgeStore.Bench.Corpus

type Ablation =
    {
        Variant: string
        TaskKind: string
        Tasks: int
        Recall: float
        Precision: float
        MeanTokens: float
    }

let private prf (gold: Set<string>) (found: Set<string>) =
    let tp = Set.intersect gold found |> Set.count |> float
    let r = if gold.IsEmpty then 1.0 else tp / float gold.Count

    let p =
        if found.IsEmpty then
            (if gold.IsEmpty then 1.0 else 0.0)
        else
            tp / float found.Count

    r, p

let private evidence (g: KnowledgeGraph) k =
    Graph.incoming g k
    |> List.filter (fun e -> e.Kind = Supports || e.Kind = Contradicts)
    |> List.map _.Source
    |> Set.ofList

/// Removes inverse (`supporting_evidence` / `contradicting_evidence`) relations.
let private withoutInverse (records: KnowledgeRecord list) =
    records
    |> List.map (fun r ->
        { r with
            Extra = r.Extra |> List.filter (fun (k, _) -> not (List.contains k Codec.inverseFields))
        })

/// Evaluates feature ablations against the full typed system's answers.
let run (count: string -> int) (records: KnowledgeRecord list) : Ablation list =
    let full = Graph.build false records
    let noInverse = Graph.build false (withoutInverse records)
    let withMentions = Graph.build true records
    let keys = full.Records |> Map.keys |> List.ofSeq |> List.sort
    let card g k = count (Context.card g k)

    let body k =
        count (Context.card full k + "\n\n" + full.Records[k].Body)

    let evidenceTasks = keys |> List.filter (fun k -> not (evidence full k).IsEmpty)

    let impactTasks =
        keys |> List.filter (fun k -> (Graph.impactOf full 6 k).Count >= 2)

    let measure
        variant
        kind
        tasks
        (gold: string -> Set<string>)
        (answer: string -> Set<string>)
        (cost: Set<string> -> int)
        =
        let rs =
            tasks
            |> List.map (fun t ->
                let found = answer t
                let r, p = prf (gold t) found
                r, p, cost found)

        {
            Variant = variant
            TaskKind = kind
            Tasks = List.length tasks
            Recall = rs |> List.averageBy (fun (r, _, _) -> r)
            Precision = rs |> List.averageBy (fun (_, p, _) -> p)
            MeanTokens = rs |> List.averageBy (fun (_, _, c) -> float c)
        }

    let cards g = Seq.sumBy (card g)

    let impact g d k =
        Graph.impactOf g d k |> Map.keys |> Set.ofSeq

    let untyped =
        // All relation kinds (including related_documents) treated as dependencies, both directions.
        fun k -> Graph.neighbourhood full 6 k |> Set.remove k

    [
        measure "full typed system" "find-evidence" evidenceTasks (evidence full) (evidence full) (cards full)
        measure
            "− inverse links (record's own links only)"
            "find-evidence"
            evidenceTasks
            (evidence full)
            (evidence noInverse)
            (cards noInverse)
        measure
            "− cards (full bodies delivered)"
            "find-evidence"
            evidenceTasks
            (evidence full)
            (evidence full)
            (Seq.sumBy body)
        measure "full typed system" "change-impact" impactTasks (impact full 6) (impact full 6) (cards full)
        measure
            "− transitive traversal (1 hop)"
            "change-impact"
            impactTasks
            (impact full 6)
            (impact full 1)
            (cards full)
        measure "− inverse links" "change-impact" impactTasks (impact full 6) (impact noInverse 6) (cards noInverse)
        measure
            "− relation types (untyped neighbourhood, 6 hops)"
            "change-impact"
            impactTasks
            (impact full 6)
            untyped
            (cards full)
        measure
            "+ prose mentions as edges"
            "change-impact"
            impactTasks
            (impact full 6)
            (fun k -> Graph.impactWith (fun _ -> true) withMentions 6 k |> Map.keys |> Set.ofSeq)
            (cards withMentions)
        measure
            "− cards (full bodies delivered)"
            "change-impact"
            impactTasks
            (impact full 6)
            (impact full 6)
            (Seq.sumBy body)
    ]

type StrictSummary =
    {
        Kind: string
        Records: int
        Representable: int
        Reasons: (string * int) list
    }

/// How much of the real corpus fits the strict per-kind types.
let strict (records: KnowledgeRecord list) : StrictSummary list =
    let g = Graph.build false records

    records
    |> List.map (fun r -> r, Strict.classify (Strict.evidenceFor g (Graph.key r)) r)
    |> List.groupBy (fst >> _.Kind >> KnowledgeKind.name)
    |> List.map (fun (k, xs) ->
        {
            Kind = k
            Records = xs.Length
            Representable = xs |> List.filter (snd >> Result.isOk) |> List.length
            Reasons =
                xs
                |> List.collect (fun (_, res) ->
                    match res with
                    | Error es -> es
                    | Ok _ -> [])
                |> List.countBy id
                |> List.sortByDescending snd
        })
    |> List.sortByDescending _.Records

type DriftRow =
    {
        Repository: string
        Document: string
        Family: string
        Listed: int
        Actual: int
        Missing: int
        Unknown: int
    }

let private indexLike =
    Regex(@"(?i)(index|decisions|decision-log|registry|catalog|current-state|traceability)[^/]*\.md$")

/// Hand-maintained index drift: non-record Markdown files whose name marks them
/// as an index (INDEX, DECISIONS, decision log, registry, catalog, current state,
/// traceability) and that enumerate ≥3 ids of one id family, compared with the
/// records of that family that exist.
let drift (files: SourceFile list) (records: KnowledgeRecord list) : DriftRow list =
    let family (id: string) =
        Regex.Replace(id, @"-[A-Z]?\d{2,4}[A-Z]?$", "")

    let recordPaths =
        records
        |> List.map (fun r -> r.Provenance.Repository, r.Provenance.Path)
        |> Set.ofList

    let actual =
        records
        |> List.groupBy (fun r -> r.Provenance.Repository, family r.Id)
        |> List.map (fun (k, rs) -> k, rs |> List.map _.Id |> Set.ofList)
        |> Map.ofList

    files
    |> List.filter (fun f -> not (recordPaths.Contains(f.Repository, f.Path)) && indexLike.IsMatch f.Path)
    |> List.collect (fun f ->
        KnowledgeRecord.idPattern.Matches f.Text
        |> Seq.map _.Value
        |> Seq.distinct
        |> Seq.groupBy family
        |> Seq.choose (fun (fam, ids) ->
            let listed = Set.ofSeq ids

            match Map.tryFind (f.Repository, fam) actual with
            | Some act when listed.Count >= 3 && act.Count >= 3 ->
                Some
                    {
                        Repository = f.Repository
                        Document = f.Path
                        Family = fam
                        Listed = listed.Count
                        Actual = act.Count
                        Missing = Set.difference act listed |> Set.count
                        Unknown = Set.difference listed act |> Set.count
                    }
            | _ -> None)
        |> List.ofSeq)

type CompilationResult =
    {
        Records: int
        IndexTokens: int
        CompileMilliseconds: float
        Deterministic: bool
        ChangedLinesForOneStatusEdit: int
    }

/// Compiles a generated index view; checks determinism and diff locality.
let compilation (count: string -> int) (records: KnowledgeRecord list) : CompilationResult =
    let render rs =
        let g = Graph.build false rs

        rs
        |> List.sortBy Graph.key
        |> List.map (Graph.key >> Context.card g)
        |> String.concat "\n\n"

    let sw = Stopwatch.StartNew()
    let a = render records
    sw.Stop()
    let b = render (List.rev records)

    let edited =
        records
        |> List.mapi (fun i r -> if i = 0 then { r with Status = Some "deprecated" } else r)
        |> render

    let changed =
        Seq.zip (a.Split('\n')) (edited.Split('\n'))
        |> Seq.filter (fun (x, y) -> x <> y)
        |> Seq.length

    {
        Records = records.Length
        IndexTokens = count a
        CompileMilliseconds = sw.Elapsed.TotalMilliseconds
        Deterministic = (a = b)
        ChangedLinesForOneStatusEdit = changed
    }
