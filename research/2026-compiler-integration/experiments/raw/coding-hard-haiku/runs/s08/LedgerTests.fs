module FxLedger.Tests.LedgerTests

open System
open Xunit
open FxIdentity.Identity
open FxLedger.Ledger

let entry id tenant amount (date: DateOnly) approved =
    { Id = id; TenantId = tenant; Amount = amount; Description = "x"; Date = date
      CreatedBy = "u9"; Approved = approved; ApprovedBy = None; RefundOf = None }

let d (m: int) (day: int) = DateOnly(2026, m, day)

let mk id tenant amount (date: DateOnly) approved (desc: string) =
    { Id = id; TenantId = tenant; Amount = amount; Description = desc; Date = date
      CreatedBy = "u9"; Approved = approved; ApprovedBy = None; RefundOf = None }

let errOf (r: Result<Entry list, LedgerError>) =
    match r with
    | Error e -> Some e
    | Ok _ -> None

let okOf (r: Result<Entry list, LedgerError>) =
    match r with
    | Ok l -> l
    | Error e -> failwithf "unexpected error %A" e

let now = DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero)

let sessionWith roles : Session =
    { UserId = "u1"; TenantId = "t1"; Roles = roles
      IssuedAt = now.AddHours(-1.0); LastSeen = now.AddMinutes(-1.0) }

// ---- Existing tests ----

[<Fact>]
let ``balance counts approved entries well before the cut-off`` () =
    let entries = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 2) false; entry "e3" "t2" 7m (d 1 1) true ]
    Assert.Equal(10m, balance "t1" (d 3 1) entries)

[<Fact>]
let ``balance excludes entries after the cut-off`` () =
    Assert.Equal(0m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 2 1) true ])

// ---- WI-0002: addEntry (RQ-FXL-0001, RQ-FXL-0002, RQ-FXL-0101) ----

let today = d 10 10

[<Fact>]
let ``addEntry accepts amount 1000000 and rejects 1000000.01 and 0 with InvalidAmount`` () =
    Assert.True((addEntry today (mk "a" "t1" 1000000m today false "x") []).IsOk)
    Assert.Equal(Some InvalidAmount, errOf (addEntry today (mk "a" "t1" 1000000.01m today false "x") []))
    Assert.Equal(Some InvalidAmount, errOf (addEntry today (mk "a" "t1" 0m today false "x") []))

[<Fact>]
let ``addEntry accepts a 200 character description after trimming and rejects 201`` () =
    let s200 = String('a', 200)
    Assert.True((addEntry today (mk "a" "t1" 1m today false ("  " + s200 + "  ")) []).IsOk)
    Assert.Equal(Some InvalidDescription, errOf (addEntry today (mk "a" "t1" 1m today false (String('a', 201))) []))

[<Fact>]
let ``addEntry accepts an entry dated today and rejects tomorrow with FutureDate`` () =
    Assert.True((addEntry today (mk "a" "t1" 1m today false "x") []).IsOk)
    Assert.Equal(Some FutureDate, errOf (addEntry today (mk "a" "t1" 1m (today.AddDays 1) false "x") []))

[<Fact>]
let ``addEntry stores the trimmed description unapproved and appended last`` () =
    let existing = mk "e0" "t1" 3m today true "old"
    let result = okOf (addEntry today (mk "e1" "t1" 5m today true "  rent ") [ existing ])
    Assert.Equal(2, result.Length)
    Assert.Equal(existing, result.[0])
    let stored = result.[1]
    Assert.Equal("rent", stored.Description)
    Assert.False(stored.Approved)
    Assert.Equal(None, stored.ApprovedBy)
    Assert.Equal("e1", stored.Id)

[<Fact>]
let ``addEntry rejects a tab or a line feed in the description with InvalidDescription`` () =
    Assert.Equal(Some InvalidDescription, errOf (addEntry today (mk "a" "t1" 1m today false "a\tb") []))
    Assert.Equal(Some InvalidDescription, errOf (addEntry today (mk "a" "t1" 1m today false "a\nb") []))

[<Fact>]
let ``addEntry rejects a duplicate id from any tenant with DuplicateId`` () =
    Assert.Equal(Some DuplicateId, errOf (addEntry today (mk "a" "t1" 1m today false "x") [ mk "a" "t2" 1m today true "y" ]))

[<Fact>]
let ``addEntry reports the first failing rule in the DF-FXL-0001 order`` () =
    Assert.Equal(Some InvalidAmount, errOf (addEntry today (mk "a" "t1" 0m (today.AddDays 1) false "") [ mk "a" "t1" 1m today false "x" ]))
    Assert.Equal(Some InvalidDescription, errOf (addEntry today (mk "a" "t1" 1m (today.AddDays 1) false "") []))

// ---- WI-0004: balance cut-off (RQ-FXL-0005) ----

[<Fact>]
let ``balance includes an approved entry dated exactly the cut-off`` () =
    Assert.Equal(10m, balance "t1" (d 3 1) [ entry "e1" "t1" 10m (d 3 1) true ])

[<Fact>]
let ``balance excludes unapproved entries, other tenants and entries after the cut-off`` () =
    let entries =
        [ entry "e1" "t1" 10m (d 3 1) false
          entry "e2" "t2" 20m (d 3 1) true
          entry "e3" "t1" 30m (d 3 2) true
          entry "e4" "t1" 40m (d 3 1) true ]
    Assert.Equal(40m, balance "t1" (d 3 1) entries)

// ---- WI-0006: exportCsv (RQ-FXL-0007, RQ-FXL-0107) ----

[<Fact>]
let ``exportCsv of a tenant with no approved entries is just the header line`` () =
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [ mk "a" "t1" 1m today false "x" ])

[<Fact>]
let ``exportCsv writes a description with comma and quotes as a doubled quoted field`` () =
    let out = exportCsv "t1" [ mk "a" "t1" 1m today true "a,\"b\"" ]
    Assert.Equal("id,date,amount,description\na,2026-10-10,1.00,\"a,\"\"b\"\"\"\n", out)

[<Fact>]
let ``exportCsv writes invariant dates and amounts ordered by date then id for approved tenant entries`` () =
    let entries =
        [ mk "e2" "t1" 5m (d 1 2) true "second"
          mk "r1" "t1" (-5m) (d 1 3) true "refund"
          mk "u1" "t1" 9m (d 1 2) false "unapproved"
          mk "x9" "t2" 9m (d 1 2) true "other tenant"
          mk "e1" "t1" 3.5m (d 1 2) true "first"
          mk "z0" "t1" 1m (d 1 1) true "earliest" ]
    let expected =
        "id,date,amount,description\n"
        + "z0,2026-01-01,1.00,earliest\n"
        + "e1,2026-01-02,3.50,first\n"
        + "e2,2026-01-02,5.00,second\n"
        + "r1,2026-01-03,-5.00,refund\n"
    Assert.Equal(expected, exportCsv "t1" entries)

[<Fact>]
let ``exportCsv quotes descriptions containing a carriage return or line feed`` () =
    let out = exportCsv "t1" [ mk "e1" "t1" 1m (d 1 2) true "a\nb"; mk "e2" "t1" 1m (d 1 2) true "c\rd" ]
    Assert.Equal("id,date,amount,description\ne1,2026-01-02,1.00,\"a\nb\"\ne2,2026-01-02,1.00,\"c\rd\"\n", out)

[<Fact>]
let ``exportCsv neutralises a leading formula character with an apostrophe`` () =
    let out = exportCsv "t1" [ mk "a" "t1" 1m today true "=SUM(A1)" ]
    Assert.Equal("id,date,amount,description\na,2026-10-10,1.00,'=SUM(A1)\n", out)

[<Fact>]
let ``exportCsv neutralises -5,x before quoting`` () =
    let out = exportCsv "t1" [ mk "a" "t1" 1m today true "-5,x" ]
    Assert.Equal("id,date,amount,description\na,2026-10-10,1.00,\"'-5,x\"\n", out)

// ---- WI-0008: refund (RQ-FXL-0009, RQ-FXL-0109, fx-identity RQ-FXID-0002/0004) ----

let original = mk "o1" "t1" 100m (d 1 1) true "orig"

let clerk = sessionWith [ Clerk ]

[<Fact>]
let ``refunds of 60 then 40 succeed and a further 0.01 is rejected`` () =
    let l1 = okOf (refund now clerk (d 1 1) "r1" "o1" 60m [ original ])
    let l2 = okOf (refund now clerk (d 1 1) "r2" "o1" 40m l1)
    Assert.Equal(Some RefundExceedsOriginal, errOf (refund now clerk (d 1 1) "r3" "o1" 0.01m l2))

[<Fact>]
let ``a refund larger than the original is rejected with RefundExceedsOriginal`` () =
    Assert.Equal(Some RefundExceedsOriginal, errOf (refund now clerk (d 1 1) "r1" "o1" 100.01m [ original ]))

[<Fact>]
let ``a refund is allowed 90 days after the original date and rejected at 91 days`` () =
    Assert.True((refund now clerk ((d 1 1).AddDays 90) "r1" "o1" 1m [ original ]).IsOk)
    Assert.Equal(
        Some(Unsupported "refund window closed"),
        errOf (refund now clerk ((d 1 1).AddDays 91) "r1" "o1" 1m [ original ])
    )

[<Fact>]
let ``a refund appends an unapproved negative entry linked to the original`` () =
    let result = okOf (refund now clerk (d 1 1) "r1" "o1" 60m [ original ])
    let stored = List.last result
    Assert.Equal(2, result.Length)
    Assert.Equal("r1", stored.Id)
    Assert.Equal("t1", stored.TenantId)
    Assert.Equal(-60m, stored.Amount)
    Assert.Equal("Refund of o1", stored.Description)
    Assert.Equal(d 1 1, stored.Date)
    Assert.Equal("u1", stored.CreatedBy)
    Assert.False(stored.Approved)
    Assert.Equal(None, stored.ApprovedBy)
    Assert.Equal(Some "o1", stored.RefundOf)

[<Fact>]
let ``a refund needs an active session`` () =
    let expired = { clerk with LastSeen = now.AddMinutes(-16.0) }
    Assert.Equal(Some SessionExpired, errOf (refund now expired (d 1 1) "r1" "o1" 1m [ original ]))

[<Fact>]
let ``a refund needs the Clerk or Admin role`` () =
    Assert.Equal(Some NotAuthorized, errOf (refund now (sessionWith [ Viewer; Approver ]) (d 1 1) "r1" "o1" 1m [ original ]))

[<Fact>]
let ``a refund of an unapproved original is NotFound`` () =
    let unapproved = { original with Approved = false }
    Assert.Equal(Some NotFound, errOf (refund now clerk (d 1 1) "r1" "o1" 1m [ unapproved ]))

[<Fact>]
let ``a session idle exactly 15 minutes is active and at 15 minutes and 1 second it is expired`` () =
    let s = { clerk with LastSeen = now }
    Assert.True(isActive (now.AddMinutes 15.0) s)
    Assert.False(isActive ((now.AddMinutes 15.0).AddSeconds 1.0) s)

[<Fact>]
let ``a session older than 8 hours is expired even if used recently`` () =
    let old = { clerk with IssuedAt = now.AddHours(-8.0).AddMinutes(-1.0); LastSeen = now }
    Assert.False(isActive now old)
    Assert.True(isActive now { clerk with IssuedAt = now.AddHours(-8.0); LastSeen = now })

let private ledgerSource () =
    let rec find (dir: IO.DirectoryInfo) =
        if isNull dir then
            failwith "src/FxLedger/Ledger.fs not found above the test binary"
        else
            let candidate = IO.Path.Combine(dir.FullName, "src", "FxLedger", "Ledger.fs")
            if IO.File.Exists candidate then IO.File.ReadAllText candidate else find dir.Parent

    find (IO.DirectoryInfo AppContext.BaseDirectory)

[<Fact>]
let ``application session checks call isActive and contain no timeout constants`` () =
    let src = ledgerSource ()
    Assert.Contains("isActive now session", src)
    Assert.DoesNotContain("TimeSpan", src)
    Assert.DoesNotContain("FromMinutes", src)
    Assert.DoesNotContain("FromHours", src)
