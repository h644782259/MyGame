# Free-order expedition seal evidence

Windows delivery `3d2c33ea3a4262d1186e0bede073d2041cc551cb` has the same tree (`5481a6e53248797f4a82ae05465d2653ff935927`) as the isolated validation snapshot `1e61f1166e7a2ec60c37aa066557a9699b77ce4b`. iOS delivery `2243793d8fc63eaf859861468ddf99779fc6d534` has the same tree as compiled candidate `388b85f41f5a773b1a81860f49e6920f4c8cb2df`. The difference is rebasing onto parent-merged PR22 main, with no file changes. Android feature candidate is `e94f2914b435f7f0824f643f7401bfeb6863f8b7`, separate from the PR22-only delivery archive.

This review branch preserves evidence; its tip is not a new runtime candidate. `platform-sync.json` records original source identities and protected platform/font file hashes. The final handoff maps tested and rebased delivery heads.

The aggregate is one shared-source default-JIT run. Separate platform API compilations use cached Unity 2021.3.33 references, not Unity 6 or native builds. Recorded UI rectangles do not test Unity font rendering; managed scene and persistence-service doubles do not establish real disk IO or device play. Seed/navigation assertions do not establish gameplay feel.

`baseline-source-audit.md` records five pre-existing unregistered source-contract failures. These are not reported as passes and were not altered in this feature. No Unity Editor, engine import/shader/JsonUtility/PlayMode, touch/controller hardware or device performance acceptance was available.

Final aggregate: **191/191 passed**, ended 2026-10-02T17:46:30.261731+00:00, with no source changes during execution. Report SHA256 `555afa33acab9def224a8f93c2f1fd912d0e081099ceb37276c926fde188b3da`. Three separate API compilations passed. Explicit Redrock checks remain separately counted.

`First-Room-Choice/probe.py`, `report.json` and logs execute the real RunChoices/Rooms and production InputBlocked/UpdateTimeScale/ConfirmRoomInterlude, unlike the simpler registered host fixture. All 90 chain assertions and both compiled negative controls passed. Scene and save-service boundaries remain doubles; this probe is additional evidence, not another aggregate check.
