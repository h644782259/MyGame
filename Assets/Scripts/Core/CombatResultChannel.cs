namespace Emberfall
{
    // Presentation receipt only. Independent of opportunity clocks and combat state.
    public sealed class CombatResultChannel
    {
        private object owner, target;
        private int epoch, suppressedReceipt;
        private CombatOpportunityKind suppressedKind;
        private bool initialized;
        public string Observe(CombatOpportunityState result,object currentOwner,int currentEpoch,object currentTarget,bool blocked)
        {
            bool changed=initialized&&(!ReferenceEquals(owner,currentOwner)||epoch!=currentEpoch||!ReferenceEquals(target,currentTarget));
            owner=currentOwner;epoch=currentEpoch;target=currentTarget;initialized=true;
            if(blocked||changed||currentTarget==null)
            {if(result.Kind!=CombatOpportunityKind.None){suppressedReceipt=result.Receipt;suppressedKind=result.Kind;}return "";}
            if(result.Kind==suppressedKind&&result.Receipt==suppressedReceipt)return "";
            return result.Caption;
        }
    }
}
