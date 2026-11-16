module Hidden.T3

open Xunit
open Hidden.Common
open FxIdentity.Identity
open FxLedger.Ledger

let approver = session "boss" "t1" [ Approver ]
let ledger = [ entry "e1" "t1" 10m (d 1 1) false; entry "e2" "t2" 10m (d 1 1) false; entry "e3" "t1" 5m (d 1 1) true ]

[<Fact>]
let ``approves and records approver`` () =
    let result = ok (approve t0 approver "e1" ledger)
    Assert.Equal<Entry list>([ { ledger[0] with Approved = true; ApprovedBy = Some "boss" }; ledger[1]; ledger[2] ], result)

[<Fact>]
let ``idle boundary uses isActive`` () =
    Assert.True(approve (t0.AddMinutes 15.0) approver "e1" ledger |> Result.isOk)
    Assert.True(approve (t0.AddMinutes(15.0).AddSeconds 1.0) approver "e1" ledger |> isError SessionExpired)

[<Fact>]
let ``absolute lifetime enforced`` () =
    let s = { approver with IssuedAt = t0.AddHours -9.0 }
    Assert.True(approve t0 s "e1" ledger |> isError SessionExpired)

[<Fact>]
let ``admin alone is not approver`` () =
    Assert.True(approve t0 (session "a" "t1" [ Admin ]) "e1" ledger |> isError NotAuthorized)

[<Fact>]
let ``expired beats unauthorized`` () =
    Assert.True(approve (t0.AddHours 1.0) (session "a" "t1" [ Viewer ]) "e1" ledger |> isError SessionExpired)

[<Fact>]
let ``other tenant is not found`` () =
    Assert.True(approve t0 approver "e2" ledger |> isError NotFound)
    Assert.True(approve t0 approver "zz" ledger |> isError NotFound)

[<Fact>]
let ``self approval and already approved`` () =
    Assert.True(approve t0 (session "author" "t1" [ Approver ]) "e1" ledger |> isError SelfApproval)
    Assert.True(approve t0 approver "e3" ledger |> isError AlreadyApproved)
