using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SettingsMenu
{
    public GameObject Root { get; }
    private readonly MenuUIBuilder _ui;
    private GameSettingsData _draft;
    private readonly Slider _sound, _music;
    private readonly TMP_Text _soundValue, _musicValue, _fullscreenText, _resolutionText, _qualityText;
    private readonly Button _apply;
    private readonly Vector2Int[] _resolutions;
    private Action _closed;
    private int _resolutionIndex;

    public SettingsMenu(Transform parent, MenuUIBuilder ui)
    {
        _ui = ui;
        var rect = ui.Panel("Настройки", parent, Vector2.zero, new Vector2(1000, 760)); Root = rect.gameObject;
        ui.Label(rect, "НАСТРОЙКИ ПУТЕШЕСТВИЯ", new Vector2(0, 295), new Vector2(880, 60), 38, gold: true);
        ui.Label(rect, "ЗВУК", new Vector2(-380, 220), new Vector2(120, 38), 20, TextAlignmentOptions.Left, true);
        RowLabel("Общая громкость", 158);
        _soundValue = ui.Label(rect, "100%", new Vector2(400, 158), new Vector2(100, 50), 22);
        _sound = ui.Slider(rect, "Общая громкость", new Vector2(150, 158), 1,
            value => { _draft.sound = value; _soundValue.text = Mathf.RoundToInt(value * 100) + "%"; });
        RowLabel("Музыка", 90);
        _musicValue = ui.Label(rect, "70%", new Vector2(400, 90), new Vector2(100, 50), 22);
        _music = ui.Slider(rect, "Музыка", new Vector2(150, 90), .7f,
            value => { _draft.music = value; _musicValue.text = Mathf.RoundToInt(value * 100) + "%"; });
        ui.Label(rect, "ЭКРАН И ГРАФИКА", new Vector2(-270, 10), new Vector2(340, 38), 20, TextAlignmentOptions.Left, true);
        RowLabel("Режим экрана", -55);
        _fullscreenText = Choice("Полный экран", -55,
            direction => { _draft.fullscreen = !_draft.fullscreen; RefreshChoices(); });
        _resolutions = Screen.resolutions.Select(value => new Vector2Int(value.width, value.height))
            .Concat(new[] { new Vector2Int(Screen.width, Screen.height), new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
            .Where(value => value.x >= 640 && value.y >= 480).Distinct().OrderBy(value => value.x).ThenBy(value => value.y).ToArray();
        RowLabel("Разрешение", -125);
        _resolutionText = Choice("Разрешение", -125,
            direction => { _resolutionIndex = (_resolutionIndex + direction + _resolutions.Length) % _resolutions.Length; SelectResolution(); RefreshChoices(); });
        RowLabel("Качество графики", -195);
        _qualityText = Choice("Качество", -195,
            direction => { _draft.quality = (_draft.quality + direction + QualitySettings.names.Length) % QualitySettings.names.Length; RefreshChoices(); });
        _apply = ui.Button(rect, "Применить", new Vector2(235, -290), new Vector2(390, 62),
            () => { GameSettings.Apply(_draft); Close(); });
        ui.Button(rect, "Назад", new Vector2(-235, -290), new Vector2(390, 62), Close);
        ui.Label(rect, "Стрелки — выбор параметра. Esc — назад.", new Vector2(0, -345), new Vector2(880, 35), 18, gold: true);
        Root.SetActive(false);
    }
    private void RowLabel(string text, float y) => _ui.Label(Root.transform, text,
        new Vector2(-235, y), new Vector2(420, 50), 26, TextAlignmentOptions.Left);
    private TMP_Text Choice(string name, float y, Action<int> change)
    {
        var previous = _ui.Button(Root.transform, "‹", new Vector2(32, y), new Vector2(54, 55), () => change(-1));
        previous.name = name + " — назад";
        var value = _ui.Button(Root.transform, name, new Vector2(225, y), new Vector2(308, 55), () => change(1));
        var next = _ui.Button(Root.transform, "›", new Vector2(418, y), new Vector2(54, 55), () => change(1));
        next.name = name + " — вперёд";
        return value.GetComponentInChildren<TMP_Text>();
    }
    public void Open(Action closed)
    {
        _closed = closed; _draft = GameSettings.Copy();
        _sound.SetValueWithoutNotify(_draft.sound); _music.SetValueWithoutNotify(_draft.music);
        _soundValue.text = Mathf.RoundToInt(_draft.sound * 100) + "%";
        _musicValue.text = Mathf.RoundToInt(_draft.music * 100) + "%";
        _resolutionIndex = Array.FindIndex(_resolutions, value => value.x == _draft.width && value.y == _draft.height);
        if (_resolutionIndex < 0) _resolutionIndex = 0;
        RefreshChoices(); Root.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(_apply.gameObject);
    }
    public void Close() { Root.SetActive(false); var closed = _closed; _closed = null; closed?.Invoke(); }
    private void SelectResolution() { _draft.width = _resolutions[_resolutionIndex].x; _draft.height = _resolutions[_resolutionIndex].y; }
    private void RefreshChoices()
    {
        _fullscreenText.text = _draft.fullscreen ? "Полный экран" : "В окне";
        _resolutionText.text = $"{_draft.width} × {_draft.height}";
        string[] translated = { "Минимальное", "Низкое", "Среднее", "Высокое", "Очень высокое", "Максимальное" };
        _qualityText.text = _draft.quality < translated.Length ? translated[_draft.quality] : QualitySettings.names[_draft.quality];
    }
}
