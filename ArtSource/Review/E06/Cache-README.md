# Renderer display-group invalidation

RendererGroupCache marks membership dirty via child/parent/destroy callbacks and rebuilds only when dirty. Pilot hidden-state recovery keeps per-renderer identity and restores objects which leave the group. Collection preview invalidates framing/render state when group membership changes. Stable visual ticks reuse the array.

Unity does not emit transform callbacks for adding/replacing only a Renderer component on an existing GameObject. The real ApplyEquipment and changed ApplyFashion entry points explicitly invalidate every observer at the model root, including the preview cache. CollectionModelPreview.Invalidate also invalidates membership. Any future direct component-only edit must call that entry point. Current construction paths create and parent new GameObjects and therefore trigger structural notifications. Aim/physics geometry caches are outside this change.

Committed pilot adapter and collection lifecycle tests exercise hierarchy edits, component replacement through the real equipment entry point, shared-cache invalidation and restored enable states. Compiled negative controls remove the notification or retain stale group members. Logs here record the managed boundary checks at their named snapshots; the final aggregate report is the authority for the final tree. No Unity lifecycle/render result is claimed.
