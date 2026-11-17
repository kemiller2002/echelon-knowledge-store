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

[<Fact>]
let ``balance includes entries dated on the cut-off`` () =
    Assert.Equal(10m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 1 1) true ])

let today = d 6 1
let ok r = match r with Ok v -> v | Error e -> failwithf "%A" e

[<Fact>]
let ``addEntry accepts boundary values`` () =
    let e = { entry "n" "t1" 1000000m today false with Description = String('a', 200) }
    Assert.True(Result.isOk (addEntry today e []))

[<Fact>]
let ``addEntry rejects invalid amount`` () =
    Assert.Equal(Error InvalidAmount, addEntry today (entry "n" "t1" 1000000.01m today false) [])
    Assert.Equal(Error InvalidAmount, addEntry today (entry "n" "t1" 0m today false) [])

[<Fact>]
let ``addEntry rejects bad description`` () =
    let e = { entry "n" "t1" 1m today false with Description = String('a', 201) }
    Assert.Equal(Error InvalidDescription, addEntry today e [])
    Assert.Equal(Error InvalidDescription, addEntry today { e with Description = "  " } [])

[<Fact>]
let ``addEntry rejects future date and accepts today`` () =
    Assert.Equal(Error FutureDate, addEntry today (entry "n" "t1" 1m (today.AddDays 1) false) [])
    Assert.True(Result.isOk (addEntry today (entry "n" "t1" 1m today false) []))

[<Fact>]
let ``addEntry follows validation order and detects duplicates across tenants`` () =
    let existing = [ entry "n" "t2" 1m today true ]
    Assert.Equal(Error DuplicateId, addEntry today (entry "n" "t1" 1m today false) existing)
    let bad = { entry "n" "t1" 0m (today.AddDays 1) false with Description = "" }
    Assert.Equal(Error InvalidAmount, addEntry today bad existing)

[<Fact>]
let ``addEntry stores trimmed unapproved entry last`` () =
    let existing = [ entry "e1" "t1" 1m today true ]
    let e = { entry "n" "t1" 5m today true with Description = "  rent "; ApprovedBy = Some "u1" }
    let r = ok (addEntry today e existing)
    Assert.Equal(existing.Head, r.Head)
    let last = List.last r
    Assert.Equal("rent", last.Description)
    Assert.False last.Approved
    Assert.Equal(None, last.ApprovedBy)

[<Fact>]
let ``exportCsv header only when empty`` () =
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [ entry "e1" "t1" 1m today false ])

[<Fact>]
let ``exportCsv quotes and neutralises formulas and orders rows`` () =
    let es =
        [ { entry "b" "t1" 5m (d 1 2) true with Description = "a,\"b\"" }
          { entry "a" "t1" -5m (d 1 2) true with Description = "=SUM(A1)" }
          { entry "c" "t1" 1.5m (d 1 1) true with Description = "-5,x" }
          entry "z" "t2" 1m (d 1 1) true ]
    let expected =
        "id,date,amount,description\n"
        + "c,2026-01-01,1.50,\"'-5,x\"\n"
        + "a,2026-01-02,-5.00,'=SUM(A1)\n"
        + "b,2026-01-02,5.00,\"a,\"\"b\"\"\"\n"
    Assert.Equal(expected, exportCsv "t1" es)

open FxIdentity.Identity

let now = DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero)
let session roles =
    { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = now.AddMinutes -5.0; LastSeen = now.AddMinutes -1.0 }

[<Fact>]
let ``refund sequence up to the original succeeds then rejects`` () =
    let es = [ entry "o" "t1" 100m (d 1 1) true ]
    let s = session [ Clerk ]
    let es = ok (refund now s today "r1" "o" 60m es)
    let es = ok (refund now s today "r2" "o" 40m es)
    Assert.Equal(Error RefundExceedsOriginal, refund now s today "r3" "o" 0.01m es)
    let r = es.[1]
    Assert.Equal(-60m, r.Amount)
    Assert.Equal("Refund of o", r.Description)
    Assert.Equal(Some "o", r.RefundOf)
    Assert.False r.Approved

[<Fact>]
let ``refund rejects larger than original`` () =
    let es = [ entry "o" "t1" 100m (d 1 1) true ]
    Assert.Equal(Error RefundExceedsOriginal, refund now (session [ Admin ]) today "r" "o" 100.01m es)

[<Fact>]
let ``refund checks in order`` () =
    let es = [ entry "o" "t1" 100m (d 1 1) true; entry "u" "t1" 10m (d 1 1) false; entry "x" "t2" 10m (d 1 1) true ]
    let s = session [ Clerk ]
    let expired = { s with LastSeen = now.AddMinutes -20.0 }
    Assert.Equal(Error SessionExpired, refund now expired today "r" "o" 1m es)
    Assert.Equal(Error NotAuthorized, refund now (session [ Viewer; Approver ]) today "r" "o" 1m es)
    Assert.Equal(Error NotFound, refund now s today "r" "u" 1m es)
    Assert.Equal(Error NotFound, refund now s today "r" "x" 1m es)
    Assert.Equal(Error NotFound, refund now s today "r" "missing" 1m es)
    Assert.Equal(Error DuplicateId, refund now s today "u" "o" 0m es)
    Assert.Equal(Error InvalidAmount, refund now s today "r" "o" 0m es)
    Assert.Equal(Error InvalidAmount, refund now s today "r" "o" -1m es)

[<Fact>]
let ``refund of a refund is not found`` () =
    let s = session [ Clerk ]
    let es = [ { entry "o" "t1" 100m (d 1 1) true with RefundOf = Some "p" } ]
    Assert.Equal(Error NotFound, refund now s today "r" "o" 1m es)
