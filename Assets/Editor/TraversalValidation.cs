using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace Emberfall.Editor
{
    public static class TraversalValidation
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        public static IEnumerator Validate(GameSession game, Action<bool, string> check, Action<string> log)
        {
            log("TRAVERSAL — river crossing, solid cover, routes and leap landing");
            check(!game.InDungeon, "Traversal fixture begins in the actual wilderness");
            Vector3 bank = new Vector3(7, 0, -4.5f), landing = bank + Vector3.forward * 4.8f;
            check(!WorldTraversal.IsWalkable(new Vector3(7,0,-2)), "River water blocks ground movement");
            Vector3 stopped = WorldTraversal.Move(bank, Vector3.forward * 7);
            check(stopped.z < -3f && WorldTraversal.IsWalkable(stopped), "Swept fast ground movement stops at the river bank");
            check(WorldTraversal.HasGroundPath(new Vector3(0,0,-6), new Vector3(0,0,3)), "The authored bridge is a continuous walkable crossing");
            check(WorldTraversal.CanLeap(bank, landing), "A clear opposite-bank landing allows a leap across the river");
            check(!WorldTraversal.CanLeap(bank, new Vector3(7,0,-2)), "A leap cannot land in deep water");
            check(!WorldTraversal.CanLeap(new Vector3(5,0,-8), new Vector3(11,0,-8)), "A leap cannot pass through a solid boulder");
            check(WorldTraversal.HasLineOfSight(bank, landing), "Ranged combat can cross open water");
            check(!WorldTraversal.HasLineOfSight(new Vector3(5,0,-8), new Vector3(11,0,-8)), "Boulders block projectile lines");
            Vector3 routeStart = new Vector3(-8,0,-5), routeEnd = new Vector3(-8,0,7);
            var path = WorldTraversal.FindPath(routeStart, routeEnd);
            check(path.Count > 1, "Creatures find a non-direct route through the bridge");
            Vector3 previous = routeStart;
            bool clear = true, crossedBridge = false;
            foreach (Vector3 point in path)
            {
                bool segmentClear = WorldTraversal.HasGroundPath(previous, point);
                if (!segmentClear) log("Blocked route segment: " + previous + " -> " + point);
                clear &= segmentClear;
                crossedBridge |= Mathf.Abs(point.x) < 1.5f && Mathf.Abs(point.z + 1) < 1.2f;
                previous = point;
            }
            check(clear && crossedBridge && Vector3.Distance(previous, routeEnd) < .1f, "Every route segment stays walkable and crosses the visible bridge");
            var route = new WorldTraversal.Route();
            Vector3 follower = routeStart;
            bool stayedOnGround = true;
            for (int i = 0; i < 600 && Vector3.Distance(follower, routeEnd) > .3f; i++)
            {
                follower = WorldTraversal.Move(follower, route.Direction(follower, routeEnd) * .24f);
                stayedOnGround &= WorldTraversal.IsWalkable(follower);
            }
            check(stayedOnGround && Vector3.Distance(follower, routeEnd) <= .3f,
                "Actual cached steering plus collision reaches the opposite bank without cutting corners or stalling");
            foreach (EnemyController enemy in game.Enemies) check(WorldTraversal.IsWalkable(enemy.transform.position, enemy.IsBoss ? 1f : .45f), "Spawned enemies are placed on valid ground");
            PlayerController player = game.Player;
            Vector3 original = player.transform.position;
            bool enabled = player.enabled;
            player.enabled = false;
            player.Teleport(bank);
            MethodInfo jump = typeof(PlayerController).GetMethod("TryJump", Hidden);
            MethodInfo tick = typeof(PlayerController).GetMethod("AdvanceJump", Hidden);
            check(jump != null && tick != null, "Player leap integration is available");
            check((bool)jump.Invoke(player, new object[] { Vector3.forward }), "Player starts a valid bank-to-bank jump");
            tick.Invoke(player, new object[] { .275f });
            check(player.transform.position.y > 1f, "Jump has a real vertical arc above the water");
            tick.Invoke(player, new object[] { .3f });
            check(!player.IsJumping && Mathf.Abs(player.transform.position.y) < .01f && Vector3.Distance(player.transform.position, landing) < .15f,
                "Jump lands on the validated opposite bank");
            MethodInfo blink = typeof(PlayerController).GetMethod("TryBlink", Hidden);
            typeof(PlayerController).GetField("dodgeCooldown", Hidden).SetValue(player, 0f);
            typeof(PlayerController).GetField("traversalFrame", Hidden).SetValue(player, -1);
            player.Teleport(new Vector3(5,0,-8));
            check(!(bool)blink.Invoke(player, new object[] { Vector3.right }) && player.BlinkCooldown == 0,
                "A blocked blink cannot cross a boulder or consume its cooldown");
            player.Teleport(bank);
            check((bool)blink.Invoke(player, new object[] { Vector3.forward }) && Vector3.Distance(player.transform.position, landing) < .1f && player.BlinkCooldown > 2,
                "A valid blink crosses the river immediately and starts its cooldown");
            player.Teleport(original);
            player.enabled = enabled;
            yield return null;
        }
    }
}
