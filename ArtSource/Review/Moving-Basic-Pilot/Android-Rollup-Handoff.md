# Android source rollup handoff

Prepared source candidate: `134e617944aee99f16aac29b30e984f97bc57c30`, tree `f49152e993af1bd57b9087945fdecc8f018e8b5e`, pushed to `h644782259/emberfall_win` branch `codex/android-moving-basic-pilot`. Local checkout: `/workspace/emberfall_android_moving_pilot`. It contains the prior quality-maintenance Android baseline `d0a8b8ee790b8f3e8f6fc9d61711816c15cbbd4c` plus the bounded PR25 shared changes. Windows/iOS PR25 remain draft/unmerged at this handoff; the parent owns merge and final release selection.

The final 197 registered Windows checks pass. Actual Android cached API compilation passes for all 256 runtime source files; 13 synchronized paths, 516 runtime/meta files and 69 art/pilot paths match the other platforms. All 24 Android protected files match both its round baseline and original ledger. Candidate source remained unchanged/clean through final rehash. Exact raw reports are under Validation/dcd75ac.

No new Android ZIP, APK, AAB or Library write was made in this closure. Follow the parent's single final rollup after selecting merged source; do not label an unmerged draft source archive as a released build. A real Android Unity/device build remains unverified.

Last parent-confirmed Library archive is version 5 of `libfile_29b2c7a771bc8191a6593d8035cfd597`, backing `file_00000000a43c820db4078b7878a3674a`, name `Emberfall-Android-Source-0f78800.zip`, 29,356,488 bytes, SHA256 `972d523321dd953e70da32cbe038a1988d7ee296349e605f926f158da2f7abfb`. That archive includes PR22 source `0f788008c9794de4bd50b8b7fa2136692a54d567`, not PR23/24/25. Re-read current Library identity/version before replacement; do not blindly overwrite this last-known version if the parent has since published another rollup.

Before final delivery, verify chosen Windows/iOS main trees and the corresponding Android shared-source parity, retain all Android platform/font files, archive only that exact chosen source, record bytes/SHA256, and perform the version-guarded replacement through the parent's publication workflow. Existing videos are separate native assets and must not be overwritten with the source ZIP.
