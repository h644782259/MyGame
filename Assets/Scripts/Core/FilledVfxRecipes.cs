using System;

namespace Emberfall
{
    public enum FilledVfxKind { Crescent, Ice, Fire, Summon, Charge, Thrust }
    public sealed class FilledMeshRecipe
    {
        public readonly float[] Positions, Uv;
        public readonly int[] Triangles;
        public FilledMeshRecipe(float[] positions,float[] uv,int[] triangles)
        {Positions=positions;Uv=uv;Triangles=triangles;}
    }
    public struct FilledVfxFrame
    {
        public readonly float Progress, Expansion, Opacity;
        public FilledVfxFrame(float progress,float expansion,float opacity)
        {Progress=progress;Expansion=expansion;Opacity=opacity;}
    }
    public static class FilledVfxRecipes
    {
        public const int MaximumParts=14, ReducedParts=7, DesktopEffects=20, MobileEffects=12;
        public static FilledVfxFrame Sample(FilledVfxKind kind,float age,float life)
        {
            if(!Finite(age)||!Finite(life)||life<=0||age<0)return new FilledVfxFrame(0,0,0);
            float t=Math.Max(0,Math.Min(1,age/life));
            float expansion=kind==FilledVfxKind.Charge?.55f+t*.45f:.42f+.72f*(1-(float)Math.Pow(1-Math.Min(1,t*2.8f),3));
            float opacity=kind==FilledVfxKind.Charge?Math.Min(1,t*8)*Math.Min(1,(1-t)*8):Math.Min(1,(1-t)*3.5f);
            return new FilledVfxFrame(t,expansion,Math.Max(0,opacity));
        }
        // Closed diamond-section blade, with a broad convex cutting face and tapered tips.
        public static FilledMeshRecipe Crescent(int segments=36)
        {
            segments=Math.Max(12,Math.Min(64,segments));int count=(segments+1)*4;
            float[] vertices=new float[count*3],uv=new float[count*2];int[] triangles=new int[segments*24+12];
            for(int i=0;i<=segments;i++)
            {
                float t=(float)i/segments,a=(-108+216*t)*(float)Math.PI/180;
                float taper=.015f+(float)Math.Pow(Math.Sin(t*Math.PI),.7);
                for(int j=0;j<4;j++)
                {
                    float r=j==0?1.04f:j==2?1-.37f*taper:1-.15f*taper;
                    float y=(j==1?.09f:j==3?-.075f:0)*taper+.055f*(float)Math.Sin(a);
                    int k=i*4+j;vertices[k*3]=(float)Math.Sin(a)*r;vertices[k*3+1]=y;vertices[k*3+2]=(float)Math.Cos(a)*r;
                    uv[k*2]=t;uv[k*2+1]=j==0?1:j==2?0:.6f;
                    if(i==segments)continue;
                    int p=(i*4+j)*6,n=i*4+(j+1)%4;
                    triangles[p]=k;triangles[p+1]=k+4;triangles[p+2]=n;triangles[p+3]=n;triangles[p+4]=k+4;triangles[p+5]=n+4;
                }
            }
            int end=segments*24,last=segments*4;
            int[] caps={0,1,2,0,2,3,last,last+2,last+1,last,last+3,last+2};Array.Copy(caps,0,triangles,end,caps.Length);
            return new FilledMeshRecipe(vertices,uv,triangles);
        }
        public static FilledMeshRecipe Crystal()
        {
            const int sides=6;float[] p=new float[sides*6*3],uv=new float[sides*6*2];int[] tr=new int[sides*6];
            for(int i=0;i<sides;i++)
            {
                double a=i*Math.PI*2/sides,b=(i+1)*Math.PI*2/sides;
                float ax=(float)Math.Cos(a)*.5f,az=(float)Math.Sin(a)*.5f,bx=(float)Math.Cos(b)*.5f,bz=(float)Math.Sin(b)*.5f;
                float[] face={ax,.16f,az,.12f,1,.07f,bx,.16f,bz,ax,.16f,az,bx,.16f,bz,0,0,0};
                Array.Copy(face,0,p,i*18,18);
                for(int j=0;j<6;j++){int k=i*6+j;tr[k]=k;uv[k*2]=j%3*.5f;uv[k*2+1]=j==1?1:0;}
            }
            return new FilledMeshRecipe(p,uv,tr);
        }
        // Filled, curved flame volume. Its taper is authored rather than a sphere billboard.
        public static FilledMeshRecipe Flame(int rings=8,int sides=12)
        {
            rings=Math.Max(4,Math.Min(12,rings));sides=Math.Max(6,Math.Min(20,sides));
            int surface=(rings+1)*sides;
            float[] p=new float[(surface+2)*3],uv=new float[(surface+2)*2];int[] tr=new int[(rings+1)*sides*6];
            for(int i=0;i<=rings;i++)for(int j=0;j<sides;j++)
            {
                float t=(float)i/rings,a=(float)(j*Math.PI*2/sides),r=.006f+.46f*(float)Math.Pow(Math.Sin(t*Math.PI),.65)*(1-.5f*t);
                int k=i*sides+j;p[k*3]=(float)Math.Cos(a)*r+.26f*t*t;p[k*3+1]=t;p[k*3+2]=(float)Math.Sin(a)*r+.11f*(float)Math.Sin(t*5)*t;
                uv[k*2]=(float)j/sides;uv[k*2+1]=t;
                if(i==rings)continue;int n=i*sides+(j+1)%sides,o=k*6;
                tr[o]=k;tr[o+1]=k+sides;tr[o+2]=n;tr[o+3]=n;tr[o+4]=k+sides;tr[o+5]=n+sides;
            }
            p[(surface+1)*3]=.26f;p[(surface+1)*3+1]=1;p[(surface+1)*3+2]=.11f*(float)Math.Sin(5);
            uv[(surface+1)*2+1]=1;
            for(int j=0;j<sides;j++)
            {
                int o=rings*sides*6+j*6,n=(j+1)%sides;
                tr[o]=surface;tr[o+1]=j;tr[o+2]=n;
                tr[o+3]=surface+1;tr[o+4]=rings*sides+n;tr[o+5]=rings*sides+j;
            }
            return new FilledMeshRecipe(p,uv,tr);
        }
        private static bool Finite(float n){return !float.IsNaN(n)&&!float.IsInfinity(n);}
    }
}
