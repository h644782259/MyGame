using UnityEngine;namespace Emberfall{public sealed partial class EnemyController{public float MaxHealth,Health,damage;void ApplySpawnStats(GameSession game,int level,bool boss){var kind=Kind;            int challengeTier = game.InDungeon ? game.DungeonTier : 1;
            MaxHealth = CombatBalance.EnemyHealth(level, challengeTier, boss, kind);
            damage = CombatBalance.EnemyDamage(level, challengeTier, boss);
            if(game.ChapterActive)
            {MaxHealth*=ChapterDefinition.HealthMultiplier(game.ActiveChapterDifficulty);damage*=ChapterDefinition.DamageMultiplier(game.ActiveChapterDifficulty);}
            Health = MaxHealth;}}}