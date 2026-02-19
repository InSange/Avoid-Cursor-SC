using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class SceneTransitionManager : Manager<SceneTransitionManager>
{
    [Header("UI Reference")]
    public CanvasGroup FadeCanvasGroup; // 검은 화면
    public RawImage NoiseImage;         // 노이즈 화면 (Gallery용)
    public RectTransform NoiseRect;     // 노이즈 크기 제어용 (TV 효과)

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject); // 씬 이동 시 파괴되지 않음

        FadeCanvasGroup.alpha = 0f;
        FadeCanvasGroup.blocksRaycasts = false;

        NoiseImage.color = new Color(1, 1, 1, 0);
        NoiseImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// Hub -> Gallery 이동 (Fade Out -> Noise -> Load -> Fade In)
    /// </summary>
    public void LoadGalleryScene()
    {
        StartCoroutine(Routine_HubToGallery());
    }

    /// <summary>
    /// Gallery -> Hub 이동 (Noise -> Fade Out -> Load -> Fade In)
    /// </summary>
    public void ReturnToHub()
    {
        StartCoroutine(Routine_GalleryToHub());
    }

    private IEnumerator Routine_HubToGallery()
    {
        FadeCanvasGroup.blocksRaycasts = true;

        // 1. Hub: 검은 화면 Fade Out (어두워짐)
        yield return FadeCanvasGroup.DOFade(1f, 0.5f).WaitForCompletion();

        // 2. 씬 로드
        yield return LoadSceneAsync("BossGalleryScene");

        // 3. Gallery: TV 켜지는 효과 (Noise Open)
        yield return StartCoroutine(TVTurnOnEffect(0.6f));

        FadeCanvasGroup.alpha = 0f;

        FadeCanvasGroup.blocksRaycasts = false;
    }

    private IEnumerator Routine_GalleryToHub()
    {
        FadeCanvasGroup.blocksRaycasts = true;

        FadeCanvasGroup.alpha = 1f;

        // 1. Gallery: TV 꺼지는 효과 (Noise Close)
        yield return StartCoroutine(TVTurnOffEffect(0.5f));

        // 2. (깜빡임 방지) 검은 화면 미리 켜두기
        FadeCanvasGroup.alpha = 1f;

        // 3. 씬 로드
        yield return LoadSceneAsync("MainScene"); // (MainScene 이름 확인)

        // 4. Hub: 검은 화면 Fade In (밝아짐)
        yield return FadeCanvasGroup.DOFade(0f, 0.8f).WaitForCompletion();

        FadeCanvasGroup.blocksRaycasts = false;
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;
        while (op.progress < 0.9f) yield return null;
        op.allowSceneActivation = true;
        yield return new WaitForSeconds(0.1f); // 초기화 대기
    }

    private IEnumerator TVTurnOnEffect(float duration)
    {
        NoiseImage.gameObject.SetActive(true);
        NoiseImage.color = Color.white;

        // 시작: 가로선 (Y=0.01)
        NoiseRect.localScale = new Vector3(1f, 0.01f, 1f);

        // 1. 가로선이 펴짐 (Y: 0.01 -> 1)
        NoiseRect.DOScaleY(1f, duration * 0.5f).SetEase(Ease.OutExpo);

        // 2. 동시에 노이즈 지지직
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            UpdateNoiseUV();

            if (t > duration * 0.8f)
                FadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, (t - duration * 0.8f) / (duration * 0.2f));

            yield return null;
        }

        // 3. 끝: 사라짐
        NoiseImage.color = new Color(1, 1, 1, 0);
        NoiseImage.gameObject.SetActive(false);
        NoiseRect.localScale = Vector3.one; // 복구
        FadeCanvasGroup.alpha = 0f;
    }

    private IEnumerator TVTurnOffEffect(float duration)
    {
        NoiseImage.gameObject.SetActive(true);
        NoiseImage.color = Color.white;
        NoiseRect.localScale = Vector3.one;

        FadeCanvasGroup.alpha = 1f;

        NoiseRect.DOScaleY(0.01f, duration * 0.7f).SetEase(Ease.InExpo);
        NoiseRect.DOScaleX(0f, duration * 0.3f).SetDelay(duration * 0.7f).SetEase(Ease.OutExpo);

        // 노이즈 재생
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            UpdateNoiseUV();
            yield return null;
        }

        NoiseImage.color = new Color(1, 1, 1, 0);
        NoiseImage.gameObject.SetActive(false);
        NoiseRect.localScale = Vector3.one; // 다음을 위해 복구
    }

    private void UpdateNoiseUV()
    {
        // 뭉치지 않게 오프셋만 랜덤 이동 (Tiling은 유지)
        Rect uv = NoiseImage.uvRect;
        uv.x = Random.value; // 오프셋 X 랜덤
        uv.y = Random.value; // 오프셋 Y 랜덤
        // uv.width, height는 건드리지 않음 (Inspector에서 1,1 또는 0.5, 0.5 등으로 설정)
        NoiseImage.uvRect = uv;
    }
}