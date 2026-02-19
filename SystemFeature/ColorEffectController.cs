using UnityEngine;

public static class SharedMaterials
{
    public static readonly Material Default = new Material(Shader.Find("Sprites/Default"));
    public static readonly Material Grayscale = Resources.Load<Material>("Materials/Grayscale");
}

[RequireComponent(typeof(SpriteRenderer))]
public class ColorEffectController : MonoBehaviour
{
    private SpriteRenderer _renderer;
    [SerializeField] private Material _material;
    [SerializeField] private Material _gray;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        ApplyColorSetting();
        _material = SharedMaterials.Default;
        _gray = SharedMaterials.Grayscale;

        AvoidCursorGameManager.Instance.OnGrayscaleStatusChanged += HandleGrayscaleStatusChange;
    }

    private void OnDestroy()
    {
        if (AvoidCursorGameManager.Instance != null)
        {
            AvoidCursorGameManager.Instance.OnGrayscaleStatusChanged -= HandleGrayscaleStatusChange;
        }
    }

    private void HandleGrayscaleStatusChange(bool isActive)
    {
        ApplyColorSetting();
    }

    public void ApplyColorSetting()
    {
        _renderer.material = AvoidCursorGameManager.Instance.IsGrayscaleDebuffActive
             ? SharedMaterials.Grayscale
             : SharedMaterials.Default;
    }
}