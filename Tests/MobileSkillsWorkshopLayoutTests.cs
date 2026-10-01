using System;
using Emberfall;

public static class MobileSkillsWorkshopLayoutTests
{
    private static int checks;
    private static void Check(bool value, string message)
    { checks++; if (!value) throw new Exception(message); }

    public static string Run()
    {
        checks = 0;
        float[][] devices = {
            new[] { 568f, 320f, 163f }, new[] { 667f, 375f, 163f }, new[] { 1334f, 750f, 326f },
            new[] { 2340f, 1080f, 460f }, new[] { 2048f, 1536f, 264f },
            new[] { 2388f, 1668f, 264f }, new[] { 2732f, 2048f, 264f }, new[] { 1280f, 720f, 0f }
        };
        foreach (float[] device in devices)
        {
            var controls = new MobileControlLayout(device[0], device[1], device[2]);
            var panel = new MobilePanelLayout(controls.Width, controls.Height);
            Check(panel.BodyLeft.Height >= 188 && panel.BodyRight.Height >= 188, "compact skill panes retain at least 188 touch units of scroll viewport");
            Check(panel.TabbedBody.Height >= 136, "compact camp keeps at least 136 touch units below tabs");
            Check(panel.BodyLeft.Width >= 216 && panel.BodyRight.Width >= 300, "skill list and wrapped detail have independent readable widths");
            Check(panel.TabbedBody.Width - 32 >= 504, "full-width camp cards leave room for 14–16 unit wrapped text");
            Check(!panel.BodyLeft.Overlaps(panel.BodyRight), "skill scroll panes do not overlap");
            Check(!panel.Body.Overlaps(panel.Header) && !panel.Body.Overlaps(panel.Footer), "skill header and footer stay outside scrolling content");
            Check(!panel.TabbedBody.Overlaps(panel.Tabs) && !panel.TabbedBody.Overlaps(panel.Footer), "camp tabs and footer stay outside scrolling content");
            foreach (var area in new[] { panel.BodyLeft, panel.BodyRight, panel.TabbedBody, panel.Footer, panel.Close })
                Check(area.X >= 0 && area.Y >= 0 && area.XMax <= controls.Width + .01f && area.YMax <= controls.Height + .01f,
                    "panel area is contained in actual safe-area touch geometry");
            for (int i = 0; i < 4; i++)
            {
                var tab = panel.Tab(i, 4);
                Check(tab.Height >= 44 && tab.Width >= 120, "four camp tabs retain touch-size actions");
                if (i > 0) Check(!tab.Overlaps(panel.Tab(i - 1, 4)), "camp tab targets do not overlap");
            }
            foreach (int count in new[] { 2, 3 })
                for (int i = 0; i < count; i++)
                {
                    var action = panel.FooterButton(i, count);
                    Check(action.Height == 48 && action.Width >= 170, "skill and workshop footer actions remain 48-unit targets");
                    Check(!action.Overlaps(panel.Body) && !action.Overlaps(panel.TabbedBody), "fixed action cannot be covered by content");
                    if (i > 0) Check(!action.Overlaps(panel.FooterButton(i - 1, count)), "footer action targets do not overlap");
                }
            var dialog=new MobileDialogLayout(controls.Width,controls.Height);
            Check(Math.Abs(dialog.Frame.X*2+dialog.Frame.Width-controls.Width)<.01f&&Math.Abs(dialog.Frame.Y*2+dialog.Frame.Height-controls.Height)<.01f,"save/build confirmation stays centered");
            Check(dialog.Body.Height>=178,"compact confirmation retains scrollable body space");
            Check(!dialog.Body.Overlaps(dialog.Header)&&!dialog.Body.Overlaps(dialog.Footer),"long confirmation content cannot cover title/actions");
            foreach(var area in new[]{dialog.Frame,dialog.Header,dialog.Body,dialog.Footer})
                Check(area.X>=0&&area.Y>=0&&area.XMax<=controls.Width+.01f&&area.YMax<=controls.Height+.01f,"dialog contained in safe area");
            foreach(int count in new[]{2,3})for(int i=0;i<count;i++)
            {
                var action=dialog.FooterButton(i,count);
                Check(action.Height==48&&action.Width>=160,"three-way save/load labels retain readable 48-unit action targets");
                Check(!action.Overlaps(dialog.Body),"dialog actions cannot overlap measured content");
                if(i>0)Check(!action.Overlaps(dialog.FooterButton(i-1,count)),"dialog actions stay separate");
            }
        }
        var compact = new MobilePanelLayout(568, 320);
        Check(compact.BodyLeft.Height == 188 && compact.TabbedBody.Height == 136, "568×320 boundary uses intended body heights without shrinking text");
        Check(compact.BodyLeft.Height >= 3 * 48 && compact.TabbedBody.Height >= 2 * 48,
            "compact view exposes multiple touch-sized rows before content scrolling");
        return checks + " mobile skills/workshop compact phone and tablet geometry assertions passed";
    }
}
