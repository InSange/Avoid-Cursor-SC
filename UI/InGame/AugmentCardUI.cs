using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AugmentCardUI : BaseCursorButton
{
    [Header("UI 참조")]
    public CanvasGroup CanvasGroup;
    public TextMeshProUGUI TitleText;
    public TextMeshProUGUI DescText;
    public Image IconImage;
    public GameObject DebuffBorder; // 디버프일 때 켜질 빨간 테두리

    private AugmentData _data;
    private Action<AugmentData> _onSelected;

    public AugmentData GetData() => _data;

    public void Setup(AugmentData data, Action<AugmentData> onSelected)
    {
        _data = data;
        _onSelected = onSelected;

        TitleText.text = data.Title;
        DescText.text = data.Description;
        if (data.Icon != null) IconImage.sprite = data.Icon;

        if (DebuffBorder != null)
            DebuffBorder.SetActive(data.IsDebuff);

        // (BaseCursorButton 초기화는 Awake에서 자동 처리됨)
    }

    public void PlaySelectedAnimation()
    {
        GetComponent<BoxCollider2D>().enabled = false;

        transform.DOScale(1.2f, 0.5f).SetEase(Ease.OutBack);
    }

    public void PlayDismissAnimation()
    {
        gameObject.SetActive(false);
    }

    public override void OnCursorClick(PlayerLogicBase cursor) // (인터페이스 수정에 따라 PlayerLogicBase일 수 있음)
    {
        base.OnCursorClick(cursor); // 클릭 연출
        _onSelected?.Invoke(_data); // 선택 콜백 호출
    }
}