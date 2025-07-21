using UnityEngine;

public interface IEditorOnly
{
}

public interface IBeforeBuild //auto也要清掉/不要gen, 再strip時去把auto cache
{
    public GameObject gameObject { get; }
    public void OnBeforeBuild();
}