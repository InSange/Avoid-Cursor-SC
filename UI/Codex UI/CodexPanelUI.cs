using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodexPanelUI : MonoBehaviour
{
    [Header("Data")]
    public CodexDatabase Database; // (Inspector 할당)

    [Header("Tabs")]
    public BaseCursorButton TabActive;
    public BaseCursorButton TabArtifact;
    public BaseCursorButton TabBoss;
    public GameObject HighlightActive;   // 토글 하이라이트
    public GameObject HighlightArtifact;
    public GameObject HighlightBoss;

    [Header("List View")]
    public Transform SlotContainer; // Content (Grid Layout)
    public GameObject SlotPrefab;   // CodexSlotUI 프리팹

    [Header("Detail View (Popup)")]
    public GameObject DetailPanelObject;
    public Image DetailIcon;
    public TextMeshProUGUI DetailName;
    public TextMeshProUGUI DetailDesc;

    public bool IsDetailViewOpen => DetailPanelObject != null && DetailPanelObject.activeSelf;

    [Header("Detail Buttons")]
    public GameObject EquipButtonObject; // 장착 버튼 (ActiveItem 전용)
    public TextMeshProUGUI EquipButtonText; // "장착" vs "장착중"
    public BaseCursorButton CloseDetailButton;

    private List<GameObject> _spawnedSlots = new List<GameObject>();
    private CodexCategory _currentCategory = CodexCategory.ActiveItem;
    private CodexEntry _currentSelectedEntry;

    private void Start()
    {
        // 탭 버튼 리스너 (물리 버튼 가정)
        if (TabActive) TabActive.OnClick.AddListener(() => SwitchTab(CodexCategory.ActiveItem));
        if (TabArtifact) TabArtifact.OnClick.AddListener(() => SwitchTab(CodexCategory.Artifact));
        if (TabBoss) TabBoss.OnClick.AddListener(() => SwitchTab(CodexCategory.Boss));

        // 상세창 닫기 버튼
        if (CloseDetailButton) CloseDetailButton.OnClick.AddListener(CloseDetailView);

        // 장착 버튼 리스너 (추후 구현)
        var equipBtn = EquipButtonObject.GetComponent<BaseCursorButton>();
        if (equipBtn) equipBtn.OnClick.AddListener(OnEquipClicked);

        // 초기 상태: 상세창 숨김 (화면 아래쪽 등에 배치)
        DetailPanelObject.gameObject.SetActive(false);
    }

    /// <summary>
    /// 패널이 열릴 때(카메라가 비출 때) 호출
    /// </summary>
    public void OnPanelOpen()
    {
        SwitchTab(CodexCategory.ActiveItem); // 기본 탭으로 시작
    }

    private void SwitchTab(CodexCategory category)
    {
        _currentCategory = category;

        // 1. 하이라이트 갱신
        if (HighlightActive) HighlightActive.SetActive(category == CodexCategory.ActiveItem);
        if (HighlightArtifact) HighlightArtifact.SetActive(category == CodexCategory.Artifact);
        if (HighlightBoss) HighlightBoss.SetActive(category == CodexCategory.Boss);

        // 2. 리스트 갱신
        RefreshList();

        // 3. 탭 바꿀 때 상세창 닫기
        CloseDetailView();
    }

    private void RefreshList()
    {
        // 기존 슬롯 제거
        foreach (var slot in _spawnedSlots) Destroy(slot);
        _spawnedSlots.Clear();

        if (Database == null)
        {
            Debug.LogWarning("[CodexPanelUI] Database가 인스펙터에 할당되지 않았습니다!");
            return;
        }

        var entries = Database.GetEntriesByCategory(_currentCategory);

        if (entries == null)
        {
            Debug.Log($"[CodexPanelUI] {_currentCategory} 카테고리의 항목이 없습니다.");
            return;
        }

        var gameManager = AvoidCursorGameManager.Instance;

        foreach (var entry in entries)
        {
            GameObject go = Instantiate(SlotPrefab, SlotContainer);
            CodexSlotUI slotUI = go.GetComponent<CodexSlotUI>();

            // 해금 확인
            bool isUnlocked = (entry.UnlockID == UnlockID.None) || gameManager.IsUnlocked(entry.UnlockID);

            slotUI.Setup(entry, isUnlocked, OnSlotClicked);
            _spawnedSlots.Add(go);
        }
    }

    private void OnSlotClicked(CodexEntry entry)
    {
        _currentSelectedEntry = entry;
        OpenDetailView(entry);
    }

    // --- 상세 뷰 (Detail View) ---

    private void OpenDetailView(CodexEntry entry)
    {
        bool isUnlocked = (entry.UnlockID == UnlockID.None) || AvoidCursorGameManager.Instance.IsUnlocked(entry.UnlockID);

        DetailPanelObject.gameObject.SetActive(true);

        // 1. 정보 채우기
        if (isUnlocked)
        {
            DetailIcon.sprite = entry.Icon;
            DetailIcon.color = Color.white;
            DetailName.text = entry.Name;
            DetailDesc.text = entry.Description;
        }
        else
        {
            DetailIcon.sprite = entry.Icon;
            DetailIcon.color = Color.black; // 실루엣
            DetailName.text = "???";
            DetailDesc.text = "아직 발견되지 않았습니다.";
        }

        // 2. 카테고리별 버튼 처리
        if (_currentCategory == CodexCategory.ActiveItem && isUnlocked)
        {
            EquipButtonObject.SetActive(true);
            UpdateEquipButtonState();
        }
        else
        {
            EquipButtonObject.SetActive(false); // 유물, 보스, 미해금 아이템은 장착 불가
        }
    }

    private void CloseDetailView()
    {
        DetailPanelObject.SetActive(false);
    }

    // --- 장착 로직 ---

    private void UpdateEquipButtonState()
    {
        // 현재 장착된 아이템과 선택된 아이템의 ID 비교
        UnlockID equippedID = AvoidCursorGameManager.Instance.CurrentEquippedItemID;
        bool isEquipped = (equippedID == _currentSelectedEntry.UnlockID);

        if (isEquipped)
        {
            EquipButtonText.text = "장착중";
            EquipButtonObject.GetComponent<Image>().color = Color.gray;
        }
        else
        {
            EquipButtonText.text = "장착";
            EquipButtonObject.GetComponent<Image>().color = Color.white;
        }
    }

    private void OnEquipClicked()
    {
        Debug.Log($"[Codex] 아이템 장착 시도: {_currentSelectedEntry.Name}");

        AvoidCursorGameManager.Instance.EquipActiveItem(_currentSelectedEntry.UnlockID);

        UpdateEquipButtonState();
    }
}