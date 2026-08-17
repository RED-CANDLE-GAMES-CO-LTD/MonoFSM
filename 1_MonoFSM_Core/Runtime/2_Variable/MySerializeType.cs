using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable.FieldReference;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace MonoFSM.Variable
{
    /// <summary>
    /// 讓遷移／驗證工具可以不管泛型參數就操作 MySerializedType
    /// </summary>
    public interface IMySerializedType
    {
        /// <summary>擁有這筆資料的 asset，錯誤訊息定位與 SetDirty 用</summary>
        Object BindObject { get; set; }

        /// <summary>目前序列化的型別名稱（AssemblyQualifiedName）</summary>
        string SerializedTypeName { get; }

        /// <summary>序列化的名稱是否已對齊實際型別</summary>
        bool IsNameInSync { get; }

        /// <summary>清除快取重新解析，回傳是否成功</summary>
        bool ValidateTypeReference();
    }

    [Serializable]
    public class MySerializedType : MySerializedType<object> { }

    //EditorOnly
    //T 表示這個type可以
    //兩個Type, 一個filter用，一個實際使用的
    [Serializable]
    public class MySerializedType<T> : ISerializationCallbackReceiver, IMySerializedType
    {
        [HideInInspector]
        [NonSerialized]
        public Object _bindObject; //debug用，也是 SyncTypeNameIfNeeded 寫回時 SetDirty 的對象

        Object IMySerializedType.BindObject
        {
            get => _bindObject;
            set => _bindObject = value;
        }

        string IMySerializedType.SerializedTypeName => typeName;

        //override baseType
        [FormerlySerializedAs("_baseVarTypeName")]
        [FormerlySerializedAs("_varTypeName")]
        [SerializeField]
        [PreviewInDebugMode]
        private string _baseFilterTypeName;

        private Type _baseFilterType; //default 用 T?

        [NonSerialized]
        private bool _baseFilterTypeResolved;

        [ShowInDebugMode]
        [SerializeField]
        [ReadOnly]
        private string _typeFullName; // 用於顯示和驗證 //兩種都有？搞屁？

        [ShowInDebugMode]
        [SerializeField]
        [ReadOnly]
        private string _assemblyName;

        public void SetBaseType(Type type)
        {
            if (type == null)
                return;
            _baseFilterType = type;
            _baseFilterTypeName = type.AssemblyQualifiedName;
            _baseFilterTypeResolved = true;
        }

        [PreviewInDebugMode]
        public Type BaseFilterType
        {
            get
            {
                if (!_baseFilterTypeResolved)
                {
                    _baseFilterTypeResolved = true;
                    if (!string.IsNullOrEmpty(_baseFilterTypeName))
                        _baseFilterType = Type.GetType(_baseFilterTypeName);
                }

                if (_baseFilterType != null)
                    return _baseFilterType;
                return typeof(T); //如果沒有設定，回傳T
            }
            set
            {
                _baseFilterType = value;
                _baseFilterTypeName = value?.AssemblyQualifiedName;
                _baseFilterTypeResolved = true;
            }
        }

        public bool IsTypeSet => ResolvedType != null;

        private Type _type; //cached

        [NonSerialized]
        private bool _typeResolved; //是否已經嘗試解析過（成功或失敗都算），避免反覆觸發全域掃描

        /// <summary>
        /// 懶解析入口：第一次被讀取時才真的去找型別。
        /// 解析放在這裡而不是 OnAfterDeserialize，是因為那邊在反序列化執行緒上跑，
        /// 全域 assembly 掃描會拖慢每一次 domain reload，而且沒被用到的資料也會被迫解析
        /// </summary>
        private Type ResolvedType
        {
            get
            {
                if (_typeResolved)
                    return _type;
                _typeResolved = true;

                if (typeName.IsNullOrWhitespace())
                {
                    _type = null;
                    return null;
                }

                _type = RefactorSafeNameResolver.FindTypeByCurrentOrFormerName(
                    typeName,
                    _assemblyName
                );

                if (_type == null)
                    Debug.LogError(
                        $"[MySerializedType] 找不到型別 '{typeName}'（assembly: {_assemblyName}），可能已改名或搬移，請重新選擇",
                        _bindObject
                    );
                else
                    SyncTypeNameIfNeeded(); //解析成功就把新名字寫回去，下次不用再 fallback

                return _type;
            }
        }

        private bool FilterTypes(Type type)
        {
            if (BaseFilterType == null)
                return true;
            return BaseFilterType.IsAssignableFrom(type);
        }

        public void SetType(Type type)
        {
            _type = type;
            _typeResolved = true;
            typeName = _type?.AssemblyQualifiedName ?? typeName;
            SyncNameFields();
        }

        // [Header("宣告型別：")]

        [ShowInDebugMode]
        [TypeSelectorSettings(FilterTypesFunction = nameof(FilterTypes))]
        public Type RestrictType
        {
            get
            {
                var resolved = ResolvedType;
                if (resolved == null)
                    return BaseFilterType; //default用程式碼的
                return resolved;
            }
            set
            {
                _type = value;
                _typeResolved = true;
                typeName = _type?.AssemblyQualifiedName ?? typeName;
                SyncNameFields();
            }
        }

        /// <summary>
        /// 把 _typeFullName / _assemblyName 對齊目前的 _type
        /// </summary>
        private void SyncNameFields()
        {
            if (_type != null)
            {
                _typeFullName = _type.FullName;
                _assemblyName = _type.Assembly.GetName().Name;
            }
            else
            {
                _typeFullName = "";
                _assemblyName = "";
            }
        }

        bool IsTypeMissing => typeName.IsNullOrWhitespace() == false && ResolvedType == null;

        [InfoBox("type is not exist, reselect", InfoMessageType.Error, nameof(IsTypeMissing))]
        [Required]
        [ShowInDebugMode]
        [SerializeField]
        private string typeName; //這個是full，太難了？

        [ShowInInspector]
        [HideLabel]
        [DisplayAsString]
        public string TypeName
        {
            get => ResolvedType?.Name;
            private set => throw new NotImplementedException();
        }

        public void OnBeforeSerialize()
        {
            typeName = _type?.AssemblyQualifiedName ?? typeName;
            //懶解析下 _baseFilterType 可能還沒解析出來，不能無條件覆寫，否則會把已存的名稱清掉
            _baseFilterTypeName = _baseFilterType?.AssemblyQualifiedName ?? _baseFilterTypeName;
        }

        public void OnAfterDeserialize()
        {
            //這裡在反序列化執行緒上跑，只清快取不做解析，真正的查找延到 ResolvedType 被讀取時
            _type = null;
            _typeResolved = false;
            _baseFilterType = null;
            _baseFilterTypeResolved = false;
        }

        /// <summary>
        /// 解析成功後把序列化的名稱同步成目前的型別名稱，
        /// 讓「靠 fallback 才救回來」的資料只會發生一次
        /// </summary>
        private void SyncTypeNameIfNeeded()
        {
            if (_type == null)
                return;

            var currentAssemblyQualifiedName = _type.AssemblyQualifiedName;
            var currentFullName = _type.FullName;
            var currentAssemblyName = _type.Assembly.GetName().Name;

            if (
                typeName == currentAssemblyQualifiedName
                && _typeFullName == currentFullName
                && _assemblyName == currentAssemblyName
            )
                return; //已經是最新的

            Debug.Log(
                $"[MySerializedType] 型別已重構：'{_typeFullName}' -> '{currentFullName}'，自動更新序列化名稱",
                _bindObject
            );

            typeName = currentAssemblyQualifiedName;
            _typeFullName = currentFullName;
            _assemblyName = currentAssemblyName;

#if UNITY_EDITOR
            //需要 owner 才能標 dirty，_bindObject 沒設的話就等下次 MySerializedTypeMigrationTool 掃過去
            if (_bindObject != null)
                UnityEditor.EditorUtility.SetDirty(_bindObject);
#endif
        }

        /// <summary>
        /// 清除快取重新解析，成功的話會順便把名稱寫回。回傳是否解析成功
        /// </summary>
        public bool ValidateTypeReference()
        {
            if (string.IsNullOrEmpty(typeName))
                return false;

            //清除快取，強制重新解析
            _type = null;
            _typeResolved = false;

            return ResolvedType != null;
        }

        /// <summary>
        /// 目前序列化的名稱是否已經對齊實際型別（給遷移工具判斷用）
        /// </summary>
        public bool IsNameInSync
        {
            get
            {
                var resolved = ResolvedType;
                if (resolved == null)
                    return false;
                return typeName == resolved.AssemblyQualifiedName
                    && _typeFullName == resolved.FullName
                    && _assemblyName == resolved.Assembly.GetName().Name;
            }
        }
    }
}
