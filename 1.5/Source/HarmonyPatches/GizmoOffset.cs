using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DragSorting
{
    [HotSwappable]
    public class GizmoOffset : IExposable
    {
        public string defaultLabel;
        public string defaultDesc;
        public string iconTexPath;
        public int order;
        public string type;

        public void ExposeData()
        {
            Scribe_Values.Look(ref defaultLabel, "defaultLabel");
            Scribe_Values.Look(ref defaultDesc, "defaultDesc");
            Scribe_Values.Look(ref iconTexPath, "iconTexPath");
            Scribe_Values.Look(ref order, "offset");
            Scribe_Values.Look(ref type, "type");
        }

        public bool Matches(Gizmo gizmo)
        {
            if (gizmo is Designator_Dropdown dropdown)
            {
                foreach (var designator in dropdown.elements)
                {
                    if (Matches(designator)) return true; 
                }
            }
            if (gizmo is Command command)
            {
                string normalizedCommandDesc = command.defaultDesc?.Replace("\r\n", "\n");
                string normalizedDefaultDesc = defaultDesc?.Replace("\r\n", "\n");
                string normalizedCommandLabel = command.defaultLabel?.Replace("\r\n", "\n");
                string normalizedDefaultLabel = defaultLabel?.Replace("\r\n", "\n");

                var matches = normalizedCommandLabel == normalizedDefaultLabel &&
                              normalizedCommandDesc == normalizedDefaultDesc &&
                              command.icon?.name == iconTexPath;
                return matches;
            }
            return gizmo.GetType().FullName == type;
        }

        string GetDifferences(string a, string b)
        {
            int minLength = Math.Min(a.Length, b.Length);
            List<string> diffs = new();

            for (int i = 0; i < minLength; i++)
            {
                if (a[i] != b[i])
                    diffs.Add($"[{i}] '{a[i]}' != '{b[i]}'");
            }

            if (a.Length > minLength)
                diffs.Add($"Extra in first: {a.Substring(minLength)}");
            if (b.Length > minLength)
                diffs.Add($"Extra in second: {b.Substring(minLength)}");

            return string.Join(", ", diffs);
        }
    }
}