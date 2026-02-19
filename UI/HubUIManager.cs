using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 허브 씬의 UI 상태를 '대기' 모드와 '게임 중' 모드로 전환합니다.
/// </summary>
public class HubUIManager : MonoBehaviour
{
    [Header("게임 관리자")]
    public InfiniteModeManager ModeManager;

    [Header("핵심 UI")]
    public GameObject PlayButtonObject;

    [Header("Result UI")]
    public ResultPanelUI ResultPanel;

    [Header("Pause UI")]
    public PausePanelUI PausePanel;

    [Header("허브 UI 패널 (게임 중 비활성화)")]
    public UIEntryAnimator GalleryPanelAnim;   // Top 갤러리
    public UIEntryAnimator HUDPanelAnim;       // Left HUD판넬 -> 전투시 나타남.
    public UIEntryAnimator CharacterPanelAnim;   // Right  캐릭터 판넬
    public UIEntryAnimator CharacterBtnAnim; // Righrt 캐릭터 판넬 이동용 버튼
    public UIEntryAnimator CodexPanelAnim; // Left 도감 판넬
    public UIEntryAnimator CodexBtnAnim; // Left 도감 판넬 이동용 버튼

    [Header("Codex")]
    public CodexPanelUI CodexPanelController; // (CodexPanel 오브젝트의 스크립트)

    [Header("Augment & Alert UI")]
    public AugmentSelectionPanel AugmentPanel; 
    public DebuffAlertPanelUI DebuffAlertPanel;

    private PlayButtonHandler _playButton;

    [Header("Camera Control")]
    public HubCameraController CameraController; // 할당 필요
    public RectTransform CodexPanelRect;         // 이동할 목표 위치 (World Space UI)
    public RectTransform CharacterPanelRect;

    [Header("Buttons")]
    public BaseCursorButton CodexOpenButton;
    public BaseCursorButton CharacterOpenButton;

    [Header("Return Triggers")]
    public PanelReturnTrigger CodexReturnTrigger;
    public PanelReturnTrigger CharacterReturnTrigger;

    /// <summary>
    /// 결과창이나 퍼즈창이 떠 있으면 true (인게임 입력 차단 필요)
    /// </summary>
    public bool IsMenuOpen => (ResultPanel != null && ResultPanel.gameObject.activeSelf) ||
                              (PausePanel != null && PausePanel.gameObject.activeSelf);

    private void Start()
    {
        if (PlayButtonObject != null)
            _playButton = PlayButtonObject.GetComponent<PlayButtonHandler>();
        // 도감 열기 기능
        if (CodexOpenButton != null)
            CodexOpenButton.OnClick.AddListener(() =>
            {
                CodexReturnTrigger.SetTriggerActive(false);
                CharacterReturnTrigger.SetTriggerActive(false);

                CameraController.MoveToPanel(CodexPanelRect);
                CodexPanelController.OnPanelOpen(); // 내용 갱신

                DOVirtual.DelayedCall(CameraController.TransitionDuration, () => CodexReturnTrigger.SetTriggerActive(true));
            });
        // 캐릭터 판넬 열기 기능
        if (CharacterOpenButton != null)
            CharacterOpenButton.OnClick.AddListener(() =>
            {
                CodexReturnTrigger.SetTriggerActive(false);
                CharacterReturnTrigger.SetTriggerActive(false);

                CameraController.MoveToPanel(CharacterPanelRect);

                DOVirtual.DelayedCall(CameraController.TransitionDuration, () => CharacterReturnTrigger.SetTriggerActive(true));
            });

        // UIManager가 버튼의 OnClick 이벤트를 구독 (직접 할당)
        if (_playButton != null)
        {
            _playButton.OnClick.AddListener(OnPlayButtonClicked);
        }
        else
        {
            Debug.LogError("[HubUIManager] PlayButtonHandler를 찾을 수 없습니다!");
        }

        AvoidCursorGameManager.Instance.OnGameResult += HandleGameResult;

        SwitchToHubState(isInitialStart: true);
    }

    // 편의 메서드: 증강 패널 열기
    public void OpenAugmentPanel(List<AugmentData> pool, int count, Action<AugmentData> onChosen)
    {
        if (AugmentPanel != null)
            AugmentPanel.Show(pool, count, onChosen);
    }

    public void OpenCodex()
    {
        CodexPanelController.OnPanelOpen(); // 내용 갱신
    }

    // 편의 메서드: 경고창 띄우기 (코루틴 반환)
    public IEnumerator ShowDebuffAlert(string description)
    {
        if (DebuffAlertPanel != null)
            yield return StartCoroutine(DebuffAlertPanel.ShowAlertRoutine(description));
        else
            yield return new WaitForSeconds(2.0f); // UI 없으면 대기
    }

    private void HandleGameResult(GameResultData result)
    {
        // 결과창 표시
        ResultPanel.ShowResult(result, () =>
        {
            // 확인 버튼 누르면 허브 상태로 복귀
            SwitchToHubState(isInitialStart: false);
        });
    }

    /// <summary>
    /// 'Play' 버튼을 눌렀을 때
    /// </summary>
    public void OnPlayButtonClicked()
    {
        SwitchToGameState();
    }

    /// <summary>
    /// '대기 상태' -> '게임 진행 상태'로 전환
    /// (UI 숨기기, 게임 시작)
    /// </summary>
    public void SwitchToGameState()
    {
        PlayButtonObject.gameObject.SetActive(false);

        if (GalleryPanelAnim) GalleryPanelAnim.PlayExitAnimation();
        if (CharacterPanelAnim) CharacterPanelAnim.PlayExitAnimation();
        if (CharacterBtnAnim) CharacterBtnAnim.PlayExitAnimation();
        if (CodexPanelAnim) CodexPanelAnim.PlayExitAnimation();
        if (CodexBtnAnim) CodexBtnAnim.PlayExitAnimation();

        if (HUDPanelAnim) HUDPanelAnim.PlayEntryAnimation();

        // 게임 시작 시 팝업류 확실히 닫기
        if (ResultPanel) ResultPanel.gameObject.SetActive(false);
        if (PausePanel) PausePanel.Hide();
        if (AugmentPanel) AugmentPanel.gameObject.SetActive(false);

        // InfiniteModeManager에게 게임 시작을 알림
        ModeManager.StartInfiniteMode();
    }

    /// <summary>
    /// '게임 진행 상태' -> '대기 상태'로 전환
    /// (UI 표시, 게임 오버)
    /// </summary>
    public void SwitchToHubState(bool isInitialStart = false)
    {
        if (isInitialStart)
        {
            AvoidCursorGameManager.Instance.IsGameOver = false;
        }

        if (ResultPanel != null) ResultPanel.gameObject.SetActive(false);
        if (PausePanel != null) PausePanel.Hide();
        if (AugmentPanel != null) AugmentPanel.gameObject.SetActive(false);
        if (DebuffAlertPanel != null) DebuffAlertPanel.gameObject.SetActive(false);

        PlayButtonObject.gameObject.SetActive(true);

        if (GalleryPanelAnim) GalleryPanelAnim.PlayEntryAnimation();
        if (CharacterPanelAnim) CharacterPanelAnim.PlayEntryAnimation();
        if (CharacterBtnAnim) CharacterBtnAnim.PlayEntryAnimation();
        if (CodexPanelAnim) CodexPanelAnim.PlayEntryAnimation();
        if (CodexBtnAnim) CodexBtnAnim.PlayEntryAnimation();

        if (HUDPanelAnim) HUDPanelAnim.PlayExitAnimation(); // (HUD는 허브에서 안보이게 하려면 조건 필요 -> 아래 3번 항목 참조)

        AvoidCursorGameManager.Instance.SpawnCursor();
    }
}