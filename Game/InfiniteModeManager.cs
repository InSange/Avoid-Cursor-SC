using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// '무한 모드'의 핵심 관리자입니다.
/// PoolingControllerBase를 상속받아 오브젝트 풀링 기능을 가집니다.
/// </summary>
public class InfiniteModeManager : PoolingControllerBase
{
    [Header("보스 스폰 목록")]
    public List<GameObject> BossPrefabs; // 등장 가능한 보스 프리팹 목록

    [Header("환경 패턴 프리팹")]
    public FakeCursorBlink CursorBlinkPrefab;
    public PopupSqaure PopupPrefab;

    [Header("닷지 프로토타입 설정")]
    public DodgeBullet DodgeBulletPrefab;
    public int DodgeBulletCount = 10;
    public float MinBulletSpeed = 1f;
    public float MaxBulletSpeed = 5f;

    //초기값 저장용 변수
    private int _defaultBulletCount;
    private float _defaultMinSpeed;
    private float _defaultMaxSpeed;
    private int _defaultBossCount;

    private List<DodgeBullet> _activeDodgeBullets = new List<DodgeBullet>();

    [Header("게임 상태")]
    public int CurrentStage = 1;
    public float _elapsedTime = 0f;

    public float SurvivalDuration = 30f; // 생존 페이즈 지속 시간
    public int BossCountToSpawn = 1;     // 스폰할 보스 수

    public float BreakTimeBetweenStages = 3.0f; // 증강 후 대기 시간

    public bool IsPaused { get; private set; } = false;
    [SerializeField] private bool _isGameRunning = false;
    [SerializeField] private float _bossSpawnTimer = 0f;

    private List<BaseBoss> _activeBosses = new List<BaseBoss>(); // 현재 스테이지 보스들
                                                                 // 보스별 실행 중인 환경 패턴 코루틴 매핑(보스 -> 코루틴 리스트)
    private Dictionary<BaseBoss, List<Coroutine>> _bossPatternMap = new Dictionary<BaseBoss, List<Coroutine>>();

    [Header("Augment System")]
    public List<AugmentData> AllBuffs;   // 전체 버프 리스트 (Inspector)
    public List<AugmentData> AllDebuffs; // 전체 디버프 리스트
    public int MinChoiceCount = 1;
    public int MaxChoiceCount = 6;
    private int _augmentChoiceCount = 2; // 초기 2개
    private List<string> _currentBuffs = new List<string>(); // 현재 적용된 버프리스트

    [Header("통계 관련")]
    private List<string> _defeatedBossNames = new List<string>();
    private List<UnlockID> _sessionUnlocks = new List<UnlockID>(); // 이번 판에 얻은 것

    private QuestManager _questManager;
    private AvoidCursorGameManager _gameManager;

    protected override void Awake()
    {
        // 1. 부모(PoolingControllerBase)의 Awake 실행 (Current = this 설정)
        base.Awake();

        _defaultBulletCount = DodgeBulletCount;
        _defaultMinSpeed = MinBulletSpeed;
        _defaultMaxSpeed = MaxBulletSpeed;
        _defaultBossCount = BossCountToSpawn;
    }

    private void Start()
    {
        _questManager = QuestManager.Instance;
        _gameManager = AvoidCursorGameManager.Instance;
        _gameManager.OnPlayerDeath += StopInfiniteMode;

        _gameManager.OnUnlockAchieved += HandleUnlockInSession;
    }

    private void Update()
    {
        // 게임이 실행 중이지 않다면 아무것도 안 함
        if (!_isGameRunning) return;

        // --- 1. 일시정지 상태일 때 입력 처리 ---
        if (IsPaused)
        {
            // ESC: 게임 종료 (나가기 -> 결과창)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ResumeGame(); // 시간 다시 흐르게 하고
                StopInfiniteMode(); // 게임 종료 (결과창 호출됨)
                return;
            }

            // Space: 게임 재개
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ResumeGame();
                return;
            }

            // 화살표: 페이지 넘기기 (HubUIManager를 통해 PausePanel 제어)
            // (하지만 여기서 직접 제어하거나 UIManager에 요청해야 함. 
            //  간단하게 UIManager 싱글톤이나 참조를 쓰겠습니다.)
            var pauseUI = FindObjectOfType<HubUIManager>().PausePanel; // (최적화 필요)
            if (Input.GetKeyDown(KeyCode.RightArrow)) pauseUI.NextPage();
            if (Input.GetKeyDown(KeyCode.LeftArrow)) pauseUI.PrevPage();

            return; // 일시정지 중에는 아래 게임 로직(타이머 등) 실행 안 함
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PauseGame();
            return;
        }

        // --- 3. 게임 로직 (타이머 등) ---
        _elapsedTime += Time.deltaTime;
        // ... (보스 스폰 타이머 등 기존 Update 로직)
    }

    private void OnDestroy()
    {
        if (_gameManager != null)
            _gameManager.OnUnlockAchieved -= HandleUnlockInSession;
    }

    private void HandleUnlockInSession(UnlockID id)
    {
        if (_isGameRunning)
            _sessionUnlocks.Add(id);
    }

    #region 인게임 관련 함수
    /// <summary>
    /// HubUIManager가 'Play' 버튼 클릭 시 호출합니다.
    /// </summary>
    public void StartInfiniteMode()
    {
        if (_isGameRunning) return;

        ClearAllPools();

        if (_gameManager.PlayerCursor == null)
        {
            Debug.LogError("[InfiniteModeManager] GameManager에 PlayerCursor가 없습니다!");
            return;
        }

        DodgeBulletCount = _defaultBulletCount;
        MinBulletSpeed = _defaultMinSpeed;
        MaxBulletSpeed = _defaultMaxSpeed;
        BossCountToSpawn = _defaultBossCount;

        _augmentChoiceCount = 2;

        _isGameRunning = true;
        CurrentStage = 1;
        _elapsedTime = 0f;
        _gameManager.IsGameOver = false;

        _defeatedBossNames.Clear();
        _sessionUnlocks.Clear();

        StartCoroutine(StageRoutine());
    }

    /// <summary>
    /// 플레이어 사망 시 GameManager가 호출합니다.
    /// </summary>
    public void StopInfiniteMode()
    {
        if (!_isGameRunning) return;

        Debug.Log("[InfiniteModeManager] '무한 모드'를 중지합니다.");
        _isGameRunning = false;
        StopAllCoroutines();

        // 모든 패턴 및 보스 정리
        ClearAllPools();
        CleanupBossesAndPatterns();
        DespawnDodgeBullets();

        // 활성화된 보스 강제 파괴 및 리스트 클리어
        foreach (var boss in _activeBosses)
        {
            if (boss != null) Destroy(boss.gameObject);
        }
        _activeBosses.Clear();

        if (_gameManager.PlayerCursor != null)
        {
            Destroy(_gameManager.PlayerCursor.gameObject);
            _gameManager.PlayerCursor = null;
        }

        GameResultData result = new GameResultData
        {
            SurvivalTime = _elapsedTime,
            BossKillCount = _defeatedBossNames.Count,
            DefeatedBossNames = new List<string>(_defeatedBossNames),
            EarnedUnlocks = new List<UnlockID>(_sessionUnlocks)
        };

        AvoidCursorGameManager.Instance.NotifyGameResult(result);
    }

    private IEnumerator StageRoutine()
    {
        while (_isGameRunning)
        {
            Debug.Log($"[InfiniteMode] 스테이지 {CurrentStage} 시작!");

            // --- Phase 1: 생존 (Survival) ---
            Debug.Log($"[InfiniteMode] 생존 페이즈 시작 ({SurvivalDuration}초)");

            // 닷지 불릿 시작 (디버프 적용된 수치로)
            SpawnDodgeBullets();

            // 환경 패턴도 여기서 시작 가능

            // 정해진 시간 동안 버티기
            yield return new WaitForSeconds(SurvivalDuration);


            // --- Phase 2: 보스전 (Boss Battle) ---
            Debug.Log($"[InfiniteMode] 보스 페이즈 시작! ({BossCountToSpawn}마리)");

            // 보스 스폰
            SpawnBossesForStage(BossCountToSpawn);

            // 보스가 모두 죽을 때까지 대기
            yield return new WaitUntil(() => _activeBosses.Count == 0);

            Debug.Log($"[InfiniteMode] 스테이지 {CurrentStage} 클리어!");

            // --- Phase 3: 클리어 및 증강 (Clear & Augment) ---
            DespawnDodgeBullets(); // 불릿 청소

            bool hasSelected = false;
            var uiManager = FindObjectOfType<HubUIManager>();

            List<AugmentData> validPool = GetValidBuffs(); // 유효한 증강
            uiManager.AugmentPanel.Show(validPool, _augmentChoiceCount, (chosenData) =>
            {
                ApplyAugmentEffect(chosenData);
                hasSelected = true;
            });

            yield return new WaitUntil(() => hasSelected);

            // --- Phase 4: 디버프 강제 적용 (Auto Debuff) ---
            if (CurrentStage >= 1)
            {
                yield return StartCoroutine(ApplyRandomDebuffRoutine(uiManager));
            }

            // --- Phase 5: 다음 스테이지 준비 ---
            yield return new WaitForSeconds(BreakTimeBetweenStages);

            CurrentStage++;
        }
    }

    private void ApplyAugmentEffect(AugmentData data)
    {
        Debug.Log($"[InfiniteMode] 효과 적용: {data.Title} ({data.Type}) / Value: {data.Value}");
        var playerLogic = _gameManager.PlayerCursor.GetComponent<PlayerLogicBase>();

        if (data.Type.ToString().StartsWith("Stat_") || data.Type == AugmentType.Debuff_PlayerSpeedDown)
        {// 플레이어 관련 스탯은 LogicBase에게 위임
            playerLogic.ApplyStatAugment(data.Type, data.Value);
        }
        else if (data.Type.ToString().StartsWith("Artifact_") || data.Type.ToString().StartsWith("Passive_"))
        {// 유물/패시브 활성화
            playerLogic.ActivateArtifact(data.Type, data.EffectPrefab, data.EffectAnimation);
        }
        else
        { // 게임 규칙/메타 관련
            switch (data.Type)
            {
                case AugmentType.Meta_ChoiceCountUp:
                    _augmentChoiceCount = Mathf.Min(MaxChoiceCount, _augmentChoiceCount + 1);
                    break;
                case AugmentType.Debuff_ChoiceCountDown:
                    _augmentChoiceCount = Mathf.Max(MinChoiceCount, _augmentChoiceCount - 1);
                    break;
                case AugmentType.Debuff_BulletSpeedUp:
                    MinBulletSpeed += data.Value;
                    MaxBulletSpeed += data.Value;
                    break;
                case AugmentType.Debuff_BulletCountUp:
                    DodgeBulletCount += (int)data.Value;
                    break;
                case AugmentType.Debuff_BossCountUp:
                    BossCountToSpawn += (int)data.Value;
                    break;
            }
        }

        _gameManager.AchieveUnlock(UnlockID.None);
    }

    // 증강 선택지 필터링 (AugmentSelectionPanel에 전달 전)
    public List<AugmentData> GetFilteredAugments(List<AugmentData> sourceList)
    {
        List<AugmentData> filtered = new List<AugmentData>();
        foreach (var aug in sourceList)
        {
            // 이미 디버프 전용 리스트가 있다면 여기서 필터링 안 해도 됨.
            // 하지만 Meta_ChoiceCountUp은 최대치 도달 시 제외해야 함.
            if (aug.Type == AugmentType.Meta_ChoiceCountUp && _augmentChoiceCount >= MaxChoiceCount)
                continue;

            filtered.Add(aug);
        }
        return filtered;
    }

    // 랜덤 디버프 적용 및 알림
    private IEnumerator ApplyRandomDebuffRoutine(HubUIManager uiManager)
    {
        if (AllDebuffs == null || AllDebuffs.Count == 0) yield break;

        // 조건에 맞는 디버프 필터링
        List<AugmentData> validDebuffs = new List<AugmentData>();
        foreach (var debuff in AllDebuffs)
        {
            // 선택지가 이미 1개라면 '선택지 감소' 디버프 제외
            if (debuff.Type == AugmentType.Debuff_ChoiceCountDown && _augmentChoiceCount <= MinChoiceCount)
                continue;

            validDebuffs.Add(debuff);
        }

        if (validDebuffs.Count > 0)
        {
            // 랜덤 선택
            AugmentData selectedDebuff = validDebuffs[Random.Range(0, validDebuffs.Count)];

            // 효과 적용
            ApplyAugmentEffect(selectedDebuff);

            Debug.Log($"[CURSE] 저주 발동: {selectedDebuff.Description}");
            yield return StartCoroutine(uiManager.ShowDebuffAlert(selectedDebuff.Description));
        }

        yield return null;
    }

    private void SpawnBossesForStage(int count)
    {
        _activeBosses.Clear();
        _bossPatternMap.Clear();

        for (int i = 0; i < count; i++)
        {
            // 랜덤 보스 선택 (중복 허용 or 불가 로직 추가 가능)
            if (BossPrefabs.Count == 0) break;
            GameObject prefab = BossPrefabs[Random.Range(0, BossPrefabs.Count)];

            // 위치 랜덤 (화면 상단 등)
            BaseBoss bossScript = prefab.GetComponent<BaseBoss>();
            Vector2 spawnPos = (bossScript != null) ? bossScript.GetSpawnPosition() : new Vector2(Random.Range(-5f, 5f), 3f);

            SpawnSingleBoss(prefab, spawnPos);
        }
    }

    private void SpawnSingleBoss(GameObject prefab, Vector2 pos)
    {
        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        BaseBoss boss = go.GetComponent<BaseBoss>();

        if (boss != null)
        {
            boss.OnBossDefeated += () => OnBossDefeated(boss);
            boss.EnterIntroPhase(); // 전투 시작

            _activeBosses.Add(boss);

            // 💥 보스별 환경 패턴 시작 및 관리
            List<Coroutine> runningPatterns = new List<Coroutine>();
            if (boss.EnvironmentPatterns != null)
            {
                foreach (var pattern in boss.EnvironmentPatterns)
                {
                    if (pattern != null && pattern.EnableInInfiniteMode)
                    {
                        float speedMult = 1.0f + (CurrentStage * 0.1f);
                        var routine = StartCoroutine(pattern.Execute(this, _gameManager.PlayerCursor, speedMult));
                        runningPatterns.Add(routine);
                    }
                }
            }
            _bossPatternMap.Add(boss, runningPatterns);
        }
    }

    /// <summary>
    /// 보스가 처치되었을 때 호출됩니다.
    /// </summary>
    private void OnBossDefeated(BaseBoss boss)
    {
        Debug.Log($"[InfiniteMode] 보스 처치: {boss.name}");
        _questManager?.ReportBossDefeat(boss);

        // 💥 해당 보스의 환경 패턴만 찾아서 중지
        if (_bossPatternMap.TryGetValue(boss, out var routines))
        {
            foreach (var r in routines)
            {
                if (r != null) StopCoroutine(r);
            }
            _bossPatternMap.Remove(boss);
        }

        // 활성 보스 목록에서 제거 (StageRoutine의 WaitUntil 조건을 위해)
        if (_activeBosses.Contains(boss))
            _activeBosses.Remove(boss);

        _defeatedBossNames.Add(boss.name.Replace("(Clone)", ""));

        // 해금 처리
        if (boss is IUnlockProvider provider)
        {
            foreach (var id in provider.GetUnlocksOnDefeat())
                _gameManager.AchieveUnlock(id);
        }
    }

    private void SpawnDodgeBullets()
    {
        if (DodgeBulletPrefab == null) return;

        var pool = GetOrCreatePool(DodgeBulletPrefab); // JIT

        int count = DodgeBulletCount;

        for (int i = 0; i < count; i++)
        {
            var bullet = pool.Get();
            bullet.Initialize(_gameManager.PlayerCursor, Random.Range(MinBulletSpeed, MaxBulletSpeed));
            _activeDodgeBullets.Add(bullet);
        }
    }

    private void DespawnDodgeBullets()
    {
        // 활성화된 모든 닷지 불릿을 풀로 반환
        foreach (var bullet in _activeDodgeBullets)
        {
            if (bullet != null && bullet.gameObject.activeSelf)
            {
                // 풀을 찾아서 반환 (JIT 풀링이므로 타입 기반 찾기 필요하지만, 
                // 간단하게 PoolingControllerBase의 ClearAllPools 활용하거나, 개별 Release)
                // 여기서는 안전하게 ClearAllPools 사용 (환경 패턴 잔재도 지울 겸)
            }
        }
        ClearAllPools(); // 💥 스테이지 끝날 때 싹 청소
        _activeDodgeBullets.Clear();
    }

    private void CleanupBossesAndPatterns()
    {
        foreach (var kvp in _bossPatternMap)
        {
            foreach (var r in kvp.Value) if (r != null) StopCoroutine(r);
        }
        _bossPatternMap.Clear();

        foreach (var boss in _activeBosses)
        {
            if (boss != null) Destroy(boss.gameObject);
        }
        _activeBosses.Clear();
    }
    #endregion

    #region 보조 함수
    // 💥 UI 패널을 열기 전에 호출하여 "유효한 증강 목록"만 추출하는 함수
    private List<AugmentData> GetValidBuffs()
    {
        var player = _gameManager.PlayerCursor.GetComponent<PlayerLogicBase>();
        List<AugmentData> validList = new List<AugmentData>();

        foreach (var aug in AllBuffs)
        {
            if (aug.IsDebuff) continue;

            // --- 조건부 필터링 ---
            switch (aug.Type)
            {
                // [바람의 반지 - 기본]
                case AugmentType.Artifact_SpeedBoostCycle:
                    // 이미 가지고 있으면 등장 X
                    if (player.HasSpeedBoostRing) continue;
                    break;

                // [바람의 반지 - 쿨타임 강화]
                case AugmentType.Upgrade_SpeedBoost_Cooldown:
                    // 반지가 없거나 || 이미 풀업이면 등장 X
                    if (!player.CanUpgradeSBCooldown) continue;
                    break;

                // [바람의 반지 - 속도 강화]
                case AugmentType.Upgrade_SpeedBoost_Power:
                    // 반지가 없거나 || 이미 풀업이면 등장 X
                    if (!player.CanUpgradeSBPower) continue;
                    break;

                // [포이즌 스킨 - 기본]
                case AugmentType.Passive_PoisonTrail:
                    // 이미 가지고 있으면 등장 X (강화 없음)
                    if (player.HasPoisonSkin) continue;
                    break;

                // [여신의 방패]
                case AugmentType.Passive_OrbitShield:
                    // 5개 꽉 찼으면 등장 X
                    if (player.IsOrbitShieldMaxed) continue;
                    break;

                // [메타 - 선택지 증가]
                case AugmentType.Meta_ChoiceCountUp:
                    if (_augmentChoiceCount >= MaxChoiceCount) continue;
                    break;
            }

            // 위 조건에 걸리지 않았다면 유효한 증강임
            validList.Add(aug);
        }

        return validList;
    }
    #endregion

    #region 게임 진행 함수 UI관련
    public void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f; // 시간 정지

        // HubUIManager에게 알림
        var uiManager = FindObjectOfType<HubUIManager>();
        uiManager.PausePanel.Show(_currentBuffs); // 버프 목록 전달

        Debug.Log("[InfiniteMode] 일시정지");
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f; // 시간 재개

        var uiManager = FindObjectOfType<HubUIManager>();
        uiManager.PausePanel.Hide();

        Debug.Log("[InfiniteMode] 재개");
    }
    #endregion
}