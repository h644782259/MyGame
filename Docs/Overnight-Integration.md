# Overnight integration acceptance record

This integrates the visual/mobile/save/world pass, the subsequent per-skill/companion/progression review, and focused regression audits. Windows and iOS share gameplay rules and tests; iOS retains its full Chinese font, device-family/export configuration and6000.6.4f1 project target.

## 新内容从哪里进入

下面是按当前源码入口核对的操作指引；仍需在可运行的 Unity / 目标设备上验证实际画面和手感。

1. **新副本与试炼**：角色达到2级后，走到营地北方发光传送门附近（小地图菱形）。Windows 按 T，手机点左侧「进入副本」，打开「选择冒险」。可选「沉星遗迹」「守望林庭」「烬河突围」「蚀星斗场」「回廊远征」（列表向下滚动可见第五项）；再选阶数、普通治疗或限疗挑战，点「进入挑战」。若被拦下，先领取营地恢复栏装备、腾出战利品位置或开启并收起上次通关宝箱的奖励展示。
2. **五种冒险的区别**：沉星遗迹是三波普通副本；守望林庭需要清敌并在无敌人压制的据点内累计守点时间；烬河突围需要限时清完三阶段；蚀星斗场是三首领连战；回廊远征有五个房间、可选晶核支线、休整祝福和大型星环首领。远征北门解锁后靠近，Windows 按 T、手机点「进入下一间」。进入新一局成功后技能冷却重置，房间之间不重置。
3. **阶数与奖励**：可选最高阶数为普通沉星遗迹最高通关阶数+1，上限100；其他试炼和回廊远征不会推进这个解锁记录。入场页显示本次通关碎片数，随阶数分档增加，详见[成长与奖励](Progression-Growth.md)。
4. **城镇与 NPC**：点小地图，或打开「城镇旅行地图」（手机暂停菜单也有入口）。风语营地默认开放；赤岩驿站5级开放；星望城12级或首次通关普通沉星遗迹后开放。先退出挑战并远离敌人，再点已解锁目的地免费传送。回到营地区域，靠近商人、铁匠或兑换员，点出现的交互按钮，分别购买药剂/出售、部位强化、机制装备兑换；三处服务相同，不会刷新或重抽背包。
5. **重配与两套方案**：回营地区域打开「营地工坊」→「战技」，点「免费重置配点」或「配装方案 · 记录 / 应用两套」，手机在面板内滑动可找到它们。重置先显示返还点数并确认，保留已学1阶；方案记录技能、精通、职业路线、快捷栏和现有装备编号，不复制装备、不新建角色存档，也不重置冷却。应用方案时装备必须仍在当前背包，点数和等级需满足校验。
6. **机制装备升华**：通关普通「沉星遗迹」第5阶，穿戴本职业已发现配方的史诗机制装备，回「营地工坊」→「机制图鉴」，准备24枚星烬碎片后使用该装备的升华按钮。变为传说时保留物品编号、等级、变体和部位强化；这与「战技」里的精通核心是两个系统。
7. **手机操作**：十个技能位置固定，不再翻页；已学主动技能点一次自动瞄准释放，被动格不用点击。空白区域上下拖动调俯仰，在滚动面板内容上滑动滚动；靠近入口、NPC或房门后用左侧交互按钮。保存、读取和返回主菜单集中在暂停菜单，手动保存会询问覆盖；Windows 的退出按钮关闭游戏，手机返回主菜单。

入口核对：[副本选择与阶数](../Assets/Scripts/Core/GameSession.cs)、[五种冒险菜单](../Assets/Scripts/UI/GameUI.Modes.cs)、[城镇解锁](../Assets/Scripts/Core/HubTravelRules.cs)、[工坊](../Assets/Scripts/UI/GameUI.Expedition.cs)、[移动工坊](../Assets/Scripts/UI/GameUI.MobileWorkshop.cs)、[升华条件](../Assets/Scripts/Core/ProgressionService.cs)。

## Visible and playable changes in source

- Rounded/bevelled shared models, articulated movement, cloth motion, thick evolving3D skill volumes, bounded overlapping effects and original application icon
- Ten fixed skill positions; mobile one-tap targeting, contextual entrance/NPC/room interaction, blank-area camera pitch, direct panel swiping, responsive inventory/skills/workshop/rewards and safer compact overlays
- One manual-save entry with confirmation, pause loading, centered scroll-safe dialogs, current-slot automatic saving, explicit deletion and bounded application-generated save resources; Windows exit and mobile return-to-title flow
- Structured recap, actionable attention indicators, unavailable-equipment distinction and more run blessings, real interruption and bounded knockback
- Three NPC service hubs and map travel, three objective/timed arena modes, differentiated five-room expedition, optional room choices, destructible props and phased large astrolabe boss
- Per-skill damage/event budgets, shared fan-arrow target cap, timed legal Boss approach fallback, stable charged summon targets, full-Pack refresh and non-disruptive dodge protection
- Pause-safe scheduled damage with bounded catch-up, shared terrain/area/chain occlusion and entry-level snapshots
- Atomic joint respec and two character-owned build presets; distinct10/20-point mastery cores, tier rewards and identity-preserving mechanism-quality ascension
- Atomic/retry-safe reward and explicit action storage, linked-file protection, transition preflight, reduced retained companion/equipment lookup allocations and bounded build-size history
- Minimap uses actual live terrain/bridges/obstacles and refreshes on hub change or destroyed props instead of reusing forest overlays everywhere

## Evidence levels

Managed production-rule tests and source/API compilation are available in the cloud validation runner. The deterministic48-scenario combat ledger measures declared ideal event/energy budgets over5s/10s, not real target hit rate, class ranking or measured encounter time. Pure geometry checks establish valid/bounded meshes and UI rectangles, not visual quality.

The filled skill effects are evolving world-space3D mesh surfaces, with hit-synchronized animation. They are not a character sprite sheet or an imported flipbook texture. The supplied reference informed thick silhouette changes, directional thrust and eruption staging. The generated optional atlas was not shipped.

Still requires a runnable Unity editor and target devices: shader import, true JsonUtility/player serialization, rendered character/effect quality, actual finger interaction, GPU frame/memory timings, gamefeel and moving-target combat, Windows package, Xcode/IPA/signing/install. No screenshots or device passes are inferred from source checks. The cloud still rejects the local socket families Unity needs; no security workaround was used.

Source-tree byte counts are tracked separately from the actual build-size callback. New meshes/materials are reused and no historic builds/caches/player saves are bundled by these changes. The actual installer/IPA size remains unmeasured until a real build runs.
