using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬의 모든 오브젝트 풀링을 관리하는 '싱글톤' 기반 클래스입니다.
/// InfiniteModeManager와 BossGalleryManager에서 사용
/// </summary>
public abstract class PoolingControllerBase : MonoBehaviour
{
    public static PoolingControllerBase Current { get; private set; }

    [Header("오브젝트 풀링")]
    private Dictionary<Type, object> _typePools = new Dictionary<Type, object>();

    protected virtual void Awake()
    {
        if (Current != null && Current != this)
        {
            Destroy(gameObject);
            return;
        }
        Current = this;
        AvoidCursorGameManager.Instance.IsGameOver = false;
        Debug.Log($"[PoolingControllerBase] {this.GetType().Name}가 Current로 등록됨.");
    }

    #region 오브젝트 풀링 API
    /// <summary>
    /// (보스 등이 호출) 
    /// 풀이 없으면 'prefab'을 기반으로 즉시 생성하고, 있으면 반환합니다.
    /// </summary>
    public ObjectPool<T> GetOrCreatePool<T>(T prefab) where T : Component
    {
        Type type = typeof(T);

        // 1. 이미 풀이 있으면 즉시 반환
        if (_typePools.TryGetValue(type, out var obj))
        {
            return obj as ObjectPool<T>;
        }

        // 2. 풀이 없으면, 'prefab'을 기반으로 새로 생성
        if (prefab == null)
        {
            Debug.LogError($"[PoolingControllerBase] {type}의 풀을 생성하려 했으나 prefab이 null입니다!");
            return null;
        }

        Debug.Log($"[PoolingControllerBase] JIT 풀 생성: {prefab.name}");
        var pool = new ObjectPool<T>(prefab, initialSize: 10); // (초기 10개 생성)
        _typePools[type] = pool; // 딕셔너리에 등록
        return pool;
    }

    /// <summary>
    /// (투사체 등이 호출) 
    /// 이미 'GetOrCreatePool'로 생성된 풀을 반환합니다.
    /// </summary>
    public ObjectPool<T> GetPool<T>() where T : Component
    {
        if (_typePools.TryGetValue(typeof(T), out var obj))
        {
            return obj as ObjectPool<T>;
        }

        // (보스가 GetOrCreatePool을 호출하기 전에 투사체가 반환을 시도한 경우)
        Debug.LogError($"[PoolingControllerBase] {typeof(T)}의 풀을 찾을 수 없습니다. GetOrCreatePool이 먼저 호출되어야 합니다.");
        return null;
    }

    /// <summary>
    /// 모든 풀의 활성 인스턴스를 한꺼번에 반환합니다.
    /// </summary>
    public void ClearAllPools()
    {
        Debug.Log("[PoolingControllerBase] 모든 풀링된 오브젝트를 회수합니다.");
        foreach (var poolObj in _typePools.Values)
        {
            var method = poolObj.GetType().GetMethod("ReleaseAllActive");
            method?.Invoke(poolObj, null);
        }
    }
    #endregion
}
