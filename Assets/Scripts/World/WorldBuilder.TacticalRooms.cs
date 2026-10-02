using UnityEngine;
namespace Emberfall
{
    public static partial class WorldBuilder
    {
        private static void BuildTacticalRoom(Transform parent,WorldResources r,int layout)
        {
            // Shared entrance/exit shell, without old room interiors.
            BuildLinkedRoom(parent,r,5);
            TacticalRoomGeometry.Register(layout);
            Material stone=r.Material(new Color(.33f,.38f,.42f));
            foreach(var wall in TacticalRoomGeometry.Walls(layout))
                Primitive(parent,"Tactical sight-blocking wall",PrimitiveType.Cube,wall.Position+Vector3.up*.9f,new Vector3(wall.Size.x,1.8f,wall.Size.y),stone,cameraOccluder:true);
            if(TacticalRoomGeometry.Flooded(layout))
            {
                Rect bridge=TacticalRoomGeometry.Bridge(layout);
                Ribbon(parent,r,"Flooded crossing",TacticalRoomGeometry.River(),3.2f,.06f,r.Material(new Color(.06f,.27f,.35f)));
                Primitive(parent,"Offset wooden bridge",PrimitiveType.Cube,new Vector3((bridge.xMin+bridge.xMax)*.5f,.09f,0),new Vector3(5.4f,.1f,6),r.Material(new Color(.42f,.28f,.16f)));
            }
        }
        public static GameObject MakeRoomObjective(Vector3 position)
        {
            GameObject root=new GameObject("Room capture boundary");root.transform.position=position;
            WorldResources r=root.AddComponent<WorldResources>();Material gold=r.Material(new Color(1,.8f,.25f),true);
            Ring(root.transform,r,"Stand inside",position+Vector3.up*.08f,RoomTacticalRegion.CaptureRadius,.09f,gold,false);
            Crystal(root.transform,r,position+Vector3.up*.6f,.35f,gold);
            return root;
        }
        public static GameObject MakeRoomContestMarker(Transform enemy,float footprint)
        {
            var root=new GameObject("Contesting objective: double gold ring");
            root.transform.SetParent(enemy,false);
            var r=root.AddComponent<WorldResources>();
            var gold=r.Material(new Color(1f,.82f,.18f),true);
            Ring(root.transform,r,"Contest inner",enemy.position+Vector3.up*.08f,footprint+.10f,.055f,gold,false);
            Ring(root.transform,r,"Contest outer",enemy.position+Vector3.up*.08f,footprint+.23f,.055f,gold,false);
            return root;
        }
    }
}
