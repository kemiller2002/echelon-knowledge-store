module Echelon.KnowledgeStore.Bench.Baseline

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.RegularExpressions
open Echelon.KnowledgeStore
open Echelon.KnowledgeStore.Bench.Corpus

let private linkPattern =
    Regex(@"\]\(([^)\s]+)(?:\s+""[^""]*"")?\)", RegexOptions.Compiled)

let private hash (s: string) =
    SHA256.HashData(Encoding.UTF8.GetBytes s) |> Convert.ToHexString

let private normalise (s: string) = Regex.Replace(s, @"\s+", " ").Trim()

/// Relative links and whether their target exists.
let links (root: string) (f: SourceFile) =
    linkPattern.Matches f.Text
    |> Seq.map _.Groups[1].Value
    |> Seq.filter (fun t ->
        not (
            t.StartsWith "http:"
            || t.StartsWith "https:"
            || t.StartsWith "#"
            || t.StartsWith "mailto:"
            || t.Contains "{"
        ))
    |> Seq.map (fun t ->
        let target = Uri.UnescapeDataString(t.Split('#')[0])

        let dir =
            Path.GetDirectoryName(Path.Combine(root, f.Repository, f.Path))
            |> Option.ofObj
            |> Option.defaultValue ""

        let full = Path.GetFullPath(Path.Combine(dir, target))
        t, File.Exists full || Directory.Exists full)
    |> List.ofSeq

let private isVendored (p: string) =
    let q = "/" + p
    q.Contains "/vendor/" || q.Contains "/archive/"

let private instructionFile (p: string) =
    List.contains
        p
        [
            "AGENTS.md"
            "CLAUDE.md"
            "GEMINI.md"
            ".github/copilot-instructions.md"
            "BOOTSTRAP.md"
            "HANDOFF.md"
        ]

type RepoBaseline =
    {
        Repository: string
        Files: int
        VendoredFiles: int
        Bytes: int64
        Tokens: Map<string, int64>
        AverageTokens: float
        WithFrontMatter: int
        WithId: int
        FrontMatterErrors: int
        RelativeLinks: int
        BrokenLinks: int
        InstructionTokens: int64
        MaxFileTokens: int
    }

type DuplicationBaseline =
    {
        DuplicateFileCopies: int
        DuplicateFileTokens: int64
        FilesWithCrossRepoCopies: int
        DuplicateParagraphCopies: int
        DuplicateParagraphTokens: int64
    }

let measure (root: string) (families: Tokens.Family list) (files: SourceFile list) =
    let primary = Tokens.primary families

    let tokenCache =
        files
        |> List.map (fun f -> (f.Repository, f.Path), primary.Count f.Text)
        |> Map.ofList

    let perRepo =
        files
        |> List.groupBy _.Repository
        |> List.map (fun (repo, fs) ->
            let ls = fs |> List.collect (links root)

            {
                Repository = repo
                Files = fs.Length
                VendoredFiles = fs |> List.filter (_.Path >> isVendored) |> List.length
                Bytes = fs |> List.sumBy (fun f -> int64 (Encoding.UTF8.GetByteCount f.Text))
                Tokens =
                    families
                    |> List.map (fun fam -> fam.Name, fs |> List.sumBy (fun f -> int64 (fam.Count f.Text)))
                    |> Map.ofList
                AverageTokens = fs |> List.averageBy (fun f -> float tokenCache[(f.Repository, f.Path)])
                WithFrontMatter =
                    fs
                    |> List.filter (fun f ->
                        match f.Parsed with
                        | Ok d -> d.FrontMatter.IsSome
                        | Error _ -> true)
                    |> List.length
                WithId =
                    fs
                    |> List.filter (fun f ->
                        match f.Parsed with
                        | Ok { FrontMatter = Some fm } -> (Yaml.field "id" fm).IsSome
                        | _ -> false)
                    |> List.length
                FrontMatterErrors = fs |> List.filter (_.Parsed >> Result.isError) |> List.length
                RelativeLinks = ls.Length
                BrokenLinks = ls |> List.filter (snd >> not) |> List.length
                InstructionTokens =
                    fs
                    |> List.filter (_.Path >> instructionFile)
                    |> List.sumBy (fun f -> int64 tokenCache[(f.Repository, f.Path)])
                MaxFileTokens = fs |> List.map (fun f -> tokenCache[(f.Repository, f.Path)]) |> List.max
            })

    let byHash =
        files
        |> List.groupBy (fun f -> hash (normalise f.Text))
        |> List.filter (fun (_, fs) -> fs.Length > 1)

    let paragraphs =
        files
        |> List.collect (fun f ->
            f.Text.Replace("\r\n", "\n").Split("\n\n")
            |> Array.map normalise
            |> Array.filter (fun p -> p.Length >= 200)
            |> Array.distinct
            |> Array.map (fun p -> p, f)
            |> List.ofArray)
        |> List.groupBy fst
        |> List.filter (fun (_, xs) -> xs.Length > 1)

    let duplication =
        {
            DuplicateFileCopies = byHash |> List.sumBy (fun (_, fs) -> fs.Length - 1)
            DuplicateFileTokens =
                byHash
                |> List.sumBy (fun (_, fs) ->
                    int64 (fs.Length - 1) * int64 tokenCache[(fs.Head.Repository, fs.Head.Path)])
            FilesWithCrossRepoCopies =
                byHash
                |> List.filter (fun (_, fs) -> (fs |> List.map _.Repository |> List.distinct).Length > 1)
                |> List.sumBy (fun (_, fs) -> fs.Length)
            DuplicateParagraphCopies = paragraphs |> List.sumBy (fun (_, xs) -> xs.Length - 1)
            DuplicateParagraphTokens =
                paragraphs
                |> List.sumBy (fun (p, xs) -> int64 (xs.Length - 1) * int64 (primary.Count p))
        }

    perRepo, duplication

let render (families: Tokens.Family list) (perRepo: RepoBaseline list) (dup: DuplicationBaseline) =
    let primary = (Tokens.primary families).Name
    let inline total f = perRepo |> List.sumBy f

    let rows =
        perRepo
        |> List.map (fun r ->
            [
                r.Repository
                string r.Files
                string r.VendoredFiles
                string r.Bytes
                string r.Tokens[primary]
                Report.f1 r.AverageTokens
                string r.MaxFileTokens
                Report.pct r.WithFrontMatter r.Files
                Report.pct r.WithId r.Files
                string r.FrontMatterErrors
                string r.RelativeLinks
                $"{r.BrokenLinks} ({Report.pct r.BrokenLinks r.RelativeLinks})"
                string r.InstructionTokens
            ])

    let totals =
        [
            "**total**"
            string (total _.Files)
            string (total _.VendoredFiles)
            string (total _.Bytes)
            string (total (fun r -> r.Tokens[primary]))
            Report.f1 (float (total (fun r -> r.Tokens[primary])) / float (total _.Files))
            string (perRepo |> List.map _.MaxFileTokens |> List.max)
            Report.pct (total _.WithFrontMatter) (total _.Files)
            Report.pct (total _.WithId) (total _.Files)
            string (total _.FrontMatterErrors)
            string (total _.RelativeLinks)
            $"{total _.BrokenLinks} ({Report.pct (total _.BrokenLinks) (total _.RelativeLinks)})"
            string (total _.InstructionTokens)
        ]

    let familyRows =
        families
        |> List.map (fun f -> [ f.Name; string (perRepo |> List.sumBy (fun r -> r.Tokens[f.Name])) ])

    String.concat
        "\n\n"
        [
            Report.table
                [
                    "Repository"
                    "MD files"
                    "vendored"
                    "bytes"
                    $"tokens ({primary})"
                    "avg tok/file"
                    "max tok/file"
                    "front matter"
                    "with id"
                    "FM parse errors"
                    "relative links"
                    "broken links"
                    "agent-instruction tokens"
                ]
                (rows @ [ totals ])
            Report.table [ "Tokenizer"; "Total tokens" ] familyRows
            Report.table
                [ "Duplication measure"; "Value" ]
                [
                    [ "Exact duplicate file copies (beyond first)"; string dup.DuplicateFileCopies ]
                    [ $"Tokens in duplicate copies ({primary})"; string dup.DuplicateFileTokens ]
                    [
                        "Files in duplicate sets spanning >1 repository"
                        string dup.FilesWithCrossRepoCopies
                    ]
                    [
                        "Duplicate paragraph copies (≥200 chars, beyond first)"
                        string dup.DuplicateParagraphCopies
                    ]
                    [
                        $"Tokens in duplicate paragraphs ({primary})"
                        string dup.DuplicateParagraphTokens
                    ]
                ]
        ]
