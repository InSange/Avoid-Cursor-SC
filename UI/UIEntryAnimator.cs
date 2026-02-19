using UnityEngine;
using DG.Tweening;

public class UIEntryAnimator : MonoBehaviour
{
    public enum EntryDirection { Top, Bottom, Left, Right }

    [Header("설정")]
    public EntryDirection Direction;
    public float AnimationDuration = 0.5f;
    public float Delay = 0f;
    public Ease EaseType = Ease.OutCubic;
    public float Offset = 5f;

    private RectTransform _rect;
    private Vector2 _originalPos;
    private Vector2 _hiddenPos;
    private bool _isInitialized = false;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _originalPos = _rect.anchoredPosition; // 에디터에 배치된 위치를 목표 지점으로 저장
        CalculateHiddenPosition();
        _isInitialized = true;
    }

    private void CalculateHiddenPosition()
    {
        // 화면 밖 좌표 계산 (Canvas 크기나 고정값 사용)
        // 간단하게 1000 정도 오프셋을 줍니다.
        switch (Direction)
        {
            case EntryDirection.Top: _hiddenPos = _originalPos + new Vector2(0, Offset); break;
            case EntryDirection.Bottom: _hiddenPos = _originalPos + new Vector2(0, -Offset); break;
            case EntryDirection.Left: _hiddenPos = _originalPos + new Vector2(-Offset, 0); break;
            case EntryDirection.Right: _hiddenPos = _originalPos + new Vector2(Offset, 0); break;
        }
    }

    /// <summary>
    /// UI를 화면 밖으로 즉시 이동시키고, 안으로 들어오는 애니메이션 재생
    /// </summary>
    public void PlayEntryAnimation()
    {
        if (!_isInitialized) Awake();

        gameObject.SetActive(true);
        _rect.anchoredPosition = _hiddenPos; // 시작 위치로 이동

        _rect.DOAnchorPos(_originalPos, AnimationDuration)
            .SetDelay(Delay)
            .SetEase(EaseType);
    }

    /// <summary>
    /// UI를 화면 밖으로 내보내고 비활성화 (게임 시작 시 사용)
    /// </summary>
    public void PlayExitAnimation()
    {
        _rect.DOAnchorPos(_hiddenPos, AnimationDuration)
            .SetEase(Ease.InCubic)
            .OnComplete(() => gameObject.SetActive(false));
    }
}