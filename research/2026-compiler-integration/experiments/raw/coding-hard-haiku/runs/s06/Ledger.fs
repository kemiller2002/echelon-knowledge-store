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

/// Work item WI-0002. `today` is the caller's current date.
let addEntry (today: DateOnly) (entry: Entry) (entries: Entry list) : Result<Entry list, LedgerError> =
    let description = entry.Description.Trim()

    let descriptionValid =
        description.Length > 0
        && description.Length <= maxDescriptionLength
        && not (description |> Seq.exists Char.IsControl)

    if entry.Amount <= 0m || entry.Amount > maxAmount then Error InvalidAmount
    elif not descriptionValid then Error InvalidDescription
    elif entry.Date > today then Error FutureDate
    elif entries |> List.exists (fun e -> e.Id = entry.Id) then Error DuplicateId
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

/// Work item WI-0006. Approved entries of the tenant as RFC 4180 CSV (DF-FXL-0004).
let exportCsv (tenantId: string) (entries: Entry list) : string =
    let neutralise (s: string) =
        if s.Length > 0 && "=+-@".Contains(s.[0]) then "'" + s else s

    let quote (s: string) =
        if s.IndexOfAny([| ','; '"'; '\r'; '\n' |]) >= 0 then
            "\"" + s.Replace("\"", "\"\"") + "\""
        else
            s

    let line (e: Entry) =
        let date = e.Date.ToString("yyyy-MM-dd", Globalization.CultureInfo.InvariantCulture)
        let amount = e.Amount.ToString("F2", Globalization.CultureInfo.InvariantCulture)
        String.Join(",", [| e.Id; date; amount; quote (neutralise e.Description) |]) + "\n"

    let body =
        entries
        |> List.filter (fun e -> e.TenantId = tenantId && e.Approved)
        |> List.sortWith (fun a b ->
            let c = compare a.Date b.Date
            if c <> 0 then c else String.CompareOrdinal(a.Id, b.Id))
        |> List.map line
        |> String.concat ""

    "id,date,amount,description\n" + body

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
    let refundWindowDays = 90

    if not (isActive now session) then Error SessionExpired
    elif not (hasRole Clerk session || hasRole Admin session) then Error NotAuthorized
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
                          Description = $"Refund of {originalId}"
                          Date = today
                          CreatedBy = session.UserId
                          Approved = false
                          ApprovedBy = None
                          RefundOf = Some originalId }

                    Ok(entries @ [ refundEntry ])
