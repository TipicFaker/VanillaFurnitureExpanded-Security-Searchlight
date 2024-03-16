﻿using RimWorld;
using Verse;

namespace VFESecurity
{

    [StaticConstructorOnStartup]
    public static class StaticConstructorClass
    {
        static StaticConstructorClass()
        {
            ArtilleryStrikeUtility.SetCache();

            var thingDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < thingDefs.Count; i++)
            {
                var tDef = thingDefs[i];
                if (tDef.IsWithinCategory(ThingCategoryDefOf.StoneChunks))
                {
                    tDef.projectileWhenLoaded = ThingDefOf.VFES_Artillery_Rock;
                }
            }
        }


    }

}
