using UnityEngine;
namespace Emberfall
{
    // One optional floor hazard per heroic room. It never registers navigation obstacles.
    public sealed class ChapterHazards:MonoBehaviour
    {
        private GameSession game;
        private PlayerController owner;
        private ChapterNode node;
        private int room,seed,epoch,hitCycle=-1;
        private float age;
        private Vector3 center,start,end;
        private LineRenderer boundary,clock,glyph;
        private Material material;
        private bool retired;
        private readonly Vector3[] outline=new Vector3[48];
        public static ChapterHazards Configure(GameSession owner,ChapterNode node,ChapterDifficulty difficulty,int roomIndex,int seed,ChapterRoomPlan plan)
        {
            if(owner==null||owner.Player==null||plan==null||difficulty!=ChapterDifficulty.Heroic||node==ChapterNode.StarPlatform)return null;
            Vector3 start=plan.HazardStart,end=plan.HazardEnd,center=plan.HazardCenter;
            if(node==ChapterNode.ForestCourt&&!WorldTraversal.IsWalkable(center,.2f))return null;
            if(node==ChapterNode.Redrock)
            {
                if(!WorldTraversal.IsWalkable(start,.2f))return null;
                // Rock cover shortens the real line and its drawing together; it cannot hit through the ridge.
                end=ChapterHazardGeometry.ClipLine(start,end);
                if(CombatFx.Flat(end-start).sqrMagnitude<.25f)return null;
                center=(start+end)*.5f;
            }
            var go=new GameObject(node==ChapterNode.ForestCourt?"Heroic thorn pulse":"Heroic single heat line");
            var value=go.AddComponent<ChapterHazards>();value.game=owner;value.owner=owner.Player;
            value.node=node;value.room=roomIndex;value.seed=seed;value.epoch=owner.Player.CombatEpoch;
            value.start=start;value.end=end;value.center=center;value.CreateVisuals();
            owner.LogSystem(node==ChapterNode.ForestCourt?"英雄林庭 · 荆棘脉冲：橙红圈与计时弧预告，可从外侧绕行":"英雄赤岩 · 单线热涌：橙红边界与计时弧预告，可绕线端，岩墙阻断热涌");
            return value;
        }
        private bool ValidOwner
        {get{return game!=null&&game.HasStarted&&game.ChapterActive&&!game.ChapterFinished&&!game.IsDead&&game.Player==owner&&owner!=null&&!owner.IsDead&&owner.CombatEpoch==epoch&&game.ChapterRoomIndex==room&&game.ChapterSeed==seed&&game.ActiveChapterNode==node;}}
        private void Update()
        {
            if(retired)return;
            // Retirement must run with zero scaled time and while terminal UI blocks input.
            if(!ValidOwner){Retire();return;}
            if(game.InputBlocked)return;
            Advance(Time.deltaTime);
        }
        private void Advance(float delta)
        {
            if(float.IsNaN(delta)||float.IsInfinity(delta)||delta<=0)return;
            age+=Mathf.Min(delta,.1f);
            float phase=ArenaPulseRules.Phase(age);int cycle=ArenaPulseRules.Cycle(age);
            bool warning=ArenaPulseRules.Warning(phase),active=ArenaPulseRules.Active(phase);
            Draw(warning,active,ArenaPulseRules.Progress(phase));
            if(!active||hitCycle==cycle)return;
            Vector3 position=owner.transform.position;
            bool contains=node==ChapterNode.ForestCourt?
                ArenaPulseRules.Contains(CombatFx.Flat(position-center).sqrMagnitude,CombatSight.Area(center,position)):
                CombatFx.SegmentDistance(position,start,end)<=ChapterHazardGeometry.HeatHalfWidth&&CombatSight.Area(ClosestLinePoint(position),position);
            if(!contains)return;
            hitCycle=cycle;
            owner.TakeDamageFrom(Mathf.Max(3,owner.MaxHealth*.065f),node==ChapterNode.ForestCourt?"英雄林庭 · 荆棘脉冲":"英雄赤岩 · 单线热涌");
        }
        private Vector3 ClosestLinePoint(Vector3 point)
        {
            Vector3 segment=CombatFx.Flat(end-start);
            float fraction=segment.sqrMagnitude<=.0001f?0:Mathf.Clamp01(Vector3.Dot(CombatFx.Flat(point-start),segment)/segment.sqrMagnitude);
            return start+segment*fraction;
        }
        private void CreateVisuals()
        {
            material=ThreatVisualStyle.Material();boundary=Line("Hazard damage footprint",true,.1f);
            clock=Line("Hazard independent timing arc",false,.09f);glyph=Line(node==ChapterNode.ForestCourt?"Thorn source fork":"Heat source zigzag",false,.1f);
            Vector3[] shape=node==ChapterNode.ForestCourt?new[]{new Vector3(-.35f,0,.2f),Vector3.zero,new Vector3(-.3f,0,-.25f),Vector3.zero,new Vector3(.3f,0,-.2f),Vector3.zero,new Vector3(.35f,0,.25f)}:
                new[]{new Vector3(-.4f,0,-.2f),new Vector3(-.15f,0,.25f),new Vector3(.1f,0,-.2f),new Vector3(.35f,0,.25f)};
            glyph.positionCount=shape.Length;for(int i=0;i<shape.Length;i++)glyph.SetPosition(i,center+shape[i]+Vector3.up*.2f);
            Draw(false,false,0);
        }
        private LineRenderer Line(string name,bool loop,float width)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();
            line.useWorldSpace=true;line.loop=loop;line.widthMultiplier=width;line.sharedMaterial=material;
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.sortingOrder=120;
            line.startColor=line.endColor=ThreatVisualStyle.Danger;return line;
        }
        private void Draw(bool warning,bool active,float progress)
        {
            boundary.enabled=clock.enabled=glyph.enabled=warning;if(!warning)return;
            if(node==ChapterNode.ForestCourt)
            {CombatSight.FillAreaBoundary(outline,center+Vector3.up*.16f,ArenaPulseRules.Radius);boundary.positionCount=outline.Length;boundary.SetPositions(outline);}
            else
            {
                // Capsule outline uses the same endpoints/radius as SegmentDistance damage.
                Vector3 direction=(end-start).normalized,right=Vector3.Cross(Vector3.up,direction);
                for(int i=0;i<24;i++){float a=i*Mathf.PI/23;outline[i]=end+(right*Mathf.Cos(a)+direction*Mathf.Sin(a))*ChapterHazardGeometry.HeatHalfWidth+Vector3.up*.16f;outline[i+24]=start+(-right*Mathf.Cos(a)-direction*Mathf.Sin(a))*ChapterHazardGeometry.HeatHalfWidth+Vector3.up*.16f;}
                for(int i=0;i<outline.Length;i++)outline[i]=CombatSight.BoundaryPoint(CombatSightKind.Area,ClosestLinePoint(outline[i]),outline[i])+Vector3.up*.16f;
                boundary.positionCount=outline.Length;boundary.SetPositions(outline);
            }
            boundary.widthMultiplier=active?.16f:.1f;clock.positionCount=33;
            for(int i=0;i<33;i++){float a=progress*i*Mathf.PI*2/32;clock.SetPosition(i,center+new Vector3(Mathf.Sin(a)*.72f,.2f,Mathf.Cos(a)*.72f));}
        }
        private void Retire()
        {if(retired)return;retired=true;gameObject.SetActive(false);Destroy(gameObject);}
        private void OnDisable(){retired=true;}
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
