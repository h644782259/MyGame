# Unchanged Vanguard source-material inspection

Four primary stills: `Contact24-neutral.png`, `Contact24-material.png`, `Plate-neutral.png`, `Plate-material.png`. The contact pair uses the same locked candidate contact sample 24 as the moving-basic evidence, at its supplementary close camera. Each plate contains Full, Weapon and Back source-asset panels; the neutral/material pair uses identical geometry, pose, camera, lighting, exposure, render settings and pixel dimensions. Plate assembly adds captions outside the original 640×640 native panels without scaling or retouching them. These are **Blender source-authoring images, not Unity gameplay, Standard-shader validation, or imported CollectionModelPreview framing**.

## Actual source audit

Read `/workspace/emberfall_moving_pilot/ArtSource/BlenderPilot/Emberfall-Pilot-Vanguard.blend` without saving it. Blend SHA-256: `40cd32d40e0f715c953fba556623e2c62e409724721f5b954a84ab98818d7c6b`. All six Vanguard meshes have UVMap and the same `Pilot_Atlas_Standard` material. The packed atlas is sRGB, the packed metallic/smoothness image is Non-Color; both packed byte streams equal the existing shipped PNG files exactly (asserted by `render.py`). No textures, assets, source actions, vertices, weights or shipped material values were changed.

`audit.json` records actual node links, image settings and mesh UV swatches. Armor uses steel/brass/amber tiles; body obsidian/cloth/leather; clothes cloth/linen; head obsidian/steel/brass/amber/cloth; sword obsidian/edge/brass/amber/linen/leather; back cloth. UVs select original padded palette tiles, not a newly painted texture. There is no added normal map or emission treatment.

The material images retain the **existing native Blender node graph**:

- Atlas Color → Principled Base Color, with sRGB decode.
- Non-Color mask RGB → Principled Metallic. RGB are equal in this existing grayscale mask, so the source color-to-value connection corresponds to its R channel.
- Mask Alpha → original Subtract node (`1 - alpha`) → Principled Roughness.

No new Unity-to-Blender conversion was introduced. In the existing 8-bit mask, steel/edge/brass tiles have RGB 199/255 and alpha 153/255 (roughness 0.4); other tiles have RGB 13/255 and alpha 46/255 (roughness approximately 0.8196). The native shader retains its other source Principled defaults. The neutral control replaces only the model material with gray `(0.42,0.44,0.47)`, roughness 0.68, metallic 0, specular IOR level 0, as explicitly recorded in the script.

## Unity material comparison — not output equivalence

Existing `Assets/Resources/BlenderPilot/Pilot_Atlas_Standard.mat` selects built-in Standard (shader fileID 46), enables `_METALLICGLOSSMAP`, and assigns the same atlas to `_MainTex` and the mask to `_MetallicGlossMap`. Both texture scale/offset values are `(1,1)/(0,0)`. Saved scalar/color values are:

| Unity saved property | Value |
| --- | --- |
| `_Color` | white RGBA (1,1,1,1) |
| `_Metallic` | 1 |
| `_GlossMapScale` | 1 |
| `_Glossiness` | 0.5 |
| `_SmoothnessTextureChannel` | 0 (metallic map alpha selection) |
| `_Mode` | 0 (opaque) |
| `_ZWrite`, `_SrcBlend`, `_DstBlend` | 1, 1, 0 |

`Assets/Editor/BlenderPilotImport.cs` configures the mask as non-sRGB, atlas as sRGB, alpha from input, and compressed textures. This source review does not execute that importer. `_Glossiness=0.5` is a saved Unity property, **not a fixed roughness 0.5 applied by this Blender rendering**: the unchanged native graph explicitly reads mask alpha and uses `1-alpha`. Likewise Unity Standard's active-map/scalar/keyword behavior, BRDF, reflection probes, color management and imported/compressed sampling have not been recreated or validated. Map-channel correspondence is not evidence of matching rendered output. No claim is made that the material/native plate equals the game or collection preview appearance.

## Cameras, pose and deterministic rendering

`render-manifest.json` records exact camera position/target/orthographic scale, selected mesh groups, evaluated bounds, resolution, contact policy input and lights. Contact uses camera `(5,-8,4.2)`, target `(0,0,1.1)`, orthographic scale 4.7, 800×600. It is a close inspection view, **not the default combat-distance view**. Its pose composes the unchanged native actions with locked actual production layer parameters via the previously verified `LayeredPose` math.

The three plate panels use source Idle at normalized time 0; Full shows all six meshes, Weapon only `Vanguard_Sword`, Back only `Vanguard_Back`. Each part camera is derived from that source mesh's evaluated bounds with a fixed view direction and 1.32× bounds margin, identically for both material modes. This is an explicit source inspection composition, not a test of runtime preview grouping/framing, auto-fit, equipment application, interaction or disposal.

Blender Cycles CPU, one thread, 64 fixed samples, seed 7081, adaptive sampling and denoising off (no OIDN), AgX, 4 total/2 diffuse/2 glossy bounces. Three disk area lights and constant world environment are identical between each pair. These broad source-inspection lights differ from the earlier direct-sun geometry videos; these stills do not upgrade or replace those video quality claims. No invented HDRI, relighting between pairs, post-render color changes, new assets or paid tools.

## Reproduce

```sh
blender -b --threads 1 --python-exit-code 1 --python /workspace/scratch/pilot-material-review/audit.py
blender -b --threads 1 --python-exit-code 1 --python /workspace/scratch/pilot-material-review/render.py
python3 /workspace/scratch/pilot-material-review/compose.py
```

The scripts use the existing checkout for the unchanged blend/assets. The identical LayeredPose helper and exact locked sample24 are copied locally as `layered_pose.py` and `contact-sample.json`; input provenance hashes are recorded in the manifest. The manifest, raw native panels, primary plates, logs, scripts and hashes are retained here. Assets remain unchanged. Existing fractional FBX strict-threshold failure is unrelated and remains a failure. This evidence closes the narrow source-material visibility question, not overall visual-quality acceptance or Unity/device validation.

## Observed limits

The inspected material panels show existing steel-blue, brass and teal-cloth separation, the original sword edge/inset and grip wrapping, and broad shallow cape folds. They also retain the angular helmet/armor, blocklike hands and broad rigid cape profile. Minor sampling noise remains, particularly in occluded cloth/neutral areas. These stills demonstrate existing source materials and geometry; they are not an artistic redesign or a new visual-quality GO.
