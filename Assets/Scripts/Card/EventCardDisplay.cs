using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Компонент на префабе карты события.
/// Управляет flip-анимацией и отображением данных.
/// 
/// Иерархия префаба:
/// EventCard (RectTransform, Image = рубашка)
///   └─ CardFace (RectTransform, Image = лицевая сторона)
///        ├─ TitleText (TextMeshProUGUI)
///        ├─ DescriptionText (TextMeshProUGUI)
///        └─ TypeIcon (Image, опционально)
/// </summary>
public class EventCardDisplay : MonoBehaviour
{
    [Header("Визуальные компоненты")]
    [SerializeField] private Image cardBackImage;       // рубашка — Image на корневом объекте
    [SerializeField] private GameObject cardFaceRoot;   // лицевая сторона целиком
    [SerializeField] private Image cardFaceImage;       // фоновый спрайт лицевой
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image typeIcon;            // опционально

    [Header("Анимация флипа")]
    [Tooltip("Полное время анимации флипа (секунды)")]
    [SerializeField] private float flipDuration = 0.5f;

    [Tooltip("AnimationCurve для ускорения/замедления флипа")]
    [SerializeField] private AnimationCurve flipCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Лёгкое появление (scale)")]
    [SerializeField] private float appearDuration = 0.25f;

    // ── приватное состояние ──────────────────────────────────────────────
    private EventCardData _currentCard;
    private bool _isFlipped = false;          // true = лицевая сторона видна
    private Coroutine _flipCoroutine;

    // Событие: флип завершён, карта показана
    public event Action OnCardRevealed;

    // ────────────────────────────────────────────────────────────────────
    // Public API
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Инициализация карты данными. Вызывать перед ShowCard.
    /// </summary>
    public void Setup(EventCardData data, Sprite fallbackBackSprite = null)
    {
        _currentCard = data;
        _isFlipped = false;

        // Рубашка
        if (cardBackImage != null)
            cardBackImage.sprite = (data.cardBackSprite != null) ? data.cardBackSprite : fallbackBackSprite;

        // Лицевая сторона заполняется, но скрывается
        FillFaceContent(data);
        SetFaceVisible(false);

        // Сброс трансформа
        transform.localScale = Vector3.zero;
    }

    /// <summary>
    /// Показывает карту: сначала появляется рубашкой, затем делает flip.
    /// </summary>
    public void ShowCard()
    {
        gameObject.SetActive(true);
        if (_flipCoroutine != null) StopCoroutine(_flipCoroutine);
        _flipCoroutine = StartCoroutine(AppearThenFlip());
    }

    /// <summary>
    /// Только флип без анимации появления (карта уже на экране).
    /// </summary>
    public void FlipToFace()
    {
        if (_isFlipped) return;
        if (_flipCoroutine != null) StopCoroutine(_flipCoroutine);
        _flipCoroutine = StartCoroutine(FlipCoroutine());
    }

    /// <summary>
    /// Скрыть карту (вернуть рубашку и убрать с экрана).
    /// </summary>
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

    // ────────────────────────────────────────────────────────────────────
    // Coroutines
    // ────────────────────────────────────────────────────────────────────

    private IEnumerator AppearThenFlip()
    {
        // 1. Появление: scale 0 → 1
        yield return StartCoroutine(ScaleTo(Vector3.one, appearDuration));

        // 2. Небольшая пауза, чтобы игрок успел увидеть рубашку
        yield return new WaitForSeconds(0.2f);

        // 3. Флип
        yield return StartCoroutine(FlipCoroutine());
    }

    private IEnumerator FlipCoroutine()
    {
        float halfDuration = flipDuration * 0.5f;

        // Первая половина: схлопываем по X (рубашка → ноль)
        yield return StartCoroutine(ScaleXTo(0f, halfDuration));

        // В момент "ноль" — меняем сторону
        SetFaceVisible(true);

        // Вторая половина: разворачиваем по X (ноль → 1, лицевая)
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

    // ────────────────────────────────────────────────────────────────────
    // Helpers: плавное масштабирование
    // ────────────────────────────────────────────────────────────────────

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

    // ────────────────────────────────────────────────────────────────────
    // Helpers: контент
    // ────────────────────────────────────────────────────────────────────

    private void FillFaceContent(EventCardData data)
    {
        if (titleText != null) titleText.text = data.cardTitle;
        if (descriptionText != null) descriptionText.text = data.description;

        if (cardFaceImage != null && data.cardFaceSprite != null)
            cardFaceImage.sprite = data.cardFaceSprite;
    }

    private void SetFaceVisible(bool visible)
    {
        if (cardFaceRoot != null) cardFaceRoot.SetActive(visible);
        if (cardBackImage != null) cardBackImage.enabled = !visible;
    }
}
