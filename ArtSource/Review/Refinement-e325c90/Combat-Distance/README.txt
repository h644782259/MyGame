Default-distance authored Vanguard comparison (verification artifact only)

Baseline source: c5a734e / original packed blend, SHA256 6588211d349a2e523c7192799a3fb4e52077393115fa925dd38436de365de5e9.
Refined source: 4d645c3 / packed blend, SHA256 40cd32d40e0f715c953fba556623e2c62e409724721f5b954a84ab98818d7c6b.

Camera matches unobstructed AdventureCamera defaults in GameSession.cs:
vertical FOV48 degrees; distance19 times1.2041595 =22.8790305m;
pitch48.36646 degrees; yaw0; target Unity(0,.7,0).
Blender camera(0,15.200000729688314,17.80000042214634), target(0,0,.7).
Perspective vertical sensor32mm, focal length32/(2*tan(24 degrees)).
800x600 native frames. Both use original unchanged packed atlas and Idle frame0.
Cycles CPU1,128 fixed samples, no denoiser, no adaptive sampling, zero bounce limits.
Same direct-sun lights, neutral background and staging floor z=-.045m.
This staging clearance is not evidence about Unity foot contact.
No enemies, HUD, postprocessing, cover avoidance or mobile aspect-ratio simulation.

Reproduce:
blender -b --python-exit-code 1 --threads 1 --python render_combat_distance.py -- BEFORE.blend AFTER.blend OUTPUT
python3 assemble_comparison.py

Before.png and After.png are native renderer outputs, without retouching, zoom or cropping.
Comparison.png pastes both native images at their original pixel dimensions and adds outside captions.
manifest.json records source/output/script hashes and projected mesh pixel bounds.
No frozen candidate source/assets or git commits were changed by this verification.

Reviewed result at native size:
The character occupies approximately44.21 x61.97 pixels in both frames. Overall
class silhouette and color blocking look essentially unchanged. A careful side-by-side
inspection suggests only a very small mantle surface/hem difference. The elbow bridge
and individual cloth folds are not reliably readable at this native800x600 default
camera distance. These stills do not establish a meaningful normal-distance readability
improvement, much less Unity/device quality. The earlier close views establish only
the narrow source-geometry change. No pixel-difference metric is used as a player-
experience claim.

A temporary output-basename collision in the first run was corrected by preserving
each completed native frame and checking its SHA256 against its original render
record. Pixels were not changed. The provided reproduction script fixes only that
light-variable name; the executed script is retained separately for provenance.
