# G02 — distinct protection/healing state feedback

Base: Windows `5d85e47b9fab2489d5b06963a0b896ec19112740`. Implementation and managed production regressions only; no Unity launch, shader compilation in Unity, rendered pixel acceptance, device test or playtest was performed.

- Active guard: stable low-opacity authored Cage outline, visible at birth, no pulsing growth through the actor.
- True defense passive: one stronger reveal, settling after 0.28 seconds below active-guard intensity. No artificial repeated passive cast.
- Ongoing healing: independent third state channel with a low-opacity rising shader band, including rank 1. This does not add mitigation: existing HealingProtection remains untouched. State follows the actual sequence and ends on normal completion, disable, death, epoch/owner changes, or replacement of that healing channel.
- Healing success: 0.32-second accent only after positive actual HP gain. A full caster keeps the ongoing state without a success pulse. Each actually healed companion gets a short low accent at its own transform; a full companion gets none. Existing five event times, amounts, companion fractions and rank-3 final energy remain unchanged.

No raw pooled component is retained. Sequences retain only their non-pooled state anchor; old-sequence cleanup cannot cancel a newer state channel. Guard, passive and healing channels are independent. The shader's new properties default to zero, retaining the original ordinary charge/other-spell branch. Every rental resets the new MPB properties. Runtime material/mesh/texture counts are unchanged; FilledSpell.shader content changes and final resource hash manifests must be recomputed during integration.

The existing 2.6 XZ/Y scale floors and persistent motion 20 remain unchanged. Differentiation uses intensity/UV bands rather than contracting the body envelope. `FinalBodyEnvelopeProductionTests.py` passed its real combined actor geometry clearance and existing compiled layer/old-motion/old-height controls. This is geometric evidence, not a rendered shader assessment.

## Executed evidence

- `protection-production.log`: 240 actual state/sequence/Heal/companion/MPB/lifetime assertions; five compiled negative controls (false full-health pulse, false full-companion pulse, lost passive distinction, lost sequence cleanup, stale rental mode).
- `DefenseIdentityProductionTests.log`: original guard/passive state ownership, immediate child lease release, same-frame rerent, low-tier part count, missing mesh fallback, body envelope negative controls.
- `SkillIdentityCallsiteProductionTests.log`: existing Healing/charge/endpoint integration and negative controls. The former healing stub now actually mutates HP so success feedback can observe a genuine change.
- `RestrictedHealingProductionTests.log`: 640 actual cast/sequence/Heal/potion/companion checks plus seven compiled negative controls. Visual calls remain the explicitly bounded shell in this older suite.
- `ArrowBatchGenerationProductionTests.log`: 114 retained sequence/rental checks plus two generation negative controls. Added unused healing API shell required by extracted Configure/OnDisable; arrow behavior unchanged.
- `final-body-envelope.log`: integrated actor/body/effect geometry and eight compiled regression controls.
- `api-compile.log`: all runtime sources compile for Windows, iOS and Android preprocessor branches with pinned Unity reference assemblies, zero errors. This is not a Unity build.

`protection-attempt1-negative-oracle.log` is retained: the positive 240 assertions passed, but the new false-companion negative failed an earlier, stronger assertion than the script initially expected. The oracle was corrected to that actual first failure; production code did not need adjustment for this test-run failure.

Run targeted tests with `python3 Tests/<name>.py /path/to/dotnet`. The new `ProtectionPresentationProductionTests.py` should be registered by the integration owner. `api_compile.py` takes repository path and pinned reference-directory path and makes temporary compile projects only. Logs and `source-hashes.json` identify this exact reviewed source input; no screenshots or video claim is attached to this shader-only change.
