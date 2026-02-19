using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image), typeof(RectTransform))]
public class CharacterCardUI : BaseCursorButton // 💥 1. BaseCursorButton 상속
{
    [Header("참조")]
    public Image CardIcon; // (캐릭터 아이콘)
    public GameObject SelectedBorder; // (n일 때 켜지는 테두리)

    private CharacterData _data;
    private CanvasGroup _canvasGroup; // 💥 추가
    public bool IsUnlocked { get; private set; } // 💥 외부 확인용

    protected override void Awake()
    {
        base.Awake();
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// 이 카드를 특정 데이터로 채웁니다.
    /// </summary>
    public void SetData(CharacterData data)
    {
        _data = data;

        // 1. 해금 여부 확인
        if (data.RequiredUnlockID == UnlockID.None)
        {
            IsUnlocked = true;
        }
        else
        {
            IsUnlocked = AvoidCursorGameManager.Instance.IsUnlocked(data.RequiredUnlockID);
        }

        // 2. 아이콘 설정
        CardIcon.sprite = data.PanelIcon;

        if (IsUnlocked)
        {
            // 해금됨: 원래 색상
            CardIcon.color = Color.white;
        }
        else
        {
            // 잠김: 검은색 실루엣 (Alpha 1, RGB 0)
            CardIcon.color = Color.black;
        }
    }

    /// <summary>
    /// 카드의 위치, 크기, 순서, 투명도를 업데이트합니다.
    /// </summary>
    public void UpdateVisuals(Vector2 targetPos, float targetScale, int sortingOrder, float targetAlpha)
    {
        var rt = (RectTransform)transform;

        // 1. 위치 (Y축은 0으로 고정되어서 옴)
        rt.DOAnchorPos(targetPos, 0.4f).SetEase(Ease.OutCubic);

        // 2. 크기
        rt.DOScale(targetScale, 0.4f).SetEase(Ease.OutCubic);

        // 3. 순서 (Z-Depth)
        rt.SetSiblingIndex(sortingOrder);

        // 4. 테두리 (중앙일 때만)
        SelectedBorder.SetActive(sortingOrder == 10);

        // 5. 💥 투명도 (자연스럽게 사라지기 위함)
        _canvasGroup.DOFade(targetAlpha, 0.4f);

        // (상호작용 차단: 투명하거나 뒤에 있으면 클릭 방지하고 싶다면 blocksRaycasts 조절)
        _canvasGroup.blocksRaycasts = (targetAlpha > 0.5f);
    }

    /// <summary>
    /// (BaseCursorButton 재정의)
    /// 이 카드는 클릭 시 메인 컨트롤러에게 "나를 중앙으로!"라고 알립니다.
    /// (단, 이미 중앙이면 무시)
    /// </summary>
    public override void OnCursorClick(PlayerLogicBase cursor)
    {
        if (SelectedBorder.activeSelf) return; // 이미 중앙이면 무시

        // 부모의 OnClick 이벤트 호출 (컨트롤러가 구독)
        base.OnCursorClick(cursor);
    }
    public CharacterData GetData() => _data;
}