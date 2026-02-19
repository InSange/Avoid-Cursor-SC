using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events; // 💥 1. UnityEvent를 사용하기 위해 추가
using System.Collections; // 💥 2. Coroutine을 위해 추가

/// <summary>
/// PlayerCursor의 물리 클릭에 반응하는 모든 UI 버튼의 부모 클래스.
/// Hover 피드백, Collider 자동 동기화, OnClick 이벤트를 제공합니다.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public abstract class BaseCursorButton : MonoBehaviour, IUIClickable
{
    [Header("UI 피드백 (선택 사항)")]
    [Tooltip("(선택) 루트 오브젝트에 Image가 있다면 할당하세요.")]
    public Sprite NormalSprite;
    [Tooltip("(선택) 루트 오브젝트에 Image가 있다면 할당하세요.")]
    public Sprite HighlightedSprite;
    [Tooltip("(선택) 루트 오브젝트에 Image가 있다면 할당하세요.")]
    public Sprite PressedSprite;
    [Header("클릭 이벤트")]
    [Tooltip("클릭 시 호출될 UnityEvent")]
    public UnityEvent OnClick; // 💥 3. UIManager가 할당할 이벤트

    // --- 내부 참조 ---
    protected Image _rootImage;
    protected BoxCollider2D _collider;
    protected RectTransform _rectTransform;

    protected virtual void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();

        TryGetComponent<Image>(out _rootImage);

        _collider.isTrigger = true;

        // 2. 기본 스프라이트 설정
        if (_rootImage != null && NormalSprite != null)
            _rootImage.sprite = NormalSprite;
    }

    protected virtual void Start()
    {
        SyncColliderToRectTransform();
    }

    /// <summary>
    /// RectTransform의 크기를 BoxCollider2D에 동기화합니다.
    /// </summary>
    protected virtual void SyncColliderToRectTransform()
    {
        if (_collider == null || _rectTransform == null) return;
        _collider.size = _rectTransform.rect.size;
    }

    protected virtual void OnEnable()
    {
        // 활성화되는 순간에는 레이아웃이 덜 잡혔을 수 있으므로, 
        // 한 프레임 뒤에 맞추도록 코루틴을 쓰거나 Start를 기다림.
        // 여기서는 안전하게 코루틴 호출
        StartCoroutine(SyncRoutine());
    }

    private IEnumerator SyncRoutine()
    {
        // 엔드 오브 프레임까지 대기하여 UI 레이아웃 계산이 끝나기를 기다림
        yield return new WaitForEndOfFrame();
        SyncColliderToRectTransform();
    }

    // (에디터 편의 기능)
#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (_collider == null) _collider = GetComponent<BoxCollider2D>();
        if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
        SyncColliderToRectTransform();
    }
#endif

    // --- 💥 4. IUIClickable (클릭 구현) ---
    public virtual void OnCursorClick(PlayerLogicBase cursor)
    {
        Debug.Log($"[BaseCursorButton] {name} 클릭됨!");

        // 1. Pressed 피드백 (잠깐)
        StartCoroutine(OnPressedFeedback());

        // 2. 할당된 OnClick 이벤트 호출
        OnClick?.Invoke();
    }

    protected virtual IEnumerator OnPressedFeedback()
    {
        // (기본 구현: 루트 이미지가 있다면 스프라이트 교체)
        if (_rootImage != null && PressedSprite != null)
            _rootImage.sprite = PressedSprite;

        yield return new WaitForSeconds(0.1f);

        if (_rootImage != null && _rootImage.sprite == PressedSprite)
        {
            _rootImage.sprite = HighlightedSprite; // (Hover 상태로 복귀)
        }
    }

    /// <summary>
    /// 커서가 진입했을 때(Hover) 피드백입니다.
    /// </summary>
    protected virtual void OnHoverEnter()
    {
        // (기본 구현: 루트 이미지가 있다면 스프라이트 교체)
        if (_rootImage != null && HighlightedSprite != null)
            _rootImage.sprite = HighlightedSprite;
    }

    /// <summary>
    /// 커서가 떠났을 때(Hover Exit) 피드백입니다.
    /// </summary>
    protected virtual void OnHoverExit()
    {
        // (기본 구현: 루트 이미지가 있다면 스프라이트 교체)
        if (_rootImage != null && NormalSprite != null)
            _rootImage.sprite = NormalSprite;
    }

    // --- 💥 5. Hover 피드백 (물리 트리거) ---
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            OnHoverEnter(); // 💥 가상 메서드 호출
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            OnHoverExit(); // 💥 가상 메서드 호출
        }
    }
}