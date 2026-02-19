using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
[DefaultExecutionOrder(-999)] // GameManager보다 먼저 실행되도록 설정
public class DeveloperFeatureDebug : MonoBehaviour
{
    [Header("테스트용 해금 기능")]
    public List<UnlockID> FeaturesToUnlock = new List<UnlockID>();

    private void Start()
    {
        StartCoroutine(ApplyUnlocksAfterManagerInit());
    }

    private IEnumerator ApplyUnlocksAfterManagerInit()
    {
        // GameManager가 생성될 때까지 대기
        while (AvoidCursorGameManager.Instance == null)
            yield return null;

        foreach (var feature in FeaturesToUnlock)
        {
            if (feature == UnlockID.None) continue;

            if (!AvoidCursorGameManager.Instance.IsUnlocked(feature))
            {
                AvoidCursorGameManager.Instance.ForceUnlock(feature);
            }
        }

        Debug.Log($"[DeveloperDebug] {FeaturesToUnlock.Count}개의 항목을 강제 해금했습니다.");
    }
}
#endif
