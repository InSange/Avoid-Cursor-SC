using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Boss_Firewall : BaseBoss, IUnlockProvider
{
    [Header("Structure")]
    public Transform LeftWall;
    public Transform RightWall;
    public SpriteRenderer LeftRenderer;
    public SpriteRenderer RightRenderer;

    [Header("Settings")]
    public float WallMoveDuration = 1.0f;
    public float OpenWidth = 8f;  // 평소 벌려진 거리 (X축)
    public float CrushWidth = 2.5f; // 압박했을 때 거리 (X축)

    [Header("Attack Settings")]
    public GameObject BulletPrefab; // (Bullet 프리팹 할당)
    public float BulletSpeed = 6f;

    private void Start()
    {
        base.Start();

        // 1. 자식 벽들에게 데미지 전달 기능 부여
        SetupWall(LeftWall);
        SetupWall(RightWall);

        // 2. 초기 위치 설정 (양쪽으로 벌리기)
        ResetWalls();
    }

    private void SetupWall(Transform wall)
    {
        if (wall == null) return;
        var redirector = wall.gameObject.AddComponent<RedirectDamage>();
        redirector.Initialize(this);
    }

    public override void EnterIntroPhase()
    {
        base.EnterIntroPhase(); // IsHostile = true 설정

        // 연출: 벽이 화면 밖에서 안으로 쿵! 하고 들어옴
        LeftWall.localPosition = new Vector3(-15, 0, 0);
        RightWall.localPosition = new Vector3(15, 0, 0);

        Sequence seq = DOTween.Sequence();
        seq.Append(LeftWall.DOLocalMoveX(-OpenWidth, 1.5f).SetEase(Ease.OutBounce));
        seq.Join(RightWall.DOLocalMoveX(OpenWidth, 1.5f).SetEase(Ease.OutBounce));
        seq.OnComplete(() =>
        {
            OnIntroComplete(); // BaseBoss에게 알림 -> 전투 시작
        });
    }

    // FSM 애니메이션이 없으므로 수동으로 Idle 전환
    protected override void OnIntroComplete()
    {
        CursorFSM.ChangeState(CursorState.Idle);
        StartSkillLoop(3.0f); // 3초마다 패턴 실행
    }

    public override void UseSkillPattern()
    {
        // 랜덤 패턴 선택
        int pattern = Random.Range(0, 3);

        switch (pattern)
        {
            case 0: StartCoroutine(Pattern_Crush()); break; // 압박
            case 1: StartCoroutine(Pattern_Barrage()); break; // 탄막
            case 2: StartCoroutine(Pattern_ScanLaser()); break; // (구현 예정 or 간단 탄막)
        }
    }

    // --- 패턴 1: 압박 (Crush) ---
    private IEnumerator Pattern_Crush()
    {
        Debug.Log("[Firewall] 접근 거부 (Crush)");

        // 1. 경고 (색상 변경)
        LeftRenderer.color = Color.red;
        RightRenderer.color = Color.red;
        yield return new WaitForSeconds(0.5f);

        // 2. 좁혀오기 (플레이어를 가둠)
        LeftWall.DOLocalMoveX(-CrushWidth, 0.5f).SetEase(Ease.InCubic);
        RightWall.DOLocalMoveX(CrushWidth, 0.5f).SetEase(Ease.InCubic);

        yield return new WaitForSeconds(0.6f);

        // 3. 쾅! (화면 흔들림 효과 추가 가능)
        // Camera.main.DOShakePosition(0.3f, 0.5f); 

        yield return new WaitForSeconds(1.0f);

        // 4. 복귀
        ResetWalls();
    }

    // --- 패턴 2: 방화벽 탄막 (Barrage) ---
    private IEnumerator Pattern_Barrage()
    {
        Debug.Log("[Firewall] 패킷 폭격 (Barrage)");

        // 벽 안쪽면에서 총알 발사
        int waves = 5;
        for (int i = 0; i < waves; i++)
        {
            SpawnBulletFromWall(LeftWall, Vector2.right);
            SpawnBulletFromWall(RightWall, Vector2.left);
            yield return new WaitForSeconds(0.3f);
        }
    }

    // --- 패턴 3: 스캔 (Scan) ---
    private IEnumerator Pattern_ScanLaser()
    {
        // 간단하게 위아래로 움직이며 총알 뿌리기
        Debug.Log("[Firewall] 보안 스캔");

        float duration = 2.0f;

        // 벽을 위아래로 엇갈리게 이동
        LeftWall.DOLocalMoveY(3f, duration).SetLoops(2, LoopType.Yoyo);
        RightWall.DOLocalMoveY(-3f, duration).SetLoops(2, LoopType.Yoyo);

        float timer = 0f;
        while (timer < duration * 2)
        {
            if (Random.value > 0.7f) SpawnBulletFromWall(LeftWall, Vector2.right);
            if (Random.value > 0.7f) SpawnBulletFromWall(RightWall, Vector2.left);
            timer += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        // 위치 복구
        LeftWall.DOLocalMoveY(0, 0.5f);
        RightWall.DOLocalMoveY(0, 0.5f);
    }

    private void SpawnBulletFromWall(Transform wall, Vector2 dir)
    {
        if (PoolingControllerBase.Current == null || BulletPrefab == null) return;

        // 벽의 Y축 랜덤 위치에서 발사
        Vector3 spawnPos = wall.position + new Vector3(0, Random.Range(-4f, 4f), 0);

        var bullet = PoolingControllerBase.Current.GetOrCreatePool(BulletPrefab.GetComponent<Bullet>()).Get();
        bullet.transform.position = spawnPos;
        bullet.Speed = BulletSpeed;
        bullet.Initialize(dir, Bullet.BulletType.Straight, 1, "Player");
    }

    private void ResetWalls()
    {
        // 원래 색상과 위치로 복귀
        LeftRenderer.color = Color.white;
        RightRenderer.color = Color.white;

        LeftWall.DOLocalMoveX(-OpenWidth, WallMoveDuration).SetEase(Ease.OutQuad);
        RightWall.DOLocalMoveX(OpenWidth, WallMoveDuration).SetEase(Ease.OutQuad);
    }

    // 피격 시 부모(나)도 깜빡임
    public override void OnHit(int damage)
    {
        base.OnHit(damage);
        if (IsAlive)
        {
            LeftRenderer.DOColor(Color.red, 0.1f).SetLoops(2, LoopType.Yoyo);
            RightRenderer.DOColor(Color.red, 0.1f).SetLoops(2, LoopType.Yoyo);
        }
    }

    public IEnumerable<UnlockID> GetUnlocksOnDefeat()
    {
        // 방어형 보스 처치 시 -> 배리어 아이템 해금
        yield return UnlockID.Item_Barrier;
    }
}