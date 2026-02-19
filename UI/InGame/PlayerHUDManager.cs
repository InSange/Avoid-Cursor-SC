using UnityEngine;
using UnityEngine.UI;
using TMPro; // TMP를 사용하지 않더라도 using은 유지

/// <summary>
/// 플레이어 HUD를 관리하는 스크립트.
/// HP/Stamina 바를 동적으로 확장합니다.
/// </summary>
public class PlayerHUDManager : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("씬에 존재하는 플레이어 로직 컴포넌트")]
    public PlayerLogicBase PlayerLogic;

    [Header("HP Bar")]
    public RectTransform HPBarRect; // 막대바의 RectTransform (Width 제어용)
    public Image HPBarImage;       // Fill Amount 제어용

    [Header("Stamina Bar")]
    public RectTransform StaminaBarRect;
    public Image StaminaBarImage;

    [Header("Skill Interface")]
    public Image CoolImage; // 쿨다운 이미지 (Fill Vertical)
    public Image ArtifactIcon; // 유물 아이템 아이콘

    [Header("Scaling Settings")]
    [Tooltip("MaxHP/MaxStamina 1 포인트 당 막대바가 늘어나는 픽셀 수")]
    private const float PIXELS_PER_UNIT = 10f;

    private UIEntryAnimator _animator;

    void Start()
    {
        _animator = GetComponent<UIEntryAnimator>();

        if (_animator != null)
        {
            _animator.gameObject.SetActive(false); // 일단 끄기
        }

        if (AvoidCursorGameManager.Instance != null)
        {
            AvoidCursorGameManager.Instance.OnPlayerSpawned += HandlePlayerSpawned;

            // (혹시 HUD가 늦게 켜져서 이미 플레이어가 있는 경우를 대비)
            if (AvoidCursorGameManager.Instance.PlayerCursor != null)
            {
                var logic = AvoidCursorGameManager.Instance.PlayerCursor.GetComponent<PlayerLogicBase>();
                if (logic != null) HandlePlayerSpawned(logic);
            }
        }
    }

    void Update()
    {
        if (PlayerLogic == null) return;

        // 💥 2. Fill Amount는 매 프레임 업데이트
        UpdateHealthAndStaminaFill();
        UpdateItemCooldown();

        // 💥 (TODO: 체력/스태미나가 증강으로 인해 늘어날 경우, 여기서 UpdateMaxHealthAndStamina() 호출 필요)
    }

    private void HandlePlayerSpawned(PlayerLogicBase newPlayer)
    {
        // 이전 플레이어의 구독 해제
        if (PlayerLogic != null)
            PlayerLogic.OnStatsChanged -= UpdateMaxHealthAndStamina;

        PlayerLogic = newPlayer;

        // 💥 3. (신규) 새 플레이어 이벤트 구독
        PlayerLogic.OnStatsChanged += UpdateMaxHealthAndStamina;

        UpdateMaxHealthAndStamina();
        Debug.Log("[HUD] 플레이어 참조 및 스탯 UI 갱신됨.");
    }

    public void ShowHUD()
    {
        if (_animator != null) _animator.PlayEntryAnimation();
        else gameObject.SetActive(true);
    }

    public void HideHUD()
    {
        if (_animator != null) _animator.PlayExitAnimation();
        else gameObject.SetActive(false);
    }

    /// <summary>
    /// HP/Stamina의 최대값 변경에 따른 바의 길이(Width)를 업데이트합니다. (오른쪽 확장)
    /// </summary>
    public void UpdateMaxHealthAndStamina()
    {
        if (PlayerLogic == null) return;

        // HP Bar Width
        if (HPBarRect != null)
        {
            float newWidth = PlayerLogic.MaxHP * PIXELS_PER_UNIT;
            HPBarRect.sizeDelta = new Vector2(newWidth, HPBarRect.sizeDelta.y);
        }

        // Stamina Bar Width
        if (StaminaBarRect != null)
        {
            float newWidth = PlayerLogic.MaxStamina * PIXELS_PER_UNIT;
            StaminaBarRect.sizeDelta = new Vector2(newWidth, StaminaBarRect.sizeDelta.y);
        }
    }

    /// <summary>
    /// 현재 HP/Stamina 잔량에 따라 Fill Amount를 업데이트합니다.
    /// </summary>
    private void UpdateHealthAndStaminaFill()
    {
        // 1. HP (1.0 = Full, 0.0 = Empty)
        if (HPBarImage != null)
        {
            HPBarImage.fillAmount = PlayerLogic.CurrentHP / PlayerLogic.MaxHP;
        }
        // 2. Stamina
        if (StaminaBarImage != null)
        {
            StaminaBarImage.fillAmount = PlayerLogic.CurrentStamina / PlayerLogic.MaxStamina;
        }
    }

    /// <summary>
    /// 스킬 쿨다운 타이머에 따라 CoolImage의 Fill Amount를 업데이트합니다.
    /// </summary>
    private void UpdateItemCooldown()
    {
        if (CoolImage == null) return;

        if (PlayerLogic.IsItemReady)
        {
            CoolImage.fillAmount = 0f; // 쿨다운 끝 (스킬 사용 가능)
        }
        else
        {
            // 쿨다운 진행 중: 1.0 (시작)에서 0.0 (끝)으로 감소
            CoolImage.fillAmount = PlayerLogic.CurrentItemCooldown / PlayerLogic.ItemCooldownTime;
        }
    }

    private void OnDestroy()
    {
        // 💥 2. (필수) 이벤트 구독 해제
        if (AvoidCursorGameManager.Instance != null)
        {
            AvoidCursorGameManager.Instance.OnPlayerSpawned -= HandlePlayerSpawned;
        }

        if (PlayerLogic != null)
        {
            PlayerLogic.OnStatsChanged -= UpdateMaxHealthAndStamina;
        }
    }
}