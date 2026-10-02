# Companion navigation allocation check

Scope: .NET 8 managed production-algorithm measurements with existing Unity value/object shims. These are not Unity frame allocations, device timings, combat playback, or a frame-rate improvement claim.

The executed call chain is the actual `AcquirePackTarget` → `LegalPackTarget` → `PackCanReach` → `WorldTraversal.CanReach/FindPath`, plus actual `WorldTraversal.Route.Direction`. Pack reachability already has a bounded 32-target, 250 ms cache with endpoint/revision invalidation; clear segments bypass that search. This change does not alter those policies.

Each case uses six enemies and one or four wolves (the normal non-Twin wolf cap), 180 warmup query ticks, then 240 measured query ticks with controlled moving endpoints and a 1/60-second simulated clock. The blocked fixture uses the Forest central-island footprint. Endpoint motion is a repeatable benchmark input, not a simulated physical party trajectory. Setup, trace buffers, serialization, hashing, and reporting are outside `GC.GetAllocatedBytesForCurrentThread` measurements.

Observed on this workspace's .NET 8 x64 runtime:

| Queries | Wolves | Legacy allocated bytes | Current allocated bytes | Saved |
|---|---:|---:|---:|---:|
| Open | 1 | 0 | 0 | 0 |
| Open | 4 | 0 | 0 | 0 |
| Blocked | 1 | 1,746,560 | 1,742,080 | 4,480 (0.257%) |
| Blocked | 4 | 7,204,560 | 7,186,080 | 18,480 (0.257%) |

The change removes two invariant eight-int array allocations, 112 bytes per A* search on this runtime. Both are now private static readonly fields, with exactly the same neighbor order and no writes after initialization or exposure to callers. They are constants in use, not a path cache or shared search workspace. The dominant per-search cost/parent/closed arrays, heap, and returned path still allocate. This is a modest reduction, not a major performance improvement.

`CompanionPathAllocationTests.py` compiles the current production source and a legacy variant that restores per-search offset arrays. All four cases require identical SHA-256 traces of every measured target index and direction float bit pattern, unchanged output checksums, no warmed open-path allocation, and the expected blocked-query allocation reduction. Allocation byte counts are runtime-specific; the executable regression targets .NET 8, not Mono/IL2CPP.

Run:

```sh
python3 Tests/CompanionPathAllocationTests.py /path/to/dotnet
```

No aggregate test runner was changed. Existing actual Forest detour/cache and all-room geometry regressions are also run for this change.
