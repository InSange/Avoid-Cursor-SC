using UnityEngine;

/// <summary>
/// 화면 상단에 배치되어, PlayerCursor가 닿으면
/// GalleryPanelController의 ShowPreview()를 호출하는 트리거입니다.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class GalleryPeekTrigger : MonoBehaviour
{
    [Tooltip("연결할 GalleryPanelController")]
    public GalleryPanelController PanelController;

    private void Awake()
    {
        // 1. 반드시 트리거여야 함
        GetComponent<BoxCollider2D>().isTrigger = true;

        // 2. 부모에서 컨트롤러 자동 찾기 (편의)
        if (PanelController == null)
            PanelController = GetComponentInParent<GalleryPanelController>();
    }

    /// <summary>
    /// PlayerCursor가 이 트리거에 닿았을 때
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"{gameObject.name} 트리거에 닿았습니다!");
            PanelController?.ShowPreview();
        }
    }

    // (참고: PlayerCursor가 떠날 때(OnTriggerExit2D)
    //  HidePanel()을 호출하면 원본과 더 유사하게 작동합니다)

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"{gameObject.name} 트리거에서 벗어났습니다!");

            // 1. 컨트롤러가 있는지 확인
            if (PanelController == null) return;

            // 2. 현재 상태가 'Expanded'(확장됨)가 아닌,
            //    'Previewing'(미리보기) 상태일 때만 숨깁니다!
            if (PanelController.CurrentState == PanelState.Previewing)
            {
                PanelController.HidePanel();
            }
        }
    }
}