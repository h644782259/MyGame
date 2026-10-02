"""Production wiring contracts; real combat callbacks and GUI still need Unity acceptance."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def read(p):return (root/'Assets/Scripts'/p).read_text()
service=read('Core/ProgressionService.cs')
exp=read('Core/GameSession.Expedition.cs')
mode=read('Core/GameSession.Modes.cs')
room=read('Core/GameSession.RoomChain.cs')
camp=read('UI/GameUI.Expedition.cs')+read('UI/GameUI.MobileWorkshop.cs')
selector=read('UI/GameUI.ProgressionGoal.cs')
checks=0
def check(ok,why):
 global checks
 checks+=1
 assert ok,why
check('Progression.HighestUnlockedAdventureTier' in exp,'all five entry modes share authoritative tier cap')
check('ticket.Reward.Materials,DungeonTier)' in mode and 'TierRewardBand.Materials(4,DungeonTier),DungeonTier)' in room,'successful alternate-mode settlement passes actual tier into the atomic receipt transaction')
check('!ModeRun.RewardPending' in mode and 'RoomChainRun.Failed||RoomChainRun.RewardClaimed' in room,'failed/incomplete runs cannot admit progression settlement')
check('Profile.classTutorialCompleted' in exp and 'RecordClassTutorialEvidence(hero)' in exp,'real class callback commits persistent evidence')
check('key=="职业能力"?4' not in exp and 'Progression.RecordTutorialEvidence(tutorialBit)' in exp,'generic casts never complete class mechanic tutorial; real basics use transactional persistence')
check('SummonedCompanion.Count(Player)' in exp and 'session.ClassTutorialVisible' in camp,'starter commanded pets and learned-skill availability gate lesson display')
check('ClassTutorialText' in camp and 'classTutorialCompleted' in camp,'both camp UIs render actual mechanic lesson independently of legacy mask')
for p in ['UI/GameUI.Expedition.cs','UI/GameUI.MobileWorkshop.cs','UI/GameUI.Modes.cs','UI/GameUI.RunRecap.cs']:
 check(('SelectedProgressionGoal(' in read(p) and 'selectedGoal.Title' in read(p)) if p=='UI/GameUI.Modes.cs' else 'ProgressionGoalStatus(' in read(p),'camp, entry and results read shared selected-goal state: '+p)
check('data.Snapshot.RewardMaterials' in read('UI/GameUI.RunRecap.cs'),'results include actual settled run gains')
check('SelectProgressionGoal(kind,id,tier,kind==ProgressionGoalKind.Reforge?session.Progression.Profile.level:0)' in selector and 'BeginTouchScroll' in selector,'goal replacement requires an explicit selection in bounded scroll UI')
check('progressionGoalCharacter!=session.Progression.CurrentSlotId' in selector,'goal modal retires on character change')
check('OpenProgressionGoals' in camp and 'OpenBuildPlans' in camp and 'campTab=1' in camp and 'campTab=0' in camp,'postclear tutorial links to core, class route, saved builds and next chosen goal')
print(f'PASS: {checks} adventure progression UI/lifecycle contracts (not Unity execution)')
