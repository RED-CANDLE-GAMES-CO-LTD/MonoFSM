using System.Collections.Generic;
using System.Linq;
using System.Text;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace MonoFSM.Core.Editor
{
   public static class AnimatorRefactor
   {
#if UNITY_EDITOR
      private const string menuPath = "GameObject/赤燭RCG/Refactor 節點 #r";

      // [InitializeOnLoadMethod]
      // static void Init()
      // {
      //    EditorApplication.hierarchyChanged += HierarchyChanged;
      //    //TODO: 這個有點浪費效能，應該要特殊模式才啟動
      // }

      public static void Activate()
      {
         
         if(_isRefactoring)
            return;
         Debug.Log("Activate");
         _isRefactoring = true;
         EditorApplication.hierarchyChanged -= HierarchyChanged;
         EditorApplication.hierarchyChanged += HierarchyChanged;
      }

      private static bool _isRefactoring = false;
      // public static void DeactivateCheck()
      // {
      //    
      //    Debug.Log("DeactivateCheck");
      //    var node = GameObject.FindObjectOfType<RefactorNode>();
      //    if (node == null)
      //    {
      //       Debug.Log("Deactivated");
      //       EditorApplication.hierarchyChanged -= HierarchyChanged;
      //    }
      // }
      static void HierarchyChanged()
      { 
         RefactorNode[] nodes = null;
         if (PrefabStageUtility.GetCurrentPrefabStage())
         {
            //TODO: 還可以更好，不用每次都取得所有的RefactorNode，進入PrefabStage時才要
            nodes = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot
               .GetComponentsInChildren<RefactorNode>(true);
            Debug.Log("Find all RefactorNode in Prefab" + nodes.Length);
         }
         else
         {
            nodes = Object.FindObjectsByType<RefactorNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log("Find all RefactorNode in Scene" + nodes.Length);
         }
      
        
         if (nodes == null || nodes.Length == 0)
         {
            Debug.Log("AnimatorRefactor Deactivated");
            EditorApplication.hierarchyChanged -= HierarchyChanged;
            _isRefactoring = false;
            return;
         }
         foreach (var node in nodes)
         {
            if (node.gameObject.name != node.currentName)
            {
               Debug.Log("GameObject was renamed from " + node.currentName + " to " + node.gameObject.name);
               
               var oldPath = GetRelativePathWithOldName(node.gameObject, node.currentName);
               var newPath = GetRelativePath(node.gameObject);
               
               RefactorClips(node.gameObject, oldPath, newPath);

               node.currentName = node.gameObject.name;
            }
         }
      }
      
      public static string GetRelativePathWithOldName(GameObject go,string oldName)
      {
         var animator = go.GetComponentInParent<Animator>();
         var path = oldName;
         var parent = go.transform.parent;
         while (parent != null && parent.gameObject != animator.gameObject)
         {
            path = parent.name + "/" + path;
            parent = parent.parent;
         }
         return path;
      }
      
      //return the relative path of a GameObject to the parent animator controller
      public static string GetRelativePath(GameObject go)
      {
         var animator = go.GetComponentInParent<Animator>();
         var path = go.name;
         var parent = go.transform.parent;
         while (parent != null && parent.gameObject != animator.gameObject)
         {
            path = parent.name + "/" + path;
            parent = parent.parent;
         }
         return path;
      }
      //
      //
      // //return two string,  1: the relative path of a GameObject, 2: the path of newName, to the parent animator controller
      // private static (string, string) GetRelativePath(GameObject go, string newName)
      // {
      //    var animator = go.GetComponentInParent<Animator>();
      //    var path = go.name;
      //    var parent = go.transform.parent;
      //    while (parent != null && parent.gameObject != animator.gameObject)
      //    {
      //       path = parent.name + "/" + path;
      //       parent = parent.parent;
      //    }
      //    var newPath = newName;
      //    parent = go.transform.parent;
      //    while (parent != null && parent.gameObject != animator.gameObject)
      //    {
      //       newPath = parent.name + "/" + newPath;
      //       parent = parent.parent;
      //    }
      //    return (path, newPath);
      // }
      
      private static (string, string) GetRelativePath(GameObject go, string newName)
      {
         var animator = go.GetComponentInParent<Animator>();
         var pathBuilder = new StringBuilder(go.name);
         var newPathBuilder = new StringBuilder(newName);
         var parent = go.transform.parent;
         while (parent != null && parent.gameObject != animator.gameObject)
         {
            pathBuilder.Insert(0, parent.name + "/");
            newPathBuilder.Insert(0, parent.name + "/");
            parent = parent.parent;
         }
         return (pathBuilder.ToString(), newPathBuilder.ToString());
      }



      //check if the selection is valid
      [MenuItem(menuPath, true)]
      private static bool RenameNodeCheck()
      {
         if (Selection.activeGameObject == null)
            return false;

         var animator = Selection.activeGameObject.GetComponentInParent<Animator>();  
         if (animator == null)
            return false;
         
         if (animator.runtimeAnimatorController == null)
            return false;
         
         selectingNode = Selection.activeGameObject;
         return true;
      }

      private static GameObject selectingNode;

      // private static string savedLastNewName = "GameObject"; 
      [MenuItem(menuPath)]
      static void RenameNode()
      {
         if (selectingNode.TryGetComponent<RefactorNode>(out var node))
         {
            GameObject.DestroyImmediate(node);
         }
         else Undo.AddComponent<RefactorNode>(selectingNode);
         // selectingNode.AddComponent<RefactorNode>();
         // var newName = savedLastNewName;
         // var (oldPath, newPath) = GetRelativePath(selectingNode, newName);
         //
         // if (selectingNode == null)
         //    return;
         //
         // RefactorClips(selectingNode,oldPath, newPath);
         // Undo.RecordObject(selectingNode, "Rename Node");
         // selectingNode.name = newName;
      }

      
      
      
     
      //把Animator的Default State的key貼到所有的GameObject上
      [MenuItem("CONTEXT/Animator/Paste Default State Key to GameObjects")]
      public static void PasteDefaultStateToGameObject(MenuCommand menuCommand)
      {
         var animator = menuCommand.context as Animator;
         var animatorController = animator.runtimeAnimatorController;
         var cc = animatorController as AnimatorController;
         var clip = cc.layers[0].stateMachine.defaultState.motion as AnimationClip;
         // var clips = animatorController.animationClips;
         // var clip = clips[0];


         var curveBindings = AnimationUtility.GetCurveBindings(clip);
         var refCurveBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
         foreach (var curveBinding in curveBindings)
         {
            var curve = AnimationUtility.GetEditorCurve(clip, curveBinding);

            var target = animator.transform.Find(curveBinding.path).GetComponent(curveBinding.type);
            Debug.Log(curveBinding.path + " " + curveBinding.propertyName + " " + curveBinding.type + curve[0].value,
               target);
            Undo.RecordObject(target, "Paste State");
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(curveBinding.propertyName).floatValue = curve[0].value;
            serializedObject.ApplyModifiedProperties();
            Undo.FlushUndoRecordObjects();
         }

         foreach (var curveBinding in refCurveBindings)
         {
            Debug.Log(curveBinding.path);
            var curve = AnimationUtility.GetObjectReferenceCurve(clip, curveBinding);
            var target = animator.transform.Find(curveBinding.path).GetComponent(curveBinding.type);
            Undo.RecordObject(target, "Paste State");
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(curveBinding.propertyName).objectReferenceValue = curve[0].value;
            serializedObject.ApplyModifiedProperties();
            Undo.FlushUndoRecordObjects();
         }
      }
    public static void RefactorClips(GameObject gObj,string oldPath,string newPath)
      {
         var animator = gObj.GetComponentInParent<Animator>();
         
         var animatorController = animator.runtimeAnimatorController;
         if (animator.runtimeAnimatorController == null)
            return;
         //find all clips in the animator controller
         var clips = animatorController.animationClips;
         
         
         //parse all clips
         foreach (var clip in clips)
         {
            ParseClipAndRenamePath(clip, oldPath, newPath);
            //undo record for the selectingNode name, and rename
         }
      }

      // void OpenAIRename(GameObject selectingNode, string newGameObjectName)
      // {
      //      // Start an Undo record
      //      Undo.RecordObject(selectingNode, "Rename GameObject and Curve Bindings");
      //
      //      // Rename the game object
      //      selectingNode.name = newGameObjectName;
      //
      //      // Get the animator component of the game object's parent
      //      Animator animator = GetComponentInParent<Animator>();
      //
      //      // Loop through each animation clip in the animator
      //      foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
      //      {
      //          // Get the curve bindings for the clip
      //          EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);
      //
      //          // Create a list to store the modified curves
      //          List<AnimationCurve> modifiedCurves = new List<AnimationCurve>();
      //
      //          // Loop through each curve binding in the clip
      //          foreach (EditorCurveBinding curveBinding in curveBindings)
      //          {
      //              // Check if the curve binding is related to the game object
      //              if (curveBinding.path.StartsWith(selectingNode.name))
      //              {
      //                  // Get the curve from the curve binding
      //                  AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, curveBinding);
      //
      //                  // Modify the path of the curve binding to reflect the new game object name
      //                  curveBinding.path = curveBinding.path.Replace(selectingNode.name, newGameObjectName);
      //
      //                  // Set the modified curve back into the animation clip
      //                  AnimationUtility.SetEditorCurve(clip, curveBinding, curve);
      //
      //                  // Add the modified curve to the list
      //                  modifiedCurves.Add(curve);
      //              }
      //          }
      //
      //          // Apply the modified curves to the animation clip
      //          AnimationUtility.SetEditorCurve(clip, "", null);
      //
      //          foreach (AnimationCurve modifiedCurve in modifiedCurves)
      //          {
      //              AnimationUtility.SetEditorCurve(clip, "", modifiedCurve);
      //          }
      //
      //          // Record the clip as modified for Undo
      //          Undo.RegisterCompleteObjectUndo(clip, "Rename GameObject and Curve Bindings");
      //      }
      //
      //      // Record the game object as modified for Undo
      //      Undo.RecordObject(selectingNode, "Rename GameObject and Curve Bindings");
      //  }

      //parse clip, replace the specific attribute with the new name
      static void ParseClipAndRenamePath(AnimationClip clip, string oldPath, string newPath)
      {
         //record undo for the clip
         Undo.RecordObject(clip, "Rename path in Clip");
         
         var curveBindings =  AnimationUtility.GetCurveBindings(clip);
         var refCurveBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);

         //parse all curve bindings
         foreach (var curveBinding in curveBindings)
         {
            //if the path of curveBinding is under the old path, replace it with the new path
            RenameCurveBinding(clip, curveBinding, oldPath, newPath);
         }
         foreach (var curveBinding in refCurveBindings)
         {
            //if the path of curveBinding is under the old path, replace it with the new path
            RenameCurveBinding(clip, curveBinding, oldPath, newPath);
         }
      }
      
      static void RenameCurveBinding(AnimationClip clip, EditorCurveBinding curveBinding, string oldPath, string newPath)
      {
         if (curveBinding.path.StartsWith(oldPath) && (curveBinding.path.Length == oldPath.Length || curveBinding.path[oldPath.Length] == '/'))
         {
            // Debug.Log("curveBinding.path: " + curveBinding.path + " oldPath: " + oldPath + " newPath: " + newPath);
            // Debug.Log(curveBinding.propertyName);
            int index = curveBinding.path.IndexOf(oldPath);
            string newString = curveBinding.path.Substring(0, index) + newPath + curveBinding.path.Substring(index + oldPath.Length);

            var curve = AnimationUtility.GetEditorCurve(clip, curveBinding);
            ObjectReferenceKeyframe[] objectReferenceCurve = AnimationUtility.GetObjectReferenceCurve(clip, curveBinding);
            
            //remove old curve
            if (curve != null)
               AnimationUtility.SetEditorCurve(clip, curveBinding, null);
            else
               AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, null);
            
            var newCurveBinding = new EditorCurveBinding
            {
               path = newString,
               type = curveBinding.type,
               propertyName = curveBinding.propertyName
            };
            
           
            //add new curve
            if(curve != null)
               AnimationUtility.SetEditorCurve(clip, newCurveBinding, curve);
            else
               AnimationUtility.SetObjectReferenceCurve(clip, newCurveBinding, objectReferenceCurve);

         }
      }
      //enable record animation when press shift + r
      // [MenuItem("Edit/Record Animation #r")]
      // static void RecordAnimation()
      // {
      //    //Get Animation Window
      //    var animationWindow = EditorWindow.GetWindow<AnimationWindow>();
      //    animationWindow.recording = !animationWindow.recording;
      // }


      //TODO: 找有沒有PrefabVariant?
      public static IEnumerable<GameObject> FindAllPrefabVariants(string parentAssetPath)
      {
         return FindAllPrefabVariants(AssetDatabase.LoadAssetAtPath<GameObject>(parentAssetPath));
      }

      public static IEnumerable<GameObject> FindAllPrefabVariants(GameObject parent)
      {
         return AssetDatabase.FindAssets("t:prefab").Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<GameObject>).Where(go => go != null)
            .Where(go => PrefabUtility.GetPrefabAssetType(go) == PrefabAssetType.Variant)
            .Where(go => PrefabUtility.GetCorrespondingObjectFromSource(go) == parent);
      }
#endif

   }
   
   

}
