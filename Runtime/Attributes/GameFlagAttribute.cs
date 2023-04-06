using UnityEngine;
using UnityEngine.Events;
public interface IDataOwner
{
    void FlagGeneratedPostProcess(GameFlagBase flag);
    public string Name { get; }
}
//點ref: GameFlagGeneratorPropertyDrawer
public class GameFlagAttribute : PropertyAttribute
{
    public GameFlagAttribute()
    {
        this.flagName = "";
    }
    //
    //TODO: 空的顯示warning?
    //FlagFolderPath + SubFolderName + sceneName+Position + flagName
    public GameFlagAttribute(string subFolderName, string flagName)
    {
        this.subFolderName = subFolderName;
        this.flagName = flagName;
    }
    public string postProcessMethodName;
    public string subFolderName = "";
    public string flagName = "";
}