using UnityEngine;

[RequireComponent(typeof(CursorObject))]
public class WaveAttack : MonoBehaviour
{
    public float DamageRadius = 0.8f;
    public int DamageAmount = 2;
    public LayerMask HitLayers;

    private void Start()
    {
        var cursor = GetComponent<CursorObject>();
        cursor.OnAnimationComplete += HandleAnimationComplete;
        cursor.PlayAnimation(cursor.Animation); // 또는 외부에서 주입

        DealDamage(); // 즉시 히트 처리
        Debug.Log($"스타트 호출 {DamageRadius} , {DamageAmount}");
    }

    public void Initialize(float radius, int damage, LayerMask layers)
    {
        DamageRadius = radius;
        DamageAmount = damage;
        HitLayers = layers;

        Debug.Log("이니셜라이즈 호출");
    }


    private void DealDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, DamageRadius, HitLayers);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Projectile"))
            {
                Destroy(hit.gameObject);
            }
            else
            {
                var hittable = hit.GetComponent<IHittable>();
                hittable?.OnHit(DamageAmount);
            }
        }
    }

    private void HandleAnimationComplete()
    {
        Destroy(gameObject);
    }
}