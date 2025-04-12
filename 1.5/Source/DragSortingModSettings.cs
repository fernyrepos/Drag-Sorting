using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DragSorting
{
    [HotSwappable]
    public class DragSortingModSettings : ModSettings
    {
        public Dictionary<DesignationCategoryDef, GizmoOffsets> reorderedDesignators = new Dictionary<DesignationCategoryDef, GizmoOffsets>();
        public Dictionary<DesignationCategoryDef, GizmoOffsets> movedDesignators = new Dictionary<DesignationCategoryDef, GizmoOffsets>();
        public List<CategoryTabOrder> reorderedCategories = new List<CategoryTabOrder>();
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref reorderedDesignators, "reorderedDesignators", LookMode.Def, LookMode.Deep);
            Scribe_Collections.Look(ref movedDesignators, "movedDesignators", LookMode.Def, LookMode.Deep);
            Scribe_Collections.Look(ref reorderedCategories, "reorderedCategories", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                reorderedCategories ??= new List<CategoryTabOrder>();
                reorderedDesignators ??= new Dictionary<DesignationCategoryDef, GizmoOffsets>();
                movedDesignators ??= new Dictionary<DesignationCategoryDef, GizmoOffsets>();
            }
        }


        public void ResetArchitectMenu()
        {
            reorderedDesignators.Clear();
            reorderedCategories.Clear();
            DesignationCategoryDef_ResolvedAllowedDesignators_Patch.ClearCache();
            var ui = (MainTabWindow_Architect)MainButtonDefOf.Architect.TabWindow;
            if (ui != null)
            {
                ui.CacheDesPanels();
                ui.CacheSearchState();
            }
        }

        public void ResetThingGizmos()
        {
            Thing_ExposeData_Patch.thingReorderedGizmos.Clear();
        }
    }
}