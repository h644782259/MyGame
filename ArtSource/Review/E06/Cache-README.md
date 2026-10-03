# Renderer display-group invalidation

RendererGroupCache marks membership dirty via child/parent/destroy callbacks and rebuilds only when dirty. Pilot hidden-state recovery keeps per-renderer identity and restores objects which leave the group. Collection preview invalidates framing/render state when group membership changes. Stable visual ticks reuse the array.

Unity does not emit transform callbacks for adding/replacing only a Renderer component on an existing GameObject. The real ApplyEquipment and changed ApplyFashion entry points explicitly invalidate every observer at the model root, including the preview cache. CollectionModelPreview.Invalidate also invalidates membership. Any future direct component-only edit must call that entry point. Current construction paths create and parent new GameObjects and therefore trigger structural notifications. Aim/physics geometry caches are outside this change.

Committed pilot adapter and collection lifecycle tests exercise hierarchy edits, component replacement through the real equipment entry point, shared-cache invalidation and restored enable states. Compiled negative controls remove the notification or retain stale group members. Logs here record the managed boundary checks at their named snapshots; the final aggregate report is the authority for the final tree. No Unity lifecycle/render result is claimed.

## PR35 teardown correction

Structural invalidation now drops subscriptions for nodes which have left the cache root immediately, even when no later Read occurs. Another cache rooted inside the detached subtree keeps its own subscriptions. CombatModel.OnDestroy restores previously hidden renderer states, disposes the pilot cache and clears its references. Repeated teardown is safe.

The persistent RendererCacheRelease test in Tests/BlenderPilotAdapterProductionFixture.cs runs from the normal adapter suite; it is not only a temporary runner. It detaches a surviving subtree, destroys the model without another Sample/Read, checks original visible/hidden states, empty old cache membership/root/observer references, retained local-cache ownership and final disposal. Negative controls remove actual OnDestroy cleanup and detached-observer removal separately. Raw targeted rerun outputs are Cache-release-pilot.log and Cache-release-preview.log. These tests execute production cache and ownership logic against managed Unity lifecycle boundaries, not a native engine lifetime acceptance.
