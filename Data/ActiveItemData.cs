using UnityEngine;

[CreateAssetMenu(fileName = "NewActiveItem", menuName = "CursorReboot/Active Item Data")]
public class ActiveItemData : ScriptableObject
{
    [Header("기본 정보")]
    public UnlockID ID;          // 아이템 식별 ID (예: Item_WaveAttack_Wide)
    public string ItemName;      // 표시 이름
    public Sprite Icon;          // UI 아이콘

    [Header("성능")]
    public float Cooldown = 5.0f; // 쿨타임

    [Header("발동 효과")]
    [Tooltip("스킬 사용 시 플레이어 위치에 생성될 프리팹 (예: WaveEffect)")]
    public GameObject EffectPrefab;
}