# Moving basic attack: bounded authored-pilot candidate

This work concerns the existing optional Vanguard pilot, which remains disabled by default. It does not extend the equipment, class or skill roster, change combat timing, add root motion, or change saves.

## Observed baseline

Merged Windows source `6afcb19b3c293f521279c3290d25920ad750f116` has the same tree as reviewed PR24 head `d1e1606ccddec402862141346f6fefa364f77d87`. Moving basic attacks currently switch from the imported character to the procedural character. Procedural legs continue moving: frozen legs are **not** the shipped baseline. Simply removing the eligibility speed guard would freeze the imported legs because the Basic clip keys all 19 bones, with identity lower-body poses.

The baseline inspection uses actual production procedural geometry and motion, the three compatible starter items, and the original Blender Move action. It reconstructs that narrow scenario in Blender; it is not Unity gameplay footage. The default combat-camera view makes the helmet, mantle and weapon silhouette switch discernible. Static inspection alone does not establish transition quality or gameplay feel.

## Candidate ownership and transitions

The sampler caches and validates the 19 named bones and their parent relationships. Root retains sampled local TRS. Nine lower-body bones are Pelvis, both Thigh/Shin/Foot chains, and both Tabard bones. Nine upper-body bones are Spine, Head, Mantle, and both UpperArm/Forearm/Hand chains. The five runtime sword sockets must be unique direct children of Hand.R and retain their authored local transforms.

Idle, sampled from the existing presentation time, and Move, sampled from the existing displacement phase, form a common local-TRS base using the existing smoothed normalized locomotion speed. This removes the previous idle/move speed-threshold discontinuity. Normal locomotion and basic attacks use the same base; no extra transition clock is introduced. Static basic attacks consequently retain the current Idle lower-body base rather than the Basic clip's identity lower-body channels.

During a basic attack, Root and the nine lower-body bones retain that base. The upper nine use the authoritative Basic action phase. Attack weight is one through phase .75, then decreases with smoothstep to zero at phase one, blending back to the current base. Position and scale interpolate linearly; rotation uses spherical interpolation. Immediate phase .52 contact remains exact, with no added windup or blend-in. An instantaneous upper-body contact change remains intentional; this is not a promise of continuous upper-body motion across an immediate gameplay hit.

Hit and stationary representative skill retain their full-body paths. Moving skills, other skills, charge, death, airborne/landing, strafe/backstep, incompatible gear and fashion retain procedural fallback. The sword and its sockets follow Hand.R; there is no independent weapon interpolation.

Moving-basic eligibility also checks the actual accepted walking direction against the player's current facing. Player Update records movement before FaceAim can turn toward a target; the existing smoothed direction alone can remain stale for several frames after a 90- or 180-degree turn. SetLocomotion therefore records the accepted vector in the owner's world basis, and the sampler reprojects it through the current owner orientation. This extra guard never advances locomotion again. A valid zero displacement clears the old direction while allowing the existing speed decay; unknown or nonfinite movement is rejected. The ordinary default-off path does no extra transform projection.

The imported visual becomes visible only after a successful complete sample. A sampler exception hides it and restores the procedural renderers in the same call, preventing an incomplete combination from being presented. This boundary does not catch gameplay execution or introduce saved state. Existing action-over-hit priority remains: cancelling an action during a still-live hit window can reveal the Hit pose before normal locomotion resumes.

## Acceptance boundary

Acceptance requires actual production sampler/state tests with meaningful hierarchical TRS doubles, compiled negative controls, matched native Blender baseline/candidate videos at the default combat camera, and FBX roundtrip evidence. The comparison must preserve source timing, camera, lighting and geometry. Rendered smoothness, feet motion and recovery must be inspected, not inferred from sampling counts.

The registered facing probe executes extracted production walking and Update-tail code, the original FaceAim method, and BasicAttack through its visual commit. It covers target turns, subsequent frames, stops, collision-accepted zero movement, owner versus model orientation and exact gait advancement. Input, aim resolution, traversal and engine objects remain test boundaries; damage/effects after the visual commit are excluded. The isolated-preview clock test checks the final Idle pose at the expected preview time rather than the last sampled clip's timestamp.

Unity Editor import, native clip bindings, skinned bounds, shadows, shader appearance, touch/device performance and gameplay feel remain unverified without an available licensed Editor/device environment. Current source inspection has not established a shadow or bounds defect; this candidate must not add speculative renderer toggles. Validation results and final retention decision are pending.
