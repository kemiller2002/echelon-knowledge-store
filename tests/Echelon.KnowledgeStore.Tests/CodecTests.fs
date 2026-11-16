module Echelon.KnowledgeStore.Tests.CodecTests

open Xunit
open Echelon.KnowledgeStore
open Echelon.KnowledgeStore.Tests.Fixtures

let private core = all |> List.map Codec.core

[<Fact>]
let ``json round-trips`` () =
    Assert.Equal(Ok core, Codec.ofJson (Codec.toJson core))

[<Fact>]
let ``jsonl round-trips`` () =
    Assert.Equal(Ok core, Codec.ofJsonl (Codec.toJsonl core))

[<Fact>]
let ``yaml round-trips including bodies with thematic breaks`` () =
    Assert.Equal(Ok core, Codec.ofYamlText (Codec.toYamlText core))

[<Fact>]
let ``structured markdown round-trips losslessly with extra fields`` () =
    all
    |> List.iter (fun r -> Assert.Equal(Ok r, Codec.ofMarkdown (Codec.toMarkdown true r)))

[<Fact>]
let ``dsl round-trips`` () =
    match Dsl.read (Dsl.write core) with
    | Ok back -> Assert.Equal<KnowledgeRecord list>(core, back)
    | Error ds -> failwithf "%A" ds

[<Fact>]
let ``dsl reports diagnostics with line numbers instead of throwing`` () =
    match Dsl.read "decision DF-1\n  title \"unterminated\n  bogus x\n" with
    | Ok _ -> failwith "expected diagnostics"
    | Error ds ->
        Assert.Contains(ds, fun d -> d.Line = 2)
        Assert.Contains(ds, fun d -> d.Line = 3)

[<Fact>]
let ``dsl tokenizer handles quoted strings`` () =
    Assert.Equal(Ok [ "a"; "b c"; "d\"e" ], Dsl.tokenize "a \"b c\" \"d\\\"e\"")

[<Fact>]
let ``fsharp source writer emits one record per entry`` () =
    let src = FSharpSource.write core
    Assert.Equal(core.Length, src.Split("{ Id = ").Length - 1)

[<Fact>]
let ``dsl tokenizer handles many tokens without stack overflow`` () =
    let line = String.replicate 24_000 "\"a\" "

    match Dsl.tokenize line with
    | Ok ts -> Assert.Equal(24_000, ts.Length)
    | Error e -> failwith e

[<Fact>]
let ``dsl tokenizer rejects over-long lines`` () =
    Assert.True(Dsl.tokenize (String.replicate 60_000 "a ") |> Result.isError)
