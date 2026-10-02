using System;
namespace Emberfall
{
    public enum BlenderPilotClip { Idle, Move, Basic, Hit, Skill }
    public struct BlenderPilotSample
    {
        public BlenderPilotClip Clip;
        public float NormalizedTime;
        public BlenderPilotSample(BlenderPilotClip clip,float time) { Clip=clip;NormalizedTime=time; }
    }
    // Presentation clock only. Accepts the authoritative existing action phase;
    // it cannot dispatch attacks or decide whether a hit is legal.
    public static class BlenderPilotPosePolicy
    {
        private static float Safe(float value) { return float.IsNaN(value)||float.IsInfinity(value)?0:value; }
        private static float Clamp(float value) { return Math.Max(0,Math.Min(1,Safe(value))); }
        private static float Repeat(float value) { value=Safe(value);return value-(float)Math.Floor(value); }
        public static BlenderPilotSample Select(float time,float idleLength,float phase,float speed,
            bool acting,bool basic,float actionProgress,float hurtAge)
        {
            if(acting)return new BlenderPilotSample(basic?BlenderPilotClip.Basic:BlenderPilotClip.Skill,Clamp(actionProgress));
            if(hurtAge>=0&&hurtAge<.22f)return new BlenderPilotSample(BlenderPilotClip.Hit,Clamp(hurtAge/.22f));
            if(speed>.05f)return new BlenderPilotSample(BlenderPilotClip.Move,Repeat(phase/((float)Math.PI*2)));
            return new BlenderPilotSample(BlenderPilotClip.Idle,Repeat(time/Math.Max(.001f,Safe(idleLength))));
        }
    }
}
