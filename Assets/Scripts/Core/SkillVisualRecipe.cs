namespace Emberfall
{
    // Element and silhouette are gameplay identities, independent of palette/accessibility tint.
    public enum SkillVisualRecipe { Neutral, Steel, Ice, Fire, Poison, Lightning, Arcane, Spirit }
    public static class SkillVisualRecipes
    {
        public static SkillVisualRecipe Ultimate(ElementalistSpecialization specialization, int step, bool final)
        {
            if (specialization == ElementalistSpecialization.Burn) return SkillVisualRecipe.Fire;
            if (specialization == ElementalistSpecialization.Shatter) return SkillVisualRecipe.Ice;
            if (final) return SkillVisualRecipe.Arcane;
            return step % 3 == 0 ? SkillVisualRecipe.Fire : step % 3 == 1 ? SkillVisualRecipe.Ice : SkillVisualRecipe.Lightning;
        }
        public static FilledVfxKind Filled(SkillVisualRecipe recipe)
        {
            switch (recipe)
            {
                case SkillVisualRecipe.Fire: return FilledVfxKind.Fire;
                case SkillVisualRecipe.Ice: return FilledVfxKind.Ice;
                case SkillVisualRecipe.Steel: return FilledVfxKind.Sword;
                case SkillVisualRecipe.Lightning: return FilledVfxKind.Lightning;
                case SkillVisualRecipe.Arcane: return FilledVfxKind.Arcane;
                default: return FilledVfxKind.Summon;
            }
        }
    }
}
