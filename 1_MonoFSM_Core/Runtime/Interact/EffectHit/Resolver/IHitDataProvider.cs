using UnityEngine;

public interface IHitDataProvider
{
   public IEffectHitData GetHitData();
}

public interface ICollisionDataProvider
{
   public Collision GetCollision();
}