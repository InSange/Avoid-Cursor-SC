using UnityEngine;
using DG.Tweening;

/// <summary>
/// 'Q' 키를 눌러 허브 패널과 다른 패널 간의
/// 카메라 이동(X/Y) 및 줌(OrthoSize)을 토글합니다.
/// (Orthographic 카메라 + World Space Canvas 전용)
/// </summary>
public class HubCameraController : MonoBehaviour
{
    [Header("카메라 설정")]
    [Tooltip("씬의 메인 카메라 (Orthographic)")]
    public Camera MainCamera;
    [Tooltip("애니메이션에 걸리는 시간")]
    public float TransitionDuration = 0.8f;
    [Tooltip("애니메이션 Ease 효과")]
    public Ease TransitionEase = Ease.OutQuad;

    [Header("대상 패널")]
    [Tooltip("중앙 허브 패널 (RectTransform)")]
    public RectTransform HubPanel;
    public RectTransform CodexPanel;     // 도감 (왼쪽)
    public RectTransform CharacterPanel; // 캐릭터 (오른쪽 등)

    [Header("캔버스")]
    [Tooltip("World Space Canvas (스케일 계산용)")]
    public Canvas RootCanvas;

    [Header("줌(Zoom) 설정")]
    [Tooltip("허브일 때의 카메라 줌 크기 (기본값 5)")]
    public float HubOrthoSize = 5f;

    private float _canvasScale = 0.01f;
    private Transform _cameraTransform;
    private bool _isAtHub = true; // 현재 상태 추적용도

    public bool IsAtHub => _isAtHub; // 외부 확인용도

    void Start()
    {
        if (MainCamera == null) MainCamera = Camera.main;
        _cameraTransform = MainCamera.transform;

        if (RootCanvas != null)
            _canvasScale = RootCanvas.transform.localScale.x;
    }

    /// <summary>
    /// 외부(UI 버튼)에서 호출: 특정 패널로 카메라 이동
    /// </summary>
    public void MoveToPanel(RectTransform targetPanel)
    {
        if (targetPanel == null) return;

        _isAtHub = (targetPanel == HubPanel);

        Vector3 targetLocalPos = targetPanel.localPosition;
        float targetWorldX = targetLocalPos.x * _canvasScale;
        float targetWorldY = targetLocalPos.y * _canvasScale;
        Vector3 currentCamPos = _cameraTransform.position;


        _cameraTransform.DOKill();
        MainCamera.DOKill();

        Vector3 targetPos = new Vector3(targetWorldX, targetWorldY, currentCamPos.z);
        _cameraTransform.DOMove(targetPos, TransitionDuration).SetEase(TransitionEase);
    }

    /// <summary>
    /// 허브로 복귀하는 편의 메서드
    /// </summary>
    public void ReturnToHub()
    {
        MoveToPanel(HubPanel);
    }
}