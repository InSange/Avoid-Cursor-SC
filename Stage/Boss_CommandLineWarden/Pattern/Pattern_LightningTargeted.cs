using UnityEngine;

[CreateAssetMenu(menuName = "CursorReboot/Patterns/Lightning Targeted")]
public class Pattern_LightningTargeted : EnvironmentPatternBase
{
    protected override void SpawnPattern(PoolingControllerBase poolController, Transform playerTarget)
    {
        if (playerTarget == null) return;

        var lightning = poolController.GetOrCreatePool(PatternPrefab.GetComponent<Boss_Lightning>()).Get();
        Vector3 offset = new Vector3(0.25f, 1.0f, 0f);
        lightning.transform.position = playerTarget.position + offset;
    }
}