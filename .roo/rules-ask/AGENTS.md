# Project Documentation Context (non-obvious)

- Canonical project docs live in `Assets/Scripts/MD/` (README, ARCHITECTURE, SYSTEMS, SCRIPTS_AND_IMPORTERS, CARD_CREATION_GUIDE, ShadowCards_Mechanics, REFACTORING). They predate the event/FSM refactor — treat method/event names there as historical, not authoritative.
- `Assets/Scripts/Archive/` holds legacy/removed scripts (old `Cell` enum, old `City1..City5.asset`) — do not cite them as current behavior.
- Content data locations: `Assets/Data/Items` (18 items), `Assets/Data/Team` (units + `New Player Stats.asset`), `Assets/Data/Story`, `Assets/Data/ShadowEvent`, `Assets/Data/TraderProfile.asset`. The game has 6 cities and 8 battle cards.
- Gameplay scenes: `Assets/Scenes/Map.unity` and `Assets/Scenes/MapPrototipe.unity`; `SampleScene`, `Hex`, `Hex 1` are scratch/experimental.
- Some `.asset` files use Cyrillic names (`Assets/Archive/алмазы.asset`, `вино.asset`) — keep an eye on encoding when scripting paths.
