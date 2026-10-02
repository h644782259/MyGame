using System;
namespace Emberfall
{
    // A quote captures the target, not a promise to chase later player levels.
    public sealed class ReforgeQuote
    {
        public string ItemId {get;}
        public int FromLevel {get;}
        public int TargetLevel {get;}
        public int GoldCost {get;}
        internal string SavePath {get;}
        internal ReforgeQuote(string id,int from,int target,int gold,string savePath)
        {ItemId=id;FromLevel=from;TargetLevel=target;GoldCost=gold;SavePath=savePath;}
    }
    public partial class ProgressionService
    {
        public static int ReforgeGoldCost(int fromLevel,int targetLevel)
        {
            if(fromLevel<1||targetLevel>100||targetLevel<=fromLevel)throw new ArgumentOutOfRangeException();
            return (targetLevel-fromLevel)*(fromLevel+targetLevel+21);
        }
        public ReforgeQuote QuoteReforge(string id,int targetLevel=0)
        {
            ItemData item=FindItem(id);int target=targetLevel==0?Profile.level:targetLevel;
            if(item==null||target<=item.level||target>Profile.level||target>100||item.level<1||
                MechanicGoalEligibility(id,ProgressionGoalKind.Reforge).Length>0)return null;
            return new ReforgeQuote(id,item.level,target,ReforgeGoldCost(item.level,target),SaveFilePath);
        }
        public string ReforgeLockReason(ReforgeQuote quote,bool inCamp)
        {
            if(!inCamp)return "只能在营地重铸机制装备。";
            if(quote==null)return "请选择可成长的本职业机制装备及有效目标等级。";
            ItemData item=FindItem(quote.ItemId);
            if(quote.SavePath!=SaveFilePath||item==null||item.level!=quote.FromLevel||quote.TargetLevel>Profile.level||
                quote.TargetLevel<=item.level||quote.GoldCost!=ReforgeGoldCost(item.level,quote.TargetLevel))return "重铸报价已变化，请重新确认。";
            string reason=MechanicGoalEligibility(quote.ItemId,ProgressionGoalKind.Reforge);if(reason.Length>0)return reason;
            return Profile.gold<quote.GoldCost?"重铸需要 "+quote.GoldCost+" 金币（现有 "+Profile.gold+"），不消耗星烬碎片。":string.Empty;
        }
        public bool ReforgeMechanic(ReforgeQuote quote,bool inCamp)
        {
            string reason=ReforgeLockReason(quote,inCamp);if(reason.Length>0)return Fail(reason);
            GameProfile candidate=Snapshot();candidate.gold-=quote.GoldCost;
            ItemData item=candidate.inventory.Find(x=>x.id==quote.ItemId);item.level=quote.TargetLevel;
            item.upgradeLevel=0;item.upgradeBaseInitialized=false;SetRolledStats(item);EnsureUpgradeBasis(item);
            if(IsEquipped(candidate,item.id))ApplyUpgradeRank(item,candidate.slotUpgradeRanks[(int)item.slot]);
            return CommitCandidate(candidate);
        }
    }
}
