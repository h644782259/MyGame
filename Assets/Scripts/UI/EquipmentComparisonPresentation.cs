using System;
namespace Emberfall
{
    public static class EquipmentComparisonPresentation
    {
        public static EquipmentMechanic ActiveMechanic(ItemData item, HeroClass hero)
        {
            if(item==null || item.mechanic==EquipmentMechanic.None || !Enum.IsDefined(typeof(EquipmentMechanic),item.mechanic))return EquipmentMechanic.None;
            return BuildCatalog.MechanicClass(item.mechanic)==hero && BuildCatalog.MechanicSlot(item.mechanic)==item.slot ? item.mechanic:EquipmentMechanic.None;
        }
        public static string Label(ItemData item, HeroClass hero)
        {
            var mechanic=ActiveMechanic(item,hero);
            if(mechanic==EquipmentMechanic.None)return "无生效机制";
            return BuildCatalog.MechanicName(mechanic)+((mechanic==EquipmentMechanic.FrostEcho||mechanic==EquipmentMechanic.CinderTrail)?" · "+(item.mechanicVariantUnlocked&&item.mechanicVariant==1?"变体 B":"变体 A"):"");
        }
        public static bool SameMechanism(ItemData current, ItemData next, HeroClass hero)
        {
            var a=ActiveMechanic(current,hero);var b=ActiveMechanic(next,hero);
            return a==b && (a==EquipmentMechanic.None||Label(current,hero)==Label(next,hero));
        }
        public static string Changes(ItemData current, ItemData next, HeroClass hero)
        {
            string gained=Label(next,hero),lost=Label(current,hero);
            if(SameMechanism(current,next,hero))return "机制保留 · "+gained;
            return "获得 · "+gained+"\n失去 · "+lost;
        }
        public static string Description(ItemData item,HeroClass hero)
        {
            if(item!=null&&item.mechanic!=EquipmentMechanic.None&&ActiveMechanic(item,hero)==EquipmentMechanic.None)
                return "该机制与当前职业或部位不匹配，不生效。";
            return ActiveMechanic(item,hero)==EquipmentMechanic.None?"无特殊效果。":BuildCatalog.MechanicDescription(item.mechanic);
        }
        public static string CollectionState(bool owned,bool worn,bool strongest)
        {return !owned?"未获得 · 仅试穿":(worn?"已穿戴":"已收藏")+(strongest?" · 属性来源":" · 非属性来源");}
        public static FashionData Trial(FashionSlot slot,Rarity rarity)
        {
            if(!Enum.IsDefined(typeof(FashionSlot),slot)||!Enum.IsDefined(typeof(Rarity),rarity))throw new ArgumentOutOfRangeException();
            return new FashionData{id="fashion-"+(int)slot+"-"+(int)rarity,slot=slot,rarity=rarity};
        }
        public static FashionData Receipt(ChestReward receipt)
        {
            if(receipt==null||!receipt.Slot.HasValue||!receipt.Rarity.HasValue||!Enum.IsDefined(typeof(FashionSlot),receipt.Slot.Value)||!Enum.IsDefined(typeof(Rarity),receipt.Rarity.Value))return null;
            return Trial(receipt.Slot.Value,receipt.Rarity.Value);
        }
    }
}
