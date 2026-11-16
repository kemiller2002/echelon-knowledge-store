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

let today = d 6 15
let mk id amount desc date =
    { entry id "t1" amount date false with Description = desc }

[<Fact>]
let ``addEntry accepts amount 1000000 and rejects 1000000.01 and 0`` () =
    Assert.True(Result.isOk (addEntry today (mk "a" 1000000m "x" today) []))
    Assert.Equal(Error InvalidAmount, addEntry today (mk "a" 1000000.01m "x" today) [])
    Assert.Equal(Error InvalidAmount, addEntry today (mk "a" 0m "x" today) [])

[<Fact>]
let ``addEntry accepts 200 char description after trimming and rejects 201`` () =
    let ok = " " + String('a', 200) + " "
    Assert.True(Result.isOk (addEntry today (mk "a" 1m ok today) []))
    Assert.Equal(Error InvalidDescription, addEntry today (mk "a" 1m (String('a', 201)) today) [])
    Assert.Equal(Error InvalidDescription, addEntry today (mk "a" 1m "   " today) [])

[<Fact>]
let ``addEntry accepts today and rejects tomorrow`` () =
    Assert.True(Result.isOk (addEntry today (mk "a" 1m "x" today) []))
    Assert.Equal(Error FutureDate, addEntry today (mk "a" 1m "x" (today.AddDays 1)) [])

[<Fact>]
let ``addEntry rejects duplicate id in any tenant`` () =
    let existing = [ entry "a" "t2" 1m today true ]
    Assert.Equal(Error DuplicateId, addEntry today (mk "a" 1m "x" today) existing)

[<Fact>]
let ``addEntry reports first failing rule in fixed order`` () =
    let existing = [ entry "a" "t1" 1m today true ]
    let bad = mk "a" 0m "" (today.AddDays 1)
    Assert.Equal(Error InvalidAmount, addEntry today bad existing)
    Assert.Equal(Error InvalidDescription, addEntry today { bad with Amount = 1m } existing)
    Assert.Equal(Error FutureDate, addEntry today { bad with Amount = 1m; Description = "x" } existing)
    Assert.Equal(Error DuplicateId, addEntry today { bad with Amount = 1m; Description = "x"; Date = today } existing)

[<Fact>]
let ``addEntry stores trimmed unapproved entry appended last`` () =
    let existing = [ entry "e1" "t1" 1m today true ]
    let e = { mk "n" 5m "  rent " today with Approved = true; ApprovedBy = Some "boss" }
    match addEntry today e existing with
    | Ok l ->
        Assert.Equal(2, l.Length)
        Assert.Equal(existing.Head, l.Head)
        let last = List.last l
        Assert.False last.Approved
        Assert.Equal(None, last.ApprovedBy)
        Assert.Equal("rent", last.Description)
        Assert.Equal("n", last.Id)
    | Error e -> failwithf "unexpected %A" e

[<Fact>]
let ``balance includes approved entry dated exactly on the cut-off`` () =
    Assert.Equal(10m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 1 1) true ])

[<Fact>]
let ``balance excludes unapproved entries and other tenants on the cut-off`` () =
    let es = [ entry "e1" "t1" 10m (d 1 1) false; entry "e2" "t2" 7m (d 1 1) true; entry "e3" "t1" 3m (d 1 1) true ]
    Assert.Equal(3m, balance "t1" (d 1 1) es)

[<Fact>]
let ``exportCsv with no approved entries exports just the header`` () =
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [ entry "e1" "t1" 1m today false; entry "e2" "t2" 1m today true ])

[<Fact>]
let ``exportCsv quotes description a,"b" correctly`` () =
    let e = { entry "e1" "t1" 5m (d 1 2) true with Description = "a,\"b\"" }
    Assert.Equal("id,date,amount,description\ne1,2026-01-02,5.00,\"a,\"\"b\"\"\"\n", exportCsv "t1" [ e ])

[<Fact>]
let ``exportCsv orders rows by date then ordinal id with negative refund amounts`` () =
    let es =
        [ entry "b" "t1" 1m (d 1 2) true
          entry "B" "t1" 2m (d 1 2) true
          { entry "r" "t1" -5m (d 1 1) true with Description = "line1\nline2" } ]
    let expected =
        "id,date,amount,description\nr,2026-01-01,-5.00,\"line1\nline2\"\nB,2026-01-02,2.00,x\nb,2026-01-02,1.00,x\n"
    Assert.Equal(expected, exportCsv "t1" es)

let now = DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero)
let sess roles =
    { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = now.AddMinutes -5.0; LastSeen = now.AddMinutes -1.0 }
let orig = entry "o" "t1" 100m (d 6 1) true

[<Fact>]
let ``refund partial refunds up to original succeed and further 0.01 is rejected`` () =
    let r1 = refund now (sess [ Clerk ]) today "r1" "o" 60m [ orig ]
    let l1 = match r1 with Ok l -> l | Error e -> failwithf "%A" e
    let l2 = match refund now (sess [ Admin ]) today "r2" "o" 40m l1 with Ok l -> l | Error e -> failwithf "%A" e
    Assert.Equal(Error RefundExceedsOriginal, refund now (sess [ Clerk ]) today "r3" "o" 0.01m l2)

[<Fact>]
let ``refund larger than original is rejected`` () =
    Assert.Equal(Error RefundExceedsOriginal, refund now (sess [ Clerk ]) today "r1" "o" 100.01m [ orig ])

[<Fact>]
let ``refund stores the new entry in the specified form`` () =
    match refund now (sess [ Clerk ]) today "r1" "o" 25m [ orig ] with
    | Ok [ o; r ] ->
        Assert.Equal(orig, o)
        Assert.Equal("r1", r.Id)
        Assert.Equal("t1", r.TenantId)
        Assert.Equal(-25m, r.Amount)
        Assert.Equal("Refund of o", r.Description)
        Assert.Equal(today, r.Date)
        Assert.Equal("u1", r.CreatedBy)
        Assert.False r.Approved
        Assert.Equal(None, r.ApprovedBy)
        Assert.Equal(Some "o", r.RefundOf)
    | x -> failwithf "unexpected %A" x

[<Fact>]
let ``refund checks session, role, original, duplicate, amount in order`` () =
    let expired = { sess [ Clerk ] with LastSeen = now.AddMinutes -16.0 }
    Assert.Equal(Error SessionExpired, refund now expired today "o" "zz" 0m [ orig ])
    Assert.Equal(Error NotAuthorized, refund now (sess [ Viewer; Approver ]) today "o" "zz" 0m [ orig ])
    Assert.Equal(Error NotFound, refund now (sess [ Clerk ]) today "o" "zz" 0m [ orig ])
    Assert.Equal(Error DuplicateId, refund now (sess [ Clerk ]) today "o" "o" 0m [ orig ])
    Assert.Equal(Error InvalidAmount, refund now (sess [ Clerk ]) today "n" "o" 0m [ orig ])
    Assert.Equal(Error InvalidAmount, refund now (sess [ Clerk ]) today "n" "o" -1m [ orig ])

[<Fact>]
let ``refund original must be same tenant, approved and not a refund`` () =
    let s = sess [ Clerk ]
    Assert.Equal(Error NotFound, refund now s today "n" "o" 1m [ { orig with TenantId = "t2" } ])
    Assert.Equal(Error NotFound, refund now s today "n" "o" 1m [ { orig with Approved = false } ])
    Assert.Equal(Error NotFound, refund now s today "n" "o" 1m [ { orig with RefundOf = Some "x" } ])
