# Integrated attempt v1 — historical failed run

Source Windows `608e65914d3fece96fe8fe886022e53887a758a3`, iOS `c2c6b28ad15d0c8cb135fae9e04ed581b1dabbe8`.

This is an immutable intermediate attempt, not final acceptance. 225/251 checks passed. The full failure list and raw output are preserved in `Managed/report.json`, `Managed/*.log` and `managed-raw.log`. Existing extracted fixtures lacked new production dependency/signature boundaries; root fixes and parent review product corrections follow in later commits.

The complete input audit caught two tracked historical scenery snapshot gzip files rewritten by the old FixedScenery test runner. The narrower cloud C# source hash reported no changes, so it alone is insufficient. This attempt is not a stable frozen acceptance; the runner will be corrected to write generated snapshots only under ignored TestResults. External reference inputs stayed unchanged; see `input-manifest.json` and `input-stability.json`. Explicit Windows/iOS/Android API builds passed against pinned references; this is not Unity execution. Platform sync records apply only to the source commits above.
