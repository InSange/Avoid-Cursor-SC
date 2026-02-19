using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Boss_NullFragment : BaseBoss, IUnlockProvider
{
    private SpriteRenderer spriteRenderer;

    [Header("스킬 프리팹")]
    public Bullet BulletPrefab;

    [Header("발사 위치 & 플레이어 참조")]
    [SerializeField] private Transform FireCenter;
    [SerializeField] private Transform PlayerTransform;

    [Header("화면 밖 발사기 설정")]
    public float EdgeSpawnRate = 2.5f;
    public float EdgeBulletSpeed = 5f;
    public float SpawnDistance = 10f;

    private bool IsEdgeSpawning = false;

    [Header("페이즈별 발사 주기")]
    [SerializeField] private float Phase1Rate = 2.5f;
    [SerializeField] private float Phase2Rate = 1.75f;
    [SerializeField] private float Phase3Rate = 1.0f;

    [SerializeField] private float Phase1EdgeRate = 3.5f;
    [SerializeField] private float Phase2EdgeRate = 2.5f;
    [SerializeField] private float Phase3EdgeRate = 1.25f;

    [SerializeField] private float _baseSkillBulletSpeed = 5f;

    protected override void HandleAnimationEvent(string eventName)
    {
        /*if (eventName == "IntroEnd")
        {
            Debug.Log("Idle 로 바꾼다 !!");
            CursorFSM.ChangeState(CursorState.Idle);
            OnIntroComplete();
        }*/
    }

    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();

        CursorFSM.OnStateAnimationComplete += (state) =>
        {
            if (state == CursorState.Die)
            {
                Destroy(gameObject);
            }
            /*            else if(state == CursorState.Intro)
                        {
                            Debug.Log("Idle 로 바꾼다 !!");
                            CursorFSM.ChangeState(CursorState.Idle);
                            OnIntroComplete();
                        }*/
        };
    }

    public override void InitializeAsNPC(BossGalleryManager manager)
    {
        // 부모의 기본 NPC 초기화 (Idle 상태 전환, IsHostile = false 등)
        base.InitializeAsNPC(manager);

        // 💥 추가: Intro 애니메이션을 건너뛰므로 스케일과 컬러를 강제 설정
        transform.localScale = new Vector3(1.5f, 1.5f, 1f); // 목표 스케일

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f; // 투명도 완전 불투명
            spriteRenderer.color = c;
        }
    }

    public override void ApplyGalleryBuffs(float multiplier)
    {
        // 1. 부모의 기본 강화 (애니메이션 가속) 실행
        base.ApplyGalleryBuffs(multiplier);

        // 2. 이동/패턴 속도 관련 변수 강화
        // (값이 작을수록 빠른 것들은 나누고, 클수록 빠른 것들은 곱합니다)

        // 발사 주기 단축 (더 자주 발사)
        Phase1Rate /= multiplier;
        Phase2Rate /= multiplier;
        Phase3Rate /= multiplier;

        Phase1EdgeRate /= multiplier;
        Phase2EdgeRate /= multiplier;
        Phase3EdgeRate /= multiplier;

        EdgeSpawnRate /= multiplier;

        // 투사체 속도 증가 (더 빠르게 날아옴)
        EdgeBulletSpeed *= multiplier;

        Debug.Log($"[NullFragment] 갤러리 버프 적용 완료: 발사 속도 및 투사체 속도 x{multiplier}");
    }

    protected override void UpdatePhase()
    {
        float ratio = (float)CurrentHP / MaxHP;
        BossPhase next = ratio <= 0.33f ? BossPhase.Phase3
                        : ratio <= 0.66f ? BossPhase.Phase2
                        : BossPhase.Phase1;

        if (next != CurrentPhase)
        {
            CurrentPhase = next;
            OnPhaseChanged(next);
            Debug.Log($"[Boss] 페이즈 전환: {CurrentPhase} → {next}");
        }
    }

    protected override void OnPhaseChanged(BossPhase newPhase)
    {
        // 중심 발사 주기 설정
        float centerRate = newPhase == BossPhase.Phase1 ? Phase1Rate
                        : newPhase == BossPhase.Phase2 ? Phase2Rate
                        : Phase3Rate;
        // 엣지 발사 주기 설정
        float edgeRate = newPhase == BossPhase.Phase1 ? Phase1EdgeRate
                      : newPhase == BossPhase.Phase2 ? Phase2EdgeRate
                      : Phase3EdgeRate;

        // 중심 스킬 루프
        StartSkillLoop(centerRate);

        // 엣지 스폰 루프
        if (IsEdgeSpawning)
            CancelInvoke(nameof(SpawnFromEdge));
        InvokeRepeating(nameof(SpawnFromEdge), 1f, edgeRate);
        IsEdgeSpawning = true;
    }

    public override void UseSkillPattern()
    {
        if (PlayerTransform == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) PlayerTransform = go.transform;
        }
        if (PlayerTransform == null || FireCenter == null || BulletPrefab == null) return;

        switch (CurrentPhase)
        {
            case BossPhase.Phase1:
                FireSingleTargetingBullet();
                break;
            case BossPhase.Phase2:
                FireDoubleBullet();
                break;
            case BossPhase.Phase3:
                FireSpreadShot(5, 15f);
                break;
        }
    }

    private void SpawnFromEdge()
    {
        if (!PlayerTransform || BulletPrefab == null) return;

        Vector2 screenCenter = (Vector2)Camera.main.transform.position;
        Vector2 spawnPos = screenCenter + Random.insideUnitCircle.normalized * SpawnDistance;

        var bullet = PoolingControllerBase.Current.GetOrCreatePool(BulletPrefab).Get();
        if (bullet == null) return;

        Bullet.BulletType type = (Random.value < 0.5f) ? Bullet.BulletType.Targeting : Bullet.BulletType.Straight;
        Vector2 direction = (type == Bullet.BulletType.Targeting)
            ? ((Vector2)PlayerTransform.position - spawnPos).normalized
            : (spawnPos - screenCenter).normalized * -1;

        bullet.transform.position = spawnPos;
        bullet.Speed = EdgeBulletSpeed;
        bullet.Initialize(direction, type, damage: 1, targetTag: "Player");
    }

    private void FireSingleTargetingBullet()
    {
        Vector2 spawnPos = FireCenter.position;
        Vector2 target = PlayerTransform.position;

        var bullet = PoolingControllerBase.Current.GetOrCreatePool(BulletPrefab).Get();
        if (bullet == null) return;

        Vector2 direction = (target - spawnPos).normalized;
        bullet.transform.position = spawnPos;

        bullet.Speed = 5f * CursorObject.PlaybackSpeed; ;
        bullet.Initialize(direction, Bullet.BulletType.Targeting, damage: 1, targetTag: "Player");
    }

    private void FireDoubleBullet()
    {
        for (int i = -1; i <= 1; i += 2)
        {
            Vector3 spawnPos = FireCenter.position;
            Vector2 dir = ((Vector2)(PlayerTransform.position - spawnPos)).normalized;
            Vector2 sideOffset = Vector2.Perpendicular(dir) * 0.5f * i;

            var bullet = PoolingControllerBase.Current.GetOrCreatePool(BulletPrefab).Get();
            if (bullet == null) continue;

            bullet.transform.position = spawnPos + (Vector3)sideOffset;
            bullet.Speed = 5f * CursorObject.PlaybackSpeed; ;
            bullet.Initialize(dir, Bullet.BulletType.Targeting, damage: 1, targetTag: "Player");
        }
    }

    private void FireSpreadShot(int count, float angleBetween)
    {
        Vector2 dir = ((Vector2)(PlayerTransform.position - FireCenter.position)).normalized;
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float startAngle = baseAngle - angleBetween * (count - 1) / 2f;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + i * angleBetween;
            Vector2 shotDir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

            var bullet = PoolingControllerBase.Current.GetOrCreatePool(BulletPrefab).Get();
            if (bullet == null) continue;

            bullet.transform.position = FireCenter.position;
            bullet.Speed = 5f * CursorObject.PlaybackSpeed; ;
            bullet.Initialize(shotDir.normalized, Bullet.BulletType.Straight, damage: 1, targetTag: "Player");
        }
    }



    public override void OnHit(int damage)
    {
        base.OnHit(damage);
        if (IsAlive)
            StartCoroutine(HitFlash());
    }

    private IEnumerator HitFlash()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }

    protected override void Die()
    {
        base.Die();
        // 스킬/엣지 스폰 정지
        StopSkillLoop();
        CancelInvoke(nameof(SpawnFromEdge));
    }

    public IEnumerable<UnlockID> GetUnlocksOnDefeat()
    {
        // 튜토리얼 보스 처치 시, NPC와 컬러 복구 패시브를 해금합니다.
        yield return UnlockID.BossNPC_NullFragment;
        yield return UnlockID.Passive_ColorRestored;
    }
}
