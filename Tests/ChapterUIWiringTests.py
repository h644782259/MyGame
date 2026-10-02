"""Chapter consumer routing contracts and old-routing mutations; not rendered UI evidence."""
from pathlib import Path
r=Path(__file__).resolve().parents[1]/'Assets/Scripts/UI'
ui=(r/'GameUI.cs').read_text();hubs=(r/'GameUI.Hubs.cs').read_text();chapter=(r/'GameUI.Chapter.cs').read_text();mobile=(r/'GameUI.Mobile.cs').read_text();modes=(r/'GameUI.Modes.cs').read_text()
def routed(main,npc):
 return 'else if (panel == Panel.Chapter) DrawChapterSelection();' in main and 'if (kind == HubNpcKind.Exchange) { OpenChapterSelection();return; }' in npc
assert routed(ui,hubs)
assert not routed(ui.replace('else if (panel == Panel.Chapter) DrawChapterSelection();',''),hubs),'missing entry dispatch mutation rejected'
assert not routed(ui,hubs.replace('OpenChapterSelection();return;','campTab=1;panel=Panel.Camp;')),'old NPC direct exchange mutation rejected'
assert 'CurrentHub' not in hubs[hubs.index('private void OpenNearbyHubNpc()'):hubs.index('private string HubInventoryTitle')],'all town exchange NPCs use one story entry'
assert 'private void OpenChapterExchange()' in chapter and 'campTab=1;panel=Panel.Camp;' in chapter and '"机制兑换"' in chapter,'old exchange remains reachable'
assert 'if(session.NearChapterExit)session.EnterNextChapterRoom();' in mobile,'real touch action advances chapter, not legacy room'
assert 'session.ChapterObjectiveCompact' in modes and modes.index('if(session.ChapterActive)')<modes.index('if(session.RoomChainRun!=null)'),'chapter mobile state precedes nullable legacy mode access'
assert ui.index('else if(session.ChapterFinished)DrawChapterResult();')<ui.index('else if(session.ModeFinished)'),'chapter result does not fall into legacy mode reward recap'
assert 'session.ChapterRewardMaterials' in chapter and 'if(!failed&&!pending)' in chapter,'committed result uses captured actual reward'
assert 'if(panel==Panel.Chapter||session.ChapterFinished)return;' in ui,'toast cannot cover fixed chapter actions'
assert all(name not in chapter for name in ['SelectedArenaMode','SelectedDungeonTier','SelectedChallengeMode']),'chapter choices never change legacy selections'
print('PASS: chapter NPC/entry/exit/result wiring and old-routing mutation controls')
