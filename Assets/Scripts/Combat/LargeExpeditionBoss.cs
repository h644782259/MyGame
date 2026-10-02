using UnityEngine;

namespace Emberfall
{
    // Opt-in only for the fifth large-expedition room. EnemyController drives Tick,
    // so mechanic ownership cannot race its ordinary charge/slam/volley update.
    public sealed class LargeExpeditionBoss : MonoBehaviour
    {
        public LargeBossPhaseState State { get; private set; }
        private EnemyController boss;
        private GameSession game;
        private readonly DestructibleProp[] anchors = new DestructibleProp[3];
        private GameObject anchorRoot, beamRoot;
        private WorldResources resources;
        private readonly LineRenderer[] beamLines = new LineRenderer[6];
        private Material beamMaterial;
        private Vector3 phaseCenter;
        private float startAngle;
        private bool chapterConfigured;
        private ChapterDifficulty chapterDifficulty;
        private int ownerEpoch,ownerRoom,ownerSeed;
        private PlayerController owner;
        private float sweepSign=1;
        public float BeamWorldAngle {get{return startAngle+(State==null?0:State.BeamAngle*sweepSign);}}
        private int seed;
        private bool stopped;

        public static LargeExpeditionBoss Configure(EnemyController boss, int tier, int seed)
        {
            if (boss == null || !boss.IsBoss || boss.IsDead) return null;
            var existing = boss.GetComponent<LargeExpeditionBoss>();
            if (existing != null) return existing;
            var value = boss.gameObject.AddComponent<LargeExpeditionBoss>();
            value.boss = boss; value.game = GameSession.Instance; value.seed = seed;
            value.State = new LargeBossPhaseState();
            boss.ConfigureLargeExpedition(value);
            return value;
        }
        public static LargeExpeditionBoss ConfigureChapter(EnemyController boss,int tier,int seed,ChapterDifficulty difficulty)
        {
            var value=Configure(boss,tier,seed);
            if(value==null||value.chapterConfigured)return value;
            // Explicit chapter opt-in only, before the first threshold; legacy Configure stays unchanged.
            if(value.State.PhaseNumber!=0)return value;
            value.chapterConfigured=true;value.chapterDifficulty=difficulty;
            value.State=new LargeBossPhaseState(difficulty!=ChapterDifficulty.Normal);
            value.owner=value.game==null?null:value.game.Player;
            value.ownerEpoch=value.owner==null?-1:value.owner.CombatEpoch;
            value.ownerRoom=value.game==null?-1:value.game.ChapterRoomIndex;value.ownerSeed=value.game==null?0:value.game.ChapterSeed;
            return value;
        }
        internal bool Tick(float delta)
        {
            if (stopped || State == null) return false;
            bool alive = boss != null && !boss.IsDead && boss.enabled && gameObject.activeInHierarchy &&
                game != null && game.HasStarted && game.Player != null && !game.IsDead && ChapterOwnerValid;
            if (!alive) { StopEncounter(); return false; }
            bool active = !game.InputBlocked;
            if (!active) { State.Advance(delta, false, true); return State.OwnsAttacks; }
            LargeBossPhase before = State.Phase;
            if (State.TryBegin(boss.Health / Mathf.Max(1, boss.MaxHealth), !boss.IsStunned))
            {
                BeginPhasePresentation();
            }
            for (int i=0;i<anchors.Length;i++)
                if ((State.LiveAnchorMask & (1<<i)) != 0 && (anchors[i]==null || anchors[i].Broken || !anchors[i].gameObject.activeInHierarchy))
                    State.DestroyAnchor(State.PhaseNumber,i);
            State.Advance(delta,true,!boss.IsDead);
            if(before==LargeBossPhase.Recovery&&State.Phase==LargeBossPhase.Windup)BeginPhasePresentation();
            if (State.Phase == LargeBossPhase.Exposed && before != LargeBossPhase.Exposed)
                AnnounceExposure();
            bool beamVisible = State.Phase == LargeBossPhase.Windup || State.Phase == LargeBossPhase.Beam;
            if (beamRoot != null) beamRoot.SetActive(beamVisible);
            if (!beamVisible) ReleaseAnchors();
            if (beamVisible)
            {
                Vector3 direction=Quaternion.Euler(0,BeamWorldAngle,0)*Vector3.forward;
                Vector3 from=phaseCenter+direction*1.5f,to=ClipBeam(from,phaseCenter+direction*LargeBossPhaseState.BeamLength);
                DrawBeam(from,to,direction);
                if (State.DamagePulse && !game.InputBlocked && !boss.IsDead && !game.Player.IsDead &&
                    CombatFx.SegmentDistance(game.Player.transform.position,from,to)<=LargeBossPhaseState.BeamHalfWidth+.4f &&
                    WorldTraversal.HasGroundPath(from,game.Player.transform.position,.12f))
                    game.Player.TakeDamageFrom(boss.AttackDamage*.55f,"星环执政官 · 环流扫射");
            }
            return State.OwnsAttacks;
        }
        private bool ChapterOwnerValid
        {get{return !chapterConfigured||(game!=null&&game.ChapterActive&&!game.ChapterFinished&&game.Player==owner&&owner!=null&&!owner.IsDead&&owner.CombatEpoch==ownerEpoch&&game.ChapterRoomIndex==ownerRoom&&game.ChapterSeed==ownerSeed);}}
        private void BeginPhasePresentation()
        {
            boss.BeginLargeBossMechanic();phaseCenter=CombatFx.Flat(transform.position);
            Vector3 toward=CombatFx.Flat(game.Player.transform.position-phaseCenter);
            bool heroic=chapterConfigured&&chapterDifficulty==ChapterDifficulty.Heroic;
            sweepSign=ChapterBossPattern.SweepSign(heroic,seed,State.PhaseNumber);
            startAngle=Mathf.Atan2(toward.x,toward.z)*Mathf.Rad2Deg+ChapterBossPattern.StartOffset(heroic,seed,State.PhaseNumber);
            CreateAnchors();EnsureBeam();
            game.SpawnMechanismText(transform.position+Vector3.up*3.6f,State.IsFollowup?"追加扫射预警 · 断锚可中止":"断能蓄力 · 摧毁供能锚",new Color(.35f,.92f,1));
            game.LogSystem("星环执政官 · 橙红边界表示危险，青色符号可打断，摧毁供能锚可中止扫射并暴露核心"+
                (chapterConfigured&&chapterDifficulty!=ChapterDifficulty.Normal?" · 完整扫射后追加预警；断锚可止连扫":"")+
                (heroic?(sweepSign>0?" · 本轮顺时针扫射":" · 本轮逆时针扫射"):""));
        }
        internal void InterruptWindup()
        {
            if (stopped || State == null || !State.InterruptWindup()) return;
            if (beamRoot != null) beamRoot.SetActive(false);
            ReleaseAnchors(); AnnounceExposure();
        }
        private void AnnounceExposure()
        {
            if(game==null||boss==null||boss.IsDead)return;
            game.SpawnMechanismText(transform.position+Vector3.up*3.2f,"核心暴露 · 伤害 +35%",new Color(1,.82f,.35f));
            CombatFx.Ring(transform.position,1.6f,new Color(.2f,.9f,1),.5f,.1f);
        }
        private void CreateAnchors()
        {
            ReleaseAnchors();
            anchorRoot=new GameObject("Large boss power anchors");anchorRoot.transform.SetParent(transform,false);
            resources=anchorRoot.AddComponent<WorldResources>();
            Material shell=resources.Material(new Color(.25f,.34f,.43f),false,VisualSurface.Metal);
            Material trim=resources.Material(new Color(.7f,.49f,.24f),false,VisualSurface.Metal);
            Material glow=resources.Material(new Color(.2f,.87f,1),true,VisualSurface.Crystal);
            int mask=0;int level=game.Progression.Profile.level;
            for(int i=0;i<3;i++)
            {
                // Near-core anchors are reachable by existing one-tap boss-targeted AoEs.
                Vector3 point=Vector3.zero;bool safe=false;
                for(int attempt=0;attempt<6;attempt++)
                {
                    float angle=startAngle+i*120f+attempt*13f+((seed&15)-7)+
                        ChapterBossPattern.AnchorOffset(chapterConfigured&&chapterDifficulty==ChapterDifficulty.Heroic,seed,State.PhaseNumber);
                    point=phaseCenter+Quaternion.Euler(0,angle,0)*Vector3.forward*2.6f;
                    if(DestructibleProp.CanPlace(point,.55f)&&WorldTraversal.HasGroundPath(phaseCenter,point,.15f)&&
                        CombatFx.Flat(point-game.Player.transform.position).magnitude>1.2f){safe=true;break;}
                }
                if(!safe)continue;
                Transform root=LargeBossRig.Joint(anchorRoot.transform,"Destructible power anchor",Vector3.zero);root.position=point;
                Transform model=LargeBossRig.Joint(root,"Anchor geometry",Vector3.zero);
                LargeBossRig.Part(model,"Power anchor plinth",PrimitiveType.Cylinder,new Vector3(0,.13f,0),new Vector3(.84f,.13f,.84f),shell);
                LargeBossRig.Part(model,"Breakable crystal",PrimitiveType.Cube,new Vector3(0,.82f,0),new Vector3(.46f,.88f,.46f),glow).localRotation=Quaternion.Euler(0,45,0);
                for(int arm=0;arm<3;arm++)
                {
                    Transform brace=LargeBossRig.Joint(model,"Anchor bronze cage",Vector3.zero);brace.localRotation=Quaternion.Euler(0,arm*120,0);
                    LargeBossRig.Part(brace,"Anchor claw",PrimitiveType.Capsule,new Vector3(0,.45f,.3f),new Vector3(.1f,.34f,.12f),trim).localRotation=Quaternion.Euler(-12,0,0);
                }
                var prop=root.gameObject.AddComponent<DestructibleProp>();
                prop.Initialize(DestructibleKind.Crate,level,.55f,false,PropRecovery.None,model,shell);
                anchors[i]=prop;mask|=1<<i;
            }
            State.CommitAnchors(mask);
        }
        private void ReleaseAnchors()
        {
            if(anchorRoot!=null){anchorRoot.SetActive(false);Destroy(anchorRoot);anchorRoot=null;}
            for(int i=0;i<anchors.Length;i++)anchors[i]=null;
        }
        private void EnsureBeam()
        {
            if(beamRoot!=null)return;
            beamRoot=new GameObject("Telegraphed rotating floor beam");beamRoot.transform.SetParent(transform,false);
            Shader shader=Resources.Load<Shader>("ThreatBoundary");beamMaterial=shader==null?CombatFx.NewGlow():new Material(shader);beamMaterial.renderQueue=3900;
            for(int i=0;i<beamLines.Length;i++)
            {
                var obj=new GameObject(i==3?"Sweep direction arrow":i==4?"Interrupt symbol":i==5?"Windup timing arc":"Beam footprint");obj.transform.SetParent(beamRoot.transform,false);
                var line=obj.AddComponent<LineRenderer>();line.sharedMaterial=beamMaterial;line.useWorldSpace=true;
                line.positionCount=i==5?33:i>=3?3:2;line.numCapVertices=4;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                line.sortingOrder=125;beamLines[i]=line;
            }
        }
        private void DrawBeam(Vector3 from,Vector3 to,Vector3 direction)
        {
            Vector3 right=Vector3.Cross(Vector3.up,direction);from.y=to.y=.18f;
            bool live=State.Phase==LargeBossPhase.Beam;
            Color tint=new Color(1,.24f,.12f,.85f);
            for(int i=0;i<3;i++)
            {
                float side=i==0?0:i==1?-LargeBossPhaseState.BeamHalfWidth:LargeBossPhaseState.BeamHalfWidth;
                beamLines[i].SetPosition(0,from+right*side);
                float fill=live||i!=0?1:1-State.Remaining/LargeBossPhaseState.WindupSeconds;
                beamLines[i].SetPosition(1,Vector3.Lerp(from,to,fill)+right*side);
                beamLines[i].widthMultiplier=i==0?(live?(EffectPreferences.ReducedEffects?.22f:.7f):.06f):.09f;
                beamLines[i].startColor=beamLines[i].endColor=tint;
            }
            // Arrow and damage beam share the configured signed sweep direction.
            Vector3 head=Vector3.Lerp(from,to,.78f);beamLines[3].SetPosition(0,head-direction*.3f);
            beamLines[3].SetPosition(1,head+right*(.6f*sweepSign));beamLines[3].SetPosition(2,head+direction*.3f);
            beamLines[3].widthMultiplier=.11f;beamLines[3].startColor=beamLines[3].endColor=tint;
            Vector3 clock=phaseCenter+Vector3.up*.22f;
            var symbol=beamLines[4];symbol.enabled=!live&&boss.CanBeSkillInterrupted;
            symbol.SetPosition(0,clock+new Vector3(-.2f,0,.19f));symbol.SetPosition(1,clock+new Vector3(.03f,0,-.19f));symbol.SetPosition(2,clock+new Vector3(.2f,0,.19f));
            symbol.widthMultiplier=.09f;symbol.startColor=symbol.endColor=new Color(.2f,1,.9f,1);
            var timer=beamLines[5];timer.enabled=!live;timer.widthMultiplier=.09f;timer.startColor=timer.endColor=tint;
            float progress=Mathf.Clamp01(1-State.Remaining/LargeBossPhaseState.WindupSeconds);
            for(int i=0;i<33;i++){float a=progress*i*Mathf.PI*2/32;timer.SetPosition(i,clock+new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*.55f);}

        }
        private static Vector3 ClipBeam(Vector3 from,Vector3 to)
        {
            if(WorldTraversal.HasGroundPath(from,to,.12f))return to;
            float low=0,high=1;for(int i=0;i<10;i++){float mid=(low+high)*.5f;if(WorldTraversal.HasGroundPath(from,Vector3.Lerp(from,to,mid),.12f))low=mid;else high=mid;}
            return Vector3.Lerp(from,to,low);
        }
        internal void StopEncounter()
        {
            if(stopped)return;stopped=true;if(State!=null)State.Dispose();ReleaseAnchors();
            if(beamRoot!=null){beamRoot.SetActive(false);Destroy(beamRoot);beamRoot=null;}
        }
        private void OnDisable(){StopEncounter();}
        // Runs even with timeScale zero: terminal menus must not retain live hazards.
        private void LateUpdate()
        {if(!stopped&&(game==null||!game.HasStarted||game.IsDead||game.ModeFinished||!ChapterOwnerValid||boss==null||boss.IsDead))StopEncounter();}
        private void OnDestroy(){StopEncounter();if(beamMaterial!=null)Destroy(beamMaterial);}
    }
}
