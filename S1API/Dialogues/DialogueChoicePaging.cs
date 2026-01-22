using System;
using System.Reflection;
using HarmonyLib;
using S1API.Logging;

#if IL2CPPMELON
using Il2CppScheduleOne.Dialogue;
using Il2CppScheduleOne.UI;
using Il2CppSystem.Collections.Generic;
#elif IL2CPPBEPINEX
using ScheduleOne.Dialogue;
using ScheduleOne.UI;
using Il2CppSystem.Collections.Generic;
#elif MONOMELON || MONOBEPINEX
using ScheduleOne.Dialogue;
using ScheduleOne.UI;
using System.Collections.Generic;
#endif

namespace S1API.Dialogues
{
    /// <summary>
    /// Opt-in dialogue UI paging for cases where a node has more choices than the vanilla UI supports.
    /// </summary>
    public static class DialogueChoicePaging
    {
        private static readonly Log Logger = new Log("S1API.DialogueChoicePaging");

        private const string HarmonyId = "S1API.DialogueChoicePaging";

        private static bool _enabled;
        private static bool _patched;
        private static DialogueChoicePagingOptions _options = new DialogueChoicePagingOptions();

#if (IL2CPPMELON || IL2CPPBEPINEX)
        private static DialogueHandler? _handler;
        private static DialogueNodeData? _node;
        private static string? _text;
        private static Il2CppSystem.Collections.Generic.List<DialogueChoiceData>? _fullChoices;
#elif (MONOMELON || MONOBEPINEX)
        private static DialogueHandler? _handler;
        private static DialogueNodeData? _node;
        private static string? _text;
        private static System.Collections.Generic.List<DialogueChoiceData>? _fullChoices;
#endif

        private static int _pageIndex;
        private static int _pageCount;

        /// <summary>
        /// Whether paging is currently enabled.
        /// </summary>
        public static bool IsEnabled => _enabled;

        /// <summary>
        /// Enables dialogue choice paging with the provided options.
        /// </summary>
        public static void Enable(DialogueChoicePagingOptions options)
        {
            _options = (options ?? new DialogueChoicePagingOptions()).Normalize();
            _enabled = true;
            EnsurePatched();
        }

        /// <summary>
        /// Disables dialogue choice paging. Patches remain applied, but they become no-ops.
        /// </summary>
        public static void Disable()
        {
            _enabled = false;
            ClearPaging();
        }

        private static bool PagingActive =>
            _enabled && _fullChoices != null && _pageCount > 1;

        private static int ClampInt(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static void EnsurePatched()
        {
            if (_patched)
                return;

            try
            {
                var harmony = new HarmonyLib.Harmony(HarmonyId);
                var type = typeof(DialogueCanvas);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                var display = global::S1API.Utils.ReflectionUtils.GetMethod(type, "DisplayDialogueNode", flags);
                var choiceSelected = global::S1API.Utils.ReflectionUtils.GetMethod(type, "ChoiceSelected", flags);
                var endDialogue = global::S1API.Utils.ReflectionUtils.GetMethod(type, "EndDialogue", flags);
                var isChoiceValid = global::S1API.Utils.ReflectionUtils.GetMethod(type, "IsChoiceValid", flags);

                if (display == null || choiceSelected == null || endDialogue == null || isChoiceValid == null)
                {
                    Logger.Warning("DialogueCanvas methods not found; paging will be unavailable.");
                    return;
                }

                var displayPrefix = new HarmonyMethod(typeof(DialogueChoicePaging).GetMethod(nameof(DisplayDialogueNodePrefix), BindingFlags.Static | BindingFlags.NonPublic));
                var displayPostfix = new HarmonyMethod(typeof(DialogueChoicePaging).GetMethod(nameof(DisplayDialogueNodePostfix), BindingFlags.Static | BindingFlags.NonPublic));
                harmony.Patch(display, prefix: displayPrefix, postfix: displayPostfix);

                var choicePrefix = new HarmonyMethod(typeof(DialogueChoicePaging).GetMethod(nameof(ChoiceSelectedPrefix), BindingFlags.Static | BindingFlags.NonPublic));
                harmony.Patch(choiceSelected, prefix: choicePrefix);

                var endPostfix = new HarmonyMethod(typeof(DialogueChoicePaging).GetMethod(nameof(EndDialoguePostfix), BindingFlags.Static | BindingFlags.NonPublic));
                harmony.Patch(endDialogue, postfix: endPostfix);

                var validPrefix = new HarmonyMethod(typeof(DialogueChoicePaging).GetMethod(nameof(IsChoiceValidPrefix), BindingFlags.Static | BindingFlags.NonPublic));
                harmony.Patch(isChoiceValid, prefix: validPrefix);

                _patched = true;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to patch DialogueCanvas for paging: {ex.Message}");
            }
        }

        private static bool IsChoiceValidPrefix(int choiceIndex, ref string reason, ref bool __result)
        {
            try
            {
                if (!PagingActive) return true;
                if (_handler == null || _fullChoices == null) return true;

                if (IsMoreIndex(choiceIndex))
                {
                    reason = string.Empty;
                    __result = true;
                    return false;
                }

                int total = _fullChoices.Count;
                int pageStart = _pageIndex * _options.ChoicesPerPage;
                int remaining = Math.Max(0, total - pageStart);
                int realOnPage = Math.Min(_options.ChoicesPerPage, remaining);

                if (choiceIndex < 0 || choiceIndex >= realOnPage)
                {
                    reason = string.Empty;
                    __result = false;
                    return false;
                }

                int realIndex = pageStart + choiceIndex;
                var current = _handler.CurrentChoices;
                if (current == null || realIndex < 0 || realIndex >= current.Count)
                {
                    reason = string.Empty;
                    __result = false;
                    return false;
                }

                var data = current[realIndex];
                var label = data?.ChoiceLabel ?? string.Empty;
                if (string.IsNullOrWhiteSpace(label))
                {
                    reason = string.Empty;
                    __result = false;
                    return false;
                }

                string invalidReason;
                bool ok = _handler.CheckChoice(label, out invalidReason);
                reason = invalidReason;
                __result = ok;
                return false;
            }
            catch
            {
                return true;
            }
        }

        private static bool IsMoreIndex(int choiceIndex)
        {
            if (!PagingActive) return false;
            if (_fullChoices == null) return false;

            int total = _fullChoices.Count;
            int pageStart = _pageIndex * _options.ChoicesPerPage;
            int remaining = Math.Max(0, total - pageStart);
            int realOnPage = Math.Min(_options.ChoicesPerPage, remaining);
            int moreIndex = realOnPage;
            return choiceIndex == moreIndex;
        }

#if (IL2CPPMELON || IL2CPPBEPINEX)
        private static void DisplayDialogueNodePrefix(DialogueHandler diag, DialogueNodeData node, string dialogueText, ref Il2CppSystem.Collections.Generic.List<DialogueChoiceData> choices)
        {
            try
            {
                if (!_enabled) return;
                if (choices == null || choices.Count <= _options.MaxVisibleChoices)
                {
                    ClearPagingIfNewNode(diag, node);
                    return;
                }

                bool isNewToken = _handler != diag || _node != node;
                if (isNewToken)
                {
                    _handler = diag;
                    _node = node;
                    _text = dialogueText;
                    _pageIndex = 0;
                    _fullChoices = CopyList(choices);
                    _pageCount = ComputePageCount(_fullChoices.Count);
                }
                else
                {
                    _text = dialogueText;
                    if (_fullChoices == null || _fullChoices.Count != choices.Count)
                    {
                        _fullChoices = CopyList(choices);
                        _pageCount = ComputePageCount(_fullChoices.Count);
                        _pageIndex = ClampInt(_pageIndex, 0, Math.Max(0, _pageCount - 1));
                    }
                }

                if (!PagingActive) return;
                choices = BuildPageChoices(_fullChoices, _pageIndex, _pageCount);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Dialogue choice paging DisplayDialogueNodePrefix failed: {ex.Message}");
            }
        }

        private static void DisplayDialogueNodePostfix(DialogueHandler diag)
        {
            try
            {
                if (!PagingActive) return;
                if (_handler != diag) return;
                diag.CurrentChoices = _fullChoices!;
            }
            catch { }
        }

        private static bool ChoiceSelectedPrefix(DialogueCanvas __instance, ref int choiceIndex)
        {
            try
            {
                if (!PagingActive) return true;
                if (__instance == null) return true;
                if (_fullChoices == null || _handler == null || _node == null || _text == null) return true;

                int total = _fullChoices.Count;
                int pageStart = _pageIndex * _options.ChoicesPerPage;
                int remaining = Math.Max(0, total - pageStart);
                int realOnPage = Math.Min(_options.ChoicesPerPage, remaining);
                int moreIndex = realOnPage;

                if (choiceIndex == moreIndex)
                {
                    _pageIndex = NextPageIndex(_pageIndex, _pageCount, _options.WrapPages);
                    __instance.SkipNextRollout = true;
                    __instance.DisplayDialogueNode(_handler, _node, _text, _fullChoices);
                    return false;
                }

                if (choiceIndex < 0 || choiceIndex >= realOnPage) return true;
                choiceIndex = pageStart + choiceIndex;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Dialogue choice paging ChoiceSelectedPrefix failed: {ex.Message}");
                return true;
            }
        }

#elif (MONOMELON || MONOBEPINEX)
        private static void DisplayDialogueNodePrefix(DialogueHandler diag, DialogueNodeData node, string dialogueText, ref System.Collections.Generic.List<DialogueChoiceData> choices)
        {
            try
            {
                if (!_enabled) return;
                if (choices == null || choices.Count <= _options.MaxVisibleChoices)
                {
                    ClearPagingIfNewNode(diag, node);
                    return;
                }

                bool isNewToken = _handler != diag || _node != node;
                if (isNewToken)
                {
                    _handler = diag;
                    _node = node;
                    _text = dialogueText;
                    _pageIndex = 0;
                    _fullChoices = new System.Collections.Generic.List<DialogueChoiceData>(choices);
                    _pageCount = ComputePageCount(_fullChoices.Count);
                }
                else
                {
                    _text = dialogueText;
                    if (_fullChoices == null || _fullChoices.Count != choices.Count)
                    {
                        _fullChoices = new System.Collections.Generic.List<DialogueChoiceData>(choices);
                        _pageCount = ComputePageCount(_fullChoices.Count);
                        _pageIndex = ClampInt(_pageIndex, 0, Math.Max(0, _pageCount - 1));
                    }
                }

                if (!PagingActive) return;
                choices = BuildPageChoices(_fullChoices, _pageIndex, _pageCount);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Dialogue choice paging DisplayDialogueNodePrefix failed: {ex.Message}");
            }
        }

        private static void DisplayDialogueNodePostfix(DialogueHandler diag)
        {
            try
            {
                if (!PagingActive) return;
                if (_handler != diag) return;
                diag.CurrentChoices = _fullChoices!;
            }
            catch { }
        }

        private static bool ChoiceSelectedPrefix(DialogueCanvas __instance, ref int choiceIndex)
        {
            try
            {
                if (!PagingActive) return true;
                if (__instance == null) return true;
                if (_fullChoices == null || _handler == null || _node == null || _text == null) return true;

                int total = _fullChoices.Count;
                int pageStart = _pageIndex * _options.ChoicesPerPage;
                int remaining = Math.Max(0, total - pageStart);
                int realOnPage = Math.Min(_options.ChoicesPerPage, remaining);
                int moreIndex = realOnPage;

                if (choiceIndex == moreIndex)
                {
                    _pageIndex = NextPageIndex(_pageIndex, _pageCount, _options.WrapPages);
                    __instance.SkipNextRollout = true;
                    __instance.DisplayDialogueNode(_handler, _node, _text, _fullChoices);
                    return false;
                }

                if (choiceIndex < 0 || choiceIndex >= realOnPage) return true;
                choiceIndex = pageStart + choiceIndex;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Dialogue choice paging ChoiceSelectedPrefix failed: {ex.Message}");
                return true;
            }
        }

#endif

        private static int NextPageIndex(int currentPageIndex, int pageCount, bool wrap)
        {
            if (pageCount <= 1) return 0;
            int next = currentPageIndex + 1;
            if (next < pageCount) return next;
            return wrap ? 0 : currentPageIndex;
        }

        private static void EndDialoguePostfix()
        {
            ClearPaging();
        }

        private static void ClearPagingIfNewNode(DialogueHandler diag, DialogueNodeData node)
        {
            if (_handler != diag || _node != node) ClearPaging();
        }

        private static void ClearPaging()
        {
            _handler = null;
            _node = null;
            _text = null;
            _fullChoices = null;
            _pageIndex = 0;
            _pageCount = 0;
        }

        private static int ComputePageCount(int totalChoices)
        {
            if (totalChoices <= _options.MaxVisibleChoices) return 0;
            return (totalChoices + _options.ChoicesPerPage - 1) / _options.ChoicesPerPage;
        }

#if (IL2CPPMELON || IL2CPPBEPINEX)
        private static Il2CppSystem.Collections.Generic.List<DialogueChoiceData> CopyList(Il2CppSystem.Collections.Generic.List<DialogueChoiceData> src)
        {
            var dst = new Il2CppSystem.Collections.Generic.List<DialogueChoiceData>();
            if (src == null) return dst;
            for (int i = 0; i < src.Count; i++)
            {
                var item = src[i];
                if (item != null) dst.Add(item);
            }
            return dst;
        }

        private static Il2CppSystem.Collections.Generic.List<DialogueChoiceData> BuildPageChoices(Il2CppSystem.Collections.Generic.List<DialogueChoiceData> full, int pageIndex, int pageCount)
        {
            var page = new Il2CppSystem.Collections.Generic.List<DialogueChoiceData>();
            int total = full.Count;
            int start = pageIndex * _options.ChoicesPerPage;
            int take = Math.Min(_options.ChoicesPerPage, Math.Max(0, total - start));
            for (int i = 0; i < take; i++)
            {
                var c = full[start + i];
                if (c != null) page.Add(c);
            }

            string borrowedLabel = string.Empty;
            try
            {
                int nextPageIndex = NextPageIndex(pageIndex, pageCount, _options.WrapPages);
                int nextStart = nextPageIndex * _options.ChoicesPerPage;
                if (nextStart >= 0 && nextStart < full.Count)
                {
                    var next = full[nextStart];
                    if (next != null && !string.IsNullOrWhiteSpace(next.ChoiceLabel))
                        borrowedLabel = next.ChoiceLabel;
                }
            }
            catch { }

            string moreText;
            try
            {
                moreText = string.Format(_options.MoreTextFormat, pageIndex + 1, pageCount);
            }
            catch
            {
                moreText = $"More ({pageIndex + 1}/{pageCount})";
            }

            page.Add(new DialogueChoiceData
            {
                ChoiceLabel = borrowedLabel,
                ChoiceText = moreText,
            });

            return page;
        }
#elif (MONOMELON || MONOBEPINEX)
        private static System.Collections.Generic.List<DialogueChoiceData> BuildPageChoices(System.Collections.Generic.List<DialogueChoiceData> full, int pageIndex, int pageCount)
        {
            var page = new System.Collections.Generic.List<DialogueChoiceData>();
            int total = full.Count;
            int start = pageIndex * _options.ChoicesPerPage;
            int take = Math.Min(_options.ChoicesPerPage, Math.Max(0, total - start));
            for (int i = 0; i < take; i++)
            {
                var c = full[start + i];
                if (c != null) page.Add(c);
            }

            string borrowedLabel = string.Empty;
            try
            {
                int nextPageIndex = NextPageIndex(pageIndex, pageCount, _options.WrapPages);
                int nextStart = nextPageIndex * _options.ChoicesPerPage;
                if (nextStart >= 0 && nextStart < full.Count)
                {
                    var next = full[nextStart];
                    if (next != null && !string.IsNullOrWhiteSpace(next.ChoiceLabel))
                        borrowedLabel = next.ChoiceLabel;
                }
            }
            catch { }

            string moreText;
            try
            {
                moreText = string.Format(_options.MoreTextFormat, pageIndex + 1, pageCount);
            }
            catch
            {
                moreText = $"More ({pageIndex + 1}/{pageCount})";
            }

            page.Add(new DialogueChoiceData
            {
                ChoiceLabel = borrowedLabel,
                ChoiceText = moreText,
            });

            return page;
        }
#endif
    }
}
