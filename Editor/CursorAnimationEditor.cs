using UnityEngine;
using UnityEditor;

// CursorAnimation 에셋을 위한 커스텀 에디터를 정의합니다.
[CustomEditor(typeof(CursorAnimation))]
public class CursorAnimationEditor : Editor
{
    // 미리보기를 위한 내부 변수들
    private SpriteRenderer _previewRenderer;
    private double _startTime;
    private int _currentFrameIndex;
    private bool _isPlaying;

    private float _previewRotation = 0f;

    #region 설정 및 해제

    // 에디터가 활성화될 때 호출
    void OnEnable()
    {
        // 미리보기용 스프라이트 렌더러가 없으면 생성
        if (_previewRenderer == null)
        {
            // 임시 게임오브젝트를 만들고 숨깁니다. 씬에 보이지 않습니다.
            var go = new GameObject("Animation Preview") { hideFlags = HideFlags.DontSave };
            _previewRenderer = go.AddComponent<SpriteRenderer>();
        }
        // 에디터 업데이트 이벤트에 우리의 업데이트 함수를 등록
        EditorApplication.update += EditorUpdate;
    }

    // 에디터가 비활성화될 때 호출
    void OnDisable()
    {
        // 미리보기용으로 만들었던 임시 오브젝트 파괴
        if (_previewRenderer != null)
        {
            DestroyImmediate(_previewRenderer.gameObject);
        }
        // 에디터 업데이트 이벤트 구독 해제
        EditorApplication.update -= EditorUpdate;
    }

    #endregion

    #region 인스펙터 GUI

    // 인스펙터 창에 기본 UI 외에 추가적인 UI를 그립니다.
    public override void OnInspectorGUI()
    {
        // 기본 인스펙터 필드들을 먼저 그립니다 (Frames, Loop 등)
        base.OnInspectorGUI();

        GUILayout.Space(10); // 여백

        // 재생/정지 버튼을 가로로 나란히 배치
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Play"))
        {
            Play();
        }
        if (GUILayout.Button("Stop"))
        {
            Stop();
        }
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region 미리보기 GUI 및 로직

    // 이 에디터가 미리보기를 지원하는지 여부를 반환
    public override bool HasPreviewGUI()
    {
        return true;
    }

    // 미리보기 창의 제목을 반환
    public override GUIContent GetPreviewTitle()
    {
        return new GUIContent("Animation Preview");
    }

    // 인스펙터 하단 미리보기 영역을 그리는 함수
    public override void OnPreviewGUI(Rect r, GUIStyle background)
    {
        if (_previewRenderer == null || _previewRenderer.sprite == null)
            return;

        // 스프라이트를 주어진 영역(r)에 맞춰 그립니다.
        Texture2D tex = _previewRenderer.sprite.texture;
        Rect texCoords = _previewRenderer.sprite.textureRect;
        texCoords.x /= tex.width;
        texCoords.y /= tex.height;
        texCoords.width /= tex.width;
        texCoords.height /= tex.height;

        // 비율에 맞게 그리기 위해 계산
        float spriteRatio = _previewRenderer.sprite.bounds.size.x / _previewRenderer.sprite.bounds.size.y;
        float previewRatio = r.width / r.height;

        Rect viewRect = r;
        if (previewRatio > spriteRatio)
        {
            float newWidth = r.height * spriteRatio;
            float xOffset = (r.width - newWidth) / 2;
            viewRect = new Rect(r.x + xOffset, r.y, newWidth, r.height);
        }
        else
        {
            float newHeight = r.width / spriteRatio;
            float yOffset = (r.height - newHeight) / 2;
            viewRect = new Rect(r.x, r.y + yOffset, r.width, newHeight);
        }

        Matrix4x4 oldMatrix = GUI.matrix;
        Vector2 pivot = viewRect.center; // 이미지의 중앙을 축으로 회전
        GUIUtility.RotateAroundPivot(_previewRotation, pivot);

        GUI.DrawTextureWithTexCoords(viewRect, tex, texCoords);

        GUI.matrix = oldMatrix;
    }

    // 에디터 모드에서 계속 호출되며 애니메이션을 업데이트
    private void EditorUpdate()
    {
        if (!_isPlaying) return;

        // target은 현재 인스펙터에서 선택된 CursorAnimation 에셋입니다.
        var anim = (CursorAnimation)target;
        if (anim == null || anim.Frames.Length == 0) return;

        double elapsedTime = EditorApplication.timeSinceStartup - _startTime;

        // 현재 시간에 맞는 프레임을 찾습니다.
        double frameTime = 0;
        int frameIndex = 0;
        bool frameFound = false;

        for (int i = 0; i < anim.Frames.Length; i++)
        {
            double frameEndTime = frameTime + anim.Frames[i].Duration;

            if (elapsedTime < frameEndTime)
            {
                frameIndex = i;

                float timeInCurrentFrame = (float)(elapsedTime - frameTime);
                var currentFrame = anim.Frames[i];

                float prevRotation = (i == 0) ? 0f : anim.Frames[i - 1].TargetRotation;

                if (currentFrame.RotationDuration > 0f)
                {
                    float t = Mathf.Clamp01(timeInCurrentFrame / currentFrame.RotationDuration);
                    _previewRotation = Mathf.LerpAngle(prevRotation, currentFrame.TargetRotation, t);
                }
                else
                {
                    _previewRotation = currentFrame.TargetRotation;
                }

                frameFound = true;
                break;
            }
            frameTime += anim.Frames[i].Duration;
        }

        if (!frameFound)
        {
            if (anim.Loop)
            {
                _startTime = EditorApplication.timeSinceStartup;
                // 루프 시 첫 프레임부터 다시 시작하므로 회전도 초기화
                _previewRotation = 0f;
            }
            else
            {
                Stop();
                return;
            }
        }

        _currentFrameIndex = frameIndex;
        // Index Out of Range 방지
        if (_currentFrameIndex < anim.Frames.Length)
        {
            _previewRenderer.sprite = anim.Frames[_currentFrameIndex].FrameSprite;
        }

        // 미리보기 창을 다시 그리도록 요청
        Repaint();
    }

    #endregion

    #region 재생 제어

    private void Play()
    {
        _isPlaying = true;
        _startTime = EditorApplication.timeSinceStartup;
        _currentFrameIndex = 0;
        _previewRotation = 0f;
    }

    private void Stop()
    {
        _isPlaying = false;
        // 정지 시 첫 프레임으로
        var anim = (CursorAnimation)target;
        if (anim != null && anim.Frames.Length > 0 && _previewRenderer != null)
        {
            _previewRenderer.sprite = anim.Frames[0].FrameSprite;
        }
        _previewRotation = 0f;
        Repaint();
    }

    #endregion
}