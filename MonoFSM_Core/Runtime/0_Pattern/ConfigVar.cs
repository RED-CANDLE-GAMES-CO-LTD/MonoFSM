namespace RCGMaker.Core
{
    public interface IConfigVar
    {
        object GetValue();
        string GetDescription();
    }
}