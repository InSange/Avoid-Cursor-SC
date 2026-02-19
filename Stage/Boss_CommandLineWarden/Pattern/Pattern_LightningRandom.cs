using UnityEngine;

[CreateAssetMenu(menuName = "CursorReboot/Patterns/Lightning Random")]
public class Pattern_LightningRandom : EnvironmentPatternBase
{
    protected override void SpawnPattern(PoolingControllerBase poolController, Transform playerTarget)
    {
        Vector2 screenPos = Camera.main.ViewportToWorldPoint(new Vector3(Random.value, Random.value, 0));

        var lightning = poolController.GetOrCreatePool(PatternPrefab.GetComponent<Boss_Lightning>()).Get();
        lightning.transform.position = screenPos;
    }
}