using System.Collections.Generic;
using Verse;

namespace DragSorting
{
    public class GizmoOffsets : IExposable
    {
        public List<GizmoOffset> gizmoOffsets = new List<GizmoOffset>();
        public void ExposeData()
        {
            Scribe_Collections.Look(ref gizmoOffsets, "gizmoOffsets", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                gizmoOffsets ??= new List<GizmoOffset>();
            }
        }
    }
}