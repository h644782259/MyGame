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
}
