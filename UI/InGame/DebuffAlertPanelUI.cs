using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebuffAlertPanelUI : MonoBehaviour
{
    [Header("UI 참조")]
    public CanvasGroup PanelGroup; // 투명도 제어용
    public TextMeshProUGUI DescriptionText;
    public Image BackgroundImage; // (빨간색 배경)

    [Header("연출 설정")]
    public float FadeDuration = 0.5f;
    public float DisplayDuration = 2.0f; // 떠 있는 시간

    private void Awake()
    {
        // 초기화
        PanelGroup.alpha = 0f;
        PanelGroup.blocksRaycasts = false; // 클릭 방지
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 경고창을 띄우고, 연출이 끝날 때까지 대기하는 코루틴을 반환합니다.
    /// </summary>
    public IEnumerator ShowAlertRoutine(string description)
    {
        gameObject.SetActive(true);
        DescriptionText.text = description;

        // 1. Fade In (0 -> 1)
        yield return PanelGroup.DOFade(1f, FadeDuration).WaitForCompletion();

        // 2. 대기
        yield return new WaitForSeconds(DisplayDuration);

        // 3. Fade Out (1 -> 0)
        yield return PanelGroup.DOFade(0f, FadeDuration).WaitForCompletion();

        gameObject.SetActive(false);
    }
}