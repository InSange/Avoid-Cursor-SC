using UnityEngine;

/// <summary>
/// 닷지 프로토타입용 총알.
/// 화면 밖으로 나가면 풀로 돌아가지 않고, 즉시 재진입하여
/// 플레이어를 다시 조준합니다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class DodgeBullet : MonoBehaviour, IPoolable
{
    public float Speed; // 💥 1. 속도는 외부에서(매니저가) 설정

    private Transform _playerTransform;
    private Vector2 _direction;
    private Camera _cam;

    [Tooltip("true이면 현재 화면 밖->안으로 진입 중임을 의미")]
    private bool _isWrapping;

    void Awake()
    {
        _cam = Camera.main;
        GetComponent<Collider2D>().isTrigger = true;
    }

    /// <summary>
    /// (InfiniteModeManager가 호출)
    /// 총알의 목표와 속도를 설정합니다.
    /// </summary>
    public void Initialize(Transform player, float speed)
    {
        _playerTransform = player;
        Speed = speed;

        RespawnFromEdge();
    }

    /// <summary>
    /// (풀에서 꺼낼 때 호출)
    /// </summary>
    public void OnSpawn()
    {
/*        // 💥 2. 화면 밖에서 시작하도록 '래핑' 상태로 스폰
        _isWrapping = true;
        RespawnFromEdge();*/
    }

    /// <summary>
    /// (풀로 반환될 때 호출)
    /// </summary>
    public void OnDespawn()
    {
        _playerTransform = null; // 참조 리셋
    }

    void Update()
    {
        // 플레이어가 없거나 (게임 오버 등) 스폰 전이면 중지
        if (_playerTransform == null) return;

        // 1. 설정된 방향으로 이동
        transform.position += (Vector3)_direction * Speed * Time.deltaTime;

        // 2. 뷰포트 좌표 계산 (0.0 ~ 1.0)
        Vector3 vp = _cam.WorldToViewportPoint(transform.position);

        // 3. 화면 밖으로 나갔는지 확인 (버퍼 0.1f 포함)
        bool isOffScreen = vp.x < -0.1f || vp.x > 1.1f || vp.y < -0.1f || vp.y > 1.1f;

        if (_isWrapping && !isOffScreen)
        {
            // '래핑' 상태였는데 화면 안으로 진입함
            _isWrapping = false;
        }
        else if (!_isWrapping && isOffScreen)
        {
            // '비-래핑' 상태였는데 화면 밖으로 나감
            _isWrapping = true;
            RespawnFromEdge(); // 💥 4. 즉시 재스폰 및 재조준
        }
    }

    /// <summary>
    /// (IOSystemStageEnvironmentController의 SpawnPopup 로직 재활용)
    /// 화면 사방 중 한 곳에서 리스폰하고 플레이어를 조준합니다.
    /// </summary>
    void RespawnFromEdge()
    {
        if (_playerTransform == null) return;

        // 0: 왼쪽, 1: 오른쪽, 2: 아래, 3: 위
        int side = Random.Range(0, 4);

        Vector3 bl = _cam.ViewportToWorldPoint(new Vector3(-0.1f, -0.1f, 0)); // 버퍼 포함
        Vector3 tr = _cam.ViewportToWorldPoint(new Vector3(1.1f, 1.1f, 0)); // 버퍼 포함

        Vector2 spawnPos;

        switch (side)
        {
            case 0: // 왼쪽
                spawnPos = new Vector2(bl.x, Random.Range(bl.y, tr.y));
                break;
            case 1: // 오른쪽
                spawnPos = new Vector2(tr.x, Random.Range(bl.y, tr.y));
                break;
            case 2: // 아래
                spawnPos = new Vector2(Random.Range(bl.x, tr.x), bl.y);
                break;
            default: // 위
                spawnPos = new Vector2(Random.Range(bl.x, tr.x), tr.y);
                break;
        }

        transform.position = spawnPos;

        // 5. 플레이어를 향해 방향과 각도 재설정
        _direction = (_playerTransform.position - transform.position).normalized;
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg + 180f;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    // 💥 6. 플레이어와 충돌 처리 (기존 Bullet.cs 로직 재사용)
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return; // (플레이어 태그 확인)

        if (other.TryGetComponent<IHittable>(out var h))
        {
            h.OnHit(1); // (데미지 1 고정)
        }

        // (피격 시에는 풀로 돌아가지 않고 즉시 재스폰)
        _isWrapping = true;
        RespawnFromEdge();
    }
}