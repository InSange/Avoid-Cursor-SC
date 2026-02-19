using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CursorObject))]
public class Boss_Lightning : MonoBehaviour
{
    #region 모든 프리팹 고정일듯
    [Header("Animation & Damage Settings")]
    public CursorAnimation LightningAnimation;    // 위에서 만든 SO
    public int Damage = 1;
    public LayerMask TargetLayer;   // ex) “Player” 레이어만 설정

    [SerializeField] private CursorObject CursorObject;
    [SerializeField] private SpriteRenderer Sprite;
    [SerializeField] private Vector2 SwipSize;
    [SerializeField] private Vector3 LightNingAttackOffset = new Vector3(0.15f, 0.6f, 0);
    [SerializeField] private Vector3 LightNingAttackSize = new Vector3(0.9f, 1.5f, 0);

    void Awake()
    {
        CursorObject = GetComponent<CursorObject>();
        Sprite = GetComponent<SpriteRenderer>();
        // 프레임 이벤트 & 완료 이벤트 구독
        CursorObject.OnAnimationEvent += HandleAnimationEvent;
        CursorObject.OnAnimationComplete += HandleAnimationComplete;

        TargetLayer = LayerMask.GetMask("Player");
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
        CursorObject.PlayAnimation(LightningAnimation);
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
            Vector2 center = (Vector2)(transform.position - LightNingAttackOffset);
            Collider2D[] hits = Physics2D.OverlapBoxAll(
                center,
                LightNingAttackSize,
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
        PoolingControllerBase.Current.GetPool<Boss_Lightning>()
                             .Release(this);
    }
    #endregion

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position - LightNingAttackOffset, LightNingAttackSize);
    }
}