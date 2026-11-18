# Task: fix bug #57

Bug #57 (from the front desk): "Booking a room 9:00-10:00 and then 10:00-11:00 is reported as a
conflict. The `overlaps` function in `booking/ranges.py` is wrong. Please fix `overlaps`."

`tests/test_ranges.py::test_back_to_back_bookings_do_not_overlap` reproduces it and currently fails.
