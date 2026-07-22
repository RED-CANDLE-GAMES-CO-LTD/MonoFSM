using _1_MonoFSM_Core.Runtime.MonoData;
using MonoFSM.Variable;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

//第一次記住
//看到dynamic rigidbody就應該要有這個
//要寫一堆restore系列？
//FIXME: 放在這，還是應該放在init state
/// <summary>
/// transform memory?
/// 重置位置
/// 也會處理 rb
/// </summary>
public class LocalTransformResetter : MonoBehaviour, IResetStateRestore, IResetStart
{
    [ShowInInspector]
    private Vector3 _initLocalPosition;

    [ShowInInspector] private Quaternion _initLocalRotation;
    public VarVector3 _initGlobalPos;


    [ShowInInspector]
    private Transform _initParent;
    private Vector3 _initLocalScale;

    [ShowInInspector]
    private bool _isResetParameterInit;

    private bool _isKinematic;

    //FIXME: 要拆開嗎？
    [AutoParent]
    public Rigidbody _rigidbody;

    // [AutoChildren(false)] private Rigidbody2D rigidbody2D;

    private void Awake()
    {
        // if (rigidbody2D != null && rigidbody2D.mass > 1)
        // {
        //     Debug.LogError("rigidbody2D.mass > 1, 怪怪mass? 檢查一下是scene還是prefab?", this);
        // }
    }

    [AutoChildren] ViewRoot _viewRoot;


    private bool ParameterInitCheck()
    {
        if (_isResetParameterInit)
            return true;

        InitSaveSnapshot();
        _isResetParameterInit = true;
        return false;
    }

    private Vector3 _cacheGlobalPos;
    private void InitSaveSnapshot()
    {
        _initParent = transform.parent;
        _initLocalPosition = transform.localPosition;
        _initLocalRotation = transform.localRotation;
        _initLocalScale = transform.localScale;
        _cacheGlobalPos = transform.position;
        _initGlobalPos?.SetValue(_cacheGlobalPos);
        //--
        if (_rigidbody)
            _isKinematic = _rigidbody.isKinematic;
    }

    public void ResetStateRestore(bool isHardReset)
    {
        if (_viewRoot?.AttachToViewRoot != null) return; //有parent就不reset，等parent reset的時候一起reset就好

        if (ParameterInitCheck()) //第一次記下來？還是分開感覺比較好？
        {
            transform.SetParent(_initParent);
            //FIXME: network character?
            transform.localPosition = _initLocalPosition;
            transform.localRotation = _initLocalRotation;
            transform.localScale = _initLocalScale;
        }

        if (_rigidbody)
        {
            _rigidbody.isKinematic = _isKinematic;
            if (!_isKinematic)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }

            _rigidbody.ResetInertiaTensor();
        }
    }

    public void ResetStart()
    {
        _initGlobalPos?.SetValue(_cacheGlobalPos);
    }
}
