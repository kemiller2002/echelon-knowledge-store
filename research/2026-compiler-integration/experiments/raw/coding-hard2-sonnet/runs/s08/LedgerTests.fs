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

// ---- WI-0001 total ----
[<Fact>]
let ``total rounds approved 0.005 to 0.01`` () =
    Assert.Equal(0.01m, total "t1" [ entry "e1" "t1" 0.005m (d 1 1) true ])

[<Fact>]
let ``total rounds approved -0.005 to -0.01`` () =
    Assert.Equal(-0.01m, total "t1" [ entry "e1" "t1" -0.005m (d 1 1) true ])

[<Fact>]
let ``total ignores unapproved and other tenants`` () =
    let es = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 1) false; entry "e3" "t2" 7m (d 1 1) true ]
    Assert.Equal(10m, total "t1" es)

// ---- WI-0003 approve ----
open FxIdentity.Identity

let t0 = DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero)
let session roles lastSeen =
    { UserId = "approver1"; TenantId = "t1"; Roles = roles; IssuedAt = t0 - TimeSpan.FromHours 1.0; LastSeen = lastSeen }
let fresh roles = session roles t0
let pending id amount = entry id "t1" amount (d 1 1) false

[<Fact>]
let ``approve allows session idle exactly 15 minutes`` () =
    let s = session [ Approver ] (t0 - TimeSpan.FromMinutes 15.0)
    match approve t0 s "e1" [ pending "e1" 10m ] with
    | Ok [ e ] ->
        Assert.True e.Approved
        Assert.Equal(Some "approver1", e.ApprovedBy)
    | r -> failwithf "unexpected %A" r

[<Fact>]
let ``approve gives SessionExpired at 15 minutes 1 second idle`` () =
    let s = session [ Approver ] (t0 - TimeSpan.FromSeconds 901.0)
    Assert.Equal(Error SessionExpired, approve t0 s "e1" [ pending "e1" 10m ])

[<Fact>]
let ``approve Admin without Approver is NotAuthorized`` () =
    Assert.Equal(Error NotAuthorized, approve t0 (fresh [ Admin ]) "e1" [ pending "e1" 10m ])

[<Fact>]
let ``approve other tenant entry is NotFound`` () =
    let e = { pending "e1" 10m with TenantId = "t2" }
    Assert.Equal(Error NotFound, approve t0 (fresh [ Approver ]) "e1" [ e ])

[<Fact>]
let ``approve own entry is SelfApproval`` () =
    let e = { pending "e1" 10m with CreatedBy = "approver1" }
    Assert.Equal(Error SelfApproval, approve t0 (fresh [ Approver ]) "e1" [ e ])

[<Fact>]
let ``approve already approved is AlreadyApproved`` () =
    let e = { pending "e1" 10m with Approved = true }
    Assert.Equal(Error AlreadyApproved, approve t0 (fresh [ Approver ]) "e1" [ e ])

[<Fact>]
let ``approve high value needs Admin too`` () =
    Assert.Equal(Error NotAuthorized, approve t0 (fresh [ Approver ]) "e1" [ pending "e1" 10000.01m ])
    match approve t0 (fresh [ Approver; Admin ]) "e1" [ pending "e1" 10000.01m ] with
    | Ok _ -> ()
    | r -> failwithf "unexpected %A" r

[<Fact>]
let ``approve exactly 10000 needs only Approver`` () =
    match approve t0 (fresh [ Approver ]) "e1" [ pending "e1" 10000m ] with
    | Ok _ -> ()
    | r -> failwithf "unexpected %A" r

// ---- WI-0005 view ----
[<Fact>]
let ``view Admin sees only own tenant in order`` () =
    let es = [ entry "a" "t1" 1m (d 1 1) true; entry "b" "t2" 1m (d 1 1) true; entry "c" "t1" 1m (d 1 1) false ]
    match view t0 (fresh [ Admin ]) es with
    | Ok r -> Assert.Equal<string list>([ "a"; "c" ], r |> List.map _.Id)
    | r -> failwithf "unexpected %A" r

[<Fact>]
let ``view expired session gets SessionExpired`` () =
    let s = session [ Admin ] (t0 - TimeSpan.FromMinutes 16.0)
    Assert.Equal(Error SessionExpired, view t0 s [ entry "a" "t1" 1m (d 1 1) true ])
