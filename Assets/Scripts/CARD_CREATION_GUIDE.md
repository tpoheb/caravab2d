# Гайд по созданию 18 карт событий в Unity
# Тысяча Дорог — Event Cards

## Как создавать карты

### Карты Тени (Мгновенные)
ПКМ в Project → Event System → Card → заполнить поля

### Карты Руки (В РУКУ)
ПКМ в Project → Game → Cards → HandCard → заполнить поля

---

## КАРТЫ ТЕНИ (ShadowCardData)

### 1. Шёпот Прошлого
- cardName: Шепот Прошлого
- effectType: Money
- value: -50
- isTemporary: false
- description: Голоса из Мглы заставляют вас бросать монеты на дорогу, чтобы «откупиться» от призраков.

### 2. Благословенный Оазис
- cardName: Благословенный Оазис
- effectType: AddGoods
- value: 1
- isTemporary: false
- description: Вы находите скрытый источник, где караван может отдохнуть.

### 3. Песчаная Слепота
- cardName: Песчаная Слепота
- effectType: Attack
- value: -2
- isTemporary: true
- duration: 1  ← "до следующего боя" = HandManager/BattleManager сбрасывает после боя
- description: Глаза воинов воспалены от магической пыли.

### 4. Забытая Заначка
- cardName: Забытая Заначка
- effectType: Money
- value: 100
- isTemporary: false
- description: Вы находите кошель, потерянный другим караваном в спешке.

### 5. Зов Мглы
- cardName: Зов Мглы
- effectType: FireCrewMember
- value: 1  ← не используется логикой, но заполни для ясности
- isTemporary: false
- description: Один из ваших людей уходит в туман, ведомый галлюцинациями.

### 6. Дар Незнакомца
- cardName: Дар Незнакомца
- effectType: AddGoods
- value: 1
- isTemporary: false
- description: Встречный путник отдаёт вам лишний тюк, прежде чем исчезнуть в песках.

### 7. Лишние Рты
- cardName: Лишние Рты
- effectType: WagePenalty
- value: 0  ← не используется (флаг)
- isTemporary: false
- applyOnceInCity: true
- description: Группа паломников прибилась к каравану, съедая ваши запасы.

### 8. Благосклонность Звезд
- cardName: Благосклонность Звезд
- effectType: Attack
- value: 5
- isTemporary: true
- duration: 1  ← "на один следующий бой"
- description: Моральный дух команды на высоте, мечи кажутся легче.

### 9. Гнилая Сбруя
- cardName: Гнилая Сбруя
- effectType: Capacity
- value: -5
- isTemporary: true
- duration: 999  ← "до конца пути" = очень долго, или сбрось вручную при достижении цели
- description: Ремни на верблюдах трещат, часть груза приходится перевязать.

### 10. Тень Инквизитора
- cardName: Тень Инквизитора
- effectType: Confiscation
- value: 0  ← основное значение не нужно
- penaltyValue: 200
- isTemporary: false
- description: Если у вас есть Осколки Прошлого, вы теряете их и платите 200 динар штрафа.

### 11. Эхо Халифата
- cardName: Эхо Халифата
- effectType: BonusTrade
- value: 5
- isTemporary: false
- applyOnceInCity: true
- description: Вы находите древний указ, подтверждающий ваши торговые права.

### 12. Обвал на Тропе
- cardName: Обвал на Тропе
- effectType: RemoveGoods
- value: -2  ← или 2, ShadowEffectManager берёт Abs()
- isTemporary: false
- description: Часть поклажи сорвалась в пропасть или утонула в зыбучих песках.

### 13. Лихорадка
- cardName: Лихорадка
- effectType: TeamStats
- value: -50  ← -50% от всех характеристик команды
- isTemporary: true
- duration: 3
- description: Болезнь подкосила всех: воины слабы, погонщики медленны.

---

## КАРТЫ РУКИ (HandCardData)

### 14. Старая Карта
- cardName: Старая Карта
- category: Logistic
- effectType: ChooseDice
- value: 0
- description: Позволяет выбрать любое число на кубике пути (1–6) вместо броска.

### 15. Дымовая Завеса
- cardName: Дымовая Завеса
- category: Tactical
- effectType: EscapeBattle
- value: 0
- description: Позволяет мгновенно завершить бой без штрафов и наград.

### 16. Второй Шанс
- cardName: Второй Шанс
- category: Tactical
- effectType: Reroll
- value: 0
- description: Позволяет перебросить любой кубик (пути или битвы).

### 17. Странный Амулет
- cardName: Странный Амулет
- category: Tactical
- effectType: CancelCard
- value: 0
- description: Позволяет отменить действие любой вытянутой карты Тени или Битвы.

### 18. Мистический Узел
- cardName: Мистический Узел
- category: Economic
- effectType: DoubleGoods
- value: 0
- description: Вы можете удвоить количество одного типа товара в вашем инвентаре.

---

## Важные доработки в других скриптах

### PlayerInventory — нужно добавить методы:
```csharp
public void AddRandomGoods(int amount) { /* выбрать случайный тип, добавить */ }
public void RemoveRandomGoods(int amount) { /* удалить случайные товары */ }
public bool ConfiscateContraband() { /* удалить Осколки Прошлого, вернуть true если были */ }
public void DoubleGoods(GoodsType type) { /* удвоить кол-во товара */ }
```

### PlayerStats — нужно добавить методы:
```csharp
public bool FireRandomCrewMember() { /* уволить случайного, вернуть false если пусто */ }
public void ApplyTeamStatsMultiplier(int percent) { /* умножить все стат на (100+percent)/100 */ }
public void RevertTeamStatsMultiplier(int percent) { /* откатить множитель */ }
```

### GameManager — нужно добавить методы:
```csharp
public void PromptDiceChoice() { /* показать UI выбора числа 1–6 */ }
public void PromptDoubleGoods() { /* показать UI выбора типа товара */ }
```

### BattleManager — нужно добавить:
```csharp
public void AddAttackBonus(int value) { /* добавить бонус к текущему броску */ }
public void ForceEndBattle(bool escaped) { /* завершить бой без результата */ }
```

### CardManager.DrawCard() — добавить проверку CancelCard:
```csharp
public void DrawCard()
{
    // Проверяем амулет
    if (HandManager.Instance != null && HandManager.Instance.ConsumeCancelCard())
    {
        Debug.Log("[CardManager] Карта отменена амулетом.");
        _shuffledDeck.RemoveAt(0); // выбрасываем карту без эффекта
        gameManager.OnCardCancelled();
        return;
    }
    // ... остальная логика
}
```
