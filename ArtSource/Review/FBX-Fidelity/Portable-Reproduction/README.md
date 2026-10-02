# Portable archived FBX-fidelity reproduction

`reproduce.py` was actually executed against frozen Windows candidate 43039f5f121fadec57b3744cc38a833b80b237db and the existing FBX-Fidelity archive. It completed successfully. It changes neither checkout nor archive.

```sh
python3 reproduce.py   --source-root /path/to/candidate-checkout   --evidence-root /path/to/ArtSource/Review/FBX-Fidelity   --output /path/to/fresh-nonexistent-output   --blender /usr/bin/blender
```

The candidate checkout must contain the locked blend/helper/final FBX/textures represented by this archive. The wrapper refuses input mismatches and existing output directories. It verifies every archive manifest entry, retains unchanged original archived scripts in OriginalScripts, and stages known-path-adapted scripts with copied source, baseline, PNGs and policy inputs. All text substitutions and before/after script hashes are recorded. No historical report or script is overwritten.

Actual run: `actual-run/run-summary.json`; four command logs are at `actual-run/00-hardened_probe.log` through `03-helper_equivalence.log`. `wrapper.log` contains terminal status. The complete staged reports and recreated FBXs are under `actual-run/Stage`.

Terminal assertions: baseline strict FAIL, candidate strict PASS over4923 samples including288 layered samples; exact three finite negative controls; three helper rejection/restoration controls; static invariants; final helper-to-dense-tested curve equivalence; raw KeyTime counts43200→123300. Reproduced curves and all verified static data also exactly match archived final candidate fingerprints. Candidate input hashes and archive hashes remain unchanged. This is source/Blender-FBX evidence, not Unity import/device acceptance or the unrelated full198 suite.

The FBX output is **not byte-identical** to the frozen candidate. `export-timestamps.json`, read independently by `timestamps.py`, records actual FBXHeaderExtension/CreationTimeStamp values: candidate21:02:04.874 versus reproduction21:14:21.956 on2026-10-02. Header timestamps therefore differ; this is not a claim that timestamps are the only differing bytes. The wrapper explicitly verifies exact imported curves and full tested static fingerprints rather than claiming binary-byte determinism. Both the candidate input SHA and reproduced FBX SHA are in the summary.

The wrapper, summary, logs and timestamp diagnostic may be archived alongside the existing evidence. Reproducing again uses a new output directory; it does not depend on earlier scratch paths. The artifact archive itself supplies all historical script and input dependencies.
