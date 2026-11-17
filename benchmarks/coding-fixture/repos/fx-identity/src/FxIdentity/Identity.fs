module FxIdentity.Identity

open System

type Role =
    | Viewer
    | Clerk
    | Approver
    | Admin

type Session =
    { UserId: string
      TenantId: string
      Roles: Role list
      IssuedAt: DateTimeOffset
      LastSeen: DateTimeOffset }

/// See RQ-FXID-0002.
let idleTimeout = TimeSpan.FromMinutes 15.0

/// See RQ-FXID-0002.
let maxLifetime = TimeSpan.FromHours 8.0

/// Whether the session may be used at `now` (RQ-FXID-0002).
let isActive (now: DateTimeOffset) (s: Session) =
    now - s.LastSeen <= idleTimeout && now - s.IssuedAt <= maxLifetime

/// Whether the session holds exactly this role (RQ-FXID-0001).
let hasRole (role: Role) (s: Session) = List.contains role s.Roles
