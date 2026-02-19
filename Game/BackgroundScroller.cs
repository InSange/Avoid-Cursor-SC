using UnityEngine;
using UnityEngine.UI;

public class BackgroundScroller : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("배경이 흐르는 속도 (양수면 위->아래)")]
    public float ScrollSpeed = 0.1f;

    [Tooltip("체크하면 화면 해상도가 변해도 원본 이미지 비율(1:1)을 유지합니다.")]
    public bool MaintainPixelSize = true;

    private RawImage _rawImage;
    private float _currentOffsetY = 0f;

    void Start()
    {
        // UI에서는 Renderer가 아니라 RawImage를 가져옵니다.
        _rawImage = GetComponent<RawImage>();

        if (MaintainPixelSize && _rawImage.texture != null)
        {
            UpdateTiling();
        }
    }

    void Update()
    {
        if (_rawImage == null) return;

        // 1. 시간 흐름에 따라 오프셋 증가
        _currentOffsetY += Time.deltaTime * ScrollSpeed;

        // 2. RawImage의 UV Rect 수정
        // uvRect는 (x, y, width, height) 구조입니다.
        // width, height는 유지하고 y값(세로 위치)만 변경합니다.
        Rect currentUV = _rawImage.uvRect;
        currentUV.y = _currentOffsetY;

        // 다시 할당해야 적용됨
        _rawImage.uvRect = currentUV;
    }

    /// <summary>
    /// 화면 크기와 텍스처 크기를 비교하여 UV Rect의 가로(Width), 세로(Height)를 설정합니다.
    /// </summary>
    private void UpdateTiling()
    {
        if (_rawImage.texture == null) return;

        float textureW = _rawImage.texture.width;
        float textureH = _rawImage.texture.height;

        // 현재 RawImage(화면)의 크기
        float screenW = GetComponent<RectTransform>().rect.width;
        float screenH = GetComponent<RectTransform>().rect.height;

        // 화면이 텍스처보다 몇 배 큰지 계산 (예: 화면이 3840이면 1920 텍스처가 2번 반복됨)
        float tileX = screenW / textureW;
        float tileY = screenH / textureH;

        Rect uv = _rawImage.uvRect;
        uv.width = tileX;
        uv.height = tileY;
        _rawImage.uvRect = uv;
    }
}