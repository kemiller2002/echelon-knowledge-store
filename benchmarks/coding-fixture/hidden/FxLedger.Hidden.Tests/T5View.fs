module Hidden.T5

open Xunit
open Hidden.Common
open FxIdentity.Identity
open FxLedger.Ledger

let ledger = [ entry "a" "t1" 1m (d 1 1) true; entry "b" "t2" 2m (d 1 1) true; entry "c" "t1" 3m (d 1 2) false ]

[<Fact>]
let ``admin sees only own tenant`` () =
    Assert.Equal<Entry list>([ ledger[0]; ledger[2] ], ok (view t0 (session "a" "t1" [ Admin ]) ledger))

[<Fact>]
let ``viewer sees own tenant`` () =
    Assert.Equal<Entry list>([ ledger[1] ], ok (view t0 (session "v" "t2" [ Viewer ]) ledger))

[<Fact>]
let ``expired session rejected`` () =
    Assert.True(view (t0.AddMinutes 16.0) (session "a" "t1" [ Admin ]) ledger |> isError SessionExpired)
