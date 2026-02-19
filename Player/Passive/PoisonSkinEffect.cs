using UnityEngine;

public class PoisonSkinEffect : MonoBehaviour
{
    [Header("설정")]
    public int Damage = 1;
    public float TickRate = 1f;
    public float Range = 1.2f;

    [Header("Visual")]
    public Color PoisonTint = new Color(0.6f, 0.2f, 0.8f, 1f); // 보라색

    [Header("Animation Data")]
    public CursorAnimation PoisonTickAnim;

    private float _timer = 0f;
    private LayerMask _enemyLayer;
    private SpriteRenderer _playerRenderer;
    private Color _originalColor;

    private CursorObject _visualCursorObject;
    private SpriteRenderer _visualRenderer;

    void Start()
    {
        _enemyLayer = LayerMask.GetMask("Boss", "Enemy");
        _playerRenderer = GetComponent<SpriteRenderer>();

        if (_playerRenderer != null)
        {
            _originalColor = _playerRenderer.color;
            _playerRenderer.color = _originalColor * PoisonTint;
        }

        CreateVisualObject();
    }

    void Update()
    {
        _timer += Time.deltaTime;

        if (_timer >= TickRate)
        {
            _timer = 0f;
            ApplyPoisonDamage();

            if (_visualCursorObject != null && PoisonTickAnim != null)
            {
                _visualRenderer.enabled = true;
                _visualCursorObject.PlayAnimation(PoisonTickAnim);
            }
        }
    }

    private void CreateVisualObject()
    {
        if (PoisonTickAnim == null)
        {
            Debug.LogWarning("[PoisonSkin] 애니메이션 데이터가 없습니다!");
            return;
        }

        GameObject go = new GameObject("PoisonVisual");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        _visualCursorObject = go.AddComponent<CursorObject>();
        _visualRenderer = go.GetComponent<SpriteRenderer>();

        if (_playerRenderer != null)
            _visualRenderer.sortingOrder = _playerRenderer.sortingOrder + 1;

        // 초기 상태: 숨김
        _visualRenderer.enabled = false;

        // 💥 애니메이션 완료 시 자동으로 숨기기 이벤트 등록
        _visualCursorObject.OnAnimationComplete += () =>
        {
            _visualRenderer.enabled = false;
        };
    }

    private void ApplyPoisonDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, Range, _enemyLayer);
        foreach (var hit in hits)
        {
            hit.GetComponent<IHittable>()?.OnHit(Damage);
        }
    }

    void OnDestroy()
    {
        // 스크립트 제거 시(죽거나 게임 종료) 원래 색상 복구
        if (_playerRenderer != null)
        {
            _playerRenderer.color = _originalColor;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, Range);
    }
}