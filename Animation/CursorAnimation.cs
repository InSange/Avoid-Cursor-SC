using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CursorAnimationFrame
{
    public Sprite FrameSprite;
    public float Duration; // 초 단위
    public string EventName; // 프레임 이벤트 명칭 (선택사항)

    [Header("Transform Scale")]
    public Vector3 TargetScale = Vector3.one;
    public float ScaleDuration = 0f;

    [Header("Transform Rotation")]
    public float TargetRotation = 0f; 
    public float RotationDuration = 0f; 
}

[CreateAssetMenu(fileName = "NewCursorAnimation", menuName = "Cursor/Animation")]
public class CursorAnimation : ScriptableObject
{
    public CursorAnimationFrame[] Frames;
    public bool Loop = true;
    public bool Interruptible = true;
    public bool UseTransformScaling = false; // 크기 연출 활성화 여부
    public bool UseAlphaFade = false;
    public float AlphaFadeDuration = 1f;
}