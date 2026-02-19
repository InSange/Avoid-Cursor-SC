using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CursorObject))]
public class Boss_Swip : MonoBehaviour, IPoolable
{
    #region 모든 프리팹 고정일듯
    [Header("Animation & Damage Settings")]
    public CursorAnimation SwipAnimation;    // 위에서 만든 SO
    public int Damage = 1;
    public LayerMask TargetLayer;             // ex) “Player” 레이어만 설정

    [SerializeField] private CursorObject CursorObject;
    [SerializeField] private SpriteRenderer Sprite;
    [SerializeField] private Vector2 SwipSize;

    void Awake()
    {
        CursorObject = GetComponent<CursorObject>();
        Sprite = GetComponent<SpriteRenderer>();
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
        SwipSize = Sprite.bounds.size;
        CursorObject.PlayAnimation(SwipAnimation);
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
            Vector2 center = (Vector2)transform.position;
            Collider2D[] hits = Physics2D.OverlapBoxAll(
                center,
                SwipSize,
                0f,
                TargetLayer);

            foreach (var col in hits)
            {
                if (col.TryGetComponent<IHittable>(out var h))
                    h.OnHit(Damage);
            }
        }
    }

    private void HandleAnimationComplete()
    {
        // 애니메이션이 끝나면 자동으로 풀에 반환

        // (수정) BaseStageEnvironmentController -> PoolingControllerBase
        PoolingControllerBase.Current.GetPool<Boss_Swip>()
                             .Release(this);
    }
    #endregion

    // (선택) 디버그용으로 편하게 영역 시각화
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, Sprite.bounds.size);
    }
}
