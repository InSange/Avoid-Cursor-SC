using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// (구 SystemFeatureEntry)
/// 갤러리 패널에 표시되는 개별 보스 아이콘.
/// 클릭 기능이 제거되고, 해금 상태만 표시합니다.
/// </summary>
public class BossGalleryEntry : BaseCursorButton
{
    [Header("참조")]
    [Tooltip("이 아이콘이 대표하는 보스의 'NPC 해금 ID' (예: BossNPC_DemonSword)")]
    public UnlockID BossUnlockID; // SystemFeature -> UnlockID로 변경

    [Header("참조")]
    public Image SystemImage;
    public GameObject LockIcon;
    public GameObject HighlightBorder;

    private bool _isUnlocked;

    protected override void Awake()
    {
        // 4. 부모(BaseCursorButton)의 Awake를 먼저 실행
        base.Awake();

        // (이 스크립트의 고유 로직은 유지)
        SystemImage.enabled = false;
        LockIcon.SetActive(true);
        HighlightBorder.SetActive(false);
    }

    public void Initialize(bool isUnlocked)
    {
        _isUnlocked = isUnlocked;

        SystemImage.enabled = isUnlocked;
        LockIcon.SetActive(!isUnlocked);
        HighlightBorder.SetActive(false);

        if (isUnlocked)
        {
            // 연출 효과
            transform.localScale = Vector3.zero;
            var cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0;

            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
            seq.Join(cg.DOFade(1f, 0.3f));
        }
    }

    protected override void OnHoverEnter()
    {
        // (부모의 스프라이트 교체 대신, HighlightBorder를 켭니다)
        if (_isUnlocked)
            HighlightBorder.SetActive(true);
    }

    protected override void OnHoverExit()
    {
        // (부모의 스프라이트 복원 대신, HighlightBorder를 끕니다)
        HighlightBorder.SetActive(false);
    }

    /// <summary>
    /// 해금된 아이콘 클릭 시, 갤러리 씬으로 이동합니다.
    /// </summary>
    public override void OnCursorClick(PlayerLogicBase cursor)
    {
        // 1. 잠겨있으면 클릭 무시
        if (!_isUnlocked)
        {
            Debug.Log("[BossGalleryEntry] 아직 해금되지 않았습니다.");
            return;
        }

        if (BossUnlockID == UnlockID.None)
        {
            Debug.LogError("[BossGalleryEntry] BossUnlockID가 설정되지 않았습니다!");
            return;
        }

        // 2. 부모의 클릭 로직 실행 (PressedSprite 피드백, OnClick 이벤트 호출)
        base.OnCursorClick(cursor);

        // 3. 갤러리 씬 이동 로직 실행
        Debug.Log($"[BossGalleryEntry] {BossUnlockID} 갤러리로 이동합니다.");
        AvoidCursorGameManager.Instance.SetGalleryContext(this.BossUnlockID);
        SceneTransitionManager.Instance.LoadGalleryScene(); // (신규)
    }
}