namespace RCGMaker.Core
{
    public interface IValueProvider
    {
        //FIXME: 這個有點討厭...
        object GetValue();

        T GetValue<T>()
        {
            return (T)GetValue();
        }

        string Description { get; }
        // string Description { get; }
    }
}