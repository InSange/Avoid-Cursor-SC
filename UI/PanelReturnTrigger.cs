using UnityEngine;
using DG.Tweening;

/// <summary>
/// 판넬 가장자리에 위치하여, 마우스를 올리면 허브 방향으로 카메라를 살짝 이동시키고
/// 클릭 시 허브로 완전히 복귀하게 하는 트리거 스크립트입니다.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class PanelReturnTrigger : MonoBehaviour, IUIClickable
{
    [Header("참조")]
    public HubCameraController CameraController;
    public RectTransform ParentPanel; // 이 트리거가 속한 판넬 (Codex 또는 Character)

    [Header("예외 처리")]
    [Tooltip("이 패널이 연결된 UI 컨트롤러 (상세창 열림 확인용)")]
    public CodexPanelUI CodexController;

    [Header("설정")]
    [Tooltip("허브 방향으로 얼마나 살짝 이동할지")]
    public float PeekOffset = 100f;
    [Tooltip("살짝 움직이는 연출 시간")]
    public float PeekDuration = 0.1f;

    private BoxCollider2D _collider;
    private Vector3 _panelWorldPos;
    private bool _isActive = false;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        _collider.isTrigger = true;
        // 초기에는 트리거를 꺼둡니다 (카메라 이동 완료 전 오작동 방지)
        SetTriggerActive(false);
    }

    private void Start()
    {
        if (CameraController == null) CameraController = FindObjectOfType<HubCameraController>();

        // 부모 판넬의 월드 좌표 미리 계산 (복귀용)
        if (ParentPanel != null)
        {
            float canvasScale = CameraController.RootCanvas.transform.localScale.x;
            _panelWorldPos = new Vector3(
                ParentPanel.localPosition.x * canvasScale,
                ParentPanel.localPosition.y * canvasScale,
                -10f // 카메라 Z축
            );
        }
    }

    /// <summary>
    /// 카메라가 판넬 이동을 마쳤을 때 HubUIManager 등에 의해 호출됩니다.
    /// </summary>
    public void SetTriggerActive(bool active)
    {
        _isActive = active;
        // 트리거 컴포넌트 자체를 껐다 켜서 물리 연산 최적화 및 오작동 방지
        _collider.enabled = active;
    }

    // --- Hover 연출 (훔쳐보기) ---

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isActive || !other.CompareTag("Player")) return;

        if (CodexController != null && CodexController.IsDetailViewOpen) return;

        // 허브 방향으로 살짝 이동
        // 도감(왼쪽)이면 오른쪽(+)으로, 캐릭터(오른쪽)이면 왼쪽(-)으로 이동해야 함
        float direction = (ParentPanel.localPosition.x < 0) ? 1f : -1f;
        float canvasScale = CameraController.RootCanvas.transform.localScale.x;
        Vector3 peekPos = _panelWorldPos + new Vector3(PeekOffset * direction * canvasScale, 0, 0);

        CameraController.MainCamera.transform.DOMove(peekPos, PeekDuration).SetEase(Ease.OutCubic);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (CodexController != null && CodexController.IsDetailViewOpen) return;

        if (!_isActive || !other.CompareTag("Player")) return;

        // 다시 원래 판넬 중심으로 복귀
        CameraController.MainCamera.transform.DOMove(_panelWorldPos, PeekDuration).SetEase(Ease.OutCubic);
    }

    // --- Click 시 복귀 ---

    public void OnCursorClick(PlayerLogicBase cursor)
    {
        if (CodexController != null && CodexController.IsDetailViewOpen) return;
        if (!_isActive) return;

        Debug.Log($"[PanelReturnTrigger] {ParentPanel.name}에서 허브로 복귀합니다.");

        // 트리거 비활성화 (이동 중에 다시 눌리는 것 방지)
        SetTriggerActive(false);

        // 허브로 실제 이동
        CameraController.ReturnToHub();
    }
}