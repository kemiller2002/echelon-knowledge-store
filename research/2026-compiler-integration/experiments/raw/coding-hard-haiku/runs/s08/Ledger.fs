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

let private maxAmount = 1_000_000m
let private maxDescriptionLength = 200
let private refundWindowDays = 90

/// Work item WI-0002. `today` is the caller's current date.
/// Checks in the DF-FXL-0001 order: amount, description, date, duplicate id.
let addEntry (today: DateOnly) (entry: Entry) (entries: Entry list) : Result<Entry list, LedgerError> =
    let description = entry.Description.Trim()

    if entry.Amount <= 0m || entry.Amount > maxAmount then
        Error InvalidAmount
    elif
        description.Length = 0
        || description.Length > maxDescriptionLength
        || description |> Seq.exists Char.IsControl
    then
        Error InvalidDescription
    elif entry.Date > today then
        Error FutureDate
    elif entries |> List.exists (fun e -> e.Id = entry.Id) then
        Error DuplicateId
    else
        let stored =
            { entry with
                Description = description
                Approved = false
                ApprovedBy = None }

        Ok(entries @ [ stored ])

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

/// Quotes a CSV field per RQ-FXL-0007 (RFC 4180 style). Formula-leading
/// characters are neutralised first, as RQ-FXL-0107 requires.
let private csvDescription (value: string) =
    let neutral =
        if value.Length > 0 && "=+-@".Contains(value.[0]) then "'" + value else value

    if neutral |> Seq.exists (fun c -> c = ',' || c = '"' || c = '\r' || c = '\n') then
        "\"" + neutral.Replace("\"", "\"\"") + "\""
    else
        neutral

/// Work item WI-0006. Approved entries of the tenant, ordered by date then id.
let exportCsv (tenantId: string) (entries: Entry list) : string =
    let rows =
        entries
        |> List.filter (fun e -> e.TenantId = tenantId && e.Approved)
        |> List.sortWith (fun a b ->
            let byDate = a.Date.CompareTo(b.Date)
            if byDate <> 0 then byDate else String.CompareOrdinal(a.Id, b.Id))
        |> List.map (fun e ->
            String.Join(
                ",",
                e.Id,
                e.Date.ToString("yyyy-MM-dd", Globalization.CultureInfo.InvariantCulture),
                e.Amount.ToString("F2", Globalization.CultureInfo.InvariantCulture),
                csvDescription e.Description
            )
            + "\n")

    "id,date,amount,description\n" + String.concat "" rows

/// Work item WI-0007. Converts an amount between ISO 4217 currencies.
let convertCurrency (amount: decimal) (fromCurrency: string) (toCurrency: string) : Result<decimal, LedgerError> =
    failwith "not implemented"

/// Work item WI-0008. Records a refund of `amount` against entry `originalId`.
/// Checks in the RQ-FXL-0009 order, with the RQ-FXL-0109 refund window
/// applied after the original is found and before the duplicate-id check.
let refund
    (now: DateTimeOffset)
    (session: Session)
    (today: DateOnly)
    (refundId: string)
    (originalId: string)
    (amount: decimal)
    (entries: Entry list)
    : Result<Entry list, LedgerError> =
    let isClerkOrAdmin = session.Roles |> List.exists (fun r -> r = Clerk || r = Admin)

    if not (isActive now session) then
        Error SessionExpired
    elif not isClerkOrAdmin then
        Error NotAuthorized
    else
        match
            entries
            |> List.tryFind (fun e ->
                e.Id = originalId
                && e.TenantId = session.TenantId
                && e.Approved
                && e.RefundOf.IsNone)
        with
        | None -> Error NotFound
        | Some original ->
            if today.DayNumber - original.Date.DayNumber > refundWindowDays then
                Error(Unsupported "refund window closed")
            elif entries |> List.exists (fun e -> e.Id = refundId) then
                Error DuplicateId
            elif amount <= 0m then
                Error InvalidAmount
            else
                let alreadyRefunded =
                    entries
                    |> List.filter (fun e -> e.RefundOf = Some originalId)
                    |> List.sumBy (fun e -> -e.Amount)

                if alreadyRefunded + amount > original.Amount then
                    Error RefundExceedsOriginal
                else
                    let refundEntry =
                        { Id = refundId
                          TenantId = session.TenantId
                          Amount = -amount
                          Description = "Refund of " + originalId
                          Date = today
                          CreatedBy = session.UserId
                          Approved = false
                          ApprovedBy = None
                          RefundOf = Some originalId }

                    Ok(entries @ [ refundEntry ])
