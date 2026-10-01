using System;
namespace Emberfall
{
    [Serializable]
    public sealed class CombatReviewConfiguration
    {
        public string id;
        public HeroClass hero;
        public ElementalistSpecialization specialization;
        public int level = 50, seed = 61453;
        public int[] skillRanks = { 1,1,1,1,1,1,1,1,1,1 };
        public string equipment = "starter-equipment; deterministic seed; exact profile saved alongside log";
    }
    public static class CombatReviewConfigurations
    {
        public static CombatReviewConfiguration[] Create()
        {
            return new[] {
                Make("vanguard",HeroClass.Vanguard),
                Make("arcanist-shatter",HeroClass.Arcanist,ElementalistSpecialization.Shatter),
                Make("arcanist-burn",HeroClass.Arcanist,ElementalistSpecialization.Burn),
                Make("ranger",HeroClass.Ranger), Make("summoner-bonded",HeroClass.Summoner)
            };
        }
        private static CombatReviewConfiguration Make(string id,HeroClass hero,ElementalistSpecialization specialization=ElementalistSpecialization.None)
        { return new CombatReviewConfiguration { id=id,hero=hero,specialization=specialization }; }
    }
}
