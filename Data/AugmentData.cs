using UnityEngine;

[CreateAssetMenu(menuName = "CursorReboot/Augment Data")]
public class AugmentData : ScriptableObject
{
    [Header("UI Info")]
    public string Title;
    [TextArea] public string Description;
    public Sprite Icon;

    [Header("Effect")]
    public AugmentType Type;
    public float Value; // (예: 10, 0.1, 1 등)

    [Tooltip("True면 플레이어가 선택하는게 아니라 스테이지 시작 시 강제로 부여되는 디버프입니다.")]
    public bool IsDebuff;

    // 이 증강과 관련된 프리팹 (패시브 오브젝트, 이펙트 등)
    [Header("Prefab Reference")]
    public GameObject EffectPrefab;
    public CursorAnimation EffectAnimation;
}