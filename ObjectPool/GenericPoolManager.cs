using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// T 컴포넌트를 갖는 GameObject 를 풀링하는 제네릭 풀.
/// Get() → Activate, Release() → Deactivate + 큐 보관,
/// ReleaseAllActive() → 활성 리스트에 남은 객체 전부 Release.
/// </summary>
public class ObjectPool<T> where T : Component
{
    private readonly T _prefab;
    private readonly Queue<T> _poolQueue = new Queue<T>();
    private readonly List<T> _activeList = new List<T>();

    public ObjectPool(T prefab, int initialSize = 0)
    {
        _prefab = prefab;
        for (int i = 0; i < initialSize; i++)
        {
            var inst = GameObject.Instantiate(_prefab);
            inst.gameObject.SetActive(false);
            _poolQueue.Enqueue(inst);
        }
    }

    /// <summary>풀에서 인스턴스를 꺼내 활성화</summary>
    public T Get()
    {
        T inst = _poolQueue.Count > 0
            ? _poolQueue.Dequeue()
            : GameObject.Instantiate(_prefab);
        inst.gameObject.SetActive(true);
        _activeList.Add(inst);
        return inst;
    }

    /// <summary>인스턴스를 비활성화하고 다시 큐에 반환</summary>
    public void Release(T inst)
    {
        if (_activeList.Remove(inst))
        {
            inst.gameObject.SetActive(false);
            _poolQueue.Enqueue(inst);
        }
    }

    /// <summary>모든 활성 인스턴스를 한꺼번에 반환</summary>
    public void ReleaseAllActive()
    {
        // ToArray 로 복사해 안전히 순회
        foreach (var inst in _activeList.ToArray())
        {
            // 💥 1. (핵심 추가) 인스턴스가 유효한지 확인합니다.
            // (Unity 오브젝트는 파괴되면 null을 반환합니다.)
            if (inst == null)
            {
                // 만약 이미 파괴되었다면, 리스트에서 제거만 하고 다음으로 넘어갑니다.
                _activeList.Remove(inst);
                continue;
            }

            // 2. 인스턴스가 유효하면, 풀에 반환하는 절차를 진행합니다.
            Release(inst);
        }
    }
}


/*public class GenericPoolManager : MonoBehaviour
{
    public static GenericPoolManager Instance { get; private set; }

    // 기존 풀
    private Dictionary<GameObject, Queue<GameObject>> _pools = new();

    // ● 추가: prefab 별로 활성화된 인스턴스 추적
    private Dictionary<GameObject, List<GameObject>> _activeMap = new();

    // ★ 추가: instance → prefab 맵
    private Dictionary<GameObject, GameObject> _instanceToPrefab = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public GameObject Get(GameObject prefab)
    {
        if (!_pools.TryGetValue(prefab, out var queue))
            _pools[prefab] = queue = new Queue<GameObject>();

        GameObject go = queue.Count > 0 ? queue.Dequeue() : Instantiate(prefab);
        go.SetActive(true);

        // ★ 맵핑 등록
        _instanceToPrefab[go] = prefab;

        // ★ 활성맵 등록
        if (!_activeMap.TryGetValue(prefab, out var list))
            _activeMap[prefab] = list = new List<GameObject>();
        list.Add(go);

        if (go.TryGetComponent<IPoolable>(out var poolable))
            poolable.OnSpawn();

        return go;
    }

    // ★ prefab 파라미터 제거한 Overload
    public void Return(GameObject instance)
    {
        if (!_instanceToPrefab.TryGetValue(instance, out var prefab))
        {
            Debug.Log("풀매니저에서 파괴");
            // 매핑이 없다면 그냥 파괴
            Destroy(instance);
            return;
        }
        Return(instance, prefab);
    }

    public void Return(GameObject instance, GameObject prefab)
    {
        if (instance.TryGetComponent<IPoolable>(out var poolable))
            poolable.OnDespawn();

        instance.SetActive(false);
        Debug.Log($"{instance}는 프리팹에서 탈락입니다.");
        // 활성맵에서 제거
        _activeMap[prefab].Remove(instance);
        _instanceToPrefab.Remove(instance);

        _pools[prefab].Enqueue(instance);
    }

    /// <summary>
    /// 특정 prefab으로부터 꺼낸 모든 활성 인스턴스를 한꺼번에 Return 합니다.
    /// </summary>
    public void ReturnAll(GameObject prefab)
    {
        if (!_activeMap.TryGetValue(prefab, out var list)) return;
        // ToArray로 복사해 안전하게 순회
        foreach (var inst in list.ToArray())
            Return(inst, prefab);
    }

    /// <summary>
    /// 풀 매니저가 관리하는 모든 prefab의 활성 인스턴스를 전부 Return 합니다.
    /// </summary>
    public void ReturnAll()
    {
        // 키(프리팹) 목록 복사
        foreach (var prefab in new List<GameObject>(_activeMap.Keys))
            ReturnAll(prefab);
    }
}
*/