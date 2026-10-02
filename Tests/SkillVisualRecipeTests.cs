using System;
using Emberfall;
public static class SkillVisualRecipeTests
{
    public static string Run()
    {
        int checks=0;
        for(int step=0;step<60;step++) foreach(bool final in new[]{false,true})
        {
            if(SkillVisualRecipes.Ultimate(ElementalistSpecialization.Burn,step,final)!=SkillVisualRecipe.Fire)throw new Exception("burn palette independent"); checks++;
            if(SkillVisualRecipes.Ultimate(ElementalistSpecialization.Shatter,step,final)!=SkillVisualRecipe.Ice)throw new Exception("shatter palette independent"); checks++;
            var expected=final?SkillVisualRecipe.Arcane:step%3==0?SkillVisualRecipe.Fire:step%3==1?SkillVisualRecipe.Ice:SkillVisualRecipe.Lightning;
            if(SkillVisualRecipes.Ultimate(ElementalistSpecialization.None,step,final)!=expected)throw new Exception("balanced phase identity"); checks++;
        }
        foreach(SkillVisualRecipe recipe in Enum.GetValues(typeof(SkillVisualRecipe)))
        {
            var kind=SkillVisualRecipes.Filled(recipe);
            if((kind==FilledVfxKind.Fire)!=(recipe==SkillVisualRecipe.Fire))throw new Exception("only fire makes flame");checks++;
            if((kind==FilledVfxKind.Ice)!=(recipe==SkillVisualRecipe.Ice))throw new Exception("only ice makes crystal");checks++;
        }
        return "PASS: "+checks+" explicit visual recipe assertions (no rendered validation)";
    }
}
