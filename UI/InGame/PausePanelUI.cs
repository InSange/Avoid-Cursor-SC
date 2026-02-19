using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PausePanelUI : MonoBehaviour
{
    [Header("UI 참조")]
    public TextMeshProUGUI BuffListText; // 버프/디버프 텍스트 (좌측/우측 등)
    public TextMeshProUGUI PageText;     // "1 / 3"

    private List<string> _allBuffs = new List<string>(); // (임시: 문자열로 관리)
    private int _currentPage = 0;
    private const int ITEMS_PER_PAGE = 10;

    public void Show(List<string> currentBuffs)
    {
        gameObject.SetActive(true);
        _allBuffs = currentBuffs;
        _currentPage = 0;
        UpdateUI();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void NextPage()
    {
        int maxPage = Mathf.Max(0, (_allBuffs.Count - 1) / ITEMS_PER_PAGE);
        if (_currentPage < maxPage)
        {
            _currentPage++;
            UpdateUI();
        }
    }

    public void PrevPage()
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (_allBuffs == null || _allBuffs.Count == 0)
        {
            BuffListText.text = "적용된 효과 없음";
            PageText.text = "1 / 1";
            return;
        }

        int start = _currentPage * ITEMS_PER_PAGE;
        int end = Mathf.Min(start + ITEMS_PER_PAGE, _allBuffs.Count);
        int maxPage = (_allBuffs.Count - 1) / ITEMS_PER_PAGE + 1;

        string content = "";
        for (int i = start; i < end; i++)
        {
            content += $"- {_allBuffs[i]}\n";
        }

        BuffListText.text = content;
        PageText.text = $"{_currentPage + 1} / {maxPage}";
    }
}