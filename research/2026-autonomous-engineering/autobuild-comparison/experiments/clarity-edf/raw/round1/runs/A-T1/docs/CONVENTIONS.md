# Ledger conventions

1. Amounts are integer cents. Floats are never used for money.
2. Any operation that divides money must conserve it: the parts always sum to the original amount.
3. When an amount cannot be divided exactly, the leftover cents go to the earliest entries, one cent each.
4. Refunds are negative amounts. They are divided by applying the same rule to the absolute value and
   negating every part, so the earliest entries carry the larger refund.
5. Invalid counts (zero or negative) raise `ValueError`. Never return an empty list.
