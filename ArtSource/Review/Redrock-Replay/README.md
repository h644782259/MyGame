# Redrock replay route illustration

[Open the standalone SVG](routes.svg). [Inspect the production export](routes.json).

This is an orthographic geometry/navigation illustration generated with managed Unity value shims. It is **not Unity footage, a gameplay test, or evidence of mobile visual readability**. Both panels use the same X/Z bounds, scale, raw seed (0), and mirror bit (0). The probe has a 0.65 m actor radius. Enemy markers show spawn-clearance radii used by admission (0.5 m, guardians 0.65 m), not their smaller runtime navigation radii (0.45 m and 0.6 m).

The exporter executes production `ChapterRoomGeometry.Plan/Register`, the extracted actual `GameSession.TryChapterSpawn` method, and production `WorldTraversal.FindPath/HasLineOfSight/HasGroundPath`. It records source hashes and generated coordinates in JSON; no obstacle or spawn coordinates are transcribed into the drawing. The two probe endpoints are deliberately selected test inputs, not player telemetry.

For these endpoints, the continuous-wall raw grid path measures 21.480907 m; the split-wall direct path measures 8 m. These are returned route lengths, not claims of optimal shortest paths. The displayed continuous path is the actual `FindPath` waypoint output, **before the runtime route follower's waypoint skipping/smoothing**; it is not a measured gameplay travel distance. Probe LOS changes from blocked to open, as does LOS between existing ranged spawn indices 0 and 1. Both variants resolve six reachable spawns at identical coordinates for this seed. The route flag uses bit 20, preserving the original low-byte fallback jitter; room-zero spawn isolation is separately tested against the actual host placement method.

The illustration omits decorative props, overhead hoist, camera behavior, animations, and temporary Heroic heat. Runtime geometry tests separately cover heat-envelope reachability and actor radii 0.45–1.3 m. Unity/device assessment under the overhead beam remains outstanding.

Reproduce from repository root with .NET 8 and Python 3:

```sh
python3 ArtSource/Review/Redrock-Replay/export.py /path/to/dotnet
```

The exporter rewrites only `routes.json` and `routes.svg` beside itself and builds in a temporary directory using the repository's existing managed fixtures. SVG is the standalone deliverable; no image renderer is required to regenerate it.
