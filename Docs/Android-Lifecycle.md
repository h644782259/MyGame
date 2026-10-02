# Android lifecycle input and audio

This shared runtime change keeps existing combat and save rules. It does not change Android PlayerSettings or native activity configuration.

- Background UI is disabled. Focus loss/suspension clears IMGUI control ownership, scrolling, skill drags, movement and orbit input. A release latch requires held pointers to lift before a new surface can receive them.
- Screen dimensions, safe-area coordinates, layout scale, raw DPI and orientation identify the touch coordinate system. A changed value cancels old pointer ownership, including a 180-degree rotation with unchanged dimensions.
- UI Back records its consumed frame. `GameUI.GameplayBackAllowed` checks the current modal state and this frame marker, so gameplay consumers must check this property even after a panel closes. Active gameplay cancels charge/targeting first; modal UI owns Back while blocked. Title Back requests the existing exit confirmation only on Android. Confirmed Android exit uses `Application.Quit`; iOS keeps its existing return-to-title behavior. Player/targeting consumer integration is supplied in the companion controller change.
- Audio observes the combined `!Focused || Suspended` state. Only transitions stop old effects and pause/unpause the ambient loop. Repeated callbacks cannot replay it; foreground alone cannot override suspension. Audio configuration rebuilds preserve a sample cursor where supplied by Unity. Ambient still ignores the menu listener pause, so manual menu pause retains its prior behavior.
- The existing save lifecycle gate remains responsible for one successful save per background episode and retries after save failure. Manual pause state is not changed by resume.

## Reproducible checks

`AndroidLifecycleTests` depends on `TouchViewportState`, `TouchReleaseLatch`, `AudioLifecycleGate`, `ApplicationPauseState`, and `SaveLifecycleGate`. It checks all 65,536 sequences of eight focus/suspension callbacks, viewport changes, held-pointer release, manual pause preservation and save retry: 2,621,497 assertions. Companion existing tests: 56 pause-state, 10 touch-release and 25 safe-exit assertions. `AndroidLifecycleSourceTests.py` checks production wiring. All 43 repository source-test groups passed. All runtime scripts compiled against available Unity reference assemblies with no symbols and with `UNITY_ANDROID` (zero warnings/errors).

These are pure-state, source-contract and reference-API checks, not engine/device tests. Actual Android system/predictive Back delivery into `KeyCode.Escape`, background audio, interruption by phone calls, orientation/safe-area rendering, resume touch ownership and quit behavior require Unity/adb/device validation. There is no claim that these behaviors were observed on a device. Existing iOS platform/font settings are unchanged.

## Pause subpage Back regression

On mobile, open Pause → More settings (page 1) → Touch layout (page 2). Previously Back toggled the session pause off from either subpage, while the visible return action led to the main pause page. Back now returns to page 0 and retains pause; a subsequent Back uses the existing main pause behavior. Dedicated panels (Controls, SaveLocation, etc.) and exit confirmations retain their own navigation and frame consumption.

`MobilePauseNavigationTests` compiles the production `GameUI.PauseNavigation.cs` partial with a minimal session/UI shell, checking 145 combinations and repeated Back behavior. It must run separately from the full Unity runtime because the shell intentionally supplies the partial's dependencies. `MobilePauseNavigationSourceTests.py` checks integration priority. These checks do not demonstrate OS Back delivery or touch rendering.
