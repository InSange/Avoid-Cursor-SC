using UnityEngine;

/// <summary>
/// 자식 오브젝트(벽)에 붙여서, 피격을 부모(보스)에게 전달합니다.
/// </summary>
public class RedirectDamage : MonoBehaviour, IHittable
{
    private IHittable _parentBoss;

    public void Initialize(IHittable parent)
    {
        _parentBoss = parent;
    }

    public void OnHit(int damage)
    {
        _parentBoss?.OnHit(damage);
    }
}