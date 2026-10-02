from pathlib import Path
root=Path(__file__).resolve().parents[1]
def read(path):return (root/path).read_text()
s=read('Assets/Scripts/Core/GameSession.RoomChain.cs')
t=read('Assets/Scripts/Core/GameSession.RoomTactics.cs')
e=read('Assets/Scripts/Combat/EnemyController.cs')
w=read('Assets/Scripts/World/WorldBuilder.TacticalRooms.cs')
assert 'RoomTactics.NextSeed(runSeed,previousRoomSeed,beforePreviousRoomSeed)' in s
assert 'RoomChainRun=new RoomChainState(runSeed)' in s
assert 'RoomTactics.EventRoom(runSeed)' in s
assert 'WorldTraversal.CanReach' in s and 'WorldTraversal.CanReach' in t
assert 'if(!SaveBeforeLeaving())return false;' in s
assert s.index('if(!SaveBeforeLeaving())return false;')<s.index('RoomChainRun.Next(true,false)')
assert 'Player.RetireCombatForWorldTransition()' in s and 'roomEnemies.Clear()' in s
assert 'RoomChainRun.Finished||InputBlocked||Player==null' in t
assert 'WorldTraversal.HasLineOfSight(enemy.transform.position,target)' in t
assert 'enemy==roomSupplier||enemy.IsBoss' in t and 'roomSupplier.IsDead' in t
assert 'amount *= session.RoomSupportMultiplier(this)' in e
assert 'WorldTraversal.HasLineOfSight(enemy.transform.position,roomSupplier.transform.position)' in t
assert 'TacticalRoomGeometry.Register(layout)' in w and 'TacticalRoomGeometry.Walls(layout)' in w
assert 'MakeRoomObjective(first)' in t and '2.4f,.09f' in w
assert 'TickRoomTactics();' in read('Assets/Scripts/Core/GameSession.cs')

assert "Player.Teleport(dungeon&&RoomChainRun!=null?TacticalRoomGeometry.Entrance:" in read("Assets/Scripts/Core/GameSession.cs")
assert "Player.Teleport(TacticalRoomGeometry.Entrance)" in s
assert "Vector3 entrance=Entrance;" in read("Assets/Scripts/World/TacticalRoomGeometry.cs")
assert "var entrance=TacticalRoomGeometry.Entrance;" in read("Tests/TacticalRoomGeometryTests.cs")

mobile=read('Assets/Scripts/UI/GameUI.Mobile.cs');modes=read('Assets/Scripts/UI/GameUI.Modes.cs')
assert 'TouchRect(l.AdventureStatus)' in mobile
assert 'RoomObjectivePresentation.Create(session.RoomChainRun,session.RoomCaptureInside,session.RoomCaptureContested,session.InputBlocked,session.RoomSupplyActive)' in modes
for value in ['objective.Title','objective.ProgressText','objective.Hint','objective.Fraction','objective.SupportHint']:assert value in modes
assert 'RoomCaptureContested=contested;' in t and 'RoomCaptureInside=Vector3.Distance(Player.transform.position,target)<2.4f;' in t

assert "session.IsRoomSupplier(this)" in e and "6米内可见同伴减伤30%" in e

print('PASS: room tactics pause, route, support, mobile objective and shared arrival wiring (source contracts only)')

# Desktop and mobile must both describe the full boss-room completion condition.
assert 'if(run.Room.Boss)return "击败首领与护卫";' in t
assert 'session.SpecialAdventure?session.ModeObjectiveStatus' in read('Assets/Scripts/UI/GameUI.cs')
assert 'return RoomObjectiveStatus;' in read('Assets/Scripts/Core/GameSession.Modes.cs')
assert 'TacticalObjectiveStatus' in s
assert '"击败首领与护卫"' in read('Assets/Scripts/UI/RoomObjectivePresentation.cs')
print('PASS: desktop/mobile boss objective includes required guards (source contracts only)')
