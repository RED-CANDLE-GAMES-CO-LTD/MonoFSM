using UnityEngine;
using UnityEngine.Events;

//點ref: GameFlagGeneratorPropertyDrawer
//FIXME: Deprecated 這個之後可能不用了，已經都用GameState了
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