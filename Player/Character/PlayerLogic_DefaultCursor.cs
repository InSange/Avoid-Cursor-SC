using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 기본 캐릭터(마우스 커서)의 공격 로직 구현체입니다.
/// </summary>
public class PlayerLogic_DefaultCursor : PlayerLogicBase
{
    public override float SkillStaminaCost => 5f;

    [Header("스킬 데이터")]
    public GameObject WavePrefab;

    // Left Click: 대상에게 데미지 1을 주는 찌르기 (Poke)
    public override void ExecuteBasicAttack()
    {
        // 1. FSM을 Attack1 상태로 전환 (애니메이션 시작)
        Manager.FsmController.ChangeState(CursorState.Attack1);

        // 💥 2. (핵심) 애니메이션 이벤트 없이 즉시 데미지 판정 실행
        ExecutePokeDamage();
    }

    private void ExecutePokeDamage()
    {
        Vector2 pokePosition = Manager.transform.position;

        Collider2D[] hits = Physics2D.OverlapPointAll(pokePosition, GameAttackLayers);

        if (hits.Length > 0)
        {
            foreach (Collider2D hit in hits)
            {
                if (hit.TryGetComponent<IHittable>(out var hittable))
                {
                    hittable.OnHit(1); // 1 damage poke
                    return; // 한 대상만 공격하고 종료
                }
            }
        }
    }

    // Right Click: 스태미나를 소모하여 WaveAttack 발동
    public override void ExecuteSkill()
    {
        SpawnWaveAttack();
    }

    public override void ExecuteItem()
    {
        // throw new System.NotImplementedException();
        Debug.Log("[DefaultCursor] 아이템 사용됨! (스페이스바)");
    }

    public override void HandleAnimationEvent(string eventName)
    {
        if (eventName == "SpawnWave")
        {
            Debug.Log("SpawnWave 발동");
            SpawnWaveAttack();
        }
    }

    // (WaveAttack 로직은 이전 PlayerCursorStateManager에서 가져옴)
    private void SpawnWaveAttack()
    {
        if (WavePrefab == null) return;

        GameObject wave = Instantiate(WavePrefab, Manager.transform.position, Quaternion.identity);

        WaveAttack waveAttack = wave.GetComponent<WaveAttack>();
        waveAttack.Initialize(0.7f, 2, LayerMask.GetMask("Boss", "Enemy", "Entity"));
    }
}