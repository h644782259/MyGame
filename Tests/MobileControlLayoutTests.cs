using System;
using System.Collections.Generic;
using Emberfall;
public static class MobileControlLayoutTests
{
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    public static string Run()
    {
        checks=0;
        float[][] devices={new[]{568f,320f,163f},new[]{1334f,750f,326f},new[]{2208f,1080f,401f},new[]{2250f,1125f,458f},new[]{2340f,1080f,460f},new[]{2048f,1536f,264f},new[]{2388f,1668f,264f},new[]{2732f,2048f,264f},new[]{1280f,720f,0f}};
        foreach(var d in devices)
        {
            var l=new MobileControlLayout(d[0],d[1],d[2]);
            var targets=new List<MobileControlLayout.Area>{l.Joystick,l.Attack,l.Dodge,l.Potion,l.Jump,l.Menu,l.Inventory,l.SkillsMenu,l.Interact};targets.AddRange(l.Skills);
            foreach(var r in targets)
            {
                Check(r.Width>=48&&r.Height>=48,"minimum 48 logical-unit touch targets");
                Check(r.X>=0&&r.Y>=0&&r.X+r.Width<=l.Width+.01f&&r.Y+r.Height<=l.Height+.01f,"safe-area contained controls");
            }
            for(int i=0;i<targets.Count;i++)for(int j=i+1;j<targets.Count;j++)Check(!targets[i].Overlaps(targets[j]),"non-overlapping touch hitboxes "+i+"/"+j+" at "+d[0]);
            Check(l.Attack.X>l.Width/2&&l.Joystick.X<l.Width/2,"separate thumb zones");
            Check(l.Cancel.X==l.Jump.X&&l.Cancel.Y==l.Jump.Y,"cancel replaces jump without additional overlap");
            foreach(var feedback in new[]{l.EncounterText,l.BossHealth,l.Notice,l.AdventureStatus})
            {
                Check(feedback.X>=0&&feedback.Y>=0&&feedback.X+feedback.Width<=l.Width&&feedback.Y+feedback.Height<=l.Height,"encounter feedback stays in safe area");
                foreach(var target in targets)Check(!feedback.Overlaps(target),"wave/boss feedback cannot cover a skill or action target");
            }
            Check(!l.AdventureStatus.Overlaps(l.BossHealth)&&!l.AdventureStatus.Overlaps(l.EncounterText),"objective card cannot cover boss bar/label");
            Check(!l.AdventureStatus.Overlaps(new MobileControlLayout.Area(12,12,175,58)),"objective does not cover player status");
            Check(!l.AdventureStatus.Overlaps(l.MoveZone)&&!l.AdventureStatus.Overlaps(l.Notice),"objective does not cover movement or notices");
            Check(l.AdventureStatus.Width>=188&&l.AdventureStatus.Height==76,"four lines fit compact phone/iPad objective card");
            Check(!l.EncounterText.Overlaps(l.BossHealth),"wave label and boss health remain separate");
            Check(!l.Notice.Overlaps(l.MoveZone)&&l.Notice.Width>=100&&l.Notice.Height>=48,"full-message touch target stays outside the entire movement zone");
        }
        Check(MobileControlLayout.DeadZone(.1f)==0,"deadzone rejects drift");
        Check(MobileControlLayout.DeadZone(1)==1&&MobileControlLayout.DeadZone(-2)==-1,"movement saturates safely");
        return checks+" mobile control geometry assertions passed";
    }
}
