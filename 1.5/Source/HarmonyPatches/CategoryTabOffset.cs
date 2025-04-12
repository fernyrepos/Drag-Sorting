using System;
using System.Collections.Generic;
using Verse;

namespace DragSorting
{
    [HotSwappable]
    public class CategoryTabOrder : IExposable
    {
        public string defName; // Using defName to identify categories
        public int order;

        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref order, "offset");
        }

        public bool Matches(DesignationCategoryDef def)
        {
            return def.defName == defName;
        }
    }
}