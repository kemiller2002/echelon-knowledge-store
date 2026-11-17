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

// ---- WI-0002 addEntry ----
let today = d 6 15
let newEntry id amount desc date =
    { entry id "t1" amount date false with Description = desc }

[<Fact>]
let ``addEntry accepts amount 1000000`` () =
    Assert.True(Result.isOk (addEntry today (newEntry "a" 1000000m "x" today) []))

[<Fact>]
let ``addEntry rejects amount 1000000.01 and 0 with InvalidAmount`` () =
    Assert.Equal(Error InvalidAmount, addEntry today (newEntry "a" 1000000.01m "x" today) [])
    Assert.Equal(Error InvalidAmount, addEntry today (newEntry "a" 0m "x" today) [])

[<Fact>]
let ``addEntry description length 200 after trim accepted and 201 rejected`` () =
    Assert.True(Result.isOk (addEntry today (newEntry "a" 1m (" " + String('a', 200) + " ") today) []))
    Assert.Equal(Error InvalidDescription, addEntry today (newEntry "a" 1m (String('a', 201)) today) [])
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
let ``addEntry reports first failing rule in order amount description date duplicate`` () =
    let existing = [ entry "a" "t1" 1m today true ]
    let bad = newEntry "a" 0m "" (today.AddDays 1)
    Assert.Equal(Error InvalidAmount, addEntry today bad existing)
    Assert.Equal(Error InvalidDescription, addEntry today { bad with Amount = 1m } existing)
    Assert.Equal(Error FutureDate, addEntry today { bad with Amount = 1m; Description = "x" } existing)
    Assert.Equal(Error DuplicateId, addEntry today { bad with Amount = 1m; Description = "x"; Date = today } existing)

[<Fact>]
let ``addEntry stores trimmed unapproved entry appended last`` () =
    let existing = [ entry "e0" "t1" 1m today true ]
    let e = { newEntry "a" 5m "  rent " today with Approved = true; ApprovedBy = Some "u1" }
    match addEntry today e existing with
    | Ok l ->
        Assert.Equal(2, l.Length)
        Assert.Equal(existing.Head, l.Head)
        let last = List.last l
        Assert.Equal("rent", last.Description)
        Assert.False(last.Approved)
        Assert.Equal(None, last.ApprovedBy)
        Assert.Equal("a", last.Id)
    | Error e -> failwithf "unexpected %A" e

[<Fact>]
let ``addEntry rejects control characters in description`` () =
    Assert.Equal(Error InvalidDescription, addEntry today (newEntry "a" 1m "a\tb" today) [])
    Assert.Equal(Error InvalidDescription, addEntry today (newEntry "a" 1m "a\nb" today) [])
    Assert.True(Result.isOk (addEntry today (newEntry "a" 1m "\tab\n" today) []))

[<Fact>]
let ``addEntry control character keeps description position in order`` () =
    Assert.Equal(Error InvalidAmount, addEntry today (newEntry "a" 0m "a\tb" today) [])
    Assert.Equal(Error InvalidDescription, addEntry today (newEntry "a" 1m "a\tb" (today.AddDays 1)) [])

// ---- WI-0004 balance ----
[<Fact>]
let ``balance includes approved entry dated exactly on cut-off`` () =
    Assert.Equal(10m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 1 1) true ])

[<Fact>]
let ``balance excludes unapproved other tenants and later entries`` () =
    let es =
        [ entry "e1" "t1" 10m (d 1 1) true
          entry "e2" "t1" 5m (d 1 1) false
          entry "e3" "t2" 7m (d 1 1) true
          entry "e4" "t1" 3m (d 1 2) true ]
    Assert.Equal(10m, balance "t1" (d 1 1) es)

// ---- WI-0006 exportCsv ----
let approvedWith id date amount desc =
    { entry id "t1" amount date true with Description = desc }

[<Fact>]
let ``exportCsv with no approved entries is just the header`` () =
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [ entry "e1" "t1" 1m (d 1 1) false; entry "e2" "t2" 1m (d 1 1) true ])
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [])

[<Fact>]
let ``exportCsv quotes description a,"b"`` () =
    let csv = exportCsv "t1" [ approvedWith "e1" (d 1 1) 5m "a,\"b\"" ]
    Assert.Equal("id,date,amount,description\ne1,2026-01-01,5.00,\"a,\"\"b\"\"\"\n", csv)

[<Fact>]
let ``exportCsv formats rows ordered by date then id with refund negative`` () =
    let es =
        [ approvedWith "b" (d 1 2) 1.5m "x"
          approvedWith "B" (d 1 2) -5m "y"
          approvedWith "a" (d 1 3) 10m "z"
          approvedWith "c" (d 1 1) 2m "line\nbreak" ]
    let expected =
        "id,date,amount,description\nc,2026-01-01,2.00,\"line\nbreak\"\nB,2026-01-02,-5.00,y\nb,2026-01-02,1.50,x\na,2026-01-03,10.00,z\n"
    Assert.Equal(expected, exportCsv "t1" es)

[<Fact>]
let ``exportCsv neutralises formulas with leading apostrophe before quoting`` () =
    let es =
        [ approvedWith "e1" (d 1 1) 1m "=SUM(A1)"
          approvedWith "e2" (d 1 2) 1m "+1"
          approvedWith "e3" (d 1 3) 1m "-1"
          approvedWith "e4" (d 1 4) 1m "@x"
          approvedWith "e5" (d 1 5) 1m "=a,b"
          approvedWith "e6" (d 1 6) 1m "a=b" ]
    let expected =
        "id,date,amount,description\ne1,2026-01-01,1.00,'=SUM(A1)\ne2,2026-01-02,1.00,'+1\ne3,2026-01-03,1.00,'-1\ne4,2026-01-04,1.00,'@x\ne5,2026-01-05,1.00,\"'=a,b\"\ne6,2026-01-06,1.00,a=b\n"
    Assert.Equal(expected, exportCsv "t1" es)

// ---- WI-0008 refund ----
open FxIdentity.Identity

let now = DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero)
let session roles =
    { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = now.AddHours -1.0; LastSeen = now.AddMinutes -1.0 }
let orig = entry "o1" "t1" 100m (d 6 1) true
let doRefund s id amt es = refund now s today id "o1" amt es

[<Fact>]
let ``refund of 60 then 40 succeed and further 0.01 rejected`` () =
    let s = session [ Clerk ]
    let l1 = doRefund s "r1" 60m [ orig ] |> Result.defaultWith (fun _ -> failwith "r1")
    let l2 = doRefund s "r2" 40m l1 |> Result.defaultWith (fun _ -> failwith "r2")
    Assert.Equal(Error RefundExceedsOriginal, doRefund s "r3" 0.01m l2)

[<Fact>]
let ``refund larger than original is rejected`` () =
    Assert.Equal(Error RefundExceedsOriginal, doRefund (session [ Admin ]) "r1" 100.01m [ orig ])

[<Fact>]
let ``refund appends entry in specified form`` () =
    match doRefund (session [ Clerk ]) "r1" 25m [ orig ] with
    | Ok [ o; r ] ->
        Assert.Equal(orig, o)
        Assert.Equal({ Id = "r1"; TenantId = "t1"; Amount = -25m; Description = "Refund of o1"; Date = today
                       CreatedBy = "u1"; Approved = false; ApprovedBy = None; RefundOf = Some "o1" }, r)
    | x -> failwithf "unexpected %A" x

[<Fact>]
let ``refund checks session roles original and ids in order`` () =
    let expired = { session [ Clerk ] with LastSeen = now.AddMinutes -16.0 }
    Assert.Equal(Error SessionExpired, doRefund expired "r1" 1m [ orig ])
    Assert.Equal(Error NotAuthorized, doRefund (session [ Viewer; Approver ]) "r1" 1m [ orig ])
    let s = session [ Clerk ]
    Assert.Equal(Error NotFound, doRefund s "r1" 1m [])
    Assert.Equal(Error NotFound, doRefund s "r1" 1m [ { orig with TenantId = "t2" } ])
    Assert.Equal(Error NotFound, doRefund s "r1" 1m [ { orig with Approved = false } ])
    Assert.Equal(Error NotFound, doRefund s "r1" 1m [ { orig with RefundOf = Some "z" } ])
    Assert.Equal(Error DuplicateId, doRefund s "o1" 0m [ orig ])
    Assert.Equal(Error InvalidAmount, doRefund s "r1" 0m [ orig ])
    Assert.Equal(Error InvalidAmount, doRefund s "r1" -1m [ orig ])

[<Fact>]
let ``refund window closes after 90 days`` () =
    let s = session [ Clerk ]
    let old = { orig with Date = today.AddDays -90 }
    Assert.True(Result.isOk (doRefund s "r1" 1m [ old ]))
    let older = { orig with Date = today.AddDays -91 }
    Assert.Equal(Error(Unsupported "refund window closed"), doRefund s "r1" 1m [ older ])
    // window check precedes duplicate-id check
    Assert.Equal(Error(Unsupported "refund window closed"), doRefund s "o1" 1m [ older ])
