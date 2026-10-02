"""Consumer wiring and old-subtitle mutation controls; not a Unity rendering test."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def read(name):return (root/'Assets/Scripts'/name).read_text()
def method(source,name):
 a=source.index(name);b=source.index('{',a)+1;depth=1
 while depth:
  depth+=(source[b]=='{')-(source[b]=='}');b+=1
 return source[a:b]
def hooked(desktop,mobile):
 return 'BlessingSubtitle(false)' in method(desktop,'private void DrawBlessingChoice()') and 'BlessingSubtitle(true)' in method(mobile,'private void DrawMobileBlessingChoice()')
desktop=read('UI/GameUI.Expedition.cs');mobile=read('UI/GameUI.MobileBlessings.cs')
assert hooked(desktop,mobile),'both rendered headers must consume authoritative subtitle'
assert not hooked(desktop.replace('BlessingSubtitle(false)','"首房 · 定打法 / 补资源与生存"'),mobile),'old desktop subtitle mutation must be detected'
assert not hooked(desktop,mobile.replace('BlessingSubtitle(true)','"星泉 · 强化搭配 / 补短板"')),'old mobile subtitle mutation must be detected'
helper=read('UI/GameUI.Blessings.cs')
assert 'RoomBlessingPreview.Subtitle(session.RoomChainRun,session.RunChoices.CompletedWave,fallback)' in helper
assert '选择一项 · 仅本局生效' in helper and 'session.DungeonWave' in helper,'ordinary wave fallback preserved'
spawn=read('Core/GameSession.RoomChain.cs')
assert 'index<plan.EnemyCount' in spawn and 'bool boss=plan.Boss&&index==0' in spawn,'boss plan is spawned as one boss with remaining guards'
assert 'Boss ? 3 : 6' in read('Core/RoomChainState.cs'),'two guard caption matches current authoritative plan count'
assert '"星烬祝福",BlessingSubtitle(true),false,true' in mobile,'mobile preview stays in existing header slot'
assert 'TouchFont(12)' in read('UI/GameUI.MobilePanels.cs'),'existing small header typography retained'
print('PASS: room preview two-entry hooks and both old-subtitle mutation controls (not rendering)')
