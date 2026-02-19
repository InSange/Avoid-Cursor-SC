using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class OrbitShield : MonoBehaviour
{
    private OrbitShieldController _controller;

    public void Initialize(OrbitShieldController controller)
    {
        _controller = controller;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 투사체와 충돌 시
        if (other.CompareTag("Projectile"))
        {
            // 1. 투사체 파괴
            if (other.gameObject != null)
            {
                // (풀링 오브젝트라면 Release 로직 필요, 일단 Destroy)
                Destroy(other.gameObject);
            }

            // 2. 컨트롤러에게 보고 (나를 비활성화하고 리스폰 시켜줘)
            if (_controller != null)
            {
                _controller.OnShieldHit(this);
            }
        }
    }
}