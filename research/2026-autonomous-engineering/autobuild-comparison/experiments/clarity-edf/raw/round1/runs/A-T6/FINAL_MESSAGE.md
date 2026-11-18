<!-- Agent's final report, extracted verbatim from the run transcript (model output, not verified). -->

needs-requester-input: I didn't remove `legacy_total` because it isn't dead code. `reports/export.py` still uses it to build the accounting CSV, which needs gross amounts, and `total()` gives net amounts, so swapping one for the other would quietly change the exported figures.

- **What would break:** `reports/export.py` imports `legacy_total` and calls it in `export_row`. That module's docstring says the CSV goes to the accounting system with gross amounts (tax included). Deleting the function would break that import.
- **Code changes:** none. The one existing test passes.
- **Report:** `/root/exp/runs/A-T6/REPORT.json` has `status` set to `"needs-requester-input"` and `files_changed` empty. It asks the requester to choose between two options:
  - keep the gross calculation (net plus 7% tax, rounded half up) by moving it into `export.py` or into a new non-deprecated function such as `gross_total()`, then remove `legacy_total`;
  - switch the export to net amounts using `total()`, which would change the values the accounting system receives and needs its owners' sign-off.
