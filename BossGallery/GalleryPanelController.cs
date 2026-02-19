using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


/// <summary>
/// (구 SystemFeaturePanelController)
/// 허브 씬의 상단 드롭다운 UI.
/// 해금된 보스 NPC 현황을 보여주며, '보스 갤러리' 씬으로 입장하는 버튼을 포함합니다.
/// </summary>
public class GalleryPanelController : MonoBehaviour
{
    [Header("UI 참조")]
    public RectTransform PanelTransform;
    public Image ToggleButtonIcon;
    public Sprite IconExpand; // [v]
    public Sprite IconCollapse; // [ㅅ]

    [Tooltip("상단 미리보기 트리거 (자동 비활성화용)")]
    public GalleryPeekTrigger PeekTrigger;

    [Header("버튼 (BaseCursorButton)")]
    [Tooltip("패널을 열고 닫는 토글 버튼")]
    public BaseCursorButton ToggleCursorButton;

    [Tooltip("보스 아이콘 목록 (SystemModuleEntry)")]
    public BossGalleryEntry[] FeatureEntries;

    [Header("애니메이션")]
    public float PreviewOffsetY = 1050;
    public float ExpandedOffsetY = 50f;
    public float HiddenOffsetY = 1080;
    public float AnimationDuration = 0.5f;

    private Dictionary<UnlockID, BossGalleryEntry> _featureMap;
    [SerializeField] private PanelState _currentState = PanelState.Hidden;

    public PanelState CurrentState => _currentState;

    private void Awake()
    {
        _featureMap = new();
        var gameManager = AvoidCursorGameManager.Instance;

        foreach (var entry in FeatureEntries)
        {
            if (entry.BossUnlockID == UnlockID.None) continue;
            _featureMap[entry.BossUnlockID] = entry;

            bool isUnlocked = gameManager.IsUnlocked(entry.BossUnlockID);

            entry.gameObject.SetActive(true);
            entry.Initialize(isUnlocked);
        }
    }

    private void OnEnable()
    {
        if (AvoidCursorGameManager.Instance != null)
            AvoidCursorGameManager.Instance.OnUnlockAchieved += HandleUnlock;
    }

    private void OnDisable()
    {
        if (AvoidCursorGameManager.Instance != null)
            AvoidCursorGameManager.Instance.OnUnlockAchieved -= HandleUnlock;
    }

    /// <summary>
    /// 새로운 요소가 해금되었을 때 호출됩니다.
    /// </summary>
    private void HandleUnlock(UnlockID id)
    {
        if (_featureMap.TryGetValue(id, out var entry))
        {
            if (entry != null)
            {
                entry.Initialize(true); // 해금 연출 재생
            }
        }
    }

    private void Start()
    {
        if (ToggleCursorButton != null)
        {
            ToggleCursorButton.OnClick.AddListener(OnToggleClicked);
        }
        else Debug.LogWarning("[GalleryPanel] ToggleCursorButton이 할당되지 않았습니다.");

        if (PeekTrigger == null)
            PeekTrigger = GetComponentInChildren<GalleryPeekTrigger>();
    }

/*    private void Update()
    {
        Vector2 mousePos = Input.mousePosition;
        if (_currentState == PanelState.Hidden && mousePos.y >= Screen.height - 40f)
        {
            ShowPreview();
        }
    }*/

    private void OnToggleClicked()
    {
        if (_currentState == PanelState.Expanded)
            HidePanel();
        else
            ExpandPanel();
    }

    public void ShowPreview()
    {
        if (_currentState == PanelState.Expanded) return;

        _currentState = PanelState.Previewing;
        MovePanelTo(PreviewOffsetY);
    }

    private void ExpandPanel()
    {
        _currentState = PanelState.Expanded;
        ToggleButtonIcon.sprite = IconCollapse;
        MovePanelTo(ExpandedOffsetY);
    }

    public void HidePanel()
    {
        _currentState = PanelState.Hidden;
        ToggleButtonIcon.sprite = IconExpand;
        MovePanelTo(HiddenOffsetY);
    }

    private void MovePanelTo(float targetY)
    {
        PanelTransform.DOAnchorPosY(targetY, AnimationDuration).SetEase(Ease.OutCubic);
    }
}
