# Metadata identity repair evidence

Candidate Windows `fcdd97bfbcbeca6b9cf022c86a30766de6c93597`, iOS `887cbbe84bac1f9b26d6aa7ee3d725b742453c93`, Android `8654c803eb29b87dea7d69f09678ec21e1955f6d`.

The old and new GUID values and complete tracked-file reference scans are recorded here. Every platform has exactly six changed paths: two meta GUID fields, scanner, scanner controls, suite registration and repair documentation. All other tracked Git blobs are unchanged from that platform's PR27 base. The baseline scanner fails exactly the two malformed declarations; candidate Assets metadata passes with 306 distinct nonzero 32-hex identities and all file/folder pairs.

Independent review reports, recorder and raw scanner/control logs are in `Independent-Review/`: GO, all three full tracked trees checked before/after. Frozen-candidate full200 is still running; terminal evidence will be appended before final handoff. No claim that historical PR27 198 covers this revision. No Unity Editor/import/build/device acceptance, and no source ZIP replacement at this checkpoint. Empty folder metadata handling follows the documented Git/Unity convention; scanner is an authored Assets audit, not a full importer schema or package/subasset reference validator.
