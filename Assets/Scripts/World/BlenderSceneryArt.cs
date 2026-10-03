using UnityEngine;
namespace Emberfall
{
    // Scenery-only rollout. Does not opt in the experimental hero or touch game rules.
    // Set false before world construction for the original procedural art fallback.
    public static class BlenderSceneryArt
    {
        public static bool Enabled=true;
        public static GameObject CreatePilotProp(string name,Transform parent,Vector3 position)
        {
            if(!Enabled)return null;
            GameObject source=Resources.Load<GameObject>("BlenderPilot/"+name);
            if(!HasSafeGeometry(source))return null;
            GameObject instance=Object.Instantiate(source,parent,false);
            instance.transform.localPosition=position;
            if(!BlenderPilotArt.ApplyMaterial(instance))return Reject(instance);
            return instance;
        }
        public static GameObject Create(string name,Transform parent,Vector3 position,WorldResources resources)
        {
            if(!Enabled||resources==null)return null;
            GameObject source=Resources.Load<GameObject>("BlenderScenery/"+name);
            if(!HasSafeGeometry(source))return null;
            GameObject instance=Object.Instantiate(source,parent,false);
            instance.transform.localPosition=position;
            foreach(MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                Material[] slots=renderer.sharedMaterials;
                for(int i=0;i<slots.Length;i++)
                {
                    if(slots[i]==null)return Reject(instance);
                    string key=slots[i].name;
                    if(key=="Clay")slots[i]=resources.Material(new Color(.49f,.27f,.18f),false,VisualSurface.Stone);
                    else if(key=="Ochre")slots[i]=resources.Material(new Color(.64f,.43f,.22f),false,VisualSurface.Stone);
                    else if(key=="Stone")slots[i]=resources.Material(new Color(.35f,.4f,.43f),false,VisualSurface.Stone);
                    else if(key=="Bark")slots[i]=resources.Material(new Color(.27f,.20f,.14f),false,VisualSurface.Wood);
                    else if(key=="Leaf")slots[i]=resources.Material(new Color(.18f,.36f,.26f),false,VisualSurface.Foliage);
                    else return Reject(instance);
                }
                renderer.sharedMaterials=slots;
            }
            return instance;
        }
        private static bool HasSafeGeometry(GameObject source)
        {
            if(source==null||source.GetComponentsInChildren<Collider>(true).Length!=0)return false;
            MeshFilter[] filters=source.GetComponentsInChildren<MeshFilter>(true);
            if(filters.Length==0)return false;
            foreach(MeshFilter filter in filters)if(filter.sharedMesh==null||filter.GetComponent<MeshRenderer>()==null)return false;
            return true;
        }
        private static GameObject Reject(GameObject instance)
        { instance.SetActive(false);Object.Destroy(instance);return null; }
    }
}
