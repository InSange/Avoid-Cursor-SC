using UnityEngine;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(CursorObject))]
public class CursorStateController : MonoBehaviour
{
    [SerializeField] public CursorState CurrentState { get; private set; }

    [System.Serializable]
    public struct AnimationEntry
    {
        public CursorState State;
        public CursorAnimation Animation;
    }

    public AnimationEntry[] Animations;

    [SerializeField] private Dictionary<CursorState, CursorAnimation> _animationMap;
    [SerializeField] private CursorObject _cursor;

    public Action<CursorState> OnStateAnimationComplete;


    private void Awake()
    {
        _cursor = GetComponent<CursorObject>();

        _animationMap = new Dictionary<CursorState, CursorAnimation>();
        foreach (var entry in Animations)
        {
            if (!_animationMap.ContainsKey(entry.State))
                _animationMap.Add(entry.State, entry.Animation);
        }

        _cursor.OnAnimationComplete += () =>
        {
            OnStateAnimationComplete?.Invoke(CurrentState);
        };
    }

    public void ChangeState(CursorState newState)
    {
        if (CurrentState == newState) return;
        if (!_animationMap.ContainsKey(newState)) return;
        var anim = _animationMap[newState];

        bool forcePlay = (newState == CursorState.Die);

        if (forcePlay || !_cursor.IsPlaying || (_cursor.Animation != null && _cursor.Animation.Interruptible))
        {
         Debug.Log($"상태 변경!! {CurrentState} => {newState}");
            CurrentState = newState;
            _cursor.PlayAnimation(anim);
        }
    }

    public bool HasAnimation(CursorState state)
    {
        return _animationMap.ContainsKey(state);
    }
}