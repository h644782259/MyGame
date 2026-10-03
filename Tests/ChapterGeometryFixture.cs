// Unrelated profile/reward contracts for compiling the production ChapterNode definition.
// No navigation, generation, objective geometry or chapter progression implementation is copied.
namespace Emberfall
{
    public sealed class GameProfile
    {
        public int chapterCompletedMask,chapterFirstRewardMask,chapterRevision,chapterHighestAdventureTier,chapterPriorAdventureTier;
        public int chapterDifficultyRewardRevision,chapterDifficultyRewardMask,mechanicMaterials;
        public int chapterMasteryMask;public int[] chapterMasteryTiers;
        public int[] chapterHighestDifficulties;
        public long chapterRewardSequence;
        public string lastChapterRewardId;
    }
    public static class TierRewardBand { public static int Materials(int basis,int tier){throw new System.NotSupportedException("No reward execution in geometry fixture");} }
}
