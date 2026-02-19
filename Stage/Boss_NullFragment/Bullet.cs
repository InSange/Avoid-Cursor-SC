using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour, IPoolable
{
    public float Speed = 5f;
    public int Damage = 1;
    private Vector2 Direction;

    public enum BulletType { Targeting, Straight }
    public BulletType BulletMovementType;

    public string TargetTag = "Player"; // 기본값은 플레이어

    private bool IsDead => AvoidCursorGameManager.Instance.IsGameOver;

    public void Initialize(Vector2 direction, BulletType type, int damage = 1, string targetTag = "Player")
    {
        Direction = direction.normalized;
        BulletMovementType = type;
        Damage = damage;
        TargetTag = targetTag;

        float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg + 180f;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }


    private void Update()
    {
        transform.position += (Vector3)Direction * Speed * Time.deltaTime;

        if (!IsVisible() || IsDead)
        {
            // (안전 장치: Current가 null일 수도 있으므로)
            if (PoolingControllerBase.Current != null)
            {
                PoolingControllerBase.Current.GetPool<Bullet>().Release(this);
            }
            else
            {
                // 풀이 없으면 그냥 파괴
                Destroy(gameObject);
            }
        }
    }

    private bool IsVisible()
    {
        Vector3 screenPoint = Camera.main.WorldToViewportPoint(transform.position);
        return screenPoint.x >= -0.1f && screenPoint.x <= 1.1f &&
               screenPoint.y >= -0.1f && screenPoint.y <= 1.1f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(TargetTag)) return;

        IHittable target = other.GetComponent<IHittable>();
        if (target != null)
        {
            Debug.Log($"[{TargetTag}] 피격됨!");
            target.OnHit(Damage);
            if (PoolingControllerBase.Current != null)
            {
                PoolingControllerBase.Current.GetPool<Bullet>().Release(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
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