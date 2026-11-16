namespace Echelon.KnowledgeStore

open System
open System.Text.RegularExpressions

/// A retrievable unit of text (whole document or heading-delimited section).
type Chunk =
    {
        Key: string
        Heading: string
        Text: string
    }

/// Okapi BM25 over chunks. Pure: the index is an immutable value.
type Bm25Index =
    {
        Chunks: Chunk array
        Terms: Map<string, int> array
        DocFreq: Map<string, int>
        AvgLength: float
    }

[<RequireQualifiedAccess>]
module Retrieval =

    let private tokenPattern =
        Regex(@"[A-Za-z0-9][A-Za-z0-9\-]*", RegexOptions.Compiled)

    let private stop =
        set
            [
                "the"
                "a"
                "an"
                "and"
                "or"
                "of"
                "to"
                "in"
                "is"
                "for"
                "on"
                "with"
                "by"
                "be"
                "as"
                "it"
                "that"
                "this"
                "are"
                "from"
                "at"
                "which"
                "what"
                "all"
            ]

    /// Lower-cased terms; identifier-shaped tokens are kept whole.
    let terms (text: string) =
        tokenPattern.Matches text
        |> Seq.map (fun m -> m.Value.ToLowerInvariant().Trim('-'))
        |> Seq.filter (fun t -> t.Length > 1 && not (stop.Contains t))
        |> List.ofSeq

    /// Splits a Markdown body into sections at `#`–`###` headings.
    let sections (key: string) (title: string) (body: string) : Chunk list =
        let lines = body.Split('\n')

        let flush heading acc chunks =
            match acc with
            | [] -> chunks
            | _ ->
                {
                    Key = key
                    Heading = heading
                    Text = String.Join("\n", List.rev acc)
                }
                :: chunks

        let heading, acc, chunks =
            lines
            |> Array.fold
                (fun (heading, acc, chunks) (line: string) ->
                    if Regex.IsMatch(line, @"^#{1,3} ") then
                        (line.TrimStart('#').Trim(), [ line ], flush heading acc chunks)
                    else
                        (heading, line :: acc, chunks))
                (title, [], [])

        flush heading acc chunks
        |> List.rev
        |> List.filter (fun c -> not (String.IsNullOrWhiteSpace c.Text))

    let build (chunks: Chunk list) : Bm25Index =
        let arr = chunks |> Array.ofList

        let tfs =
            arr
            |> Array.map (fun c -> terms (c.Heading + "\n" + c.Text) |> List.countBy id |> Map.ofList)

        let df =
            tfs
            |> Array.collect (Map.keys >> Array.ofSeq)
            |> Array.countBy id
            |> Map.ofArray

        let avg =
            if arr.Length = 0 then
                1.0
            else
                tfs |> Array.averageBy (fun m -> m |> Map.values |> Seq.sum |> float)

        {
            Chunks = arr
            Terms = tfs
            DocFreq = df
            AvgLength = max avg 1.0
        }

    /// Top `k` chunks by BM25 score (k1 = 1.2, b = 0.75), with scores.
    let search (index: Bm25Index) (k: int) (query: string) : (Chunk * float) list =
        let n = float index.Chunks.Length
        let qs = terms query |> List.distinct

        index.Terms
        |> Array.mapi (fun i tf ->
            let len = tf |> Map.values |> Seq.sum |> float

            let score =
                qs
                |> List.sumBy (fun q ->
                    match Map.tryFind q tf, Map.tryFind q index.DocFreq with
                    | Some f, Some d ->
                        let idf = Math.Log(1.0 + (n - float d + 0.5) / (float d + 0.5))
                        let f = float f
                        idf * f * 2.2 / (f + 1.2 * (0.25 + 0.75 * len / index.AvgLength))
                    | _ -> 0.0)

            index.Chunks[i], score)
        |> Array.filter (snd >> (<) 0.0)
        |> Array.sortByDescending snd
        |> Array.truncate k
        |> List.ofArray

    /// Exact-substring search (grep-like) over chunks.
    let grep (chunks: Chunk list) (needle: string) =
        chunks
        |> List.filter (fun c -> c.Text.Contains(needle, StringComparison.Ordinal))
