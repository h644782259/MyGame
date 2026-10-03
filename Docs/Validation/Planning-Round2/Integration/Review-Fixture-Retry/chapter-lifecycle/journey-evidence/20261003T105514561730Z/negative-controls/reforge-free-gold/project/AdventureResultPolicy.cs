namespace Emberfall
{
 /// <summary>A terminal adventure freezes both damage and economy callbacks in the same frame.</summary>
 public static class AdventureResultPolicy
 {
  public static bool AcceptsDamage(bool started,bool terminal){return started&&!terminal;}
  public static bool AcceptsKill(bool started,bool terminal,bool registered){return AcceptsDamage(started,terminal)&&registered;}
 }
}
