using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 플레이어 캐릭터의 상태, HP, 스태미나, I-Frame, 고유 공격 로직을 정의하는 통합 기본 클래스입니다.
/// </summary>
[RequireComponent(typeof(PlayerCursorStateManager), typeof(CursorStateController), typeof(CursorObject))]
public abstract class PlayerLogicBase : MonoBehaviour, IHittable
{
    protected PlayerCursorStateManager Manager;
    protected SpriteRenderer _renderer;

    [Header("Health & Status")]
    public int MaxHP = 10;
    [SerializeField] private int currentHP;
    public bool IsDead { get; private set; }

    [Header("Invincibility")]
    public float InvincibilityDuration = 0.25f;
    protected bool _isInvincible = false;

    [Header("Stamina")]
    public float MaxStamina = 10f;
    [SerializeField] private float currentStamina;
    public float BaseStaminaRegenRate = 2f; // 초당 20 회복

    [Header("Item Cooldown")]
    public float ItemCooldownTime = 5.0f;
    [SerializeField] private float _currentItemCooldown = 0f;

    // --- 스탯 보너스 변수들 ---
    public float BonusMoveSpeed { get; set; } = 0f;     // 이동속도 증가량 (Buff - Debuff)
    public float CooldownReduction { get; set; } = 0f;  // 쿨감 % (0 ~ 1.0)
    public float BonusStaminaRegen { get; set; } = 0f;  // 재생량 증가분

    public float CurrentHP => currentHP;
    public float CurrentStamina => currentStamina;
    public float CurrentItemCooldown => _currentItemCooldown;
    public float RealStaminaRegen => BaseStaminaRegenRate + BonusStaminaRegen;
    // Item 쿨다운 계산 (기본 * (1 - 쿨감%))
    public float RealItemCooldownTime => Mathf.Max(0.1f, ItemCooldownTime * (1f - (CooldownReduction / 100f)));

    public bool IsItemReady => _currentItemCooldown <= 0f; // 스킬 사용 가능 여부
    public float InitialMaxHP { get; private set; } // 동적 확장을 위한 초기 최대값
    public float InitialMaxStamina { get; private set; }
    public abstract float SkillStaminaCost { get; }


    [Header("Passives")]
    private OrbitShieldController _shieldController;
    public GameObject OrbitShieldPrefab; // (여신의 방패 프리팹)
    private bool _hasSpeedBoostRing = false;
    private bool _hasPoisonSkin = false;

    [Header("Artifact Visuals")]
    public CursorAnimation SpeedBoostAnim;
    private CursorObject _speedBoostVisual;
    private SpriteRenderer _speedBoostRenderer;

    #region 아티팩트 관련 변수
    // [바람의 반지 변수]
    private float _sbCooldown = 10f; // 기본 10초
    private float _sbAmount = 10f;   // 기본 속도 +10
    private int _sbCooldownLevel = 0; // 현재 쿨감 강화 횟수 (Max 5)
    private int _sbPowerLevel = 0;    // 현재 파워 강화 횟수 (Max 4)

    // 쿨타임 강화 가능 여부: 반지가 있고 && 레벨이 5 미만일 때
    public bool CanUpgradeSBCooldown => _hasSpeedBoostRing && _sbCooldownLevel < 5;
    // 파워 강화 가능 여부: 반지가 있고 && 레벨이 4 미만일 때
    public bool CanUpgradeSBPower => _hasSpeedBoostRing && _sbPowerLevel < 4;

    // [필터링용 프로퍼티] - 매니저가 이 값들을 보고 증강을 띄울지 말지 결정함
    public bool HasSpeedBoostRing => _hasSpeedBoostRing;
    public bool HasPoisonSkin => _hasPoisonSkin;
    #endregion

    [Header("Active Item")]
    public ActiveItemData CurrentActiveItem;

    protected LayerMask GameAttackLayers;

    public event System.Action OnStatsChanged;

    protected virtual void Awake()
    {
        Manager = GetComponent<PlayerCursorStateManager>();
        _renderer = GetComponent<SpriteRenderer>();
        _shieldController = GetComponent<OrbitShieldController>();
        if (_shieldController == null)
            _shieldController = gameObject.AddComponent<OrbitShieldController>();

        GameAttackLayers = LayerMask.GetMask("Boss", "Enemy");
    }

    protected virtual void Start()
    {
        currentHP = MaxHP;
        currentStamina = MaxStamina;
        Manager.Initialize(); // FSM 초기화

        // 인게임 초기 최대값 저장
        InitialMaxHP = MaxHP;
        InitialMaxStamina = MaxStamina;
    }

    protected virtual void Update()
    {
        // Stamina 자동 회복
        if (currentHP > 0 && currentStamina < MaxStamina)
        {
            currentStamina = Mathf.Min(MaxStamina, currentStamina + RealStaminaRegen * Time.deltaTime);
        }

        if (_currentItemCooldown > 0)
        {
            _currentItemCooldown -= Time.deltaTime;
        }

#if UNITY_EDITOR
        HandleDebugInput();
#endif
    }

    private void HandleDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ModifyMaxStats(hpDelta: 5, staminaDelta: 0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ModifyMaxStats(hpDelta: 0, staminaDelta: 5);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ModifyMaxStats(hpDelta: -5, staminaDelta: 0);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ModifyMaxStats(hpDelta: 0, staminaDelta: -5);

        if (Input.GetKeyDown(KeyCode.O))
        {
            // 테스트를 위해 Resources에서 직접 로드 (경로는 본인 프로젝트에 맞게!)
            // 예: "Prefabs/AvoidCursor/Player Passive Prefab/OrbitShield"
            var data = Resources.Load<AugmentData>("Data/Augemet Data/Passive_OrbitShield Data");
            if (data != null)
            {
                Debug.Log($"[Debug] {data.Title} 강제 획득!");
                ActivateArtifact(data.Type, data.EffectPrefab, data.EffectAnimation);
            }
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            var data = Resources.Load<AugmentData>("Data/Augemet Data/Passive_PoisonTrail Data");
            if (data != null)
            {
                Debug.Log($"[Debug] {data.Title} 강제 획득!");
                ActivateArtifact(data.Type, data.EffectPrefab, data.EffectAnimation);
            }
            else
            {
                Debug.LogError("[Debug] 데이터를 찾을 수 없습니다. 'Resources/Augments/Passive_PoisonTrail' 경로 확인 필요.");
            }
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            // 실제 데이터를 로드해서 테스트 (경로가 정확해야 함!)
            var data = Resources.Load<AugmentData>("Data/Augemet Data/Artifact_WindRing_Base Data");
            if (data != null)
            {
                Debug.Log($"[Debug] {data.Title} 강제 획득!");
                // 증강 획득 시뮬레이션
                ActivateArtifact(data.Type, data.EffectPrefab, data.EffectAnimation);
            }
            else
            {
                Debug.LogError("[Debug] 데이터를 찾을 수 없습니다. 'Resources/Augments/Artifact_SpeedBoostCycle' 경로 확인 필요.");
            }
        }
    }

    #region 증강 효과
    // --- 증강 적용 메서드 (GameManager가 호출) ---
    public void ApplyStatAugment(AugmentType type, float value)
    {
        switch (type)
        {
            case AugmentType.Stat_MaxHPUp:
                MaxHP += (int)value;
                currentHP += (int)value; // 최대치 늘면서 현재 체력도 같이 회복
                break;
            case AugmentType.Stat_MaxStaminaUp:
                MaxStamina += value;
                currentStamina += value;
                break;
            case AugmentType.Stat_MoveSpeedUp:
                BonusMoveSpeed += value;
                break;
            case AugmentType.Stat_CooldownReduction:
                CooldownReduction = Mathf.Min(70f, CooldownReduction + value); // 최대 70% 제한 예시
                break;
            case AugmentType.Stat_HPRecover:
                currentHP = Mathf.Min(MaxHP, currentHP + (int)value);
                break;
            case AugmentType.Stat_StaminaRegenUp:
                BonusStaminaRegen += value;
                break;

            // 디버프 (플레이어 스탯 관련)
            case AugmentType.Debuff_PlayerSpeedDown:
                BonusMoveSpeed -= value; // 속도 감소
                break;
        }
        // (UI 갱신 이벤트 호출)
        OnStatsChanged?.Invoke();
    }

    // --- 유물/패시브 활성화 ---
    public void ActivateArtifact(AugmentType type, GameObject prefab = null, CursorAnimation animData = null)
    {
        switch (type)
        {  // 바람의 반지 (최초 획득 시)
            case AugmentType.Artifact_SpeedBoostCycle:
                if (!_hasSpeedBoostRing)
                {
                    _hasSpeedBoostRing = true;

                    if (animData != null) SpeedBoostAnim = animData;
                    StartCoroutine(SpeedBoostRingRoutine());
                }
                break;
            // 바람의 반지 (쿨타임 강화)
            case AugmentType.Upgrade_SpeedBoost_Cooldown:
                if (CanUpgradeSBCooldown)
                {
                    _sbCooldownLevel++;
                    _sbCooldown -= 1.0f; // 1초 감소
                    Debug.Log($"[Upgrade] 바람의 반지 쿨타임 강화! 현재: {_sbCooldown}초 (Lv.{_sbCooldownLevel})");
                }
                break;
            // 바람의 반지 (속도 강화)
            case AugmentType.Upgrade_SpeedBoost_Power:
                if (CanUpgradeSBPower)
                {
                    _sbPowerLevel++;
                    _sbAmount += 5.0f; // 속도 +5 추가
                    Debug.Log($"[Upgrade] 바람의 반지 속도 강화! 현재: +{_sbAmount} (Lv.{_sbPowerLevel})");
                }
                break;
                // 여신의 방패 획득
            case AugmentType.Passive_OrbitShield:
                if (prefab != null)
                {
                    // "방패 개수 하나 늘려줘" (최대 5개 제한은 Controller가 함)
                    _shieldController.AddShield(prefab);
                }
                else
                {
                    Debug.LogError("방패 프리팹이 없습니다!");
                }
                break;
            case AugmentType.Passive_PoisonTrail:
                if (!_hasPoisonSkin)
                {
                    _hasPoisonSkin = true;
                    var effect = gameObject.AddComponent<PoisonSkinEffect>();

                    effect.PoisonTickAnim = animData;
                }
                break;
        }

        OnStatsChanged?.Invoke();
    }

    // [바람의 반지 로직]
    private IEnumerator SpeedBoostRingRoutine()
    {
        if (_speedBoostVisual == null && SpeedBoostAnim != null)
        {
            GameObject go = new GameObject("SpeedBoostVisual");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;

            _speedBoostVisual = go.AddComponent<CursorObject>();
            _speedBoostRenderer = go.GetComponent<SpriteRenderer>();

            _speedBoostRenderer.sortingOrder = _renderer.sortingOrder - 1;
            _speedBoostRenderer.enabled = false;

            _speedBoostVisual.OnAnimationComplete += () =>
            {
                _speedBoostRenderer.enabled = false;
            };
        }

        while (true)
        {
            yield return new WaitForSeconds(_sbCooldown); // 재사용 대기 시간

            // 발동
            BonusMoveSpeed += _sbAmount;

            // 이펙트 ON
            if (_speedBoostVisual != null && SpeedBoostAnim != null)
            {
                _speedBoostRenderer.enabled = true;
                _speedBoostVisual.PlayAnimation(SpeedBoostAnim);
            }

            yield return new WaitForSeconds(2f); // 지속시간 2초

            BonusMoveSpeed -= _sbAmount;
        }
    }

    public bool IsOrbitShieldMaxed
    {
        get
        {
            // 컨트롤러가 없으면(아직 획득 전) 0개이므로 false
            if (_shieldController == null) return false;

            // 현재 개수가 최대치 이상이면 true
            return _shieldController.CurrentCount >= _shieldController.MaxShieldCount;
        }
    }
    #endregion

    #region 장비 관련
    /// <summary>
    /// 매니저가 호출하여 아이템 데이터를 주입합니다.
    /// </summary>
    public void EquipItem(UnlockID itemId)
    {
        var db = AvoidCursorGameManager.Instance.ItemDB;
        if (db == null) return;

        var data = db.GetItemByID(itemId);
        if (data != null)
        {
            CurrentActiveItem = data;
            ItemCooldownTime = data.Cooldown; // 쿨타임 갱신
            _currentItemCooldown = 0f; // 교체 시 쿨타임 초기화? (선택사항)
            Debug.Log($"[Player] 아이템 장착됨: {data.ItemName}");
        }
    }
    
    // 장비 사용
    public virtual void ExecuteItem()
    {
        if (CurrentActiveItem == null || CurrentActiveItem.EffectPrefab == null)
        {
            Debug.LogWarning("[Player] 장착된 아이템이 없거나 프리팹이 비어있습니다.");
            return;
        }

        // 아이템 효과(프리팹) 생성
        Instantiate(CurrentActiveItem.EffectPrefab, transform.position, Quaternion.identity);
        Debug.Log($"[Player] 아이템 사용: {CurrentActiveItem.ItemName}");
    }
    #endregion

    private void ModifyMaxStats(int hpDelta, float staminaDelta)
    {
        // 최대 체력 변경
        if (hpDelta != 0)
        {
            MaxHP = Mathf.Max(1, MaxHP + hpDelta); // 최소 1 유지
            currentHP = Mathf.Clamp(currentHP + hpDelta, 1, MaxHP); // 현재 체력도 같이 조정
        }

        // 최대 스태미나 변경
        if (staminaDelta != 0)
        {
            MaxStamina = Mathf.Max(1f, MaxStamina + staminaDelta);
            currentStamina = Mathf.Clamp(currentStamina + staminaDelta, 0f, MaxStamina);
        }

        Debug.Log($"[Debug] Stats Updated -> MaxHP: {MaxHP}, MaxStamina: {MaxStamina}");

        // HUD에게 변경 알림
        OnStatsChanged?.Invoke();
    }

    public abstract void ExecuteBasicAttack(); // 좌 클릭 기본 공격
    public abstract void ExecuteSkill(); // 우 클릭 스킬 공격
    public abstract void HandleAnimationEvent(string eventName); // 애니메이션 처리

    #region IHittable 및 상태 메서드 통합
    public void OnHit(int damage)
    {
        if (IsDead || _isInvincible) return;

        currentHP -= damage;

        if (currentHP <= 0)
            OnDeath();
        else
        {
            Manager.SetHitState();

            StartCoroutine(InvincibilityFlash());
        }
    }

    protected virtual void OnDeath()
    {
        IsDead = true;
        Manager.SetDeadState(); // FSM 상태 중지
    }

    protected virtual IEnumerator InvincibilityFlash()
    {
        _isInvincible = true;
        float timer = 0f;
        float blinkRate = 0.1f;

        while (timer < InvincibilityDuration)
        {
            _renderer.enabled = !_renderer.enabled;
            yield return new WaitForSeconds(blinkRate);
            timer += blinkRate;
        }

        _renderer.enabled = true;
        _isInvincible = false;
    }

    public bool TryUseItem()
    {
        if (IsItemReady)
        {
            _currentItemCooldown = RealItemCooldownTime; // 쿨감 적용된 시간
            ExecuteItem();
            return true;
        }
        return false;
    }

    public bool TryConsumeStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            return true;
        }
        return false;
    }
    #endregion
}