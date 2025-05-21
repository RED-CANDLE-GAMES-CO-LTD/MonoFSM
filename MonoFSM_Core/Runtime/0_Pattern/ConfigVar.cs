namespace RCGMaker.Core
{
    public interface IConfigVar
    {
        object GetValue();
        T GetValue<T>();
        string GetDescription();
        // string Description { get; }
    }
}