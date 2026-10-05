public enum GameState
{
    Idle,
    InCity,
    Moving,
    DrawingCard,      // ← новое: ожидаем нажатия кнопки "Вытянуть карту"
    ResolvingEvent,
    InBattle,
    EndingTurn,      // Результат уже зафиксирован; повторные действия запрещены.
    WaitingForAI,    // Игрок ждёт завершения последовательных ходов ИИ.
}