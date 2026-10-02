using UnityEngine;
namespace Emberfall
{
    public sealed partial class AdventureCamera
    {
        private Camera viewCamera;
        private GUIStyle occlusionLabel;
        private Font occlusionFont;
        private void UpdateVisibility()
        {
            var game=GameSession.Instance;
            if(viewCamera==null)viewCamera=GetComponent<Camera>();
            if(viewCamera!=null)
            {
                viewCamera.ResetProjectionMatrix();
                if(MobileControls.Active&&game!=null&&game.HasStarted)
                {
                    var layout=MobileControls.Layout;var clear=layout.CombatView;var safe=MobileControls.SafeArea;
                    // The camera continues looking at the hero; a finite off-axis
                    // projection places that hero in a geometry-verified HUD gap.
                    float x=(safe.x+(clear.X+clear.Width*.5f)*layout.Scale)/Screen.width;
                    float y=(safe.y+safe.height-(clear.Y+clear.Height*.5f)*layout.Scale)/Screen.height;
                    Matrix4x4 projection=viewCamera.projectionMatrix;
                    projection.m02=CameraVisibilityRules.Projection(x);projection.m12=CameraVisibilityRules.Projection(y);viewCamera.projectionMatrix=projection;
                }
            }
            if(game==null||game.Player==null||!game.HasStarted||game.IsDead){CameraOcclusionSurface.RestoreAll();return;}
            CameraOcclusionSurface.Advance(transform.position,game.Player.transform.position+Vector3.up,Time.unscaledDeltaTime);
        }
        private void RestoreVisibility(){CameraOcclusionSurface.RestoreAll();if(viewCamera!=null)viewCamera.ResetProjectionMatrix();GameFont.Release(ref occlusionFont);occlusionLabel=null;}
        private void OnGUI()
        {
            var game=GameSession.Instance;
            if(CameraOcclusionSurface.LastOccluders==0||viewCamera==null||game==null||game.Player==null||game.IsDead||game.InputBlocked)return;
            Vector3 point=viewCamera.WorldToScreenPoint(game.Player.transform.position+Vector3.up*1.25f);if(point.z<=0)return;
            if(occlusionLabel==null){occlusionFont=GameFont.Shared;occlusionLabel=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,fontSize=12,font=occlusionFont};}
            Matrix4x4 old=GUI.matrix;Color color=GUI.color;int depth=GUI.depth;
            GUI.matrix=Matrix4x4.identity;GUI.depth=50;float density=MobileControls.Active?MobileControls.Layout.Scale:1;
            Rect marker=new Rect(point.x-18*density,Screen.height-point.y-10*density,36*density,20*density);
            GUI.color=new Color(.02f,.05f,.08f,.8f);GUI.DrawTexture(marker,Texture2D.whiteTexture);GUI.color=Color.white;
            occlusionLabel.fontSize=Mathf.RoundToInt(12*density);occlusionLabel.normal.textColor=Color.white;GUI.Label(marker,"角色",occlusionLabel);
            GUI.matrix=old;GUI.color=color;GUI.depth=depth;
        }
    }
}
