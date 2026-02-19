using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class PopupSqaure : MonoBehaviour, IPoolable
{
    [Header("이동 속도")]
    public float Speed = 5f;

    [Header("충돌 설정")]
    public int Damage = 1;
    public string TargetTag = "Player";      // 태그로 판정
    public float Knockback = 5f;            // 넉백 세기
    public float Radius = 0.5f;          // BoxCollider2D 크기 (반사각형이니까 extents로 세팅)

    // 바깥에서 Spawn 시점에 Init() 으로 방향을 세팅
    private Vector2 _direction;
    private bool _hasEnteredView;

    void Awake()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    /// <summary>
    /// 외부에서 스폰할 때 호출해서 이동 방향을 세팅합니다.
    /// </summary>
    public void Init(Vector2 dir)
    {
        _direction = dir.normalized;
        _hasEnteredView = false;
    }

    private void Update()
    {
        // 1) 이동
        transform.position += (Vector3)_direction * Speed * Time.deltaTime;

        // 2) 뷰포트 좌표 확인
        Vector3 vp = Camera.main.WorldToViewportPoint(transform.position);

        if (!_hasEnteredView)
        {
            // 아직 화면 안으로 들어온 적 없으면, 화면 안으로 들어왔는지 체크
            if (vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f)
                _hasEnteredView = true;
        }
        else
        {
            // 한번 들어온 후에는 화면 밖으로 완전히 이탈하면 반환
            if (vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f)
            {
                PoolingControllerBase.Current
                           .GetPool<PopupSqaure>()
                           .Release(this);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 태그 체크
        if (!other.CompareTag(TargetTag)) return;

        // 1) 데미지 적용
        if (other.TryGetComponent<IHittable>(out var h))
            h.OnHit(Damage);
        Debug.Log($"{h} 한테 데미지 {Damage}적용");

        // 3) 풀로 반환
        PoolingControllerBase.Current.GetPool<PopupSqaure>()
                                     .Release(this);
    }

    // 풀에서 꺼낼 때 호출
    public void OnSpawn()
    {
        // 필요 시 추가 초기화 (e.g. animation reset)
    }

    // 풀에 반환될 때 호출
    public void OnDespawn()
    {
        // 필요 시 상태 초기화 (e.g. remove forces, reset color)
    }
}