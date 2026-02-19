using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss_DemonSword : BaseBoss, IUnlockProvider
{
    [Header("Delay Settings (초)")]
    [Tooltip("Idle 상태 후 최소 대기 시간")]
    public float MinIdleDelay = 0.5f;
    [Tooltip("Idle 상태 후 최대 대기 시간")]
    public float MaxIdleDelay = 1.5f;
    [Header("연계 공격 횟수")]
    [Tooltip("최대 콤보 횟수. 이 횟수에 도달하면 강제로 Idle 상태로 전환됩니다.")]
    public int MaxComboCount = 4;

    [Header("Chase Settings")]
    [Tooltip("추격 상태로 전환할 확률 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float ChaseChance = 0.3f; // 30% 확률로 추격
    public float ChaseSpeed = 5f;
    public float ChaseDuration = 2f; // 추격 지속 시간
    public float OptimalAttackDistance = 1.2f; // 이 거리 안으로 들어오면 추격을 멈추고 공격
    public float CheckOptimalAttackDistance = 5f; // 공격 범위 기즈모 체크용도 -> 공격패턴 1,2,3,4

    [Header("Attack Patterns")]
    [Tooltip("시작기로 사용할 수 있는 모든 공격 패턴 목록")]
    public List<AttackPattern> OpenerAttacks;
    [Tooltip("특수 광역기 공격 패턴")]
    public AttackPattern SpecialAttack;

    [Header("Attack Hitboxes")]
    public Vector2 Attack1_HitboxSize = new Vector2(4.2f, 2.3f);
    public Vector2 Attack1_HitboxOffset = new Vector2(0.2f, -0.4f);
    public Vector2 Attack2_HitboxSize = new Vector2(3.6f, 2.6f);
    public Vector2 Attack2_HitboxOffset = new Vector2(0f, -0.2f);
    public Vector2 Attack3_HitboxSize = new Vector2(6.8f, 2.3f);
    public Vector2 Attack3_HitboxOffset = new Vector2(2.6f, -0.45f);
    public Vector2 Attack4_HitboxSize = new Vector2(3.1f, 2.8f);
    public Vector2 Attack4_HitboxOffset = new Vector2(0.63f, -0.45f);
    public int AttackDamage = 1; // 모든 공격의 기본 데미지

    // --- 내부 상태 변수 ---
    private int _currentComboCount = 0;
    private Coroutine _actionCoroutine;
    private Transform _playerTransform;
    private AttackPattern _currentAttackPattern; // 현재 실행 중인 공격 패턴 저장

    protected override void Start()
    {
        base.Start();
        _playerTransform = AvoidCursorGameManager.Instance.PlayerCursor.transform;
    }

    #region 애니메이션 이벤트 및 완료 제어
    protected override void HandleAnimationEvent(string eventName)
    {
        switch (eventName)
        {
            case "Attack1":
                PerformHitCheck(Attack1_HitboxOffset, Attack1_HitboxSize, AttackDamage);
                break;
            case "Attack2":
                PerformHitCheck(Attack2_HitboxOffset, Attack2_HitboxSize, AttackDamage);
                break;
            case "Attack3":
                PerformHitCheck(Attack3_HitboxOffset, Attack3_HitboxSize, AttackDamage);
                break;
            case "Attack4":
                PerformHitCheck(Attack4_HitboxOffset, Attack4_HitboxSize, AttackDamage);
                break;
            case "Teleport":
                PerformTeleport();
                break;
                // "Damage_Dash", "Damage_Wide" 등 필요한 만큼 추가
        }
    }

    public override void HandleStateAnimationComplete(CursorState state)
    { // 애니메이션 완료 후 처리입니다잉
        base.HandleStateAnimationComplete(state);

        if (IsAttackState(state))
        {
            DecideChainAction();
        }
    }
    #endregion

    private bool IsAttackState(CursorState state)
    {
        // CursorState에 정의된 모든 공격 상태를 여기에 포함시키세요.
        return state == CursorState.Attack1 ||
               state == CursorState.Attack2 ||
               state == CursorState.Attack3 ||
               state == CursorState.Attack4;
    }

    private void DecideChainAction()
    {
        if (_currentAttackPattern == null) { ChangeToIdle(); return; }

        if (_currentComboCount >= MaxComboCount || Random.value > _currentAttackPattern.ComboContinueChance)
        {
            ChangeToIdle();
            return;
        }

        if (_currentAttackPattern.PossibleNextChains.Count > 0)
        {
            var nextAttack = _currentAttackPattern.PossibleNextChains[Random.Range(0, _currentAttackPattern.PossibleNextChains.Count)];
            StartAttack(nextAttack);
        }
        else
        {
            ChangeToIdle();
        }
    }

    protected override void OnIntroComplete()
    {
        Debug.Log($"{gameObject.name}보스 등장 완료");

        ChangeToIdle();
    }

    private void ChangeToIdle()
    {
        CursorFSM.ChangeState(CursorState.Idle);
        _currentComboCount = 0;

        if (_actionCoroutine != null) StopCoroutine(_actionCoroutine);
        _actionCoroutine = StartCoroutine(IdleWaitAndDecideNext());
    }

    private IEnumerator IdleWaitAndDecideNext()
    {
        float delay = Random.Range(MinIdleDelay, MaxIdleDelay);
        yield return new WaitForSeconds(delay);

        if (IsAlive && CursorFSM.CurrentState == CursorState.Idle)
        {
            // 💡 NEW: 추격할지, 바로 공격할지 결정
            if (Random.value < ChaseChance)
            {
                StartChase();
            }
            else
            {
                DecideOpenerAction();
            }
        }
    }

    private void StartChase()
    {
        if (_actionCoroutine != null) StopCoroutine(_actionCoroutine);
        _actionCoroutine = StartCoroutine(ChaseRoutine());
    }

    private IEnumerator ChaseRoutine()
    {
        CursorFSM.ChangeState(CursorState.Walk); // 추격 시 걷는 애니메이션 재생
        float timer = 0f;

        while (timer < ChaseDuration)
        {
            // 플레이어와의 거리가 최적 공격 거리보다 가까워지면 추격 중단하고 바로 공격
            if (Vector2.Distance(transform.position, _playerTransform.position) <= OptimalAttackDistance)
            {
                break;
            }

            // 플레이어 방향으로 이동
            Vector2 direction = (_playerTransform.position - transform.position).normalized;
            transform.position = Vector2.MoveTowards(transform.position, _playerTransform.position, ChaseSpeed * Time.deltaTime);
            SetFacingDirection(direction.x > 0); // 방향 전환

            timer += Time.deltaTime;
            yield return null;
        }

        // 추격이 끝나면 다음 행동(공격)을 결정
        DecideOpenerAction();
    }

    protected virtual void SetFacingDirection(bool faceRight)
    {
        float yRotation = faceRight ? 0f : 180f;
        transform.rotation = Quaternion.Euler(0, yRotation, 0);
    }

    private void DecideOpenerAction()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, _playerTransform.position);
        var availableOpeners = new List<AttackPattern>();

        foreach (var pattern in OpenerAttacks)
        {
            if (distanceToPlayer >= pattern.MinDistance && distanceToPlayer <= pattern.MaxDistance)
            {
                availableOpeners.Add(pattern);
            }
        }

        if (availableOpeners.Count > 0)
        {
            StartAttack(availableOpeners[Random.Range(0, availableOpeners.Count)]);
        }
        else
        {
            // 마땅한 공격이 없으면 다시 Idle (혹은 추격)
            ChangeToIdle();
        }
    }

    void StartAttack(AttackPattern pattern)
    {
        if (pattern == null) return;

        Vector2 direction = (_playerTransform.position - transform.position).normalized;
        SetFacingDirection(direction.x > 0); // 방향 전환
        _currentAttackPattern = pattern; // 💡 현재 공격 패턴 저장
        _currentComboCount++;
        CursorFSM.ChangeState(pattern.State);
    }

    private void PerformHitCheck(Vector2 offset, Vector2 size, int damage)
    {
        // 보스가 바라보는 방향에 따라 오프셋의 x값을 반전
        bool isFacingRight = Mathf.Abs(transform.rotation.eulerAngles.y) < 90f;
        float facingDir = isFacingRight ? 1f : -1f;

        Vector2 finalOffset = new Vector2(offset.x * facingDir, offset.y);
        Vector2 center = (Vector2)transform.position + finalOffset;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, LayerMask.GetMask("Player"));

        foreach (var col in hits)
        {
            if (col.TryGetComponent<IHittable>(out var hittable))
            {
                hittable.OnHit(damage);
            }
        }
    }

    /// <summary>
    /// 'Teleport' 애니메이션 이벤트 발생 시, 조건에 맞춰 플레이어에게 순간이동합니다.
    /// </summary>
    public void PerformTeleport()
    {
        // --- 1. 방향 및 거리 계산 ---

        // 보스의 정면 방향을 나타내는 벡터 (Y축 회전 값을 기준으로)
        // 오른쪽을 바라보면 (1, 0), 왼쪽을 바라보면 (-1, 0)이 됩니다.
        Vector2 bossForward = transform.rotation.y == 0f ? Vector2.right : Vector2.left;

        // 보스에서 플레이어로 향하는 방향 벡터
        Vector2 directionToPlayer = (_playerTransform.position - transform.position).normalized;

        // --- 2. 방향 일치 여부 확인 (핵심 로직) ---

        // 내적(Dot Product)을 사용하여 플레이어가 보스의 정면에 있는지 확인합니다.
        // 결과가 양수(+)이면 정면, 음수(-)이면 후면에 있다는 의미입니다.
        float dotProduct = Vector2.Dot(bossForward, directionToPlayer);

        // 플레이어가 보스의 정면에 있지 않다면(뒤에 있다면), 함수를 즉시 종료합니다.
        if (dotProduct <= 0)
        {
            // 제자리에 머무릅니다.
            Debug.Log("플레이어가 등 뒤에 있어 텔레포트하지 않습니다.");
            return;
        }

        // --- 3. 조건부 위치 결정 및 이동 ---

        float distanceToPlayer = Vector2.Distance(transform.position, _playerTransform.position);
        float maxTeleportRange = 5.0f;
        Vector2 targetPosition;

        if (distanceToPlayer <= maxTeleportRange)
        {
            // 조건 1: 최대 사거리 안 -> 플레이어의 위치로 직접 이동
            targetPosition = _playerTransform.position;
        }
        else
        {
            // 조건 2: 최대 사거리 밖 -> 플레이어 방향으로 5.0f만큼만 이동
            targetPosition = (Vector2)transform.position + directionToPlayer * maxTeleportRange;
        }

        // --- 4. 실제 이동 및 후처리 ---

        // (선택사항) 순간이동 시각/청각 효과(VFX/SFX)를 여기에 추가할 수 있습니다.
        // 예: Instantiate(TeleportVFX, transform.position, Quaternion.identity);

        // 보스의 위치를 최종 목표 지점으로 설정
        transform.position = targetPosition;

        // 이동 후, 플레이어를 정확히 바라보도록 방향을 다시 설정
        SetFacingDirection(directionToPlayer.x > 0);

        Debug.Log($"텔레포트 완료! 최종 위치: {targetPosition}");
    }

    void OnDrawGizmosSelected()
    {
        // 예시: 가로 베기 히트박스 시각화
        /*Gizmos.color = Color.yellow;
        float facingDirection = transform.localScale.x > 0 ? 1f : -1f;
        Vector2 finalOffset = new Vector2(Attack1_HitboxOffset.x * facingDirection, Attack1_HitboxOffset.y);
        Gizmos.DrawWireCube((Vector2)transform.position + finalOffset, Attack1_HitboxSize);*/

        // 💡 NEW: OptimalAttackDistance를 시안(Cyan) 색상의 원으로 표시
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, OptimalAttackDistance);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, CheckOptimalAttackDistance);
    }

    public override void UseSkillPattern()
    {
        // 기존 페이즈별 스킬 루프 구현
    }

    public IEnumerable<UnlockID> GetUnlocksOnDefeat()
    {
        // 데몬 소드 처치 시, NPC와 '파이어 소드' 아이템을 해금합니다.
        yield return UnlockID.BossNPC_DemonSword;
        yield return UnlockID.Item_FireSword;
    }
}
