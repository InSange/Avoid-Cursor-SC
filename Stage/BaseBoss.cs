using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CursorStateController))]
public abstract class BaseBoss : MonoBehaviour, IHittable
{
    public event Action OnBossDefeated; // 보스 사망했을 때
    public event Action<BaseBoss> OnCombatStarted; // 보스 NPC에서 적대

    [Header("보스 기본 설정")]
    public int MaxHP = 10;
    [SerializeField] protected int CurrentHP;
    [SerializeField] protected BossPhase CurrentPhase = BossPhase.Intro;
    protected bool IsAlive = false;

    [Header("환경 패턴 데이터")]
    public List<EnvironmentPatternBase> EnvironmentPatterns;

    [Header("보스 애니메이션 설정")]
    [SerializeField] protected CursorStateController CursorFSM;
    [SerializeField] protected CursorObject CursorObject;

    [Tooltip("true이면 전투 상태, false이면 갤러리의 평화(NPC) 상태입니다.")]
    [SerializeField] public bool IsHostile { get; protected set; } = false; //IsHostile 추가
    //도발 진행 중인지 확인하는 플래그
    protected bool _isProvoking = false;

    [Header("NPC 설정")]
    [TextArea] public string HostileDialogue = "감히... 후회하게 만들어주마.";

    protected BossGalleryManager GalleryManagerRef;
    private Coroutine SkillCoroutine;

    protected virtual void Awake()
    {
        CurrentHP = MaxHP;
        IsAlive = true;

        CursorObject = GetComponent<CursorObject>();
        CursorObject.OnAnimationEvent += HandleAnimationEvent;

        CursorFSM = GetComponent<CursorStateController>();
        // FSM 애니메이션 완료 이벤트 구독
        if (CursorFSM != null)
            CursorFSM.OnStateAnimationComplete += HandleStateAnimationComplete;
    }

    protected virtual void Start()
    {
        Debug.Log("스타트");
        //EnterIntroPhase();
    }

    /// <summary>
    /// InfiniteModeManager가 보스를 스폰할 때 호출하여 위치를 받아옵니다.
    /// </summary>
    public virtual Vector2 GetSpawnPosition()
    {
        // 기본값: 화면 상단 무작위 (기존 로직)
        return new Vector2(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-3f, 3f));
    }

    /// <summary>
    ///(InfiniteModeManager가 호출) 즉시 '전투' 모드로 보스를 시작합니다.
    /// </summary>
    public virtual void EnterIntroPhase()
    {
        Debug.Log("보스 인트로 시작 (전투 모드)");
        IsHostile = true;   // 전투 모드 활성화
        CurrentPhase = BossPhase.Intro;
        IsAlive = true;
        CursorFSM?.ChangeState(CursorState.Intro);
    }
    
    /// <summary>
    /// (BossGalleryManager가 호출) '평화(NPC)' 모드로 보스를 초기화합니다.
    /// </summary>
    public virtual void InitializeAsNPC(BossGalleryManager manager)
    {
        Debug.Log("보스 NPC 모드로 초기화");
        GalleryManagerRef = manager;
        IsHostile = false; // 평화 모드 활성화
        IsAlive = true;
        CurrentPhase = BossPhase.Phase1; // Intro 페이즈 스킵
        CursorFSM?.ChangeState(CursorState.Idle); // Idle 상태로 시작
    }

    public virtual void HandleStateAnimationComplete(CursorState state)
    {
        if (state == CursorState.Intro)
        {
            // 1) Intro 끝나면 Idle로 전환
            CursorFSM.ChangeState(CursorState.Idle);
            // 2) 등장 연출 완료 이벤트
            OnIntroComplete();
        }
        else if (state == CursorState.Die)
        {
            if (IsHostile) // 전투 중 사망 시에만 이벤트 호출
                OnBossDefeated?.Invoke();

            CursorObject.OnAnimationEvent -= HandleAnimationEvent;
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 등장 애니메이션 완료 시 호출
    /// </summary>
    protected virtual void OnIntroComplete()
    {
        Debug.Log("보스 등장 완료");
        UpdatePhase();
    }

    protected virtual void HandleAnimationEvent(string eventName)
    {
/*        if (eventName == "IntroEnd")
        {
            OnIntroComplete();
        }*/
    }

    /// <summary>
    /// 데미지 처리 (Intro 중엔 무적)
    /// </summary>
    public virtual void OnHit(int damage)
    {
        if (!IsAlive) return;

        if (IsHostile)
        {
            // --- '전투' 상태일 때 (기존 로직) ---
            if (CursorFSM.CurrentState == CursorState.Intro)
                return; // 인트로 중 무적

            CurrentHP -= damage;
            UpdatePhase();
            Debug.Log($"[BaseBoss] 피격됨! 남은 체력: {CurrentHP}");

            if (CurrentHP <= 0)
            {
                Die();
            }
        }
        else if (!_isProvoking)
        {
            Debug.Log($"[BaseBoss] {gameObject.name} 도발 시퀀스 시작!");

            // 중복 실행 방지
            _isProvoking = true;

            // 대화 상호작용만 중단 (창은 끄지 않음)
            if (GalleryManagerRef != null && GalleryManagerRef.Terminal != null)
            {
                GalleryManagerRef.Terminal.AbortInteraction();
            }

            // 적대화 대사 및 전투 전환 코루틴 시작
            StartCoroutine(TriggerHostileSequence());
        }
    }

    private IEnumerator TriggerHostileSequence()
    {
        // 1. 터미널을 통해 적대 대사 출력
        // (TerminalManager가 닫히지 않았다면 텍스트 출력, 닫혔다면 다시 잠깐 켜서 출력할 수도 있음)
        // 여기서는 BossGalleryManager를 통해 터미널에 접근합니다.
        if (GalleryManagerRef != null && GalleryManagerRef.Terminal != null)
        {
            // 터미널을 잠시 활성화 (혹시 꺼졌다면)
            GalleryManagerRef.Terminal.TerminalCanvas.SetActive(true);

            string bossName = GetType().Name.ToUpper();
            GalleryManagerRef.Terminal.PrintBossAngry(bossName, HostileDialogue);

            // 대사를 읽을 시간 (예: 2초) 대기
            yield return new WaitForSeconds(2.0f);

            // 터미널 종료
            GalleryManagerRef.ForceStopDialogue(this);
        }
        else
        {
            // 터미널이 없다면 짧게 대기
            yield return new WaitForSeconds(0.5f);
        }

        // 2. 전투 돌입 (Intro 생략하고 바로 Idle/패턴 시작)
        EnterCombatPhase();
    }

    protected virtual void EnterCombatPhase()
    {
        Debug.Log("보스 전투 태세 돌입!");
        _isProvoking = false; // 도발 끝
        IsHostile = true;

        // 전투시작 알림 -> 매니저 패턴실행
        OnCombatStarted?.Invoke(this);

        // Intro 애니메이션 대신 바로 Idle로 시작
        CursorFSM?.ChangeState(CursorState.Idle);

        // IntroComplete를 수동으로 호출하여 초기 패턴 루프 시작
        OnIntroComplete();
    }

    /// <summary>
    /// 페이즈 전환 로직 (체력 비율 등)
    /// </summary>
    protected virtual void UpdatePhase()
    {
        // 기본 구현 없음. 자식에서 페이즈 분기
    }

    /// <summary>
    /// 페이즈가 바뀔 때마다 호출
    /// </summary>
    protected virtual void OnPhaseChanged(BossPhase newPhase) { }

    /// <summary>
    /// 사망 처리: Die 애니메이션 진입
    /// </summary>
    protected virtual void Die()
    {
        Debug.Log("보스 사망!");
        IsAlive = false;
        CurrentPhase = BossPhase.Die;

        if (IsHostile) // 전투 중일 때만 스킬 루프 정지
            StopSkillLoop();

        CursorFSM?.ChangeState(CursorState.Die);

        //Destroy(gameObject, 0.5f);
    }
    /// <summary>
    /// 페이즈별 스킬을 실행할 추상 메서드
    /// </summary>
    public abstract void UseSkillPattern();

    /// <summary>
    /// 스킬 루프 시작 (interval 간격으로 UseSkillPattern 호출)
    /// </summary>
    protected void StartSkillLoop(float interval)
    {
        if (SkillCoroutine != null)
            StopCoroutine(SkillCoroutine);

        SkillCoroutine = StartCoroutine(SkillLoop(interval));
    }

    private IEnumerator SkillLoop(float interval)
    {
        // (선택) 초기 지연
        yield return new WaitForSeconds(1f);
        while (IsAlive && IsHostile) // IsHostile 조건 추가
        {
            UseSkillPattern();
            yield return new WaitForSeconds(interval);
        }
    }

    /// <summary>
    /// 스킬 루프 정지
    /// </summary>
    protected void StopSkillLoop()
    {
        if (SkillCoroutine != null)
        {
            StopCoroutine(SkillCoroutine);
            SkillCoroutine = null;
        }
    }

    /// <summary>
    /// 갤러리 매니저가 호출. 보스의 능력치를 강화합니다.
    /// </summary>
    /// <param name="multiplier">강화 배수 (예: 1.5 = 1.5배 빠르고 강함)</param>
    public virtual void ApplyGalleryBuffs(float multiplier)
    {
        Debug.Log($"[BaseBoss] 난이도 강화 적용! (x{multiplier})");

        // 1. 애니메이션 속도 증가
        if (CursorObject != null)
        {
            CursorObject.PlaybackSpeed = multiplier;
        }

        // 2. (선택) 체력 증가? (필요하다면)
        // MaxHP = Mathf.RoundToInt(MaxHP * multiplier);
        // CurrentHP = MaxHP;
    }
}
