namespace Echelon.KnowledgeStore

open System
open System.Text
open System.Text.Json

/// A compact, non-executable, line-oriented record syntax (experimental):
///
///     hypothesis HY-FE-2026-B006
///       title "Integrated profile adds value"
///       status proposed
///       supporting_evidence EV-FE-2026-B003
///       text
///         Body lines, indented four spaces.
///
/// The reader is total: it returns diagnostics with line numbers and never
/// evaluates content.
[<RequireQualifiedAccess>]
module Dsl =

    type Diagnostic = { Line: int; Message: string }

    let private bare (s: string) =
        s.Length > 0
        && s.Length <= 200
        && s |> Seq.forall (fun c -> Char.IsAsciiLetterOrDigit c || "-_./:#".Contains c)
        && not (s.StartsWith "\"")

    let private token (s: string) =
        if bare s then s else JsonSerializer.Serialize s

    let private fieldLine (k: string) (vs: string list) =
        $"  {k} {String.Join(' ', vs |> List.map token)}"

    /// Record (core view) → DSL text.
    let ofRecord (r: KnowledgeRecord) : string =
        let p = r.Provenance

        let opt k =
            Option.map (fun v -> fieldLine k [ v ]) >> Option.toList

        let date (d: DateOnly) = d.ToString("yyyy-MM-dd")

        let rels =
            RelationKind.fields
            |> List.filter (fun (_, _, inv) -> not inv)
            |> List.distinctBy (fun (_, k, _) -> k)
            |> List.choose (fun (f, kind, _) ->
                r.Relations
                |> List.filter (fun x -> x.Kind = kind)
                |> List.map _.Target
                |> function
                    | [] -> None
                    | ts -> Some(fieldLine f ts))

        let inverse =
            r.Extra
            |> List.filter (fun (k, _) -> List.contains k Codec.inverseFields)
            |> List.choose (fun (k, v) ->
                match Yaml.strings v with
                | [] -> None
                | ts -> Some(fieldLine k ts))

        let body =
            if r.Body = "" then
                []
            else
                "  text"
                :: (r.Body.Split('\n')
                    |> Array.map (fun l -> if l = "" then "" else "    " + l)
                    |> List.ofArray)

        [ $"{KnowledgeKind.name r.Kind} {token r.Id}"; fieldLine "title" [ r.Title ] ]
        @ opt "status" r.Status
        @ opt "confidence" (Some(Confidence.name r.Confidence) |> Option.filter ((<>) ""))
        @ [ fieldLine "repository" [ p.Repository ]; fieldLine "path" [ p.Path ] ]
        @ opt "author" p.Author
        @ opt "source_uri" p.SourceUri
        @ opt "created" (p.Created |> Option.map date)
        @ opt "updated" (p.Updated |> Option.map date)
        @ opt "retrieved" (p.Retrieved |> Option.map date)
        @ rels
        @ inverse
        @ body
        |> String.concat "\n"

    let write (records: KnowledgeRecord list) =
        records |> List.map ofRecord |> String.concat "\n\n"

    /// Splits arguments: bare tokens or JSON-quoted strings.
    let tokenize (s: string) : Result<string list, string> =
        let rec go (i: int) acc =
            if i >= s.Length then
                Ok(List.rev acc)
            elif s[i] = ' ' then
                go (i + 1) acc
            elif s[i] = '"' then
                let rec close j escaped =
                    if j >= s.Length then None
                    elif escaped then close (j + 1) false
                    elif s[j] = '\\' then close (j + 1) true
                    elif s[j] = '"' then Some j
                    else close (j + 1) false

                match close (i + 1) false with
                | None -> Error "unterminated string"
                | Some j ->
                    try
                        match JsonSerializer.Deserialize<string>(s.Substring(i, j - i + 1)) with
                        | null -> Error "invalid string"
                        | v -> go (j + 1) (v :: acc)
                    with :? JsonException ->
                        Error "invalid string escape"
            else
                let j = s.IndexOf(' ', i)
                let j = if j < 0 then s.Length else j
                go j (s.Substring(i, j - i) :: acc)

        go 0 []

    type private Block =
        {
            Header: int * string
            Fields: (int * string) list
            Text: string list
        }

    /// Groups lines into record blocks.
    let private blocks (lines: string array) =
        lines
        |> Array.indexed
        |> Array.fold
            (fun (acc: Block list, inText: bool) (i, line) ->
                let n = i + 1

                match acc with
                | _ when line.Length > 0 && line[0] <> ' ' ->
                    ({
                        Header = n, line
                        Fields = []
                        Text = []
                     }
                     :: acc,
                     false)
                | b :: rest when inText && (line = "" || line.StartsWith "    ") ->
                    ({ b with
                        Text = (if line = "" then "" else line.Substring 4) :: b.Text
                     }
                     :: rest,
                     true)
                | b :: rest when line.TrimEnd() = "  text" -> (b :: rest, true)
                | b :: rest when line.StartsWith "  " ->
                    ({ b with
                        Fields = (n, line.Substring 2) :: b.Fields
                     }
                     :: rest,
                     false)
                | _ when line = "" -> (acc, inText)
                | [] ->
                    ({
                        Header = n, line
                        Fields = []
                        Text = []
                     }
                     :: acc,
                     false)
                | b :: rest ->
                    ({ b with
                        Fields = (n, line) :: b.Fields
                     }
                     :: rest,
                     false))
            ([], false)
        |> fst
        |> List.rev
        |> List.map (fun b ->
            { b with
                Fields = List.rev b.Fields
                Text = b.Text |> List.rev |> List.rev |> List.skipWhile ((=) "") |> List.rev
            })

    let private single =
        set
            [
                "title"
                "status"
                "confidence"
                "repository"
                "path"
                "author"
                "source_uri"
                "created"
                "updated"
                "retrieved"
            ]

    let private multi =
        RelationKind.fields |> List.map (fun (f, _, _) -> f) |> Set.ofList

    let private readBlock (b: Block) : Result<KnowledgeRecord, Diagnostic list> =
        let hn, header = b.Header

        let headerResult =
            match tokenize header with
            | Ok [ kind; id ] when kind |> Seq.forall (fun c -> Char.IsAsciiLetterLower c || c = '_' || c = '-') ->
                Ok(kind, id)
            | _ ->
                Error
                    [
                        {
                            Line = hn
                            Message = "expected '<kind> <id>'"
                        }
                    ]

        let fields =
            b.Fields
            |> List.map (fun (n, line) ->
                match tokenize line with
                | Ok(k :: [ v ]) when single.Contains k -> Ok(k, YScalar v)
                | Ok(k :: vs) when multi.Contains k && not vs.IsEmpty -> Ok(k, YSeq(vs |> List.map YScalar))
                | Ok(k :: _) ->
                    Error
                        {
                            Line = n
                            Message = $"unknown field or wrong arity: '{k}'"
                        }
                | Ok [] -> Error { Line = n; Message = "empty field" }
                | Error e -> Error { Line = n; Message = e })

        let errors =
            (match headerResult with
             | Error es -> es
             | Ok _ -> [])
            @ (fields
               |> List.choose (function
                   | Error d -> Some d
                   | Ok _ -> None))

        match headerResult, errors with
        | Ok(kind, id), [] ->
            let kvs = fields |> List.choose Result.toOption

            YMap(
                [ "id", YScalar id; "kind", YScalar kind ]
                @ kvs
                @ [ "body", YScalar(String.Join("\n", b.Text)) ]
            )
            |> Codec.ofYaml
            |> Result.mapError (fun e -> [ { Line = hn; Message = e } ])
        | _ -> Error errors

    /// DSL text → records, or every diagnostic found. Bounded input size.
    let read (text: string) : Result<KnowledgeRecord list, Diagnostic list> =
        if text.Length > 50_000_000 then
            Error
                [
                    {
                        Line = 0
                        Message = "input too large"
                    }
                ]
        else
            let results = text.Replace("\r\n", "\n").Split('\n') |> blocks |> List.map readBlock

            match
                results
                |> List.collect (function
                    | Error ds -> ds
                    | Ok _ -> [])
            with
            | [] -> Ok(results |> List.choose Result.toOption)
            | ds -> Error ds
