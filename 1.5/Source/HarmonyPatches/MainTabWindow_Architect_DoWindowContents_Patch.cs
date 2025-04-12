using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse.Steam;


namespace DragSorting
{
    [HotSwappable]
    [HarmonyPatch(typeof(MainTabWindow_Architect), "DoWindowContents")]
    public static class MainTabWindow_Architect_DoWindowContents_Patch
    {
        private static int rowReorderableId;
        private static ArchitectCategoryTab currentDraggingCategory;
        public static Dictionary<Rect, ArchitectCategoryTab> categoryRects = new Dictionary<Rect, ArchitectCategoryTab>();
        public static bool Prefix(MainTabWindow_Architect __instance, Rect inRect)
        {
            categoryRects.Clear(); // Clear rects at the start of each frame
            Text.Font = GameFont.Small;
            float butWidth = inRect.width / 2f;
            float num = 0f;
            bool flag = ModsConfig.IdeologyActive && Find.IdeoManager.classicMode;
            ArchitectCategoryTab architectCategoryTab = __instance.OpenTab();
            if (KeyBindingDefOf.Accept.KeyDownEvent)
            {
                if (__instance.quickSearchWidget.filter.Active && architectCategoryTab?.UniqueSearchMatch != null)
                {
                    __instance.forceActivatedCommand = architectCategoryTab.UniqueSearchMatch;
                    Event.current.Use();
                }
                else if (!SteamDeck.IsSteamDeck)
                {
                    __instance.Close();
                    Event.current.Use();
                }
            }

            ReorderGroup(__instance, inRect, 0); // Apply reorderable group to the whole menu

            for (int i = 0; i < __instance.desPanelsCached.Count; i += 2)
            {
                ArchitectCategoryTab architectCategoryTab2 = __instance.desPanelsCached[i];
                ArchitectCategoryTab architectCategoryTab3 = ((i + 1 < __instance.desPanelsCached.Count) ? __instance.desPanelsCached[i + 1] : null);
                DoCategoryRowReorderable(__instance, architectCategoryTab2, architectCategoryTab3, butWidth, num, 
                    architectCategoryTab, new Rect(inRect.x, __instance.WinHeight, inRect.width, inRect.height));
                num += 1f;
            }
            float num2 = inRect.width;
            if (flag)
            {
                num2 -= 32f;
            }
            Rect rect = new Rect(0f, num * 32f + 1f, num2, 24f);
            __instance.quickSearchWidget.OnGUI(rect, __instance.CacheSearchState);
            if (!__instance.didInitialUnfocus)
            {
                UI.UnfocusCurrentControl();
                __instance.didInitialUnfocus = true;
            }
            if (flag && Widgets.ButtonImage(new Rect(rect.xMax + 4f, rect.y, 24f, 24f).ContractedBy(2f), MainTabWindow_Architect.ChangeStyleIcon.Texture))
            {
                if (Find.WindowStack.IsOpen<Dialog_StyleSelection>())
                {
                    Find.WindowStack.TryRemove(typeof(Dialog_StyleSelection));
                }
                else
                {
                    Find.WindowStack.Add(new Dialog_StyleSelection());
                }
            }

            if (currentDraggingCategory != null)
            {
                Vector2 mousePos = Event.current.mousePosition;
                Rect dragRect = new Rect(UI.MousePositionOnUIInverted.x, UI.MousePositionOnUIInverted.y, butWidth, 32f);
                Color? labelColor = Color.white;
                string label = currentDraggingCategory.def.LabelCap;
                Find.WindowStack.ImmediateWindow(currentDraggingCategory.GetHashCode(), dragRect, WindowLayer.Super,
                delegate
                {
                    var rect = dragRect.AtZero();
                    Widgets.ButtonTextSubtle(rect, label);
                });
                currentDraggingCategory = null;
            }
            return false;
        }



        private static void ReorderGroup(MainTabWindow_Architect __instance, Rect inRect, int currentRow)
        {
            if (Event.current.type == EventType.Repaint)
            {
                Rect architectMenuRect = inRect;
                int currentRowForGroup = currentRow;
                rowReorderableId = ReorderableWidget.NewGroup(delegate (int from, int to)
                {
                    ReorderCategories(from, to, currentRowForGroup, __instance);
                }, ReorderableDirection.Horizontal, architectMenuRect, 0f, null);
            }
        }

        private static void DoCategoryRowReorderable(MainTabWindow_Architect __instance, ArchitectCategoryTab left, 
        ArchitectCategoryTab right, float butWidth, float curYInd, ArchitectCategoryTab openTab, Rect inRect)
        {
            float currentX = 0f;
            var offset = (float)(UI.screenHeight - 35) - inRect.height;
            if (left != null)
            {
                Rect buttonRect = new Rect(currentX, curYInd * 32f, butWidth, 32f);
                bool reordering = ReorderableWidget.Reorderable(rowReorderableId, buttonRect.ContractedBy(5), useRightButton: true);
                if (reordering)
                {
                    currentDraggingCategory = left;
                }
                __instance.DoCategoryButton(left, butWidth, currentX / butWidth, curYInd, openTab, left.Visible);
                categoryRects[new Rect(buttonRect.x + inRect.x, buttonRect.y + offset, buttonRect.width, buttonRect.height)] = left; // Store rect and category
                currentX += butWidth;
            }

            if (right != null)
            {
                Rect buttonRect = new Rect(currentX, curYInd * 32f, butWidth, 32f);
                bool reordering = ReorderableWidget.Reorderable(rowReorderableId, buttonRect.ContractedBy(5), useRightButton: true);
                if (reordering)
                {
                    currentDraggingCategory = right;
                }
                __instance.DoCategoryButton(right, butWidth, currentX / butWidth, curYInd, openTab, right.Visible);
                categoryRects[new Rect(buttonRect.x + inRect.x, buttonRect.y + offset, buttonRect.width, buttonRect.height)] = right; // Store rect and category
            }
        }

        private static void ReorderCategories(int from, int to, int currentRow, MainTabWindow_Architect __instance)
        {
            List<ArchitectCategoryTab> desPanels = __instance.desPanelsCached;
            ArchitectCategoryTab draggedCategory = desPanels[from];

            if (draggedCategory == null)
            {
                Log.Error($"Could not find category for visual index {from}");
                return;
            }
            if (to > from)
            {
                to--;
            }
            desPanels.RemoveAt(from);
            desPanels.Insert(to, draggedCategory);

            DragSortingMod.settings.reorderedCategories = desPanels.Select(x => new CategoryTabOrder
            {
                defName = x.def.defName,
                order = desPanels.IndexOf(x)
            }).ToList();

            // Save settings
            DragSortingMod.settings.Write();

            // Refresh the UI
            __instance.CacheDesPanels();
            __instance.CacheSearchState();

        }
    }
}