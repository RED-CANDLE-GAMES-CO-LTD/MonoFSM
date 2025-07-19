using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_2022_2_OR_NEWER
// using Unity.Plastic.Newtonsoft.Json;
// using Unity.Plastic.Newtonsoft.Json.Linq;

#else
#endif

public class AbstractScriptableData<TField, TType> : GameFlagBase where TField : FlagField<TType>
{
    public TField field;

    public void Revert() //什麼時候需要revert?
    {
        field.RevertToLastValue();
    }

    public virtual TType CurrentValue
    {
        get => field.CurrentValue;
        set =>
            //FIXME: 拿掉
            field.CurrentValue = value;
    }

    [TextArea] public string Note;
}

public static class NativeDataHelper
{
    public static object GetProperty<T>(this INativeData data, string propertyName)
    {
        var t = data.GetType();
        var property = t.GetProperty(propertyName);
        if (property == null)
        {
            Debug.LogError("No property found:" + propertyName, data as Object);
            return null;
        }

        var value = property.GetValue(data);
        return value;
    }

    //get field
    public static object GetField<T>(this INativeData data, string fieldName)
    {
        var t = data.GetType();
        var field = t.GetField(fieldName);
        if (field == null)
        {
            Debug.LogError("No field found:" + fieldName, data as Object);
            return null;
        }

        var value = field.GetValue(data);
        return value;
    }
}

public interface INativeDataProvider
{
    public INativeData GetNativeData();
    public Type GetNativeDataType();
}

public interface INativeData
{
}

public interface INativeDataConsumer
{
    void UpdateNativeData(INativeData data);
}

//最基礎的GameFlag元件
public abstract class GameFlagBase : ScriptableObject, ISerializable, ISelfValidator, INativeData
{
    public IEnumerable<string> GetAllFlagFieldNames<T>()
    {
        //get field which inherit from FlagField
        var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(f => f.FieldType.IsSubclassOf(typeof(T)));
        return fields.Select(f => f.Name);
    }

    // public bool isAutoGenType = false; //非自動生成的不要被覆蓋掉
    // protected bool inited = false;
    [Header("Asset GUID")] [DisableIf("@true")] [SerializeField]
    // [ReadOnly]
    private string SaveID = "";

    public void SetSaveID(string id)
    {
        SaveID = id;
        _finalSaveID = SaveID + GetType().Name;
    }

    private string _finalSaveID; //這個cache有點討厭？

    [PreviewInInspector]
    public string FinalSaveID
    {
        get
        {
            if (string.IsNullOrEmpty(_finalSaveID))
                _finalSaveID = SaveID + GetType().Name;
            return _finalSaveID;
        }
    }

    public string GetSaveID => SaveID;

    public enum GameStateType
    {
        Manual, //手動串，可能多對一
        AutoUnique //一對一最單純的，自動生成，可以整包砍掉重建
    }

    [EnumToggleButtons] [DisableIf("@true")]
    public GameStateType gameStateType = GameStateType.Manual;

    [EditorOnly]
    protected virtual void OnValidate()
    {
        ValidateSaveID();
    }

    [Button]
    [EditorOnly]
    // [Button]
    private void ValidateSaveID()
    {
        // Debug.Log("ValidateSaveID" + name, this);
#if UNITY_EDITOR
        if (gameStateType == GameStateType.AutoUnique)
        {
            //不用做事
        }
        else //manual, duplicate的時候會需要重新assign
        {
            var guid = this.GetAssetGUID();

            if (GetSaveID == guid) return;
            _finalSaveID = "";
            SetSaveID(guid);

            this.SafeSetDirty();
        }
#endif
    }

    // public Vector3 position;//該在這裡綁嗎?
    private void InitField<TField, T>(FlagFieldBase field, TestMode mode) where TField : FlagField<T>
    {
        if (field == null)
        {
            Debug.LogError("field is null" + field + ",flag:" + this, this);
            return;
        }

        ((TField)field).Init(mode, this);
    }

    public virtual void FlagAwake(TestMode mode) //抓default Value或currentValue
    {
        fieldCaches.Clear();
        FetchFields();
        // var myField = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        // Debug.Log("Flag Convertor WriteJSON");
        foreach (var fieldInfo in fieldCaches)
        {
            var fieldBase = fieldInfo.Value;
            switch (fieldBase)
            {
                case FlagFieldBool field:
                    InitField<FlagFieldBool, bool>(field, mode);
                    break;
                case FlagFieldInt field:
                    InitField<FlagFieldInt, int>(field, mode);
                    break;
                case FlagFieldString field:
                    InitField<FlagFieldString, string>(field, mode);
                    break;
                case FlagFieldFloat field:
                    InitField<FlagFieldFloat, float>(field, mode);
                    break;
            }
        }
    }

    // Define a delegate with the same signature as FieldInfo.GetValue
    private delegate FlagFieldBase GetValueDelegate(GameFlagBase obj);

// Create a dictionary to store the delegates
    // private Dictionary<string, GetValueDelegate> fieldDelegates = new();

// Get all fields of the GameFlagBase instance


    private void FetchFields()
    {
        if (fieldCaches.Count > 0)
            return;

        var fields = GetType()
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields)
        {
            // Check if the field's type is FlagFieldBase or a subclass of FlagFieldBase
            if (typeof(FlagFieldBase).IsAssignableFrom(field.FieldType))
            {
                // Create a delegate that gets the value of the field
                // var getValueDelegate =
                //     (GetValueDelegate)Delegate.CreateDelegate(typeof(GetValueDelegate), field, "GetValue");
                fieldCaches.Add(field.Name, field.GetValue(this) as FlagFieldBase);
                // Add the delegate to the dictionary
                // fieldDelegates.Add(field.Name, getValueDelegate);
            }
        }
    }


    //FIXME: 濫扣
    //Reset還會去用lastMode...這個狀態有點多餘
    public virtual void Reset() //抓default Value或currentValue
    {
        // fieldCaches.Clear();
        // FetchFields();
        foreach (var fieldDelegate in fieldCaches)
        {
            Debug.Log("Reset:" + fieldDelegate.Value, this);
            var fieldBase = fieldDelegate.Value;
            fieldBase.ResetToDefault();
        }
    }

    public virtual void FlagInitStart() //特殊的flag要做一些initialize的話在這
    {
//clear 
    }

    public virtual void FlagEquipCheck()
    {
    }

    public virtual bool IsEquipping()
    {
        return false;
    }

    // private void OnDisable() {

    // }
    // public virtual string ToJSON()
    // {
    //     // Get the type handle of a specified class.

    //     // Get the fields of the specified class.

    // public virtual void FromJSON(string text)
    // {

    // }
    public virtual void GenerateFlagPostProcess()
    {
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        // Debug.Log("GetObject Data");

        FieldInfo[] myField = GetType().GetFields();
        for (var i = 0; i < myField.Length; i++)
        {
            if (myField[i].FieldType == typeof(FlagFieldBool))
            {
                info.AddValue(myField[i].Name, myField[i].GetValue(this));
            }

            if (myField[i].FieldType == typeof(FlagFieldInt))
            {
                info.AddValue(myField[i].Name, myField[i].GetValue(this));
            }
        }
    }

    public FlagField<T> FindField<T>(string fieldName)
    {
        if (fieldName == null)
            return null;
        if (fieldName.Trim().Length == 0)
            return null;

        if (fieldCaches.ContainsKey(fieldName))
            return fieldCaches[fieldName] as FlagField<T>;

        try
        {
            var t = this.GetType();
            var field = t.GetField(fieldName).GetValue(this) as FlagField<T>;
            fieldCaches.Add(fieldName, field);

            return field;
            //
        }
        catch
        {
            return null;
        }
    }


    public Dictionary<string, FlagFieldBase> fieldCaches = new();


    // public void OnBeforeSerialize()
    // {
    //     ValidateSaveID();
    // }

    // public void OnAfterDeserialize()
    // {
    //     // throw new NotImplementedException();
    // }

    [EditorOnly]
    public void Validate(SelfValidationResult result)
    {
#if UNITY_EDITOR
        //往上找看看有沒有GameFlagCollection
        var path = AssetDatabase.GetAssetPath(this);
        // Debug.Log("Validate:" + path);
        var rootPath = path.Split('/')[0];
        //find GameFlagCollection in parent

        var allProjectFlags = AssetDatabase.FindAssets("t:GameFlagCollection", new[] { rootPath });
        // Debug.Log("RootPath:" + rootPath + " allProjectFlags:" + allProjectFlags.Length);
        foreach (var flag in allProjectFlags)
        {
            var flagPath = AssetDatabase.GUIDToAssetPath(flag);
            // Debug.Log("Flag:" + flagPath);
            //check if the path is in the parent folder of the flag
            var folderPath = System.IO.Path.GetDirectoryName(flagPath);
            if (path.Contains(folderPath))
            {
                // Debug.Log("folderPath:" + folderPath + " Path:" + path);

                var flagCollection = AssetDatabase.LoadAssetAtPath<GameFlagCollection>(flagPath);
                if (flagCollection.Flags.Contains(this))
                    return;
                else
                {
                    result.AddError("Not in FlagCollection:" + flagCollection.name).WithFix(() =>
                    {
                        if (!flagCollection.Flags.Contains(this))
                            flagCollection.Flags.Add(this);
                        EditorUtility.SetDirty(flagCollection);
                    });
                }
            }
        }

        //FIXME: 沒有完全解決，放多個路徑和共用同個型別要限制資料夾還是蠻頭大的
        this.AssetInFolderValidate(new string[] { GameStateAttribute.GameStateFolderPath, "17_PlayerPrefFlag" },
            result);
#endif
    }

    // bool 

    [Button]
    private void MoveAssetToFolder()
    {
#if UNITY_EDITOR
        var targetPath = "Assets/" + GameStateAttribute.GameStateFolderPath + "/" + name + ".asset";
        Debug.Log("MoveAssetToFolder: targetPath:" + targetPath);
        var result = AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(this),
            targetPath);

        Debug.Log("MoveAssetToFolder: result:" + result);
#endif
    }

    /*
    //FIXME: 要把這個換掉嗎？
    public void FlagToJSON(JSONObject o)
    {
        //FIXME: remove fields that are not serializable
        foreach (var fieldGetter in fieldCaches)
        {
            var fieldBase = fieldGetter.Value;
            switch (fieldBase)
            {
                case FlagFieldBool field:
                    o.AddField(fieldGetter.Key, field.SaveValue);
                    break;
                case FlagFieldInt field:
                    o.AddField(fieldGetter.Key, field.SaveValue);
                    break;
                case FlagFieldString field:
                    o.AddField(fieldGetter.Key, field.SaveValue);
                    break;
                case FlagFieldFloat field:
                    o.AddField(fieldGetter.Key, field.SaveValue);
                    break;
                case FlagFieldLong field:
                    o.AddField(fieldGetter.Key, field.SaveValue);
                    break;
            }
        }
    }
*/
    //Dictionary type.
    public void FlagToJSON(Dictionary<string, object> o)
    {
        //FIXME: remove fields that are not serializable
        foreach (var fieldGetter in fieldCaches)
        {
            var fieldBase = fieldGetter.Value;
            switch (fieldBase)
            {
                case FlagFieldBool field:
                    o.Add(fieldGetter.Key, field.SaveValue);
                    break;
                case FlagFieldInt field:
                    o.Add(fieldGetter.Key, field.SaveValue);
                    break;
                case FlagFieldString field:
                    o.Add(fieldGetter.Key, field.SaveValue);
                    break;
                case FlagFieldFloat field:
                    o.Add(fieldGetter.Key, field.SaveValue);
                    break;
                case FlagFieldLong field:
                    o.Add(fieldGetter.Key, field.SaveValue);
                    break;
            }
        }
    }
}

// public class FlagJsonConverter : JsonConverter
// {
//     private readonly Type[] _types;
//
//     public FlagJsonConverter(params Type[] types)
//     {
//         _types = types;
//     }
//
//     public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
//     {
//         var flagDict = value as Dictionary<string, GameFlagBase>;
//         var result = new JObject();
//
//         foreach (var fPair in flagDict)
//         {
//             var flag = fPair.Value as GameFlagBase;
//             var o = new JObject();
//
//             // o.Add("flagpath", );
//             var myField = flag.GetType().GetFields();
//             // Debug.Log("Flag Convertor WriteJSON");
//             foreach (var f in myField)
//             {
//                 if (f.FieldType == typeof(FlagFieldBool))
//                 {
//                     if (f.GetValue(flag) is FlagFieldBool field) o.Add(f.Name, JObject.FromObject(field));
//                     // o.Add(myField[i].Name, );
//
//                     // Debug.Log("Field" + myField[i].Name);
//                     // o.Add(myField[i].Name, JToken.FromObject(myField[i].GetValue(value)));
//
//                     // info.AddValue(myField[i].Name, myField[i].GetValue(this));
//                 }
//                 // if (myField[i].FieldType == typeof(FlagFieldInt))
//                 // {
//                 //     info.AddValue(myField[i].Name, myField[i].GetValue(this));
//                 // }
//             }
//
//             result.Add(flag.FinalSaveID, o);
//
//         }
//         result.WriteTo(writer);
//
//         // JToken t = JToken.FromObject(value);
//
//         // if (t.Type != JTokenType.Object)
//         // {
//         //     t.WriteTo(writer);
//         // }
//         // else
//         // {
//         //     JObject o = (JObject)t;
//
//
//         //     o.AddFirst(new JProperty("Keys", new JArray(propertyNames)));
//
//         //     o.WriteTo(writer);
//         // }
//     }
//
//     public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
//     {
//         Debug.Log(reader.ValueType);
//         Debug.Log(objectType);
//         if (existingValue == null)
//         {
//             Debug.Log("NNull");
//         }
//         else
//         {
//             Debug.Log(existingValue);
//         }
//         Dictionary<string, GameFlagBase> flagDict = existingValue as Dictionary<string, GameFlagBase>;
//         Debug.Log(flagDict.Count);
//         var obj = JObject.ReadFrom(reader);
//
//         var flagList = obj.Values().ToList();
//         for (var i = 0; i < flagList.Count; i++)
//         {
//             var flagPath = Convert.ToString(flagList[i].SelectToken("flagPath"));
//             if (!flagDict.ContainsKey(flagPath))
//             {
//                 Debug.Log("Nokey " + flagPath);
//                 break;
//             }
//
//             GameFlagBase flagBase = flagDict[flagPath];
//             FieldInfo[] myField = flagBase.GetType().GetFields();
//             for (var j = 0; j < myField.Length; j++)
//             {
//                 if (myField[j].FieldType == typeof(FlagFieldBool))
//                 {
//                     Debug.Log("fieldName" + myField[j].Name);
//                     var cValue = Convert.ToBoolean(obj.SelectToken(myField[j].Name).SelectToken("CurrentValue"));
//                     (myField[j].GetValue(flagBase) as FlagFieldBool).CurrentValue = cValue;
//                 }
//                 // if (myField[i].FieldType == typeof(FlagFieldInt))
//                 // {
//                 //     info.AddValue(myField[i].Name, myField[i].GetValue(this));
//                 // }
//             }
//         }
//         return existingValue;
//     }
//
//     public override bool CanRead
//     {
//         get { return true; }
//     }
//
//     public override bool CanConvert(Type objectType)
//     {
//         // return _types.Any(t => t == objectType);
//         return true;
//     }
//
//
//     
// }