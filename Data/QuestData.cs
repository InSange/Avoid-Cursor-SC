using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 퀘스트의 목표와 보상을 정의하는 ScriptableObject입니다.
/// </summary>
[CreateAssetMenu(fileName = "NewQuest", menuName = "CursorReboot/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("1. 퀘스트 식별")]
    [Tooltip("이 퀘스트의 고유 ID입니다. (예: Quest_Survive10Min)")]
    public UnlockID QuestID;

    [Header("2. 퀘스트 정보 (UI 표시용)")]
    public string QuestName = "퀘스트 이름";
    [TextArea(3, 5)]
    public string QuestDescription = "퀘스트 설명 (예: 보스 10마리 처치)";

    [Header("3. 퀘스트 목표")]
    [Tooltip("이 퀘스트의 달성 목표 유형입니다.")]
    public QuestType Type;

    [Tooltip("달성해야 하는 목표 수치입니다. (예: 10 (마리), 600 (초))")]
    public int TargetValue = 10;

    [Header("4. 퀘스트 보상")]
    [Tooltip("이 퀘스트를 완료했을 때 해금되는 모든 보상 목록입니다.")]
    public List<UnlockID> Rewards;
}

/// <summary>
/// 퀘스트의 목표 유형을 정의합니다.
/// (QuestManager가 이 타입을 보고 어떤 이벤트를 추적할지 결정)
/// </summary>
public enum QuestType
{
    SurviveTime,    // 생존 시간 (초)
    DefeatBoss,     // 특정 보스 처치 (TODO: 세분화 필요)
    DefeatAnyBoss,  // 아무 보스나 처치 (횟수)
    DodgeBullets    // 총알 회피 (횟수)
}