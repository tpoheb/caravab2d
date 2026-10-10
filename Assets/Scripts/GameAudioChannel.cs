using UnityEngine;

/// <summary>Добавьте на музыкальный AudioSource для отдельного регулятора музыки.</summary>
[RequireComponent(typeof(AudioSource))]
public sealed class GameAudioChannel : MonoBehaviour
{
    [SerializeField] private bool _isMusic = true;
    private AudioSource _source;
    private float _baseVolume;
    private void Awake() { _source = GetComponent<AudioSource>(); _baseVolume = _source.volume; }
    private void OnEnable() { GameSettings.Changed += Apply; Apply(); }
    private void OnDisable() => GameSettings.Changed -= Apply;
    private void Apply() => _source.volume = _baseVolume * (_isMusic ? GameSettings.Current?.music ?? 0.7f : 1f);
}
