#!/usr/bin/env python3
"""Kill-reward adapter contracts. Source inspection, not Unity execution."""
from pathlib import Path

root = Path(__file__).resolve().parent.parent
service = (root / "Assets/Scripts/Core/ProgressionService.cs").read_text(encoding="utf-8")
session = (root / "Assets/Scripts/Core/GameSession.cs").read_text(encoding="utf-8")
checks = 0


def check(ok, why):
    global checks
    if not ok:
        raise AssertionError(why)
    checks += 1


award = service[service.index("public void GrantEnemyKillReward("):service.index("public void GrantExperience(")]
kill = session[session.index("public void OnEnemyKilled("):session.index("private IEnumerator NextWave(")]
check(award.index("if (gold < 0 || experience < 0)") < award.index("Profile.kills ="),
      "invalid reward arguments are checked before any profile mutation")
check("(long)Profile.kills + 1" in award and "Math.Min(int.MaxValue" in award,
      "kill count saturates without integer overflow or resetting prior progress")
check(award.count("Commit();") == 1 and "CommitCandidate(" not in award and "Profile =" not in award,
      "one live reward commit retains the existing profile instead of rolling earnings back")
check("AddGold(" not in award and "GrantExperience(" not in award and "Save();" not in award,
      "combined grant does not invoke separate saving reward APIs")
check(award.index("int earnedLevel = Profile.level;") < award.index("Commit();") < award.index("LeveledUp(level)"),
      "earned event range is captured before the one commit and its final-state Changed callback")
check("for (int level = oldLevel + 1; level <= earnedLevel; level++)" in award,
      "each earned level is notified in ascending order independently of callback profile changes")
check(kill.index("AdventureResultPolicy.AcceptsKill(") < kill.index("Progression.GrantEnemyKillReward(") and
      kill.index("!Enemies.Remove(enemy)") < kill.index("Progression.GrantEnemyKillReward("),
      "the existing current-enemy admission guard prevents duplicate or stale kill awards")
check(kill.count("Progression.GrantEnemyKillReward(gold, experience);") == 1 and
      all(old not in kill for old in ("Profile.kills++", "Progression.AddGold(", "Progression.GrantExperience(")),
      "session awards kills, gold and XP once through the coupled service operation")
check(kill.index("int level = Progression.Profile.level;") < kill.index("Progression.GrantEnemyKillReward(") <
      kill.index("Progression.RollLoot(Progression.Profile.level + (boss ? 1 : 0)"),
      "reward values use pre-kill level while item rolling sees the gained level")
check(kill.index("DeliverEnemyLoot(loot, position);") < kill.index("Progression.Save();") and kill.count("Progression.Save();") == 1,
      "final save remains after callbacks and loot delivery to persist real direct mutations")
delivery = session[session.index("private void DeliverEnemyLoot("):session.index("public GroundLootPickup SpawnGroundLoot(")]
check("Progression.CollectLoot(loot)" in delivery and "SpawnGroundLoot(loot, position)" in delivery,
      "actual dropped-item collection keeps its independent transaction and retained-identity fallback")
commit = service[service.index("private void Commit()"):service.index("private void RaiseChanged()")]
check(commit.index("Save();") < commit.index("RaiseChanged();"),
      "one Changed sees the save result before ordered level callbacks, including write failure")
print(f"PASS: {checks} earned-kill reward source contracts (not Unity execution)")
