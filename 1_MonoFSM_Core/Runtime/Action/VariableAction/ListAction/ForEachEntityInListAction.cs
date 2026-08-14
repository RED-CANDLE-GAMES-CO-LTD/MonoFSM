using System.Reflection;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Runtime;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Variable;
using MonoFSM.Foundation;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace _1_MonoFSM_Core.Runtime.Action.VariableAction.ListAction
{
    /// <summary>
    ///     對 VarListEntity 的每個 entity 依序執行子 action（foreach 節點）。
    ///     每輪把當前 index 推進游標，子樹靠游標解析出「當前 entity」，
    ///     因此既有的 SetVarFloatConstAction / SetVarBoolAction / condition 都能原封不動重用。
    ///     接線（按 Inspector 上的「生成迭代節點」一鍵產生）：
    ///     [ForEachEntityInListAction] _list = 目標 list
    ///     ├ VarInt "cursor"（_cursorVar，留空則改用 list 自己的 CurrentIndex）
    ///     ├ VarEntity "CurrentItem"
    ///     │   ├ EntityFromListIndexProvider (_varList = 同一顆 list, _index → cursor)
    ///     │   └ VarFloat "TargetHP" (_varTag = HP) ← 走 varRef proxy 讀寫到當前 entity
    ///     └ SetVarFloatConstAction (_targetVar → TargetHP)
    ///     proxy var（上例的 TargetHP）必須是 VarEntity 的 child GameObject，不能同物件
    ///     （_parentVarEntity 是 [AutoParent(includeSelf:false)]），且底下不要再掛 value source
    ///     ——讀走 valueSource、寫走 varRef，來源會不一致。
    /// </summary>
    [QuickCreate]
    public class ForEachEntityInListAction : AbstractStateAction, IActionParent
    {
        public override string Description =>
            $"ForEach {(_list != null ? _list.name : "?")}" + (_isReverse ? " (reverse)" : "");

        [Required]
        [DropDownRef]
        [SerializeField]
        private VarListEntity _list;

        [Tooltip("迭代游標。留空則直接推進 list 自己的 CurrentIndex（跑完還原原值），" +
                 "子樹的 EntityFromListIndexProvider 用 _index = -1 取 CurrentListItem。" +
                 "list 的 CurrentIndex 在別處另有語意（例如 GrabSlotHolder 的當前 slot）時，" +
                 "指定獨立 cursor 才不會互相干擾，巢狀 foreach 也一定要各自的 cursor")]
        [DropDownRef]
        [SerializeField]
        private VarInt _cursorVar;

        [Tooltip("反向迭代。子 action 會把 entity 移出 list 時（例如 despawn）必須開，否則會漏掉元素")]
        [SerializeField]
        private bool _isReverse;

        [CompRef]
        [AutoChildren(DepthOneOnly = true)]
        private IEventReceiver[] _actions;

        //生成用的引用，runtime 不讀，留著讓按鈕知道節點已經建過
        [PreviewInInspector]
        [SerializeField]
        private VarEntity _currentItemVar;

        protected override void OnActionExecuteImplement()
        {
            if (_list == null)
            {
                Debug.LogError($"[ForEach] {name} 沒接 _list，跳過", this);
                return;
            }

            //借用 list 自己的游標時，跑完要還原，不然會蓋掉別人的選取狀態
            var originalIndex = _cursorVar == null ? _list.CurrentIndex : 0;

            if (_isReverse)
                for (var i = _list.Count - 1; i >= 0; i--)
                    ExecuteAt(i);
            else
                //每輪重讀 Count：子 action 若動到 list，正向迭代至少不會越界
                for (var i = 0; i < _list.Count; i++)
                    ExecuteAt(i);

            if (_cursorVar == null)
                _list.SetCurrentIndexTo(Mathf.Min(originalIndex, _list.Count - 1));
            else
                //收尾把游標推出界，讓子樹的 proxy 解析成 null，不留最後一顆 entity 的殘影。
                //不能用 -1，那是 EntityFromListIndexProvider 的「取 CurrentListItem」語意
                _cursorVar.SetValue(_list.Count, this);
        }

        private void ExecuteAt(int index)
        {
            if (_cursorVar != null)
                _cursorVar.SetValue(index, this);
            else
                _list.SetCurrentIndexTo(index);

            foreach (var action in _actions)
            {
                if (action == null)
                    continue;
                if (!action.IsValid)
                    continue;
                action.EventReceived();
            }
        }

#if UNITY_EDITOR
        private const string UndoName = "Generate ForEach Nodes";

        [PropertyOrder(-1)]
        [EnableIf("@this._list != null")]
        [Button("生成迭代節點（cursor + CurrentItem）", ButtonSizes.Medium)]
        private void GenerateIterationNodes()
        {
            if (_list == null)
            {
                Debug.LogError("[ForEach] 先指定 _list 再生成", this);
                return;
            }

            Undo.RecordObject(this, UndoName);

            if (_cursorVar == null)
                _cursorVar = CreateChildComponent<VarInt>(transform, "cursor");

            if (_currentItemVar == null)
            {
                _currentItemVar = CreateChildComponent<VarEntity>(transform, "CurrentItem");
                var provider = CreateChildComponent<EntityFromListIndexProvider>(
                    _currentItemVar.transform,
                    "EntityFromListIndex"
                );
                var so = new SerializedObject(provider);
                so.FindProperty("_varList").objectReferenceValue = _list;
                so.FindProperty("_index").FindPropertyRelative("_var").objectReferenceValue =
                    _cursorVar;
                so.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(this);
            Selection.activeGameObject = _currentItemVar.gameObject;
            Debug.Log(
                $"[ForEach] 已生成迭代節點。下一步在 {_currentItemVar.name} 底下建 proxy var"
                    + "（用下面的按鈕或 Alt+V），再把 action 的 targetVar 指過去",
                _currentItemVar
            );
        }

        [PropertyOrder(-1)]
        [EnableIf("@this._currentItemVar != null")]
        [Button("在 CurrentItem 底下建 proxy var", ButtonSizes.Medium)]
        [InfoBox("選一個 VariableTag，會依它的 VariableMonoType 建出對應型別的 Var（VarFloat / VarBool …），" +
                 "掛在 CurrentItem 的 child 上並設好 _varTag，之後 action 的 targetVar 指這顆就能寫到當前 entity")]
        private void AddProxyVar([SOConfig("VariableType")] VariableTag varTag)
        {
            if (_currentItemVar == null)
            {
                Debug.LogError("[ForEach] 先生成迭代節點", this);
                return;
            }

            if (varTag == null)
            {
                Debug.LogError("[ForEach] 沒選 VariableTag", this);
                return;
            }

            var varType = varTag.VariableMonoType;
            if (varType == null)
            {
                Debug.LogError($"[ForEach] {varTag.name} 沒有 VariableMonoType，無法決定要建哪種 Var", varTag);
                return;
            }

            var go = CreateChildGameObject(_currentItemVar.transform, varTag.name);
            var comp = (AbstractMonoVariable)Undo.AddComponent(go, varType);
            comp._varTag = varTag;
            InvokeRename(comp);
            EditorUtility.SetDirty(comp);

            Selection.activeGameObject = go;
            Debug.Log($"[ForEach] 建立 {varType.Name}（{varTag.name}）proxy var，指這顆就會寫到當前 entity", comp);
        }

        private static T CreateChildComponent<T>(Transform parent, string goName)
            where T : Component
        {
            var go = CreateChildGameObject(parent, goName);
            var comp = Undo.AddComponent<T>(go);
            InvokeRename(comp);
            EditorUtility.SetDirty(comp);
            return comp;
        }

        private static GameObject CreateChildGameObject(Transform parent, string goName)
        {
            var go = new GameObject(goName);
            StageUtility.PlaceGameObjectInCurrentStage(go);
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            Undo.SetTransformParent(go.transform, parent, false, UndoName);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go;
        }

        //Rename 是 protected virtual，跨型別只能反射叫，做法對齊 VarQuickCreateShortcut
        private static void InvokeRename(Component comp)
        {
            const BindingFlags bf =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (var t = comp.GetType(); t != null; t = t.BaseType)
            {
                var m = t.GetMethod("Rename", bf, null, System.Type.EmptyTypes, null);
                if (m == null)
                    continue;
                m.Invoke(comp, null);
                return;
            }
        }
#endif
    }
}
