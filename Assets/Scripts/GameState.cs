public enum GameState
{
    Idle,               // игра еще не началась
    InCity,             // игрок в городе, выбирает путь
    Moving,             // токен делает шаг
    RollingDice,        // бросок кубика
    ResolvingEvent,     // бой / карта / событие
    TurnComplete,        // ход завершён
    InBattle           // битва

}