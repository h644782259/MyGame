using System;
using System.Collections.Generic;
namespace Emberfall
{
    // One synchronous attack resolves all targets before counterplay is decided.
    // This is not a timer or delayed damage queue; outer End runs in the same call.
    public static class CombatImpactBatch
    {
        private static int depth;
        private static readonly List<Action> pending = new List<Action>();
        public static void Begin() { depth++; }
        public static void Resolve(Action action)
        {
            if (action == null) return;
            if (depth == 0) action();
            else if (!pending.Contains(action)) pending.Add(action);
        }
        public static void End()
        {
            if (depth <= 0) throw new InvalidOperationException("Unbalanced combat impact batch");
            if (--depth != 0) return;
            if (pending.Count == 0) return;
            var work = pending.ToArray(); pending.Clear();
            foreach (var action in work) action();
        }
    }
}
