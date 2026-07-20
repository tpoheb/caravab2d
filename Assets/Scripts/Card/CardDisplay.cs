using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    [Header("Визуальные компоненты")]
    [SerializeField] private Image cardBackImage;
    [SerializeField] private GameObject cardFaceRoot;
    [SerializeField] private Image cardFaceImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image typeIcon;

    [Header("Спрайты (единые для всех карт)")]
    [SerializeField] private Sprite cardFaceSprite;
    [SerializeField] private Sprite cardBackSprite;

    [Header("Анимация флипа")]
    [SerializeField] private float flipDuration = 0.5f;
    [SerializeField] private AnimationCurve flipCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Лёгкое появление (scale)")]
    [SerializeField] private float appearDuration = 0.25f;

    private ICard _currentCard;
    private bool _isFlipped = false;
    private Coroutine _flipCoroutine;

    public event Action OnCardRevealed;

    public void Setup(ICard data)
    {
        _currentCard = data;
        _isFlipped = false;

        if (cardBackImage != null)
            cardBackImage.sprite = cardBackSprite;

        if (cardFaceImage != null)
            cardFaceImage.sprite = cardFaceSprite;

        FillFaceContent(data);
        SetFaceVisible(false);

        transform.localScale = Vector3.zero;
    }

    public ICard GetCurrentCard() => _currentCard;

    public void ShowCard()
    {
        gameObject.SetActive(true);
        if (_flipCoroutine != null) StopCoroutine(_flipCoroutine);
        _flipCoroutine = StartCoroutine(AppearThenFlip());
    }

    public void FlipToFace()
    {
        if (_isFlipped) return;
        if (_flipCoroutine != null) StopCoroutine(_flipCoroutine);
        _flipCoroutine = StartCoroutine(FlipCoroutine());
    }

    public void HideCard(bool immediate = false)
    {
        if (_flipCoroutine != null) StopCoroutine(_flipCoroutine);

        if (immediate)
        {
            gameObject.SetActive(false);
            _isFlipped = false;
            return;
        }

        StartCoroutine(HideCoroutine());
    }

    private IEnumerator AppearThenFlip()
    {
        yield return StartCoroutine(ScaleTo(Vector3.one, appearDuration));
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(FlipCoroutine());
    }

    private IEnumerator FlipCoroutine()
    {
        float halfDuration = flipDuration * 0.5f;
        yield return StartCoroutine(ScaleXTo(0f, halfDuration));
        SetFaceVisible(true);
        yield return StartCoroutine(ScaleXTo(1f, halfDuration));

        _isFlipped = true;
        OnCardRevealed?.Invoke();
    }

    private IEnumerator HideCoroutine()
    {
        yield return StartCoroutine(ScaleTo(Vector3.zero, appearDuration));
        gameObject.SetActive(false);
        _isFlipped = false;
        SetFaceVisible(false);
    }

    private IEnumerator ScaleTo(Vector3 target, float duration)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = flipCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
            transform.localScale = Vector3.LerpUnclamped(startScale, target, t);
            yield return null;
        }

        transform.localScale = target;
    }

    private IEnumerator ScaleXTo(float targetX, float duration)
    {
        float startX = transform.localScale.x;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = flipCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
            Vector3 s = transform.localScale;
            s.x = Mathf.LerpUnclamped(startX, targetX, t);
            transform.localScale = s;
            yield return null;
        }

        Vector3 final = transform.localScale;
        final.x = targetX;
        transform.localScale = final;
    }

    private void FillFaceContent(ICard data)
    {
        if (titleText != null) titleText.text = data.CardName;
        if (descriptionText != null) descriptionText.text = data.Description;
    }

    private void SetFaceVisible(bool visible)
    {
        if (cardFaceRoot != null) cardFaceRoot.SetActive(visible);
        if (cardBackImage != null) cardBackImage.enabled = !visible;
    }
}