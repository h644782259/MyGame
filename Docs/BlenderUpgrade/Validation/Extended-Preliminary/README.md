# Failed preliminary aggregate — not acceptance

This initial E aggregate is retained for transparency. It failed three harness integrations: HeroPoseCommit and EquipmentComposition omitted explicit optional authored-motion fixture methods; the knockdown geometry export interpreted the runner's SDK positional argument as its output filename. The SDK write was rejected by the OS (text file busy), and the subsequent actual geometry suite and negative control passed with the corrected CLI. No runtime source fix was needed.

The two legacy fixtures now declare their existing fallback boundary, with all six affected suites and negative controls passing. The geometry script now accepts the SDK positional argument consistently and requires an explicit --output .json for custom output. It cannot treat the SDK path as an output file.

This run also records test source edits made while diagnosing these failures. It is not a frozen successful report. A subsequent isolated rerun started before the third mismatch was discovered was deliberately stopped; it is also not counted. The final Extended-Frozen report is the only whole-batch acceptance record.
