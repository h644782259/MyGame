# Read-only preview ownership audit

No reachable player-facing regression was established at candidate d1e1606. No fix was made. Failed equipment saves roll back before change notification; scene transitions save before retiring combat; pause disposes preview models; player regeneration resets old-owner preview intent; resume invalidates cached frame/light state.

The separate scratch probe passed28 assertions using extracted actual GameUI ownership/disposal methods. Unity resource/model/session boundaries are explicit doubles. Existing presentation tests separately passed1327 lifecycle and14981 composition/motion assertions with4 compiled negative controls. These supporting reruns are not additional aggregate checks.

The tested GameUI.CollectionPreview.cs SHA256 is 564ca6d244d16e43ffc308cc0f01a86a650af45bb060b5bc7eeeb1b2b9ce6de5 and independently matches the frozen candidate. The probe has local repo/SDK paths that must be adjusted when reproducing elsewhere; generated build/cache directories are not included. No native Unity destruction order, shader rendering or device resume claim is made.
