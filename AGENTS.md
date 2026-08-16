# AGENTS.md

This file provides guidance to agents when working with code in this repository.

## Stack
- Unity 6 LTS (6000.4.0f1), C#. Game "1000 Дорог" runs on URP 17.4; HDRP 17.4 is also in the manifest but unused by game code.
- No asmdef in `Assets/Scripts/` — all game code compiles into the default Assembly-CSharp. Only `Assets/Plugins/Dreamteck/` (Splines v3.0.6) has asmdefs.
- No npm/CLI scripts. Build, run, test go through the Unity Editor (solution `caravan2d.slnx`, Unity 6 `.slnx` format).

## Tests
- `com.unity.test-framework` is installed, but the project has ZERO tests (no `[Test]`/`[UnityTest]`/NUnit in `Assets/`). There is no test convention to follow — tests must be created from scratch.

## Architecture (verify against code, NOT the docs)
- `Assets/Scripts/MD/*.md` describe an OLDER design (`static event`, `StartNewTurn()`, `DiceSystem.OnDiceEvent`). Current code uses **instance** events — [`DiceSystem.OnDiceRolled`](Assets/Scripts/DiceSystem.cs:15), [`PlayerToken.OnArrivedAtCity`](Assets/Scripts/PlayerToken.cs:8) — and a [`GameManager`](Assets/Scripts/GameManager.cs:4) FSM via [`GameState`](Assets/Scripts/GameState.cs:1) (Idle/InCity/Moving/DrawingCard/ResolvingEvent/InBattle). Method names changed (`StartTurn()`, `RequestEndTurn()`). Always trust the `.cs` files.
- Systems communicate through events with `Subscribe()/Unsubscribe()` in `OnEnable()/OnDisable()`; `GameManager` (Singleton, `Instance`) is the single orchestrator of turn phases.
- Dependencies are injected via `[SerializeField] private` and checked by a `ValidateReferences()` call in `Awake()`. ScriptableObjects under `Assets/Data/` are the data layer (Item, CityData, UnitData, PlayerStats, BattleCardData, ShadowCardData).

## AI layer (`Assets/Scripts/AI/`)
- [`ITrader`](Assets/Scripts/AI/ITrader.cs:9) unifies player and AI; `TurnQueue` drives turns by `Initiative`; each turn uses one immutable [`GameSnapshot`](Assets/Scripts/AI/GameSnapshot.cs:8) for fair planning.
- `AITrader.PlanTurn()` is invoked from `Task.Run()` (background thread) → planning/strategy code must be pure: no world mutation, no `UnityEngine` API calls. `AITurnManager.ProcessAITurn()` is called from `GameManager.RequestEndTurn()` after the player turn.

## Gotchas
- Route steps: `PathCellInitializer` only counts child GameObjects tagged `"Cell"`; un-tagged children are silently ignored.
- `PlayerInventory.LoadInventory()` uses `Resources.Load("Items/...")` but items live in `Assets/Data/Items/` (not `Resources/`) → save/load is broken.
- Editor importers differ: `BattleCardImporter` (Tools → Battle Cards) reads COMMA CSV and dedupes assets by **filename** (renaming creates a duplicate); `TradeDataImporter` (Trade → Import Trade Data) reads TAB-separated TSV (dialog filters "csv") and sets `Item.weight` only on first creation.

## Code style
- No namespaces (global namespace). Russian comments, `Debug.Log` messages, and `[Header]`/`[Tooltip]` text.
- `_camelCase` private fields, PascalCase public members; `[SerializeField] private` for Inspector DI; pure logic in `static` classes taking dependencies as parameters (see [`TradeTransactionHandler`](Assets/Scripts/Trade/TradeTransactionHandler.cs:1)).
