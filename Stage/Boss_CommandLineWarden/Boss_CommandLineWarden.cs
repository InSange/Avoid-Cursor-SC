using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;

[RequireComponent(typeof(SpriteRenderer), typeof(CursorStateController))]
public class Boss_CommandLineWarden : BaseBoss, IUnlockProvider
{
    [Header("Movement Settings")]
    [Tooltip("보스의 스폰 좌표 (화면 안)")]
    public float SpawnX = 9f;
    [Tooltip("보스가 최종 멈출 X 좌표 (화면 안)")]
    public float EntryX = 5f;
    [Tooltip("Intro/Walk 속도")]
    public float IntroWalkSpeed = 1.2f, WalkSpeed = 2f;

    [Header("Teleport Settings")]
    [Tooltip("Teleport 후 스폰될 X 좌표 절대값")]
    public float OffscreenX = 9f;
    [Tooltip("Teleport 후 Y 좌표 범위")]
    public float TeleportMinY = -3f, TeleportMaxY = 3f;

    [Header("Delay Settings (초)")]
    [Tooltip("Idle 상태 후 최소 대기 시간")]
    public float MinIdleDelay = 1f;
    [Tooltip("Idle 상태 후 최대 대기 시간")]
    public float MaxIdleDelay = 3f;
    [Tooltip("라이트닝 어택 간격")]
    public float LightningStepDelay = 0.5f;

    // 보스가 자신의 공격 프리팹을 직접 들고 있도록 변수 추가
    [Header("보스 공격 패턴 프리팹")]
    public Boss_Swip SwipPrefab;
    public Boss_Lightning LightningPrefab;


    // 내부용 코루틴 레퍼런스
    private Coroutine _idleCoroutine;
    private Coroutine _attack2Routine; // Lightning패턴용 코루틴

    public SpriteRenderer SpriteRenderer;

    protected override void Awake()
    {
        base.Awake();
        SpriteRenderer = GetComponent<SpriteRenderer>();
    }

    #region 애니메이션 이벤트 및 완료 제어
    protected override void HandleAnimationEvent(string eventName)
    {
        if (eventName == "SpawnSwip")
        {
            SpawnSwip();
        }
        else if(eventName == "SpawnLightning")
        {
            SpawnLightning();
        }
    }

    public override void HandleStateAnimationComplete(CursorState state)
    {
        switch (state)
        {
            case CursorState.Intro:
                base.HandleStateAnimationComplete(state); // IDle로 전환후 OnIntroComplete 호출.
                break;
            case CursorState.Die:
                base.HandleStateAnimationComplete(state);
                break;
            case CursorState.Walk:
                ChangeToIdle();
                break;
            case CursorState.Teleport:
                OnTeleportEnd();
                break;
            case CursorState.Attack1:
            case CursorState.Attack2:
                ChangeToIdle();
                break;
        }
    }
    #endregion

    public override Vector2 GetSpawnPosition()
    {
        // 50% 확률로 왼쪽(-12) 또는 오른쪽(12)
        bool spawnRight = Random.value > 0.5f;

        // 등장 목표 지점(EntryX)도 방향에 맞춰 설정
        EntryX = spawnRight ? 5f : -5f;
        SpawnX = spawnRight ? 9f : -9f;

        // 실제 스폰 좌표 반환
        return new Vector2(SpawnX, Random.Range(-3f, 3f));
    }

    public override void EnterIntroPhase()
    {
        base.EnterIntroPhase();
        SetFacingDirection(transform.position.x < EntryX);

        float dist = Mathf.Abs(EntryX - SpawnX);
        float animDuration = GetAnimationDuration(CursorState.Intro);

        float calculatedSpeed = dist / (animDuration / CursorObject.PlaybackSpeed);
        
        StartCoroutine(MoveTo(new Vector2(EntryX, transform.position.y), calculatedSpeed, OnIntroArrived));
    }
    private void OnIntroArrived()
    {
        CursorFSM.ChangeState(CursorState.Idle);
    }

    private IEnumerator MoveTo(Vector2 target, float speed, System.Action onArrived)
    {
        while ((Vector2)transform.position != target)
        {
            transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        onArrived?.Invoke();
    }

    protected override void OnIntroComplete()
    {
        Debug.Log($"{gameObject.name}보스 등장 완료");

        ChangeToIdle();
    }

    private void ChangeToIdle()
    {
        // 1) 애니메이션 상태 전환
        CursorFSM.ChangeState(CursorState.Idle);

        // 2) 이전에 돌고 있던 Idle 코루틴 정지
        if (_idleCoroutine != null)
            StopCoroutine(_idleCoroutine);

        // 3) 랜덤 딜레이 후 액션 결정
        _idleCoroutine = StartCoroutine(IdleWaitAndDecide());
    }

    private IEnumerator IdleWaitAndDecide()
    {
        // 1) 랜덤 딜레이
        float delay = Random.Range(MinIdleDelay, MaxIdleDelay);
        yield return new WaitForSeconds(delay);

        // 2) 보스가 아직 살아 있고, 실제 Idle 상태라면 다음 행동
        if (IsAlive && CursorFSM.CurrentState == CursorState.Idle)
            DecideNextAction();
    }

    private void DecideNextAction()
    {
        float r = Random.value;
        if (r < .5f) StartAttack(CursorState.Attack1);
        else if (r < .8f) StartAttack(CursorState.Attack2);
        else StartTeleport();
    }

    void StartAttack(CursorState attackState)
    {
        CursorFSM.ChangeState(attackState);
        // 패턴 실행은 따로
    }

    /// <summary>
    /// Attack1 애니메이션 타임라인에 "SpawnSwip" 이벤트로 호출될 메서드
    /// </summary>
    public void SpawnSwip()
    {
        if (SwipPrefab == null) return;
        if (PoolingControllerBase.Current == null) return;

            // GetOrCreatePool에 자신의 프리팹을 전달
            var swip = PoolingControllerBase
                    .Current
                    .GetOrCreatePool(SwipPrefab) // (구: GetPool<Boss_Swip>())
                    .Get();

        if (swip == null) return;
        var swipCursor = swip.GetComponent<CursorObject>();
        if (swipCursor != null)
        {
            // 보스의 현재 재생 속도를 스킬에 그대로 적용
            swipCursor.PlaybackSpeed = this.CursorObject.PlaybackSpeed;
        }

        // 2) 화면 절반 중심 위치 계산
        bool bossOnRight = transform.position.x > 0;
        float x = bossOnRight ? 4f : -4f;
        swip.transform.position = new Vector2(x, 0);
        swip.transform.rotation = bossOnRight ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180f, 0);
    }

    /// <summary>
    /// Attack2 애니메이션 타임라인에 "SpawnLightning" 이벤트로 호출될 메서드
    /// </summary>
    public void SpawnLightning()
    {
        _attack2Routine = StartCoroutine(LightningAttackSequence());
    }

    private IEnumerator LightningAttackSequence()
    {
        Vector2 playerPos = AvoidCursorGameManager.Instance.PlayerCursor.position;
        Vector2 offset = new Vector2(0.25f, 1.0f);

        // 1) 첫 번개: 플레이어 바로 위
        LightningAttack(playerPos + offset);
        yield return new WaitForSeconds(LightningStepDelay);

        float ratio = (float)CurrentHP / MaxHP;

        if (ratio > 0.66f)
        {
            // 체력 66~100% 구간: 첫 번개만
        }
        else if (ratio > 0.33f)
        {
            // 체력 33~66% 구간: 대각선 X 패턴, 2 레이어
            float diagDistance = 2f; // 레이어 간격
            Vector2[] diagDirs = {
                new Vector2( 1,  1),
                new Vector2(-1,  1),
                new Vector2(-1, -1),
                new Vector2( 1, -1),
            };

            for (int layer = 1; layer <= 2; layer++)
            {
                float d = diagDistance * layer;
                foreach (var dir in diagDirs)
                {
                    LightningAttack(playerPos + dir.normalized * d);
                }
                yield return new WaitForSeconds(LightningStepDelay);
            }
        }
        else
        {
            // 체력 0~33% 구간: 원형으로 퍼져나가기 (첫 파도 건너뛰고, 충분한 간격)
            int waves = 3;
            int perWave = 8;
            float radiusStep = 2.5f; // 중심과 충분히 띄운 반경

            for (int w = 1; w <= waves; w++)
            {
                float r = radiusStep * w;
                for (int i = 0; i < perWave; i++)
                {
                    float angle = 360f / perWave * i;
                    Vector2 dir = new Vector2(
                        Mathf.Cos(angle * Mathf.Deg2Rad),
                        Mathf.Sin(angle * Mathf.Deg2Rad));
                    LightningAttack(playerPos + dir * r);
                }
                yield return new WaitForSeconds(LightningStepDelay);
            }
        }
    }

    void LightningAttack(Vector2 Pos)
    {
        if (LightningPrefab == null) return;

        // GetOrCreatePool에 자신의 프리팹을 전달
        var LightNing = PoolingControllerBase
            .Current
            .GetOrCreatePool(LightningPrefab) // (구: GetPool<Boss_Lightning>())
            .Get();

        if (LightNing == null) return;

        var lnCursor = LightNing.GetComponent<CursorObject>();
        if (lnCursor != null)
        {
            lnCursor.PlaybackSpeed = this.CursorObject.PlaybackSpeed;
        }

        LightNing.transform.position = Pos;
    }


    void StartTeleport()
    {
        CursorFSM.ChangeState(CursorState.Teleport);
    }

    void OnTeleportEnd()
    {
        // 1) 랜덤 좌/우 offscreen 스폰
        bool fromLeft = Random.value < .5f;
        float x = fromLeft ? -OffscreenX : +OffscreenX;
        float y = Random.Range(TeleportMinY, TeleportMaxY);
        EntryX = fromLeft ? -5 : 5;
        transform.position = new Vector2(x, y);

        // 2) 바라볼 방향 설정
        SetFacingDirection(fromLeft);

        // 3) Walk → Entry 포인트까지 이동
        CursorFSM.ChangeState(CursorState.Walk);

        float distance = Vector2.Distance(transform.position, new Vector2(EntryX, y));
        float animDuration = GetAnimationDuration(CursorState.Walk);

        float calculatedSpeed = distance / (animDuration / CursorObject.PlaybackSpeed);

        StartCoroutine(MoveTo(new Vector2(EntryX, y), calculatedSpeed, () =>
        {
            ChangeToIdle();
        }));
    }

    /// <summary>
    /// true면 오른쪽(face right), false면 왼쪽(face left)
    /// </summary>
    private void SetFacingDirection(bool faceRight)
    {
        // 기본 스프라이트는 왼쪽 바라보므로, 오른쪽이면 flipX = true
        transform.rotation = faceRight ? Quaternion.Euler(0, 180f, 0) : Quaternion.Euler(0, 0, 0);
    }

    public override void UseSkillPattern()
    {
        // 기존 페이즈별 스킬 루프 구현
    }

    /// <summary>
    /// FSM에 등록된 해당 상태의 애니메이션 길이를 반환합니다.
    /// </summary>
    private float GetAnimationDuration(CursorState state)
    {
        // CursorStateController의 Animations 배열을 순회하거나 딕셔너리 참조
        // (BaseBoss나 Controller에 public 접근자가 필요할 수 있음)
        // 여기서는 간단히 CursorFSM의 inspector 배열을 순회한다고 가정 (비효율적이지만 확실함)

        foreach (var entry in CursorFSM.Animations)
        {
            if (entry.State == state && entry.Animation != null)
            {
                // 프레임 수 * 프레임당 시간
                if (entry.Animation.Frames.Length > 0)
                    return entry.Animation.Frames.Length * entry.Animation.Frames[0].Duration;
            }
        }
        return 1.0f; // 기본값 (오류 방지)
    }

    public IEnumerable<UnlockID> GetUnlocksOnDefeat()
    {
        // IO 시스템 보스 처치 시, 해당 NPC를 해금합니다.
        yield return UnlockID.BossNPC_CommandLineWarden;
    }
}
