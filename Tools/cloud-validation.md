# Portable source validation

Requires Python 3.9+, Bash, and the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
The first reference download also requires curl.

```sh
bash Tests/Run-CloudValidation.sh
bash Tests/Run-CloudValidation.sh --download-references
# An SDK installed outside PATH:
bash Tests/Run-CloudValidation.sh --dotnet /path/to/dotnet --compile
# Exact installed Unity 6.6 APIs, including the project's Editor tools:
bash Tests/Run-CloudValidation.sh --unity-editor /path/to/Editor/Unity
```

The first command runs the existing `ProgressionTests.cs` and `SkillRuntimeTests.cs`
against the actual production source, in separate temporary projects. Saves and
generated build files are isolated and removed afterward. Logs and a JSON report
remain under the ignored `Tests/TestResults/Cloud-*` directory.

`--download-references` also compiles every `Assets/Scripts/**/*.cs` file with C# 9
against the same pinned `UnityEngine.Modules` 2021.3.33 package used by
`Tests/Run-CompileCheck.ps1`. The package SHA-512 is checked before extraction.
`--compile` reuses reference DLLs already present in `Tools/ReferenceAssemblies`.
Generated projects restore without external package feeds.

When present, `UpgradeProgressionTests.cs`, `BossAttackPolicyTests.cs`,
`PlayerUpgradeTests.cs`, `EncounterPlanTests.cs`, `RunChoicesTests.cs`, and
`ApplicationPauseStateTests.cs` are run separately against their production logic.
The progression suites cover permanent slot reinforcement, highest-rank legacy
migration, automatic equip/preview equality, failed-write rollback, and repeated
swap/sale/save/load stability. UI and real Unity JsonUtility acceptance remain
separate engine checks.
The JSON report records source SHA-256 hashes and refuses an overall pass if
source files changed during the run; rerun after concurrent edits finish.
`--unity-editor` additionally compiles Windows and iOS runtime branches, Editor
tools, and the separate visual-validation player source against the installed
Unity 6000.6 managed assemblies, with the relevant Unity 6.6 defines and .NET
Standard 2.1 references. It does not launch the Editor or activate a license.

These checks use .NET 8 and the standalone tests' existing Unity shims. They do
not validate Unity's real JsonUtility, Editor-driven compilation/import, physics,
rendering, shaders, GUI, audio, or Windows/iOS builds. Run the repository's real
Unity validation and platform builds separately before release.
