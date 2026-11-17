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
let ``balance includes an approved entry dated exactly on the cut-off`` () =
    let entries =
        [ entry "e1" "t1" 10m (d 3 1) true
          entry "e2" "t1" 5m (d 3 1) false
          entry "e3" "t2" 7m (d 3 1) true ]
    Assert.Equal(10m, balance "t1" (d 3 1) entries)

// ---- WI-0002: addEntry (RQ-FXL-0001, RQ-FXL-0002, RQ-FXL-0101) ----

let today = DateOnly(2026, 11, 16)

let withDesc (desc: string) (e: Entry) = { e with Description = desc }

[<Fact>]
let ``addEntry accepts amount 1000000 and rejects 1000000.01 and 0`` () =
    let ok = addEntry today (entry "n1" "t1" 1000000m today true) []
    Assert.True(Result.isOk ok)
    Assert.Equal<Result<Entry list, LedgerError>>(Error InvalidAmount, addEntry today (entry "n2" "t1" 1000000.01m today true) [])
    Assert.Equal<Result<Entry list, LedgerError>>(Error InvalidAmount, addEntry today (entry "n3" "t1" 0m today true) [])

[<Fact>]
let ``addEntry accepts a 200 character description and rejects 201`` () =
    let at200 = entry "n1" "t1" 5m today true |> withDesc (String('x', 200))
    let at201 = entry "n2" "t1" 5m today true |> withDesc (String('x', 201))
    Assert.True(Result.isOk (addEntry today at200 []))
    Assert.Equal<Result<Entry list, LedgerError>>(Error InvalidDescription, addEntry today at201 [])

[<Fact>]
let ``addEntry rejects control characters in the description`` () =
    let tab = entry "n1" "t1" 5m today true |> withDesc "a\tb"
    let lf = entry "n2" "t1" 5m today true |> withDesc "a\nb"
    Assert.Equal<Result<Entry list, LedgerError>>(Error InvalidDescription, addEntry today tab [])
    Assert.Equal<Result<Entry list, LedgerError>>(Error InvalidDescription, addEntry today lf [])

[<Fact>]
let ``addEntry accepts an entry dated today and rejects tomorrow as FutureDate`` () =
    Assert.True(Result.isOk (addEntry today (entry "n1" "t1" 5m today true) []))
    Assert.Equal<Result<Entry list, LedgerError>>(
        Error FutureDate,
        addEntry today (entry "n2" "t1" 5m (today.AddDays 1) true) [])

[<Fact>]
let ``addEntry rejects an id already used by another tenant as DuplicateId`` () =
    let existing = [ entry "n1" "t2" 5m today true ]
    Assert.Equal<Result<Entry list, LedgerError>>(
        Error DuplicateId,
        addEntry today (entry "n1" "t1" 5m today true) existing)

[<Fact>]
let ``addEntry stores the entry trimmed, unapproved and appended last`` () =
    let existing = [ entry "e0" "t1" 1m today true ]
    let input = entry "n1" "t1" 5m today true |> withDesc "  rent "
    match addEntry today input existing with
    | Error e -> failwithf "unexpected %A" e
    | Ok ledger ->
        Assert.Equal(2, ledger.Length)
        Assert.Equal("e0", ledger.[0].Id)
        let stored = ledger.[1]
        Assert.Equal("rent", stored.Description)
        Assert.False(stored.Approved)
        Assert.Equal(None, stored.ApprovedBy)

// ---- WI-0006: exportCsv (RQ-FXL-0007, RQ-FXL-0107) ----

[<Fact>]
let ``exportCsv of a tenant with no approved entries is just the header`` () =
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [ entry "e1" "t1" 5m (d 1 1) false ])

[<Fact>]
let ``exportCsv quotes a description containing a comma and doubles quotes`` () =
    let e = entry "e1" "t1" 10m (d 1 2) true |> withDesc "a,\"b\""
    Assert.Equal("id,date,amount,description\ne1,2026-01-02,10.00,\"a,\"\"b\"\"\"\n", exportCsv "t1" [ e ])

[<Fact>]
let ``exportCsv neutralises a leading formula character`` () =
    let f = entry "e1" "t1" 10m (d 1 2) true |> withDesc "=SUM(A1)"
    Assert.Equal("id,date,amount,description\ne1,2026-01-02,10.00,'=SUM(A1)\n", exportCsv "t1" [ f ])

[<Fact>]
let ``exportCsv applies the apostrophe before quoting`` () =
    let r = entry "e1" "t1" (-5m) (d 1 2) true |> withDesc "-5,x"
    Assert.Equal("id,date,amount,description\ne1,2026-01-02,-5.00,\"'-5,x\"\n", exportCsv "t1" [ r ])

[<Fact>]
let ``exportCsv orders by date then id and omits other tenants and unapproved entries`` () =
    let entries =
        [ entry "b" "t1" 1m (d 1 2) true
          entry "a" "t1" 2m (d 1 2) true
          entry "z" "t1" 3m (d 1 1) true
          entry "x" "t2" 4m (d 1 1) true
          entry "u" "t1" 6m (d 1 1) false ]
    let expected =
        "id,date,amount,description\n"
        + "z,2026-01-01,3.00,x\n"
        + "a,2026-01-02,2.00,x\n"
        + "b,2026-01-02,1.00,x\n"
    Assert.Equal(expected, exportCsv "t1" entries)

// ---- WI-0008: refund (RQ-FXL-0009, RQ-FXL-0109, DF-FXL-0006) ----

let now = DateTimeOffset(2026, 11, 16, 9, 0, 0, TimeSpan.Zero)

let sessionWith (roles: Role list) : Session =
    { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = now; LastSeen = now }

[<Fact>]
let ``refunds of 60 then 40 succeed and a further 0.01 is rejected`` () =
    let s = sessionWith [ Clerk ]
    let original = [ entry "o1" "t1" 100m (today.AddDays(-10)) true ]
    match refund now s today "r1" "o1" 60m original with
    | Error e -> failwithf "unexpected %A" e
    | Ok l1 ->
        match refund now s today "r2" "o1" 40m l1 with
        | Error e -> failwithf "unexpected %A" e
        | Ok l2 ->
            Assert.Equal<Result<Entry list, LedgerError>>(
                Error RefundExceedsOriginal,
                refund now s today "r3" "o1" 0.01m l2)

[<Fact>]
let ``a refund larger than the original is rejected`` () =
    let s = sessionWith [ Clerk ]
    let original = [ entry "o1" "t1" 100m (today.AddDays(-10)) true ]
    Assert.Equal<Result<Entry list, LedgerError>>(
        Error RefundExceedsOriginal,
        refund now s today "r1" "o1" 100.01m original)

[<Fact>]
let ``a successful refund is appended as an unapproved negative entry`` () =
    let s = sessionWith [ Admin ]
    let original = [ entry "o1" "t1" 100m (today.AddDays(-10)) true ]
    match refund now s today "r1" "o1" 25m original with
    | Error e -> failwithf "unexpected %A" e
    | Ok ledger ->
        let r = List.last ledger
        Assert.Equal("r1", r.Id)
        Assert.Equal(-25m, r.Amount)
        Assert.Equal("Refund of o1", r.Description)
        Assert.Equal(Some "o1", r.RefundOf)
        Assert.False(r.Approved)
        Assert.Equal("u1", r.CreatedBy)

[<Fact>]
let ``a refund dated 90 days after the original is allowed and 91 days is rejected`` () =
    let s = sessionWith [ Clerk ]
    let at90 = [ entry "o1" "t1" 100m (today.AddDays(-90)) true ]
    let at91 = [ entry "o1" "t1" 100m (today.AddDays(-91)) true ]
    Assert.True(Result.isOk (refund now s today "r1" "o1" 10m at90))
    Assert.Equal<Result<Entry list, LedgerError>>(
        Error(Unsupported "refund window closed"),
        refund now s today "r1" "o1" 10m at91)

[<Fact>]
let ``a refund by an expired session is rejected as SessionExpired`` () =
    let baseSession = sessionWith [ Clerk ]
    let s = { baseSession with LastSeen = now.AddMinutes(-16.0) }
    let original = [ entry "o1" "t1" 100m (today.AddDays(-10)) true ]
    Assert.Equal<Result<Entry list, LedgerError>>(
        Error SessionExpired,
        refund now s today "r1" "o1" 10m original)

[<Fact>]
let ``a Viewer may not refund`` () =
    let s = sessionWith [ Viewer ]
    let original = [ entry "o1" "t1" 100m (today.AddDays(-10)) true ]
    Assert.Equal<Result<Entry list, LedgerError>>(
        Error NotAuthorized,
        refund now s today "r1" "o1" 10m original)
