# Android development build — Unity 6000.6.3f1

This dedicated Android clone prepares a **development APK**, not a store release. No APK has been produced in the current cloud environment. See [the actual preflight report](Android-Toolchain-Report.json).

## Configuration

- Package: `com.h644782259.emberfall.android.dev` (development identity; changing this later changes the Android application/save sandbox).
- ARM64 only, IL2CPP, Minimal managed stripping. Android 8/API 26 minimum; pinned target API 36, not `Automatic` (avoids a host SDK silently changing behavior). Targeting API 36 does not itself establish Google Play eligibility.
- OpenGL ES 3.1 requirement; both landscape orientations, portrait disabled. The existing GameActivity, predictive-back and render-outside-safe-area settings remain. Cutouts/system bars and Android 15/16 edge-to-edge behavior require device validation.
- Existing Android Medium quality selection retained. No shared quality levels or Windows/iOS settings were downgraded. The inherited `ProjectTools` Editor initialization still sets current quality values; this builder does not call that helper or add quality mutations.
- No custom keystore, no expansion file, no App Bundle, no export, no forced internet/storage permissions. **These are configuration intentions; the merged APK permission set remains unverified.** The build audit lists every actual declared permission and exits nonzero if any require review. No runtime/native permission plugin was added.

## Exact supported tools

Install the exact Editor and Android Build Support, Android SDK & NDK Tools, and OpenJDK through the official Unity Hub on an authorized machine. The scripts never install software, accept licences, sign in, modify External Tools preferences, generate signing keys, or upload artifacts.

| Component | Required for 6000.6.3f1 |
| --- | --- |
| Editor | 6000.6.3f1, revision 45d8eee7de74 |
| SDK platform | android-36 |
| SDK build tools | 36.0.0 |
| SDK command-line tools / platform tools | 16 / 36.0.0 (Unity-supported bundle) |
| NDK | r27c / 27.2.12479018 |
| OpenJDK | 17 |
| Gradle / Android Gradle Plugin | 9.3.1 / 9.1.1 |

Sources checked against [6000.6.3f1 release notes](https://unity.com/releases/editor/whats-new/6000.6.3f1), [6000.6 dependency versions](https://docs.unity3d.com/6000.6/Documentation/Manual/android-supported-dependency-versions.html), [6000.6 Gradle compatibility](https://docs.unity3d.com/6000.6/Documentation/Manual/android-gradle-version-compatibility.html), [Android requirements](https://docs.unity3d.com/6000.6/Documentation/Manual/android-requirements-and-compatibility.html), and [official setup](https://docs.unity3d.com/6000.6/Documentation/Manual/android-sdksetup.html).

Use the bundled External Tools selections. The Editor entrypoint refuses custom tool paths rather than silently changing machine preferences. Preflight verifies key files, exact NDK/JDK/Gradle metadata and version response; it is not proof that licences or all dependencies are usable. Unity itself must successfully compile/build.

## Commands

From the repository root, with Python 3 and the exact installed Unity executable:

```bash
python3 Tests/AndroidPlatformTests.py
python3 Tools/Android/preflight.py --unity /path/to/Editor/Unity --report Tests/TestResults/Android/preflight.json
python3 Tools/Android/build_debug.py --unity /path/to/Editor/Unity
```

`UNITY_EDITOR` may replace `--unity`. Preflight exit 2 means blocked; exit 0 means tools present but still unverified. The build wrapper only launches Unity after preflight passes. It creates a unique `Builds/Android/<UTC>-<nonce>/` directory, stores the Unity log and preflight, invokes `Emberfall.Editor.AndroidDevelopmentBuild.BuildDebugApk`, and requires a real nonempty APK plus build receipt. It then checks actual APK package/min/target/debuggable flags using bundled aapt2, ABI/IL2CPP contents, and signature using apksigner verification. Audit exit 0 is **not device acceptance**. Any declared permission causes exit 2 and an explicit review requirement; even normal development-tool permissions are not silently accepted.

The same Editor entrypoint is exposed at `Emberfall > Android`. Switch to Android first. Prefer the Python wrapper because it performs the complete host dependency checks and post-build audit. No command installs an APK on a device automatically.

## Signing and licences

Only already-existing standard local Android debug signing material is permitted. Both preflight and the Editor builder check presence/nonzero length without reading or logging its contents/path. Missing material stops the build before Gradle can create a default key. Android home and JVM-option environment overrides are rejected to avoid redirecting default signing to an unchecked location. There is no key generation fallback, keytool invocation, private-key upload, custom-keystore password input, or store signing flow. Do not run a stock Android build outside these guarded entrypoints if it would auto-generate a key. Existing local material is never copied into this repository; credentials and Android caches are ignored. Its validity is established only by an eventual build/signature audit, not by a presence check. See [Unity Android keystores](https://docs.unity3d.com/6000.6/Documentation/Manual/android-keystore.html).

An existing SDK licence record is only a presence check, not legal acceptance. Valid Unity entitlement and user-managed licence acceptance remain prerequisites. See [Unity Hub licence management](https://docs.unity.com/en-us/hub/manage-license). No passwords or licence secrets are requested by these scripts.

## Current evidence and remaining acceptance

The actual host has Linux x86_64 and system OpenJDK 21.0.12.1; **Java 21 is not the required bundled JDK 17**. Unity, Android Build Support, SDK/NDK, adb, Gradle and standard debug material are missing in preflight. No Editor compile, shader build, IL2CPP link, APK, merged-manifest audit, install, device footage or performance result is claimed. Five Python policy/parser/negative-path tests pass; synthetic ZIP fixtures are parser tests only.

The earlier official Linux Editor URL `https://download.unity3d.com/download_unity/45d8eee7de74/LinuxEditorInstaller/Unity-6000.6.3f1.tar.xz` was rejected by the environment's proxy (CONNECT HTTP 403, curl exit 56). We did not bypass it or retry downloads. Evidence remains in `/workspace/shared/unity-environment/download-head.log`. This is a download blocker, not the old environment's AF_UNIX issue; local socket probes in this machine previously succeeded.

After a genuine build, validate API 26 and API 36 ARM64 devices: two landscape rotations/cutouts; Chinese font glyphs; simultaneous movement/attack and cancelled touches; background/resume/save recovery; long combat thermal/memory/frame behavior; upgrade/save persistence; and all declared APK permissions. Shared code uses `KeyCode.Escape`; target-36 GameActivity/predictive-back delivery needs device verification, with no claim that the current manifest proves it. [Unity permission documentation](https://docs.unity3d.com/6000.6/Documentation/Manual/android-permissions-in-unity.html) explains that API usage/plugins can add permissions during packaging.

No Android GitHub repository or origin is created by this batch. The parent task owns repository creation and publication; the local `shared-source` remote remains unchanged.
