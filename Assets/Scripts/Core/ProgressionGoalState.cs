namespace Emberfall
{
    public enum ProgressionGoalAction { None, ClaimCore, ExchangeCore, ClaimPending, ClaimRecovery, Equip, UnlockVariant, Ascend, Reforge, OpenPresets }
    // One selected identity and one next action, shared by camp, entry and results.
    public sealed class ProgressionGoalState
    {
        public string Identity,Title,Step,ItemId;
        public int MaterialCost;
        public bool Done,CanAct;
        public ProgressionGoalAction Action;
        public string ActionIdentity {get{return Identity+"/"+(int)Action+"/"+ItemId;}}
        public string ActionLabel
        {
            get
            {
                switch(Action)
                {
                    case ProgressionGoalAction.ClaimCore:return "领取目标核心";
                    case ProgressionGoalAction.ExchangeCore:return "兑换目标核心";
                    case ProgressionGoalAction.ClaimPending:case ProgressionGoalAction.ClaimRecovery:return "领取目标装备";
                    case ProgressionGoalAction.Equip:return "穿戴目标装备";
                    case ProgressionGoalAction.UnlockVariant:return "解锁目标变体";
                    case ProgressionGoalAction.Ascend:return "升华目标装备";
                    case ProgressionGoalAction.Reforge:return "重铸目标装备";
                    case ProgressionGoalAction.OpenPresets:return "打开配装方案";
                    default:return "继续当前目标";
                }
            }
        }
    }
}
