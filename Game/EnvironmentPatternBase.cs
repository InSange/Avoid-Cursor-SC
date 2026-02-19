using System.Collections;
using UnityEngine;

/// <summary>
/// 모든 환경 패턴(번개, 불, 얼음 등)의 부모 클래스입니다.
/// 각 패턴은 이 클래스를 상속받아 구체적인 스폰 로직(ExecutePattern)을 구현합니다.
/// </summary>
public abstract class EnvironmentPatternBase : ScriptableObject
{
    [Header("패턴 설정")]
    public GameObject PatternPrefab; // (예: Boss_Lightning 프리팹)
    public float DefaultInterval = 3.0f;

    [Tooltip("체크하면 InfiniteMode에서도 등장합니다. 체크 해제하면 Gallery 전용이 됩니다.")]
    public bool EnableInInfiniteMode = true;

    /// <summary>
    /// 매니저가 이 코루틴을 시작시킵니다.
    /// </summary>
    public IEnumerator Execute(PoolingControllerBase poolController, Transform playerTarget, float speedMultiplier = 1.0f)
    {
        // (시작 딜레이)
        yield return new WaitForSeconds(1.0f);

        while (true)
        {
            // 1. 패턴 실행
            SpawnPattern(poolController, playerTarget);

            // 2. 대기 (난이도에 따라 빨라짐)
            yield return new WaitForSeconds(DefaultInterval / speedMultiplier);
        }
    }

    /// <summary>
    /// 실제 패턴이 생성되는 로직 (자식 클래스에서 구현)
    /// </summary>
    protected abstract void SpawnPattern(PoolingControllerBase poolController, Transform playerTarget);
}