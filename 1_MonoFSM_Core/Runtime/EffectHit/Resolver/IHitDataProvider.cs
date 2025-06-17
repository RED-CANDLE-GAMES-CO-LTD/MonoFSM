using MonoFSM.Core.Detection;
using UnityEngine;

public interface IHitDataProvider
{
   public IEffectHitData GetHitData();
}

public interface ICollisionDataProvider
{
   public Collision GetCollision();
}

public interface IDetectDataProvider
{
   public DetectData? GetDetectData();
}