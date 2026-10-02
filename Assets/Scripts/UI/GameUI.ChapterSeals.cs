using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private void DrawChapterSeals(Rect area,float u)
        {
            for(int i=0;i<2;i++)
            {
                var seal=session.ChapterSealView(i);if(seal==null)return;
                Rect row=new Rect(area.x,area.y+i*22*u,area.width,21*u);
                if(seal.Occupied)Fill(row,new Color(.16f,.23f,.19f,.95f));
                Color tint=seal.Complete?jade:seal.Contested?gold:seal.Occupied?pale:muted;
                Text(new Rect(row.x+3*u,row.y,row.width-6*u,17*u),seal.Label,Mathf.RoundToInt(11*u),tint,seal.Occupied,false,TextAnchor.MiddleLeft);
                Bar(new Rect(row.x+3*u,row.yMax-3*u,row.width-6*u,2*u),seal.Seconds/3f,tint);
            }
        }
    }
}
