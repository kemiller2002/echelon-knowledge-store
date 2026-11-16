module FxLedger.Tests.LedgerTests

open System
open Xunit
open FxIdentity.Identity
open FxLedger.Ledger

let entry id tenant amount (date: DateOnly) approved =
    { Id = id; TenantId = tenant; Amount = amount; Description = "x"; Date = date
      CreatedBy = "u9"; Approved = approved; ApprovedBy = None; RefundOf = None }

let d (m: int) (day: int) = DateOnly(2026, m, day)

let today = DateOnly(2026, 11, 16)

let addResult (e: Entry) (entries: Entry list) : Result<Entry list, LedgerError> = addEntry today e entries

let withDescription (text: string) (e: Entry) = { e with Description = text }

let assertError (expected: LedgerError) (actual: Result<Entry list, LedgerError>) =
    Assert.Equal(Error expected, actual)

let assertRefundError (expected: LedgerError) (actual: Result<Entry list, LedgerError>) = assertError expected actual

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
let ``balance excludes unapproved entries and other tenants on the cut-off date`` () =
    let entries = [ entry "e1" "t1" 10m (d 1 1) false; entry "e2" "t2" 7m (d 1 1) true ]
    Assert.Equal(0m, balance "t1" (d 1 1) entries)

[<Fact>]
let ``addEntry accepts an amount of 1,000,000 and rejects 1,000,000.01 and 0`` () =
    Assert.True((addResult (entry "e1" "t1" 1000000m (d 1 1) false) []).IsOk)
    assertError InvalidAmount (addResult (entry "e1" "t1" 1000000.01m (d 1 1) false) [])
    assertError InvalidAmount (addResult (entry "e1" "t1" 0m (d 1 1) false) [])

[<Fact>]
let ``addEntry accepts a 200 character description and rejects 201`` () =
    let ok = String('a', 200)
    Assert.True((addResult (entry "e1" "t1" 1m (d 1 1) false |> withDescription ok) []).IsOk)
    assertError InvalidDescription (addResult (entry "e1" "t1" 1m (d 1 1) false |> withDescription (String('a', 201))) [])

[<Fact>]
let ``addEntry measures the description length after trimming`` () =
    let padded = "  " + String('a', 200) + "  "
    Assert.True((addResult (entry "e1" "t1" 1m (d 1 1) false |> withDescription padded) []).IsOk)

[<Fact>]
let ``addEntry rejects an empty description after trimming`` () =
    assertError InvalidDescription (addResult (entry "e1" "t1" 1m (d 1 1) false |> withDescription "   ") [])

[<Fact>]
let ``addEntry accepts an entry dated today and rejects tomorrow with FutureDate`` () =
    Assert.True((addResult (entry "e1" "t1" 1m today false) []).IsOk)
    assertError FutureDate (addResult (entry "e1" "t1" 1m (today.AddDays 1) false) [])

[<Fact>]
let ``addEntry rejects control characters in descriptions`` () =
    assertError InvalidDescription (addResult (entry "e1" "t1" 1m (d 1 1) false |> withDescription "a\tb") [])
    assertError InvalidDescription (addResult (entry "e1" "t1" 1m (d 1 1) false |> withDescription "a\nb") [])

[<Fact>]
let ``addEntry rejects a duplicate id from any tenant`` () =
    assertError DuplicateId (addResult (entry "e1" "t2" 1m (d 1 1) false) [ entry "e1" "t1" 1m (d 1 1) true ])

[<Fact>]
let ``addEntry reports the first failing rule in DF-FXL-0001 order`` () =
    // amount fails and so does the date: amount is reported
    assertError InvalidAmount (addResult (entry "e1" "t1" 0m (today.AddDays 1) false) [])
    // description fails and the id is a duplicate: description is reported
    assertError InvalidDescription (addResult (entry "e1" "t1" 1m (d 1 1) false |> withDescription "" ) [ entry "e1" "t1" 1m (d 1 1) true ])

[<Fact>]
let ``addEntry stores trimmed unapproved entries appended last`` () =
    let existing = [ entry "e0" "t1" 1m (d 1 1) true ]
    let incoming = { entry "e1" "t1" 5m (d 1 2) true with Description = "  rent " }
    match addResult incoming existing with
    | Ok [ first; stored ] ->
        Assert.Equal("e0", first.Id)
        Assert.Equal("e1", stored.Id)
        Assert.Equal("rent", stored.Description)
        Assert.False(stored.Approved)
        Assert.Equal(None, stored.ApprovedBy)
    | other -> Assert.Fail(sprintf "unexpected result: %A" other)

[<Fact>]
let ``exportCsv writes only the header for a tenant with no approved entries`` () =
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [ entry "e1" "t1" 1m (d 1 1) false ])

[<Fact>]
let ``exportCsv quotes descriptions and doubles embedded quotes`` () =
    let csv = exportCsv "t1" [ entry "e1" "t1" 1m (d 1 1) true |> withDescription "a,\"b\"" ]
    Assert.Equal("id,date,amount,description\ne1,2026-01-01,1.00,\"a,\"\"b\"\"\"\n", csv)

[<Fact>]
let ``exportCsv orders rows by date then id and writes invariant amounts`` () =
    let csv =
        exportCsv "t1"
            [ entry "b" "t1" 5.5m (d 1 2) true
              entry "a" "t1" 1m (d 1 2) true
              entry "c" "t1" 2m (d 1 1) true
              entry "x" "t2" 9m (d 1 1) true ]
    Assert.Equal("id,date,amount,description\nc,2026-01-01,2.00,x\na,2026-01-02,1.00,x\nb,2026-01-02,5.50,x\n", csv)

[<Fact>]
let ``exportCsv writes a negative refund amount with two decimals`` () =
    let csv = exportCsv "t1" [ entry "r1" "t1" -5m (d 1 1) true ]
    Assert.Equal("id,date,amount,description\nr1,2026-01-01,-5.00,x\n", csv)

[<Fact>]
let ``exportCsv neutralises leading formula characters before quoting`` () =
    let csv =
        exportCsv "t1"
            [ entry "e1" "t1" 1m (d 1 1) true |> withDescription "=SUM(A1)"
              entry "e2" "t1" 1m (d 1 2) true |> withDescription "-5,x" ]
    Assert.Equal("id,date,amount,description\ne1,2026-01-01,1.00,'=SUM(A1)\ne2,2026-01-02,1.00,\"'-5,x\"\n", csv)

let session (roles: Role list) : Session =
    let now = DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.Zero)
    { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = now.AddHours(-1.0); LastSeen = now.AddMinutes(-1.0) }

let now = DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.Zero)

let refundResult (roles: Role list) (refundId: string) (amount: decimal) (entries: Entry list) =
    refund now (session roles) today refundId "o1" amount entries

[<Fact>]
let ``refund rejects refunds that exceed the remainder of the original`` () =
    let original = [ entry "o1" "t1" 100m (d 11 1) true ]
    let afterFirst = refundResult [ Clerk ] "r1" 60m original |> Result.defaultWith (fun _ -> failwith "first refund failed")
    let afterSecond = refundResult [ Clerk ] "r2" 40m afterFirst |> Result.defaultWith (fun _ -> failwith "second refund failed")
    assertRefundError RefundExceedsOriginal (refundResult [ Clerk ] "r3" 0.01m afterSecond)

[<Fact>]
let ``refund appends the refund entry with the expected fields`` () =
    let original = [ entry "o1" "t1" 100m (d 11 1) true ]
    match refundResult [ Admin ] "r1" 60m original with
    | Ok [ _; r ] ->
        Assert.Equal("r1", r.Id)
        Assert.Equal("t1", r.TenantId)
        Assert.Equal(-60m, r.Amount)
        Assert.Equal("Refund of o1", r.Description)
        Assert.Equal(today, r.Date)
        Assert.Equal("u1", r.CreatedBy)
        Assert.False(r.Approved)
        Assert.Equal(None, r.ApprovedBy)
        Assert.Equal(Some "o1", r.RefundOf)
    | other -> Assert.Fail(sprintf "unexpected result: %A" other)

[<Fact>]
let ``refund requires an active session`` () =
    let s = { (session [ Clerk ]) with LastSeen = now.AddMinutes(-16.0) }
    let result = refund now s today "r1" "o1" 1m [ entry "o1" "t1" 100m (d 11 1) true ]
    assertRefundError SessionExpired result

[<Fact>]
let ``refund requires the Clerk or Admin role`` () =
    assertRefundError NotAuthorized (refundResult [ Viewer; Approver ] "r1" 1m [ entry "o1" "t1" 100m (d 11 1) true ])

[<Fact>]
let ``refund rejects originals that are unapproved, refunds, or in another tenant`` () =
    assertRefundError NotFound (refundResult [ Clerk ] "r1" 1m [ entry "o1" "t1" 100m (d 11 1) false ])
    let refundOfO1 = { entry "o1" "t1" -1m (d 11 1) true with RefundOf = Some "o0" }
    assertRefundError NotFound (refundResult [ Clerk ] "r1" 1m [ refundOfO1 ])
    assertRefundError NotFound (refundResult [ Clerk ] "r1" 1m [ entry "o1" "t2" 100m (d 11 1) true ])

[<Fact>]
let ``refund allows an original dated exactly 90 days before today`` () =
    let original = [ entry "o1" "t1" 100m (today.AddDays(-90)) true ]
    Assert.True((refundResult [ Clerk ] "r1" 1m original).IsOk)

[<Fact>]
let ``refund rejects an original dated 91 days before today as window closed`` () =
    let original = [ entry "o1" "t1" 100m (today.AddDays(-91)) true ]
    assertRefundError (Unsupported "refund window closed") (refundResult [ Clerk ] "r1" 1m original)

[<Fact>]
let ``refund rejects a duplicate refund id`` () =
    let original = [ entry "o1" "t1" 100m (d 11 1) true; entry "r1" "t1" 1m (d 11 1) false ]
    assertRefundError DuplicateId (refundResult [ Clerk ] "r1" 1m original)
