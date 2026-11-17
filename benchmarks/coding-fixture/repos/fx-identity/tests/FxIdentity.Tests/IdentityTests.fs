module FxIdentity.Tests.IdentityTests

open System
open Xunit
open FxIdentity.Identity

let t0 = DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero)

let session =
    { UserId = "u1"; TenantId = "t1"; Roles = [ Admin ]; IssuedAt = t0; LastSeen = t0 }

[<Fact>]
let ``exactly 15 minutes idle is active`` () =
    Assert.True(isActive (t0.AddMinutes 15.0) session)

[<Fact>]
let ``admin does not hold approver`` () = Assert.False(hasRole Approver session)
