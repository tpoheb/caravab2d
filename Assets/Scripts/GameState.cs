public enum GameState
{
    Idle,
    InCity,
    Moving,
    DrawingCard,      // ← новое: ожидаем нажатия кнопки "Вытянуть карту"
    ResolvingEvent,
    InBattle,
}