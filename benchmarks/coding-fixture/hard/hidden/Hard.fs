/// Hidden tests for the accepted amendments of the hard variant.
module Hidden.Hard

open Xunit
open Hidden.Common
open FxIdentity.Identity
open FxLedger.Ledger

module T2Hard =
    let fresh = { entry "n" "t1" 10m today false with Description = "rent" }

    [<Fact>]
    let ``control characters rejected`` () =
        Assert.True(addEntry today { fresh with Description = "a\tb" } [] |> isError InvalidDescription)
        Assert.True(addEntry today { fresh with Description = "a\nb" } [] |> isError InvalidDescription)

    [<Fact>]
    let ``control character check keeps precedence after amount`` () =
        Assert.True(addEntry today { fresh with Amount = 0m; Description = "a\tb" } [] |> isError InvalidAmount)

module T3Hard =
    let ledger = [ entry "big" "t1" 10000.01m (d 1 1) false; entry "edge" "t1" 10000m (d 1 1) false ]

    [<Fact>]
    let ``high value needs approver and admin`` () =
        Assert.True(approve t0 (session "b" "t1" [ Approver ]) "big" ledger |> isError NotAuthorized)
        Assert.True(approve t0 (session "b" "t1" [ Approver; Admin ]) "big" ledger |> Result.isOk)

    [<Fact>]
    let ``exactly ten thousand needs only approver`` () =
        Assert.True(approve t0 (session "b" "t1" [ Approver ]) "edge" ledger |> Result.isOk)

    [<Fact>]
    let ``high value check after not found and before self approval`` () =
        Assert.True(approve t0 (session "b" "t1" [ Approver ]) "zz" ledger |> isError NotFound)
        Assert.True(approve t0 (session "author" "t1" [ Approver ]) "big" ledger |> isError NotAuthorized)

module T6Hard =
    [<Fact>]
    let ``formula prefixes neutralised before quoting`` () =
        let es =
            [ { entry "a" "t1" 1m (d 1 1) true with Description = "=SUM(A1)" }
              { entry "b" "t1" 1m (d 1 2) true with Description = "-5,x" }
              { entry "c" "t1" 1m (d 1 3) true with Description = "@x" } ]
        let expected =
            "id,date,amount,description\n"
            + "a,2026-01-01,1.00,'=SUM(A1)\n"
            + "b,2026-01-02,1.00,\"'-5,x\"\n"
            + "c,2026-01-03,1.00,'@x\n"
        Assert.Equal(expected, exportCsv "t1" es)

module T8Hard =
    let clerk = session "clerk" "t1" [ Clerk ]

    let isUnsupported (r: Result<'a, LedgerError>) =
        match r with
        | Error(Unsupported _) -> true
        | _ -> false

    [<Fact>]
    let ``refund window is 90 days`` () =
        let at90 = [ entry "o" "t1" 100m (today.AddDays -90) true ]
        let at91 = [ entry "o" "t1" 100m (today.AddDays -91) true ]
        Assert.True(refund t0 clerk today "r1" "o" 1m at90 |> Result.isOk)
        Assert.True(refund t0 clerk today "r1" "o" 1m at91 |> isUnsupported)

    [<Fact>]
    let ``window checked after not found and before duplicate`` () =
        let at91 = [ entry "o" "t1" 100m (today.AddDays -91) true ]
        Assert.True(refund t0 clerk today "zz" "x" 1m at91 |> isError NotFound)
        Assert.True(refund t0 clerk today "o" "o" 1m at91 |> isUnsupported)
