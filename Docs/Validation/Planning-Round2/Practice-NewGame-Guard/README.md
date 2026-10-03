# Practice NewGame API guard

Combined independent review found that direct NewGame on the memory-only practice service reached pendingChestContexts.Remove(null). Normal UI uses guarded CreateNewSlot, but the public API boundary still required an explicit guard. NewGame now rejects practice before profile/path mutation.

production.log contains 33 real service assertions and eight compiled negative controls, including exact removal of the NewGame guard. The profile identity/fields and original disk bytes remain unchanged. first-negative-oracle.log preserves the initial oracle failure: after the chest context integration, removing the existing chest guard raised a null-key exception before the old named assertion. The current test explicitly checks no exception and still rejects the same removed-guard mutant.

No Unity runtime execution.
