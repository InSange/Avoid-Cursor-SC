using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CodexSlotUI : BaseCursorButton
{
    [Header("UI 참조")]
    public Image IconImage;
    public GameObject LockIcon; // 잠김 상태일 때 켜질 아이콘
    public GameObject SelectionFrame; // 선택되었을 때 켜질 테두리

    private CodexEntry _entry;
    private Action<CodexEntry> _onClickCallback;
    private bool _isUnlocked;

    public void Setup(CodexEntry entry, bool isUnlocked, Action<CodexEntry> onClick)
    {
        _entry = entry;
        _isUnlocked = isUnlocked;
        _onClickCallback = onClick;

        // 아이콘 설정
        if (entry.Icon != null) IconImage.sprite = entry.Icon;

        // 잠김/해제 상태 시각화
        if (isUnlocked)
        {
            IconImage.color = Color.white;
            if (LockIcon) LockIcon.SetActive(false);
        }
        else
        {
            IconImage.color = Color.black; // 실루엣 처리 (또는 회색)
            if (LockIcon) LockIcon.SetActive(true);
        }

        // 초기화 시 선택 해제
        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (SelectionFrame) SelectionFrame.SetActive(isSelected);
    }

    public override void OnCursorClick(PlayerLogicBase cursor) // 혹은 PlayerLogicBase
    {
        base.OnCursorClick(cursor);

        // 잠겨있어도 정보는 볼 수 있게 할지, 아예 못 보게 할지 결정
        // 여기서는 잠겨있으면 클릭은 되지만 "???"로 뜨게 처리하기 위해 콜백 호출
        _onClickCallback?.Invoke(_entry);
    }
}