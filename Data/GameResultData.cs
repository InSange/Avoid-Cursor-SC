using System.Collections.Generic;

/// <summary>
/// 게임 한 판이 끝났을 때의 통계 데이터
/// </summary>
[System.Serializable]
public struct GameResultData
{
    public float SurvivalTime; // 생존 시간 (초)
    public int BossKillCount;  // 처치한 보스 수
    public List<string> DefeatedBossNames; // 처치한 보스 이름 목록
    public List<UnlockID> EarnedUnlocks;   // 이번 판에 해금한 것들
}