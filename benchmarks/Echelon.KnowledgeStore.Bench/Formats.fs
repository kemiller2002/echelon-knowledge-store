module Echelon.KnowledgeStore.Bench.Formats

open System
open Echelon.KnowledgeStore
open Echelon.KnowledgeStore.Bench.Corpus

/// A serialisation under test: writer, and reader for round-trip checking.
type Format =
    {
        Name: string
        Write: KnowledgeRecord list -> string
        Read: (string -> Result<KnowledgeRecord list, string>) option
        RoundTrip: (KnowledgeRecord list -> Result<KnowledgeRecord list, string>) option
    }

let formats: Format list =
    [
        {
            Name = "structured-markdown"
            Write = List.map (Codec.toMarkdown false) >> String.concat "\n"
            // One file per record: concatenated front-matter documents are ambiguous
            // because bodies may contain `---` thematic breaks.
            Read = None
            RoundTrip =
                Some(fun rs ->
                    rs
                    |> List.map (Codec.toMarkdown false >> Codec.ofMarkdown)
                    |> List.fold
                        (fun acc r ->
                            match acc, r with
                            | Ok xs, Ok x -> Ok(x :: xs)
                            | Error e, _
                            | _, Error e -> Error e)
                        (Ok [])
                    |> Result.map List.rev)
        }
        {
            Name = "json"
            Write = Codec.toJson
            Read = Some Codec.ofJson
            RoundTrip = None
        }
        {
            Name = "jsonl"
            Write = Codec.toJsonl
            Read = Some Codec.ofJsonl
            RoundTrip = None
        }
        {
            Name = "yaml"
            Write = Codec.toYamlText
            Read = Some Codec.ofYamlText
            RoundTrip = None
        }
        {
            Name = "fsharp-records"
            Write = FSharpSource.write
            Read = None
            RoundTrip = None
        }
        {
            Name = "dsl"
            Write = Dsl.write
            Read =
                Some(fun t ->
                    Dsl.read t
                    |> Result.mapError (fun ds ->
                        String.Join("; ", ds |> List.truncate 3 |> List.map (fun d -> $"line {d.Line}: {d.Message}"))))
            RoundTrip = None
        }
    ]

/// Plain Markdown: title + body, no metadata (relations survive only as prose).
let plainMarkdown (rs: KnowledgeRecord list) =
    rs |> List.map (fun r -> $"# {r.Title}\n\n{r.Body}\n") |> String.concat "\n"

let private metadataOnly (r: KnowledgeRecord) = { r with Body = "" }

type FormatCost =
    {
        Format: string
        View: string
        Tokens: Map<string, int64>
        Bytes: int64
    }

type RoundTrip =
    {
        Format: string
        Records: int
        Equal: int
        FirstMismatch: string
    }

/// Token cost of every format for the full records and for metadata only.
let costs (families: Tokens.Family list) (files: SourceFile list) (records: KnowledgeRecord list) =
    let core = records |> List.map Codec.core

    let byKey =
        files |> List.map (fun f -> (f.Repository, f.Path), f.Text) |> Map.ofList

    let measure name view (text: string) =
        {
            Format = name
            View = view
            Tokens = families |> List.map (fun f -> f.Name, int64 (f.Count text)) |> Map.ofList
            Bytes = int64 (Text.Encoding.UTF8.GetByteCount text)
        }

    let original =
        records
        |> List.map (fun r -> byKey[(r.Provenance.Repository, r.Provenance.Path)])
        |> String.concat "\n"

    let full =
        [
            measure "original-markdown" "full" original
            measure "plain-markdown" "full" (plainMarkdown core)
        ]
        @ (formats |> List.map (fun f -> measure f.Name "full" (f.Write core)))

    let meta =
        let m = core |> List.map metadataOnly
        formats |> List.map (fun f -> measure f.Name "metadata" (f.Write m))

    let cards =
        let g = Graph.build false records
        let text = records |> List.map (Graph.key >> Context.card g) |> String.concat "\n\n"
        [ measure "context-cards" "card" text ]

    full @ meta @ cards

/// Write → read equality per format (core view).
let roundTrips (records: KnowledgeRecord list) =
    let core = records |> List.map Codec.core

    formats
    |> List.choose (fun f ->
        match f.RoundTrip, f.Read with
        | Some rt, _ -> Some(f, rt)
        | None, Some read -> Some(f, (fun rs -> read (f.Write rs)))
        | None, None -> None)
    |> List.map (fun (f, roundTrip) ->
        match roundTrip core with
        | Error e ->
            {
                Format = f.Name
                Records = core.Length
                Equal = 0
                FirstMismatch = "read failed: " + e
            }
        | Ok back ->
            let pairs =
                List.zip (List.truncate back.Length core) (List.truncate core.Length back)

            let equal = pairs |> List.filter (fun (a, b) -> a = b) |> List.length

            let first =
                pairs
                |> List.tryFind (fun (a, b) -> a <> b)
                |> Option.map (fun (a, b) ->
                    let diffs =
                        [
                            if a.Id <> b.Id then
                                "id"
                            if a.Title <> b.Title then
                                "title"
                            if a.Status <> b.Status then
                                "status"
                            if a.Relations <> b.Relations then
                                "relations"
                            if a.Provenance <> b.Provenance then
                                "provenance"
                            if a.Body <> b.Body then
                                "body"
                            if a.Extra <> b.Extra then
                                "extra"
                            if a.Kind <> b.Kind then
                                "kind"
                            if a.Confidence <> b.Confidence then
                                "confidence"
                        ]

                    $"{a.Provenance.Repository}:{a.Id} differs in {String.Join(',', diffs)}")
                |> Option.defaultValue (
                    if back.Length <> core.Length then
                        $"count {back.Length} vs {core.Length}"
                    else
                        ""
                )

            {
                Format = f.Name
                Records = core.Length
                Equal = equal
                FirstMismatch = first
            })

/// Lossless migration check: original Markdown → record (all fields) →
/// canonical Markdown → record. Compares full records including extra fields.
let migration (records: KnowledgeRecord list) =
    let back = records |> List.map (Codec.toMarkdown true >> Codec.ofMarkdown)

    let normaliseExtra (r: KnowledgeRecord) =
        // Canonical form keeps extras; inverse lists in canonical core form.
        r

    let results =
        List.zip records back
        |> List.map (fun (a, b) ->
            match b with
            | Error e -> a, Some("parse: " + e)
            | Ok b ->
                let a' = normaliseExtra a

                let fields =
                    [
                        if a'.Title <> b.Title then
                            "title"
                        if a'.Status <> b.Status then
                            "status"
                        if a'.Relations <> b.Relations then
                            "relations"
                        if a'.Body <> b.Body then
                            "body"
                        if a'.Extra <> b.Extra then
                            "extra"
                        if a'.Provenance <> b.Provenance then
                            "provenance"
                        if a'.Kind <> b.Kind then
                            "kind"
                    ]

                a,
                (if fields.IsEmpty then
                     None
                 else
                     Some(String.Join(",", fields))))

    results
