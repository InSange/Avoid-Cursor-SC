using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// '보스 갤러리' 씬의 NPC 스폰 및 상호작용을 관리합니다.
/// </summary>
public class BossGalleryManager : PoolingControllerBase
{
    [System.Serializable]
    public struct BossSpawnEntry
    {
        public UnlockID NpcUnlockID;
        public GameObject BossPrefab;
        public Transform SpawnPoint;
        [Tooltip("이 보스가 주는 퀘스트 데이터")]
        public QuestData Quest; // 💥 추가됨
    }

    [Header("보스 스폰 목록")]
    [Tooltip("인스펙터에서 갤러리에 등장할 모든 보스 정보를 설정합니다.")]
    public List<BossSpawnEntry> BossEntries;

    [Header("Difficulty Settings")]
    [Tooltip("갤러리 보스의 강화 배수 (기본 1.2 = 20% 더 빠름)")]
    public float DifficultyMultiplier = 1.3f;

    [Header("상호작용 참조")]
    [Tooltip("씬에 배치된 TerminalManager 참조")]
    public TerminalManager Terminal; // TerminalManager 참조

    [Header("Player Settings")]
    public Transform GalleryPlayerSpawnPoint;

    [Header("UI & Transition")]
    public PlayerHUDManager HUDManager;

    private AvoidCursorGameManager _gameManager;

    protected override void Awake()
    {
        base.Awake(); // 여기서 PoolingControllerBase.Current = this 로 설정됨
    }

    void Start()
    {
        _gameManager = AvoidCursorGameManager.Instance;
        if (Terminal == null)
            Terminal = FindObjectOfType<TerminalManager>();

        SpawnPlayerInGallery();

        // GameManager로부터 내가 스폰해야 할 보스 ID를 가져옴
        UnlockID bossToSpawn = UnlockID.BossNPC_CommandLineWarden;//AvoidCursorGameManager.SelectedBossForGallery;

        //    다음에 씬이 잘못 로드되는 것을 방지하기 위해 ID를 즉시 리셋
        _gameManager.SetGalleryContext(UnlockID.None);

        if (bossToSpawn == UnlockID.None)
        {
            Debug.LogError("[GalleryManager] 스폰할 보스 정보가 없습니다!");
            return;
        }

        foreach (var entry in BossEntries)
        {
            if (entry.NpcUnlockID == bossToSpawn)
            {
                SpawnSingleBossNPC(entry);
                break;
            }
        }

        if (HUDManager != null) HUDManager.HideHUD();

        AvoidCursorGameManager.Instance.OnPlayerDeath += HandleGalleryDeath;
    }

    private void OnDestroy()
    {
        if (AvoidCursorGameManager.Instance != null)
            AvoidCursorGameManager.Instance.OnPlayerDeath -= HandleGalleryDeath;
    }

    private void SpawnPlayerInGallery()
    {
        if (_gameManager == null) return;

        // 1. GameManager의 스폰 포인트를 현재 씬의 것으로 갱신
        if (GalleryPlayerSpawnPoint != null)
        {
            _gameManager.CursorSpawnPoint = GalleryPlayerSpawnPoint;
        }

        // 2. 이전 씬의 플레이어 참조가 남아있을 수 있으므로 초기화 (Missing Reference 방지)
        // (GameManager가 DontDestroyOnLoad라도, 씬 이동 시 플레이어는 파괴되었음)
        if (_gameManager.PlayerCursor == null)
        {
            _gameManager.SpawnCursor();
        }
        // 만약 PlayerCursor가 null이 아닌데 Missing 상태라면(유니티 특성),
        // GameManager의 SpawnCursor 내부에서 체크하고 재생성할 것입니다.
        // 하지만 더 확실하게 하려면:
        else if (_gameManager.PlayerCursor.gameObject == null) // 유니티 오버로딩 null 체크
        {
            _gameManager.PlayerCursor = null;
            _gameManager.SpawnCursor();
        }
    }

    /// <summary>
    /// 지정된 단일 보스를 NPC 상태로 스폰시킵니다.
    /// </summary>
    private void SpawnSingleBossNPC(BossSpawnEntry entry)
    {
        if (entry.BossPrefab == null) return;

        GameObject go = Instantiate(entry.BossPrefab, entry.SpawnPoint.position, entry.SpawnPoint.rotation);
        BaseBoss boss = go.GetComponent<BaseBoss>();

        if (boss != null)
        {
            boss.InitializeAsNPC(this);
            Debug.Log($"[GalleryManager] {entry.NpcUnlockID} NPC 스폰 완료.");

            boss.OnCombatStarted += HandleBossCombatStarted;

            // 💥 2. (핵심) 스폰 즉시 터미널 대화 자동 시작
            if (Terminal != null && entry.Quest != null)
            {
                Terminal.StartQuestDialogue(boss, entry.Quest);
            }
            else
            {
                // 퀘스트가 없거나 터미널이 없으면 그냥 기본 메시지 출력
                Terminal?.PrintSystem($"Connection established with {boss.name}. No quests available.");
            }
        }
    }

    private void HandleBossCombatStarted(BaseBoss boss)
    {
        Debug.Log($"[GalleryManager] {boss.name} 적대화 확인! 버프 및 패턴 가동.");

        if (HUDManager != null) HUDManager.ShowHUD();

        // A. 난이도 버프 적용 (배속 등)
        boss.ApplyGalleryBuffs(DifficultyMultiplier);

        // B. 해당 보스의 환경 패턴 실행
        if (boss.EnvironmentPatterns != null)
        {
            foreach (var pattern in boss.EnvironmentPatterns)
            {
                // 갤러리 난이도 배수 적용하여 실행
                StartCoroutine(pattern.Execute(this, AvoidCursorGameManager.Instance.PlayerCursor, DifficultyMultiplier));
            }
        }
    }

    private void HandleGalleryDeath()
    {
        Debug.Log("[Gallery] 플레이어 사망! 허브로 복귀합니다.");
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.ReturnToHub();
        }
    }

    public void ForceStopDialogue(BaseBoss boss)
    {
        if (Terminal != null)
        {
            // 터미널(Canvas)을 비활성화하여 대화창을 끕니다.
            Terminal.CloseTerminal();
            Debug.Log($"[GalleryManager] 전투 시작! 터미널을 종료합니다.");
        }
    }
}