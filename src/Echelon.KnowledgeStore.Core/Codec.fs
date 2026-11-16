namespace Echelon.KnowledgeStore

open System
open System.Text
open System.Text.Json

/// The intermediate representation (IR) of the knowledge compiler: every record
/// maps to one inert `Yaml` tree, and every syntax (YAML, JSON, JSONL,
/// structured Markdown) is a printer/reader of that tree.
[<RequireQualifiedAccess>]
module Codec =

    let private kinds =
        [
            Requirement
            Decision
            Hypothesis
            Evidence
            Experiment
            Theory
            ResearchPackage
            Journal
            ResearchQuestion
            Governance
            Method
            Verification
            Document
        ]

    let parseKind (s: string) =
        kinds
        |> List.tryFind (fun k -> KnowledgeKind.name k = s)
        |> Option.defaultValue (Other s)

    let private date (d: DateOnly) = d.ToString("yyyy-MM-dd")

    let private parseDate (s: string) =
        match DateOnly.TryParseExact(s, "yyyy-MM-dd") with
        | true, d -> Some d
        | _ -> None

    /// Inverse evidence fields kept on the record (`supporting_evidence`, ...).
    let inverseFields =
        RelationKind.fields
        |> List.filter (fun (_, _, inv) -> inv)
        |> List.map (fun (f, _, _) -> f)

    /// The core view: the record without unrecognised extra fields, except the
    /// inverse evidence lists, which carry relations.
    let core (r: KnowledgeRecord) =
        { r with
            Extra =
                r.Extra
                |> List.filter (fun (k, _) -> List.contains k inverseFields)
                |> List.map (fun (k, v) -> k, Yaml.strings v)
                |> List.filter (snd >> List.isEmpty >> not)
                |> List.map (fun (k, vs) -> k, YSeq(vs |> List.map YScalar))
        }

    /// Record → IR. `includeExtra` keeps all unrecognised fields (lossless).
    let toYaml (includeExtra: bool) (r: KnowledgeRecord) : Yaml =
        let opt k v =
            v |> Option.map (fun s -> k, YScalar s) |> Option.toList

        let p = r.Provenance

        let relations =
            RelationKind.fields
            |> List.filter (fun (_, _, inv) -> not inv)
            |> List.map (fun (f, kind, _) -> f, kind)
            |> List.distinctBy snd
            |> List.choose (fun (field, kind) ->
                r.Relations
                |> List.filter (fun x -> x.Kind = kind)
                |> List.map (_.Target >> YScalar)
                |> function
                    | [] -> None
                    | xs -> Some(field, YSeq xs))

        let extra =
            r.Extra
            |> List.filter (fun (k, _) -> includeExtra || List.contains k inverseFields)

        YMap(
            [
                "id", YScalar r.Id
                "kind", YScalar(KnowledgeKind.name r.Kind)
                "title", YScalar r.Title
            ]
            @ opt "status" r.Status
            @ opt "confidence" (Some(Confidence.name r.Confidence) |> Option.filter ((<>) ""))
            @ [ "repository", YScalar p.Repository; "path", YScalar p.Path ]
            @ opt "author" p.Author
            @ opt "source_uri" p.SourceUri
            @ opt "created" (p.Created |> Option.map date)
            @ opt "updated" (p.Updated |> Option.map date)
            @ opt "retrieved" (p.Retrieved |> Option.map date)
            @ relations
            @ extra
            @ [ "body", YScalar r.Body ]
        )

    /// IR → record.
    let ofYaml (y: Yaml) : Result<KnowledgeRecord, string> =
        let str k = Yaml.field k y |> Option.bind Yaml.text

        let known =
            [
                "id"
                "kind"
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
                "body"
            ]
            @ (RelationKind.fields
               |> List.filter (fun (_, _, inv) -> not inv)
               |> List.map (fun (f, _, _) -> f))

        match y, str "id" with
        | YMap kvs, Some id ->
            let relations =
                kvs
                |> List.collect (fun (k, v) ->
                    RelationKind.fields
                    |> List.tryFind (fun (f, _, inv) -> f = k && not inv)
                    |> Option.map (fun (_, kind, _) ->
                        Yaml.strings v |> List.map (fun t -> { Kind = kind; Target = t }))
                    |> Option.defaultValue [])

            Ok
                {
                    Id = id
                    Kind = str "kind" |> Option.map parseKind |> Option.defaultValue Document
                    Title = str "title" |> Option.defaultValue ""
                    Status = str "status"
                    Confidence = Confidence.parse (str "confidence")
                    Relations = relations
                    Provenance =
                        {
                            Repository = str "repository" |> Option.defaultValue ""
                            Path = str "path" |> Option.defaultValue ""
                            Author = str "author"
                            SourceUri = str "source_uri"
                            Created = str "created" |> Option.bind parseDate
                            Updated = str "updated" |> Option.bind parseDate
                            Retrieved = str "retrieved" |> Option.bind parseDate
                        }
                    Body = str "body" |> Option.defaultValue ""
                    Extra = kvs |> List.filter (fun (k, _) -> not (List.contains k known))
                }
        | _ -> Error "record must be a mapping with an id"

    // ---------- JSON ----------

    let private jsonString (s: string) = JsonSerializer.Serialize s

    /// IR → JSON text. `indent = None` gives compact single-line JSON.
    let rec jsonOf (indent: int option) (depth: int) (y: Yaml) : string =
        let nl d =
            match indent with
            | Some n -> "\n" + String(' ', n * d)
            | None -> ""

        let sep =
            match indent with
            | Some _ -> ": "
            | None -> ":"

        match y with
        | YNull -> "null"
        | YScalar s -> jsonString s
        | YSeq [] -> "[]"
        | YMap [] -> "{}"
        | YSeq xs ->
            let items = xs |> List.map (fun x -> nl (depth + 1) + jsonOf indent (depth + 1) x)
            "[" + String.Join(",", items) + nl depth + "]"
        | YMap kvs ->
            let items =
                kvs
                |> List.map (fun (k, v) -> nl (depth + 1) + jsonString k + sep + jsonOf indent (depth + 1) v)

            "{" + String.Join(",", items) + nl depth + "}"

    let rec private ofJsonElement (e: JsonElement) : Yaml =
        match e.ValueKind with
        | JsonValueKind.Object ->
            e.EnumerateObject()
            |> Seq.map (fun p -> p.Name, ofJsonElement p.Value)
            |> List.ofSeq
            |> YMap
        | JsonValueKind.Array -> e.EnumerateArray() |> Seq.map ofJsonElement |> List.ofSeq |> YSeq
        | JsonValueKind.String -> YScalar(e.GetString() |> Option.ofObj |> Option.defaultValue "")
        | JsonValueKind.Null
        | JsonValueKind.Undefined -> YNull
        | _ -> YScalar(e.GetRawText())

    /// JSON text → IR. Data-only; depth limited.
    let yamlOfJson (text: string) : Result<Yaml, string> =
        try
            use doc = JsonDocument.Parse(text, JsonDocumentOptions(MaxDepth = 32))
            Ok(ofJsonElement doc.RootElement)
        with :? JsonException as ex ->
            Error ex.Message

    // ---------- YAML text ----------

    let private yamlKey (k: string) =
        if
            k |> Seq.forall (fun c -> Char.IsAsciiLetterOrDigit c || c = '_' || c = '-')
            && k.Length > 0
        then
            k
        else
            jsonString k

    let private blockable (s: string) =
        s.Contains '\n'
        && not (s.StartsWith " ")
        && not (s.EndsWith "\n")
        && not (s.EndsWith " ")
        && not (
            s.Split('\n')
            |> Array.exists (fun l -> l.Length > 0 && String.IsNullOrWhiteSpace l)
        )
        && not (s.Contains '\t')
        && not (s.Contains '\r')

    let private plainSafe (s: string) =
        s.Length > 0
        && s.Length < 120
        && s
           |> Seq.forall (fun c -> Char.IsAsciiLetterOrDigit c || c = '-' || c = '_' || c = '.' || c = '/')
        && Char.IsAsciiLetter s[0]
        && not (List.contains (s.ToLowerInvariant()) [ "true"; "false"; "yes"; "no"; "on"; "off"; "null" ])

    /// IR → block YAML text (data-only subset readable by `Yaml.parse`).
    let rec yamlText (depth: int) (y: Yaml) : string =
        let pad = String(' ', 2 * depth)

        let scalar (s: string) = if plainSafe s then s else jsonString s

        let inlineValue v =
            match v with
            | YNull -> Some "null"
            | YScalar s when blockable s -> None
            | YScalar s -> Some(scalar s)
            | YSeq [] -> Some "[]"
            | YMap [] -> Some "{}"
            | YSeq xs when
                xs
                |> List.forall (function
                    | YScalar s -> plainSafe s
                    | _ -> false)
                ->
                Some("[" + String.Join(", ", xs |> List.choose Yaml.text) + "]")
            | _ -> None

        match y with
        | YMap kvs ->
            kvs
            |> List.map (fun (k, v) ->
                match inlineValue v, v with
                | Some s, _ -> $"{pad}{yamlKey k}: {s}\n"
                | None, YScalar s ->
                    let ind = String(' ', 2 * (depth + 1))

                    $"{pad}{yamlKey k}: |2-\n"
                    + String.Join("\n", s.Split('\n') |> Array.map (fun l -> if l = "" then "" else ind + l))
                    + "\n"
                | None, other -> $"{pad}{yamlKey k}:\n" + yamlText (depth + 1) other)
            |> String.concat ""
        | YSeq xs ->
            xs
            |> List.map (fun v ->
                match inlineValue v, v with
                | Some s, _ -> $"{pad}- {s}\n"
                | None, other -> $"{pad}-\n" + yamlText (depth + 1) other)
            |> String.concat ""
        | YScalar s -> pad + jsonString s + "\n"
        | YNull -> pad + "null\n"

    // ---------- Collections ----------

    let toJson (records: KnowledgeRecord list) =
        records |> List.map (toYaml false) |> YSeq |> jsonOf (Some 2) 0

    let toJsonl (records: KnowledgeRecord list) =
        records |> List.map (toYaml false >> jsonOf None 0) |> String.concat "\n"

    let toYamlText (records: KnowledgeRecord list) =
        records
        |> List.map (fun r -> "---\n" + yamlText 0 (toYaml false r))
        |> String.concat ""

    let private collect (results: Result<'a, string> list) =
        results
        |> List.fold
            (fun acc r ->
                match acc, r with
                | Ok xs, Ok x -> Ok(x :: xs)
                | (Error _ as e), _ -> e
                | _, Error e -> Error e)
            (Ok [])
        |> Result.map List.rev

    let ofJson (text: string) =
        yamlOfJson text
        |> Result.bind (function
            | YSeq xs -> xs |> List.map ofYaml |> collect
            | _ -> Error "expected a JSON array")

    let ofJsonl (text: string) =
        text.Split('\n')
        |> Array.filter (String.IsNullOrWhiteSpace >> not)
        |> List.ofArray
        |> List.map (yamlOfJson >> Result.bind ofYaml)
        |> collect

    let ofYamlText (text: string) =
        System.Text.RegularExpressions.Regex.Split(text, "(?m)^---\n")
        |> Array.filter (String.IsNullOrWhiteSpace >> not)
        |> List.ofArray
        |> List.map (Yaml.parse >> Result.bind ofYaml)
        |> collect

    // ---------- Structured Markdown ----------

    /// Record → canonical structured Markdown (front matter + body).
    let toMarkdown (includeExtra: bool) (r: KnowledgeRecord) =
        match toYaml includeExtra r with
        | YMap kvs ->
            "---\n"
            + yamlText 0 (YMap(kvs |> List.filter (fst >> (<>) "body")))
            + "---\n\n"
            + r.Body
            + "\n"
        | other -> yamlText 0 other

    /// Canonical structured Markdown → record.
    let ofMarkdown (text: string) =
        FrontMatter.split text
        |> Result.bind (fun doc ->
            match doc.FrontMatter with
            | Some(YMap kvs) ->
                let body = doc.Body.TrimStart('\n')

                let body =
                    if body.EndsWith "\n" then
                        body.Substring(0, body.Length - 1)
                    else
                        body

                ofYaml (YMap(kvs @ [ "body", YScalar body ]))
            | _ -> Error "missing front matter")
