using HarmonyLib;
using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Linq;

namespace DragSorting
{
    [HotSwappable]
    [HarmonyPatch(typeof(DesignationCategoryDef), "ResolvedAllowedDesignators", MethodType.Getter)]
    public static class DesignationCategoryDef_ResolvedAllowedDesignators_Patch
    {
        public static bool recursionTrap;
        public static IEnumerable<Designator> Postfix(IEnumerable<Designator> __result, DesignationCategoryDef __instance)
        {
            if (recursionTrap)
            {
                return __result;
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
            return modifiedDesignators;
        }
    }
}