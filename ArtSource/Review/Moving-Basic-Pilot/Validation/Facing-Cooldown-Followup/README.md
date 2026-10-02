# Supplemental cooldown sequence

Frozen Windows source: `dcd75ac8a96806788868696f05cffcf4a4d583f4`. Scratch-only test variant: no tracked source change and no aggregate rerun.

90 assertions and two compiled negative controls passed. For each 90°, 180° and −90° turn, only the first of four frames commits the basic attack. Its action ID stays 21; ages are .4983333, .5183333, .5383333 and .5583333; authored visibility remains false and gait advances once per frame.

Cooldown values (.46, .44, .42, .40) are supplied by the host because the extracted production Update tail excludes the earlier cooldown decrement. That tail executes the actual cooldown branch, FaceAim, Animate and AnimateHero clock. This does not test damage, the complete Update or Unity frame execution. The script records its original scratch snapshot paths; manifest.json hashes every frozen source input and the variant. Original raw stdout is preserved bytewise.
