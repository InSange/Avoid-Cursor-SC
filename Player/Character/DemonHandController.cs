using System.Collections;
using UnityEngine;

public class DemonHandController : MonoBehaviour
{
    [Header("Settings")]
    public float PunchDistance = 1.5f; // 펀치 사거리
    public float PunchSpeed = 15f;     // 펀치 속도
    public int Damage = 1;
    public float HitRadius = 0.1f;

    private Vector3 _originLocalPos;   // 원래 손 위치 (어깨)
    private SpriteRenderer _renderer;
    private bool _isAttacking = false;

    // 외부에서(본체) 상태 확인용 프로퍼티
    public bool IsAttacking => _isAttacking;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _originLocalPos = transform.localPosition; // 초기 배치된 위치를 기준점으로 삼음
    }

    /// <summary>
    /// 본체에서 호출: 정렬 순서 설정
    /// </summary>
    public void SetSortingOrder(int order)
    {
        if (_renderer != null) _renderer.sortingOrder = order;
    }

    /// <summary>
    /// 펀치 실행 (World Position 기준 방향)
    /// </summary>
    public void Punch(Vector2 targetDir)
    {
        if (_isAttacking) return;
        StartCoroutine(PunchRoutine(targetDir));
    }

    private IEnumerator PunchRoutine(Vector2 direction)
    {
        _isAttacking = true;

        // 목표 지점 계산 (내 현재 위치 + 방향 * 거리)
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + (Vector3)(direction.normalized * PunchDistance);

        // 뻗기 (Jab Out)
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * PunchSpeed;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        // 데미지 판정 (펀치 끝에서)
        CheckHit();

        // 돌아오기 (Return) - 로컬 좌표 0(원래 위치)로 복귀
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * PunchSpeed;
            // transform.localPosition을 원래 위치(_originLocalPos)로 복귀
            transform.localPosition = Vector3.Lerp(transform.localPosition, _originLocalPos, t);
            yield return null;
        }

        transform.localPosition = _originLocalPos; // 오차 보정
        _isAttacking = false;
    }

    private void CheckHit()
    {
        // 펀치 끝지점에서 작은 원으로 충돌 검사
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.5f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player")) continue; // 내 몸통은 무시

            if (hit.TryGetComponent<IHittable>(out var target))
            {
                target.OnHit(Damage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 1. 현재 위치에서 최대 사거리 표시 (녹색 선)
        Gizmos.color = Color.green;
        // 실제 공격은 World 방향으로 나가지만, 기즈모는 대략적인 범위만 보여줌
        Gizmos.DrawWireSphere(transform.position, PunchDistance);

        // 2. 타격 판정 크기 표시 (노란 원)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, HitRadius);
    }
}