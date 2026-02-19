using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FakeCursorBlink : MonoBehaviour, IPoolable
{
    #region 모든 프리팹 고정일듯
    [Header("Animation & Damage Settings")]
    public CursorAnimation BlinkAnimation;    // 위에서 만든 SO
    public int Damage = 1;
    public float EffectRadius = 0.5f;
    public LayerMask TargetLayer;             // ex) “Player” 레이어만 설정

    private CursorObject CursorObject;

    void Awake()
    {
        CursorObject = GetComponent<CursorObject>();
        // 프레임 이벤트 & 완료 이벤트 구독
        CursorObject.OnAnimationEvent += HandleAnimationEvent;
        CursorObject.OnAnimationComplete += HandleAnimationComplete;
    }

    // 실행
    private void OnEnable()
    {
        OnSpawn();
    }

    // 풀에서 꺼낼 때 자동으로 애니메이션 재생
    public void OnSpawn()
    {
        CursorObject.PlayAnimation(BlinkAnimation);
    }

    // 풀로 반환될 때 이벤트 구독 해제 등 클린업
    public void OnDespawn()
    {
        // 필요 시 상태 초기화
        CursorObject.OnAnimationEvent -= HandleAnimationEvent;
        CursorObject.OnAnimationComplete -= HandleAnimationComplete;
    }


    private void HandleAnimationEvent(string eventName)
    {
        if (eventName == "Damage")
        {
            Debug.Log($"{gameObject.name} 데미지 주기!");
            // 꽉 찬 사각형 프레임(2번째)일 때 호출
            // 반경 EffectRadius 내에 플레이어가 있으면 OnHit
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                transform.position,
                EffectRadius,
                TargetLayer);

            foreach (var col in hits)
            {
                var hittable = col.GetComponent<IHittable>();
                if (hittable != null)
                    hittable.OnHit(Damage);
            }
        }
    }

    private void HandleAnimationComplete()
    {
        PoolingControllerBase.Current.GetPool<FakeCursorBlink>()
                                     .Release(this);
    }
    #endregion

    // (선택) 디버그용으로 편하게 영역 시각화
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, EffectRadius);
    }
}