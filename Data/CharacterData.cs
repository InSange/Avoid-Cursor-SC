using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "CursorReboot/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("해금 조건")]
    [Tooltip("이 캐릭터를 해금하기 위해 필요한 ID (None이면 기본 해금)")]
    public UnlockID RequiredUnlockID;

    [Header("UI 표시 정보")]
    public string CharacterName;
    [TextArea(3, 5)]
    public string Description;
    public Sprite PanelIcon; // 💥 캐러셀 카드에 표시될 아이콘

    [Header("인게임 정보")]
    [Tooltip("'장착하기' 눌렀을 때 스폰될 플레이어 프리팹")]
    public GameObject PlayerCursorPrefab;

    // (추후 스탯 등 추가 가능)
}