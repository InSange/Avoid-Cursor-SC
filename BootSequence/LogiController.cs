using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LogiController : MonoBehaviour
{
    [Header("Image")]
    public Image ImageSource;

    [Header("Emotion Sprites")]
    public Sprite NeutralSprite;
    public Sprite HappySprite;
    public Sprite SurprisedSprite;
    public Sprite AngrySprite;
    public Sprite MaliciousSprite;
    public Sprite HitSprite;

    [Header("Transition Settings")]
    public float FadeDuration = 0.5f;
    public float ScaleDuration = 0.3f;

    private LogiEmotion _currentEmotion;

    private void Awake()
    {
        SetEmotion(LogiEmotion.Neutral, true);
    }

    public void SetEmotion(LogiEmotion emotion, bool instant = false)
    {
        if (_currentEmotion == emotion) return;

        _currentEmotion = emotion;
        Sprite nextSprite = GetSpriteForEmotion(emotion);

        if (instant)
        {
            ImageSource.sprite = nextSprite;
            return;
        }

        StartCoroutine(TransitionEmotion(nextSprite));
    }

    private Sprite GetSpriteForEmotion(LogiEmotion emotion)
    {
        return emotion switch
        {
            LogiEmotion.Neutral => NeutralSprite,
            LogiEmotion.Happy => HappySprite,
            LogiEmotion.Surprised => SurprisedSprite,
            LogiEmotion.Angry => AngrySprite,
            LogiEmotion.Malicious => MaliciousSprite,
            LogiEmotion.Hit => HitSprite,
            _ => NeutralSprite,
        };
    }

    private IEnumerator TransitionEmotion(Sprite newSprite)
    {
        float t = 0f;
        Color color = ImageSource.color;

        // Fade Out
        while (t < FadeDuration / 2f)
        {
            t += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, t / (FadeDuration / 2f));
            ImageSource.color = color;
            yield return null;
        }

        ImageSource.sprite = newSprite;

        // Fade In
        t = 0f;
        while (t < FadeDuration / 2f)
        {
            t += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, t / (FadeDuration / 2f));
            ImageSource.color = color;
            yield return null;
        }

        ImageSource.color = Color.white;
    }
}