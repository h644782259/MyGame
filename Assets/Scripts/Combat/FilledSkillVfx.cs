using UnityEngine;

namespace Emberfall
{
    // All primary silhouettes are filled world-space meshes. No damage, physics,
    // screen flashes, character sprites, or camera-facing replacement billboards.
    internal sealed class FilledSkillVfx : MonoBehaviour
    {
        private sealed class Piece
        {
            public Transform Transform; public Renderer Renderer;
            public Vector3 Position, Scale; public Quaternion Rotation;
            public float Delay, Phase, TravelScale=1; public int Motion;
        }
        private static Mesh crescent, crystal, flame, sword, lightning, arcane, rupture, arcaneShard;
        private static Material sharedMaterial;
        private static int active;
        private readonly Piece[] pieces=new Piece[FilledVfxRecipes.MaximumParts];
        private readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
        private int count,epoch;
        private PlayerController owner;
        private FilledVfxKind kind;
        private Color tint;
        private float age,life,size;
        private bool registered;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAssets()
        {
            if(crescent!=null)Destroy(crescent);if(crystal!=null)Destroy(crystal);if(flame!=null)Destroy(flame);
            if(sword!=null)Destroy(sword);if(lightning!=null)Destroy(lightning);if(arcane!=null)Destroy(arcane);if(rupture!=null)Destroy(rupture);if(arcaneShard!=null)Destroy(arcaneShard);
            if(sharedMaterial!=null)Destroy(sharedMaterial);crescent=crystal=flame=sword=lightning=arcane=rupture=arcaneShard=null;sharedMaterial=null;active=0;
        }
        private static Mesh Mesh(FilledMeshRecipe data,string name)
        {
            var vertices=new Vector3[data.Positions.Length/3];var uv=new Vector2[vertices.Length];
            for(int i=0;i<vertices.Length;i++){vertices[i]=new Vector3(data.Positions[i*3],data.Positions[i*3+1],data.Positions[i*3+2]);uv[i]=new Vector2(data.Uv[i*2],data.Uv[i*2+1]);}
            var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=data.Triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static void EnsureAssets()
        {
            if(crescent==null)crescent=Mesh(FilledVfxRecipes.Crescent(),"Filled curved crescent volume");
            if(crystal==null)crystal=Mesh(FilledVfxRecipes.Crystal(),"Faceted ice spear");
            if(flame==null)flame=Mesh(FilledVfxRecipes.Flame(),"Curved flame tongue volume");
            if(sword==null)sword=Mesh(FilledVfxRecipes.Sword(),"Ridged sword blade guard and grip");
            if(lightning==null)lightning=Mesh(FilledVfxRecipes.Lightning(),"Angular branched lightning volume");
            if(arcane==null)arcane=Mesh(FilledVfxRecipes.Arcane(),"Arcane cubical lattice");
            if(rupture==null)rupture=Mesh(FilledVfxRecipes.Rupture(),"Ground rupture branches");
            if(arcaneShard==null)arcaneShard=Mesh(FilledVfxRecipes.ArcaneShard(),"Broken arcane strut");
            if(sharedMaterial==null)
            {Shader shader=Resources.Load<Shader>("FilledSpell");sharedMaterial=new Material(shader!=null?shader:Shader.Find("Sprites/Default"));sharedMaterial.renderQueue=3070;}
        }
        private static FilledSkillVfx Create(PlayerController hero,Vector3 at,Vector3 forward,FilledVfxKind type,float radius,Color color,float duration,Transform parent=null)
        {
            int maximum=Application.isMobilePlatform?FilledVfxRecipes.MobileEffects:FilledVfxRecipes.DesktopEffects;
            if(hero==null||hero.IsDead||active>=maximum||!Finite(radius)||radius<=0||!Finite(forward.x)||!Finite(forward.y)||!Finite(forward.z)||!Finite(duration)||duration<=0||!Finite(at.x)||!Finite(at.y)||!Finite(at.z))return null;
            EnsureAssets();var root=new GameObject("Filled "+type+" effect");if(parent!=null)root.transform.SetParent(parent,true);root.transform.position=at;
            root.transform.rotation=Quaternion.LookRotation(forward.sqrMagnitude>.0001f?forward.normalized:Vector3.forward);
            var fx=root.AddComponent<FilledSkillVfx>();fx.owner=hero;fx.epoch=hero.CombatEpoch;fx.kind=type;fx.size=Mathf.Clamp(radius,.15f,8);
            fx.tint=color;fx.life=Mathf.Clamp(duration,.12f,12);fx.Register();return fx;
        }
        public static void Crescent(PlayerController hero,Vector3 at,Vector3 forward,float radius,Color color)
        {
            var fx=Create(hero,at+Vector3.up*.82f,forward,FilledVfxKind.Crescent,radius,color,.34f);if(fx==null)return;
            fx.Add(crescent,Vector3.zero,new Vector3(fx.size,fx.size*.7f,fx.size),Quaternion.Euler(-12,0,0),0,0,0);
            fx.Add(crescent,new Vector3(0,.08f,-.1f),Vector3.one*fx.size*.88f,Quaternion.Euler(8,-16,0),.02f,0,1);
            for(int i=0;i<4;i++)fx.Add(crystal,new Vector3((i-1.5f)*.25f,.1f,.8f)*fx.size,new Vector3(.09f,.4f,.12f)*fx.size,Quaternion.Euler(85,i*33,0),.02f+i*.018f,4,i);
        }
        public static void Impact(PlayerController hero,Vector3 at,float radius,FilledVfxKind type,Color color)
        {
            if(type!=FilledVfxKind.Ice&&type!=FilledVfxKind.Fire&&type!=FilledVfxKind.Summon&&type!=FilledVfxKind.Sword&&type!=FilledVfxKind.Lightning&&type!=FilledVfxKind.Arcane)return;
            var fx=Create(hero,at,Vector3.forward,type,radius,color,type==FilledVfxKind.Fire?.8f:1.05f);if(fx==null)return;
            float unit=Mathf.Min(1.6f,fx.size*.55f);
            // Allocate landing base, identity silhouette and contact flash BEFORE repeated ornaments.
            // Add's actual budget is still the last authority: mobile 10, reduced 7.
            fx.Add(rupture,Vector3.up*.07f,new Vector3(fx.size*.58f,.8f,fx.size*.58f),Quaternion.identity,0,5,0,fx.size*.7f,"Landing base");
            Mesh main=type==FilledVfxKind.Sword?sword:type==FilledVfxKind.Lightning?lightning:type==FilledVfxKind.Arcane?arcane:type==FilledVfxKind.Ice?crystal:type==FilledVfxKind.Fire?flame:crescent;
            int motion=type==FilledVfxKind.Sword?8:type==FilledVfxKind.Lightning?9:type==FilledVfxKind.Arcane?10:type==FilledVfxKind.Ice?1:type==FilledVfxKind.Fire?2:3;
            Vector3 dimensions=type==FilledVfxKind.Sword?new Vector3(.95f,3.2f,.95f)*unit:type==FilledVfxKind.Lightning?new Vector3(1.3f,2.2f,1.3f)*unit:Vector3.one*unit*1.65f;
            fx.Add(main,Vector3.zero,dimensions,Quaternion.identity,0,motion,0,
                type==FilledVfxKind.Sword?unit*.55f:type==FilledVfxKind.Lightning?unit*.85f:type==FilledVfxKind.Summon?unit*1.8f+fx.size*.3f:unit*2f,"Primary "+type);
            fx.Add(rupture,Vector3.up*.11f,new Vector3(fx.size*.3f,.65f,fx.size*.3f),Quaternion.Euler(0,65,0),0,5,1,fx.size*.38f,"Contact flash");
            int n=EffectPreferences.ReducedEffects?5:type==FilledVfxKind.Summon?6:10;
            for(int i=0;i<n;i++)
            {
                float angle=i*2.399963f,r=(.18f+(i%3)*.24f)*fx.size;Vector3 radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                if(type==FilledVfxKind.Ice)
                    fx.Add(crystal,radial*r,new Vector3(.32f,1.1f+(i%3)*.45f,.37f)*unit,Quaternion.Euler(radial.z*23,angle*Mathf.Rad2Deg,-radial.x*23),i*.016f,1,angle,unit*1.2f);
                else if(type==FilledVfxKind.Fire)
                    fx.Add(flame,radial*r*.55f,new Vector3(.95f,1.6f+(i%3)*.3f,.95f)*unit,Quaternion.Euler(radial.z*17,i*51,-radial.x*17),i*.012f,2,angle,unit*1.8f);
                else if(type==FilledVfxKind.Summon)
                    fx.Add(crescent,radial*fx.size*.25f+Vector3.up*.65f,Vector3.one*fx.size*.4f,Quaternion.Euler(90,angle*Mathf.Rad2Deg,0),i*.024f,3,angle,fx.size*.9f);
                else
                    fx.Add(type==FilledVfxKind.Sword?rupture:type==FilledVfxKind.Arcane?arcaneShard:main,radial*r+Vector3.up*.16f,Vector3.one*unit*.35f,Quaternion.Euler(0,i*51,0),(type==FilledVfxKind.Arcane?.34f:.08f)+i*.018f,type==FilledVfxKind.Sword?5:type==FilledVfxKind.Arcane?3:motion,angle,unit*.6f+(type==FilledVfxKind.Arcane?fx.size*.3f:0));
            }
        }
        public static void Charge(Transform parent,PlayerController hero,Vector3 at,float radius,Color color,float duration)
        {
            var fx=Create(hero,at,Vector3.forward,FilledVfxKind.Charge,Mathf.Min(radius,3.5f),color,duration,parent);if(fx==null)return;
            for(int i=0;i<3;i++)fx.Add(crescent,Vector3.up*(.18f+i*.2f),new Vector3(fx.size*.65f,.8f,fx.size*.65f),Quaternion.Euler(i*12,i*120,0),0,6,i);
        }
        public static void Thrust(PlayerController hero,Vector3 start,Vector3 end,Color color,float lifetime,float width)
        {
            if(!Finite(end.x)||!Finite(end.y)||!Finite(end.z)||!Finite(width)||width<=0)return;
            Vector3 delta=end-start;if(delta.sqrMagnitude<.001f)return;
            var fx=Create(hero,start,Vector3.forward,FilledVfxKind.Thrust,1,color,Mathf.Min(lifetime,.7f));if(fx==null)return;
            fx.Add(flame,Vector3.zero,new Vector3(Mathf.Clamp(width*3,.22f,1.5f),Mathf.Min(24,delta.magnitude),Mathf.Clamp(width*3,.22f,1.5f)),Quaternion.FromToRotation(Vector3.up,delta),0,7,0);
            fx.Add(crystal,delta*.84f,new Vector3(width*1.4f,Mathf.Min(3,delta.magnitude*.3f),width*1.4f),Quaternion.FromToRotation(Vector3.up,delta),0,7,1);
        }
        private void Add(Mesh mesh,Vector3 at,Vector3 dimensions,Quaternion rotation,float delay,int motion,float phase,float footprint=0,string label="Repeated ornament")
        {
            int cap=EffectPreferences.ReducedEffects?FilledVfxRecipes.ReducedParts:Application.isMobilePlatform?10:FilledVfxRecipes.MaximumParts;
            if(count>=cap)return;
            float fit=1;
            if(footprint>0)
            {
                float px,pz;
                if(!FilledVfxPlacement.TryPlace(at.x,at.z,size,footprint,(x,z,r)=>{
                    Vector3 candidate=transform.position+new Vector3(x,0,z);
                    // The center ray must be damage-visible; only the destination needs a full disk.
                    return CombatSight.Area(transform.position,candidate)&&CombatSight.VisualFootprint(candidate,candidate,r);
                },out px,out pz,out fit))return;
                at.x=px;at.z=pz;dimensions*=fit;
            }
            var obj=new GameObject("Filled spell surface / "+label);obj.transform.SetParent(transform,false);
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=sharedMaterial;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.sortingOrder=10;
            var piece=new Piece{Transform=obj.transform,Renderer=renderer,Position=at,Scale=dimensions,Rotation=rotation,Delay=delay,Motion=motion,Phase=phase,TravelScale=footprint>0?fit:1};pieces[count++]=piece;
            Animate(piece); // The mesh exists at the actual hit frame, before the next Update.
        }
        private void Update()
        {
            var game=GameSession.Instance;
            if(owner==null||owner.IsDead||owner.CombatEpoch!=epoch||game==null||game.Player!=owner||!game.HasStarted||game.ModeFinished){Destroy(gameObject);return;}
            if(game.InputBlocked||Time.deltaTime<=0)return;
            age+=Time.deltaTime;if(age>=life){Destroy(gameObject);return;}
            for(int i=0;i<count;i++)Animate(pieces[i]);
        }
        private void Animate(Piece p)
        {
            float local=age-p.Delay;if(local<0){p.Transform.gameObject.SetActive(false);return;}
            p.Transform.gameObject.SetActive(true);var f=FilledVfxRecipes.Sample(kind,local,Mathf.Max(.1f,life-p.Delay));
            Vector3 at=p.Position,scale=p.Scale;Quaternion rotation=p.Rotation;
            float t=f.Progress;
            switch(p.Motion)
            {
                case 0: scale*=.85f+t*.3f;rotation*=Quaternion.Euler(0,Mathf.Lerp(-18,38,t),-t*11);break;
                case 1: scale.y*=.25f+.75f*Mathf.Min(1,local*18);at.y-=Mathf.Max(0,t-.55f)*1.3f;break;
                case 2: scale*=f.Expansion;scale.y*=1+t*.55f;at.y+=t*.65f;rotation*=Quaternion.Euler(0,t*45,0);break;
                case 3: at+=new Vector3(Mathf.Cos(p.Phase),0,Mathf.Sin(p.Phase))*t*size*.3f*p.TravelScale;at.y+=Mathf.Sin(t*Mathf.PI)*1.1f;scale*=1-t*.4f;rotation*=Quaternion.Euler(t*65,t*35,0);break;
                case 4: at+=new Vector3(Mathf.Sin(p.Phase),.4f,Mathf.Cos(p.Phase))*local*3;scale*=1-t*.8f;break;
                case 5: scale*=f.Expansion;rotation*=Quaternion.Euler(0,t*45,0);break;
                case 6: scale*=.8f+Mathf.Sin(t*Mathf.PI)*.2f;rotation*=Quaternion.Euler(0,local*(p.Phase%2==0?95:-80),0);at.y+=Mathf.Sin(local*3+p.Phase)*.06f;break;
                case 7: scale.x*=1-t*.75f;scale.z*=1-t*.75f;break;
                // The tip is already on the real landing frame; settle does not fake a delayed hit.
                case 8: at.y-=Mathf.Min(.12f,t*.5f)*p.TravelScale;scale.y*=1-Mathf.Max(0,t-.55f)*.3f;break;
                case 9: scale.x*=t<.12f?1:t<.28f?.78f:1-t*.5f;scale.z*=1-t*.4f;break;
                case 10: scale*=t<.18f?Mathf.Lerp(1,.58f,t/.18f):t<.42f?Mathf.Lerp(.58f,1.08f,(t-.18f)/.24f):1.08f-(t-.42f)*.55f;rotation*=Quaternion.Euler(t*30,t*55,0);break;
            }
            p.Transform.localPosition=at;p.Transform.localScale=scale;p.Transform.localRotation=rotation;
            // Placement reserved the complete motion footprint once. Do not toggle a whole
            // primary silhouette each frame when a growing bounds circle grazes a wall.
            Color color=tint;color.a*=f.Opacity*(kind==FilledVfxKind.Charge?.35f:.9f)*Mathf.Lerp(.55f,1,EffectPreferences.EffectsScale);
            block.SetColor("_Color",color);block.SetFloat("_Opacity",f.Opacity);block.SetFloat("_Progress",t);
            block.SetFloat("_Style",kind==FilledVfxKind.Fire||kind==FilledVfxKind.Summon?1:.35f);p.Renderer.SetPropertyBlock(block);
        }
        private void Register(){if(registered)return;registered=true;active++;}
        private void Release(){if(!registered)return;registered=false;active=Mathf.Max(0,active-1);}
        private void OnDisable(){Release();}
        private void OnEnable()
        {
            if(owner==null||registered)return;
            if(active>=(Application.isMobilePlatform?FilledVfxRecipes.MobileEffects:FilledVfxRecipes.DesktopEffects)){Destroy(gameObject);return;}
            Register();
        }
        private void OnDestroy(){Release();}
        private static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
    }
}
