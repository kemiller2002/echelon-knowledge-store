module Echelon.KnowledgeStore.Tests.RetrievalTests

open Xunit
open Echelon.KnowledgeStore
open Echelon.KnowledgeStore.Tests.Fixtures

[<Fact>]
let ``sections split at headings`` () =
    let cs = Retrieval.sections "k" "T" "intro\n\n## A\n\na text\n\n## B\n\nb text"
    Assert.Equal<string list>([ "T"; "A"; "B" ], cs |> List.map _.Heading)

[<Fact>]
let ``bm25 ranks the matching chunk first`` () =
    let index =
        Retrieval.build
            [
                {
                    Key = "1"
                    Heading = "x"
                    Text = "github app tokens expire"
                }
                {
                    Key = "2"
                    Heading = "y"
                    Text = "survey page webkit firefox"
                }
            ]

    match Retrieval.search index 2 "firefox webkit" with
    | (c, _) :: _ -> Assert.Equal("2", c.Key)
    | [] -> failwith "no hits"

[<Fact>]
let ``context package lists evidence and never drops silently`` () =
    let p =
        Context.compile (fun s -> s.Length) 100_000 graph EvidenceReview "praxis:HY-T-2026-0001"

    Assert.Equal<string list>([], p.Omitted)
    Assert.Contains("EV-T-2026-0001", p.Text)

    let tight =
        Context.compile (fun s -> s.Length) 50 graph EvidenceReview "praxis:HY-T-2026-0001"

    Assert.NotEmpty(tight.Omitted)
    Assert.Contains("OMITTED", tight.Text)

[<Fact>]
let ``cards are deterministic regardless of input order`` () =
    let g1 = Graph.build false all
    let g2 = Graph.build false (List.rev all)
    Assert.Equal(Context.card g1 "praxis:HY-T-2026-0001", Context.card g2 "praxis:HY-T-2026-0001")

[<Fact>]
let ``strict model rejects requirement without acceptance and accepts one with it`` () =
    let ok = Strict.classify ([], []) requirement
    Assert.True(Result.isOk ok)

    let bad =
        Strict.classify
            ([], [])
            { requirement with
                Body = "No criteria."
            }

    Assert.True(Result.isError bad)

[<Fact>]
let ``strict hypothesis standing reflects evidence`` () =
    match Strict.classify (Strict.evidenceFor graph "praxis:HY-T-2026-0001") hypothesis with
    | Ok(StrictHypothesis(_, _, Contested _, _, _)) -> ()
    | other -> failwithf "unexpected %A" other

[<Fact>]
let ``strict hypothesis cannot claim support without evidence`` () =
    let claimed =
        { hypothesis with
            Status = Some "supported"
        }

    Assert.True(Strict.classify ([], []) claimed |> Result.isError)
