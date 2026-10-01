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
print('PASS: room tactics pause, route, support, marker and transition wiring (source contracts only)')

assert "dungeon ? (RoomChainRun!=null ? -12 : -9) : -10" in read("Assets/Scripts/Core/GameSession.cs")
