using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New AttackPattern", menuName = "Boss/AttackPattern")]
public class AttackPattern : ScriptableObject
{
    [Header("기본 정보")]
    public CursorState State; // 이 공격에 해당하는 CursorState

    [Header("콤보 연계 설정")]
    [Tooltip("이 공격 이후 연계 가능한 다음 공격 패턴 목록")]
    public List<AttackPattern> PossibleNextChains;

    [Tooltip("콤보를 이어갈 기본 확률 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float ComboContinueChance = 0.8f; // 80% 확률로 콤보 지속

    [Header("조건부 설정")]
    [Tooltip("이 공격을 시작기로 사용하기 위한 최소/최대 사정거리")]
    public float MinDistance = 0f;
    public float MaxDistance = 5f;
}