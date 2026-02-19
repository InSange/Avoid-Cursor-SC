using UnityEngine;

/// <summary>
/// 데몬 캐릭터 로직
/// - 기본: 오른쪽 바라봄
/// - 이동 방향에 따라 몸체 좌우 반전 (Y축 회전)
/// - 추후 손(무기) 오브젝트를 하위에서 별도로 제어 예정
/// </summary>
public class PlayerLogic_Demon : PlayerLogicBase
{
    [Header("Demon Hands")]
    public DemonHandController RightHand; // 오른손 (선공)
    public DemonHandController LeftHand;  // 왼손 (후공)

    [Header("Hand Settings")]
    public Vector3 RightHandOffset = new Vector3(-0.2f, -0.3f, 0); // 오른손 기본 위치
    public Vector3 LeftHandOffset = new Vector3(0.23f, -0.3f, 0);  // 왼손 기본 위치

    [Header("Combat")]
    public float AutoTargetRadius = 5f; // 자동 타겟팅 범위
    private int _comboIndex = 0; // 0: 오른손 준비, 1: 왼손 준비

    // 본체의 Sprite Order (Inspector에서 확인, 보통 0)
    private int _bodySortOrder = 0;

    [Header("Demon Stats")]
    public float DemonMoveSpeed = 5; // 기본 이동 속도

    // 이전 프레임의 위치를 저장하여 이동 방향 계산
    private Vector3 _lastPosition;
    private bool _isFacingRight = true;

    public override float SkillStaminaCost => 20f;

    protected override void Start()
    {
        base.Start();
        Manager.BaseMoveSpeed = DemonMoveSpeed;

        if (TryGetComponent<SpriteRenderer>(out var sr))
            _bodySortOrder = sr.sortingOrder;

        // 초기 손 위치 세팅 
        if (RightHand) RightHand.transform.localPosition = RightHandOffset;
        if (LeftHand) LeftHand.transform.localPosition = LeftHandOffset;

        // 초기 위치 저장
        _lastPosition = transform.position;
    }

    protected override void Update()
    {
        base.Update();

        if (IsDead) return;

        // 방향 및 레이어(손)
        HandleFacingAndSorting();
    }

    private void HandleFacingAndSorting()
    {
        // 현재 프레임의 이동량 계산
        float deltaX = transform.position.x - _lastPosition.x;

        if (deltaX > 0.001f) _isFacingRight = true;
        else if (deltaX < -0.001f) _isFacingRight = false;

        transform.rotation = _isFacingRight ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180f, 0);

        RightHand.SetSortingOrder(_bodySortOrder + 1);
        LeftHand.SetSortingOrder(_bodySortOrder - 1);

        // 현재 위치를 마지막 위치로 갱신
        _lastPosition = transform.position;
    }

    public override void ExecuteBasicAttack()
    {
        Vector2 targetDir = GetAttackDirection();

        if (_comboIndex == 0) // 오른손 차례
        {
            if (!RightHand.IsAttacking)
            {
                RightHand.Punch(targetDir);
                _comboIndex = 1; // 다음은 왼손
            }
        }
        else // 왼손 차례
        {
            if (!LeftHand.IsAttacking)
            {
                LeftHand.Punch(targetDir);
                _comboIndex = 0; // 다시 오른손
            }
        }
    }

    public override void ExecuteSkill()
    {
        // TODO: 스킬 구현
    }

    public override void HandleAnimationEvent(string eventName)
    {
        // 본체 애니메이션 이벤트 처리 (필요 시)
    }

    private Vector2 GetAttackDirection()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, AutoTargetRadius, GameAttackLayers);

        Collider2D closestEnemy = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            // 벽이나 장애물이 아닌 'IHittable'을 가진 대상만
            if (hit.GetComponent<IHittable>() == null) continue;

            float d = Vector2.Distance(transform.position, hit.transform.position);
            if (d < closestDist)
            {
                closestDist = d;
                closestEnemy = hit;
            }
        }

        // 2. 적이 있으면 -> 적 방향 리턴
        if (closestEnemy != null)
        {
            return (closestEnemy.transform.position - transform.position).normalized;
        }

        return transform.right;
    }

    // 디버그용 (공격 범위 표시)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AutoTargetRadius);
    }
}