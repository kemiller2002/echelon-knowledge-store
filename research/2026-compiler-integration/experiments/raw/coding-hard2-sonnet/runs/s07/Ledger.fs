module FxLedger.Ledger

open System
open FxIdentity.Identity

type Entry =
    { Id: string
      TenantId: string
      Amount: decimal
      Description: string
      Date: DateOnly
      CreatedBy: string
      Approved: bool
      ApprovedBy: string option
      RefundOf: string option }

type LedgerError =
    | InvalidAmount
    | InvalidDescription
    | FutureDate
    | DuplicateId
    | NotAuthorized
    | SessionExpired
    | NotFound
    | SelfApproval
    | AlreadyApproved
    | RefundExceedsOriginal
    | Unsupported of string

/// Work item WI-0001.
let total (tenantId: string) (entries: Entry list) : decimal = failwith "not implemented"

/// Work item WI-0002. `today` is the caller's current date.
let addEntry (today: DateOnly) (entry: Entry) (entries: Entry list) : Result<Entry list, LedgerError> =
    let desc = if isNull entry.Description then "" else entry.Description.Trim()
    if entry.Amount <= 0m || entry.Amount > 1000000m then
        Error InvalidAmount
    elif desc.Length = 0 || desc.Length > 200 || desc |> Seq.exists Char.IsControl then
        Error InvalidDescription
    elif entry.Date > today then
        Error FutureDate
    elif entries |> List.exists (fun e -> e.Id = entry.Id) then
        Error DuplicateId
    else
        Ok(entries @ [ { entry with Description = desc; Approved = false; ApprovedBy = None } ])

/// Work item WI-0003. Returns the ledger with the entry approved.
let approve
    (now: DateTimeOffset)
    (session: Session)
    (entryId: string)
    (entries: Entry list)
    : Result<Entry list, LedgerError> =
    failwith "not implemented"

/// Balance of a tenant's approved entries dated up to `until`.
let balance (tenantId: string) (until: DateOnly) (entries: Entry list) : decimal =
    entries
    |> List.filter (fun e -> e.TenantId = tenantId && e.Approved && e.Date <= until)
    |> List.sumBy _.Amount

/// Work item WI-0005. Entries the session may see.
let view (now: DateTimeOffset) (session: Session) (entries: Entry list) : Result<Entry list, LedgerError> =
    failwith "not implemented"

/// Work item WI-0006.
let exportCsv (tenantId: string) (entries: Entry list) : string =
    let inv = Globalization.CultureInfo.InvariantCulture
    let field (desc: string) =
        let d =
            if desc.Length > 0 && (desc[0] = '=' || desc[0] = '+' || desc[0] = '-' || desc[0] = '@') then
                "'" + desc
            else
                desc
        if d.IndexOfAny [| ','; '"'; '\r'; '\n' |] >= 0 then
            "\"" + d.Replace("\"", "\"\"") + "\""
        else
            d
    let rows =
        entries
        |> List.filter (fun e -> e.TenantId = tenantId && e.Approved)
        |> List.sortWith (fun a b ->
            let c = compare a.Date b.Date
            if c <> 0 then c else String.CompareOrdinal(a.Id, b.Id))
        |> List.map (fun e ->
            String.Join(",", [ e.Id; e.Date.ToString("yyyy-MM-dd", inv); e.Amount.ToString("F2", inv); field e.Description ])
            + "\n")
    "id,date,amount,description\n" + String.Join("", rows)

/// Work item WI-0007. Converts an amount between ISO 4217 currencies.
let convertCurrency (amount: decimal) (fromCurrency: string) (toCurrency: string) : Result<decimal, LedgerError> =
    failwith "not implemented"

/// Work item WI-0008. Records a refund of `amount` against entry `originalId`.
let refund
    (now: DateTimeOffset)
    (session: Session)
    (today: DateOnly)
    (refundId: string)
    (originalId: string)
    (amount: decimal)
    (entries: Entry list)
    : Result<Entry list, LedgerError> =
    if not (isActive now session) then
        Error SessionExpired
    elif not (hasRole Clerk session || hasRole Admin session) then
        Error NotAuthorized
    else
        match
            entries
            |> List.tryFind (fun e ->
                e.Id = originalId && e.TenantId = session.TenantId && e.Approved && e.RefundOf.IsNone)
        with
        | None -> Error NotFound
        | Some original ->
            if today.DayNumber - original.Date.DayNumber > 90 then
                Error(Unsupported "refund window closed")
            elif entries |> List.exists (fun e -> e.Id = refundId) then
                Error DuplicateId
            elif amount <= 0m then
                Error InvalidAmount
            else
                let already =
                    entries
                    |> List.filter (fun e -> e.RefundOf = Some originalId)
                    |> List.sumBy (fun e -> -e.Amount)
                if already + amount > original.Amount then
                    Error RefundExceedsOriginal
                else
                    Ok(
                        entries
                        @ [ { Id = refundId
                              TenantId = session.TenantId
                              Amount = -amount
                              Description = "Refund of " + originalId
                              Date = today
                              CreatedBy = session.UserId
                              Approved = false
                              ApprovedBy = None
                              RefundOf = Some originalId } ]
                    )
