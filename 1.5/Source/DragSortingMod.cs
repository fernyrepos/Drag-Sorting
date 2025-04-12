using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DragSorting
{
    [HotSwappable]
    public class DragSortingMod : Mod
    {
        public DragSortingMod(ModContentPack pack) : base(pack)
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                settings = GetSettings<DragSortingModSettings>();
            });
            new Harmony("DragSortingMod").PatchAll();
        }

        public static DragSortingModSettings settings;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            float y = inRect.y;
            if (Widgets.ButtonText(new Rect(0f, y, inRect.width, 24f), "Reset Pawn Gizmos Order"))
            {
                Thing_ExposeData_Patch.thingReorderedGizmos.RemoveAll(x => x.Key is Pawn);
            }
            y += 28f;
            if (Widgets.ButtonText(new Rect(0f, y, inRect.width, 24f), "Reset Thing Gizmos Order"))
            {
                Thing_ExposeData_Patch.thingReorderedGizmos.RemoveAll(x => x.Key is not Pawn and not Building);
            }
            y += 28f;
            if (Widgets.ButtonText(new Rect(0f, y, inRect.width, 24f), "Reset Building Gizmos Order"))
            {
                Thing_ExposeData_Patch.thingReorderedGizmos.RemoveAll(x => x.Key is Building);
            }
            y += 28f;
            if (Widgets.ButtonText(new Rect(0f, y, inRect.width, 24f), "Reset Architect Menu Order"))
            {
                settings.reorderedCategories.Clear();
                var ui = (MainTabWindow_Architect)MainButtonDefOf.Architect.TabWindow;
                if (ui != null)
                {
                    ui.CacheDesPanels();
                    ui.CacheSearchState();
                }
            }
            y += 28f;
            if (Widgets.ButtonText(new Rect(0f, y, inRect.width, 24f), "Reset All"))
            {
                settings.ResetThingGizmos();
                settings.ResetArchitectMenu();
            }
        }

        public override string SettingsCategory()
        {
            return Content.Name;
        }
    }
}