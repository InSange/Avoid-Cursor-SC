using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "CursorReboot/Patterns/Lightning Linear")]
public class Pattern_LightningLinear : EnvironmentPatternBase
{
    protected override void SpawnPattern(PoolingControllerBase poolController, Transform playerTarget)
    {
        poolController.StartCoroutine(SpawnLinearRoutine(poolController));
    }

    private IEnumerator SpawnLinearRoutine(PoolingControllerBase poolController)
    {
        Camera cam = Camera.main;

        // 0:왼쪽->오른쪽, 1:오른쪽->왼쪽, 2:위->아래, 3:아래->위
        int side = Random.Range(0, 4);

        // 줄 개수 랜덤 (1 ~ 3개)
        int lineCount = Random.Range(1, 3);

        // 뷰포트 경계 (여유 있게 0.1 ~ 0.9)
        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0.1f, 0.1f, 0));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(0.9f, 0.9f, 0));

        // 라인 간격 계산용
        float width = max.x - min.x;
        float height = max.y - min.y;

        for (int i = 0; i < lineCount; i++)
        {
            // 이번 라인의 고정 위치 (랜덤)
            float fixedPos;

            // 수평 공격 (좌/우)
            if (side == 0 || side == 1)
            {
                // Y축 랜덤 위치 선택
                fixedPos = Random.Range(min.y, max.y);

                // X축을 따라 번개 5~6개를 쫘르륵 생성 (Line Effect)
                int lightningCount = 6;
                float step = width / lightningCount;

                for (int j = 0; j <= lightningCount; j++)
                {
                    float xPos = (side == 0) ? (min.x + step * j) : (max.x - step * j);
                    SpawnLightning(poolController, new Vector2(xPos, fixedPos));
                    yield return new WaitForSeconds(0.05f); // 순차 생성 연출
                }
            }
            // 수직 공격 (상/하)
            else
            {
                // X축 랜덤 위치 선택
                fixedPos = Random.Range(min.x, max.x);

                int lightningCount = 6;
                float step = height / lightningCount;

                for (int j = 0; j <= lightningCount; j++)
                {
                    float yPos = (side == 3) ? (min.y + step * j) : (max.y - step * j); // 2:위(max)->아래, 3:아래(min)->위
                    SpawnLightning(poolController, new Vector2(fixedPos, yPos));
                    yield return new WaitForSeconds(0.05f);
                }
            }

            // 다음 라인 생성 전 약간의 텀 (동시 다발적 느낌을 위해 짧게)
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void SpawnLightning(PoolingControllerBase pool, Vector2 pos)
    {
        var lightning = pool.GetOrCreatePool(PatternPrefab.GetComponent<Boss_Lightning>()).Get();
        lightning.transform.position = pos;
    }
}