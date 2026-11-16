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

// ---- WI-0002 ----
let today = d 6 15
let newEntry id amount (desc: string) (date: DateOnly) =
    { entry id "t1" amount date false with Description = desc }

[<Fact>]
let ``addEntry accepts amount 1000000 and rejects 1000000.01 and 0`` () =
    Assert.True(Result.isOk (addEntry today (newEntry "a" 1000000m "x" today) []))
    Assert.Equal(Error InvalidAmount, addEntry today (newEntry "a" 1000000.01m "x" today) [])
    Assert.Equal(Error InvalidAmount, addEntry today (newEntry "a" 0m "x" today) [])

[<Fact>]
let ``addEntry accepts 200 char trimmed description and rejects 201`` () =
    let s n = String('a', n)
    Assert.True(Result.isOk (addEntry today (newEntry "a" 1m ("  " + s 200 + " ") today) []))
    Assert.Equal(Error InvalidDescription, addEntry today (newEntry "a" 1m (s 201) today) [])
    Assert.Equal(Error InvalidDescription, addEntry today (newEntry "a" 1m "   " today) [])

[<Fact>]
let ``addEntry accepts today and rejects tomorrow with FutureDate`` () =
    Assert.True(Result.isOk (addEntry today (newEntry "a" 1m "x" today) []))
    Assert.Equal(Error FutureDate, addEntry today (newEntry "a" 1m "x" (today.AddDays 1)) [])

[<Fact>]
let ``addEntry rejects duplicate id in any tenant`` () =
    let existing = [ entry "a" "t2" 1m today true ]
    Assert.Equal(Error DuplicateId, addEntry today (newEntry "a" 1m "x" today) existing)

[<Fact>]
let ``addEntry reports first failing rule in fixed order`` () =
    let existing = [ entry "a" "t1" 1m today true ]
    let bad = newEntry "a" 0m "" (today.AddDays 1)
    Assert.Equal(Error InvalidAmount, addEntry today bad existing)
    Assert.Equal(Error InvalidDescription, addEntry today { bad with Amount = 1m } existing)
    Assert.Equal(Error FutureDate, addEntry today { bad with Amount = 1m; Description = "x" } existing)
    Assert.Equal(Error DuplicateId, addEntry today { bad with Amount = 1m; Description = "x"; Date = today } existing)

[<Fact>]
let ``addEntry stores trimmed unapproved entry appended last`` () =
    let existing = [ entry "e0" "t1" 1m today true ]
    let e = { newEntry "n" 5m "  rent " today with Approved = true; ApprovedBy = Some "boss" }
    match addEntry today e existing with
    | Ok [ first; added ] ->
        Assert.Equal(existing.Head, first)
        Assert.Equal("rent", added.Description)
        Assert.False added.Approved
        Assert.Equal(None, added.ApprovedBy)
        Assert.Equal("n", added.Id)
    | other -> failwithf "unexpected %A" other

// ---- WI-0004 ----
[<Fact>]
let ``balance includes approved entry dated exactly on the cut-off`` () =
    Assert.Equal(10m, balance "t1" (d 2 1) [ entry "e1" "t1" 10m (d 2 1) true ])

[<Fact>]
let ``balance excludes unapproved and other tenant entries on the cut-off`` () =
    let es = [ entry "e1" "t1" 10m (d 2 1) false; entry "e2" "t2" 4m (d 2 1) true; entry "e3" "t1" 3m (d 2 2) true ]
    Assert.Equal(0m, balance "t1" (d 2 1) es)

// ---- WI-0006 ----
let withDesc (e: Entry) desc = { e with Description = desc }

[<Fact>]
let ``exportCsv with no approved entries exports only the header`` () =
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [ entry "e1" "t1" 1m today false; entry "e2" "t2" 1m today true ])

[<Fact>]
let ``exportCsv quotes and doubles quotes in description`` () =
    let es = [ withDesc (entry "e1" "t1" 5m (d 1 2) true) "a,\"b\"" ]
    Assert.Equal("id,date,amount,description\ne1,2026-01-02,5.00,\"a,\"\"b\"\"\"\n", exportCsv "t1" es)

[<Fact>]
let ``exportCsv formats rows sorted by date then ordinal id with negative refund`` () =
    let es =
        [ entry "b" "t1" 1.5m (d 1 2) true
          entry "B" "t1" -5m (d 1 2) true
          entry "z" "t1" 10m (d 1 1) true
          withDesc (entry "q" "t1" 1m (d 1 3) true) "line\nbreak" ]
    let expected =
        "id,date,amount,description\nz,2026-01-01,10.00,x\nB,2026-01-02,-5.00,x\nb,2026-01-02,1.50,x\nq,2026-01-03,1.00,\"line\nbreak\"\n"
    Assert.Equal(expected, exportCsv "t1" es)

// ---- WI-0008 ----
open FxIdentity.Identity
let now = DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero)
let session roles = { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = now.AddHours -1.0; LastSeen = now.AddMinutes -1.0 }
let orig = entry "o" "t1" 100m (d 6 1) true
let doRefund s id amt es = refund now s today id "o" amt es
let applyOk r = match r with Ok es -> es | Error e -> failwithf "unexpected %A" e

[<Fact>]
let ``refund 60 then 40 succeed and a further 0.01 is rejected`` () =
    let s = session [ Clerk ]
    let es1 = applyOk (doRefund s "r1" 60m [ orig ])
    let es2 = applyOk (doRefund s "r2" 40m es1)
    Assert.Equal(Error RefundExceedsOriginal, doRefund s "r3" 0.01m es2)
    let r = List.last es2
    Assert.Equal(-40m, r.Amount)
    Assert.Equal("Refund of o", r.Description)
    Assert.Equal(today, r.Date)
    Assert.Equal("u1", r.CreatedBy)
    Assert.False r.Approved
    Assert.Equal(None, r.ApprovedBy)
    Assert.Equal(Some "o", r.RefundOf)
    Assert.Equal("t1", r.TenantId)

[<Fact>]
let ``refund larger than the original is rejected`` () =
    Assert.Equal(Error RefundExceedsOriginal, doRefund (session [ Admin ]) "r1" 100.01m [ orig ])

[<Fact>]
let ``refund checks session, role, original, duplicate and amount in order`` () =
    let expired = { session [ Clerk ] with LastSeen = now.AddMinutes -16.0 }
    Assert.Equal(Error SessionExpired, doRefund expired "r1" 1m [ orig ])
    Assert.Equal(Error NotAuthorized, doRefund (session [ Viewer; Approver ]) "r1" 1m [ orig ])
    let s = session [ Clerk ]
    Assert.Equal(Error NotFound, doRefund s "r1" 1m [])
    Assert.Equal(Error NotFound, doRefund s "r1" 1m [ { orig with TenantId = "t2" } ])
    Assert.Equal(Error NotFound, doRefund s "r1" 1m [ { orig with Approved = false } ])
    Assert.Equal(Error NotFound, doRefund s "r1" 1m [ { orig with RefundOf = Some "x" } ])
    Assert.Equal(Error DuplicateId, doRefund s "o" 0m [ orig ])
    Assert.Equal(Error InvalidAmount, doRefund s "r1" 0m [ orig ])
    Assert.Equal(Error InvalidAmount, doRefund s "r1" -1m [ orig ])
    // order: duplicate beats invalid amount; role beats not found
    Assert.Equal(Error NotAuthorized, doRefund (session [ Viewer ]) "r1" 1m [])
