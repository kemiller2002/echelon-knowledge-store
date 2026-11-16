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

open FxIdentity.Identity

let now0 = DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero)

let session roles idle =
    { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = now0.AddMinutes(-60.0); LastSeen = now0 - idle }

[<Fact>]
let ``total rounds half away from zero`` () =
    Assert.Equal(0.01m, total "t1" [ entry "a" "t1" 0.005m (d 1 1) true ])
    Assert.Equal(-0.01m, total "t1" [ entry "a" "t1" -0.005m (d 1 1) true ])

[<Fact>]
let ``total ignores unapproved and other tenants`` () =
    Assert.Equal(3m, total "t1" [ entry "a" "t1" 3m (d 1 1) true; entry "b" "t1" 4m (d 1 1) false; entry "c" "t2" 5m (d 1 1) true ])

[<Fact>]
let ``approve succeeds at exactly 15 minutes idle`` () =
    let r = approve now0 (session [ Approver ] (TimeSpan.FromMinutes 15.0)) "e1" [ entry "e1" "t1" 1m (d 1 1) false ]
    match r with
    | Ok [ e ] -> Assert.True e.Approved; Assert.Equal(Some "u1", e.ApprovedBy)
    | _ -> failwith "expected Ok"

[<Fact>]
let ``approve expired at 15 minutes 1 second`` () =
    let r = approve now0 (session [ Approver ] (TimeSpan.FromSeconds 901.0)) "e1" [ entry "e1" "t1" 1m (d 1 1) false ]
    Assert.Equal(Error SessionExpired, r)

[<Fact>]
let ``approve admin without approver not authorized`` () =
    let r = approve now0 (session [ Admin ] TimeSpan.Zero) "e1" [ entry "e1" "t1" 1m (d 1 1) false ]
    Assert.Equal(Error NotAuthorized, r)

[<Fact>]
let ``approve other tenant is not found`` () =
    let r = approve now0 (session [ Approver ] TimeSpan.Zero) "e1" [ entry "e1" "t2" 1m (d 1 1) false ]
    Assert.Equal(Error NotFound, r)

[<Fact>]
let ``approve own entry is self approval`` () =
    let e = { entry "e1" "t1" 1m (d 1 1) false with CreatedBy = "u1" }
    Assert.Equal(Error SelfApproval, approve now0 (session [ Approver ] TimeSpan.Zero) "e1" [ e ])

[<Fact>]
let ``approve already approved`` () =
    let r = approve now0 (session [ Approver ] TimeSpan.Zero) "e1" [ entry "e1" "t1" 1m (d 1 1) true ]
    Assert.Equal(Error AlreadyApproved, r)

[<Fact>]
let ``view admin sees only own tenant`` () =
    let es = [ entry "a" "t1" 1m (d 1 1) true; entry "b" "t2" 1m (d 1 1) true; entry "c" "t1" 1m (d 1 1) false ]
    match view now0 (session [ Admin ] TimeSpan.Zero) es with
    | Ok l -> Assert.Equal<string list>([ "a"; "c" ], l |> List.map _.Id)
    | _ -> failwith "expected Ok"

[<Fact>]
let ``view expired session`` () =
    Assert.Equal(Error SessionExpired, view now0 (session [ Admin ] (TimeSpan.FromHours 1.0)) [])
