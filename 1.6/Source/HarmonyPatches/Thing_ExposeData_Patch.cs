using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace DragSorting
{
    [HarmonyPatch(typeof(Thing), "ExposeData")]
    public static class Thing_ExposeData_Patch
    {
        public static void Postfix(Thing __instance)
        {
            var reorderedGizmosForThing = __instance.GetReorderedGizmos();
            Scribe_Deep.Look(ref reorderedGizmosForThing, "reorderedGizmosThing");
            if (reorderedGizmosForThing != null)
            {
                thingReorderedGizmos[__instance] = reorderedGizmosForThing;
            }
        }

        public static Dictionary<Thing, GizmoOffsets> thingReorderedGizmos = new Dictionary<Thing, GizmoOffsets>();
        public static GizmoOffsets GetReorderedGizmos(this Thing thing)
        {
            if (!thingReorderedGizmos.TryGetValue(thing, out var data))
            {
                thingReorderedGizmos[thing] = data = new GizmoOffsets();
            }
            return data;
        }
    }
}