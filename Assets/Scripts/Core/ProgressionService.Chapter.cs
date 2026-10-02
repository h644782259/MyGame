using System;
namespace Emberfall
{
    public partial class ProgressionService
    {
        private ChapterRunReceipt chapterAttempt;
        public bool TryBeginChapterNode(ChapterNode node,ChapterDifficulty difficulty,int tier,out ChapterRunReceipt receipt)
        {
            receipt=null;
            if(!HasActiveSave||!ChapterProgression.CanEnter(Profile,node,difficulty)||tier<1||tier>Math.Min(100,HighestAdventureTier+1)||Profile.chapterRewardSequence==long.MaxValue)return Fail("章节或难度尚未解锁。");
            int materials=ChapterProgression.CompletionMaterials(Profile,node,tier);
            receipt=new ChapterRunReceipt(node,difficulty,tier,materials,Profile.chapterRewardSequence+1,SaveFilePath);
            chapterAttempt=receipt;LastError=string.Empty;return true;
        }
        public void CancelChapterRun(){chapterAttempt=null;}
        public bool TryCompleteChapterNode(ChapterRunReceipt receipt)
        {
            if(receipt==null||receipt.SavePath!=SaveFilePath)return Fail("章节结算已失效。");
            if(receipt.Sequence==Profile.chapterRewardSequence&&receipt.Id==Profile.lastChapterRewardId){LastError=string.Empty;return true;}
            if(!ReferenceEquals(receipt,chapterAttempt)||receipt.Sequence!=Profile.chapterRewardSequence+1||!ChapterProgression.CanEnter(Profile,receipt.Node,receipt.Difficulty))return Fail("章节结算已失效。");
            GameProfile candidate=Snapshot();ChapterProgression.Normalize(candidate);
            int index=(int)receipt.Node,bit=1<<index;
            candidate.chapterRevision=1;candidate.chapterCompletedMask|=bit;candidate.chapterFirstRewardMask|=bit;
            candidate.chapterHighestDifficulties[index]=Math.Max(candidate.chapterHighestDifficulties[index],(int)receipt.Difficulty+1);
            candidate.mechanicMaterials=(int)Math.Min(999999L,(long)candidate.mechanicMaterials+receipt.Materials);
            candidate.chapterRewardSequence=receipt.Sequence;candidate.lastChapterRewardId=receipt.Id;
            if(receipt.Node==ChapterNode.StarPlatform)
            {
                if(candidate.highestAdventureTier>candidate.chapterHighestAdventureTier)candidate.chapterPriorAdventureTier=Math.Max(candidate.chapterPriorAdventureTier,candidate.highestAdventureTier);
                candidate.chapterHighestAdventureTier=Math.Max(candidate.chapterHighestAdventureTier,receipt.Tier);
                candidate.highestAdventureTier=Math.Max(candidate.highestAdventureTier,receipt.Tier);
            }
            return CommitCandidate(candidate);
        }
    }
}
