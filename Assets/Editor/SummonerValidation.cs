using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    public static class SummonerValidation
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        // Call on the fully learned Summoner in an isolated wilderness fixture.
        public static IEnumerator Validate(GameSession game, Action<bool, string> check, Action<string> log)
        {
            PlayerController player = game.Player;
            check(player.HeroClass == HeroClass.Summoner && !game.InDungeon, "Summoner fixture is a wilderness summoner");
            float previousRespawnTimer = Read<float>(game, "respawnTimer");
            Set(game, "respawnTimer", 600f);
            try
            {
            Clear(game);
            player.Teleport(new Vector3(0, 0, -5));
            player.RefreshStats(true);
            game.SetPaused(false);
            SkillRuntime runtime = Read<SkillRuntime>(player, "skillRuntime");
            int frame = Time.frameCount;
            while (Time.frameCount == frame) yield return null;

            foreach (SummonedCompanion.Kind form in (SummonedCompanion.Kind[])Enum.GetValues(typeof(SummonedCompanion.Kind)))
            {
                Clear(game);
                player.Teleport(new Vector3(0, 0, -5));
                runtime.Advance(200); runtime.FillEnergy();
                EnemyController enemy = Spawn(game, EnemyKind.Guardian, player.transform.position + Vector3.forward * 3);
                Set(player, "aimPoint", enemy.transform.position);
                int skill = form == SummonedCompanion.Kind.Wolf ? 2 : form == SummonedCompanion.Kind.Spirit ? 4 : 9;
                Invoke(player, "CastSkill", skill);
                SummonedCompanion pet = Find(player, form);
                check(pet != null && pet.IsAlive && pet.Owner == player, form + " actual class skill creates a living companion belonging to its caster");
                check(Mathf.Abs(pet.RemainingLifetime - (form == SummonedCompanion.Kind.Treant ? 16 : 24)) < .01f,
                    form + " awakened companion has its documented finite lifetime");
                float health = enemy.Health;
                double until = Time.timeAsDouble + .8;
                while (Time.timeAsDouble < until) yield return null;
                check(!enemy.IsAggro && enemy.Health == health, form + " does not automatically provoke nearby ordinary wildlife");
                enemy.TakeDamage(1, Vector3.zero, impact: false);
                health = enemy.Health;
                double deadline = EditorApplication.timeSinceStartup + 5;
                while (enemy.Health >= health && EditorApplication.timeSinceStartup < deadline) yield return null;
                check(enemy.Health < health, form + " deals real combat damage after the player provokes an enemy");
                if (form == SummonedCompanion.Kind.Treant)
                    check(enemy.StatusEffects.KnockedDown, "Ancient guardian's actual heavy hit applies knockdown");
                float petHealth = pet.Health;
                pet.TakeDamage(25);
                check(Mathf.Abs(pet.Health - (petHealth - 25)) < .01f && pet.IsAlive, form + " has real damageable health");
                SummonedCompanion.HealAll(player, .1f);
                check(pet.Health > petHealth - 25 && pet.Health <= pet.MaxHealth, form + " receives bounded companion healing");
                pet.transform.position = enemy.transform.position + Vector3.right * .5f;
                check(SummonedCompanion.ThreatTarget(enemy, player.transform.position) == pet, form + " can become the nearby monster's real combat target");
                pet.TakeDamage(pet.MaxHealth * 2);
                check(!pet.IsAlive && SummonedCompanion.Count(player, form == SummonedCompanion.Kind.Treant) == 0, form + " death immediately removes the companion from its active cap");
            }

            Clear(game);
            player.Teleport(new Vector3(0, 0, -5));
            SummonedCompanion oldest = null;
            for (int i = 0; i < 7; i++)
            {
                SummonedCompanion pet = SummonedCompanion.Summon(player, game, i % 2 == 0 ? SummonedCompanion.Kind.Wolf : SummonedCompanion.Kind.Spirit, 3, player.transform.position, 10);
                if (i == 0) oldest = pet;
                check(SummonedCompanion.Count(player) == Math.Min(i + 1, 4), "Ordinary summon population never exceeds four during repeated casts");
            }
            check(!oldest.IsAlive, "Repeated summons replace the oldest ordinary companion");
            SummonedCompanion firstTree = SummonedCompanion.Summon(player, game, SummonedCompanion.Kind.Treant, 3, player.transform.position, 10);
            SummonedCompanion tree = SummonedCompanion.Summon(player, game, SummonedCompanion.Kind.Treant, 3, player.transform.position, 10);
            check(!firstTree.IsAlive && tree.IsAlive && SummonedCompanion.Count(player) == 4 && SummonedCompanion.Count(player, true) == 1,
                "Guardian replacement preserves the separate four ordinary plus one guardian cap");

            foreach (SummonedCompanion pet in UnityEngine.Object.FindObjectsOfType<SummonedCompanion>())
                if (pet.Owner == player) pet.TakeDamage(pet.MaxHealth * .65f);
            tree = Find(player, SummonedCompanion.Kind.Treant);
            float treeBefore = tree.Health;
            typeof(PlayerController).GetProperty("Health").SetValue(player, player.MaxHealth * .35f, null);
            float playerBefore = player.Health;
            runtime.Advance(200); runtime.FillEnergy();
            Invoke(player, "CastSkill", 6);
            double healUntil = Time.timeAsDouble + 1.15;
            while (Time.timeAsDouble < healUntil) yield return null;
            check(tree.Health > treeBefore && player.Health > playerBefore, "Actual rejuvenation ticks heal both the caster and living companions");

            float lifetime = tree.RemainingLifetime;
            game.SetPaused(true);
            try
            {
                double pauseUntil = EditorApplication.timeSinceStartup + .12;
                while (EditorApplication.timeSinceStartup < pauseUntil) yield return null;
                check(Mathf.Abs(tree.RemainingLifetime - lifetime) < .001f, "Companion lifetime freezes while the game is paused");
            }
            finally { game.SetPaused(false); }
            player.Teleport(player.transform.position + Vector3.right);
            frame = Time.frameCount;
            while (Time.frameCount < frame + 2) yield return null;
            check(SummonedCompanion.Count(player) == 0 && SummonedCompanion.Count(player, true) == 0, "Changing encounter epoch dismisses every old companion");
            SummonedCompanion doomed = SummonedCompanion.Summon(player, game, SummonedCompanion.Kind.Wolf, 1, player.transform.position, 10);
            float savedHealth = player.Health;
            try
            {
                typeof(PlayerController).GetProperty("Health").SetValue(player, 0f, null);
                Invoke(doomed, "Update");
                check(!doomed.gameObject.activeSelf && SummonedCompanion.Count(player) == 0, "Owner death immediately dismisses its companion in the live update path");
            }
            finally { typeof(PlayerController).GetProperty("Health").SetValue(player, savedHealth, null); }
            SummonedCompanion expired = SummonedCompanion.Summon(player, game, SummonedCompanion.Kind.Spirit, 1, player.transform.position, 10);
            typeof(SummonedCompanion).GetProperty("RemainingLifetime").SetValue(expired, 0f, null);
            Invoke(expired, "Update");
            check(!expired.gameObject.activeSelf && SummonedCompanion.Count(player) == 0, "Lifetime expiry runs through live dismissal and frees the ordinary summon slot");

            Clear(game);
            EnemyController normal = Spawn(game, EnemyKind.Guardian, new Vector3(-4, 0, 3));
            EnemyController boss = Spawn(game, EnemyKind.Guardian, new Vector3(4, 0, 3), true);
            normal.StatusEffects.Knockup(1f, 1.8f);
            boss.StatusEffects.Knockup(1f, 1.8f);
            check(Read<float>(boss.StatusEffects, "airborneDuration") < Read<float>(normal.StatusEffects, "airborneDuration") && Read<float>(boss.StatusEffects, "airborneHeight") < Read<float>(normal.StatusEffects, "airborneHeight"),
                "Boss resistance reduces both airborne duration and lift height");
            double riseUntil = Time.timeAsDouble + .12;
            while (Time.timeAsDouble < riseUntil) yield return null;
            CombatModel normalModel = normal.GetComponentInChildren<CombatModel>();
            normalModel.Animate(0, 0, false);
            check(normal.StatusEffects.IsAirborne && normal.StatusEffects.AirborneHeight > .15f && normalModel.transform.localPosition.y > .15f,
                "Airborne control raises the actual monster model above the ground");
            float airborneRemaining = Read<float>(normal.StatusEffects, "airborneTime");
            normal.StatusEffects.Knockup(1.2f, 2f);
            check(Read<float>(normal.StatusEffects, "airborneTime") == airborneRemaining, "Repeated hits cannot restart the same airborne arc");

            normal.transform.rotation = Quaternion.identity;
            float before = normal.Health;
            normal.TakeDamage(100, Vector3.back, impact: false);
            float front = before - normal.Health;
            before = normal.Health;
            normal.TakeDamage(100, Vector3.forward, impact: false);
            float behind = before - normal.Health;
            Set(normal, "preparing", true);
            before = normal.Health;
            normal.TakeDamage(100, Vector3.back, impact: false);
            float exposed = before - normal.Health;
            check(Mathf.Abs(front - 65) < .01f && Mathf.Abs(behind - 100) < .01f && Mathf.Abs(exposed - 100) < .01f,
                "Guardian stone armor reduces frontal damage 35%, but rear hits and its windup expose full damage");
            Set(normal, "preparing", false);
            Clear(game);
            player.Teleport(new Vector3(0, 0, -5));
            player.RefreshStats(true);
            runtime.Advance(200); runtime.FillEnergy();
            log("SUMMONER passed real skill summons, neutral wildlife, attacks, health/healing, finite lifetime, 4+1 cap, pause, epoch/death cleanup, visible airborne resistance and guardian armor.");
            }
            finally { Set(game, "respawnTimer", previousRespawnTimer); }
        }

        private static void Clear(GameSession game)
        {
            foreach (EnemyController enemy in game.Enemies.ToArray())
                if (enemy != null) { enemy.gameObject.SetActive(false); UnityEngine.Object.Destroy(enemy.gameObject); }
            game.Enemies.Clear();
            foreach (SummonedCompanion pet in UnityEngine.Object.FindObjectsOfType<SummonedCompanion>())
                if (pet.Owner == game.Player) pet.Dismiss();
        }
        private static EnemyController Spawn(GameSession game, EnemyKind kind, Vector3 at, bool boss = false)
        {
            Invoke(game, "SpawnEnemy", kind, 1000, at, boss);
            EnemyController enemy = game.Enemies[game.Enemies.Count - 1];
            enemy.enabled = false;
            return enemy;
        }
        private static SummonedCompanion Find(PlayerController owner, SummonedCompanion.Kind kind)
        {
            foreach (SummonedCompanion pet in UnityEngine.Object.FindObjectsOfType<SummonedCompanion>())
                if (pet.Owner == owner && pet.Form == kind && pet.IsAlive) return pet;
            return null;
        }
        private static T Read<T>(object target, string name) { return (T)target.GetType().GetField(name, Private).GetValue(target); }
        private static void Set(object target, string name, object value) { target.GetType().GetField(name, Private).SetValue(target, value); }
        private static object Invoke(object target, string name, params object[] values) { return target.GetType().GetMethod(name, Private).Invoke(target, values); }
    }
}
