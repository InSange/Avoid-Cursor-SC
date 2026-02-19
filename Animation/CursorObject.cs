using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CursorObject : MonoBehaviour
{
    public CursorAnimation Animation;

    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private int _currentFrameIndex;
    [SerializeField] private float _timer;
    [SerializeField] private bool _isPlaying;

    public float PlaybackSpeed { get; set; } = 1.0f; // 애니메이션 재생 속도
    public float ManualRotationSpeed { get; set; } = 0f; // 수동 회전 제아

    // Scale 관련
    private Vector3 _initialScale;
    private Vector3 _targetScale;
    private float _scaleTimer;
    private float _scaleDuration;

    // Rotation 관련
    private float _initialZ; // 시작 각도 (Z축)
    private float _targetZ;  // 목표 각도 (Z축)
    private float _rotationTimer;
    private float _rotationDuration;

    private bool _isFading = false;
    private float _fadeTimer = 0f;

    public Action<string> OnAnimationEvent;
    public Action OnAnimationComplete;

    public bool IsPlaying => _isPlaying;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _initialScale = transform.localScale;
    }

    private void Start()
    {
        TryApplyColorEffect(); // 무조건 ColorEffectController는 붙는다
    }

    private void Update()
    {
        if (ManualRotationSpeed != 0f) // 수동 회전 제어 특정 스킬 등에서 강제로 돌려야 할 때 사용
        {
            transform.Rotate(0, 0, ManualRotationSpeed * Time.deltaTime);
        }

        if (!_isPlaying || Animation == null || Animation.Frames.Length == 0) return;

        float scaledDeltaTime = Time.deltaTime * PlaybackSpeed;

        _timer += scaledDeltaTime;
        _scaleTimer += scaledDeltaTime;
        _rotationTimer += scaledDeltaTime;

        // 스케일 연출
        if (Animation.UseTransformScaling && _scaleDuration > 0f)
        {
            float t = Mathf.Clamp01(_scaleTimer / _scaleDuration);
            transform.localScale = Vector3.Lerp(_initialScale, _targetScale, t);
        }

        // 현재 애니메이션에서 회전 상태 구현
        if (_rotationDuration > 0f)
        {
            float t = Mathf.Clamp01(_rotationTimer / _rotationDuration);
            float currentZ = Mathf.LerpAngle(_initialZ, _targetZ, t);

            float currentY = transform.localEulerAngles.y;
            transform.localRotation = Quaternion.Euler(0, currentY, currentZ);
        }

        if (!_isFading && _timer >= Animation.Frames[_currentFrameIndex].Duration)
        {
            _timer = 0f;
            _currentFrameIndex++;

            if (_currentFrameIndex >= Animation.Frames.Length)
            {
                if (Animation.Loop)
                    _currentFrameIndex = 0;
                else
                {
                    _currentFrameIndex = Animation.Frames.Length - 1;
                    _isPlaying = false;

                    if (Animation.UseAlphaFade)
                    {
                        _isFading = true;
                        _fadeTimer = 0f;
                    }

                    OnAnimationComplete?.Invoke();
                //    Debug.Log("애니메이션 완료");
                }
            }

            if (_isPlaying)
            {
                var frame = Animation.Frames[_currentFrameIndex];
                _spriteRenderer.sprite = frame.FrameSprite;

                if (!string.IsNullOrEmpty(frame.EventName))
                {   // 이벤트로 State를 처리하려면 애니메이션에 Interruptible이 활성화 되어 있어야함!
                    OnAnimationEvent?.Invoke(frame.EventName);
                   // Debug.Log("애니메이션 이벤트 완료");
                }
                SetupScaling(frame);
                SetupRotation(frame);
            }

            if (_isFading)
            {
                _fadeTimer += Time.deltaTime;
                float fadeT = Mathf.Clamp01(_fadeTimer / Animation.AlphaFadeDuration);

                Color color = _spriteRenderer.color;
                color.a = Mathf.Lerp(1f, 0f, fadeT);
                _spriteRenderer.color = color;
            }
        }
    }

    #region 애니메이션 관련
    public void PlayAnimation(CursorAnimation newAnimation)
    {
        if (!_isPlaying || (Animation != null && Animation.Interruptible))
        {
            Animation = newAnimation;
            _currentFrameIndex = 0;
            _timer = 0f;
            _fadeTimer = 0f;
            _isPlaying = true;
            _isFading = false;
            if (Animation != null && Animation.Frames.Length > 0)
            {
                var frame = Animation.Frames[0];
                _spriteRenderer.sprite = frame.FrameSprite ? frame.FrameSprite : _spriteRenderer.sprite;
                SetupScaling(frame);
                _spriteRenderer.color = new Color(1f, 1f, 1f, 1f);
            }
        }
    }

    private void SetupScaling(CursorAnimationFrame frame)
    {
        if (Animation.UseTransformScaling && frame.ScaleDuration > 0f)
        {
            _initialScale = transform.localScale;
            _targetScale = frame.TargetScale;
            _scaleDuration = frame.ScaleDuration;
            _scaleTimer = 0f;
        }
    }

    // Z축 각도(float)만 설정하도록 변경
    private void SetupRotation(CursorAnimationFrame frame)
    {
        float currentZ = transform.localEulerAngles.z;
        float targetZ = frame.TargetRotation;

        // Duration이 있는 경우 (부드럽게)
        if (frame.RotationDuration > 0f)
        {
            _initialZ = currentZ;
            _targetZ = targetZ;
            _rotationDuration = frame.RotationDuration;
            _rotationTimer = 0f;
        }
        // Duration이 0인 경우 (즉시)
        else
        {
            _rotationDuration = 0f;
            float currentY = transform.localEulerAngles.y;
            transform.localRotation = Quaternion.Euler(0, currentY, targetZ);

            // 초기값 갱신
            _initialZ = targetZ;
            _targetZ = targetZ;
        }
    }

    public void SetPaused(bool isPaused)
    {
        _isPlaying = !isPaused;
    }

    public void ResetRotation()
    {
        float currentY = transform.localEulerAngles.y;
        transform.localRotation = Quaternion.Euler(0, currentY, 0);

        ManualRotationSpeed = 0f;
    }
    #endregion
    #region 시스템 기능
    /// <summary>
    /// 컬러 기능
    /// </summary>
    private void TryApplyColorEffect()
    {
        if (!TryGetComponent<ColorEffectController>(out var effect))
        {
            var controller = gameObject.AddComponent<ColorEffectController>();
            controller.ApplyColorSetting(); // 바로 설정
        }
        else
        {
            effect.ApplyColorSetting(); // 이미 있으면 설정만 갱신
        }
    }

    private void OnDestroy()
    {

    }
    #endregion
}
