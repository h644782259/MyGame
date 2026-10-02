# Quality maintenance evidence

Frozen candidates: Windows `d1e1606ccddec402862141346f6fefa364f77d87`, iOS `61c5b2527e52f8316630edc8a2e4e5a56b72d1c5`, Android `d0a8b8ee790b8f3e8f6fc9d61711816c15cbbd4c`. This review branch adds evidence only and is not a different runtime candidate.

`HUD-before.json` and `HUD-after.json` preserve all five warmed allocation samples and source hashes. The author commit named in the after report differs from the integration commit, but all measured runtime source hashes were independently matched to the frozen candidate. Actual draw bodies and constructor run; host getters/rendering boundaries are no-allocation managed doubles. This does not measure total Unity HUD allocation, native resources, font rendering or frame time.

Companion logs compare actual production selection/cache and Route/FindPath against a compiled legacy per-search-array variant. Controlled moving endpoints are benchmark inputs, not live combat trajectories. Four target/direction traces match exactly; blocked cases save112B/search (~0.257%), open cases remain zero. Main A* buffer allocations remain.

Scoped logs are supporting evidence, not additional aggregate check counts. The three scenery wrappers were executed independently by their author; the other62 SourceTests entries were executed by root during integration. These separate runs are not an all-repository-Test-files claim. The five formerly stale contracts now run through registered production drivers; their coverage/boundary map is in Tests/SceneryPresentationCoverage.md and Docs/Quality-Regression-Maintenance.md.

No Unity6 Editor, shader/material rendering, real JsonUtility, device controls/gameplay, native platform package or performance acceptance was executed. The full196 aggregate run is still in progress at this evidence commit; no complete aggregate pass is claimed here. Final reports will be added in a subsequent evidence-only commit.
