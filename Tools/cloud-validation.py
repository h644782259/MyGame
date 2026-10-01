#!/usr/bin/env python3
"""Run the existing standalone tests and an optional Unity API compile check.

No gameplay behavior is copied here. Generated projects and save fixtures live in
a temporary directory. This is not a Unity Editor, rendering, or player test.
"""
import argparse
import base64
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
from xml.sax.saxutils import escape, quoteattr
import zipfile

ROOT = Path(__file__).resolve().parent.parent
PACKAGE_URL = ("https://api.nuget.org/v3-flatcontainer/unityengine.modules/"
               "2021.3.33/unityengine.modules.2021.3.33.nupkg")
PACKAGE_SHA512 = ("ad7eBwkG66RQ0ToAMD/ak8MZ6pfgrYXxcPGftN8H7ydkn5TdrwlU5qgZTkHpjOGYo"
                  "IJvIk3+77VWq53/UU2dqA==")


def unity_references(download):
    root = ROOT / "Tools/ReferenceAssemblies"
    references = root / "UnityEngine/lib/netstandard2.0"
    if references.is_dir() and list(references.glob("*.dll")):
        return references
    if not download:
        raise RuntimeError("Unity reference DLLs are missing; pass --download-references "
                           "to retrieve the repository's pinned compile-only NuGet package.")
    root.mkdir(parents=True, exist_ok=True)
    archive = root / "unityengine.modules.2021.3.33.nupkg"
    if not archive.exists():
        pending = archive.with_suffix(".download")
        subprocess.run(["curl", "--fail", "--location", "--retry", "1", "--connect-timeout",
                        "20", "--max-time", "180", PACKAGE_URL, "--output", str(pending)], check=True)
        pending.replace(archive)
    digest = base64.b64encode(hashlib.sha512(archive.read_bytes()).digest()).decode("ascii")
    if digest != PACKAGE_SHA512:
        raise RuntimeError("Unity reference package SHA-512 mismatch; refusing to extract it.")
    destination = (root / "UnityEngine").resolve()
    with zipfile.ZipFile(archive) as package:
        for entry in package.infolist():
            if not (destination / entry.filename).resolve().is_relative_to(destination):
                raise RuntimeError("Unsafe reference-package archive path.")
        package.extractall(destination)
    if not list(references.glob("*.dll")):
        raise RuntimeError("The pinned package contains no reference DLLs at the expected path.")
    return references


def write_project(directory, sources, program=None, references=None, framework="net8.0", defines=""):
    directory.mkdir()
    if program is not None:
        entry = directory / "Program.cs"
        entry.write_text(program, encoding="utf-8")
        sources = [*sources, entry]
    source_items = "\n".join("    <Compile Include=" + quoteattr(str(path)) + " />" for path in sources)
    reference_items = ""
    if references is not None:
        reference_items = "\n".join(
            "    <Reference Include=" + quoteattr(path.stem) + "><HintPath>" + escape(str(path)) +
            "</HintPath><Private>false</Private></Reference>" for path in sorted(references))
    project = directory / "Validation.csproj"
    project.write_text('''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>''' + framework + '''</TargetFramework>
    <OutputType>''' + ("Exe" if program is not None else "Library") + '''</OutputType>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <LangVersion>9.0</LangVersion>
    <NuGetAudit>false</NuGetAudit>
    <DefineConstants>''' + escape(defines) + '''</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
''' + source_items + "\n" + reference_items + '''
  </ItemGroup>
</Project>
''', encoding="utf-8")
    return project


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", default=os.environ.get("DOTNET", "dotnet"),
                        help=".NET 8 SDK executable (or set DOTNET)")
    parser.add_argument("--compile", action="store_true", help="also compile all runtime sources against Unity references")
    parser.add_argument("--download-references", action="store_true", help="download pinned Unity reference DLLs if missing; implies --compile")
    parser.add_argument("--unity-editor", type=Path, help="also compile Windows/iOS runtime, Editor, and visual-validation source using installed Unity 6000.6 DLLs (does not launch Unity)")
    args = parser.parse_args()
    dotnet = shutil.which(args.dotnet)
    if not dotnet:
        parser.error(".NET 8 SDK is required. Install it from https://dotnet.microsoft.com/download/dotnet/8.0 or set --dotnet.")
    timestamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")
    output = ROOT / "Tests/TestResults" / ("Cloud-" + timestamp)
    output.mkdir(parents=True)
    initial_sources = source_hashes()
    report = {"startedUtc": timestamp, "project": str(ROOT), "checks": [],
              "scope": "Standalone production-logic tests; optional Unity API/source compilation. "
                       "No Unity Editor execution, real JsonUtility, rendering, shaders, or platform build executed."}
    failed = False
    with tempfile.TemporaryDirectory(prefix="EmberfallCloudValidation-") as temporary:
        workspace = Path(temporary)
        config = workspace / "NuGet.Config"
        config.write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
        env = os.environ.copy()
        env.update({"DOTNET_CLI_TELEMETRY_OPTOUT": "1", "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1",
                    "DOTNET_GENERATE_ASPNET_CERTIFICATE": "false", "DOTNET_NOLOGO": "1",
                    "DOTNET_CLI_HOME": str(workspace / "dotnet-home"), "NUGET_PACKAGES": str(workspace / "nuget")})
        checks = [
            ("progression", [ROOT / "Assets/Scripts/Core/GameTypes.cs", ROOT / "Assets/Scripts/Core/ProgressionService.cs",
                             ROOT / "Tests/ProgressionTests.cs"],
             'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(ProgressionTests.Run(args[0])); } }'),
            ("skills", [ROOT / "Assets/Scripts/Core/GameTypes.cs", ROOT / "Assets/Scripts/Core/SkillRuntime.cs",
                        ROOT / "Tests/SkillRuntimeTests.cs"],
             'using System; internal static class Program { static void Main() { Console.WriteLine(SkillRuntimeTests.Run()); } }'),
        ]
        if (ROOT / "Tests/UpgradeProgressionTests.cs").exists():
            checks.append(("upgrade-progression", [ROOT / "Assets/Scripts/Core/GameTypes.cs",
                          ROOT / "Assets/Scripts/Core/ProgressionService.cs", ROOT / "Tests/ProgressionTests.cs",
                          ROOT / "Tests/UpgradeProgressionTests.cs"],
                          'using System; internal static class Program { static void Main(string[] args) { Console.WriteLine(UpgradeProgressionTests.Run(args[0])); } }'))
        if (ROOT / "Tests/BossAttackPolicyTests.cs").exists():
            checks.append(("boss-attack-policy", [ROOT / "Assets/Scripts/Combat/BossAttackPolicy.cs",
                          ROOT / "Tests/BossAttackPolicyTests.cs"],
                          'using System; internal static class Program { static void Main() { Console.WriteLine(BossAttackPolicyTests.Run()); } }'))
        if (ROOT / "Tests/PlayerUpgradeTests.cs").exists():
            checks.append(("player-upgrades", [ROOT / "Assets/Scripts/Core/GameTypes.cs",
                          ROOT / "Assets/Scripts/Core/SkillRuntime.cs", ROOT / "Tests/SkillRuntimeTests.cs",
                          ROOT / "Assets/Scripts/Combat/PlayerUpgradeRules.cs", ROOT / "Assets/Scripts/Combat/CombatDamage.cs",
                          ROOT / "Tests/PlayerUpgradeTests.cs"],
                          'using System; internal static class Program { static void Main() { Console.WriteLine(PlayerUpgradeTests.Run()); } }'))
        if (ROOT / "Tests/EncounterPlanTests.cs").exists():
            checks.append(("encounter-plans", [ROOT / "Assets/Scripts/Core/GameTypes.cs",
                          ROOT / "Assets/Scripts/Core/SkillRuntime.cs", ROOT / "Tests/SkillRuntimeTests.cs",
                          ROOT / "Assets/Scripts/Core/EncounterPlan.cs", ROOT / "Tests/EncounterPlanTests.cs"],
                          'using System; internal static class Program { static void Main() { Console.WriteLine(EncounterPlanTests.Run()); } }'))
        if (ROOT / "Tests/RunChoicesTests.cs").exists():
            checks.append(("run-choices", [ROOT / "Assets/Scripts/Core/GameTypes.cs",
                          ROOT / "Assets/Scripts/Core/SkillRuntime.cs", ROOT / "Tests/SkillRuntimeTests.cs",
                          ROOT / "Assets/Scripts/Core/RunChoices.cs", ROOT / "Tests/RunChoicesTests.cs"],
                          'using System; internal static class Program { static void Main() { Console.WriteLine(RunChoicesTests.Run()); } }'))
        if (ROOT / "Tests/ApplicationPauseStateTests.cs").exists():
            checks.append(("application-pause-state", [ROOT / "Assets/Scripts/Core/ApplicationPauseState.cs",
                          ROOT / "Tests/ApplicationPauseStateTests.cs"],
                          'using System; internal static class Program { static void Main() { Console.WriteLine(ApplicationPauseStateTests.Run()); } }'))
        for name, sources, program in checks:
            project = write_project(workspace / name, sources, program)
            commands = [[dotnet, "restore", str(project), "--configfile", str(config), "--verbosity", "quiet"],
                        [dotnet, "run", "--project", str(project), "--no-restore", "--configuration", "Release", "--", str(workspace / "saves")]]
            passed = run_check(name, commands, env, output, report)
            failed = failed or not passed
        if args.compile or args.download_references:
            try:
                refs = unity_references(args.download_references)
                sources = sorted((ROOT / "Assets/Scripts").rglob("*.cs"))
                project = write_project(workspace / "runtime-compile", sources, references=list(refs.glob("*.dll")))
                commands = [[dotnet, "restore", str(project), "--configfile", str(config), "--verbosity", "quiet"],
                            [dotnet, "build", str(project), "--no-restore", "--configuration", "Release", "--verbosity", "minimal"]]
                passed = run_check("runtime-compile", commands, env, output, report)
                report["runtimeSourceCount"] = len(sources)
                failed = failed or not passed
            except (OSError, RuntimeError, subprocess.CalledProcessError) as error:
                print("FAIL runtime-compile: " + str(error), file=sys.stderr)
                report["checks"].append({"name": "runtime-compile", "passed": False, "error": str(error)})
                failed = True
        if args.unity_editor:
            managed = args.unity_editor.resolve().parent / "Data/Managed"
            modules = managed / "UnityEngine"
            if not (modules / "UnityEngine.CoreModule.dll").is_file():
                report["checks"].append({"name": "exact-unity-compile", "passed": False,
                                         "error": "Unity engine module DLLs not found beside the specified executable."})
                failed = True
            else:
                report["unityEditorReferences"] = str(args.unity_editor.resolve())
                for variant, extra_defines in [
                    ("runtime", "UNITY_STANDALONE;UNITY_STANDALONE_WIN"),
                    ("ios-runtime", "UNITY_IOS"),
                    ("editor", "UNITY_EDITOR;UNITY_EDITOR_LINUX"),
                    ("visual-validation", "EMBERFALL_VISUAL_VALIDATION;UNITY_STANDALONE;UNITY_STANDALONE_WIN"),
                ]:
                    name = "exact-unity-" + variant + "-compile"
                    refs = list(modules.glob("UnityEngine*.dll"))
                    sources = sorted((ROOT / "Assets/Scripts").rglob("*.cs"))
                    defines = "UNITY_6000_0_OR_NEWER;UNITY_6000_6_OR_NEWER;" + extra_defines
                    if variant == "editor":
                        refs += list(modules.glob("UnityEditor*.dll"))
                        sources += sorted((ROOT / "Assets/Editor").rglob("*.cs"))
                    elif variant == "visual-validation":
                        sources += sorted((ROOT / "Assets/Tests").rglob("*.cs"))
                    project = write_project(workspace / name, sources, references=refs,
                                            framework="netstandard2.1", defines=defines)
                    commands = [[dotnet, "restore", str(project), "--configfile", str(config), "--verbosity", "quiet"],
                                [dotnet, "build", str(project), "--no-restore", "--configuration", "Release", "--verbosity", "minimal"]]
                    passed = run_check(name, commands, env, output, report)
                    failed = failed or not passed
    final_sources = source_hashes()
    changed = sorted(path for path in initial_sources.keys() | final_sources.keys()
                     if initial_sources.get(path) != final_sources.get(path))
    report["sourceSha256"] = initial_sources
    report["sourceChangedDuringRun"] = changed
    if changed:
        print("Source changed during validation; rerun after edits finish: " + ", ".join(changed))
        failed = True
    report["completedUtc"] = datetime.now(timezone.utc).isoformat()
    report["passed"] = not failed
    (output / "report.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(report["scope"])
    print("Report: " + str(output / "report.json"))
    return 1 if failed else 0


def source_hashes():
    sources = sorted((ROOT / "Assets").rglob("*.cs")) + sorted((ROOT / "Tests").glob("*.cs"))
    return {str(path.relative_to(ROOT)): hashlib.sha256(path.read_bytes()).hexdigest() for path in sources}


def run_check(name, commands, env, output, report):
    chunks = []
    passed = True
    for command in commands:
        result = subprocess.run(command, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
        chunks.append(result.stdout)
        print(result.stdout, end="", flush=True)
        if result.returncode != 0:
            passed = False
            break
    log = output / (name + ".log")
    log.write_text("\n".join(chunks), encoding="utf-8")
    report["checks"].append({"name": name, "passed": passed, "log": log.name})
    print(("PASS " if passed else "FAIL ") + name, flush=True)
    return passed


if __name__ == "__main__":
    raise SystemExit(main())
