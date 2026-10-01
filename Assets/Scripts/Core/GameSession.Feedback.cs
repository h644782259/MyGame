using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameSession
    {
        public sealed class SystemMessage { public string Text; public float Time; }
        private readonly List<SystemMessage> systemMessages = new List<SystemMessage>();
        public IList<SystemMessage> SystemMessages { get { return systemMessages.AsReadOnly(); } }
        public void LogSystem(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (systemMessages.Count >= 32) systemMessages.RemoveAt(0);
            systemMessages.Add(new SystemMessage { Text = message, Time = Time.unscaledTime });
        }
        public void SpawnCombatDamage(Vector3 position, string value, bool critical)
        {
            if (FloatingNumber.ActiveCount >= 48) return;
            GameObject go = new GameObject(critical ? "Critical Damage" : "Damage");
            go.transform.position = position;
            go.AddComponent<FloatingNumber>().Initialize(value,
                critical ? new Color(1f,.72f,.23f) : new Color(1f,.96f,.85f), critical);
        }
    }
}
