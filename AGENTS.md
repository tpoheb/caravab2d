# AGENTS.md

This file provides guidance to agents working in this repository. Technical documentation was synchronized with code on 2026-10-05; always verify behavior against `.cs` files.

## Stack

- Unity 6 (6000.4.0f1), C#. Game "1000 Дорог" uses URP 17.4.0; HDRP 17.4.0 is installed but unused by game code.
- No asmdef in `Assets/Scripts/`: game code compiles into Assembly-CSharp. Dreamteck Splines has separate plugin assemblies.
- Build, run and checks go through Unity Editor (`caravan2d.slnx`). No npm/CLI game scripts.

## Checks

- Unity Test Framework 1.6.0 is installed; there are no project NUnit `[Test]` / `[UnityTest]` suites.
- Real Editor checks exist in [CardSystemChecks.cs](Assets/Editor/Tests/CardSystemChecks.cs). Run **Tools → Проверки → Фазы хода, колода и общий рынок**, or Unity `-batchmode -nographics -projectPath <project> -executeMethod CardSystemChecks.Run -quit -logFile <log>` when no other Editor has the project open.
- Success is logged as `CARD_SYSTEM_CHECKS_PASSED`. Checks cover weighted draws, discard/refill, shared runtime markets, copied snapshots, background planning, sequential AI and a player turn containing city entry plus the next route's first cell.
- These checks are not a Play Mode scene run or a player build. For documentation-only changes, check links and consistency; no Unity run is needed.

## Architecture

- Documentation is under [Assets/MD](Assets/MD/README.md), not `Assets/Scripts/MD`. Files in `Design/` describe design intent; do not assume every rule is implemented.
- [GameManager](Assets/Scripts/GameManager.cs) is the player-turn orchestrator and Singleton. FSM: `Idle`, `InCity`, `Moving`, `DrawingCard`, `ResolvingEvent`, `InBattle`, `EndingTurn`, `WaitingForAI`.
- One personal player turn contains one road-cell step. After AI completes, player entry from the last cell into a city, free city actions and movement onto the next route's first cell belong to one personal turn. `TurnNumber` advances after AI, before player movement.
- `StartTurn()` is private and initializes the first turn. UI uses `OnPathSelected()`, `RequestEndTurn()` and the draw-card button. `CompleteEventPhase()` delegates to normal end-turn handling.
- [DiceSystem.OnDiceRolled](Assets/Scripts/DiceSystem.cs) is an instance `Action<int>` for battle attack only. Dice no longer selects a card type; `DiceEventType.cs` is unused by the current game flow.
- [PlayerToken](Assets/Scripts/PlayerToken.cs) has instance `OnArrivedAtCity` / `OnStepCompleted` events. GameManager and CityManager use `Subscribe()/Unsubscribe()` in `OnEnable()/OnDisable()`; some other systems, including TradeSystem, retain `Awake()/OnDestroy()` subscriptions.
- Dependencies are generally Inspector-injected using `[SerializeField] private`; verify required references in the actual component. ScriptableObjects under `Assets/Data/` provide configuration.

## Cards

- [CardManager](Assets/Scripts/Card/CardManager.cs) owns one CardDeck, one current card and one discard list. Inspector catalogs `allShadowCards` / `allBattleCards` are preserved for scene references and merged at runtime. MapPrototipe contains 25 new events and 20 battles; draws use weights only, without difficulty filtering. Serialized minDifficulty/maxDifficulty fields remain for data compatibility and do not affect draws.
- [CardDeck.Draw](Assets/Scripts/Card/CardDeck.cs) selects by positive weight among eligible remaining cards and removes the result. `HideEventCard()` discards completed/cancelled cards once. Refill from discard happens before a draw only when the entire deck is empty.
- CardDeckUI only displays cards and counts from CardManager. CardDisplay is immediate; there is no awaited flip animation or separate gameplay queue in UI.
- CardDeck uses UnityEngine.Random and belongs on the main thread. The optional `weightSelector` is an extension point; cell/province modifiers are not wired into gameplay yet.
- Interactive preparation for events/battles is agreed but not implemented. Events currently apply on reveal; PrepareBattle initializes a battle and is followed by an automatic roll.
- Hand-card timing and the new duration rule still require user clarification. Do not invent them. Current ProcessTurn ticks at the end of the current personal player turn; a newly gained duration-1 effect can expire that same turn.
- Hand cards are removed from currentHand on use; they do not have their own discard/refill collection.

## AI and economy (`Assets/Scripts/AI/`)

- ITrader is implemented by AITrader and PlayerTraderAdapter; the current AITurnManager queue contains AI traders only. GameManager controls the human separately.
- TurnQueue preserves participant/profile order. Initiative fields remain but do not affect ordering. AI trade and movement execute sequentially; player awaits `AITurnManager.ProcessAITurnAndWait()`.
- AI moves by road cells, enters cities for free before the next planning phase, and does not resolve event/battle cards.
- All AI plan against one copied [GameSnapshot](Assets/Scripts/AI/GameSnapshot.cs) via Task.Run. Planning must not mutate the world or call UnityEngine API. Snapshot City/path references are identifiers, not objects to inspect from worker threads.
- Snapshot prices, stocks and path lists are copied. AiStrategy uses snapshot market data, System.Random and System.Math; queue does not mutate trader state while plans run.
- WorldEconomy and TradeSystem use the same `City.RuntimeData`, initialized by CityManager. Never substitute source CityData assets as mutable markets.
- Shared markets do not imply identical player/AI transaction-cost formulas; these remain separate.

## Gotchas

- PathCellInitializer initializes children tagged `Cell`; PathController caches all direct children and expects a Cell component on each. Keep route children consistent.
- PlayerInventory.LoadInventory uses `Resources.Load("Items/...")`, but item assets live in `Assets/Data/Items/`; loading these saves is still broken.
- BattleCardImporter supports comma CSV and tab TSV with eight positional columns; asset identity is the expected filename including ID and name. Renaming can duplicate an asset.
- ShadowCardCSVImporter reads named columns from `Assets/Data/ShadowCards.csv`; asset filenames also include ID and name.
- TradeDataImporter currently reads comma CSV via `Tools/Trade/Import Trade Data`, updates existing Item.weight, and clears/rebuilds the item list of each city present in the file. Older TSV-only documentation is obsolete.
- BattleCardData.crewLoss / rewardHandCardID exist but are not used by the current BattleManager. CityManager does not currently call ShadowEffectManager.OnEnterCity / ConsumeBonusTrade.

## Code style

- Main game systems use the global namespace; preserve existing explicit namespaces where present.
- Russian comments, logs and Inspector labels. Prefer `_camelCase` private fields, PascalCase public members and `[SerializeField] private` dependencies.
- Prefer static logic taking dependencies as parameters where appropriate (see TradeTransactionHandler). Verify side effects and Unity dependencies before calling a helper from background planning.
