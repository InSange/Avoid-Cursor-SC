using DG.Tweening;
using System;
using TMPro;
using UnityEngine;

public class ResultPanelUI : MonoBehaviour
{
    [Header("UI 참조")]
    public TextMeshProUGUI SurvivalTimeText;
    public TextMeshProUGUI UnlocksText; // 해금 목록

    [Header("Animation")]
    public CanvasGroup PanelCanvasGroup; // 전체 페이드용
    public RectTransform ContentContainer; // 텍스트들이 담긴 부모 (LayoutGroup 권장)

    private Action _onConfirmCallback;
    private bool _isWaitingForInput = false;

    private void Update()
    {
        // 💥 Space 키를 누르면 허브로 복귀
        if (_isWaitingForInput && Input.GetKeyDown(KeyCode.Space))
        {
            OnClose();
        }
    }

    /// <summary>
    /// 결과 데이터를 받아 UI에 표시합니다.
    /// </summary>
    public void ShowResult(GameResultData data, Action onConfirm)
    {
        gameObject.SetActive(true);
        _onConfirmCallback = onConfirm;

        _isWaitingForInput = false;
        Invoke(nameof(EnableInput), 0.5f);

        int minutes = Mathf.FloorToInt(data.SurvivalTime / 60F);
        int seconds = Mathf.FloorToInt(data.SurvivalTime % 60F);
        SurvivalTimeText.text = $"생존 시간 : {minutes:00}m {seconds:00}s";

        if (data.EarnedUnlocks != null && data.EarnedUnlocks.Count > 0)
        {
            string unlockStr = "획득한 보상\n";
            foreach (var id in data.EarnedUnlocks)
            {
                unlockStr += $"- {id}\n";
            }
            UnlocksText.text = unlockStr;
        }
        else
        {
            UnlocksText.text = "획득한 보상 없음";
        }

        PanelCanvasGroup.alpha = 0f;
        PanelCanvasGroup.DOFade(1f, 0.5f); // 배경 페이드 인

        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(0.2f);

        SurvivalTimeText.alpha = 0;
        SurvivalTimeText.transform.localScale = Vector3.one * 1.5f;
        seq.Append(SurvivalTimeText.DOFade(1, 0.3f));
        seq.Join(SurvivalTimeText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));

        // Unlocks Text
        UnlocksText.alpha = 0;
        seq.AppendInterval(0.1f);
        seq.Append(UnlocksText.DOFade(1, 0.5f));
        seq.Join(UnlocksText.rectTransform.DOAnchorPosY(0, 0.5f).From(new Vector2(0, -50))); // 아래에서 위로

        _isWaitingForInput = false;
        Invoke(nameof(EnableInput), seq.Duration()); // 애니메이션 끝나면 입력 허용
    }

    private void EnableInput()
    {
        _isWaitingForInput = true;
    }

    private void OnClose()
    {
        _isWaitingForInput = false;
        gameObject.SetActive(false);
        _onConfirmCallback?.Invoke();
    }
}