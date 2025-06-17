using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace MonoFSM.Core
{
    [DrawerPriority(0, 1, 0.25)]
    public class GameStateAttributeDrawer : OdinAttributeDrawer<GameStateAttribute>
    {
        private bool isPrefabKindMatch => PrefabKindMatchTagCheck(Property);

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var propertyValue = Property.ValueEntry.WeakSmartValue as Object;
            if (propertyValue != null)
            {
                //TODO: 重新幫gameState命名？
                CallNextDrawer(label);
                return;
            }

            
            // SirenixEditorGUI.BeginBox();
            // SirenixEditorGUI.BeginInlineBox();
            // Rect rect = EditorGUILayout.GetControlRect();
            //[]: 判定是不是有auto
           

            // EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.BeginVertical();
            this.CallNextDrawer(label);
            // EditorGUILayout.EndVertical();

            var guiContent = new GUIContent("Create GameState");
            // var buttonRect = new Rect(rect.x + rect.width - 100, rect.y, 100, rect.height);
            // if (SirenixEditorGUI.SDFIconButton(
            //         EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight,
            //             GUILayout.MaxWidth(
            //                 SirenixEditorGUI.CalculateMinimumSDFIconButtonWidth(guiContent,
            //                     EditorGUIUtility.singleLineHeight))), guiContent, SdfIconType.SdCard,
            //         IconAlignment.LeftEdge))


            if (parentComp.GetComponent<AutoGenGameState>() == null)
            {
                var buttonClicked = SirenixEditorGUI.SDFIconButton(
                    EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight),
                    guiContent,
                    SdfIconType.SdCard,
                    IconAlignment.LeftEdge);
                if (buttonClicked)
                {
                    // ScriptableObject.CreateInstance<> 
                    //finalName = sceneName+Position+flagName

                    //TODO: Scene要用folder裝起來嗎？

                    //FIXME:還需要手動按嗎？ AutoFix不就把事情做掉了 只是還要再寫autoFix那邊不是很多餘...

                    var gameStateSo =
                        Property.ValueEntry.TypeOfValue.CreateGameStateSO(parentComp, Attribute.SubFolderName);

                    Property.ValueEntry.WeakSmartValue = gameStateSo;

                    //PostProcess

                    if (parentComp is IDataOwner flagOwner) flagOwner.FlagGeneratedPostProcess(gameStateSo);
                }
            }
            else
            {
                if (isPrefabKindMatch && propertyValue == null)
                    SirenixEditorGUI.ErrorMessageBox("(AutoSave)存檔以生成GameState");
            }
          

            // EditorGUILayout.EndHorizontal();
            if (HasGameStateRequireAtPrefabKind())
                return;
            if (SirenixEditorGUI.SDFIconButton("PrefabKindMatchTagCheck", 20, SdfIconType.Alarm))
            {
                //add component to parent
                var tag = parentComp.TryGetCompOrAdd<GameStateRequireAtPrefabKind>();
            }
            //[]: auto 的button?
            if (SirenixEditorGUI.SDFIconButton("Create AutoGen Tag", 20, SdfIconType.Recycle))
            {
                //add component to parent
                var tag = parentComp.TryGetCompOrAdd<AutoGenGameState>();
            }
        }

//TODO:
//情境1:集中管理在同個directory
//情境2：放在Prefab旁邊的那種嗎？放在scene旁邊？

//TODO: 取名要用什麼方式？ 用sceneName+Position+flagName?
        private bool PrefabKindMatchTagCheck(InspectorProperty property)
        {
            var comp = property.ParentValues[0] as Component;
            if (comp == null) return false;
            var tag = comp.GetComponent<GameStateRequireAtPrefabKind>();

            //NOTE: 不該給過，要不然prefab會很吵
            if (tag == null) return false;
            if (tag.IsPrefabKindMatch) return true;
            return false; //不是那個環境就不用顯示了
        }

        private MonoBehaviour parentComp => Property.SerializationRoot.ParentValues[0] as MonoBehaviour;

        private bool HasGameStateRequireAtPrefabKind()
        {
            return parentComp.GetComponent<GameStateRequireAtPrefabKind>() != null;
        }
    }
}