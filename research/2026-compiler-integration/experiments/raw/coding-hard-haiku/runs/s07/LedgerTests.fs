module FxLedger.Tests.LedgerTests

open System
open Xunit
open FxIdentity.Identity
open FxLedger.Ledger

let entry id tenant amount (date: DateOnly) approved =
    { Id = id; TenantId = tenant; Amount = amount; Description = "x"; Date = date
      CreatedBy = "u9"; Approved = approved; ApprovedBy = None; RefundOf = None }

let d (m: int) (day: int) = DateOnly(2026, m, day)

let t0 = DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero)
let today = d 6 15
let session roles = { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = t0; LastSeen = t0 }

let isOk r = match r with Ok _ -> true | Error _ -> false
let errorOf r = match r with Error e -> Some e | Ok _ -> None
let okOrFail r = match r with Ok v -> v | Error e -> failwithf "unexpected error %A" e

[<Fact>]
let ``balance counts approved entries well before the cut-off`` () =
    let entries = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 2) false; entry "e3" "t2" 7m (d 1 1) true ]
    Assert.Equal(10m, balance "t1" (d 3 1) entries)

[<Fact>]
let ``balance excludes entries after the cut-off`` () =
    Assert.Equal(0m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 2 1) true ])

[<Fact>]
let ``an approved entry dated exactly the cut-off is included`` () =
    Assert.Equal(10m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 1 1) true ])

[<Fact>]
let ``amount 1000000 is accepted`` () =
    Assert.True(isOk (addEntry today (entry "n1" "t1" 1_000_000m today false) []))

[<Fact>]
let ``amount above 1000000 or not above zero is rejected with InvalidAmount`` () =
    Assert.Equal(Some InvalidAmount, errorOf (addEntry today (entry "n1" "t1" 1_000_000.01m today false) []))
    Assert.Equal(Some InvalidAmount, errorOf (addEntry today (entry "n1" "t1" 0m today false) []))

[<Fact>]
let ``a description of 200 characters after trimming is accepted`` () =
    let input = { (entry "n1" "t1" 5m today false) with Description = "  " + String('a', 200) + "  " }
    Assert.True(isOk (addEntry today input []))

[<Fact>]
let ``a description of 201 characters is rejected with InvalidDescription`` () =
    let input = { (entry "n1" "t1" 5m today false) with Description = String('a', 201) }
    Assert.Equal(Some InvalidDescription, errorOf (addEntry today input []))

[<Fact>]
let ``an entry dated today is accepted`` () =
    Assert.True(isOk (addEntry today (entry "n1" "t1" 5m today false) []))

[<Fact>]
let ``an entry dated tomorrow is rejected with FutureDate`` () =
    Assert.Equal(Some FutureDate, errorOf (addEntry today (entry "n1" "t1" 5m (today.AddDays 1) false) []))

[<Fact>]
let ``an entry whose id already exists is rejected with DuplicateId`` () =
    Assert.Equal(Some DuplicateId, errorOf (addEntry today (entry "e1" "t2" 5m today false) [ entry "e1" "t1" 5m today false ]))

[<Fact>]
let ``an added entry is stored trimmed, unapproved and appended last`` () =
    let existing = entry "e1" "t1" 3m (d 1 1) true
    let input = { (entry "n1" "t1" 5m today true) with Description = "  rent " }
    let result = okOrFail (addEntry today input [ existing ])
    Assert.Equal(2, result.Length)
    Assert.Equal(existing, result.Head)
    let stored = List.last result
    Assert.Equal("n1", stored.Id)
    Assert.False(stored.Approved)
    Assert.Equal(None, stored.ApprovedBy)
    Assert.Equal("rent", stored.Description)

[<Fact>]
let ``a tenant with no approved entries exports just the header line`` () =
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [ entry "e1" "t1" 5m (d 1 1) false ])

[<Fact>]
let ``a description containing a comma and quotes is quoted with doubled quotes`` () =
    let input = { (entry "e1" "t1" 5m (d 1 1) true) with Description = "a,\"b\"" }
    Assert.Equal("id,date,amount,description\ne1,2026-01-01,5.00,\"a,\"\"b\"\"\"\n", exportCsv "t1" [ input ])

[<Fact>]
let ``csv rows are ordered by date then id and amounts have two decimals`` () =
    let entries =
        [ entry "b" "t1" 1m (d 1 2) true; entry "a" "t1" -5m (d 1 2) true; entry "c" "t1" 2m (d 1 1) true
          entry "z" "t2" 9m (d 1 1) true ]
    Assert.Equal("id,date,amount,description\nc,2026-01-01,2.00,x\na,2026-01-02,-5.00,x\nb,2026-01-02,1.00,x\n", exportCsv "t1" entries)

let original = entry "o1" "t1" 100m (d 1 1) true

[<Fact>]
let ``refunds of 60 then 40 succeed and a further 0.01 is rejected`` () =
    let r1 = okOrFail (refund t0 (session [ Clerk ]) today "r1" "o1" 60m [ original ])
    let r2 = okOrFail (refund t0 (session [ Clerk ]) today "r2" "o1" 40m r1)
    Assert.Equal(Some RefundExceedsOriginal, errorOf (refund t0 (session [ Clerk ]) today "r3" "o1" 0.01m r2))

[<Fact>]
let ``a refund larger than the original is rejected`` () =
    Assert.Equal(Some RefundExceedsOriginal, errorOf (refund t0 (session [ Admin ]) today "r1" "o1" 100.01m [ original ]))

[<Fact>]
let ``a refund is appended unapproved with a negative amount and RefundOf set`` () =
    let result = okOrFail (refund t0 (session [ Clerk ]) today "r1" "o1" 25m [ original ])
    let refundEntry = List.last result
    Assert.Equal("r1", refundEntry.Id)
    Assert.Equal(-25m, refundEntry.Amount)
    Assert.Equal(Some "o1", refundEntry.RefundOf)
    Assert.Equal("Refund of o1", refundEntry.Description)
    Assert.False(refundEntry.Approved)
    Assert.Equal("u1", refundEntry.CreatedBy)

[<Fact>]
let ``a session holding only Viewer cannot refund`` () =
    Assert.Equal(Some NotAuthorized, errorOf (refund t0 (session [ Viewer; Approver ]) today "r1" "o1" 10m [ original ]))

[<Fact>]
let ``an expired session cannot refund`` () =
    Assert.Equal(Some SessionExpired, errorOf (refund (t0.AddMinutes 16.0) (session [ Clerk ]) today "r1" "o1" 10m [ original ]))
