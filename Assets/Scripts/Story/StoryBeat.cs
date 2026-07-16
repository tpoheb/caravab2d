using System.Collections.Generic;
using UnityEngine;

namespace StorySystem
{
    /// <summary>
    /// Одна нарративная вставка. Создаётся через Assets > Create > 1000Roads > Story Beat.
    /// Хранит условия показа, текст окна и запись для дневника.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStoryBeat", menuName = "1000Roads/Story Beat")]
    public class StoryBeat : ScriptableObject
    {
        [Header("Идентификация")]
        [Tooltip("Уникальный ID этой вставки. Используется для сохранения состояния 'уже показано'")]
        public string beatId;

        [Header("Контент окна")]
        [Tooltip("Заголовок нарративного окна")]
        public string windowTitle;

        [Tooltip("Основной текст нарративной вставки")]
        [TextArea(4, 10)]
        public string windowText;

        [Tooltip("Иллюстрация для окна (опционально)")]
        public Sprite illustration;

        [Header("Дневник")]
        [Tooltip("Краткая запись, которая появится в дневнике после показа окна")]
        [TextArea(2, 4)]
        public string journalEntry;

        [Tooltip("Заголовок записи в дневнике")]
        public string journalTitle;

        [Header("Порядок и приоритет")]
        [Tooltip("Порядковый номер внутри акта. Меньше = раньше (если несколько вставок готовы одновременно)")]
        public int sortOrder;

        [Header("Условия показа (все должны быть выполнены — логика AND)")]
        public List<StoryCondition> conditions = new List<StoryCondition>();

        // ──────────────────────────────────────────────
        // Валидация в редакторе
        // ──────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(beatId))
            {
                beatId = name;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
