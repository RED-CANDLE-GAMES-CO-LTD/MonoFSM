using UnityEngine;

public class ApplicationCore : SingletonBehaviour<ApplicationCore>
{

   private void Start()
   {
      //這邊跟 LevelReseter 一樣
      PoolManager.HandleGameLevelAwakeReverse(this.gameObject);
      PoolManager.HandleGameLevelAwake(this.gameObject);
      PoolManager.HandleGameLevelStartReverse(this.gameObject);
      PoolManager.HandleGameLevelStart(this.gameObject);
      PoolManager.ResetReload(this.gameObject);
   }
}
