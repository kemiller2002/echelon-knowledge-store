# Booking model
- Times are integer minutes since midnight. An `Interval(start, end)` is half-open: it contains
  `start` and excludes `end`. A booking 540-600 (9:00-10:00) does not contain minute 600.
- `booking.interval` holds the primitive predicates; `ranges` and `conflicts` build on them.
- The nightly report uses `conflicts()`; the calendar view uses `merge_ranges()` to draw busy blocks,
  where back-to-back bookings are drawn as one block.
