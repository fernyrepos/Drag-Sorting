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
        public string tutorTag; // Added for faster Command matching

        // Cached values for optimization
        private string _normalizedDefaultDesc;
        private string _normalizedDefaultLabel;
        private Type _cachedType;
        private bool _cachePopulated = false;
        public void ExposeData()
        {
            Scribe_Values.Look(ref defaultLabel, "defaultLabel");
            Scribe_Values.Look(ref defaultDesc, "defaultDesc");
            Scribe_Values.Look(ref iconTexPath, "iconTexPath");
            Scribe_Values.Look(ref order, "offset");
            Scribe_Values.Look(ref type, "type");
            Scribe_Values.Look(ref tutorTag, "tutorTag"); // Added

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                PopulateCache();
            }
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
                // --- TutorTag Matching (Primary) ---
                // Use tutorTag for matching if available on both this offset and the command
                bool tutorTagAvailable = !string.IsNullOrEmpty(tutorTag);
                bool commandTutorTagAvailable = !string.IsNullOrEmpty(command.tutorTag);

                if (tutorTagAvailable && commandTutorTagAvailable && string.Equals(tutorTag, command.tutorTag, StringComparison.Ordinal) is false)
                {
                    return false;
                }
                // If only one has a tutorTag, they don't match based on tutorTag alone.
                // If this offset expects a tutorTag but the command doesn't have one, it's not a match.
                if (tutorTagAvailable && !commandTutorTagAvailable)
                {
                    return false;
                }
                // If the command has a tutorTag but this offset doesn't specify one,
                // proceed to fallback matching (icon/label/desc).

                // --- Fallback Matching (Icon/Label/Desc) ---
                // Ensure cache is populated if loaded outside of normal Scribe flow

                // Check icon first
                if (!string.Equals(command.icon?.name, iconTexPath, StringComparison.Ordinal))
                {
                    return false;
                }

                if (!_cachePopulated) PopulateCache();

                // Check label next, normalizing only if icon matched
                string normalizedCommandLabel = command.defaultLabel?.Replace("\r\n", "\n");
                if (!string.Equals(normalizedCommandLabel, _normalizedDefaultLabel, StringComparison.Ordinal))
                {
                    return false;
                }

                // Check description last, normalizing only if icon and label matched
                string normalizedCommandDesc = command.defaultDesc?.Replace("\r\n", "\n");
                if (!string.Equals(normalizedCommandDesc, _normalizedDefaultDesc, StringComparison.Ordinal))
                {
                    return false;
                }

                // All fallback checks passed
                return true;
            }
            // Ensure cache is populated
            if (!_cachePopulated) PopulateCache();
            // Use cached type if available, otherwise fallback to string comparison (should be rare)
            return (_cachedType != null) ? gizmo.GetType() == _cachedType : string.Equals(gizmo.GetType().FullName, type, StringComparison.Ordinal);
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

        public string ToName()
        {
            return iconTexPath;
        }

        private void PopulateCache()
        {
            if (_cachePopulated) return;

            _normalizedDefaultDesc = defaultDesc?.Replace("\r\n", "\n");
            _normalizedDefaultLabel = defaultLabel?.Replace("\r\n", "\n");

            if (!string.IsNullOrEmpty(type))
            {
                _cachedType = GenTypes.GetTypeInAnyAssembly(type);
                if (_cachedType == null)
                {
                    // Log warning only once per offset type if type resolution fails
                    Log.WarningOnce($"[DragSorting] Could not resolve type '{type}' for GizmoOffset matching. Falling back to string comparison.", type.GetHashCode());
                }
            }
            _cachePopulated = true;
        }
    }
}