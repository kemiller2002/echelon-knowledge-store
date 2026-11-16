module FxLedger.Tests.LedgerTests

open System
open Xunit
open FxIdentity.Identity
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
let ``balance includes an approved entry dated exactly on the cut-off`` () =
    Assert.Equal(10m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 1 1) true ])

[<Fact>]
let ``balance still excludes unapproved entries dated on the cut-off`` () =
    Assert.Equal(0m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 1 1) false ])

// ---- WI-0002: addEntry (RQ-FXL-0001, RQ-FXL-0002, RQ-FXL-0101) ----

let today = d 6 15

let newEntry id tenant amount (description: string) (date: DateOnly) =
    { Id = id; TenantId = tenant; Amount = amount; Description = description; Date = date
      CreatedBy = "u1"; Approved = true; ApprovedBy = Some "u2"; RefundOf = None }

[<Fact>]
let ``addEntry accepts the maximum amount of 1,000,000`` () =
    Assert.True(Result.isOk (addEntry today (newEntry "a1" "t1" 1_000_000m "rent" today) []))

[<Fact>]
let ``addEntry rejects amounts above 1,000,000 and amounts not above 0`` () =
    let r1 = addEntry today (newEntry "a1" "t1" 1_000_000.01m "rent" today) []
    let r2 = addEntry today (newEntry "a2" "t1" 0m "rent" today) []
    Assert.True((r1 = Error InvalidAmount))
    Assert.True((r2 = Error InvalidAmount))

[<Fact>]
let ``addEntry accepts a 200 character description and rejects 201`` () =
    let ok = String('a', 200)
    let tooLong = String('a', 201)
    Assert.True(Result.isOk (addEntry today (newEntry "a1" "t1" 5m ok today) []))
    Assert.True((addEntry today (newEntry "a2" "t1" 5m tooLong today) [] = Error InvalidDescription))

[<Fact>]
let ``addEntry measures the description length after trimming`` () =
    let padded = "  " + String('a', 200) + "  "
    Assert.True(Result.isOk (addEntry today (newEntry "a1" "t1" 5m padded today) []))

[<Fact>]
let ``addEntry rejects an empty description and descriptions with control characters`` () =
    Assert.True((addEntry today (newEntry "a1" "t1" 5m "   " today) [] = Error InvalidDescription))
    Assert.True((addEntry today (newEntry "a2" "t1" 5m "a\tb" today) [] = Error InvalidDescription))
    Assert.True((addEntry today (newEntry "a3" "t1" 5m "a\nb" today) [] = Error InvalidDescription))

[<Fact>]
let ``addEntry accepts an entry dated today and rejects tomorrow with FutureDate`` () =
    Assert.True(Result.isOk (addEntry today (newEntry "a1" "t1" 5m "rent" today) []))
    Assert.True((addEntry today (newEntry "a2" "t1" 5m "rent" (today.AddDays 1)) [] = Error FutureDate))

[<Fact>]
let ``addEntry rejects a duplicate id from any tenant`` () =
    let existing = [ newEntry "a1" "t2" 5m "other" today ]
    Assert.True((addEntry today (newEntry "a1" "t1" 5m "rent" today) existing = Error DuplicateId))

[<Fact>]
let ``addEntry stores the description trimmed, unapproved, appended last`` () =
    let existing = [ newEntry "a0" "t1" 3m "first" today ]
    match addEntry today (newEntry "a1" "t1" 5m "  rent  " today) existing with
    | Ok ledger ->
        let last = List.last ledger
        Assert.Equal(2, ledger.Length)
        Assert.Equal("a0", ledger.Head.Id)
        Assert.Equal("a1", last.Id)
        Assert.Equal("rent", last.Description)
        Assert.False(last.Approved)
        Assert.Equal(None, last.ApprovedBy)
    | Error e -> failwithf "unexpected error %A" e

// ---- WI-0006: exportCsv (RQ-FXL-0007, DF-FXL-0004) ----

[<Fact>]
let ``exportCsv of a tenant with no approved entries is only the header`` () =
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [ entry "e1" "t1" 5m (d 1 1) false ])

[<Fact>]
let ``exportCsv quotes a description with a comma and doubles embedded quotes`` () =
    let e = { entry "e1" "t1" 5m (d 1 1) true with Description = "a,\"b\"" }
    Assert.Equal("id,date,amount,description\ne1,2026-01-01,5.00,\"a,\"\"b\"\"\"\n", exportCsv "t1" [ e ])

[<Fact>]
let ``exportCsv keeps approved entries of one tenant ordered by date then id`` () =
    let entries =
        [ entry "b" "t1" 1m (d 2 1) true
          entry "a" "t1" -5m (d 2 1) true
          entry "c" "t1" 2.5m (d 1 9) true
          entry "x" "t2" 9m (d 1 1) true
          entry "u" "t1" 4m (d 1 1) false ]
    let expected =
        "id,date,amount,description\n"
        + "c,2026-01-09,2.50,x\n"
        + "a,2026-02-01,-5.00,x\n"
        + "b,2026-02-01,1.00,x\n"
    Assert.Equal(expected, exportCsv "t1" entries)

// ---- WI-0008: refund (RQ-FXL-0009, RQ-FXL-0109, DF-FXL-0006) ----

let now = DateTimeOffset(2026, 6, 15, 9, 0, 0, TimeSpan.Zero)

let clerk = { UserId = "u5"; TenantId = "t1"; Roles = [ Clerk ]; IssuedAt = now; LastSeen = now }

let original amount (date: DateOnly) =
    { entry "orig" "t1" amount date true with Description = "sale" }

[<Fact>]
let ``refund allows 60 then 40 against an original of 100 and rejects a further 0.01`` () =
    let entries0 = [ original 100m today ]
    let entries1 =
        match refund now clerk today "r1" "orig" 60m entries0 with
        | Ok l -> l
        | Error e -> failwithf "unexpected %A" e
    let entries2 =
        match refund now clerk today "r2" "orig" 40m entries1 with
        | Ok l -> l
        | Error e -> failwithf "unexpected %A" e
    Assert.True((refund now clerk today "r3" "orig" 0.01m entries2 = Error RefundExceedsOriginal))

[<Fact>]
let ``refund records a negative unapproved entry linked to the original`` () =
    match refund now clerk today "r1" "orig" 25m [ original 100m today ] with
    | Ok ledger ->
        let r = List.last ledger
        Assert.Equal(-25m, r.Amount)
        Assert.False(r.Approved)
        Assert.Equal(Some "orig", r.RefundOf)
        Assert.Equal("Refund of orig", r.Description)
        Assert.Equal("u5", r.CreatedBy)
    | Error e -> failwithf "unexpected %A" e

[<Fact>]
let ``refund rejects an amount larger than the original`` () =
    Assert.True((refund now clerk today "r1" "orig" 100.01m [ original 100m today ] = Error RefundExceedsOriginal))

[<Fact>]
let ``refund allows an original dated 90 days ago and rejects one dated 91 days ago`` () =
    Assert.True(Result.isOk (refund now clerk today "r1" "orig" 10m [ original 100m (today.AddDays -90) ]))
    Assert.True((refund now clerk today "r2" "orig" 10m [ original 100m (today.AddDays -91) ] = Error(Unsupported "refund window closed")))

[<Fact>]
let ``refund rejects an expired session with SessionExpired`` () =
    let later = now.AddMinutes 15.01
    Assert.True((refund later clerk today "r1" "orig" 10m [ original 100m today ] = Error SessionExpired))

[<Fact>]
let ``refund requires Clerk or Admin`` () =
    let approverOnly = { clerk with Roles = [ Approver ] }
    Assert.True((refund now approverOnly today "r1" "orig" 10m [ original 100m today ] = Error NotAuthorized))
