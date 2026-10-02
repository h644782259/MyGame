# Frozen Vanguard static source review

Evidence-only scripts for frozen runtime/assets at `49e624b3f0729ff44761a262a062dc67b849dd84`. No `.blend`, FBX, textures, runtime source, or platform settings are modified. This is **native Blender source review, not Unity screenshots, gameplay acceptance, or art-quality GO**.

Reproduce (Blender 4.3.2 and Pillow):

```sh
blender -b --threads 1 --python ArtSource/BlenderPilotEvidence/render_static_review.py -- REPOSITORY BASELINE_GEOMETRY_JSON OUTPUT
python3 ArtSource/BlenderPilotEvidence/assemble_review.py OUTPUT
```

The baseline JSON is the pinned `03422ab` production geometry export described by `Tools/ArtSourceEvidence/export_silhouettes.py`; select Vanguard / base outfit / no wings (`hero=0,tier=-1,wing=0`). Vertices and triangles come from executed production recipes and managed transform reconstruction. Unity axes are converted into Blender axes. Shared-vertex normals are averaged; this is not a Unity material or renderer match. `--combat-only` rerenders only the comparison pair.

## What the images establish

- Current source front/side/back use unchanged authored geometry, evaluated idle frame 0, and original atlas materials.
- Old/new combat views share neutral gray material, direct sun lights, floor, resolution and camera. The camera copies default `GameSession.cs` AdventureCamera values: perspective vertical FOV 48°, distance `19 × 1.2041595`, pitch 48.36646°, yaw 0, target height .7. It does not recreate cover avoidance, mobile aspect ratios, enemies, environments or postprocessing.
- Cycles CPU uses exactly one thread, 128 fixed samples, no denoising, no adaptive sampling, and zero bounce limits. Portraits are 640 × 800; combat frames are 800 × 600. Caption sheets paste images at original pixel size, without retouching.
- Floor z is −.045, while this evaluated hero's minimum z is approximately .035. The visible staging clearance is not evidence of Unity foot-contact behavior.
- Manifest records source hashes and camera settings. Source blend hash is checked after rendering. No authored asset is saved.

## Remaining visual limitations

The clearer front and side views still show angular helmet/armor silhouettes, abrupt shoulder-to-arm and elbow-to-forearm transitions, simple cylindrical hands, and narrow straight back cloth with limited volume in profile. The rear view reads as a broad stiff plate with a small lower notch rather than draping cloth. More samples do not refine these meshes or establish acceptable joints, cloth dynamics, animation blending or Unity shading. Those require separate geometry work and actual engine review. No performance, device compatibility or player-experience claims follow from these images.

Final review sheets are published under `ArtSource/Review/Vanguard-Static-Review.png` and `ArtSource/Review/Vanguard-Combat-Distance-Comparison.png`. Individual render outputs remain outside Assets under `/workspace/scratch/pilot-static-review-49e624b`. Delivery/upload is owned by the parent task.
