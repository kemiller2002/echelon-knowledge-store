module Hidden.T2

open Xunit
open Hidden.Common
open FxLedger.Ledger

let fresh = { entry "n" "t1" 10m today false with Description = "rent" }

[<Fact>]
let ``amount bounds`` () =
    Assert.True(addEntry today { fresh with Amount = 1000000m } [] |> Result.isOk)
    Assert.True(addEntry today { fresh with Amount = 1000000.01m } [] |> isError InvalidAmount)
    Assert.True(addEntry today { fresh with Amount = 0m } [] |> isError InvalidAmount)
    Assert.True(addEntry today { fresh with Amount = -1m } [] |> isError InvalidAmount)

[<Fact>]
let ``description bounds after trimming`` () =
    Assert.True(addEntry today { fresh with Description = "  " + String.replicate 200 "a" + " " } [] |> Result.isOk)
    Assert.True(addEntry today { fresh with Description = String.replicate 201 "a" } [] |> isError InvalidDescription)
    Assert.True(addEntry today { fresh with Description = "   " } [] |> isError InvalidDescription)

[<Fact>]
let ``future date rejected and today accepted`` () =
    Assert.True(addEntry today fresh [] |> Result.isOk)
    Assert.True(addEntry today { fresh with Date = today.AddDays 1 } [] |> isError FutureDate)

[<Fact>]
let ``duplicate id across tenants rejected`` () =
    Assert.True(addEntry today fresh [ entry "n" "t2" 1m (d 1 1) true ] |> isError DuplicateId)

[<Fact>]
let ``error precedence amount description date duplicate`` () =
    let bad = { fresh with Amount = 0m; Description = ""; Date = today.AddDays 1 }
    Assert.True(addEntry today bad [ fresh ] |> isError InvalidAmount)
    Assert.True(addEntry today { bad with Amount = 1m } [ fresh ] |> isError InvalidDescription)
    Assert.True(addEntry today { bad with Amount = 1m; Description = "x" } [ fresh ] |> isError FutureDate)

[<Fact>]
let ``stored trimmed unapproved and appended`` () =
    let existing = entry "e" "t1" 1m (d 1 1) true
    let input = { fresh with Description = "  rent "; Approved = true; ApprovedBy = Some "x" }
    let result = ok (addEntry today input [ existing ])
    Assert.Equal<Entry list>([ existing; { fresh with Description = "rent" } ], result)
