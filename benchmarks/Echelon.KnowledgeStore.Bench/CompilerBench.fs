module Echelon.KnowledgeStore.Bench.CompilerBench

open System
open System.Diagnostics
open Echelon.KnowledgeStore
open Echelon.KnowledgeStore.Compiler

let config =
    {
        AsOf = DateOnly(2026, 11, 16)
        StaleAfterDays = 180
        Visibility = Corpus.privateRepositories |> Seq.map (fun r -> r, Private) |> Map.ofSeq
        Audience = Private
        SupportedSchema = 1
        ExistingPaths = None
        DisabledRules = Set.empty
        Revisions = Map.empty
    }

let sources (files: Corpus.SourceFile list) =
    files |> List.map (fun f -> Pipeline.sourceFile f.Repository f.Path f.Text)

let private median (xs: float list) =
    xs |> List.sort |> fun s -> s[s.Length / 2]

let private time f =
    let sw = Stopwatch.StartNew()
    let r = f ()
    sw.Stop()
    r, sw.Elapsed.TotalMilliseconds

let private outputsHash (c: Compilation) =
    Artifacts.outputs c
    |> List.map (fun (n, t) -> n + ":" + Pipeline.sha256 t)
    |> String.concat "|"

type Timing =
    {
        Scenario: string
        Files: int
        MedianMs: float
        Runs: int
        OutputsEqualCold: bool option
    }

/// Cold vs warm (incremental) compilation, and incremental correctness.
let timings (files: SourceFile list) : Timing list =
    let runs = 5
    let cold () = Pipeline.compile config Map.empty files
    // JIT warm-up so that timings measure steady-state compilation.
    cold () |> ignore
    let coldResults = List.init runs (fun _ -> time cold)
    let (c0, cache), _ = coldResults.Head

    let warm cache fs = Pipeline.compile config cache fs

    let edit n (fs: SourceFile list) =
        fs
        |> List.mapi (fun i f ->
            if i % (max 1 (fs.Length / max 1 n)) = 0 && i / (max 1 (fs.Length / max 1 n)) < n then
                Pipeline.sourceFile f.Repository f.Path (f.Text + "\n<!-- edit -->\n")
            else
                f)

    let scenario name (fs: SourceFile list) =
        let warmRuns = List.init runs (fun _ -> time (fun () -> warm cache fs))
        let coldRef, _ = time (fun () -> Pipeline.compile config Map.empty fs)
        let (cw, _), _ = warmRuns.Head

        {
            Scenario = name
            Files = fs.Length
            MedianMs = warmRuns |> List.map snd |> median
            Runs = runs
            OutputsEqualCold = Some(outputsHash cw = outputsHash (fst coldRef))
        }

    let parseOnly =
        List.init runs (fun _ -> time (fun () -> files |> List.map (Pipeline.parse config) |> List.length))
        |> List.map snd
        |> median

    [
        {
            Scenario = "cold (parse + graph + validate + precompute)"
            Files = files.Length
            MedianMs = coldResults |> List.map snd |> median
            Runs = runs
            OutputsEqualCold = None
        }
        {
            Scenario = "parse only (all files)"
            Files = files.Length
            MedianMs = parseOnly
            Runs = runs
            OutputsEqualCold = None
        }
        scenario "warm, no change" files
        scenario "warm, 1 file changed" (edit 1 files)
        scenario "warm, 10% of files changed" (edit (files.Length / 10) files)
    ]

type MutationResult =
    {
        Mutation: string
        Record: string
        Result: string
        Codes: string
    }

/// The 25 agent-edit mistakes used by the adversarial review, applied in memory.
let mutations (files: SourceFile list) : MutationResult list =
    let find (repo: string) (id: string) =
        files
        |> List.find (fun f -> f.Repository = repo && (f.Path.Split('/') |> Array.last).StartsWith(id + "--"))

    let h = find "praxis" "HY-FE-2026-B006"
    let d = find "praxis" "DF-ROS-2026-A031"
    let fm = find "forma" "HY-FORMA-2026-0002"
    let r (a: string) (b: string) (t: string) = t.Replace(a, b)

    let cases: (SourceFile * string * (string -> string)) list =
        [
            h,
            "field-name typo supporting_evidence->supporting_evidences",
            r "supporting_evidence:" "supporting_evidences:"
            h, "status typo proposed->propsed", r "status: proposed" "status: propsed"
            h, "status set to supported (1 supports, 2 contradict)", r "status: proposed" "status: supported"
            h, "confidence invalid label (hgih)", r "confidence: very-low" "confidence: hgih"
            h, "confidence numeric 0.8", r "confidence: very-low" "confidence: 0.8"
            h, "confidence raised very-low->high, no new evidence", r "confidence: very-low" "confidence: high"
            h,
            "evidence id typo B003->B030",
            r "supporting_evidence: [EV-FE-2026-B003]" "supporting_evidence: [EV-FE-2026-B030]"
            h,
            "evidence id swapped to real but wrong record",
            r "supporting_evidence: [EV-FE-2026-B003]" "supporting_evidence: [EV-ROS-2026-A043]"
            h,
            "evidence id lower-cased",
            r "supporting_evidence: [EV-FE-2026-B003]" "supporting_evidence: [ev-fe-2026-b003]"
            h, "duplicate status key appended", r "confidence: very-low" "confidence: very-low\nstatus: supported"
            h,
            "unquoted colon in title",
            r "title: Framework Engineering integrated" "title: Framework Engineering: integrated"
            h, "id field deleted", r "id: HY-FE-2026-B006\n" ""
            h,
            "contradicting evidence removed from list only",
            r "contradicting_evidence: [EV-FE-2026-B004, EV-FE-2026-B005]" "contradicting_evidence: [EV-FE-2026-B004]"
            h, "relation stated in prose only", (fun t -> t + "\n\nThis is also supported by EV-ROS-2026-A043.\n")
            d,
            "superseded_by cleared, status left superseded",
            r "superseded_by:\n  - DF-ROS-2026-A032" "superseded_by: []"
            d, "status reverted to accepted, superseded_by kept", r "status: superseded" "status: accepted"
            d, "superseded_by -> nonexistent A099", r "  - DF-ROS-2026-A032" "  - DF-ROS-2026-A099"
            d, "superseded_by -> self", r "  - DF-ROS-2026-A032" "  - DF-ROS-2026-A031"
            d,
            "list item indentation broken",
            r "superseded_by:\n  - DF-ROS-2026-A032" "superseded_by:\n- DF-ROS-2026-A032\n   - DF-ROS-2026-A033"
            d,
            "superseded_by written as scalar (benign)",
            r "superseded_by:\n  - DF-ROS-2026-A032" "superseded_by: DF-ROS-2026-A032"
            d, "superseded_by key mis-cased", r "superseded_by:" "Superseded_By:"
            fm, "derived_from typo -> derive_from", r "derived_from:" "derive_from:"
            fm, "derived_from -> dependsOn", r "derived_from:" "dependsOn:"
            fm, "status rejected->refuted", r "status: rejected" "status: refuted"
            fm,
            "contradicting evidence moved to supporting",
            r
                "supporting_evidence: []\ncontradicting_evidence: [EX-FORMA-2026-0001]"
                "supporting_evidence: [EX-FORMA-2026-0001]\ncontradicting_evidence: []"
        ]

    let baseC, cache = Pipeline.compile config Map.empty files

    let sig' (d: Diagnostic) =
        d.Code, d.Record, d.Message, d.Location |> Option.map (fun l -> l.Repository + "/" + l.Path)

    let baseSet = baseC.Diagnostics |> List.map sig' |> Set.ofList

    let run (target: SourceFile) name (mutate: string -> string) (extra: SourceFile list) =
        let text = mutate target.Text

        let changed =
            if text = target.Text && extra.IsEmpty then
                "UNCHANGED (mutation did not apply)"
            else
                ""

        let fs =
            (files
             |> List.map (fun f ->
                 if f = target then
                     Pipeline.sourceFile f.Repository f.Path text
                 else
                     f))
            @ extra

        let c, _ = Pipeline.compile config cache fs
        let fresh = c.Diagnostics |> List.filter (fun d -> not (baseSet.Contains(sig' d)))
        let gate = fresh |> List.filter (fun d -> d.Level = Level.Error)

        let flagged =
            fresh
            |> List.filter (fun d -> d.Level = Level.Warning || d.Level = Level.RequiresReview)

        {
            Mutation = name
            Record = $"{target.Repository}/{target.Path.Split('/') |> Array.last}"
            Result =
                if changed <> "" then changed
                elif not gate.IsEmpty then "BLOCKING"
                elif not flagged.IsEmpty then "FLAGGED"
                elif not fresh.IsEmpty then "ADVISORY-ONLY"
                else "UNDETECTED"
            Codes = fresh |> List.map _.Code |> List.distinct |> List.sort |> String.concat ", "
        }

    (cases |> List.map (fun (f, n, m) -> run f n m []))
    @ [
        run
            h
            "copy with id unchanged (duplicate id)"
            id
            [ Pipeline.sourceFile h.Repository (h.Path.Replace(".md", "-copy.md")) h.Text ]
    ]

type OptimisationResult =
    {
        Packages: int
        Complete: int
        LedgerEntries: int
        Unaccounted: int
        TargetLost: int
        MeanTokens: float
        MeanCompileMs: float
    }

/// Builds a task package for every record; checks the no-silent-loss invariant.
let optimisation (count: string -> int) (c: Compilation) : OptimisationResult =
    let keys = c.Graph.Records |> Map.keys |> List.ofSeq

    let results =
        keys
        |> List.map (fun k ->
            let p, ms = time (fun () -> Packages.task c k)
            let unacc = p.Ledger |> List.length |> ignore |> fun () -> 0
            p, ms, unacc)

    let lost (p: Package) =
        // Every pre-optimisation item must be in the output or the ledger (checked inside
        // Packages.task); here: the target must always be present.
        if p.Items |> List.exists (fun i -> i.Key = p.Target && i.Section = "Target") then
            0
        else
            1

    let incompleteFromLoss (p: Package) =
        match p.Status with
        | Incomplete rs when rs |> List.exists (fun r -> r.StartsWith "optimisation lost") -> 1
        | _ -> 0

    {
        Packages = results.Length
        Complete = results |> List.filter (fun (p, _, _) -> p.Status = Complete) |> List.length
        LedgerEntries = results |> List.sumBy (fun (p, _, _) -> p.Ledger.Length)
        Unaccounted = results |> List.sumBy (fun (p, _, _) -> incompleteFromLoss p)
        TargetLost = results |> List.sumBy (fun (p, _, _) -> lost p)
        MeanTokens = results |> List.averageBy (fun (p, _, _) -> float (count p.Text))
        MeanCompileMs = results |> List.averageBy (fun (_, ms, _) -> ms)
    }

type LosslessResult =
    {
        Records: int
        IdenticalFrontMatter: int
        DifferingFields: (string * int) list
    }

/// Source-level losslessness: original front-matter tree vs the tree regenerated
/// from the record (canonical Markdown), field by field.
let lossless (files: SourceFile list) (c: Compilation) : LosslessResult =
    let fmOf (text: string) =
        match FrontMatter.split text with
        | Ok { FrontMatter = Some(YMap kvs) } -> Some(kvs |> Map.ofList)
        | _ -> None

    let rename = Map.ofList [ "author_agent", "author"; "created_by_agent", "author" ]

    let pairs =
        c.Units
        |> List.choose (fun u ->
            match u.Record, fmOf u.File.Text with
            | Some r, Some original when not (r.Extra |> List.exists (fst >> (=) "id_source")) ->
                let regenerated = fmOf (Codec.toMarkdown true r) |> Option.defaultValue Map.empty
                Some(original, regenerated)
            | _ -> None)

    let norm (y: Yaml) =
        match y with
        | YScalar s -> YScalar(s.Trim())
        | YSeq [] -> YNull
        | other -> other

    let diffs (original: Map<string, Yaml>, regenerated: Map<string, Yaml>) =
        original
        |> Map.toList
        |> List.filter (fun (k, v) ->
            let k' = rename |> Map.tryFind k |> Option.defaultValue k

            match Map.tryFind k' regenerated with
            | Some v' -> norm v <> norm v'
            | None -> norm v <> YNull)
        |> List.map fst

    let all = pairs |> List.map diffs

    {
        Records = pairs.Length
        IdenticalFrontMatter = all |> List.filter List.isEmpty |> List.length
        DifferingFields = all |> List.concat |> List.countBy id |> List.sortByDescending snd
    }

type ResolutionStats =
    {
        Relations: int
        Resolved: int
        Unresolved: int
        UnresolvedIdLike: int
        UnresolvedPaths: int
        UnresolvedOther: int
    }

let resolution (c: Compilation) : ResolutionStats =
    let total = c.Graph.Edges.Length + c.Graph.Unresolved.Length

    let un =
        c.Graph.Unresolved |> List.map snd |> List.filter (fun r -> r.Kind <> Mentions)

    let isPath (t: string) = t.Contains "/" || t.EndsWith ".md"

    let idLike =
        un
        |> List.filter (fun r -> not (isPath r.Target) && KnowledgeRecord.idPattern.IsMatch r.Target)
        |> List.length

    let paths = un |> List.filter (fun r -> isPath r.Target) |> List.length

    {
        Relations = total
        Resolved = c.Graph.Edges.Length
        Unresolved = un.Length
        UnresolvedIdLike = idLike
        UnresolvedPaths = paths
        UnresolvedOther = un.Length - idLike - paths
    }
