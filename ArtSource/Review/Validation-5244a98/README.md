# Frozen source validation — 2026-10-02

189/189 checks passed under default JIT, with no source changes during the run. `handoff.json` maps the tested commit and identical Windows tree to the final Windows/iOS/Android heads, plus runtime/resource parity hashes. `report.json` is the unmodified runner report; `full.log` contains complete aggregate output. Individual log references in the report are relative to the retained execution evidence directory; the aggregate output is published here.

Three conditional API compilations use cached Unity 2021 reference assemblies. No Unity 6 Editor, real JsonUtility, engine import, shader execution, platform build, touchscreen playtest or device performance acceptance is implied. Native Blender review images are separate authoring evidence. Current character geometry remains visually provisional.

This evidence branch adds no runtime changes. The final Windows candidate is `5244a985c71efc8bd7cbb55859e77e891b0720f0`; iOS is `150ba90ea125362b3c4d913aa88dda84636bb15c`; Android is `a5d78d03ce3ae84c3d90b39cfb72e60661aab0fb`. Both GitHub PRs remain draft and main is unchanged.
