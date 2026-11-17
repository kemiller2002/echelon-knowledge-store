module FxLedger.Tests.LedgerTests

open System
open Xunit
open FxLedger.Ledger
open FxIdentity.Identity

let entry id tenant amount (date: DateOnly) approved =
    { Id = id; TenantId = tenant; Amount = amount; Description = "x"; Date = date
      CreatedBy = "u9"; Approved = approved; ApprovedBy = None; RefundOf = None }

let d (m: int) (day: int) = DateOnly(2026, m, day)

[<Fact>]
let ``balance counts approved entries well before the cut-off`` () =
    let entries = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 2) false; entry "e3" "t2" 7m (d 1 1) true ]
    Assert.Equal(10m, balance "t1" (d 3 1) entries)

[<Fact>]
let ``balance excludes entries after the cut-off`` () =
    Assert.Equal(0m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 2 1) true ])

[<Fact>]
let ``balance includes entries dated on the cut-off`` () =
    Assert.Equal(10m, balance "t1" (d 2 1) [ entry "e1" "t1" 10m (d 2 1) true ])

let today = d 6 1
let withDesc desc = { entry "n1" "t1" 10m today false with Description = desc }

[<Fact>]
let ``addEntry amount bounds`` () =
    Assert.Equal(Ok [ { withDesc "x" with Amount = 1000000m } ], addEntry today { withDesc "x" with Amount = 1000000m } [])
    Assert.Equal(Error InvalidAmount, addEntry today { withDesc "x" with Amount = 1000000.01m } [])
    Assert.Equal(Error InvalidAmount, addEntry today { withDesc "x" with Amount = 0m } [])

[<Fact>]
let ``addEntry description rules`` () =
    Assert.True(Result.isOk (addEntry today (withDesc ("  " + String('a', 200) + " ")) []))
    Assert.Equal(Error InvalidDescription, addEntry today (withDesc (String('a', 201))) [])
    Assert.Equal(Error InvalidDescription, addEntry today (withDesc "   ") [])
    Assert.Equal(Error InvalidDescription, addEntry today (withDesc "a\tb") [])
    Assert.Equal(Error InvalidDescription, addEntry today (withDesc "a\nb") [])

[<Fact>]
let ``addEntry date and duplicate and order`` () =
    Assert.True(Result.isOk (addEntry today (withDesc "x") []))
    Assert.Equal(Error FutureDate, addEntry today { withDesc "x" with Date = d 6 2 } [])
    Assert.Equal(Error DuplicateId, addEntry today (withDesc "x") [ entry "n1" "t2" 1m today true ])
    Assert.Equal(Error InvalidAmount, addEntry today { withDesc "" with Amount = 0m; Date = d 7 1 } [ entry "n1" "t1" 1m today true ])
    Assert.Equal(Error InvalidDescription, addEntry today { withDesc "" with Date = d 7 1 } [ entry "n1" "t1" 1m today true ])
    Assert.Equal(Error FutureDate, addEntry today { withDesc "x" with Date = d 7 1 } [ entry "n1" "t1" 1m today true ])

[<Fact>]
let ``addEntry stores trimmed unapproved appended last`` () =
    let existing = entry "e0" "t1" 1m today true
    let e = { withDesc "  rent " with Approved = true; ApprovedBy = Some "u1" }
    match addEntry today e [ existing ] with
    | Ok [ a; b ] ->
        Assert.Equal(existing, a)
        Assert.Equal({ e with Description = "rent"; Approved = false; ApprovedBy = None }, b)
    | r -> failwithf "unexpected %A" r

let csvEntry id date amount desc =
    { entry id "t1" amount date true with Description = desc }

[<Fact>]
let ``exportCsv header only`` () =
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [ entry "e1" "t1" 1m today false ])

[<Fact>]
let ``exportCsv format quoting ordering and formulas`` () =
    let es =
        [ csvEntry "b" (d 1 2) 5m "a,\"b\""
          csvEntry "a" (d 1 2) -5m "=SUM(A1)"
          csvEntry "c" (d 1 1) 1234.5m "-5,x"
          { csvEntry "z" (d 1 1) 1m "no" with TenantId = "t2" } ]
    let expected =
        "id,date,amount,description\n"
        + "c,2026-01-01,1234.50,\"'-5,x\"\n"
        + "a,2026-01-02,-5.00,'=SUM(A1)\n"
        + "b,2026-01-02,5.00,\"a,\"\"b\"\"\"\n"
    Assert.Equal(expected, exportCsv "t1" es)

let now = DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero)
let session roles =
    { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = now.AddMinutes -5.0; LastSeen = now.AddMinutes -1.0 }
let orig = entry "o1" "t1" 100m (d 5 1) true

[<Fact>]
let ``refund cumulative cap`` () =
    let s = session [ Clerk ]
    let step id amt es = match refund now s today id "o1" amt es with Ok r -> r | e -> failwithf "%A" e
    let es = [ orig ] |> step "r1" 60m |> step "r2" 40m
    Assert.Equal(Error RefundExceedsOriginal, refund now s today "r3" "o1" 0.01m es)
    Assert.Equal(Error RefundExceedsOriginal, refund now s today "r3" "o1" 100.01m [ orig ])
    let last = List.last es
    Assert.Equal({ Id = "r2"; TenantId = "t1"; Amount = -40m; Description = "Refund of o1"; Date = today
                   CreatedBy = "u1"; Approved = false; ApprovedBy = None; RefundOf = Some "o1" }, last)

[<Fact>]
let ``refund checks in order`` () =
    let expired = { session [ Clerk ] with LastSeen = now.AddMinutes -16.0 }
    Assert.Equal(Error SessionExpired, refund now expired today "r" "o1" 1m [ orig ])
    Assert.Equal(Error NotAuthorized, refund now (session [ Approver ]) today "r" "o1" 1m [ orig ])
    Assert.True(Result.isOk (refund now (session [ Admin ]) today "r" "o1" 1m [ orig ]))
    let s = session [ Clerk ]
    Assert.Equal(Error NotFound, refund now s today "r" "zz" 1m [ orig ])
    Assert.Equal(Error NotFound, refund now s today "r" "o1" 1m [ { orig with TenantId = "t2" } ])
    Assert.Equal(Error NotFound, refund now s today "r" "o1" 1m [ { orig with Approved = false } ])
    Assert.Equal(Error NotFound, refund now s today "r" "o1" 1m [ { orig with RefundOf = Some "x" } ])
    Assert.Equal(Error DuplicateId, refund now s today "o1" "o1" 1m [ orig ])
    Assert.Equal(Error InvalidAmount, refund now s today "r" "o1" 0m [ orig ])

[<Fact>]
let ``refund window is 90 days`` () =
    let s = session [ Clerk ]
    Assert.True(Result.isOk (refund now s today "r" "o1" 1m [ { orig with Date = today.AddDays -90 } ]))
    Assert.Equal(Error(Unsupported "refund window closed"), refund now s today "r" "o1" 1m [ { orig with Date = today.AddDays -91 } ])
    Assert.Equal(Error(Unsupported "refund window closed"), refund now s today "o1" "o1" 1m [ { orig with Date = today.AddDays -91 } ])
