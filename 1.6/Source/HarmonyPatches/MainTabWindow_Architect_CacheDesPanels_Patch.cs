using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace DragSorting
{
    [HarmonyPatch(typeof(MainTabWindow_Architect), "CacheDesPanels")]
    public static class MainTabWindow_Architect_CacheDesPanels_Patch
    {
        public static void Postfix(MainTabWindow_Architect __instance)
        {
            __instance.desPanelsCached = SortByOffset(__instance.desPanelsCached);
        }

        private static List<ArchitectCategoryTab> SortByOffset(List<ArchitectCategoryTab> desPanels)
        {
            var reorderedList = DragSortingMod.settings.reorderedCategories;
            if (reorderedList.NullOrEmpty())
            {
                reorderedList = ReorderList(desPanels);
            }
            var ordered = desPanels.OrderBy(x => reorderedList.FirstOrDefault(y => y.Matches(x.def))?.order ?? 0).ToList();
            return ordered;
        }

        public static List<CategoryTabOrder> ReorderList(List<ArchitectCategoryTab> desPanels)
        {
            List<CategoryTabOrder> reorderedList = new List<CategoryTabOrder>();
            for (int i = 0; i < desPanels.Count; i += 2)
            {
                ArchitectCategoryTab architectCategoryTab2 = desPanels[i];
                ArchitectCategoryTab architectCategoryTab3 = ((i + 1 < desPanels.Count) ? desPanels[i + 1] : null);
                if ((architectCategoryTab2.PreferredColumn == 1 || (architectCategoryTab3 != null && architectCategoryTab3.PreferredColumn == 0)) && architectCategoryTab2.PreferredColumn != 0 && (architectCategoryTab3 == null || architectCategoryTab3.PreferredColumn != 1))
                {
                    if (architectCategoryTab3 != null)
                    {
                        reorderedList.Add(new CategoryTabOrder
                        {
                            defName = architectCategoryTab3.def.defName,
                            order = i
                        });
                    }

                    reorderedList.Add(new CategoryTabOrder
                    {
                        defName = architectCategoryTab2.def.defName,
                        order = i + 1
                    });
                }
                else
                {
                    reorderedList.Add(new CategoryTabOrder
                    {
                        defName = architectCategoryTab2.def.defName,
                        order = i
                    });
                    if (architectCategoryTab3 != null)
                    {
                        reorderedList.Add(new CategoryTabOrder
                        {
                            defName = architectCategoryTab3.def.defName,
                            order = i + 1
                        });
                    }
                }
            }
            DragSortingMod.settings.reorderedCategories = reorderedList;
            DragSortingMod.settings.Write();
            return reorderedList;
        }
    }
}