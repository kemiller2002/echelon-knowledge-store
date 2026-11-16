module Echelon.KnowledgeStore.Bench.Corpus

open System
open System.IO
open Echelon.KnowledgeStore

/// A Markdown file read from an audited repository.
type SourceFile =
    {
        Repository: string
        Path: string
        Text: string
        Parsed: Result<MarkdownDocument, string>
    }

let private excluded (p: string) =
    [
        "/.git/"
        "/node_modules/"
        "/bin/"
        "/obj/"
        "/vendor/"
        "/archive/"
        "/tests/"
        "/fixtures/"
        "/templates/"
        "/starter/"
    ]
    |> List.exists (fun x -> p.Replace('\\', '/').Contains x)

/// Every `.md` file under each repository directory (vendored copies excluded
/// unless `includeVendor`).
let load (includeVendor: bool) (root: string) (repositories: string list) : SourceFile list =
    repositories
    |> List.collect (fun repo ->
        let dir = Path.Combine(root, repo)

        if not (Directory.Exists dir) then
            []
        else
            Directory.EnumerateFiles(dir, "*.md", SearchOption.AllDirectories)
            |> Seq.map (fun f -> Path.GetRelativePath(dir, f).Replace('\\', '/'))
            |> Seq.filter (fun p ->
                let q = "/" + p

                not (
                    q.Contains "/.git/"
                    || q.Contains "/node_modules/"
                    || q.Contains "/bin/"
                    || q.Contains "/obj/"
                )
                && (includeVendor || not (excluded q)))
            |> Seq.sort
            |> Seq.map (fun p ->
                let text = File.ReadAllText(Path.Combine(dir, p))

                {
                    Repository = repo
                    Path = p
                    Text = text
                    Parsed = FrontMatter.split text
                })
            |> List.ofSeq)

let private fileId =
    System.Text.RegularExpressions.Regex(
        @"^([A-Z][A-Z0-9]{1,9}(?:-[A-Z0-9]{1,12}){0,4}-[A-Z]?\d{2,4}[A-Z]?)(?:--.*)?\.md$"
    )

/// Records whose identity comes only from the file name (`ID--slug.md`) because
/// the file has no front-matter id. They carry no typed metadata.
let filenameRecord (f: SourceFile) : KnowledgeRecord option =
    let m = fileId.Match(f.Path.Split('/') |> Array.last)

    // Files whose front matter cannot be parsed still exist: they are kept as
    // filename-identified records (and reported by the compiler as EKS0011)
    // instead of being dropped silently.
    let parsed =
        match f.Parsed with
        | Ok doc -> Some doc
        | Error _ ->
            Some
                {
                    FrontMatter = None
                    FrontMatterText = None
                    Body = f.Text
                }

    match parsed with
    | Some doc when m.Success ->
        let title =
            doc.Body.Split('\n')
            |> Array.tryFind (fun l -> l.StartsWith "# ")
            |> Option.map (fun l -> l.Substring(2).Trim())
            |> Option.defaultValue ""

        Some
            {
                Id = m.Groups[1].Value
                Kind = KnowledgeKind.infer f.Path m.Groups[1].Value
                Title = title
                Status = None
                Confidence = Unstated
                Relations = []
                Provenance =
                    {
                        Repository = f.Repository
                        Path = f.Path
                        Author = None
                        SourceUri = None
                        Created = None
                        Updated = None
                        Retrieved = None
                    }
                Body = doc.Body.Trim('\n').TrimEnd()
                Extra = [ "id_source", YScalar "filename" ]
            }
    | _ -> None

/// Records with a front-matter id (typed metadata).
let records (files: SourceFile list) : KnowledgeRecord list =
    files
    |> List.choose (fun f ->
        f.Parsed
        |> Result.toOption
        |> Option.bind (KnowledgeRecord.ofDocument f.Repository f.Path >> Result.toOption))

/// Front-matter records plus filename-identified records (the resolvable universe).
let allRecords (files: SourceFile list) : KnowledgeRecord list =
    let typed = records files

    let typedPaths =
        typed
        |> List.map (fun r -> r.Provenance.Repository, r.Provenance.Path)
        |> Set.ofList

    typed
    @ (files
       |> List.filter (fun f -> not (typedPaths.Contains(f.Repository, f.Path)))
       |> List.choose filenameRecord)

let defaultRepositories =
    [
        "aegis"
        "arca"
        "clarity-framework"
        "dokimos"
        "echelon-diagnostic-framework"
        "echelon-engineering-system"
        "echelon-organization-administration"
        "fides"
        "forma"
        "mercatus"
        "percepta"
        "praxis"
        "signal"
    ]

/// Repositories that are private on GitHub (2026-11-16). Their content must not be
/// written verbatim into this public repository; only aggregates are reported.
let privateRepositories =
    set [ "clarity-framework"; "echelon-organization-administration" ]
