using UnityEngine;

/// <summary>
/// Профиль ИИ-торговца — ScriptableObject.
/// Создаётся через ПКМ → Create → 1000Roads → TraderProfile.
/// Разные персонажи задаются данными, без изменения кода.
/// </summary>
[CreateAssetMenu(
    fileName = "TraderProfile",
    menuName  = "1000Roads/TraderProfile")]
public class TraderProfile : ScriptableObject
{
    [Header("Идентификация")]
    public string displayName = "Unnamed Trader";

    [Header("Характеристики")]
    [Tooltip("Порядок хода: выше = действует первым при конфликте путей.")]
    public int initiative = 5;

    [Tooltip("Стартовое золото.")]
    public int startGold = 100;

    [Header("Параметры ИИ")]
    [Tooltip("Насколько ИИ ценит маржу. 1 = норма, 2 = жадный.")]
    [Range(0.1f, 3f)]
    public float greedWeight = 1f;

    [Tooltip("Насколько ИИ мешает лидеру в ущерб своей выгоде.")]
    [Range(0f, 1f)]
    public float aggressionWeight = 0.3f;

    [Tooltip("Минимальный score ниже которого ИИ не двигается.")]
    public float minProfitThreshold = 5f;

    [Header("Визуал")]
    [Tooltip("Префаб токена ИИ на карте.")]
    public GameObject tokenPrefab;
}
