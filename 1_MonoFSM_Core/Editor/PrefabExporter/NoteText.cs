using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM.Editor
{
    /// <summary>
    /// 節點／component 上的人寫備註（`_note`）抽取。
    ///
    /// 這個專案的節點名多半是自動命名（`[Action] Stamina 電力 += 2`），看得出「做什麼」
    /// 但看不出「為什麼」——為什麼只有 why 寫在 note 裡。所以每一個輸出面
    /// （hierarchy、FSM markdown、refs）都該把它帶出來，不然讀的人要對每一筆再下鑽一次。
    ///
    /// 走反射而不是 SerializedObject：折疊行要數整棵子樹的 note 數，
    /// 而 PrefabTextReader.Layered 會把同一棵樹重跑幾十次探深度，
    /// 每個 component 都 new 一份 SerializedObject 太貴。反射的 FieldInfo 依型別 cache。
    /// </summary>
    public static class NoteText
    {
        public const int DefaultMaxLength = 60;

        /// <summary>`_note` = AbstractDescriptionBehaviour / AbstractSOConfig；`note` = Note 的舊欄位。</summary>
        private static readonly string[] NoteFieldNames = { "_note", "note" };

        private static readonly Dictionary<Type, FieldInfo[]> _noteFieldsByType = new();

        private static readonly FieldInfo[] NoFields = Array.Empty<FieldInfo>();

        private const BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        /// <summary>
        /// 依 NoteFieldNames 的順序收集整條繼承鏈上的候選欄位。
        /// 一個型別可能兩個都有（`Note` 的新 `_note` 在 base、舊 `note` 在自己身上），
        /// 只挑「先命中的那一個」會在舊欄位空著時漏掉新欄位的內容，所以全收、取值時再挑非空的。
        /// </summary>
        private static FieldInfo[] NoteFields(Type type)
        {
            if (_noteFieldsByType.TryGetValue(type, out var cached)) return cached;

            List<FieldInfo> found = null;
            foreach (var name in NoteFieldNames)
                for (var t = type; t != null; t = t.BaseType)
                {
                    var f = t.GetField(name, Flags);
                    if (f == null || f.FieldType != typeof(string)) continue;
                    (found ??= new List<FieldInfo>()).Add(f);
                }

            var result = found?.ToArray() ?? NoFields;
            _noteFieldsByType[type] = result;
            return result;
        }

        private static string Raw(Object obj)
        {
            if (obj == null) return null;
            foreach (var f in NoteFields(obj.GetType()))
            {
                var text = f.GetValue(obj) as string;
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }

            return null;
        }

        /// <summary>有沒有非空的 note——只做判斷不建字串（數量統計用）。</summary>
        public static bool Has(Object obj) => !string.IsNullOrWhiteSpace(Raw(obj));

        /// <summary>component / ScriptableObject 上的 note，攤平成單行。沒有就回 ""。</summary>
        public static string Of(Object obj, int maxLength = DefaultMaxLength) =>
            Flatten(Raw(obj), maxLength);

        /// <summary>節點上第一個有 note 的 component（不標型別），給「引用目標」這種只需要一句的場合。</summary>
        public static string OfGameObject(GameObject go, int maxLength = DefaultMaxLength)
        {
            if (go == null) return "";
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                var note = Of(c, maxLength);
                if (note.Length > 0) return note;
            }

            return "";
        }

        /// <summary>
        /// 節點上所有 component 的 note 併成一個尾註。只有一則時直接印內容，
        /// 多於一則才標出是哪個 component（多 component 各自有 note 的情況少，不值得每行都掛型別）。
        /// </summary>
        public static string NodeSuffix(GameObject go, int maxLength = DefaultMaxLength)
        {
            if (go == null) return "";

            string firstNote = null, firstType = null;
            StringBuilder sb = null;
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                var note = Of(c, maxLength);
                if (note.Length == 0) continue;

                if (firstNote == null)
                {
                    firstNote = note;
                    firstType = c.GetType().Name;
                    continue;
                }

                if (sb == null)
                    sb = new StringBuilder().Append(firstType).Append(": ").Append(firstNote);
                sb.Append(" | ").Append(c.GetType().Name).Append(": ").Append(note);
            }

            if (firstNote == null) return "";
            return Suffix(sb == null ? firstNote : sb.ToString());
        }

        /// <summary>子樹（不含自己）裡有幾則 note——折疊行用來標「這裡藏了多少 why」。</summary>
        public static int CountInDescendants(GameObject go)
        {
            if (go == null) return 0;
            var count = 0;
            foreach (var c in go.GetComponentsInChildren<Component>(true))
            {
                if (c == null || c.gameObject == go) continue;
                if (Has(c)) count++;
            }

            return count;
        }

        public static string Suffix(string note) =>
            string.IsNullOrEmpty(note) ? "" : "   # " + note;

        /// <summary>換行/連續空白攤成單一空白，超長截斷。maxLength &lt;= 0 = 不截斷。</summary>
        public static string Flatten(string text, int maxLength = DefaultMaxLength)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            var sb = new StringBuilder(text.Length);
            var lastWasSpace = true; // 開頭的空白直接吃掉
            foreach (var ch in text)
            {
                if (char.IsWhiteSpace(ch))
                {
                    if (!lastWasSpace) sb.Append(' ');
                    lastWasSpace = true;
                    continue;
                }

                sb.Append(ch);
                lastWasSpace = false;
            }

            var s = sb.ToString().TrimEnd();
            if (maxLength > 0 && s.Length > maxLength) s = s.Substring(0, maxLength) + "…";
            return s;
        }
    }
}
