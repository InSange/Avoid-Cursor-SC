using UnityEngine;

public class UICursorFollower : MonoBehaviour
{
    public RectTransform CursorImage;

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;
    }

    void Update()
    {
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            CursorImage.parent as RectTransform,
            Input.mousePosition,
            null,
            out pos
        );
        CursorImage.anchoredPosition = pos;
    }
}
