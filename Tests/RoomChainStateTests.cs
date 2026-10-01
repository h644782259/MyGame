using System;using Emberfall;
public static class RoomChainStateTests
{
 static int n;static void Check(bool b,string m){n++;if(!b)throw new Exception(m);}
 public static string Run()
 {
  n=0;var run=new RoomChainState();RoomChainPlan first=run.Room;Check(!run.Next(true,false),"locked door cannot be skipped by movement");
  for(int room=0;room<5;room++)
  {
   var plan=run.Room;Check(plan.Index==room&&plan.Layout==10+room,"distinct ordered room identity");
   if(plan.Interlude){Check(!run.DoorUnlocked&&run.ChooseInterlude()&&!run.ChooseInterlude(),"interlude choice exactly once unlocks exit");}
   else for(int enemy=0;enemy<plan.EnemyCount;enemy++)
   {Check(!run.Defeat(plan,enemy),"unspawned kills ignored");Check(run.Register(plan,enemy)&&!run.Register(plan,enemy),"spawn once");Check(run.Defeat(plan,enemy)&&!run.Defeat(plan,enemy),"enemy kill once; props cannot provide indices");}
   if(room<4){Check(run.DoorUnlocked&&!run.Next(false,false)&&!run.Next(true,true),"requires cleared room, proximity,unblocked interaction");Check(run.Next(true,false),"one forward transition");Check(!run.Defeat(plan,0),"stale callback from destroyed room ignored");}
  }
  Check(run.Finished&&!run.Failed&&!run.Next(true,false),"boss room terminates without backtrack");
  Check(!run.ClaimReward(false)&&run.ClaimReward(true)&&!run.ClaimReward(true),"reward only after durable commit once");
  run.Dispose();Check(!run.Register(first,0)&&!run.ChooseInterlude(),"disposed state inert");
  var failed=new RoomChainState();failed.Fail();Check(failed.Finished&&failed.Failed&&!failed.Next(true,false)&&!failed.ClaimReward(true),"failure cannot pay or advance");
  return n+" room-chain gate/identity/transition/reward assertions passed";
 }
}
