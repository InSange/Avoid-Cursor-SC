using System.Collections;
using UnityEngine;

/// <summary>
/// 모래시계 캐릭터 로직
/// - 느린 이동 속도
/// - 자동 회전 공격 (좌클릭 없음)
/// - 우클릭 시 정지 및 무적 (Stasis)
/// </summary>
public class PlayerLogic_Hourglass : PlayerLogicBase
{
    [Header("Hourglass Settings")]
    public float HourglassMoveSpeed = 12.0f; // 기본보다 느린 속도

    [Header("Auto Attack")]
    public float AttackInterval = 2.0f;  // 공격 주기
    public float AttackRadius = 2.5f;    // 공격 범위
    public int AttackDamage = 3;         // 공격력 (기본보다 강함)

    private float _attackTimer = 0f;

    [Header("Skill (Stasis)")]
    public float StasisDuration = 5f;  // 무적 지속 시간
    public override float SkillStaminaCost => 40f; // 스킬 소모 스태미나

    protected override void Start()
    {
        base.Start();

        Manager.BaseMoveSpeed = HourglassMoveSpeed;
    }

    protected override void Update()
    {
        base.Update();

        // 스킬 사용 중(정지 상태)이거나 죽었으면 공격 카운트 안 함
        if (IsDead || Manager.IsMovementLocked || Manager.FsmController.CurrentState == CursorState.Skill1) return;

        // 자동 공격 타이머 계산
        _attackTimer += Time.deltaTime;
        if (_attackTimer >= AttackInterval)
        {
            _attackTimer = 0f;
            TriggerAutoAttack();
        }
    }

    // --- 좌클릭: 기능 없음 ---
    public override void ExecuteBasicAttack()
    {
        // 모래시계는 좌클릭으로 공격하지 않으므로 비워둡니다.
        // (혹은 "틱" 소리만 나게 하거나 UI 피드백을 줄 수도 있음)
    }

    // --- 자동 공격 시작 ---
    private void TriggerAutoAttack()
    {
        // FSM을 공격 상태로 전환 -> 애니메이션 재생
        // 애니메이션에서 "SpinHit" 이벤트를 호출해야 실제 데미지가 들어감
        Manager.FsmController.ChangeState(CursorState.AutoAttack);
    }

    // --- 우클릭: 정지장(Stasis) ---
    public override void ExecuteSkill()
    {
        StartCoroutine(StasisRoutine());
    }

    private IEnumerator StasisRoutine()
    {
        Debug.Log("[Hourglass] 정지장 발동! (무적 & 정지)");

        // 무적 설정 
        Manager.IsMovementLocked = true;
        _isInvincible = true;
        Manager.SetAnimationPaused(true);

        Color originalColor = _renderer.color;
        _renderer.color = new Color(1.0f, 0.85f, 0.2f, 1.0f);

        // 지속 시간 대기
        yield return new WaitForSeconds(StasisDuration);

        // 복구
        _isInvincible = false;
        _renderer.color = originalColor;

        Manager.SetAnimationPaused(false);
        Manager.IsMovementLocked = false;
        // Idle로 복귀
        Manager.FsmController.ChangeState(CursorState.Idle);
    }

    // --- 애니메이션 이벤트 처리 ---
    public override void HandleAnimationEvent(string eventName)
    {
        // 애니메이션 클립의 특정 프레임에 "SpinHit"이라는 이벤트를 심어야 함
        if (eventName == "SpinHit")
        {
            ExecuteSpinDamage();
        }
    }

    // 실제 광역 데미지 처리
    private void ExecuteSpinDamage()
    {
        // 원형 범위 내 적 감지
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, AttackRadius, GameAttackLayers);

        if (hits.Length > 0)
        {
            // 이펙트 생성 (옵션)
            // Instantiate(SpinEffectPrefab, transform.position, Quaternion.identity);

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<IHittable>(out var hittable))
                {
                    hittable.OnHit(AttackDamage);
                }
            }
            Debug.Log($"[Hourglass] 회전 공격! {hits.Length}마리 타격.");
        }
    }

    // 아이템 사용 (기본 로직 유지)
    public override void ExecuteItem()
    {
        base.ExecuteItem();
    }

    // 에디터에서 공격 범위 확인용
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, AttackRadius);
    }
}