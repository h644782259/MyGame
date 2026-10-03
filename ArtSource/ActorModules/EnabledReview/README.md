# Corrected renderer-enabled construction evidence

The original ActorModules `Constructions/` exports included active GameObjects even where their MeshRenderer was disabled by equipment selection. Those historical PR images are preserved and should not be used as evidence of exact equipped visibility or chest/bow occlusion. `export_constructions.py` now also checks `Renderer.enabled`. These 12 replacement images use the corrected geometry, same front-right Blender camera, and separate paths.

This evidence correction does not change any Assets, Tests or Tools source. Source meshes, transforms, materials and gameplay are unchanged. The export executes actual builders with managed transform/resource doubles; the pictures remain Blender gray previews, not Unity engine/device evidence. Cloth, line-renderer bowstrings, animation and material contrast are not represented as they would be in a live frame.

Read-only inspection conclusions:

- Treant: both authored shoulder shells actually intersect the torso at rest. Respectively 4 and 2 unique sampled shoulder vertices lie inside the actual transformed torso mesh. The visual impression of suspended arms is not evidence of a detached mesh. This does not prove contact throughout all animation poses.
- Ranger: the equipment chest wrap is existing `BuildClassEquipmentArmor` geometry, not an actor-module replacement. In this right-front static view, 182/828 bow vertex samples are behind the wrap; only 38/828 are exclusively covered by it. Other body/arm parts already cover most of the far-side left-hand bow. In the base outfit, which has no equipment wrap, 426/512 bow vertex samples are also behind other parts. These counts are geometric ray samples, not visible pixel percentages; they exclude bow self-occlusion, strings and shading. There is evidence of partial static overlap, not proof that the chest wrap newly destroys Ranger identity. The quiver, arrows and hood remain visible.

No new character asset change is recommended solely on these pictures. An in-engine moving-camera/gameplay-scale comparison would be the appropriate next acceptance check before changing the ranger's proportions or treant's attachment geometry.

Reproduction:

```sh
python3 ArtSource/ActorModules/export_constructions.py "$PWD" /tmp/actor-enabled /path/to/dotnet
python3 ArtSource/ActorModules/validate_constructions.py /tmp/actor-enabled/geometry.json ArtSource/ActorModules/EnabledReview
python3 ArtSource/ActorModules/EnabledReview/measure_readability.py /tmp/actor-enabled/geometry.json
blender -b --factory-startup --threads 4 --python ArtSource/ActorModules/render_constructions.py -- /tmp/actor-enabled/geometry.json ArtSource/ActorModules/EnabledReview/Constructions
```
