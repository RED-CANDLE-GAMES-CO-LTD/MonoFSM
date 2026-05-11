#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using MonoFSM.Core;
using MonoFSM.Runtime;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[UsedImplicitly]
[DrawerPriority(0.0, 2.0, 0.25)]
// Place the drawer script file in an Editor folder or wrap it in a #if UNITY_EDITOR condition.
public class DropDownRefAttributeDrawer : OdinAttributeDrawer<DropDownRefAttribute>
{
    private ValueResolver<object> rawGetterDynamicType; //動態拿到type
    private Func<Type> getterDynamicType;

    private InspectorProperty baseMemberProperty;
    private Component _bindComp;
    private bool isArray =>
        Property.ValueEntry != null ? Property.ValueEntry.TypeOfValue.IsArray : false;

    private bool _isValueDropDownAttribute;
    private bool _isInlineEditor;

    protected override void Initialize()
    {
        rawGetterDynamicType = ValueResolver.Get<object>(Property, Attribute._dynamicTypeGetter);
        getterDynamicType = () =>
        {
            if (rawGetterDynamicType != null)
            {
                if (rawGetterDynamicType.GetValue() is Type dynamictype)
                    return dynamictype;
            }

            return Property.ValueEntry.BaseValueType;
        };

        _isValueDropDownAttribute = Property.GetAttribute<ValueDropdownAttribute>() != null;
        _isInlineEditor = Property.GetAttribute<InlineEditorAttribute>() != null;
        baseMemberProperty = Property.Parent;
        if (isArray)
        {
            _bindComp = baseMemberProperty.SerializationRoot.ParentValues[0] as Component;
        }
        else
        {
            _bindComp = Property.SerializationRoot.ParentValues[0] as Component;
        }

        if (_bindComp == null)
            throw new ArgumentNullException(nameof(Property.ParentValues));
    }

    public override bool CanDrawTypeFilter(Type type)
    {
        return true;
    }

    void ShowSelector()
    {
        //直接用property原本宣告的type來做filter
        //fixme: 可以filter某一部分？
        var filterType = Property.ValueEntry.BaseValueType;
        var dynType = getterDynamicType();
        if (dynType != null)
        {
            // Debug.Log("getterDynamicType():" + getterDynamicType());
            filterType = dynType;
        }

        if (filterType.IsArray)
        {
            filterType = filterType.GetElementType();
        }

        // Debug.Log(
        //     $"[DropDownRef] ShowSelector base={Property.ValueEntry.BaseValueType?.FullName}, dyn={dynType?.FullName}, filter={filterType?.FullName}",
        //     _bindComp
        // );

        // var currentComp = Property.ValueEntry.WeakSmartValue as Component;
        //draw SDFIcon down arrow to the right of the button
        var buttonText = _bindComp ? _bindComp.name : "None";
        if (
            SirenixEditorGUI.SDFIconButton(
                buttonText,
                16,
                SdfIconType.CaretDownFill,
                IconAlignment.RightEdge
            )
        )
        {
            var selector = new DropDownRefCompSelector(_bindComp, filterType, Attribute);
            selector.SelectionConfirmed += col =>
            {
                Property.ValueEntry.WeakSmartValue = col.FirstOrDefault();
            };

            selector.EnableSingleClickToConfirm();
            selector.ShowInPopup();
        }

        if (GUILayout.Button("+Var", GUILayout.Width(50), GUILayout.Height(18)))
        {
            CreateVarAtParentMonoEntity(filterType);
        }
    }

    private static Type[] _varTypeCache;

    private static Type[] GetAllVarTypes()
    {
        if (_varTypeCache != null)
            return _varTypeCache;

        var list = new List<Type>();
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null).ToArray();
            }

            foreach (var t in types)
            {
                if (t == null || t.IsAbstract || t.IsGenericTypeDefinition)
                    continue;
                if (!typeof(AbstractMonoVariable).IsAssignableFrom(t))
                    continue;
                list.Add(t);
            }
        }

        _varTypeCache = list.ToArray();
        return _varTypeCache;
    }

    private static Type GetVarValueType(Type varType)
    {
        var t = varType;
        while (t != null && t != typeof(object))
        {
            if (t.IsGenericType)
            {
                var def = t.GetGenericTypeDefinition();
                if (def.Name.StartsWith("GenericUnityObjectVariable")
                    || def.Name.StartsWith("TypedMonoVariable"))
                {
                    return t.GetGenericArguments()[0];
                }
            }

            t = t.BaseType;
        }

        return null;
    }

    private Type FindMatchingVarType(Type targetType)
    {
        if (targetType == null)
            return null;

        // 欄位本身就宣告為 AbstractMonoVariable 子類 → 直接用該型別
        if (typeof(AbstractMonoVariable).IsAssignableFrom(targetType)
            && !targetType.IsAbstract
            && !targetType.IsGenericTypeDefinition)
        {
            Debug.Log(
                $"[DropDownRef +Var] target itself is a Var type, use directly: {targetType.Name}",
                _bindComp
            );
            return targetType;
        }

        Type best = null;
        Type bestValueType = null;
        var candidates = new List<string>();

        foreach (var varType in GetAllVarTypes())
        {
            var valueType = GetVarValueType(varType);
            if (valueType == null)
                continue;

            // valueType 必須能裝下 targetType（targetType 是 valueType 或子類）
            if (!valueType.IsAssignableFrom(targetType))
                continue;

            candidates.Add($"{varType.Name}<{valueType.Name}>");

            // 取最具體的那個（valueType 是目前 best 的子類則更新）
            if (bestValueType == null || bestValueType.IsAssignableFrom(valueType))
            {
                best = varType;
                bestValueType = valueType;
            }
        }

        Debug.Log(
            $"[DropDownRef +Var] target={targetType.FullName}, candidates=[{string.Join(", ", candidates)}], pick={best?.Name}",
            _bindComp
        );
        return best;
    }

    private Type GetFieldDeclaredType()
    {
        // 從 baseMemberProperty (array case) 或 Property 取得實際的 FieldInfo 宣告型別
        var prop = isArray ? Property.Parent : Property;
        var memberInfo = prop?.Info?.GetMemberInfo();
        Type declared = null;
        if (memberInfo is FieldInfo fi)
            declared = fi.FieldType;
        else if (memberInfo is PropertyInfo pi)
            declared = pi.PropertyType;

        if (declared == null)
            return null;

        // 集合 → element type
        if (declared.IsArray)
            declared = declared.GetElementType();
        else if (declared.IsGenericType)
        {
            var args = declared.GetGenericArguments();
            if (args.Length == 1)
                declared = args[0];
        }

        return declared;
    }

    private void CreateVarAtParentMonoEntity(Type filterType)
    {
        if (_bindComp == null)
        {
            Debug.LogError("[DropDownRef] _bindComp is null, cannot create Var.");
            return;
        }

        var monoEntity = _bindComp.GetComponentInParent<MonoEntity>(true);
        if (monoEntity == null)
        {
            Debug.LogError(
                $"[DropDownRef] Cannot find parent MonoEntity for {_bindComp.name}",
                _bindComp
            );
            return;
        }

        var folder = monoEntity.VariableFolder;
        if (folder == null)
        {
            Debug.LogError(
                $"[DropDownRef] MonoEntity '{monoEntity.name}' has no VariableFolder",
                monoEntity
            );
            return;
        }

        // 直接用 FieldInfo 的宣告型別，避免 Odin BaseValueType 抽象化導致取到過於 base 的型別
        var declaredType = GetFieldDeclaredType() ?? filterType;
        Debug.Log(
            $"[DropDownRef +Var] declared(field)={declaredType?.FullName}, filter(arg)={filterType?.FullName}",
            _bindComp
        );
        var varType = FindMatchingVarType(declaredType);
        if (varType == null)
        {
            Debug.LogError(
                $"[DropDownRef] No AbstractMonoVariable subclass matches type {filterType?.Name}",
                _bindComp
            );
            return;
        }

        var tagName = Property.Name?.TrimStart('_') ?? "newVar";
        var newVar = folder.CreateVariable(varType, tagName);
        if (newVar == null)
            return;

        Undo.RegisterCreatedObjectUndo(newVar.gameObject, "Create Var");

        // 若 property 接受的型別本身就是 Component（VarComp / VarEntity 等都是 Component），
        // 直接把新 Var 指派回 property，省去手動拖拉。
        if (typeof(Component).IsAssignableFrom(Property.ValueEntry.BaseValueType))
        {
            Property.ValueEntry.WeakSmartValue = newVar;
        }
        else
        {
            Debug.LogError(
                $"[DropDownRef] Created Var '{newVar.name}' of type {varType.Name}, but property '{Property.NiceName}' expects type {Property.ValueEntry.BaseValueType.Name}. Please assign it manually.",
                newVar
            );
        }

        // Selection.activeGameObject = newVar.gameObject;
        EditorGUIUtility.PingObject(newVar.gameObject);
    }

    private GUIContent label;

    // private OdinSelector<object> ShowSelector(Rect rect)
    // {
    //     // GenericSelector<object> selector = this.CreateSelector();
    //     rect.x = (float) (int) rect.x;
    //     rect.y = (float) (int) rect.y;
    //     rect.width = (float) (int) rect.width;
    //     rect.height = (float) (int) rect.height;
    //     // if (this.Attribute.AppendNextDrawer && !this.isList)
    //         rect.xMax = GUIHelper.GetCurrentLayoutRect().xMax;
    //     // selector.ShowInPopup(rect, new Vector2((float) this.Attribute.DropdownWidth, (float) this.Attribute.DropdownHeight));
    //     // return (OdinSelector<object>) selector;
    // }
    void AppendNextDrawer()
    {
        IEnumerable<object> objects;
        GUILayout.BeginHorizontal();
        float width = 15f;
        if (this.label != null)
            width += GUIHelper.BetterLabelWidth;
        GUIContent btnLabel = GUIHelper.TempContent("");
        if (Property.Info.TypeOfValue == typeof(Type))
            btnLabel.image = (Texture)
                GUIHelper.GetAssetThumbnail(
                    null,
                    Property.ValueEntry.WeakSmartValue as Type,
                    false
                );
        var OnlyChangeValueOnConfirm = true;
        // objects = OdinSelector<object>.DrawSelectorDropdown(this.label, btnLabel, new Func<Rect, OdinSelector<object>>(this.ShowSelector), !this.OnlyChangeValueOnConfirm, GUIStyle.none, (GUILayoutOption[]) GUILayoutOptions.Width(width));
        if (Event.current.type == EventType.Repaint)
        {
            Rect position = GUILayoutUtility.GetLastRect().AlignRight(15f);
            position.y += 4f;
            SirenixGUIStyles.PaneOptions.Draw(position, GUIContent.none, 0);
        }
        // GUILayout.BeginVertical();
        // bool inAppendedDrawer = true;
        // if (inAppendedDrawer)
        //     GUIHelper.PushGUIEnabled(false);
        // this.CallNextDrawer((GUIContent) null);
        // if (inAppendedDrawer)
        //     GUIHelper.PopGUIEnabled();
        // GUILayout.EndVertical();
        // GUILayout.EndHorizontal();
    }

    protected override void DrawPropertyLayout(GUIContent label)
    {
        SirenixEditorGUI.BeginBox();
        //特規，客製寫拿Selection的方法
        // Debug.Log("DropDownRefAttributeDrawer:" + Property.ValueEntry.BaseValueType);
        if (_isValueDropDownAttribute)
        {
            CallNextDrawer(label);
        }
        else
        {
            // EditorGUILayout.BeginHorizontal();
            var widthRatio = 1f; //0.75f
            // var option = GUILayout.Width(EditorGUIUtility.currentViewWidth * widthRatio);
            // CallNextDrawer(label);
            // SirenixEditorGUI.BeginInlineBox();
            // AppendNextDrawer();
            GUILayout.BeginHorizontal();
            // CallNextDrawer(label);
            if (label != null)
                GUILayout.Label(label, GUILayout.Width(EditorGUIUtility.labelWidth * widthRatio));
            ShowSelector();
            GUILayout.EndHorizontal();
        }

        // SirenixEditorGUI.EndBox();
        // var labelRect = GUILayoutUtility.GetRect(label, null); // GetLastRect();
        var labelRect = GUILayoutUtility.GetLastRect();
        labelRect.width /= 2;
        //Double Click叫事件
        //ping?
        // var target = Property.ValueEntry.WeakSmartValue as UnityEngine.Object;
        //
        // if (target != null && labelRect.Contains(Event.current.mousePosition))
        // {
        //     if (Event.current.clickCount == 1)
        //         EditorGUIUtility.PingObject(target);
        //     if (Event.current.clickCount == 3)
        //         Selection.activeObject = target;
        // }


// bool hasRequiredAttr = Property.GetAttribute<RequiredAttribute>() != null;
        GUI.backgroundColor =
            Property.ValueEntry.WeakSmartValue as Object == null // && hasRequiredAttr
                ? new Color(0.9f, 0.2f, 0.3f, 0.5f)
                : new Color(0.35f, 0.3f, 0.1f, 0.2f);

        // Debug.Log("getterDynamicType():" + getterDynamicType());
        //FIXME: 最好能夠透過ComponentTypeTag來篩選type
        var newObj = SirenixEditorFields.UnityObjectField(
            Property.ValueEntry.WeakSmartValue as Object,
            // Property.ValueEntry.BaseValueType
            getterDynamicType(),
            true
        ); //GUILayout.Width(EditorGUIUtility.currentViewWidth) 這個會太肥噴掉

        // Debug.Log("Property.ValueEntry.BaseValueType:" + Property.ValueEntry.BaseValueType);
        // Debug.Log("Property.ValueEntry.TypeOfValue:" + Property.ValueEntry.TypeOfValue);
        if (newObj == _bindComp)
            Debug.LogError(
                "newObj == Property.ParentValues[0], this should not happen, please check your code. member:"
                    + Property.NiceName
                    + " type:"
                    + Property.ValueEntry.TypeOfValue,
                _bindComp
            );
        else
            Property.ValueEntry.WeakSmartValue = newObj;
        GUI.backgroundColor = Color.white;
        SirenixEditorGUI.EndBox();
    }
}

#endif
