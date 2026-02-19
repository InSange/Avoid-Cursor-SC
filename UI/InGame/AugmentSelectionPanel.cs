using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AugmentSelectionPanel : MonoBehaviour
{
    [Header("데이터 소스")]
  //  public List<AugmentData> AllAugments; // (Inspector에서 할당)

    [Header("UI 참조")]
    public Transform ButtonContainer; // VerticalLayoutGroup이 있는 부모 객체
    public GameObject AugmentCardPrefab; // AugmentCardUI가 붙은 프리팹

    private Action<AugmentData> _callback;
    private List<GameObject> _spawnedCards = new List<GameObject>();

    private bool _isSelecting = false;

    /// <summary>
    /// 패널을 열고 3개의 랜덤 증강을 제시합니다.
    /// </summary>
    public void Show(List<AugmentData> validPool, int choiceCount, Action<AugmentData> onChosen)
    {
        gameObject.SetActive(true);
        _callback = onChosen;

        foreach (var card in _spawnedCards) Destroy(card);
        _spawnedCards.Clear();

        List<AugmentData> pool = new List<AugmentData>(validPool);

        for (int i = 0; i < pool.Count; i++)
        {
            var temp = pool[i];
            int r = UnityEngine.Random.Range(i, pool.Count);
            pool[i] = pool[r];
            pool[r] = temp;
        }

        int countToSpawn = Mathf.Min(choiceCount, pool.Count);
        for (int i = 0; i < countToSpawn; i++)
        {
            GameObject go = Instantiate(AugmentCardPrefab, ButtonContainer);
            AugmentCardUI cardUI = go.GetComponent<AugmentCardUI>();
            cardUI.Setup(pool[i], OnCardSelected);
            _spawnedCards.Add(go);
        }
    }

    private void OnCardSelected(AugmentData selectedData)
    {
        if (_isSelecting) return;
        _isSelecting = true;

        StartCoroutine(SelectionSequence(selectedData));
    }

    private IEnumerator SelectionSequence(AugmentData selectedData)
    {
        foreach (GameObject go in _spawnedCards)
        {
            AugmentCardUI card = go.GetComponent<AugmentCardUI>();

            bool isTarget = (card.GetData() == selectedData);

            if (isTarget)
            {
                card.PlaySelectedAnimation();
            }
            else
            {
                card.PlayDismissAnimation();
            }
        }

        // 3. 연출 시간 대기 (예: 1초)
        yield return new WaitForSeconds(1.0f);

        // 4. 패널 닫기 및 콜백 호출
        gameObject.SetActive(false);
        _callback?.Invoke(selectedData);
        _isSelecting = false;
    }
}