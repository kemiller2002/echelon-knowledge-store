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
let ``balance includes an approved entry dated exactly on the cut-off`` () =
    Assert.Equal(10m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 1 1) true ])

// ---- WI-0002: addEntry (RQ-FXL-0001, RQ-FXL-0002, RQ-FXL-0101) ----

let today = d 6 30

let newEntry id amount description (date: DateOnly) =
    { Id = id; TenantId = "t1"; Amount = amount; Description = description; Date = date
      CreatedBy = "u1"; Approved = false; ApprovedBy = None; RefundOf = None }

let addError (e: Entry) (existing: Entry list) =
    match addEntry today e existing with
    | Error err -> err
    | Ok _ -> failwith "expected an error"

[<Fact>]
let ``addEntry accepts an amount of 1,000,000`` () =
    Assert.True((addEntry today (newEntry "n1" 1_000_000m "rent" today) []).IsOk)

[<Fact>]
let ``addEntry rejects amounts of 0 and 1,000,000.01 with InvalidAmount`` () =
    Assert.Equal(InvalidAmount, addError (newEntry "n1" 0m "rent" today) [])
    Assert.Equal(InvalidAmount, addError (newEntry "n1" 1_000_000.01m "rent" today) [])

[<Fact>]
let ``addEntry accepts a 200 character description and rejects 201`` () =
    let ok = String('a', 200)
    Assert.True((addEntry today (newEntry "n1" 1m ok today) []).IsOk)
    Assert.Equal(InvalidDescription, addError (newEntry "n1" 1m (String('a', 201)) today) [])

[<Fact>]
let ``addEntry rejects an empty description after trimming`` () =
    Assert.Equal(InvalidDescription, addError (newEntry "n1" 1m "   " today) [])

[<Fact>]
let ``addEntry rejects control characters in descriptions`` () =
    Assert.Equal(InvalidDescription, addError (newEntry "n1" 1m "a\tb" today) [])
    Assert.Equal(InvalidDescription, addError (newEntry "n1" 1m "a\nb" today) [])

[<Fact>]
let ``addEntry accepts an entry dated today and rejects tomorrow with FutureDate`` () =
    Assert.True((addEntry today (newEntry "n1" 1m "rent" today) []).IsOk)
    Assert.Equal(FutureDate, addError (newEntry "n1" 1m "rent" (today.AddDays 1)) [])

[<Fact>]
let ``addEntry rejects an id already used by another tenant with DuplicateId`` () =
    let other = { newEntry "n1" 1m "rent" today with TenantId = "t2" }
    Assert.Equal(DuplicateId, addError (newEntry "n1" 1m "rent" today) [ other ])

[<Fact>]
let ``addEntry reports the first failing rule in DF-FXL-0001 order`` () =
    Assert.Equal(InvalidAmount, addError (newEntry "n1" 0m "" (today.AddDays 1)) [])

[<Fact>]
let ``addEntry stores the description trimmed, unapproved and appended last`` () =
    let existing = [ entry "e1" "t1" 5m today true ]
    let given = { newEntry "n2" 3m "  rent " today with Approved = true; ApprovedBy = Some "u2" }
    match addEntry today given existing with
    | Ok [ first; stored ] ->
        Assert.Equal("e1", first.Id)
        Assert.Equal("n2", stored.Id)
        Assert.Equal("rent", stored.Description)
        Assert.False(stored.Approved)
        Assert.Equal(None, stored.ApprovedBy)
    | other -> failwithf "unexpected result: %A" other

// ---- WI-0006: exportCsv (RQ-FXL-0007, RQ-FXL-0107, DF-FXL-0004) ----

[<Fact>]
let ``exportCsv of a tenant with no approved entries is the header line only`` () =
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [ entry "e1" "t1" 5m today false ])

[<Fact>]
let ``exportCsv quotes descriptions and doubles embedded quotes`` () =
    let e = { entry "e1" "t1" 5m today true with Description = "a,\"b\"" }
    Assert.Equal("id,date,amount,description\ne1,2026-06-30,5.00,\"a,\"\"b\"\"\"\n", exportCsv "t1" [ e ])

[<Fact>]
let ``exportCsv writes refunds with two decimals and orders by date then id`` () =
    let refundRow = { entry "r1" "t1" (-5m) (d 6 2) true with Description = "Refund" }
    let late = entry "b" "t1" 1m (d 6 1) true
    let early = entry "a" "t1" 2.5m (d 6 1) true
    let unapproved = entry "c" "t1" 9m (d 6 1) false
    let otherTenant = entry "d" "t2" 9m (d 6 1) true
    let csv = exportCsv "t1" [ refundRow; late; unapproved; otherTenant; early ]
    Assert.Equal(
        "id,date,amount,description\n"
        + "a,2026-06-01,2.50,x\n"
        + "b,2026-06-01,1.00,x\n"
        + "r1,2026-06-02,-5.00,Refund\n",
        csv)

[<Fact>]
let ``exportCsv neutralises descriptions starting with formula characters`` () =
    let f = { entry "e1" "t1" 1m today true with Description = "=SUM(A1)" }
    let g = { entry "e2" "t1" 1m today true with Description = "-5,x" }
    Assert.Equal("id,date,amount,description\ne1,2026-06-30,1.00,'=SUM(A1)\ne2,2026-06-30,1.00,\"'-5,x\"\n", exportCsv "t1" [ f; g ])

// ---- WI-0008: refund (RQ-FXL-0009, RQ-FXL-0109, DF-FXL-0006) ----

let now = DateTimeOffset(2026, 6, 30, 12, 0, 0, TimeSpan.Zero)

let sessionWith roles : FxIdentity.Identity.Session =
    { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = now; LastSeen = now }

let clerk = sessionWith [ FxIdentity.Identity.Clerk ]

let original amount = { entry "o1" "t1" amount (today.AddDays(-1)) true with CreatedBy = "u9" }

let refundError session date id orig amount entries =
    match refund now session date id orig amount entries with
    | Error err -> err
    | Ok _ -> failwith "expected an error"

[<Fact>]
let ``refunds of 60 then 40 succeed and a further 0.01 is rejected`` () =
    let r1 = refund now clerk today "r1" "o1" 60m [ original 100m ]
    let r2 = refund now clerk today "r2" "o1" 40m (Result.defaultWith (fun _ -> failwith "r1") r1)
    let after = Result.defaultWith (fun _ -> failwith "r2") r2
    Assert.Equal(RefundExceedsOriginal, refundError clerk today "r3" "o1" 0.01m after)

[<Fact>]
let ``a refund larger than the original is rejected with RefundExceedsOriginal`` () =
    Assert.Equal(RefundExceedsOriginal, refundError clerk today "r1" "o1" 100.01m [ original 100m ])

[<Fact>]
let ``a refund dated 90 days after the original is allowed and 91 days is rejected`` () =
    let at90 = { original 100m with Date = today.AddDays(-90) }
    let at91 = { original 100m with Date = today.AddDays(-91) }
    Assert.True((refund now clerk today "r1" "o1" 1m [ at90 ]).IsOk)
    Assert.Equal(Unsupported "refund window closed", refundError clerk today "r1" "o1" 1m [ at91 ])

[<Fact>]
let ``refund records a negative unapproved entry linked to the original`` () =
    match refund now clerk today "r1" "o1" 60m [ original 100m ] with
    | Ok [ _; r ] ->
        Assert.Equal(-60m, r.Amount)
        Assert.Equal("Refund of o1", r.Description)
        Assert.Equal(today, r.Date)
        Assert.Equal("u1", r.CreatedBy)
        Assert.False(r.Approved)
        Assert.Equal(None, r.ApprovedBy)
        Assert.Equal(Some "o1", r.RefundOf)
    | other -> failwithf "unexpected result: %A" other

[<Fact>]
let ``refund requires an active session`` () =
    let stale = { clerk with LastSeen = now.AddMinutes(-16.0) }
    Assert.Equal(SessionExpired, refundError stale today "r1" "o1" 1m [ original 100m ])

[<Fact>]
let ``refund requires the Clerk or Admin role`` () =
    let viewer = sessionWith [ FxIdentity.Identity.Viewer ]
    Assert.Equal(NotAuthorized, refundError viewer today "r1" "o1" 1m [ original 100m ])

[<Fact>]
let ``refund of an entry from another tenant is NotFound`` () =
    let other = { original 100m with TenantId = "t2" }
    Assert.Equal(NotFound, refundError clerk today "r1" "o1" 1m [ other ])

[<Fact>]
let ``refund rejects a non-positive amount with InvalidAmount`` () =
    Assert.Equal(InvalidAmount, refundError clerk today "r1" "o1" 0m [ original 100m ])
