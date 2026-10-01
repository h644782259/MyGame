using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Texture2D terrainMap;
        private string terrainMapKey;
        private void DrawMinimapTerrain(Rect r)
        {
            string key=(session.InDungeon?"d":"w")+session.DungeonLayout;
            if(terrainMap==null||terrainMapKey!=key)
            {
                if(terrainMap!=null)Destroy(terrainMap);
                terrainMapKey=key;
                const int size=64;
                terrainMap=new Texture2D(size,size,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Point};
                float radius=session.ArenaRadius;
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    Vector3 point=new Vector3(((x+.5f)/size*2-1)*radius,0,((y+.5f)/size*2-1)*radius);
                    terrainMap.SetPixel(x,y,WorldTraversal.IsWalkable(point,.16f)?new Color(.14f,.22f,.23f,.65f):new Color(.38f,.43f,.48f,.95f));
                }
                terrainMap.Apply(false,true);
            }
            GUI.DrawTexture(r,terrainMap,ScaleMode.StretchToFill,true);
            if(!session.InDungeon)
            {
                Vector3[] river={new Vector3(-22,0,4.7f),new Vector3(-11,0,2),new Vector3(0,0,-1),new Vector3(7,0,-2),new Vector3(14,0,-5),new Vector3(21,0,-7)};
                for(int i=1;i<river.Length;i++)MapLine(r,river[i-1],river[i],new Color(.21f,.47f,.66f),5);
                MapLine(r,new Vector3(0,0,-16),new Vector3(0,0,11),new Color(.55f,.46f,.3f),3);
                MapLine(r,new Vector3(-1.2f,0,-3.2f),new Vector3(-1.2f,0,1.6f),gold,2);
                MapLine(r,new Vector3(1.2f,0,-3.2f),new Vector3(1.2f,0,1.6f),gold,2);
            }
            else
            {
                MapLine(r,new Vector3(0,0,-15),new Vector3(0,0,13),new Color(.29f,.38f,.42f),3);
                MapLine(r,new Vector3(-14,0,1),new Vector3(14,0,1),new Color(.29f,.38f,.42f),3);
                MapDot(r,new Vector3(12,0,-3),new Color(.33f,.85f,1),4);
            }
            Text(new Rect(r.xMax-17,r.y+1,16,15),"N",10,pale,true,false,TextAnchor.MiddleCenter);
            if(session.Player!=null)MapLine(r,session.Player.transform.position,session.Player.transform.position+session.Player.transform.forward*2.8f,Color.white,2);
        }
        private void MapLine(Rect r,Vector3 from,Vector3 to,Color tint,float thickness)
        {
            float radius=session.ArenaRadius;
            Vector2 a=new Vector2(r.x+(from.x/radius+1)*r.width*.5f,r.yMax-(from.z/radius+1)*r.height*.5f);
            Vector2 b=new Vector2(r.x+(to.x/radius+1)*r.width*.5f,r.yMax-(to.z/radius+1)*r.height*.5f);
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/2));
            for(int i=0;i<=steps;i++){Vector2 p=Vector2.Lerp(a,b,i/(float)steps);Fill(new Rect(p.x-thickness*.5f,p.y-thickness*.5f,thickness,thickness),tint);}
        }
    }
}
