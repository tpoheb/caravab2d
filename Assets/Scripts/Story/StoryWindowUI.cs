using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StorySystem
{
    /// <summary>
    /// UI-компонент нарративного окна.
    /// Подключи этот скрипт к Panel-объекту в Canvas.
    /// Назначь все поля через Inspector.
    ///
    /// Иерархия объектов в Canvas:
    ///   Canvas
    ///     └── StoryWindow (Panel) ← этот компонент здесь
    ///           ├── Illustration (Image)
    ///           ├── Title (TextMeshProUGUI)
    ///           ├── BodyText (TextMeshProUGUI)
    ///           └── CloseButton (Button)
    /// </summary>
    public class StoryWindowUI : MonoBehaviour
    {
        [Header("UI-элементы")]
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private Image illustrationImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Button closeButton;

        [Header("Опции")]
        [Tooltip("Блокировать клик вне окна (рекомендуется для нарративных вставок)")]
        [SerializeField] private bool blockInput = true;
        [SerializeField] private GameObject inputBlocker;  // полупрозрачный Panel на весь экран

        // Коллбэк вызывается после закрытия окна
        private Action _onClosed;

        public bool IsVisible => windowRoot != null && windowRoot.activeSelf;

        // ──────────────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────────────

        private void Awake()
        {
            closeButton.onClick.AddListener(Close);
            Hide();
        }

        // ──────────────────────────────────────────────
        // Публичный API
        // ──────────────────────────────────────────────

        /// <summary>
        /// Открывает нарративное окно и заполняет его данными из StoryBeat.
        /// </summary>
        public void Show(StoryBeat beat, Action onClosed = null)
        {
            _onClosed = onClosed;

            titleText.text = beat.windowTitle;
            bodyText.text  = beat.windowText;

            if (beat.illustration != null)
            {
                illustrationImage.sprite  = beat.illustration;
                illustrationImage.enabled = true;
            }
            else
            {
                illustrationImage.enabled = false;
            }

            if (blockInput && inputBlocker != null)
                inputBlocker.SetActive(true);

            windowRoot.SetActive(true);
        }

        /// <summary>
        /// Закрывает окно и вызывает коллбэк.
        /// </summary>
        public void Close()
        {
            Hide();
            _onClosed?.Invoke();
            _onClosed = null;
        }

        // ──────────────────────────────────────────────
        // Приватные методы
        // ──────────────────────────────────────────────

        private void Hide()
        {
            windowRoot.SetActive(false);

            if (inputBlocker != null)
                inputBlocker.SetActive(false);
        }
    }
}
