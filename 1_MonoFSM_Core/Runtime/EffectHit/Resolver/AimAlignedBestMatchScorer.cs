using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Interact.EffectHit
{
    /// <summary>
    ///     視線對齊版的 Best Match 評分器。
    ///     和 DefaultBestMatchScorer 的差別：多一項「receiver 有多靠近瞄準射線方向」的分數
    ///     （aimScore = dot(rayDir, 指向 receiver 的單位向量)，-1..1），且三項權重都可調：
    ///     score = MatchPriority * _priorityWeight + aimScore * _aimWeight - distance * _distanceWeight。
    ///     預設值讓「看向誰」主導排序、距離只當同視線角下的 tiebreaker，
    ///     適合同一台裝置上有多顆 receiver（大 Trigger 空間 vs 小零件）互相搶 best match 的情況。
    ///     DefaultBestMatchScorer 是全專案預設（只有 priority + 距離），不要改它；要視線加權就換掛這顆。
    /// </summary>
    public class AimAlignedBestMatchScorer : AbstractOnlyTriggerBestMatch
    {
        public enum AimScoreState
        {
            NotEvaluatedYet,
            Ok,
            AimTermSkipped_BelowMinDot,
            Fallback_NoAimRayTransform,
            Fallback_ZeroForward,
            Failed_ReceiverIsNull,
        }

        [Title("射線來源")]
        [Tooltip(
            "瞄準射線來源。請手動指到 [Raycast] RaycastCache 節點（它每次 cast 都會把自己的 rotation "
                + "轉成 cachedRay 的方向，所以 forward 就是視線方向），或任何朝向即瞄準方向的槍口節點。"
                + "用 position 當射線原點、forward 當射線方向。沒指的話會退化成 DefaultBestMatchScorer 的行為。"
        )]
        [SerializeField]
        private Transform _aimRayTransform;

        [Title("評分權重")]
        [Tooltip("MatchPriority 的權重係數。權重很大 = priority 差 1 就絕對勝出")]
        [SerializeField]
        private float _priorityWeight = 1000f;

        [Tooltip("視線對齊度（dot, -1..1）的權重係數。預設 10 = dot 差 0.1 就抵掉 1m 的距離差")]
        [SerializeField]
        private float _aimWeight = 10f;

        [Tooltip("距離的權重係數（分數會減掉 distance * 此值），同視線角下當 tiebreaker")]
        [SerializeField]
        private float _distanceWeight = 1f;

        [Tooltip(
            "視線對齊度低於此值時不加視線分（aimScore 視為 0），但不排除候選 —— "
                + "排除候選是 [If] 條件的職責。預設 0 = 側面 90° 之外的東西不吃視線加分"
        )]
        [SerializeField]
        private float _minAimDot;

        //--- 以下純除錯觀察用，不參與邏輯 ---

        [Title("除錯：最後一輪計分結果")]
        [ShowInInspector]
        private AimScoreState _lastState = AimScoreState.NotEvaluatedYet;

        [ShowInInspector]
        private GeneralEffectReceiver _debugBestReceiver;

        [ShowInInspector]
        private float _debugBestScore;

        [ShowInInspector]
        private float _debugBestAimScore;

        [ShowInInspector]
        private float _debugBestDistance;

        //一輪計分（同一個 frame 內 dealer 會把所有 receiver 掃一遍）用的 latch，換 frame 就重置
        private int _debugPassFrame = -1;

        //只在第一次缺引用時吼一聲，避免每個 receiver 每幀都刷 log
        private bool _hasWarnedMissingRayTransform;
        private bool _hasWarnedZeroForward;

        public override float CalculateScore(
            GeneralEffectDealer dealer,
            GeneralEffectReceiver receiver
        )
        {
            if (receiver == null)
            {
                _lastState = AimScoreState.Failed_ReceiverIsNull;
                Debug.LogError(
                    "[AimAlignedBestMatchScorer] receiver is null，這輪不計分",
                    this
                );
                return float.MinValue;
            }

            var receiverPos = receiver.transform.position;
            var distance = Vector3.Distance(dealer.transform.position, receiverPos);

            var aimScore = 0f;
            var state = AimScoreState.Ok;

            if (_aimRayTransform == null)
            {
                state = AimScoreState.Fallback_NoAimRayTransform;
                if (!_hasWarnedMissingRayTransform)
                {
                    _hasWarnedMissingRayTransform = true;
                    Debug.LogWarning(
                        "[AimAlignedBestMatchScorer] _aimRayTransform 沒指，退化成只用 priority + 距離"
                            + "（等同 DefaultBestMatchScorer）。請指到 [Raycast] RaycastCache 或槍口節點",
                        this
                    );
                }
            }
            else
            {
                var rayOrigin = _aimRayTransform.position;
                var rayDir = _aimRayTransform.forward;
                var toReceiver = receiverPos - rayOrigin;
                var toReceiverSqr = toReceiver.sqrMagnitude;

                if (toReceiverSqr < 1e-6f)
                {
                    //receiver 就疊在射線原點上，方向沒有意義，當成完全對齊
                    aimScore = 1f;
                }
                else if (rayDir.sqrMagnitude < 1e-6f)
                {
                    state = AimScoreState.Fallback_ZeroForward;
                    if (!_hasWarnedZeroForward)
                    {
                        _hasWarnedZeroForward = true;
                        Debug.LogWarning(
                            "[AimAlignedBestMatchScorer] _aimRayTransform.forward 是 0，"
                                + "這輪不加視線分，退化成只用 priority + 距離",
                            this
                        );
                    }
                }
                else
                {
                    //不配 normalize()：自己除掉長度，省一次 Vector3.Normalize 的 native call
                    var dot = Vector3.Dot(rayDir, toReceiver) / Mathf.Sqrt(toReceiverSqr);
                    if (dot < _minAimDot)
                    {
                        //刻意只把視線分歸零、不回 float.MinValue：排除候選是 [If] 的職責，scorer 只管排序
                        state = AimScoreState.AimTermSkipped_BelowMinDot;
                    }
                    else
                    {
                        aimScore = dot;
                    }
                }
            }

            var score =
                receiver.MatchPriority * _priorityWeight
                + aimScore * _aimWeight
                - distance * _distanceWeight;

            _lastState = state;
            RecordDebugBest(receiver, score, aimScore, distance);
            return score;
        }

        private void RecordDebugBest(
            GeneralEffectReceiver receiver,
            float score,
            float aimScore,
            float distance
        )
        {
            var frame = Time.frameCount;
            var isNewPass = _debugPassFrame != frame;
            if (isNewPass)
            {
                _debugPassFrame = frame;
                _debugBestReceiver = null;
                _debugBestScore = float.MinValue;
            }

            if (!isNewPass && score <= _debugBestScore)
                return;

            _debugBestReceiver = receiver;
            _debugBestScore = score;
            _debugBestAimScore = aimScore;
            _debugBestDistance = distance;
        }
    }
}
