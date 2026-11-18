# Task: retry with exponential backoff

Issue #41 (from the API team): "Calls to the pricing service fail intermittently. Please add
`retry(operation, attempts)` in `net/retry.py`: retry on any exception up to `attempts` times,
using `time.sleep` with exponential backoff between attempts. Return the operation's result."
