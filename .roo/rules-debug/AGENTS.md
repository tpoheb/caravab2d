# Project Debug Rules (non-obvious)

- MD docs in `Assets/Scripts/MD/` are outdated (they document `static event` and old method names). The live code uses instance events + the `GameState` FSM — read `.cs` files, not the docs, when tracing bugs.
- AI planning (`AiStrategy`, `AITrader.PlanTurn()`) runs on a `Task.Run()` background thread: `Debug.Log` from that thread may not surface in the Console. Use [`AIDebugLog`](Assets/Scripts/AI/AIDebugLog.cs:1) / [`AIDebugOverlay`](Assets/Scripts/AI/AIDebugOverlay.cs:1) / [`AIDebugger`](Assets/Scripts/AI/AIDebugger.cs:1) for AI tracing.
- A missing `gameManager`/`battleManager`/`cardManager` Inspector reference can strand the turn FSM in an event state (null-safe dispatch swallows the error). Check Inspector wiring first for "stuck turn" bugs.
- PlayerPrefs inventory save/load is silently broken (items are not under `Resources/`); do not rely on it when reproducing state.
- Unity Scene objects required by routes: children must carry the `"Cell"` tag or they are dropped from the path silently.
