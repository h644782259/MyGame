using System;
using UnityEngine;
namespace Emberfall
{
    // One bounded, isolated real model; no player controller, stats, inventory or save mutations.
    public sealed class CollectionModelPreview : IDisposable
    {
        const int PreviewLayer=31;
        GameObject stage,avatar;
        Camera camera;
        CombatModel model;
        RenderTexture texture;
        CollectionPreviewState state=new CollectionPreviewState();
        public void Rotate(float degrees){SetYaw(state.Yaw+degrees);}
        public void SetYaw(float degrees){state.SetYaw(degrees);}
        public void Invalidate(){state.Invalidate();}
        public Texture Render(HeroClass hero,ItemData weapon,ItemData armor,ItemData relic,FashionData wings,FashionData fashionWeapon)
        {
            if(stage==null||camera==null){float yaw=state.Yaw;Dispose();state.SetYaw(yaw);CreateStage();}
            state.Observe(new CollectionPreviewAppearance(hero,weapon,armor,relic,wings,fashionWeapon));
            if(texture==null)CreateTexture();
            if(!texture.IsCreated()){texture.Create();state.InvalidateTexture();}
            if(!texture.IsCreated())return null;
            if(Event.current.type!=EventType.Repaint||!state.ShouldRender(true,Time.frameCount))return texture;
            if(state.NeedsModel)
            {
                if(avatar!=null){avatar.SetActive(false);UnityEngine.Object.Destroy(avatar);}
                avatar=new GameObject("Preview mannequin");avatar.transform.SetParent(stage.transform,false);
                var random=UnityEngine.Random.state;
                try {model=CombatModel.Hero(avatar.transform,hero);model.ApplyEquipment(weapon,armor,relic);model.ApplyFashion(wings,fashionWeapon);model.Animate(0,0,false);}
                finally {UnityEngine.Random.state=random;}
                foreach(var t in avatar.GetComponentsInChildren<Transform>(true))t.gameObject.layer=PreviewLayer;
                foreach(var c in avatar.GetComponentsInChildren<Collider>(true))c.enabled=false;
                foreach(var behaviour in avatar.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
                Bounds bounds=new Bounds(avatar.transform.position+Vector3.up,Vector3.one*.1f);
                foreach(var renderer in avatar.GetComponentsInChildren<Renderer>(true))
                {renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;bounds.Encapsulate(renderer.bounds);}
                float radial=Mathf.Sqrt(Mathf.Pow(Mathf.Abs(bounds.center.x-stage.transform.position.x)+bounds.extents.x,2)+Mathf.Pow(Mathf.Abs(bounds.center.z-stage.transform.position.z)+bounds.extents.z,2));
                camera.aspect=(float)texture.width/texture.height;
                camera.orthographicSize=Mathf.Max(1.25f,bounds.extents.y,radial/camera.aspect)*1.18f;
                Vector3 center=new Vector3(stage.transform.position.x,bounds.center.y,stage.transform.position.z);
                camera.transform.position=center+Vector3.forward*8;camera.transform.LookAt(center);
                state.ModelReady();
            }
            avatar.transform.localRotation=Quaternion.Euler(0,state.Yaw,0);
            camera.Render();state.Rendered(Time.frameCount);
            return texture;
        }
        void CreateStage()
        {
            stage=new GameObject("Collection preview (isolated)"){hideFlags=HideFlags.HideAndDontSave};stage.transform.position=new Vector3(0,-10000,0);
            var lens=new GameObject("Preview camera");lens.transform.SetParent(stage.transform,false);camera=lens.AddComponent<Camera>();
            camera.enabled=false;camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.06f,.09f);
            camera.cullingMask=1<<PreviewLayer;camera.nearClipPlane=.1f;camera.farClipPlane=30;camera.allowHDR=false;camera.allowMSAA=false;
            CreateTexture();
            var lamp=new GameObject("Preview light");lamp.transform.SetParent(stage.transform,false);lamp.transform.localRotation=Quaternion.Euler(35,155,0);
            var light=lamp.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.cullingMask=1<<PreviewLayer;light.shadows=LightShadows.None;
        }
        void CreateTexture()
        {
            texture=new RenderTexture(384,480,16){name="Collection preview",hideFlags=HideFlags.HideAndDontSave};
            texture.Create();camera.targetTexture=texture;state.InvalidateTexture();
        }
        public void Dispose()
        {
            if(stage!=null){stage.SetActive(false);UnityEngine.Object.Destroy(stage);}
            if(texture!=null){texture.Release();UnityEngine.Object.Destroy(texture);}stage=null;avatar=null;model=null;camera=null;texture=null;state=new CollectionPreviewState();
        }
    }
}
