# Project Architecture Rules (non-obvious)

- Single orchestrator: [`GameManager`](Assets/Scripts/GameManager.cs:4) (Singleton) owns the `GameState` FSM. Systems must NOT advance turn/event state themselves — they emit events and let GameManager react.
- AI turn pipeline is snapshot-based: build one immutable [`GameSnapshot`](Assets/Scripts/AI/GameSnapshot.cs:8) → `PlanTurn()` (pure, background thread) → `TurnIntent` → `TurnQueue` executes by `Initiative`. AI must not read live world state mid-turn.
- [`ITrader`](Assets/Scripts/AI/ITrader.cs:9) is the only contract between player/AI and `TurnQueue`; any new participant must implement it.
- ScriptableObject-as-data: gameplay config belongs in `.asset` files (under `Assets/Data/`), not scene-serialized fields; Inspector DI (`[SerializeField]`) wires runtime systems together.
- Single-scene assumption: `GameManager`/`AITurnManager` are not `DontDestroyOnLoad`; there is no scene-change flow in current code.
- [`WorldEconomy`](Assets/Scripts/AI/WorldEconomy.cs:11) re-implements price volatility for AI trades instead of using `TradeTransactionHandler` — keep both formulas in sync when changing economy math.
