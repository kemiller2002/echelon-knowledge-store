module FxLedger.Tests.LedgerTests

open System
open Xunit
open FxLedger.Ledger

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

// ---- WI-0004
[<Fact>]
let ``balance includes approved entry dated exactly on the cut-off`` () =
    Assert.Equal(10m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 1 1) true ])

[<Fact>]
let ``balance excludes unapproved and other-tenant entries on the cut-off`` () =
    let es = [ entry "e1" "t1" 10m (d 1 1) false; entry "e2" "t2" 7m (d 1 1) true; entry "e3" "t1" 3m (d 1 2) true ]
    Assert.Equal(0m, balance "t1" (d 1 1) es)

// ---- WI-0002
let today = d 6 15
let withDesc desc (e: Entry) = { e with Description = desc }
let add e es = addEntry today e es

[<Fact>]
let ``addEntry accepts amount 1000000`` () =
    Assert.True(Result.isOk (add (entry "n" "t1" 1000000m today false) []))

[<Fact>]
let ``addEntry rejects amount 1000000.01 and 0 with InvalidAmount`` () =
    Assert.Equal(Error InvalidAmount, add (entry "n" "t1" 1000000.01m today false) [])
    Assert.Equal(Error InvalidAmount, add (entry "n" "t1" 0m today false) [])

[<Fact>]
let ``addEntry accepts 200 char description after trimming and rejects 201`` () =
    let e = entry "n" "t1" 1m today false
    Assert.True(Result.isOk (add (withDesc ("  " + String('a', 200) + " ") e) []))
    Assert.Equal(Error InvalidDescription, add (withDesc (String('a', 201)) e) [])
    Assert.Equal(Error InvalidDescription, add (withDesc "   " e) [])

[<Fact>]
let ``addEntry accepts entry dated today and rejects tomorrow with FutureDate`` () =
    Assert.True(Result.isOk (add (entry "n" "t1" 1m today false) []))
    Assert.Equal(Error FutureDate, add (entry "n" "t1" 1m (today.AddDays 1) false) [])

[<Fact>]
let ``addEntry rejects duplicate id in any tenant`` () =
    Assert.Equal(Error DuplicateId, add (entry "e1" "t1" 1m today false) [ entry "e1" "t2" 1m today true ])

[<Fact>]
let ``addEntry reports first failing rule in order amount description date duplicate`` () =
    let existing = [ entry "e1" "t1" 1m today true ]
    let all = { entry "e1" "t1" 0m (today.AddDays 1) false with Description = " " }
    Assert.Equal(Error InvalidAmount, add all existing)
    let noAmt = { all with Amount = 1m }
    Assert.Equal(Error InvalidDescription, add noAmt existing)
    let noDesc = { noAmt with Description = "ok" }
    Assert.Equal(Error FutureDate, add noDesc existing)
    Assert.Equal(Error DuplicateId, add { noDesc with Date = today } existing)

[<Fact>]
let ``addEntry stores trimmed unapproved entry appended last`` () =
    let existing = [ entry "e1" "t1" 1m today true ]
    let e = { entry "n" "t1" 5m today true with Description = "  rent "; ApprovedBy = Some "boss" }
    match add e existing with
    | Ok [ first; added ] ->
        Assert.Equal(existing.Head, first)
        Assert.Equal("rent", added.Description)
        Assert.False added.Approved
        Assert.Equal(None, added.ApprovedBy)
        Assert.Equal("n", added.Id)
    | other -> failwithf "unexpected %A" other

[<Fact>]
let ``addEntry rejects control characters in description after trimming`` () =
    let e = entry "n" "t1" 1m today false
    Assert.Equal(Error InvalidDescription, add (withDesc "a\tb" e) [])
    Assert.Equal(Error InvalidDescription, add (withDesc "a\nb" e) [])
    Assert.True(Result.isOk (add (withDesc "\t ok \n" e) []))
    // keeps description position in order: amount error still wins, date error loses
    Assert.Equal(Error InvalidAmount, add { withDesc "a\tb" e with Amount = 0m } [])
    Assert.Equal(Error InvalidDescription, add { withDesc "a\tb" e with Date = today.AddDays 1 } [])

// ---- WI-0006
[<Fact>]
let ``exportCsv with no approved entries returns just the header`` () =
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [ entry "e1" "t1" 1m today false; entry "e2" "t2" 1m today true ])

[<Fact>]
let ``exportCsv quotes description a,"b" as doubled quotes`` () =
    let e = { entry "e1" "t1" 1m (d 1 2) true with Description = "a,\"b\"" }
    Assert.Equal("id,date,amount,description\ne1,2026-01-02,1.00,\"a,\"\"b\"\"\"\n", exportCsv "t1" [ e ])

[<Fact>]
let ``exportCsv formats rows ordered by date then ordinal id with 2 decimals`` () =
    let es =
        [ entry "b" "t1" 2m (d 1 2) true
          entry "B" "t1" -5m (d 1 2) true
          entry "z" "t1" 1.5m (d 1 1) true ]
    Assert.Equal(
        "id,date,amount,description\nz,2026-01-01,1.50,x\nB,2026-01-02,-5.00,x\nb,2026-01-02,2.00,x\n",
        exportCsv "t1" es)

[<Fact>]
let ``exportCsv quotes CR and LF descriptions`` () =
    let e = { entry "e1" "t1" 1m (d 1 2) true with Description = "a\nb" }
    Assert.Equal("id,date,amount,description\ne1,2026-01-02,1.00,\"a\nb\"\n", exportCsv "t1" [ e ])

[<Fact>]
let ``exportCsv neutralises formulas with leading apostrophe before quoting`` () =
    let f desc = exportCsv "t1" [ { entry "e1" "t1" 1m (d 1 2) true with Description = desc } ]
    for c in [ "="; "+"; "-"; "@" ] do
        Assert.Equal("id,date,amount,description\ne1,2026-01-02,1.00,'" + c + "x\n", f (c + "x"))
    Assert.Equal("id,date,amount,description\ne1,2026-01-02,1.00,\"'=a,b\"\n", f "=a,b")

// ---- WI-0008
open FxIdentity.Identity

let now = DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero)
let session roles =
    { UserId = "clerk1"; TenantId = "t1"; Roles = roles; IssuedAt = now.AddHours -1.0; LastSeen = now.AddMinutes -1.0 }
let orig100 = entry "o1" "t1" 100m (d 6 1) true
let ref' id amt = refund now (session [ Clerk ]) today id "o1" amt

[<Fact>]
let ``refund fails with SessionExpired for inactive session`` () =
    let s = { session [ Clerk ] with LastSeen = now.AddMinutes -16.0 }
    Assert.Equal(Error SessionExpired, refund now s today "r1" "o1" 10m [ orig100 ])
    // expiry is checked before authorization
    let s2 = { session [ Viewer ] with LastSeen = now.AddMinutes -16.0 }
    Assert.Equal(Error SessionExpired, refund now s2 today "r1" "o1" 10m [ orig100 ])

[<Fact>]
let ``refund requires Clerk or Admin`` () =
    Assert.Equal(Error NotAuthorized, refund now (session [ Viewer; Approver ]) today "r1" "o1" 10m [ orig100 ])
    Assert.True(Result.isOk (refund now (session [ Admin ]) today "r1" "o1" 10m [ orig100 ]))
    Assert.True(Result.isOk (ref' "r1" 10m [ orig100 ]))

[<Fact>]
let ``refund original must exist in tenant be approved and not a refund`` () =
    Assert.Equal(Error NotFound, ref' "r1" 10m [])
    Assert.Equal(Error NotFound, ref' "r1" 10m [ { orig100 with TenantId = "t2" } ])
    Assert.Equal(Error NotFound, ref' "r1" 10m [ { orig100 with Approved = false } ])
    Assert.Equal(Error NotFound, ref' "r1" 10m [ { orig100 with RefundOf = Some "x" } ])

[<Fact>]
let ``refund rejects duplicate refund id`` () =
    Assert.Equal(Error DuplicateId, ref' "o1" 10m [ orig100 ])

[<Fact>]
let ``refund rejects non-positive amount`` () =
    Assert.Equal(Error InvalidAmount, ref' "r1" 0m [ orig100 ])
    Assert.Equal(Error InvalidAmount, ref' "r1" -1m [ orig100 ])

[<Fact>]
let ``refund checks run in documented order`` () =
    // duplicate id beats invalid amount; invalid amount beats exceeds
    Assert.Equal(Error DuplicateId, ref' "o1" 0m [ orig100 ])
    Assert.Equal(Error InvalidAmount, ref' "r1" -500m [ orig100 ])

[<Fact>]
let ``refund of 100 original allows 60 then 40 and rejects further 0.01`` () =
    let step id amt es = match ref' id amt es with Ok r -> r | Error e -> failwithf "%A" e
    let es = [ orig100 ] |> step "r1" 60m |> step "r2" 40m
    Assert.Equal(Error RefundExceedsOriginal, ref' "r3" 0.01m es)

[<Fact>]
let ``refund larger than original is rejected`` () =
    Assert.Equal(Error RefundExceedsOriginal, ref' "r1" 100.01m [ orig100 ])

[<Fact>]
let ``refund success appends unapproved negative entry`` () =
    match ref' "r1" 25m [ orig100 ] with
    | Ok [ o; r ] ->
        Assert.Equal(orig100, o)
        Assert.Equal(
            { Id = "r1"; TenantId = "t1"; Amount = -25m; Description = "Refund of o1"; Date = today
              CreatedBy = "clerk1"; Approved = false; ApprovedBy = None; RefundOf = Some "o1" },
            r)
    | other -> failwithf "unexpected %A" other

[<Fact>]
let ``refund window closes more than 90 days after original date`` () =
    let o = { orig100 with Date = today.AddDays -90 }
    Assert.True(Result.isOk (ref' "r1" 10m [ o ]))
    let late = { orig100 with Date = today.AddDays -91 }
    Assert.Equal(Error(Unsupported "refund window closed"), ref' "r1" 10m [ late ])
    // window check runs before duplicate id check
    Assert.Equal(Error(Unsupported "refund window closed"), ref' "o1" 10m [ late ])
