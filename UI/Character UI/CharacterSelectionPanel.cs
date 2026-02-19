using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class CharacterSelectionPanel : MonoBehaviour
{
    [Header("데이터")]
    public List<CharacterData> AllCharacters; // (GameManager에서 로드하거나 인스펙터 할당)

    [Header("UI 참조")]
    public TextMeshProUGUI CharacterNameText;
    public TextMeshProUGUI DescriptionText;
    public Image CharacterIconImage;
    public GameObject CardPrefab; // (CharacterCardUI가 붙어있는 프리팹)
    public Transform CardContainer; // (카드들이 생성될 부모)

    [Header("버튼")]
    public BaseCursorButton LeftArrowButton;
    public BaseCursorButton RightArrowButton;
    public BaseCursorButton EquipButton;

    // --- 캐러셀 상태 ---
    private List<CharacterCardUI> _cardInstances = new List<CharacterCardUI>();
    private int _currentCenterIndex = 0; // 현재 중앙(n)에 있는 캐릭터의 인덱스

    // --- 💥 캐러셀 설정 (5개: n-2 ~ n+2) ---
    // Y축은 모두 0으로 통일
    private readonly float[] _xPositions = { -500f, -250f, 0f, 250f, 500f };
    private readonly float[] _scales = { 0.6f, 0.8f, 1.0f, 0.8f, 0.6f };
    private readonly float[] _alphas = { 0.2f, 0.6f, 1.0f, 0.6f, 0.2f }; // 양 끝은 흐리게
    private readonly int[] _orders = { 1, 5, 10, 5, 1 };


    void Start()
    {
        for (int i = 0; i < AllCharacters.Count; i++)
        {
            GameObject cardGO = Instantiate(CardPrefab, CardContainer);
            // 초기 위치를 중앙에 모아두고 시작 (UpdateVisuals로 펼쳐짐)
            ((RectTransform)cardGO.transform).anchoredPosition = Vector2.zero;

            CharacterCardUI card = cardGO.GetComponent<CharacterCardUI>();
            card.SetData(AllCharacters[i]);

            int index = i;
            card.OnClick.AddListener(() => GoToCharacter(index));

            _cardInstances.Add(card);
        }

        // 3. 버튼 이벤트 구독
        LeftArrowButton.OnClick.AddListener(MoveLeft);
        RightArrowButton.OnClick.AddListener(MoveRight);
        EquipButton.OnClick.AddListener(EquipCharacter);

        // 4. 초기 UI 상태 설정 (애니메이션 없이 즉시)
        GoToCharacter(0);
    }

    /// <summary>
    /// 왼쪽 화살표 클릭
    /// </summary>
    public void MoveLeft()
    {
        // (Count를 더해서 음수가 되는 것을 방지하는 순환 인덱싱)
        int newIndex = (_currentCenterIndex - 1 + AllCharacters.Count) % AllCharacters.Count;
        GoToCharacter(newIndex);
    }

    /// <summary>
    /// 오른쪽 화살표 클릭
    /// </summary>
    public void MoveRight()
    {
        int newIndex = (_currentCenterIndex + 1) % AllCharacters.Count;
        GoToCharacter(newIndex);
    }

    /// <summary>
    /// (핵심) 지정된 인덱스를 중앙(n)으로 설정하고 UI를 갱신합니다.
    /// </summary>
    private void GoToCharacter(int newCenterIndex)
    {
        _currentCenterIndex = newCenterIndex;

        // 1. 상단 정보 갱신
        CharacterCardUI centerCard = _cardInstances[_currentCenterIndex];
        CharacterData data = centerCard.GetData();

        if (CharacterIconImage != null)
        {
            CharacterIconImage.sprite = data.PanelIcon; // 데이터에 있는 아이콘 스프라이트 적용
        }

        if (centerCard.IsUnlocked)
        {
            CharacterNameText.text = data.CharacterName;
            DescriptionText.text = data.Description;
            EquipButton.gameObject.SetActive(true); // 해금 시 장착 가능

            if (CharacterIconImage != null)
            {
                CharacterIconImage.color = Color.white;
            }
        }
        else
        {
            CharacterNameText.text = "???";
            DescriptionText.text = "해금 조건: " + GetLockConditionText(data.RequiredUnlockID);
            EquipButton.gameObject.SetActive(false); // 잠김 시 장착 불가

            if (CharacterIconImage != null)
            {
                CharacterIconImage.color = Color.black; // 완전 검정 실루엣
                // 또는 반투명하게 하려면: new Color(0, 0, 0, 0.5f); 
            }
        }

        for (int i = 0; i < _cardInstances.Count; i++)
        {
            UpdateCardVisual(i);
        }
    }

    private void UpdateCardVisual(int cardIndex)
    {
        CharacterCardUI card = _cardInstances[cardIndex];
        int listCount = _cardInstances.Count;

        // 중앙으로부터의 거리 (Offset) 계산 (순환 고려)
        int offset = cardIndex - _currentCenterIndex;

        // - (Half) ~ + (Half) 범위로 보정
        // 예: 총 10개일 때, offset이 -9면 실제로는 +1 (바로 오른쪽)
        if (offset > listCount / 2) offset -= listCount;
        if (offset < -listCount / 2) offset += listCount;

        // 💥 시각적 인덱스 (0~4) 매핑
        // offset: -2(왼쪽끝) ~ 0(중앙) ~ +2(오른쪽끝)
        // visualIndex: 0 ~ 2 ~ 4
        int visualIndex = offset + 2;

        if (visualIndex >= 0 && visualIndex < 5)
        {
            // 범위 안 (보임)
            Vector2 targetPos = new Vector2(_xPositions[visualIndex], 0); // Y=0 고정
            float scale = _scales[visualIndex];
            int order = _orders[visualIndex];
            float alpha = _alphas[visualIndex];

            card.UpdateVisuals(targetPos, scale, order, alpha);
        }
        else
        {
            // 💥 범위 밖 (숨김 처리)
            // 양 끝보다 더 멀리 보내고 투명하게 만듦
            float hiddenX = (offset > 0) ? 800f : -800f;
            Vector2 targetPos = new Vector2(hiddenX, 0);

            // Scale 0, Alpha 0으로 완전히 숨김
            card.UpdateVisuals(targetPos, 0.1f, 0, 0f);
        }
    }

    private string GetLockConditionText(UnlockID id)
    {
        // (UnlockID에 따른 힌트 텍스트 반환 로직 필요)
        return "특정 조건을 달성하세요.";
    }

    public void EquipCharacter()
    {
        CharacterData selected = AllCharacters[_currentCenterIndex];

        // GameManager에게 프리팹 교체 요청
        AvoidCursorGameManager.Instance.SetPlayerPrefab(selected.PlayerCursorPrefab);

        Debug.Log($"[CharacterSelect] {selected.CharacterName} 장착 완료!");
    }
}