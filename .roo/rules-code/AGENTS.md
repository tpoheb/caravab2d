# Project Coding Rules (non-obvious)

- No namespaces — all game classes live in the global namespace; check for name collisions across folders before adding new types.
- Inject dependencies via `[SerializeField] private` and call `ValidateReferences()` in `Awake()`; log an error for every missing reference (existing convention).
- Put pure logic in `static` classes that take all dependencies as parameters — see [`TradeTransactionHandler`](Assets/Scripts/Trade/TradeTransactionHandler.cs:1). No MonoBehaviour side effects there.
- ScriptableObjects are the data layer, but `PlayerStats` is mutated at runtime: call `playerStats.Initialize()` (or `Instantiate` a copy) before modifying; never assume `.asset` values survive Play Mode.
- Editor importers use different formats: comma CSV (`BattleCardImporter`) vs TAB-separated TSV (`TradeDataImporter`). Asset dedup: by filename (battle cards) vs by in-memory field (trade).
- Logging convention: Russian text with `[ClassName]` prefix, e.g. `[WorldEconomy]`, `[AITurnManager]`.
