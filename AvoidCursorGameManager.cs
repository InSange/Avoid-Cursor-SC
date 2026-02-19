using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AvoidCursorGameManager : Manager<AvoidCursorGameManager>
{
    [Header("SaveData")]
    private AvoidCursorSaveData _saveData;

    public event Action<PlayerLogicBase> OnPlayerSpawned;
    /// <summary>
    /// 새로운 요소(패시브, 아이템, NPC 등)가 해금될 때 호출됩니다.
    /// </summary>
    public event Action<UnlockID> OnUnlockAchieved;
    public event Action<GameResultData> OnGameResult;
    public event Action OnPlayerDeath;

    [Header("Debuff Status (Grayscale)")]
    private bool _isGrayscaleDebuffActive = false;

    /// <summary>
    /// Grayscale 디버프 상태가 변경될 때 호출됩니다.
    /// (파라미터: true = 흑백 활성화, false = 컬러 복원)
    /// </summary>
    public event Action<bool> OnGrayscaleStatusChanged;

    /// <summary>
    /// 현재 Grayscale 디버프 상태를 설정 및 조회합니다.
    /// </summary>
    public bool IsGrayscaleDebuffActive
    {
        get => _isGrayscaleDebuffActive;
        set
        {
            if (_isGrayscaleDebuffActive != value)
            {
                _isGrayscaleDebuffActive = value;
                // 💥 상태가 변경될 때마다 이벤트를 호출하여 모든 SpriteRenderer에 업데이트를 요청
                OnGrayscaleStatusChanged?.Invoke(value);
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();

        _saveData = SaveManager.Load();
    }

    #region SaveData (Unlock & Quest)

    /// <summary>
    /// 해당 ID의 요소가 해금되었는지 확인합니다.
    /// </summary>
    public bool IsUnlocked(UnlockID id)
    {
        if (id == UnlockID.None) return true;
        return _saveData.UnlockedElements.Contains(id);
    }

    /// <summary>
    /// 새로운 요소를 영구적으로 해금합니다.
    /// (보스 처치, 퀘스트 완료 시 호출)
    /// </summary>
    public void AchieveUnlock(UnlockID id)
    {
        if (id == UnlockID.None) return;

        // 이미 해금되었다면 무시
        if (_saveData.UnlockedElements.Contains(id))
            return;

        _saveData.UnlockedElements.Add(id);
        SaveManager.Save(_saveData);
        OnUnlockAchieved?.Invoke(id);

        Debug.Log($"[GameManager] 신규 해금: {id}");
    }

    /// <summary>
    /// 퀘스트의 현재 진행도를 가져옵니다.
    /// </summary>
    public int GetQuestProgress(UnlockID questId)
    {
        _saveData.QuestProgress.TryGetValue(questId, out int progress);
        return progress;
    }

    /// <summary>
    /// 퀘스트 진행도를 특정 값으로 설정합니다.
    /// </summary>
    public void SetQuestProgress(UnlockID questId, int progress)
    {
        _saveData.QuestProgress[questId] = progress;
        SaveManager.Save(_saveData);
    }

    /// <summary>
    /// 퀘스트 진행도를 1만큼 (또는 amount만큼) 증가시킵니다.
    /// </summary>
    public void IncrementQuestProgress(UnlockID questId, int amount = 1)
    {
        int currentProgress = GetQuestProgress(questId);
        currentProgress += amount;
        SetQuestProgress(questId, currentProgress);

        // TODO: 여기서 QuestManager.CheckCompletion(questId) 호출
    }
    #endregion

    [Header("Player")]
    public Transform PlayerCursor;
    public bool IsGameOver = false;

    [Header("플레이어 스폰 설정")]
    [Tooltip("플레이어 커서 프리팹")]
    public GameObject CursorPrefab;
    [Tooltip("플레이어가 처음 스폰될 위치")]
    public Transform CursorSpawnPoint;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //SpawnCursor();
    }

    #region 플레이어 관련
    public void SetPlayerPrefab(GameObject newPrefab)
    {
        if (newPrefab == null) return;

        // 1. 프리팹 교체
        CursorPrefab = newPrefab;

        Vector3 targetPos;

        // 2. 기존 플레이어가 있다면 파괴하고 재소환
        if (PlayerCursor != null)
        {
            // 기존 플레이어가 있으면 그 위치를 계승
            targetPos = PlayerCursor.position;
            Destroy(PlayerCursor.gameObject); // 기존 객체 파괴
        }
        else
        {
            // (혹시라도) 플레이어가 없다면 스폰 포인트 사용
            targetPos = (CursorSpawnPoint != null) ? CursorSpawnPoint.position : Vector3.zero;
        }

        var go = Instantiate(CursorPrefab, targetPos, Quaternion.identity);
        PlayerCursor = go.transform;
    }

    public void SpawnCursor()
    {
        if (CursorPrefab == null)
        {
            Debug.LogError("[GameManager] CursorPrefab이 할당되지 않았습니다! Inspector를 확인하세요.");
            return;
        }

        // 스폰 포인트가 설정되지 않았다면 기본 위치 사용
        Vector2 spawnPos = (CursorSpawnPoint != null) ? (Vector2)CursorSpawnPoint.position : Vector2.zero;

        var go = Instantiate(CursorPrefab, spawnPos, Quaternion.identity);

        PlayerCursor = go.transform;
        Debug.Log("[GameManager] 플레이어 스폰 완료.");

        var playerLogic = go.GetComponent<PlayerLogicBase>();
        if (playerLogic != null)
        {
            InitializePlayerItem(playerLogic);  // 장비 데이터 세팅
            OnPlayerSpawned?.Invoke(playerLogic); // 플레이어 스폰
            Debug.Log($"[GameManager] 플레이어 스폰 및 이벤트 전송 완료: {playerLogic.name}");
        }
        else
        {
            Debug.LogError("[GameManager] 스폰된 커서에 PlayerLogicBase가 없습니다!");
        }
    }

    public void PlayerDeath()
    {
        if (IsGameOver) return; // 중복 호출 방지
        IsGameOver = true;
        OnPlayerDeath?.Invoke();
    }
    #endregion

    public void StartGame()
    {

    }

    public void RestartStage()
    {
        StartCoroutine(RestartWithDelay());
    }

    private IEnumerator RestartWithDelay()
    {
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    [Header("씬 전환 or 바로 끄기")]
    public bool GameOverCheck = false;
    public void GameOver()
    {
        PlayerCursor = null;
#if UNITY_EDITOR
        if (GameOverCheck)
            UnityEditor.EditorApplication.isPlaying = false;
        else
            RestartStage(); // 임시로 재시작
#else
        RestartStage(); // 임시로 재시작
#endif
    }

    public void NotifyGameResult(GameResultData data)
    {
        OnGameResult?.Invoke(data);
    }

    #region 장비 아이템
    [Header("Item System")]
    public ItemDatabase ItemDB; // (인스펙터에서 할당 필수!)
    public UnlockID CurrentEquippedItemID = UnlockID.None;

    /// <summary>
    /// 액티브 아이템을 장착합니다. (UI에서 호출)
    /// </summary>
    public void EquipActiveItem(UnlockID itemId)
    {
        // 1. 해금 여부 확인
        if (!IsUnlocked(itemId))
        {
            Debug.LogWarning("해금되지 않은 아이템입니다.");
            return;
        }

        // 2. 장착 정보 저장
        CurrentEquippedItemID = itemId;

        // 3. 현재 플레이어에게 즉시 반영
        if (PlayerCursor != null)
        {
            var logic = PlayerCursor.GetComponent<PlayerLogicBase>();
            if (logic != null)
            {
                logic.EquipItem(itemId);
            }
        }

        Debug.Log($"[GameManager] 아이템 장착 완료: {itemId}");
    }

    /// <summary>
    /// 플레이어 스폰 시 호출되는 초기화 로직 (Start 등에서 호출하거나 OnPlayerSpawned에서 처리)
    /// </summary>
    private void InitializePlayerItem(PlayerLogicBase player)
    {
        // 저장된 장착 아이템이 있다면 플레이어에게 지급
        if (CurrentEquippedItemID != UnlockID.None)
        {
            player.EquipItem(CurrentEquippedItemID);
        }
    }

    #endregion

    #region Gallery Context

    /// <summary>
    /// '보스 갤러리' 씬에 입장할 때 스폰할 보스의 NpcUnlockID입니다.
    /// (씬 전환 시 데이터 전달용)
    /// </summary>
    public static UnlockID SelectedBossForGallery { get; private set; } = UnlockID.None;

    /// <summary>
    /// 갤러리 씬에 입장하기 직전에,
    /// SystemModuleEntry가 이 함수를 호출하여 스폰할 보스를 설정합니다.
    /// </summary>
    public void SetGalleryContext(UnlockID bossNpcID)
    {
        SelectedBossForGallery = bossNpcID;
        Debug.Log($"[GameManager] 갤러리 컨텍스트 설정: {bossNpcID}");
    }

    #endregion

    #region Debug
    /// <summary>
    /// (디버그용) 특정 요소를 강제로 해금합니다.
    /// </summary>
    public void ForceUnlock(UnlockID id)
    {
        if (id == UnlockID.None) return;

        if (!_saveData.UnlockedElements.Contains(id))
        {
            _saveData.UnlockedElements.Add(id);
            SaveManager.Save(_saveData);
            OnUnlockAchieved?.Invoke(id);
            Debug.Log($"[DEBUG] 강제 해금: {id}");
        }
    }
    #endregion
}
