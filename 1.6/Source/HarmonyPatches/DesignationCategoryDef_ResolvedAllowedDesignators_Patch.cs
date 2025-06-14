using HarmonyLib;
using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Linq;
using System;

namespace DragSorting
{
    [HotSwappable]
    [HarmonyPatch(typeof(DesignationCategoryDef), "ResolvedAllowedDesignators", MethodType.Getter)]
    public static class DesignationCategoryDef_ResolvedAllowedDesignators_Patch
    {
        public static Dictionary<DesignationCategoryDef, List<Designator>> cachedDesignators;
        public static bool recursionTrap;
        private static int frameCounter = 0;
        private const int CacheInvalidationInterval = 60;

        public static IEnumerable<Designator> Postfix(IEnumerable<Designator> __result, DesignationCategoryDef __instance)
        {
            if (recursionTrap)
            {
                return __result;
            }
            
            frameCounter++;
            if (frameCounter >= CacheInvalidationInterval)
            {
                ClearCache();
                frameCounter = 0;
            }
            
            if (cachedDesignators != null && cachedDesignators.TryGetValue(__instance, out var list))
            {
                return list;
            }

            recursionTrap = true;
            IEnumerable<Designator> originalDesignators = __result;
            List<Designator> modifiedDesignators = [.. originalDesignators];

            if (DragSortingMod.settings.movedDesignators != null)
            {
                // Remove moved designators from the original list
                foreach (var designator in originalDesignators.ToList())
                {
                    foreach (var def in DefDatabase<DesignationCategoryDef>.AllDefs)
                    {
                        if (def != __instance && DragSortingMod.settings.movedDesignators.TryGetValue(def, out var movedDesignators))
                        {
                            if (movedDesignators.gizmoOffsets.Any(x => x.Matches(designator)))
                            {
                                modifiedDesignators.Remove(designator);
                            }
                        }
                    }
                }

                foreach (var def in DefDatabase<DesignationCategoryDef>.AllDefs)
                {
                    if (def != __instance)
                    {
                        if (DragSortingMod.settings.movedDesignators.TryGetValue(__instance, out var movedDesignators))
                        {
                            foreach (var otherDesignator in def.ResolvedAllowedDesignators)
                            {
                                if (movedDesignators.gizmoOffsets.Any(x => x.Matches(otherDesignator)))
                                {
                                    modifiedDesignators.Add(otherDesignator);
                                }
                            }
                        }
                    }
                }
            }

            recursionTrap = false;
            cachedDesignators ??= new Dictionary<DesignationCategoryDef, List<Designator>>();
            cachedDesignators[__instance] = modifiedDesignators;
            return modifiedDesignators;
        }

        public static void ClearCache()
        {
            cachedDesignators = null;
        }
    }
}