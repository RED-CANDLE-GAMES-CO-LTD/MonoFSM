using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Runtime.Serialization;
using System.Linq;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;

#if UNITY_2022_2_OR_NEWER
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;

#else
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

public class AbstractScriptableData<TField, TType> : GameFlagBase where TField : FlagField<TType>
{
    public TField field;

    //
    public void Revert()
    {
        field.RevertToLastValue();
    }
    public TType CurrentValue
    {
        get => field.CurrentValue;
        set
        {
            field.CurrentValue = value;
        }
    }
}
//最基礎的GameFlag元件
[System.Serializable]
public abstract class GameFlagBase : ScriptableObject, ISerializable, ISerializationCallbackReceiver, ISelfValidator
{
    // public bool isAutoGenType = false; //非自動生成的不要被覆蓋掉
    // protected bool inited = false;
    [ReadOnly] public string SaveID = "";


    public enum GameStateType
    {
        Manual, //手動串，可能多對一
        AutoUnique //一對一最單純的，自動生成，可以整包砍掉重建
    }

    
    [EnumToggleButtons] [ReadOnly] public GameStateType gameStateType = GameStateType.Manual;

    [EditorOnly]
    protected virtual void OnValidate()
    {
        ValidateSaveID();
    }

    [EditorOnly]
    private void ValidateSaveID()
    {
#if UNITY_EDITOR
        if (gameStateType == GameStateType.AutoUnique)
        {
            //不用做事
        }
        else //manual, duplicate的時候會需要重新assign
        {
            var guid = this.GetGUID();
            if (SaveID != guid)
                SaveID = guid;
        }
#endif
    }

    // public Vector3 position;//該在這裡綁嗎?
    public virtual void FlagAwake(TestMode mode) //抓default Value或currentValue
    {
        FieldInfo[] myField = GetType().GetFields();
        // Debug.Log("Flag Convertor WriteJSON");
        for (var i = 0; i < myField.Length; i++)
        {
            if (myField[i].FieldType == typeof(FlagFieldBool))
            {
                var field = (myField[i].GetValue(this) as FlagFieldBool);
                field.Init(mode);
            }
            else if (myField[i].FieldType == typeof(FlagFieldInt))
            {
                var field = (myField[i].GetValue(this) as FlagFieldInt);
                field.Init(mode);
            }
            else if (myField[i].FieldType == typeof(FlagFieldString))
            {                    
                var field = (myField[i].GetValue(this) as FlagFieldString);
                field.Init(mode);
            }
            else if (myField[i].FieldType == typeof(FlagFieldFloat))
            {
                var field = (myField[i].GetValue(this) as FlagFieldFloat);
                field.Init(mode);
            }
        }

    }

    //FIXME: 濫扣
    //Reset還會去用lastMode...這個狀態有點多餘
    public virtual void Reset() //抓default Value或currentValue
    {
        Debug.Log("Reset data:" + name);
        FieldInfo[] myField = GetType().GetFields();
        // Debug.Log("Flag Convertor WriteJSON");
        foreach (var fieldInfo in myField)
        {
            if (fieldInfo.FieldType == typeof(FlagFieldBool))
            {
                var field = fieldInfo.GetValue(this) as FlagFieldBool;
                field?.Reset();
            }
            else if (fieldInfo.FieldType == typeof(FlagFieldInt))
            {
                var field = fieldInfo.GetValue(this) as FlagFieldInt;
                field?.Reset();
            }
            else if (fieldInfo.FieldType == typeof(FlagFieldString))
            {
                var field = fieldInfo.GetValue(this) as FlagFieldString;
                field?.Reset();
            }
            else if (fieldInfo.FieldType == typeof(FlagFieldFloat))
            {
                var field = fieldInfo.GetValue(this) as FlagFieldFloat;
                field?.Reset();
            }
        }
    }
    public virtual void FlagInit() //特殊的flag要做一些initialize的話在這
    {

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
        var t = this.GetType();
        var field = t.GetField(fieldName).GetValue(this) as FlagField<T>;
        return field;
    }


    public void OnBeforeSerialize()
    {
        ValidateSaveID();
    }

    public void OnAfterDeserialize()
    {
        // throw new NotImplementedException();
    }

    [EditorOnly]
    public void Validate(SelfValidationResult result)
    {
        this.AssetInFolderValidate(GameStateAttribute.GameStateFolderPath, result);
    }
}

public class FlagJsonConverter : JsonConverter
{
    private readonly Type[] _types;

    public FlagJsonConverter(params Type[] types)
    {
        _types = types;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Dictionary<string, GameFlagBase> flagDict = value as Dictionary<string, GameFlagBase>;
        JObject result = new JObject();

        foreach (var fPair in flagDict)
        {

            GameFlagBase flag = fPair.Value as GameFlagBase;
            JObject o = new JObject();

            // o.Add("flagpath", );
            FieldInfo[] myField = flag.GetType().GetFields();
            // Debug.Log("Flag Convertor WriteJSON");
            for (var i = 0; i < myField.Length; i++)
            {
                if (myField[i].FieldType == typeof(FlagFieldBool))
                {
                    var field = (myField[i].GetValue(flag) as FlagFieldBool);

                    o.Add(myField[i].Name, JObject.FromObject(field));
                    // o.Add(myField[i].Name, );

                    // Debug.Log("Field" + myField[i].Name);
                    // o.Add(myField[i].Name, JToken.FromObject(myField[i].GetValue(value)));

                    // info.AddValue(myField[i].Name, myField[i].GetValue(this));
                }
                // if (myField[i].FieldType == typeof(FlagFieldInt))
                // {
                //     info.AddValue(myField[i].Name, myField[i].GetValue(this));
                // }
            }

            result.Add(flag.SaveID, o);

        }
        result.WriteTo(writer);

        // JToken t = JToken.FromObject(value);

        // if (t.Type != JTokenType.Object)
        // {
        //     t.WriteTo(writer);
        // }
        // else
        // {
        //     JObject o = (JObject)t;


        //     o.AddFirst(new JProperty("Keys", new JArray(propertyNames)));

        //     o.WriteTo(writer);
        // }
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        Debug.Log(reader.ValueType);
        Debug.Log(objectType);
        if (existingValue == null)
        {
            Debug.Log("NNull");
        }
        else
        {
            Debug.Log(existingValue);
        }
        Dictionary<string, GameFlagBase> flagDict = existingValue as Dictionary<string, GameFlagBase>;
        Debug.Log(flagDict.Count);
        var obj = JObject.ReadFrom(reader);

        var flagList = obj.Values().ToList();
        for (var i = 0; i < flagList.Count; i++)
        {
            var flagPath = Convert.ToString(flagList[i].SelectToken("flagPath"));
            if (!flagDict.ContainsKey(flagPath))
            {
                Debug.Log("Nokey " + flagPath);
                break;
            }

            GameFlagBase flagBase = flagDict[flagPath];
            FieldInfo[] myField = flagBase.GetType().GetFields();
            for (var j = 0; j < myField.Length; j++)
            {
                if (myField[j].FieldType == typeof(FlagFieldBool))
                {
                    Debug.Log("fieldName" + myField[j].Name);
                    var cValue = Convert.ToBoolean(obj.SelectToken(myField[j].Name).SelectToken("CurrentValue"));
                    (myField[j].GetValue(flagBase) as FlagFieldBool).CurrentValue = cValue;
                }
                // if (myField[i].FieldType == typeof(FlagFieldInt))
                // {
                //     info.AddValue(myField[i].Name, myField[i].GetValue(this));
                // }
            }
        }
        return existingValue;
    }

    public override bool CanRead
    {
        get { return true; }
    }

    public override bool CanConvert(Type objectType)
    {
        // return _types.Any(t => t == objectType);
        return true;
    }
}