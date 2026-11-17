module FxLedger.Tests.LedgerTests

open System
open System.IO
open Xunit
open FxIdentity.Identity
open FxLedger.Ledger

let entry id tenant amount (date: DateOnly) approved =
    { Id = id; TenantId = tenant; Amount = amount; Description = "x"; Date = date
      CreatedBy = "u9"; Approved = approved; ApprovedBy = None; RefundOf = None }

let d (m: int) (day: int) = DateOnly(2026, m, day)

let today = d 10 10
let now = DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero)
let header = "id,date,amount,description\n"
let clerk = { UserId = "u1"; TenantId = "t1"; Roles = [ Clerk ]; IssuedAt = now.AddHours(-1.0); LastSeen = now }

let newEntry id (amount: decimal) (desc: string) (date: DateOnly) =
    { Id = id; TenantId = "t1"; Amount = amount; Description = desc; Date = date
      CreatedBy = "u9"; Approved = false; ApprovedBy = None; RefundOf = None }

let expectError (expected: LedgerError) (result: Result<Entry list, LedgerError>) =
    Assert.Equal<Result<Entry list, LedgerError>>(Error expected, result)

[<Fact>]
let ``balance counts approved entries well before the cut-off`` () =
    let entries = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 2) false; entry "e3" "t2" 7m (d 1 1) true ]
    Assert.Equal(10m, balance "t1" (d 3 1) entries)

[<Fact>]
let ``balance excludes entries after the cut-off`` () =
    Assert.Equal(0m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 2 1) true ])

// WI-0004 / RQ-FXL-0005
[<Fact>]
let ``balance includes an approved entry dated exactly on the cut-off date`` () =
    Assert.Equal(10m, balance "t1" (d 3 1) [ entry "e1" "t1" 10m (d 3 1) true ])

[<Fact>]
let ``balance excludes unapproved entries and other tenants dated on the cut-off date`` () =
    let entries = [ entry "e1" "t1" 10m (d 3 1) false; entry "e2" "t2" 7m (d 3 1) true ]
    Assert.Equal(0m, balance "t1" (d 3 1) entries)

// WI-0002 / RQ-FXL-0001, RQ-FXL-0002, RQ-FXL-0101
[<Fact>]
let ``addEntry accepts amounts up to 1000000 and rejects 1000000.01 and 0`` () =
    Assert.True(Result.isOk (addEntry today (newEntry "a" 1_000_000m "rent" today) []))
    expectError InvalidAmount (addEntry today (newEntry "b" 1_000_000.01m "rent" today) [])
    expectError InvalidAmount (addEntry today (newEntry "c" 0m "rent" today) [])

[<Fact>]
let ``addEntry accepts a 200 character description after trimming and rejects 201`` () =
    let s200 = String('x', 200)
    Assert.True(Result.isOk (addEntry today (newEntry "a" 1m ("  " + s200 + "  ") today) []))
    expectError InvalidDescription (addEntry today (newEntry "b" 1m (String('x', 201)) today) [])

[<Fact>]
let ``addEntry accepts an entry dated today and rejects one dated tomorrow with FutureDate`` () =
    Assert.True(Result.isOk (addEntry today (newEntry "a" 1m "rent" today) []))
    expectError FutureDate (addEntry today (newEntry "b" 1m "rent" (today.AddDays 1)) [])

[<Fact>]
let ``addEntry stores the new entry trimmed, unapproved and appended last`` () =
    let existing = entry "e1" "t1" 10m (d 1 1) true
    let input = { (newEntry "e2" 5m "  rent " (d 1 2)) with Approved = true; ApprovedBy = Some "u2" }
    match addEntry today input [ existing ] with
    | Ok [ first; stored ] ->
        Assert.Equal(existing, first)
        Assert.Equal("rent", stored.Description)
        Assert.False(stored.Approved)
        Assert.True(stored.ApprovedBy.IsNone)
        Assert.Equal("e2", stored.Id)
    | other -> Assert.Fail(sprintf "unexpected result: %A" other)

[<Fact>]
let ``addEntry rejects descriptions containing a tab or a line feed`` () =
    expectError InvalidDescription (addEntry today (newEntry "a" 1m "a\tb" today) [])
    expectError InvalidDescription (addEntry today (newEntry "b" 1m "a\nb" today) [])

[<Fact>]
let ``addEntry rejects a duplicate id from any tenant`` () =
    let existing = entry "a" "t2" 10m (d 1 1) true
    expectError DuplicateId (addEntry today (newEntry "a" 1m "rent" today) [ existing ])

// WI-0006 / RQ-FXL-0007, RQ-FXL-0107
[<Fact>]
let ``exportCsv returns only the header line when the tenant has no approved entries`` () =
    let entries = [ newEntry "e1" 5m "rent" today; entry "e2" "t2" 5m today true ]
    Assert.Equal(header, exportCsv "t1" entries)

[<Fact>]
let ``exportCsv writes a description with a comma and quotes as quoted with doubled quotes`` () =
    let e = { (entry "e1" "t1" 10m (d 1 5) true) with Description = "a,\"b\"" }
    Assert.Equal(header + "e1,2026-01-05,10.00,\"a,\"\"b\"\"\"\n", exportCsv "t1" [ e ])

[<Fact>]
let ``exportCsv writes a description starting with an equals sign with a leading apostrophe`` () =
    let e = { (entry "e1" "t1" 10m (d 1 5) true) with Description = "=SUM(A1)" }
    Assert.Equal(header + "e1,2026-01-05,10.00,'=SUM(A1)\n", exportCsv "t1" [ e ])

[<Fact>]
let ``exportCsv applies the apostrophe before quoting for a description starting with a minus sign`` () =
    let e = { (entry "e1" "t1" 10m (d 1 5) true) with Description = "-5,x" }
    Assert.Equal(header + "e1,2026-01-05,10.00,\"'-5,x\"\n", exportCsv "t1" [ e ])

[<Fact>]
let ``exportCsv orders rows by date then id and writes amounts with two decimals`` () =
    let b = { (entry "b" "t1" 1m (d 2 1) true) with Description = "late" }
    let a = { (entry "a" "t1" (-5m) (d 2 1) true) with Description = "refund" }
    let z = { (entry "z" "t1" 3m (d 1 1) true) with Description = "early" }
    Assert.Equal(header + "z,2026-01-01,3.00,early\na,2026-02-01,-5.00,refund\nb,2026-02-01,1.00,late\n", exportCsv "t1" [ b; a; z ])

// WI-0008 / RQ-FXL-0009, RQ-FXL-0109, RQ-FXID-0004
let original amount = entry "o1" "t1" amount (d 10 1) true

[<Fact>]
let ``refund accepts refunds of 60 then 40 on a 100 original and rejects a further 0.01`` () =
    let r1 = refund now clerk today "r1" "o1" 60m [ original 100m ]
    Assert.True(Result.isOk r1)
    let r2 = refund now clerk today "r2" "o1" 40m (Result.defaultValue [] r1)
    Assert.True(Result.isOk r2)
    expectError RefundExceedsOriginal (refund now clerk today "r3" "o1" 0.01m (Result.defaultValue [] r2))

[<Fact>]
let ``refund larger than the original is rejected and nothing is recorded`` () =
    expectError RefundExceedsOriginal (refund now clerk today "r1" "o1" 100.01m [ original 100m ])

[<Fact>]
let ``refund is allowed when the original is exactly 90 days old`` () =
    let o = entry "o1" "t1" 100m (today.AddDays(-90)) true
    Assert.True(Result.isOk (refund now clerk today "r1" "o1" 10m [ o ]))

[<Fact>]
let ``refund is rejected with refund window closed when the original is 91 days old`` () =
    let o = entry "o1" "t1" 100m (today.AddDays(-91)) true
    expectError (Unsupported "refund window closed") (refund now clerk today "r1" "o1" 10m [ o ])

[<Fact>]
let ``refund rejects an expired session`` () =
    let stale = { clerk with LastSeen = now.AddMinutes(-15.01) }
    expectError SessionExpired (refund now stale today "r1" "o1" 10m [ original 100m ])

[<Fact>]
let ``refund requires the Clerk or Admin role`` () =
    let viewer = { clerk with Roles = [ Viewer ] }
    expectError NotAuthorized (refund now viewer today "r1" "o1" 10m [ original 100m ])

[<Fact>]
let ``refund appends an unapproved refund entry with the specified fields`` () =
    match refund now clerk today "r1" "o1" 25m [ original 100m ] with
    | Ok [ _; refundEntry ] ->
        Assert.Equal("r1", refundEntry.Id)
        Assert.Equal("t1", refundEntry.TenantId)
        Assert.Equal(-25m, refundEntry.Amount)
        Assert.Equal("Refund of o1", refundEntry.Description)
        Assert.Equal(today, refundEntry.Date)
        Assert.Equal("u1", refundEntry.CreatedBy)
        Assert.False(refundEntry.Approved)
        Assert.True(refundEntry.ApprovedBy.IsNone)
        Assert.Equal(Some "o1", refundEntry.RefundOf)
    | other -> Assert.Fail(sprintf "unexpected result: %A" other)

[<Fact>]
let ``RQ-FXID-0004 the ledger checks sessions with isActive and contains no timeout constants`` () =
    let src = File.ReadAllText(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "src", "FxLedger", "Ledger.fs"))
    Assert.Contains("isActive", src)
    for banned in [ "FromMinutes"; "FromHours"; "idleTimeout"; "maxLifetime"; "TimeSpan" ] do
        Assert.DoesNotContain(banned, src)
