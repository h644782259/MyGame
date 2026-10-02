using System;
using System.IO;

namespace Emberfall.Editor
{
    // Editor-only fixture setup. The native recorder and managed configuration
    // checks use this same path; no live-game menus or progression rules change.
    public static class CombatReviewBuildSetup
    {
        public static void Apply(ProgressionService service,CombatReviewConfiguration config,string isolatedDirectory)
        {
            if(service==null||config==null||string.IsNullOrEmpty(isolatedDirectory)||
                Path.GetFullPath(service.SaveDirectory)!=Path.GetFullPath(isolatedDirectory)||
                service.Profile.heroClass!=config.hero||service.Profile.level!=1)
                throw new InvalidOperationException("Fixture requires a fresh matching role in its explicit isolated directory.");
            var p=service.Profile;p.level=config.level;p.xp=0;p.specialization=config.specialization;
            p.skillRanks=(int[])config.skillRanks.Clone();p.masteryRanks=(int[])config.masteryRanks.Clone();
            p.masteryRevision=1;p.masteryCore=config.masteryCore;p.summonerRoute=config.summonerRoute;p.tutorialMask=15;
            service.Save();if(!string.IsNullOrEmpty(service.LastError))throw new InvalidOperationException(service.LastError);
            if(config.mechanism==EquipmentMechanic.None)return;
            var item=service.CreateMechanicItem(config.mechanism);
            if(!service.CollectLoot(item)||!service.Equip(item.id))throw new InvalidOperationException(service.LastError);
        }
    }
}
