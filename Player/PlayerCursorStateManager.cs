using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 커서의 입력 및 FSM 상태를 제어하는 컨트롤러입니다.
/// FSM 로직을 PlayerLogicBase로 위임합니다.
/// </summary>

[RequireComponent(typeof(CursorStateController), typeof(CursorObject))]
public class PlayerCursorStateManager : MonoBehaviour
{
    [SerializeField] private HubUIManager _uiManager;

    [Header("FSM 참조")]
    [SerializeField] private CursorStateController _fsmController; // 애니메이션 실행기
    [SerializeField] private CursorObject _cursorObject;
    [SerializeField] private PlayerLogicBase _characterLogic;
    public CursorStateController FsmController => _fsmController;

    protected float MoveSpeed = 20f;

    public float BaseMoveSpeed
    {
        get => MoveSpeed;
        set => MoveSpeed = value;
    }

    public bool IsMovementLocked { get; set; } = false;

    // 멈춤 판정을 위한 버퍼 타이머
    private float _stopMoveTimer = 0f;
    private const float StopThreshold = 0.1f;

    private Camera _mainCamera;
    protected LayerMask UILayer;

    // UI 닫힘 감지용 변수
    private bool _wasMenuOpen = false;
    private float _inputBlockTimer = 0f;

    protected void Awake()
    {
        _characterLogic = GetComponent<PlayerLogicBase>();
        _fsmController = GetComponent<CursorStateController>();
        _cursorObject = GetComponent<CursorObject>();
        _mainCamera = Camera.main;

        UILayer = LayerMask.GetMask("UIInteractable");

        if (_characterLogic == null)
            Debug.LogError("[PlayerStateManager] PlayerLogicBase 구현 컴포넌트가 없습니다! (예: PlayerLogic_DefaultCursor.cs)");

        _fsmController.OnStateAnimationComplete += HandleStateAnimationComplete;
        _cursorObject.OnAnimationEvent += HandleAnimationEventRouter;
        _uiManager = FindObjectOfType<HubUIManager>();
    }

    void Update()
    {
        MoveWithMouseDelta();

        if (_characterLogic.IsDead) return;

        bool isMenuOpen = (_uiManager != null && _uiManager.IsMenuOpen);

        if (isMenuOpen)
        {
            _wasMenuOpen = true;
            return; // UI 열려있으면 입력 차단
        }
        else if (_wasMenuOpen)
        {
            // UI가 방금 닫혔다면, 잠시동안 입력 차단 (0.2초)
            _wasMenuOpen = false;
            _inputBlockTimer = 0.2f;
        }

        if (_inputBlockTimer > 0)
        {
            _inputBlockTimer -= Time.unscaledDeltaTime; // (Unscaled 시간 사용)
            return; // 타이머 도는 동안 입력 차단
        }

        // if (_fsmController.CurrentState != CursorState.Idle) return; -> 과거 Interruptible을 제외한 애니메이션 제한(실행중일 때 간섭x)을 둘려고 했지만 이걸로 발생하는 제약이 너무 큼.
        // UI 클릭 체크 (최고 우선순위)
        if (CheckForUIClick()) return;

        CursorState current = _fsmController.CurrentState;
        if (current == CursorState.Die ||
            current == CursorState.Intro ||
            current == CursorState.Teleport ||
            current == CursorState.Hit)
        {
            return;
        }

        if (current != CursorState.Skill1)
        {
            if (Input.GetMouseButtonDown(1))
            {
                CheckForSkill();
                return; // 스킬 썼으면 아래 로직 패스
            }
        }

        if (current == CursorState.Idle || current == CursorState.Walk)
        {
            if (CheckForUIClick()) return; // UI 클릭이면 공격 안 함

            if (_characterLogic != null)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    CheckForBasicAttack();
                }

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    CheckForItem();
                }
            }
        }

        // 게임 로직 (공격/스킬)
        /*        if (_characterLogic != null)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        CheckForBasicAttack();
                    }

                    if (Input.GetMouseButtonDown(1))
                    {
                        CheckForSkill();
                    }

                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        CheckForItem();
                    }
                }*/

        // 💥 디버그 키는 FSM enum에 맞춰 재배치 필요
        // if (Input.GetKeyDown(KeyCode.Alpha1)) _fsmController.ChangeState(CursorState.Idle);
    }

    private void HandleStateAnimationComplete(CursorState state)
    {
        switch (state)
        {
            case CursorState.Attack1:
            case CursorState.Skill1:
            case CursorState.AutoAttack:
            case CursorState.Hit:
                _fsmController.ChangeState(CursorState.Idle);
                break;
            case CursorState.Die:
                AvoidCursorGameManager.Instance.PlayerDeath();
                break;
        }
    }

    private void HandleAnimationEventRouter(string eventName)
    {
        _characterLogic?.HandleAnimationEvent(eventName);
    }

    protected void CheckForBasicAttack()
    {
        _fsmController.ChangeState(CursorState.Attack1); // FSM 전환
        _characterLogic.ExecuteBasicAttack(); // 💥 즉시 데미지 실행
    }

    protected void CheckForSkill()
    {
        if (_characterLogic.TryConsumeStamina(_characterLogic.SkillStaminaCost))
        {
            _fsmController.ChangeState(CursorState.Skill1);
            _characterLogic.ExecuteSkill();
        }
    }

    protected void CheckForItem()
    {
        if (_characterLogic.TryUseItem()) // Item은 쿨다운만 체크함
        {
            // Item 사용이 성공하면 쿨다운이 시작되고 ExecuteItem이 호출됩니다.
            // Item 사용 시 별도의 FSM 상태가 필요하다면 여기에 추가
            Debug.Log("[Player] Item Activated via Spacebar.");
        }
    }

    public void Initialize()
    {
        // 💥 FSM을 Idle 상태로 초기화합니다.
        _fsmController.ChangeState(CursorState.Idle);
    }

    public void SetHitState()
    {
        if (_fsmController.HasAnimation(CursorState.Hit))
        {
            _fsmController.ChangeState(CursorState.Hit);
        }

        // 애니메이션이 없으면? (기본 커서 등)
        // 아무것도 안 함 -> LogicBase에서 처리하는 '깜빡임'만 작동함
    }

    public void SetDeadState()
    {
        if (_fsmController.HasAnimation(CursorState.Die)) _fsmController.ChangeState(CursorState.Die);
        else AvoidCursorGameManager.Instance.PlayerDeath();
    }

    protected bool CheckForUIClick()
    {
        // (UI 클릭 로직은 이전과 동일)
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 clickPosition = transform.position;
            LayerMask UILayer = LayerMask.GetMask("UI", "UIInteractable"); ;
            Collider2D[] uiHits = Physics2D.OverlapPointAll(clickPosition, UILayer);

            if (uiHits.Length > 0)
            {
                foreach (Collider2D uiHit in uiHits)
                {
                    var clickable = uiHit.GetComponent<IUIClickable>();
                    if (clickable != null)
                    {
                        clickable.OnCursorClick(_characterLogic);
                        return true;
                    }
                }
            }
        }
        return false;
    }

    protected void MoveWithMouseDelta()
    {
        // (이동 로직은 이전과 동일)
        if (Time.timeScale == 0f) return;
        if (IsMovementLocked) return;

        //기본 속도 + 보너스 속도 (최소 5는 유지)
        float finalSpeed = Mathf.Max(5f, MoveSpeed + _characterLogic.BonusMoveSpeed);

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        bool isMoving = (Mathf.Abs(mouseX) > 0.001f || Mathf.Abs(mouseY) > 0.001f);
        UpdateMovementState(isMoving);

        Vector3 moveVector = new Vector3(mouseX, mouseY, 0) * finalSpeed * Time.deltaTime;
        Vector3 newPos = transform.position + moveVector;

        // 화면 클램핑 로직 (Orthographic 기준)
        Vector3 viewportPos = _mainCamera.WorldToViewportPoint(newPos);
        viewportPos.x = Mathf.Clamp(viewportPos.x, 0.01f, 0.99f);
        viewportPos.y = Mathf.Clamp(viewportPos.y, 0.01f, 0.99f);
        Vector3 clampedWorldPos = _mainCamera.ViewportToWorldPoint(viewportPos);
        clampedWorldPos.z = 0f;
        transform.position = clampedWorldPos;
    }

    private void UpdateMovementState(bool isMoving)
    {
        // 현재 상태가 움직임 관련 상태(Idle, Walk)일 때만 자동 전환 수행
        // (공격, 스킬, 사망 중에는 이동 상태가 바뀌면 안 됨)
        CursorState currentState = _fsmController.CurrentState;

        // 이동/대기 상태가 아니면(공격, 스킬 등) 로직 수행 안 함
        if (currentState != CursorState.Idle && currentState != CursorState.Walk)
        {
            _stopMoveTimer = 0f; // 다른 상태일 때는 타이머 초기화
            return;
        }

        if (isMoving)
        {
            // 움직이는 중이라면?
            _stopMoveTimer = 0f; // 멈춤 타이머 즉시 초기화

            if (currentState != CursorState.Walk)
            {
                _fsmController.ChangeState(CursorState.Walk);
            }
        }
        else
        {
            // 입력이 없는(멈춘) 프레임이라면?
            // 바로 Idle로 바꾸지 말고 시간을 잰다.
            _stopMoveTimer += Time.deltaTime;

            // 지정한 시간(0.15초) 이상 입력이 없을 때만 Idle로 전환
            if (_stopMoveTimer > StopThreshold && currentState != CursorState.Idle)
            {
                _fsmController.ChangeState(CursorState.Idle);
            }
        }
    }

    // <summary>
    /// 외부(Logic)에서 커서 애니메이션을 일시 정지/재개할 때 사용하는 다리 함수입니다.
    /// </summary>
    public void SetAnimationPaused(bool isPaused)
    {
        if (_cursorObject != null)
        {
            _cursorObject.SetPaused(isPaused);
        }
    }
}