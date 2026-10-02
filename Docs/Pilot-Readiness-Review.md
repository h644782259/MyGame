# Imported pilot readiness and preview review

Weapon and Back preview modes explicitly recognize the authored `Vanguard_Sword` and `Vanguard_Back` FBX groups. The existing sampled-pose envelope accumulates their actual renderer bounds; it does not substitute generic mannequin bounds when these imported parts are present. Procedural group names remain supported.

The hero and prop factory now refuse the imported appearance when any of these checks fail:

- Shared atlas material missing; shader absent, unsupported, or different from the required Built-in Standard shader; required properties/metallic map keyword missing.
- Albedo or metallic-smoothness texture missing, wrong texture type or invalid dimensions.
- No enabled, active mesh-bearing renderer.
- Any required clip missing, empty, nonfinite or nonpositive in duration.
- A required clip cannot affect any transform in the imported hierarchy at four sampled points, or sampling throws.

Failed instances are deactivated and destroyed before returning null, so existing factory callers build their procedural fallback. Borrowed material, mesh and texture assets remain shared and are never destroyed. Clip readiness sampling saves and restores all local TRS; automatic animation components are disabled first. This is a bounded creation-time binding smoke check, not full Unity animation/import/deformation acceptance.

Targeted production-path checks extend the existing adapter and preview-host suites. Negative controls remove imported sword/back group matching, bypass missing-material rejection, and accept unbound animations; each must fail its specific assertion. Resource/engine/animation boundaries are managed test doubles. Actual Unity 6 imported skinned bounds, shader compilation and clip deformation remain pending.
