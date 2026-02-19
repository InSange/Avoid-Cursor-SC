using UnityEngine;

public class CursorWaveEffect : MonoBehaviour
{
    private CursorObject cursor;

    void Start()
    {
        cursor = GetComponent<CursorObject>();
        cursor.OnAnimationComplete += HandleComplete;
        cursor.PlayAnimation(cursor.Animation);
    }

    void HandleComplete()
    {
        Destroy(gameObject);
    }
}