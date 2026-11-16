module Echelon.KnowledgeStore.Bench.DependencyBench

open System
open System.IO
open System.Text.Json
open System.Text.RegularExpressions
open Echelon.KnowledgeStore

/// A repository-level dependency `from → to`.
type Pair = string * string

/// Independent gold: edges verified by the audit workstream (baseline/cross-repo-edges.json).
let gold (path: string) (hardOnly: bool) : Set<Pair> =
    use doc = JsonDocument.Parse(File.ReadAllText path)

    doc.RootElement.GetProperty("edges").EnumerateArray()
    |> Seq.map (fun e ->
        e.GetProperty("from_repo").GetString() |> string,
        e.GetProperty("to_repo").GetString() |> string,
        e.GetProperty("kind").GetString() |> string)
    |> Seq.filter (fun (_, _, k) ->
        not hardOnly
        || List.contains k [ "depends-on"; "vendors"; "validates"; "implements" ])
    |> Seq.map (fun (f, t, _) -> f, t)
    |> Seq.filter (fun (f, t) -> f <> t)
    |> Set.ofSeq

/// Names by which each repository is referred to in prose.
let aliases =
    [
        "aegis", @"\bAegis\b"
        "arca", @"\bArca\b"
        "clarity-framework", @"\bClarity\b"
        "dokimos", @"\bDokimos\b"
        "echelon-diagnostic-framework", @"\bEDF\b|Echelon Diagnostic Framework"
        "echelon-engineering-system", @"Echelon Engineering System"
        "echelon-organization-administration", @"echelon-organization-administration"
        "fides", @"\bFides\b"
        "forma", @"\bForma\b"
        "mercatus", @"\bMercatus\b"
        "percepta", @"\bPercepta\b"
        "praxis", @"\bPraxis\b"
        "signal", @"Echelon Signal|\bSignal\b(?= (?:app|application|repository|consumes|uses))"
    ]
    |> List.map (fun (r, p) -> r, Regex(p, RegexOptions.Compiled))

/// Machine-readable dependency declarations: package ids, repository URLs, Praxis install manifests.
let private manifestPatterns (repo: string) =
    let pascal =
        repo.Split('-')
        |> Array.map (fun s -> string (Char.ToUpperInvariant s[0]) + s.Substring 1)
        |> String.concat ""

    [
        $@"EchelonFoundry\.{pascal}\b"
        $@"@echelon-foundry/{repo}\b"
        $@"kemiller2002/{repo}\b"
    ]
    |> List.map (fun p -> Regex(p, RegexOptions.Compiled ||| RegexOptions.IgnoreCase))

let private isManifest (p: string) =
    let name = Path.GetFileName p |> string

    name.EndsWith ".fsproj"
    || name = "package.json"
    || name = "ros.json"
    || name = "conditor.json"
    || (p.Contains "/.github/workflows/"
        && (name.EndsWith ".yml" || name.EndsWith ".yaml"))
    || name = "Directory.Packages.props"
    || name = "NuGet.config"

let private files (root: string) (repo: string) (pred: string -> bool) =
    let dir = Path.Combine(root, repo)

    if Directory.Exists dir then
        Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
        |> Seq.map (fun f -> f.Replace('\\', '/'))
        |> Seq.filter (fun f ->
            not (
                f.Contains "/.git/"
                || f.Contains "/node_modules/"
                || f.Contains "/bin/"
                || f.Contains "/obj/"
            ))
        |> Seq.filter pred
        |> List.ofSeq
    else
        []

/// M1: cross-repository edges in the typed front-matter graph.
let typedGraph (records: KnowledgeRecord list) : Set<Pair> =
    let g = Graph.build false records
    let repoOf (k: string) = k.Split(':')[0]

    g.Edges
    |> List.map (fun e -> repoOf e.Source, repoOf e.Target)
    |> List.filter (fun (a, b) -> a <> b)
    |> Set.ofList

/// M2: prose keyword search (names of other repositories in Markdown).
let keyword (root: string) (repos: string list) : Set<Pair> =
    repos
    |> List.collect (fun from ->
        let text =
            files root from (fun f -> f.EndsWith ".md" && not (f.Contains "/vendor/"))
            |> List.map File.ReadAllText

        aliases
        |> List.filter (fun (r, _) -> r <> from && List.contains r repos)
        |> List.filter (fun (_, rx) -> text |> List.exists rx.IsMatch)
        |> List.map (fun (r, _) -> from, r))
    |> Set.ofList

/// M3: machine-readable manifests (package references, workflow uses, Praxis install).
let manifests (root: string) (repos: string list) : Set<Pair> =
    repos
    |> List.collect (fun from ->
        let ms = files root from isManifest

        let declared =
            repos
            |> List.filter ((<>) from)
            |> List.filter (fun r ->
                let pats = manifestPatterns r

                ms
                |> List.exists (fun m -> let t = File.ReadAllText m in pats |> List.exists (fun p -> p.IsMatch t)))
            |> List.map (fun r -> from, r)

        let praxis =
            if from <> "praxis" && File.Exists(Path.Combine(root, from, "ros.json")) then
                [ from, "praxis" ]
            else
                []

        declared @ praxis)
    |> Set.ofList

type Score =
    {
        Method: string
        Gold: string
        Scenario: string
        GoldPairs: int
        Predicted: int
        TruePositives: int
        Precision: float
        Recall: float
    }

let score (name: string) (goldName: string) (scenario: string) (predicted: Set<Pair>) (goldSet: Set<Pair>) =
    let tp = Set.intersect predicted goldSet |> Set.count

    {
        Method = name
        Gold = goldName
        Scenario = scenario
        GoldPairs = goldSet.Count
        Predicted = predicted.Count
        TruePositives = tp
        Precision =
            if predicted.IsEmpty then
                0.0
            else
                float tp / float predicted.Count
        Recall =
            if goldSet.IsEmpty then
                0.0
            else
                float tp / float goldSet.Count
    }
