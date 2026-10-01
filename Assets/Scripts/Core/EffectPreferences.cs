using UnityEngine;

namespace Emberfall
{
    /// <summary>Local accessibility preferences, independent of character saves.</summary>
    public static class EffectPreferences
    {
        public static float CombatTextScale { get { return Mathf.Clamp(PlayerPrefs.GetFloat("Emberfall.CombatTextScale", 1.25f), 1f, 1.8f); } set { PlayerPrefs.SetFloat("Emberfall.CombatTextScale", Mathf.Clamp(value, 1f, 1.8f)); PlayerPrefs.Save(); } }
        public static float EffectsScale { get { return Mathf.Clamp(PlayerPrefs.GetFloat("Emberfall.EffectsScale", 1f), .25f, 1f); } set { PlayerPrefs.SetFloat("Emberfall.EffectsScale", Mathf.Clamp(value, .25f, 1f)); PlayerPrefs.Save(); } }
        public static bool CameraShake { get { return PlayerPrefs.GetInt("Emberfall.CameraShake", 1) != 0; } set { PlayerPrefs.SetInt("Emberfall.CameraShake", value ? 1 : 0); PlayerPrefs.Save(); } }
        public static bool ReducedEffects { get { return EffectsScale <= .5f; } }
    }
}
