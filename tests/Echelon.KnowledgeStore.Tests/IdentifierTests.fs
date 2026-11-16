module Echelon.KnowledgeStore.Tests.IdentifierTests

open Xunit
open Echelon.KnowledgeStore

let private roundTrip text =
    KnowledgeId.parse text |> Result.map KnowledgeId.format

[<Theory>]
[<InlineData("REQ-0042")>]
[<InlineData("fides:DEC-0012")>]
[<InlineData("GV-START-001")>]
let ``valid identifiers round-trip`` (text: string) = Assert.Equal(Ok text, roundTrip text)

[<Theory>]
[<InlineData("")>]
[<InlineData("   ")>]
[<InlineData("a:b:c")>]
[<InlineData("Fides:REQ-1")>]
[<InlineData("-REQ")>]
[<InlineData("REQ 1")>]
let ``invalid identifiers are rejected`` (text: string) =
    Assert.True(KnowledgeId.parse text |> Result.isError)

[<Fact>]
let ``null is rejected`` () =
    Assert.True(KnowledgeId.parse null |> Result.isError)

[<Fact>]
let ``qualify adds owner only when missing`` () =
    let qualified =
        [ "REQ-1"; "praxis:REQ-2" ]
        |> List.map (
            KnowledgeId.parse
            >> Result.map (KnowledgeId.qualify "fides" >> KnowledgeId.format)
        )

    Assert.Equal<Result<string, string> list>([ Ok "fides:REQ-1"; Ok "praxis:REQ-2" ], qualified)
