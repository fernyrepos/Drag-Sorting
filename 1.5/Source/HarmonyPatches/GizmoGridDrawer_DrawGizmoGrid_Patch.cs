using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;
using System.Linq;
using System;
using System.Collections.Generic;
using static Verse.GizmoGridDrawer;
using Verse.Steam;

namespace DragSorting
{
    [HotSwappable]
    [HarmonyPatch(typeof(GizmoGridDrawer), "DrawGizmoGrid")]
    public static class GizmoGridDrawer_DrawGizmoGrid_Patch
    {
        private static int rowReorderableId;
        private static Gizmo currentDragginGizmo;

        [HarmonyPriority(int.MaxValue)]
        public static bool Prefix(IEnumerable<Gizmo> gizmos, float startX, out Gizmo mouseoverGizmo, Func<Gizmo, bool> customActivatorFunc = null, Func<Gizmo, bool> highlightFunc = null, Func<Gizmo, bool> lowlightFunc = null, bool multipleSelected = false)
        {
            DrawGizmoGrid(gizmos, startX, out mouseoverGizmo, customActivatorFunc, highlightFunc, lowlightFunc, multipleSelected);
            return false;
        }
        public static void DrawGizmoGrid(IEnumerable<Gizmo> gizmos, float startX, out Gizmo mouseoverGizmo, Func<Gizmo, bool> customActivatorFunc = null, Func<Gizmo, bool> highlightFunc = null, Func<Gizmo, bool> lowlightFunc = null, bool multipleSelected = false)
        {
            if (Event.current.type == EventType.Layout)
            {
                mouseoverGizmo = null;
                return;
            }
            var architectCategoryTab = ArchitectCategoryTab_DesignationTabOnGUI_Patch.instance;
            var maxWidth = UI.screenWidth - 147 - startX;
            tmpAllGizmos.Clear();
            tmpAllGizmos.AddRange(gizmos);
            tmpAllGizmos.SortStable(SortByOrder);
            tmpAllGizmos = SortByOffset(tmpAllGizmos, architectCategoryTab);

            gizmoGroups.Clear();
            for (int i = 0; i < tmpAllGizmos.Count; i++)
            {
                Gizmo gizmo = tmpAllGizmos[i];
                bool flag = false;
                for (int j = 0; j < gizmoGroups.Count; j++)
                {
                    if (gizmoGroups[j][0].GroupsWith(gizmo))
                    {
                        flag = true;
                        gizmoGroups[j].Add(gizmo);
                        gizmoGroups[j][0].MergeWith(gizmo);
                        break;
                    }
                }
                if (!flag)
                {
                    List<Gizmo> list = SimplePool<List<Gizmo>>.Get();
                    list.Add(gizmo);
                    gizmoGroups.Add(list);
                }
            }
            firstGizmos.Clear();
            shrinkableCommands.Clear();
            float num = UI.screenWidth - 147;
            float num2 = (float)(UI.screenHeight - 35) - GizmoSpacing.y - 75f;
            if (SteamDeck.IsSteamDeck && SteamDeck.KeyboardShowing && Find.MainTabsRoot.OpenTab == MainButtonDefOf.Architect && ((MainTabWindow_Architect)MainButtonDefOf.Architect.TabWindow).QuickSearchWidgetFocused)
            {
                num2 -= 335f;
            }
            Vector2 vector = new Vector2(startX, num2);

            int num3 = 0;
            ReorderGroup(startX, maxWidth, vector, 0, tmpAllGizmos.ToList(), architectCategoryTab);

            for (int k = 0; k < gizmoGroups.Count; k++)
            {
                List<Gizmo> list2 = gizmoGroups[k];
                Gizmo gizmo2 = null;
                for (int l = 0; l < list2.Count; l++)
                {
                    if (!list2[l].Disabled)
                    {
                        gizmo2 = list2[l];
                        break;
                    }
                }
                if (gizmo2 == null)
                {
                    gizmo2 = list2.FirstOrDefault();
                }
                else if (gizmo2 is Command_Toggle command_Toggle)
                {
                    if (!command_Toggle.activateIfAmbiguous && !command_Toggle.isActive())
                    {
                        for (int m = 0; m < list2.Count; m++)
                        {
                            if (list2[m] is Command_Toggle { Disabled: false } command_Toggle2 && command_Toggle2.isActive())
                            {
                                gizmo2 = list2[m];
                                break;
                            }
                        }
                    }
                    if (command_Toggle.activateIfAmbiguous && command_Toggle.isActive())
                    {
                        for (int n = 0; n < list2.Count; n++)
                        {
                            if (list2[n] is Command_Toggle { Disabled: false } command_Toggle3 && !command_Toggle3.isActive())
                            {
                                gizmo2 = list2[n];
                                break;
                            }
                        }
                    }
                }


                if (gizmo2 != null)
                {
                    if (gizmo2 is Command_Ability command_Ability)
                    {
                        command_Ability.GroupAbilityCommands(list2);
                    }
                    if (gizmo2 is Command { shrinkable: not false, Visible: not false } command)
                    {
                        shrinkableCommands.Add(command);
                    }
                    if (vector.x + gizmo2.GetWidth(maxWidth) > num)
                    {
                        vector.x = startX;
                        vector.y -= 75f + GizmoSpacing.y;
                        num3++;
                    }

                    vector.x += gizmo2.GetWidth(maxWidth) + GizmoSpacing.x;
                    firstGizmos.Add(gizmo2);
                }
            }


            if (num3 > 1 && shrinkableCommands.Count > 1)
            {
                for (int num4 = 0; num4 < shrinkableCommands.Count; num4++)
                {
                    firstGizmos.Remove(shrinkableCommands[num4]);
                }
            }
            else
            {
                shrinkableCommands.Clear();
            }


            drawnHotKeys.Clear();
            customActivator = customActivatorFunc;
            Text.Font = GameFont.Tiny;
            Vector2 vector2 = new Vector2(startX, num2);
            mouseoverGizmo = null;
            Gizmo interactedGiz = null;
            Event interactedEvent = null;
            Gizmo floatMenuGiz = null;
            bool isFirst = true;
            int currentGizmoIndexInRow = 0;
            for (int num5 = 0; num5 < firstGizmos.Count; num5++)
            {
                Gizmo gizmo3 = firstGizmos[num5];
                if (!gizmo3.Visible)
                {
                    continue;
                }

                if (vector2.x + gizmo3.GetWidth(maxWidth) > num)
                {
                    vector2.x = startX;
                    vector2.y -= 75f + GizmoSpacing.y;
                    currentGizmoIndexInRow = 0;
                }
                heightDrawnFrame = Time.frameCount;
                heightDrawn = (float)UI.screenHeight - vector2.y;
                bool multipleSelected2 = false;
                for (int num6 = 0; num6 < firstGizmos.Count; num6++)
                {
                    if (num5 != num6 && firstGizmos[num5].ShowPawnDetailsWith(firstGizmos[num5]))
                    {
                        multipleSelected2 = true;
                        break;
                    }
                }


                Rect currentGizmoRect = new Rect(vector2.x, vector2.y, gizmo3.GetWidth(maxWidth), 75f);

                bool reordering = false;
                reordering = ReorderableWidget.Reorderable(rowReorderableId, currentGizmoRect, useRightButton: true);
                if (reordering)
                {
                    currentDragginGizmo = gizmo3;
                }
                GizmoRenderParms parms = default(GizmoRenderParms);
                parms.highLight = highlightFunc?.Invoke(gizmo3) ?? false;
                parms.lowLight = lowlightFunc?.Invoke(gizmo3) ?? false;
                parms.isFirst = isFirst;
                parms.multipleSelected = multipleSelected2;
                GizmoResult result2 = gizmo3.GizmoOnGUI(vector2, maxWidth, parms);
                ProcessGizmoState(gizmo3, result2, ref mouseoverGizmo);
                isFirst = false;
                GenUI.AbsorbClicksInRect(new Rect(vector2.x - 12f, vector2.y, gizmo3.GetWidth(maxWidth) + 12f, 75f + GizmoSpacing.y));
                vector2.x += gizmo3.GetWidth(maxWidth) + GizmoSpacing.x;
                currentGizmoIndexInRow++;
            }

            if (currentDragginGizmo != null && currentDragginGizmo is Command commandDragging)
            {
                currentDragginGizmo.GizmoOnGUI(new Vector2(UI.MousePositionOnUI.x, UI.screenHeight - UI.MousePositionOnUI.y), maxWidth, default);
            }
            currentDragginGizmo = null;

            float x = vector2.x;
            int num7 = 0;
            for (int num8 = 0; num8 < shrinkableCommands.Count; num8++)
            {
                Command command2 = shrinkableCommands[num8];
                float getShrunkSize = command2.GetShrunkSize;
                if (vector2.x + getShrunkSize > num)
                {
                    num7++;
                    if (num7 > 1)
                    {
                        x = startX;
                    }
                    vector2.x = x;
                    vector2.y -= getShrunkSize + 3f;
                }
                Vector2 topLeft = vector2;
                topLeft.y += getShrunkSize + 3f;
                heightDrawnFrame = Time.frameCount;
                heightDrawn = Mathf.Min(heightDrawn, (float)UI.screenHeight - topLeft.y);
                bool multipleSelected3 = false;
                for (int num9 = 0; num9 < shrinkableCommands.Count; num9++)
                {
                    if (num8 != num9 && shrinkableCommands[num8].ShowPawnDetailsWith(shrinkableCommands[num9]))
                    {
                        multipleSelected3 = true;
                        break;
                    }
                }
                GizmoRenderParms parms2 = default(GizmoRenderParms);
                parms2.highLight = highlightFunc?.Invoke(command2) ?? false;
                parms2.lowLight = lowlightFunc?.Invoke(command2) ?? false;
                parms2.isFirst = isFirst;
                parms2.multipleSelected = multipleSelected3;
                GizmoResult result3 = command2.GizmoOnGUIShrunk(topLeft, getShrunkSize, parms2);
                ProcessGizmoState(command2, result3, ref mouseoverGizmo);
                isFirst = false;
                GenUI.AbsorbClicksInRect(new Rect(topLeft.x - 3f, topLeft.y, getShrunkSize + 3f, getShrunkSize + 3f));
                vector2.x += getShrunkSize + 3f;
            }
            if (interactedGiz != null)
            {
                List<Gizmo> list3 = FindMatchingGroup(interactedGiz);
                for (int num10 = 0; num10 < list3.Count; num10++)
                {
                    Gizmo gizmo4 = list3[num10];
                    if (gizmo4 != interactedGiz && !gizmo4.Disabled && interactedGiz.InheritInteractionsFrom(gizmo4))
                    {
                        gizmo4.ProcessInput(interactedEvent);
                    }
                }
                interactedGiz.ProcessInput(interactedEvent);
                interactedGiz.ProcessGroupInput(interactedEvent, list3);
                Event.current.Use();
            }
            else if (floatMenuGiz != null)
            {
                List<FloatMenuOption> list4 = new List<FloatMenuOption>();
                foreach (FloatMenuOption rightClickFloatMenuOption in floatMenuGiz.RightClickFloatMenuOptions)
                {
                    list4.Add(rightClickFloatMenuOption);
                }
                List<Gizmo> list5 = FindMatchingGroup(floatMenuGiz);
                for (int num11 = 0; num11 < list5.Count; num11++)
                {
                    Gizmo gizmo5 = list5[num11];
                    if (gizmo5 == floatMenuGiz || gizmo5.Disabled || !floatMenuGiz.InheritFloatMenuInteractionsFrom(gizmo5))
                    {
                        continue;
                    }
                    foreach (FloatMenuOption rightClickFloatMenuOption2 in gizmo5.RightClickFloatMenuOptions)
                    {
                        FloatMenuOption floatMenuOption = null;
                        for (int num12 = 0; num12 < list4.Count; num12++)
                        {
                            if (list4[num12].Label == rightClickFloatMenuOption2.Label)
                            {
                                floatMenuOption = list4[num12];
                                break;
                            }
                        }
                        if (floatMenuOption == null)
                        {
                            list4.Add(rightClickFloatMenuOption2);
                        }
                        else
                        {
                            if (rightClickFloatMenuOption2.Disabled)
                            {
                                continue;
                            }
                            if (!floatMenuOption.Disabled)
                            {
                                Action prevAction = floatMenuOption.action;
                                Action localOptionAction = rightClickFloatMenuOption2.action;
                                floatMenuOption.action = delegate
                                {
                                    prevAction();
                                    localOptionAction();
                                };
                            }
                            else if (floatMenuOption.Disabled)
                            {
                                list4[list4.IndexOf(floatMenuOption)] = rightClickFloatMenuOption2;
                            }
                        }
                    }
                }
                Event.current.Use();
                if (list4.Any())
                {
                    Find.WindowStack.Add(new FloatMenu(list4));
                }
            }
            for (int num13 = 0; num13 < gizmoGroups.Count; num13++)
            {
                gizmoGroups[num13].Clear();
                SimplePool<List<Gizmo>>.Return(gizmoGroups[num13]);
            }
            gizmoGroups.Clear();
            firstGizmos.Clear();
            tmpAllGizmos.Clear();
            static List<Gizmo> FindMatchingGroup(Gizmo toMatch)
            {
                for (int num14 = 0; num14 < gizmoGroups.Count; num14++)
                {
                    if (gizmoGroups[num14].Contains(toMatch))
                    {
                        return gizmoGroups[num14];
                    }
                }
                return null;
            }
            void ProcessGizmoState(Gizmo giz, GizmoResult result, ref Gizmo mouseoverGiz)
            {
                if (result.State == GizmoState.Interacted || (result.State == GizmoState.OpenedFloatMenu && giz.RightClickFloatMenuOptions.FirstOrDefault() == null))
                {
                    interactedEvent = result.InteractEvent;
                    interactedGiz = giz;
                }
                else if (result.State == GizmoState.OpenedFloatMenu)
                {
                    floatMenuGiz = giz;
                }
                if ((int)result.State >= 1)
                {
                    mouseoverGiz = giz;
                }
            }
        }

        private static void ReorderGroup(float startX, float maxWidth, Vector2 vector, int gizmosProcessed, List<Gizmo> gizmos, ArchitectCategoryTab architectCategoryTab)
        {
            if (Event.current.type == EventType.Repaint)
            {
                Rect gizmoGridRowRect = new Rect(startX, vector.y, maxWidth, 75f + GizmoSpacing.y);
                var gizmosProcessedForGroup = gizmosProcessed;
                rowReorderableId = ReorderableWidget.NewGroup(delegate (int from, int to)
                {
                    ReorderDesignators(gizmos, from, to, architectCategoryTab);
                }, ReorderableDirection.Horizontal, gizmoGridRowRect, GizmoSpacing.x);
            }
        }

        private static void ReorderDesignators(List<Gizmo> gizmos, int from, int to, ArchitectCategoryTab architectCategoryTab)
        {
            GizmoOffsets list = GetGizmoOffsetList(architectCategoryTab);
            if (list is null) return;

            if (from < 0 || from >= gizmos.Count || to < 0 || to > gizmos.Count)
            {
                Log.Error($"Invalid drag index: {from} -> {to} - gizmos: {gizmos.Count}");
                return;
            }

            // Get the dragged gizmo
            Gizmo draggedGizmo = gizmos[from];

            if (draggedGizmo == null)
            {
                Log.Error($"Could not find gizmo for visual index {from}");
                return;
            }
            if (architectCategoryTab?.def != null && draggedGizmo is Designator designator)
            {
                DesignationCategoryDef oldCategoryDef = architectCategoryTab.def;
                DesignationCategoryDef newCategoryDef = null;
                foreach (var pair in MainTabWindow_Architect_DoWindowContents_Patch.categoryRects)
                {
                    if (pair.Key.Contains(UI.MousePositionOnUIInverted))
                    {
                        newCategoryDef = pair.Value.def;
                        break;
                    }
                }

                if (newCategoryDef != null && newCategoryDef != oldCategoryDef)
                {
                    // Remove from old category
                    if (DragSortingMod.settings.movedDesignators.TryGetValue(oldCategoryDef, out var movedDesignatorOffsetsOld))
                    {
                        movedDesignatorOffsetsOld.gizmoOffsets.RemoveAll(x => x.Matches(designator));
                    }
                    // Add to new category
                    if (!DragSortingMod.settings.movedDesignators.TryGetValue(newCategoryDef, out var movedDesignatorOffsetsNew))
                    {
                        movedDesignatorOffsetsNew = new GizmoOffsets();
                        DragSortingMod.settings.movedDesignators[newCategoryDef] = movedDesignatorOffsetsNew;
                    }
                    movedDesignatorOffsetsNew.gizmoOffsets.Add(CreateGizmoOffset(designator)); // create GizmoOffset and add
                    foreach (var value in DragSortingMod.settings.reorderedDesignators)
                    {
                        value.Value.gizmoOffsets.RemoveAll(x => x.Matches(designator));
                    }
                    DragSortingMod.settings.Write();
                    if (MainButtonDefOf.Architect.TabWindow is MainTabWindow_Architect architectTab)
                    {
                        architectTab.CacheDesPanels(); // Refresh UI
                        architectTab.CacheSearchState();
                    }
                    return; // Stop reordering within the same category
                }
            }

            // Adjust target position similar to ReorderCategories
            if (to > from)
            {
                to--;
            }

            // Remove and insert at new position
            gizmos.RemoveAt(from);
            gizmos.Insert(to, draggedGizmo);

            // Update all gizmo offsets based on their new positions
            list.gizmoOffsets = gizmos.Select((gizmo, index) =>
            {
                var existingOffset = list.gizmoOffsets.FirstOrDefault(x => x.Matches(gizmo));

                if (existingOffset == null)
                {
                    GizmoOffset newOffset = CreateGizmoOffset(gizmo);
                    newOffset.order = index;
                    return newOffset;
                }
                else
                {
                    existingOffset.order = index;
                    return existingOffset;
                }
            }).ToList();

            // Save settings
            DragSortingMod.settings.Write();
        }

        private static GizmoOffset CreateGizmoOffset(Gizmo gizmo)
        {
            var newOffset = new GizmoOffset();
            if (gizmo is Command command)
            {
                newOffset.defaultLabel = command.defaultLabel;
                newOffset.defaultDesc = command.defaultDesc;
                newOffset.iconTexPath = command.icon?.name;
            }
            else
            {
                newOffset.type = gizmo.GetType().FullName;
            }

            return newOffset;
        }

        private static GizmoOffsets GetGizmoOffsetList(ArchitectCategoryTab architectInstance)
        {
            var list = new GizmoOffsets();
            if (architectInstance is ArchitectCategoryTab architectCategoryTab)
            {
                if (!DragSortingMod.settings.reorderedDesignators.TryGetValue(architectCategoryTab.def, out list))
                {
                    DragSortingMod.settings.reorderedDesignators[architectCategoryTab.def] = list = new GizmoOffsets
                    {
                        gizmoOffsets = new List<GizmoOffset>()
                    };
                }
            }
            else if (Find.UIRoot is UIRoot_Play)
            {
                var instance = Find.Selector.SingleSelectedThing;
                if (instance != null)
                {
                    list = instance.GetReorderedGizmos();
                }
                else
                {
                    return null;
                }
            }
            return list;
        }

        public static string ToName(this Gizmo gizmo)
        {
            if (gizmo is Designator designator)
            {
                return designator.LabelCap;
            }
            return gizmo.ToString();
        }
        private static List<Gizmo> SortByOffset(List<Gizmo> gizmos, ArchitectCategoryTab architectCategoryTab)
        {
            GizmoOffsets reorderedList = GetGizmoOffsetList(architectCategoryTab);
            if (reorderedList != null)
            {
                var gizmoCopy = gizmos.ToList();
                var gizmosSortered = gizmos.Where(x => x.Visible).ToList();
                var gizmosToAdd = new List<Gizmo>();
                foreach (var designator in reorderedList.gizmoOffsets)
                {
                    var gizmo = gizmosSortered.FirstOrDefault(x => designator.Matches(x));
                    if (gizmo != null)
                    {
                        var index = gizmosSortered.IndexOf(gizmo);
                        var newIndex = designator.order;
                        if (newIndex < 0) newIndex = 0;
                        else if (newIndex >= gizmosSortered.Count) newIndex = gizmosSortered.Count - 1;
                        gizmosSortered.RemoveAt(index);
                        gizmosSortered.Insert(newIndex, gizmo);
                    }
                    else
                    {
                        gizmosSortered.Remove(gizmo);
                    }
                }
                foreach (var gizmo in gizmosToAdd)
                {
                    gizmosSortered.Add(gizmo);
                }
                return gizmosSortered;
            }
            return gizmos;
        }
    }
}