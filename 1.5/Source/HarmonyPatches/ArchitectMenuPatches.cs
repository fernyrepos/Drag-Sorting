using HarmonyLib;
using Verse;
using RimWorld;

namespace DragSorting
{
    [HotSwappable]
    [HarmonyPatch(typeof(ArchitectCategoryTab), "DesignationTabOnGUI")]
    public static class ArchitectCategoryTab_DesignationTabOnGUI_Patch
    {
        public static ArchitectCategoryTab instance;
        public static void Prefix(ArchitectCategoryTab __instance, Designator forceActivatedCommand)
        {
            instance = __instance;
        }

        public static void Postfix()
        {
            instance = null;
        }
    }
}