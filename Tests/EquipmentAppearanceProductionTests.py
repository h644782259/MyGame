"""Compile the actual equipment appearance UI against explicit render/GUI doubles; not pixels."""
from pathlib import Path
import subprocess,tempfile,os,sys
root=Path(__file__).resolve().parents[1]
src=(root/'Assets/Scripts/UI/GameUI.EquipmentAppearance.cs').read_text()
fixture=r'''
using System;using UnityEngine;
namespace UnityEngine {
 public struct Rect{public float x,y,width,height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}public float yMax=>y+height;}
 public struct Color{public Color(float a,float b,float c){}}
 public static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);public static float Abs(float a)=>Math.Abs(a);public static int RoundToInt(float a)=>(int)Math.Round(a);}
 public class Texture{} public enum ScaleMode{ScaleToFit}public struct Matrix{public float m00,m11;}public static class GUI{public static Matrix matrix=new Matrix{m00=1,m11=1};public static void DrawTexture(Rect r,Texture t,ScaleMode s,bool b){}}
}
namespace Emberfall {
 public enum ItemSlot{Weapon,Armor,Relic}public enum FashionSlot{Wings,Weapon}public enum CollectionPreviewComposition{Full}
 public class ItemData{public string id;public ItemSlot slot;public int rank;}
 public class FashionData{}public class Profile{public int heroClass;}
 public class Progression{public Profile Profile=new Profile();public ItemData[] items={new ItemData(),new ItemData(),new ItemData()};public FashionData[] fashions={new FashionData(),new FashionData()};public int calls;public ItemData Equipped(ItemSlot s)=>items[(int)s];public FashionData EquippedFashion(FashionSlot s)=>fashions[(int)s];public ItemData PreviewEquippedItem(ItemData i){calls++;return new ItemData{id=i.id,slot=i.slot,rank=7};}public int SlotUpgradeRank(ItemSlot s)=>7;}
 public class Session{public Progression Progression=new Progression();}public static class MobileControls{public static bool Active;}
 public class CollectionModelPreview{public static int Created;public CollectionModelPreview(){Created++;}public ItemData[] items;public FashionData wings,weapon;public int renders;public float yaw;public bool fixedFrame;public void SetComposition(CollectionPreviewComposition c){}public void SetYaw(float y){yaw=y;}public void SetEquipmentFraming(bool a,bool b){fixedFrame=a;}public void SetViewport(float x,float y,bool b){}public Texture Render(int h,ItemData w,ItemData a,ItemData r,FashionData f,FashionData g){items=new[]{w,a,r};wings=f;weapon=g;renders++;return null;}}
 public sealed partial class GameUI{
 Session session=new Session();Color jade,gold,muted;CollectionModelPreview collectionModel;string click,copy;int blocks;
 bool Button(Rect r,string s,Color c){if(click==s){click=null;return true;}return false;}void BlockUITransition(){blocks++;}void ReleaseCollectionModel(){collectionModel=null;}void Fill(Rect r,Color c){}void Text(Rect r,string s,int n,Color c,bool b,bool w){copy=s;}
 static void Check(bool b,string s){if(!b)throw new Exception(s);}
 public static void Main(){var ui=new GameUI();var p=ui.session.Progression;var rect=new Rect(0,0,420,340);var candidate=new ItemData{id="new",slot=ItemSlot.Weapon,rank=0};
 Check(!ui.DrawEquipmentAppearanceDetail(rect,candidate,1)&&CollectionModelPreview.Created==0,"lazy closed preview");ui.equipmentAppearanceOpen=true;
 for(int slot=0;slot<3;slot++){candidate.slot=(ItemSlot)slot;candidate.id="new"+slot;ui.DrawEquipmentAppearanceDetail(rect,candidate,1);Check(object.ReferenceEquals(ui.collectionModel.items[slot],p.items[slot]),"current exact loadout");ui.click="候选装备";ui.DrawEquipmentAppearanceDetail(rect,candidate,1);Check(ui.collectionModel.items[slot].id==candidate.id&&ui.collectionModel.items[slot].rank==7,"candidate uses inherited detached preview");for(int j=0;j<3;j++)if(j!=slot)Check(object.ReferenceEquals(ui.collectionModel.items[j],p.items[j]),"other slots retained");Check(candidate.rank==0&&p.items[slot].id==null,"source loadout and candidate unchanged");Check(ui.collectionModel.wings==p.fashions[0]&&ui.collectionModel.weapon==p.fashions[1]&&ui.copy.Contains("覆盖"),"real cosmetics with explicit override");Check(ui.collectionModel.fixedFrame&&ui.collectionModel.yaw==20,"fixed framing both views");}
 Check(CollectionModelPreview.Created==1,"single renderer reused");ui.click="战斗视距";ui.DrawEquipmentAppearanceDetail(rect,candidate,1);Check(ui.equipmentAppearanceDetail,"detail distance toggle");ui.click="返回属性";ui.DrawEquipmentAppearanceDetail(rect,candidate,1);Check(!ui.equipmentAppearanceOpen&&ui.collectionModel==null,"close releases renderer");Console.WriteLine("PASS equipment appearance actual UI composition, isolation, lazy single renderer, cosmetics and framing contracts");}
 }
}
'''
with tempfile.TemporaryDirectory() as d:
 p=Path(d);(p/'Actual.cs').write_text(src);(p/'Fixture.cs').write_text(fixture)
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NoWarn>0649</NoWarn></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 cmd=[sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Test.csproj')]
 subprocess.run(cmd,check=True)
 # Compiled old behavior control: using raw inventory item drops inherited rank.
 (p/'Actual.cs').write_text(src.replace('p.PreviewEquippedItem(item)','item'))
 result=subprocess.run(cmd,capture_output=True,text=True)
 assert result.returncode and 'candidate uses inherited detached preview' in result.stderr
 print('PASS compiled raw-item negative control fails inherited-preview oracle')
