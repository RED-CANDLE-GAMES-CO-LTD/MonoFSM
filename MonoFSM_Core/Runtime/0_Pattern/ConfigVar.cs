namespace RCGMaker.Core
{
    public interface IValueProvider
    {
        object GetValue();
        T GetValue<T>();
        string GetDescription();
        // string Description { get; }
    }
}