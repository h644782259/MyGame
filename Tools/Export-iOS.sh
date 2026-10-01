#!/usr/bin/env bash
# Run with: bash Tools/Export-iOS.sh. Requires macOS, Unity and its iOS Build Support.
set -euo pipefail

project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)"
editor_version="$(sed -n 's/^m_EditorVersion: //p' "$project_root/ProjectSettings/ProjectVersion.txt" | tr -d '\r')"
unity_path="${UNITY_PATH:-/Applications/Unity/Hub/Editor/$editor_version/Unity.app/Contents/MacOS/Unity}"
bundle_id="com.h644782259.emberfall.ios"
team_id=""
output=""
check_only=false

usage() {
  echo 'Usage: bash Tools/Export-iOS.sh [--unity /path/to/Unity] [--bundle-id com.yourname.emberfall] [--team-id ABCDE12345] [--output Builds/iOS/MyExport] [--check-only]'
}
while [[ $# -gt 0 ]]; do
  case "$1" in
    --unity|--bundle-id|--team-id|--output)
      [[ $# -ge 2 ]] || { usage >&2; exit 2; }
      case "$1" in
        --unity) unity_path="$2" ;;
        --bundle-id) bundle_id="$2" ;;
        --team-id) team_id="$2" ;;
        --output) output="$2" ;;
      esac
      shift 2 ;;
    --check-only) check_only=true; shift ;;
    --help|-h) usage; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; usage >&2; exit 2 ;;
  esac
done

[[ "$(uname -s)" == Darwin ]] || { echo 'This script is for macOS. Use Export-iOS.ps1 for the Windows prerequisite check.' >&2; exit 2; }
[[ -x "$unity_path" ]] || { echo "Unity editor not found: $unity_path. Install $editor_version with Unity Hub, or pass --unity." >&2; exit 2; }
ios_support="$(dirname -- "$unity_path")/../PlaybackEngines/iOSSupport"
[[ -d "$ios_support" ]] || { echo "Missing iOS Build Support for this editor. Add it in Unity Hub before exporting. No module has been installed by this script." >&2; exit 2; }
echo "Unity editor: $unity_path"
echo 'iOS Build Support: present'
if xcode-select -p >/dev/null 2>&1 && xcrun --find xcodebuild >/dev/null 2>&1; then
  echo 'Xcode command-line tools: found (device signing still requires your selected team).'
else
  echo 'Xcode is not configured. You can export source, but need full Xcode and its iOS platform support to build/run on iPhone.'
fi
if $check_only; then
  echo 'Prerequisite check only; Unity was not started and no Xcode/IPA was generated.'
  exit 0
fi

stamp="$(date +%Y%m%d-%H%M%S)-$$"
if [[ -z "$output" ]]; then output="$project_root/Builds/iOS/Xcode-$stamp"; fi
if [[ "$output" != /* ]]; then output="$project_root/$output"; fi
mkdir -p "$project_root/Logs"
log_file="$project_root/Logs/ios-export-$stamp.log"
args=(-batchmode -quit -buildTarget iOS -projectPath "$project_root" -executeMethod Emberfall.Editor.IOSBuild.Export -logFile "$log_file" -emberfallIosOutput "$output" -emberfallIosBundleId "$bundle_id")
if [[ -n "$team_id" ]]; then args+=(-emberfallIosTeamId "$team_id"); fi
echo 'Close this project in Unity before starting the export.'
if ! "$unity_path" "${args[@]}"; then
  tail -n 60 "$log_file" >&2 || true
  echo "Unity export failed. Log: $log_file" >&2
  exit 1
fi
[[ -f "$output/Unity-iPhone.xcodeproj/project.pbxproj" ]] || { echo "Unity returned without a complete Xcode project. Inspect $log_file" >&2; exit 1; }
echo "Xcode source exported: $output"
echo 'Open Unity-iPhone.xcworkspace if present; otherwise Unity-iPhone.xcodeproj. Choose your Personal Team or developer team, your connected iPhone, then Run.'
echo 'No signed IPA was created and no device installation was performed.'
