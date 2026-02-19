using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitShieldController : MonoBehaviour
{
    [Header("설정")]
    public GameObject ShieldPrefab;  // 방패 프리팹 (OrbitShieldUnit이 붙어있어야 함)
    public float RotateSpeed = 150f; // 회전 속도
    public float Radius = 1.0f;      // 거리
    public float RespawnTime = 10f;  // 재생성 시간
    public int MaxShieldCount = 5;   // 최대 개수

    private int _currentCount = 0;   // 현재 방패 개수
    private List<OrbitShield> _shields = new List<OrbitShield>();

    private float _currentAngle = 0f; // 전체 회전 각도

    public int CurrentCount => _currentCount;

    /// <summary>
    /// 증강 획득 시 호출: 방패 개수를 늘림 (+1)
    /// </summary>
    public void AddShield(GameObject prefab)
    {
        ShieldPrefab = prefab; // 프리팹 등록 (최초 1회)

        if (_currentCount >= MaxShieldCount) return;

        _currentCount++;
        RebuildFormation();
    }

    /// <summary>
    /// 방패 오브젝트들을 싹 지우고 개수에 맞춰 새로 배치합니다.
    /// </summary>
    private void RebuildFormation()
    {
        int needed = _currentCount - _shields.Count;
        for (int i = 0; i < needed; i++)
        {
            GameObject go = Instantiate(ShieldPrefab, transform); // 플레이어 자식으로
            OrbitShield shield = go.GetComponent<OrbitShield>();
            shield.Initialize(this); // 컨트롤러 주입
            _shields.Add(shield);
        }

        foreach (var shield in _shields)
        {
            shield.gameObject.SetActive(true);
        }

        UpdatePositions();
    }

    void Update()
    {
        if (_currentCount == 0) return;

        // 각도는 항상 계속 돌아감 (부서져 있어도 시간은 흐름 -> 리스폰 시 자연스러운 위치)
        _currentAngle += RotateSpeed * Time.deltaTime;
        _currentAngle %= 360f;

        UpdatePositions();
    }

    /// <summary>
    /// 모든 방패의 위치를 각도에 맞춰 재배치합니다. (대형 유지)
    /// </summary>
    private void UpdatePositions()
    {
        float angleStep = 360f / _currentCount; // (예: 3개면 120도씩)

        for (int i = 0; i < _shields.Count; i++)
        {
            if (_shields[i] == null) continue;

            // (기본 각도 + 내 순번 * 간격)
            float targetAngle = _currentAngle + (angleStep * i);
            float radian = targetAngle * Mathf.Deg2Rad;

            float x = Mathf.Cos(radian) * Radius;
            float y = Mathf.Sin(radian) * Radius;

            _shields[i].transform.localPosition = new Vector3(x, y, 0);
            // (회전도 시키려면)
            // _shields[i].transform.rotation = Quaternion.Euler(0, 0, targetAngle);
        }
    }

    /// <summary>
    /// 개별 방패가 맞았을 때 호출됨
    /// </summary>
    public void OnShieldHit(OrbitShield shield)
    {
        // 1. 해당 방패만 비활성화 (숨김)
        shield.gameObject.SetActive(false);

        // 2. 개별 리스폰 코루틴 시작
        StartCoroutine(RespawnSpecificShield(shield));
    }

    private IEnumerator RespawnSpecificShield(OrbitShield shield)
    {
        // 10초 대기
        yield return new WaitForSeconds(RespawnTime);

        // 방패가 아직 존재한다면(게임 종료 등으로 파괴되지 않았다면) 다시 켜기
        if (shield != null)
        {
            shield.gameObject.SetActive(true);
            // 위치는 Update에서 계속 잡고 있었으므로, 켜지자마자 제자리에 있음 (순간이동 X)
            Debug.Log("[Shield] 개별 방패 재생성!");
        }
    }
}