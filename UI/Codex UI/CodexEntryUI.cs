using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// '도감' UI에 표시될 퀘스트/해금 항목 1개의 UI를 제어합니다.
/// </summary>
public class CodexEntryUI : MonoBehaviour
{
    [Header("UI 참조")]
    public TextMeshProUGUI QuestNameText;
    public TextMeshProUGUI QuestDescriptionText;
    public Image QuestIcon; // (선택 사항)
    public GameObject LockOverlay; // (미해금 시 덮어씌울 자물쇠/음영)
    public GameObject RewardsPanel; // (해금 시 보상 목록을 표시할 패널)
    public TextMeshProUGUI RewardsText; // (보상 텍스트)

    /// <summary>
    /// 이 UI 항목을 초기화합니다.
    /// </summary>
    /// <param name="questData">표시할 퀘스트 데이터</param>
    /// <param name="isUnlocked">현재 해금 여부</param>
    /// <param name="currentProgress">현재 진행도 (미해금 시)</param>
    public void Initialize(QuestData questData, bool isUnlocked, int currentProgress)
    {
        if (isUnlocked)
        {
            // --- 1. 해금된 퀘스트 ---
            LockOverlay.SetActive(false);
            RewardsPanel.SetActive(true);

            QuestNameText.text = questData.QuestName;
            QuestDescriptionText.text = questData.QuestDescription;
            // QuestIcon.sprite = questData.Icon; // (QuestData에 아이콘이 있다면)

            // 보상 목록 표시
            string rewardStr = "보상: ";
            foreach (var reward in questData.Rewards)
            {
                rewardStr += $"{reward.ToString()}, "; // (나중에 이름으로 변환)
            }
            RewardsText.text = rewardStr;
        }
        else
        {
            // --- 2. 미해금 (잠긴) 퀘스트 ---
            LockOverlay.SetActive(true);
            RewardsPanel.SetActive(false);

            // (기획에 따라) 해금 전에는 "???"로 표시
            QuestNameText.text = "???";
            QuestDescriptionText.text = $"[ 퀘스트 목표: {questData.QuestDescription} ]\n" +
                                        $"(진행도: {currentProgress} / {questData.TargetValue})";
        }
    }
}