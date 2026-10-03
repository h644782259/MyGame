# Intermediate integrated checks and fixture repairs

The first integrated targeted run was 10/12 passing. Failures were missing test fixture APIs: practice hit notification and actual projectile OnDisable lifecycle. Subsequent confirmed-hit changes removed the former notification hook and added the practiceCastId optional parameter; managed Enemy test boundaries were updated to match it. Production hooks and existing gameplay assertions were retained. Additional old Enemy/Chapter/Healing fixtures needed the new explicit non-practice/state boundaries and real ThreatAdmissionPolicy source. Raw initial failures, final retries and negative-control output are retained here; these are intermediate checks, not final combination acceptance.

First-Six-API validates only the earlier a7676d9 source snapshot. Later final validation supersedes it. Baseline/ preserves the unchanged starting main 233/233 run, including all raw logs and source hashes. None is Unity execution or device acceptance.
