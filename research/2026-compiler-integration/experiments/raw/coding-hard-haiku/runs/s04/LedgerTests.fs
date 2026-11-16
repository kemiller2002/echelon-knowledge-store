module FxLedger.Tests.LedgerTests

open System
open Xunit
open FxIdentity.Identity
open FxLedger.Ledger

let entry id tenant amount (date: DateOnly) approved =
    { Id = id; TenantId = tenant; Amount = amount; Description = "x"; Date = date
      CreatedBy = "u9"; Approved = approved; ApprovedBy = None; RefundOf = None }

let d (m: int) (day: int) = DateOnly(2026, m, day)

let isOk (result: Result<Entry list, LedgerError>) =
    match result with
    | Ok _ -> true
    | Error _ -> false

let isError (expected: LedgerError) (result: Result<Entry list, LedgerError>) =
    match result with
    | Error e -> e = expected
    | Ok _ -> false

let okOrFail (result: Result<Entry list, LedgerError>) =
    match result with
    | Ok entries -> entries
    | Error e -> failwithf "expected Ok but got %A" e

[<Fact>]
let ``balance counts approved entries well before the cut-off`` () =
    let entries = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 2) false; entry "e3" "t2" 7m (d 1 1) true ]
    Assert.Equal(10m, balance "t1" (d 3 1) entries)

[<Fact>]
let ``balance excludes entries after the cut-off`` () =
    Assert.Equal(0m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 2 1) true ])

[<Fact>]
let ``balance includes an approved entry dated exactly on the cut-off`` () =
    Assert.Equal(10m, balance "t1" (d 3 1) [ entry "e1" "t1" 10m (d 3 1) true ])

// ---- addEntry (WI-0002: RQ-FXL-0001, RQ-FXL-0002, RQ-FXL-0101) ----

let today = d 11 1

let newEntry id amount (description: string) (date: DateOnly) =
    { Id = id; TenantId = "t1"; Amount = amount; Description = description; Date = date
      CreatedBy = "u1"; Approved = true; ApprovedBy = Some "u2"; RefundOf = None }

[<Fact>]
let ``addEntry accepts the maximum amount`` () =
    Assert.True(isOk (addEntry today (newEntry "n1" 1_000_000m "rent" today) []))

[<Fact>]
let ``addEntry rejects amounts of zero or above the maximum`` () =
    Assert.True(isError InvalidAmount (addEntry today (newEntry "n1" 0m "rent" today) []))
    Assert.True(isError InvalidAmount (addEntry today (newEntry "n1" 1_000_000.01m "rent" today) []))

[<Fact>]
let ``addEntry accepts a 200 character description after trimming`` () =
    let description = "  " + String('a', 200) + "  "
    Assert.True(isOk (addEntry today (newEntry "n1" 10m description today) []))

[<Fact>]
let ``addEntry rejects a 201 character description`` () =
    Assert.True(isError InvalidDescription (addEntry today (newEntry "n1" 10m (String('a', 201)) today) []))

[<Fact>]
let ``addEntry rejects a description containing a control character`` () =
    Assert.True(isError InvalidDescription (addEntry today (newEntry "n1" 10m "a\tb" today) []))
    Assert.True(isError InvalidDescription (addEntry today (newEntry "n1" 10m "a\nb" today) []))

[<Fact>]
let ``addEntry accepts an entry dated today`` () =
    Assert.True(isOk (addEntry today (newEntry "n1" 10m "rent" today) []))

[<Fact>]
let ``addEntry rejects an entry dated tomorrow with FutureDate`` () =
    Assert.True(isError FutureDate (addEntry today (newEntry "n1" 10m "rent" (today.AddDays 1)) []))

[<Fact>]
let ``addEntry rejects an id that already exists in any tenant`` () =
    let existing = [ entry "n1" "t2" 5m (d 1 1) true ]
    Assert.True(isError DuplicateId (addEntry today (newEntry "n1" 10m "rent" today) existing))

[<Fact>]
let ``addEntry reports the first failing rule in DF-FXL-0001 order`` () =
    Assert.True(isError InvalidAmount (addEntry today (newEntry "n1" 0m "" (today.AddDays 1)) []))
    Assert.True(isError InvalidDescription (addEntry today (newEntry "n1" 10m "" (today.AddDays 1)) []))
    Assert.True(isError FutureDate (addEntry today (newEntry "n1" 10m "rent" (today.AddDays 1)) [ entry "n1" "t1" 5m (d 1 1) true ]))

[<Fact>]
let ``addEntry stores the description trimmed, unapproved and appended last`` () =
    let existing = [ entry "e0" "t1" 5m (d 1 1) true ]
    let stored = okOrFail (addEntry today (newEntry "n1" 10m "  rent " today) existing)
    Assert.Equal(2, List.length stored)
    Assert.Equal<Entry>(existing.Head, stored.Head)
    let last = List.last stored
    Assert.Equal("n1", last.Id)
    Assert.Equal("rent", last.Description)
    Assert.False(last.Approved)
    Assert.True(last.ApprovedBy.IsNone)

// ---- exportCsv (WI-0006: RQ-FXL-0007, RQ-FXL-0107) ----

[<Fact>]
let ``exportCsv returns only the header when a tenant has no approved entries`` () =
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [ entry "e1" "t1" 5m (d 1 1) false; entry "e2" "t2" 5m (d 1 1) true ])

[<Fact>]
let ``exportCsv quotes descriptions with commas and doubles embedded quotes`` () =
    let e = { entry "e1" "t1" 5m (d 1 1) true with Description = "a,\"b\"" }
    Assert.Equal("id,date,amount,description\ne1,2026-01-01,5.00,\"a,\"\"b\"\"\"\n", exportCsv "t1" [ e ])

[<Fact>]
let ``exportCsv writes two-decimal amounts and orders rows by date then id`` () =
    let entries =
        [ entry "e2" "t1" 2.5m (d 1 2) true
          entry "e1" "t1" -5m (d 1 2) true
          entry "e3" "t1" 1m (d 1 1) true ]
    let expected =
        "id,date,amount,description\n"
        + "e3,2026-01-01,1.00,x\n"
        + "e1,2026-01-02,-5.00,x\n"
        + "e2,2026-01-02,2.50,x\n"
    Assert.Equal(expected, exportCsv "t1" entries)

[<Fact>]
let ``exportCsv neutralises spreadsheet formulas`` () =
    let formula = { entry "e1" "t1" 5m (d 1 1) true with Description = "=SUM(A1)" }
    let minusComma = { entry "e2" "t1" 5m (d 1 1) true with Description = "-5,x" }
    let expected =
        "id,date,amount,description\n"
        + "e1,2026-01-01,5.00,'=SUM(A1)\n"
        + "e2,2026-01-01,5.00,\"'-5,x\"\n"
    Assert.Equal(expected, exportCsv "t1" [ formula; minusComma ])

// ---- refund (WI-0008: RQ-FXL-0009, RQ-FXL-0109, DF-FXL-0006) ----

let now = DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero)

let session roles (lastSeen: DateTimeOffset) : Session =
    { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = now.AddHours(-1.0); LastSeen = lastSeen }

let clerkSession = session [ Clerk ] now

let original = { entry "o1" "t1" 100m (d 10 1) true with Description = "sale" }

[<Fact>]
let ``refund rejects an expired session with SessionExpired`` () =
    let expired = session [ Clerk ] (now.AddMinutes(-16.0))
    Assert.True(isError SessionExpired (refund now expired today "r1" "o1" 10m [ original ]))

[<Fact>]
let ``refund requires the clerk or admin role`` () =
    Assert.True(isError NotAuthorized (refund now (session [ Viewer; Approver ] now) today "r1" "o1" 10m [ original ]))
    Assert.True(isOk (refund now (session [ Admin ] now) today "r1" "o1" 10m [ original ]))

[<Fact>]
let ``refund requires an approved original in the same tenant that is not a refund`` () =
    let unapproved = { original with Id = "o2"; Approved = false }
    let otherTenant = { original with Id = "o3"; TenantId = "t2" }
    let refundEntry = { original with Id = "r0"; Amount = -10m; RefundOf = Some "o1" }
    Assert.True(isError NotFound (refund now clerkSession today "r1" "missing" 10m [ original ]))
    Assert.True(isError NotFound (refund now clerkSession today "r1" "o2" 10m [ unapproved ]))
    Assert.True(isError NotFound (refund now clerkSession today "r1" "o3" 10m [ otherTenant ]))
    Assert.True(isError NotFound (refund now clerkSession today "r1" "r0" 10m [ original; refundEntry ]))

[<Fact>]
let ``refund rejects an original older than 90 days with an unsupported window error`` () =
    let old91 = { original with Date = today.AddDays -91 }
    let old90 = { original with Date = today.AddDays -90 }
    Assert.True(isError (Unsupported "refund window closed") (refund now clerkSession today "r1" "o1" 10m [ old91 ]))
    Assert.True(isOk (refund now clerkSession today "r1" "o1" 10m [ old90 ]))

[<Fact>]
let ``refund rejects a duplicate refund id`` () =
    let clash = entry "r1" "t2" 1m (d 1 1) true
    Assert.True(isError DuplicateId (refund now clerkSession today "r1" "o1" 10m [ original; clash ]))

[<Fact>]
let ``refund rejects a non-positive amount`` () =
    Assert.True(isError InvalidAmount (refund now clerkSession today "r1" "o1" 0m [ original ]))
    Assert.True(isError InvalidAmount (refund now clerkSession today "r1" "o1" -1m [ original ]))

[<Fact>]
let ``refunds of 60 then 40 succeed and a further 0.01 is rejected`` () =
    let afterFirst = okOrFail (refund now clerkSession today "r1" "o1" 60m [ original ])
    let stored = List.last afterFirst
    Assert.Equal(-60m, stored.Amount)
    Assert.Equal("Refund of o1", stored.Description)
    Assert.Equal(Some "o1", stored.RefundOf)
    Assert.False(stored.Approved)
    Assert.True(stored.ApprovedBy.IsNone)
    Assert.Equal("t1", stored.TenantId)
    Assert.Equal("u1", stored.CreatedBy)
    Assert.Equal(today, stored.Date)
    let afterSecond = okOrFail (refund now clerkSession today "r2" "o1" 40m afterFirst)
    Assert.Equal(3, List.length afterSecond)
    Assert.True(isError RefundExceedsOriginal (refund now clerkSession today "r3" "o1" 0.01m afterSecond))

[<Fact>]
let ``a refund larger than the original is rejected`` () =
    Assert.True(isError RefundExceedsOriginal (refund now clerkSession today "r1" "o1" 100.01m [ original ]))
