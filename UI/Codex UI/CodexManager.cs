using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// '도감' UI 패널을 관리합니다.
/// QuestManager와 GameManager의 데이터를 기반으로 퀘스트 목록을 UI에 생성합니다.
/// </summary>
public class CodexManager : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("모든 퀘스트 목록을 가진 QuestManager")]
    public QuestManager QuestManager;

    [Tooltip("CodexEntryUI 프리팹")]
    public GameObject QuestEntryPrefab;

    [Tooltip("프리팹이 생성될 부모 RectTransform (Content)")]
    public RectTransform ContentRoot;

    private AvoidCursorGameManager _gameManager;

    void Start()
    {
        _gameManager = AvoidCursorGameManager.Instance;

        if (QuestManager == null)
            QuestManager = QuestManager.Instance;
    }

    /// <summary>
    /// 도감 패널이 활성화될 때마다 목록을 갱신합니다.
    /// </summary>
    private void OnEnable()
    {
        PopulateCodex();
    }

    /// <summary>
    /// 모든 퀘스트 데이터를 기반으로 도감 UI를 생성합니다.
    /// </summary>
    public void PopulateCodex()
    {
        if (QuestManager == null || _gameManager == null || QuestEntryPrefab == null || ContentRoot == null)
        {
            Debug.LogError("[CodexManager] 참조가 설정되지 않았습니다!");
            return;
        }

        // 1. 기존 UI 항목들 삭제
        foreach (Transform child in ContentRoot)
        {
            Destroy(child.gameObject);
        }

        // 2. QuestManager의 모든 퀘스트 목록을 순회
        foreach (QuestData quest in QuestManager.AllQuests)
        {
            if (quest == null) continue;

            // 3. UI 프리팹 생성
            GameObject entryGO = Instantiate(QuestEntryPrefab, ContentRoot);
            CodexEntryUI uiEntry = entryGO.GetComponent<CodexEntryUI>();

            if (uiEntry != null)
            {
                // 4. GameManager에서 해금 여부 및 진행도 확인
                bool isUnlocked = _gameManager.IsUnlocked(quest.QuestID);
                int progress = _gameManager.GetQuestProgress(quest.QuestID);

                // 5. UI 초기화
                uiEntry.Initialize(quest, isUnlocked, progress);
            }
        }
    }
}