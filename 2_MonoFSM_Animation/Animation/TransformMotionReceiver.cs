using MonoFSM.EditorExtension;
using MonoFSM.Variable.Attributes;
using UnityEngine;

public class TransformMotionReceiver : MonoBehaviour, IRootMotionReceiver, IOverrideHierarchyIcon, IDrawHierarchyBackGround, IHierarchyValueInfo
{
    [Auto] private AnimatorRootMotionRelay _relay;

    public void OnProcessRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
    {
        this.transform.position += deltaPosition;
        this.transform.rotation = deltaRotation * this.transform.rotation;
    }

#if UNITY_EDITOR
    // IOverrideHierarchyIcon 實作
    public string IconName => "Transform Icon";
    public bool IsDrawingIcon => _relay == null; // 只有獨立存在時顯示圖示
    public Texture2D CustomIcon => null;
    public bool IsPosAtHead => false; // 圖示在右邊

    // IDrawHierarchyBackGround 實作
    public Color BackgroundColor => new Color(0.8f, 0.7f, 0.2f, 0.15f); // 淡黃色
    public bool IsDrawGUIHierarchyBackground => _relay == null; // 只有獨立存在時顯示背景

    // IHierarchyValueInfo 實作 - 顯示接收狀態
    public string ValueInfo => "Transform ↰";
    public bool IsDrawingValueInfo => _relay == null; // 只有獨立存在時顯示文字
#endif
}
