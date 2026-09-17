using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MonoFSM.Core
{
    /// <summary>
    ///     Cheat 熱鍵的 modifier 旗標。Mac 的 Cmd 一律視同 <see cref="Ctrl" />（跟原本硬寫的判定一致）。
    /// </summary>
    [Flags]
    public enum CheatModifier
    {
        None = 0,
        Ctrl = 1 << 0,
        Shift = 1 << 1,
        Alt = 1 << 2,
    }

    /// <summary>
    ///     一筆 cheat 熱鍵登錄。由各自的持有者（CheatManager / FSM condition / 網路層…）在啟用時登錄，
    ///     停用時 Unregister。Command Palette 只讀這張表，不需要知道 cheat 實際住在哪。
    /// </summary>
    public class CheatEntry
    {
        public Key _key;

        /// <summary>這些 modifier 必須全部按住才算觸發</summary>
        public CheatModifier _requiredModifiers;

        /// <summary>這些 modifier 只要按住任一個就不觸發（用來還原原本 if/else 的互斥關係）</summary>
        public CheatModifier _forbiddenModifiers;

        public string _description;

        /// <summary>描述會隨場上狀態改變時用這個（例如傳送點名稱）。只在 Inspector / Palette 需要顯示時呼叫。</summary>
        public Func<string> _descriptionGetter;

        public string _source;
        public Action _invoke;

        /// <summary>按住型（例如按住 0 加速）：Palette 只顯示不觸發</summary>
        public bool _isHold;

        /// <summary>共用鍵（例如多個 prefab 的 FSM condition 各自聽同一顆鍵）：彼此之間不算熱鍵衝突</summary>
        public bool _isSharedKey;

        /// <summary>
        ///     登錄者所在的物件（通常是 gameObject），給 Editor 端「跳到節點」用（Selection + Ping）。
        ///     可為 null（純 static 的 cheat）。runtime 只存引用，不碰 Editor API。
        /// </summary>
        public UnityEngine.Object _owner;

        public string Description =>
            _descriptionGetter != null ? _descriptionGetter() : _description;

        public bool CanInvoke => !_isHold && _invoke != null;

        public string ShortcutText => CheatRegistry.FormatShortcut(_key, _requiredModifiers);
    }

    /// <summary>
    ///     執行期 cheat 熱鍵的總表。目的是讓「有哪些 cheat、按什麼鍵、誰提供的」可以被列出來
    ///     （Inspector / Command Palette 的 CHEATS 分類），而不是散在各個 Update 的 if 判斷裡。
    ///     觸發判定本身仍留在各自的持有者手上（tick 語意不同），Registry 只提供共用的 modifier 比對與 Invoke。
    /// </summary>
    public static class CheatRegistry
    {
        private static readonly List<CheatEntry> _entries = new(64);

        //Inspector / Palette 顯示用的暫存，避免每次取用都 new List
        private static readonly List<string> _debugLines = new(64);
        private static readonly List<string> _conflictLines = new(8);
        private static readonly List<CheatEntry> _conflictBuffer = new(8);
        private static readonly StringBuilder _stringBuilder = new(64);

        public static IReadOnlyList<CheatEntry> Entries => _entries;

        //Enter Play Mode 不 domain reload 時，static list 會殘留上一輪的登錄
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _entries.Clear();
        }

        public static CheatEntry Register(CheatEntry entry)
        {
            if (entry == null)
                return null;
            if (!_entries.Contains(entry))
                _entries.Add(entry);
            return entry;
        }

        public static CheatEntry Register(
            Key key,
            CheatModifier requiredModifiers,
            string description,
            string source,
            Action invoke,
            bool isHold = false,
            CheatModifier forbiddenModifiers = CheatModifier.None,
            Func<string> descriptionGetter = null,
            bool isSharedKey = false,
            UnityEngine.Object owner = null)
        {
            return Register(new CheatEntry
            {
                _key = key,
                _requiredModifiers = requiredModifiers,
                _forbiddenModifiers = forbiddenModifiers,
                _description = description,
                _descriptionGetter = descriptionGetter,
                _source = source,
                _invoke = invoke,
                _isHold = isHold,
                _isSharedKey = isSharedKey,
                _owner = owner,
            });
        }

        public static void Unregister(CheatEntry handle)
        {
            if (handle == null)
                return;
            _entries.Remove(handle);
        }

        /// <summary>
        ///     目前按住的 modifier 組合。Cmd 與 Ctrl 都算 <see cref="CheatModifier.Ctrl" />。
        /// </summary>
        public static CheatModifier CurrentModifiers(Keyboard keyboard)
        {
            if (keyboard == null)
                return CheatModifier.None;

            var result = CheatModifier.None;
            if (keyboard.ctrlKey.isPressed || keyboard.leftCommandKey.isPressed ||
                keyboard.rightCommandKey.isPressed)
                result |= CheatModifier.Ctrl;
            if (keyboard.shiftKey.isPressed)
                result |= CheatModifier.Shift;
            if (keyboard.altKey.isPressed)
                result |= CheatModifier.Alt;
            return result;
        }

        public static bool IsModifierMatched(CheatEntry entry, CheatModifier current)
        {
            if (entry == null)
                return false;
            if ((current & entry._requiredModifiers) != entry._requiredModifiers)
                return false;
            return (current & entry._forbiddenModifiers) == CheatModifier.None;
        }

        /// <summary>
        ///     這一幀是否觸發（wasPressedThisFrame + modifier 比對）。按住型一律回 false，由持有者自己判。
        /// </summary>
        public static bool IsTriggered(CheatEntry entry, Keyboard keyboard)
        {
            if (entry == null || keyboard == null || entry._isHold || entry._key <= 0)
                return false;
            if (!IsModifierMatched(entry, CurrentModifiers(keyboard)))
                return false;
            return keyboard[entry._key].wasPressedThisFrame;
        }

        public static void Invoke(CheatEntry entry)
        {
            if (entry == null)
                return;
            if (entry._isHold)
            {
                Debug.LogWarning($"[CheatRegistry] {entry.Description} 是按住型，無法一次性觸發");
                return;
            }

            if (entry._invoke == null)
            {
                Debug.LogWarning($"[CheatRegistry] {entry.Description} 沒有 invoke，無法觸發");
                return;
            }

            entry._invoke();
        }

        /// <summary>
        ///     同一組 key + modifier 被登錄超過一筆時填進 results（呼叫端自備 list，不產生 GC）。
        /// </summary>
        public static void CollectConflicts(List<CheatEntry> results)
        {
            results.Clear();
            for (var i = 0; i < _entries.Count; i++)
            {
                var a = _entries[i];
                for (var j = i + 1; j < _entries.Count; j++)
                {
                    var b = _entries[j];
                    if (a._key != b._key || a._requiredModifiers != b._requiredModifiers)
                        continue;

                    //同一個 key 被多個 FSM condition 共用是正常現象（不同 prefab 各自聽同一顆鍵）
                    if (a._isSharedKey && b._isSharedKey)
                        continue;

                    if (!results.Contains(a))
                        results.Add(a);
                    if (!results.Contains(b))
                        results.Add(b);
                }
            }
        }

        public static string FormatShortcut(Key key, CheatModifier modifiers)
        {
            _stringBuilder.Clear();
            if ((modifiers & CheatModifier.Ctrl) != 0)
                _stringBuilder.Append('⌘');
            if ((modifiers & CheatModifier.Shift) != 0)
                _stringBuilder.Append('⇧');
            if ((modifiers & CheatModifier.Alt) != 0)
                _stringBuilder.Append('⌥');
            _stringBuilder.Append(key.ToString());
            return _stringBuilder.ToString();
        }

        /// <summary>
        ///     Inspector 顯示用：每筆一行「熱鍵 | 描述 | 來源」。只在 Inspector 重繪時呼叫。
        /// </summary>
        public static List<string> GetDebugLines()
        {
            _debugLines.Clear();
            for (var i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                _debugLines.Add(
                    $"{e.ShortcutText}{(e._isHold ? "(按住)" : "")} | {e.Description} | {e._source}");
            }

            return _debugLines;
        }

        /// <summary>
        ///     Inspector 顯示用：熱鍵撞在一起的條目。只在 Inspector 重繪時呼叫。
        /// </summary>
        public static List<string> GetConflictLines()
        {
            CollectConflicts(_conflictBuffer);
            _conflictLines.Clear();
            for (var i = 0; i < _conflictBuffer.Count; i++)
            {
                var e = _conflictBuffer[i];
                _conflictLines.Add($"{e.ShortcutText} | {e.Description} | {e._source}");
            }

            return _conflictLines;
        }
    }
}
