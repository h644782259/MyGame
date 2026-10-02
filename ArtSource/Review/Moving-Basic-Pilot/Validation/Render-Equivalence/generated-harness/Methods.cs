using UnityEngine;namespace Emberfall{public enum EnemyKind { Slime, Goblin, Wisp, Guardian }public enum FashionSlot { Wings, Weapon }public enum ItemSlot { Weapon, Armor, Relic }public enum Rarity { Common, Rare, Epic, Legendary }public enum EquipmentMechanic { None, FrostEcho, CinderTrail, ReturningBlade, VenomSpread, TwinSummonResonance }public class ItemData
    {
        public string id;
        public string name;
        public ItemSlot slot;
        public Rarity rarity;
        public int level;
        public int attack;
        public int defense;
        public int health;
        public int upgradeLevel;
        public EquipmentMechanic mechanic;
        public bool locked;
        public int mechanicVariant;
        public bool mechanicVariantUnlocked;
        public int balanceRevision;
        // Persistent origins make changing a rank reversible without repeatedly
        // rounding already-upgraded attributes. Legacy saves initialize these once.
        public bool upgradeBaseInitialized;
        public int baseAttack;
        public int baseDefense;
        public int baseHealth;
        public int upgradeAnchorLevel;
        public int upgradeAnchorAttack;
        public int upgradeAnchorDefense;
        public int upgradeAnchorHealth;
    }public sealed partial class CombatModel{public void PlayAction(int skill, bool basic, float basicInterval = 0)
        {
            if (!isHero) return;
            if (actionDuration > 0 && actionAge < actionDuration || recoveryAge < .12f) BeginVisualRecovery(false);
            weaponActionId++;
            pilotCharging=false;
            actionSkill = skill;
            actionBasic = basic;
            actionAge = 0;
            actionStartedFrame = Time.frameCount;
            swingCount++;
            actionDuration = basic ? BasicActionTimeline.Duration(heroClass == HeroClass.Ranger, basicInterval > 0 ? basicInterval : SkillDamageBudgets.BasicInterval(heroClass))
                : SkillDamageBudgets.SkillPoseDuration(heroClass, skill, false);
            actionAge = actionDuration * (basic ? BasicActionTimeline.Contact(heroClass == HeroClass.Ranger) : SkillDamageBudgets.SkillPoseStart(heroClass, skill, false));
            CommitActionPose();
        }
private void CommitActionPose()
        {if(isHero&&spine!=null){AdvanceVisualMotion(Time.deltaTime);AnimateHero(locomotion.Speed,1,false,0);}}
private void OnDestroy()
        {
            foreach(Material material in palette.Values) if(material!=null) Destroy(material);
        }
public void ApplyEquipment(ItemData weapon, ItemData armor, ItemData relic)
        {
            if (!isHero || spine == null) return;
            bool resumePilot=pilotVisible;
            SetBlenderPilotVisible(false); // Restore owned renderer states before equipment builders edit them.
            pilotHasGear = !PilotStarterCompatible(weapon,ItemSlot.Weapon) || !PilotStarterCompatible(armor,ItemSlot.Armor) || !PilotStarterCompatible(relic,ItemSlot.Relic);

            string weaponKey = EquipmentKey(weapon);
            string armorKey = EquipmentKey(armor);
            string relicKey = EquipmentKey(relic);
            if (weaponKey != equipmentWeaponKey)
            {
                equipmentWeaponKey = weaponKey;
                if (equipmentWeapon != null) { equipmentWeapon.gameObject.SetActive(false); Destroy(equipmentWeapon.gameObject); }
                equipmentWeapon = null;
                SetBaseWeaponVisible(weapon == null);
                weaponStructure = new WeaponStructure(weapon == null ? 0 : new EquipmentAppearance(weapon).Tier);
                if (bowstring != null)
                {
                    bowstring.SetPosition(0, WeaponAnchorLocal(WeaponVisualAnchor.BowUpperTip));
                    bowstring.SetPosition(2, WeaponAnchorLocal(WeaponVisualAnchor.BowLowerTip));
                }
                if (weapon != null) BuildEquipmentWeapon(new EquipmentAppearance(weapon));
                RefreshWeaponFashion();
            }
            if (armorKey != equipmentArmorKey)
            {
                equipmentArmorKey = armorKey;
                if (equipmentArmor != null) { equipmentArmor.gameObject.SetActive(false); Destroy(equipmentArmor.gameObject); }
                if (equipmentLeftShoulder != null) { equipmentLeftShoulder.gameObject.SetActive(false); Destroy(equipmentLeftShoulder.gameObject); }
                if (equipmentRightShoulder != null) { equipmentRightShoulder.gameObject.SetActive(false); Destroy(equipmentRightShoulder.gameObject); }
                if(equipmentHead!=null){equipmentHead.gameObject.SetActive(false);Destroy(equipmentHead.gameObject);}
                equipmentHead=null;
                equipmentArmor = null;
                equipmentLeftShoulder = equipmentRightShoulder = null;
                SetBaseCostumeVisible(armor == null);
                if (armor != null) BuildEquipmentArmor(new EquipmentAppearance(armor));
            }
            if (relicKey != equipmentRelicKey)
            {
                equipmentRelicKey = relicKey;
                if (equipmentRelic != null) { equipmentRelic.gameObject.SetActive(false); Destroy(equipmentRelic.gameObject); }
                equipmentRelic = null;
                if (relic != null) BuildEquipmentRelic(new EquipmentAppearance(relic));
            }
            if(resumePilot&&!pilotHasGear)SetBlenderPilotVisible(true); // Capture the rebuilt procedural state, then hide it.
        }
private static string EquipmentKey(ItemData item)
        {
            return item == null ? null : item.id + "/" + item.level + "/" + (int)item.rarity + "/" + item.upgradeLevel;
        }
private static void SetVisible(Transform parent, string name, bool visible)
        {
            if (parent == null) return;
            foreach (Transform child in parent)
                if (child.name == name)
                {
                    Renderer renderer = child.GetComponent<Renderer>();
                    if (renderer != null) renderer.enabled = visible;
                }
        }
private void SetBaseWeaponVisible(bool visible)
        {
            if (swordRig != null)
                foreach (string name in new[] { "Wrapped Hilt", "Gold Pommel", "Crossguard", "Forged Sword", "Blade Fuller" })
                    SetVisible(swordRig, name, visible);
            if (staffRig != null)
                foreach (string name in new[] { "Staff", "Staff Gold", "Arcane Crystal", "Staff Crystal Crown" })
                    SetVisible(staffRig, name, visible);
            if (bowRig != null)
                foreach (string name in new[] { "Bow Limb", "Bow Grip" })
                    SetVisible(bowRig, name, visible);
        }
public void CancelAction() { BeginVisualRecovery(); weaponActionId++; actionAge = actionDuration = 0; }private void AnimateHero(float speed, float attack, bool hurt, float dt)
        {
            if(dt>0)AdvanceVisualMotion(dt);
            speed = smoothedSpeed = locomotion.Speed;
            if (tailoredCloth != null) tailoredCloth.SetMotion(speed, actionDuration > 0 && actionAge < actionDuration ? 1 : 0);
            gaitPhase = locomotion.Phase;
            if (actionDuration > 0 && Time.frameCount != actionStartedFrame) actionAge = Mathf.Min(actionAge + dt, actionDuration);
            float t = actionDuration > 0 ? actionAge / actionDuration : 1f;
            bool acting = t < 1f;
            if (SampleBlenderPilot(acting,t,hurt)) return;
}public int WeaponActionId {get{return weaponActionId;}}public void SetLocomotion(Vector3 localDisplacement,float delta,float referenceSpeed,bool walking,bool airborne=false,float jumpProgress=0)
        { RecordPilotWalking(localDisplacement,delta,referenceSpeed,walking,airborne); pilotAirborne=airborne; locomotion.Advance(localDisplacement.x,localDisplacement.z,delta,referenceSpeed,walking,airborne,jumpProgress); }public void ResetLocomotion() { pilotWalkingKnown=false;pilotAcceptedWalkingWorld=Vector3.zero;locomotion.Reset(); visualMotion.Reset(); visualYawReady=false; visualMotionFrame=-1; recoveryAge=1; }}}