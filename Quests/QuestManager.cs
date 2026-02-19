using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 모든 퀘스트의 진행도를 관리하고 완료 처리를 하는 싱글톤 매니저입니다.
/// </summary>
public class QuestManager : Manager<QuestManager>
{
    [Header("퀘스트 데이터베이스")]
    [Tooltip("Resources/Quests 폴더에 있는 모든 QuestData 에셋")]
    public List<QuestData> AllQuests;

    private Dictionary<UnlockID, QuestData> _questMap;
    private InfiniteModeManager _modeManager;
    private AvoidCursorGameManager _gameManager;

    protected override void Awake()
    {
        base.Awake();

        // 모든 퀘스트 데이터를 딕셔너리로 변환하여 빠른 조회
        _questMap = AllQuests.ToDictionary(q => q.QuestID, q => q);
    }

    private void Start()
    {
        // 핵심 매니저 참조
        _gameManager = AvoidCursorGameManager.Instance;

        // TODO: HubScene이 아닌 InfiniteMode 씬일 경우에만 구독
        // _modeManager = InfiniteModeManager.Current;
        // _modeManager.OnBossDefeatedForQuest += HandleBossDefeat;
        // _modeManager.OnTimeUpdated += HandleTimeUpdate;
    }

    /// <summary>
    /// (InfiniteModeManager에서 호출) 보스가 처치되었음을 보고받음
    /// </summary>
    public void ReportBossDefeat(BaseBoss boss)
    {
        // 1. "아무 보스나 처치" 퀘스트 진행
        ProgressQuests(QuestType.DefeatAnyBoss, 1);

        // 2. "특정 보스 처치" 퀘스트 진행 (필요 시)
        // ProgressQuests(QuestType.DefeatBoss, 1, boss.BossID);
    }

    /// <summary>
    /// (InfiniteModeManager에서 호출) 생존 시간이 갱신됨
    /// </summary>
    public void ReportSurvivalTime(float totalTimeSeconds)
    {
        // "생존 시간" 퀘스트는 누적이 아니므로, SetProgress 사용
        foreach (var quest in GetActiveQuestsByType(QuestType.SurviveTime))
        {
            // 현재 생존 시간을 그대로 저장
            int timeInt = Mathf.FloorToInt(totalTimeSeconds);
            _gameManager.SetQuestProgress(quest.QuestID, timeInt);
            CheckCompletion(quest);
        }
    }

    /// <summary>
    /// 특정 유형의 퀘스트 진행도를 'amount'만큼 증가시킵니다.
    /// </summary>
    private void ProgressQuests(QuestType type, int amount)
    {
        foreach (var quest in GetActiveQuestsByType(type))
        {
            int currentProgress = _gameManager.GetQuestProgress(quest.QuestID);
            _gameManager.SetQuestProgress(quest.QuestID, currentProgress + amount);
            CheckCompletion(quest);
        }
    }

    /// <summary>
    /// 퀘스트가 완료되었는지 확인하고, 완료 시 보상을 지급합니다.
    /// </summary>
    private void CheckCompletion(QuestData quest)
    {
        int progress = _gameManager.GetQuestProgress(quest.QuestID);

        if (progress >= quest.TargetValue)
        {
            // 퀘스트 완료 처리
            Debug.Log($"[QuestManager] 퀘스트 완료: {quest.QuestName}");

            // 1. 보상 해금
            foreach (var rewardId in quest.Rewards)
            {
                _gameManager.AchieveUnlock(rewardId);
            }

            // 2. 퀘스트 자체를 비활성화 (예: 해금 목록에서 제거)
            // TODO: 완료된 퀘스트 처리 로직
        }
    }

    /// <summary>
    /// 현재 '활성화'된 (아직 완료되지 않은) 퀘스트 중
    /// 특정 타입의 퀘스트만 필터링하여 반환합니다.
    /// </summary>
    private IEnumerable<QuestData> GetActiveQuestsByType(QuestType type)
    {
        // TODO: _gameManager.IsUnlocked()를 확인하여
        //       플레이어가 '해금'한 퀘스트 목록만 가져와야 함.

        foreach (var quest in AllQuests)
        {
            if (quest.Type == type)
            {
                // TODO: 이미 완료한 퀘스트는 제외
                yield return quest;
            }
        }
    }
}