# Switchboard Operator: Design

This document is the source of truth for how the game works and how the code is organized. Implementing agents follow it. If the code or a step brief disagrees with it, they stop and report instead of improvising. Changes are agreed with the project lead first, then written here.

Last updated 2026-10-18 · Plan: [Plan.md](Plan.md) · Progress: [Progress.md](Progress.md)

## Implementation status

Level art pass (2026-10-18, lead requested): `BackgroundPanel.prefab` groups `main_background_0` with the existing 6×6 switchboard. Its twelve lamps indicate hovered/occupied rows and columns using supplied on/off sprites. This is presentation only; call rules and socket addresses are unchanged. Six caller anchors remain despite the five windows painted into the new background; their layout migration remains pending.

| Step | State | What exists |
|---|---|---|
| 1 | Done (`e6f44e5`) | One call at a time, through Incoming → Taken → Connected → Ended. See below. |
| 1b + 1c | Done (`60c7a88`, `47735c1`) | Connection fee (D32); one people file `PhoneBook.json`, read by Sergei's book and by us (D31); the money counter in the header strip of Sergei's shop panel; a speech bubble left of the caller card; a "+N¢" popup; the temporary Directory list removed. `GameConfig` references the JSON; a plain `GameContent` class implements `IGameContent`. |
| 2a | Done (`2b00ac7`) | Up to 6 live calls, one card per caller slot. Taking a card swaps (D03). Connecting while a cord is half-patched. `BusyTracker` (D18), `CallbackScheduler` with "Calling again" cards, and a `PacingProfile`-driven spawner. Connected cards stay in their slot with a cord-colour ring and their own earnings. A dev panel (F1). |
| 2b | Done (`4c5ac50`) | Hold and resume (D07): a pulled plug puts the call OnHold (pay and duration paused, card says "On hold" with frozen earnings); the same pair closing again resumes it with no second fee; the plug in another jack or the cord to the tray cuts it off. Ringing halos draw behind all card content. |
| 3 | Done (2026-10-18) | A–Z tabs in Sergei's book (D34): `PhoneBookTab` plus `PhoneBookView.ShowLetter`/`HasLetter`, with letters that have no surname greyed out, and arrow keys while the book is open. 36 people on every jack A1–F6 (D35). Restyled cards: bold surnames in the in-hand bubble, on-hold dimming, coloured stamp inks. `ContentValidator` (Content) with `Tools/Switchboard/Validate Content`. Accepted deviations: the tab test lives in `Assets/Tests/Game/Editor/`, the validator menu in `Assets/Scripts/Game/Editor/` (see 4.1), and the validator also checks the surname order and `contactsPerPage`. After the lead's playtest: a press reaction and a placeholder click when a card is taken, typed-out bubble text, and clearer tabs for the spread on show. |
| 4 | Done (2026-10-18) | Emergencies and fines (D36, D37): `type.emergency` (30 s response ring, $2 miss or wrong-patch fine, $1 cut-off fine, 20–40¢ + 5–10¢/s for 10–20 s, at most one at a time, spawn weight 1 against Regular 5, `everyoneCalls`, `recipientTag` `emergency-service`). Police B6, Fire Brigade C2 and Doctor D4 head `PhoneBook.json`. `PenaltyService` with `FineApplied`, the "Missed -$X" stamp and a red fine popup under the money counter. `IdleGuard` (60 s, `PacingProfile.idleTimeoutSeconds`) blocks fined types and their callbacks. A presentation-only `CallCardStyle` per type (badge, accent). Dev panel "Emergency now". Accepted deviations: the countdown ring is cream on a dark track, turning orange-red in the last 10 s (a red ring was invisible on the red halo); the type badge hides once connected; a `type` given on a service is ignored. |
| Scene merge | Done (2026-10-18) | One `Final.unity` built from prefabs (D38): our `Game.prefab` and `GameUI.prefab` next to Sergei's board; build list `MainMenu` (0), `Final` (1); `Game.unity` removed; `GameInstaller` finds the views by type in its own scene. |
| 5a | Done (2026-10-18) | The designer's `Assets/Configs/Economy.json` (`upgrades` only) and `Docs/EconomyFile.md`. Domain `UpgradeService` (with `PurchaseCheck`), `StatSheet`, `ShopShelf`, `GoalProgress`, `Wallet.TrySpend`; the live income multiplier on fee and pay. `EconomyFileLoader` with plain-English checks, shown by `Validate Content`. Sergei's 4 shop buttons (coordinated, D44) carry `UpgradeSlotView` (title, description, price, savings fill, gold frame) and are 204 px wide; menu `Tools/Switchboard/Update Upgrade Shop Buttons`. Our `ShopView`, a neutral "-$X" spend popup and the "To be continued..." dialog (`GoalPanelView`). Dev panel "+ $100" and "Reset upgrades" (no refund). Accepted deviations: see 5.9 and 4.3. |
| 5b | Done (2026-10-18) | `extra_wires` and `notepad` in `Economy.json` (D23, D40, D43). `StatSheet.Cords` (from `GameConfig.startingCords` = 3), `HasNotepad` and `Changed`; Domain `Notepad` (notes on the `Call`: `CallerNote`, `RecipientNote`). Coordinated changes to Sergei's code (D44): `CableInputController.AddCable(Color)` and `MaxCables` = 6, with tray re-spacing; `PhoneBookView.ContactTapped(name, socket)` and `ContactTapsEnabled`, with a transparent row tap area in `PhoneBook.prefab`. `CordBridge.EnsureCords` adds green, violet and ivory cords (`GameConfig.extraCordColors`); `NotepadBookLink` routes book taps; the bubble shows noted jacks. Accepted deviations: see 5.9. |
| 5c | Done (2026-10-18) | `Economy.json` holds `startingCents`, `startingCords`, `pacing` and `calls` (D47), read by `EconomyFileLoader` into `CallTypeRecord`s and `PacingSettings`; the domain is unchanged. `CallerTypeDefinition` is card look only; `PacingProfile` and `Default.asset` are gone. Values unchanged (a real-file test pins them). Accepted deviations: a negative `startingCents` and half-written ranges are also reported; `startingCords` above 6 falls back to 3; `Validate Content` prefixes people messages with "People file: ". |
| Upstream merge | Done (2026-10-18, `a173e75`) | Sergei's `2fc3810`: persistent wiring (D45) in `OperatorDesk`/`CallService` with `PersistentWiringTests`; "New order!" stamp and `StampExitAnimation`; bubble only on the last-clicked card; `CallAudio` and `BackgroundMusic`; board gold light; `CordBridge.Synchronize` and insertion anchors. His `CableInputController` keeps a held cord when a call card is clicked (it now references our `CallCardView`). Mia: new art, including `caller-04`/`caller-05` (slices fixed to 190×190, lead-approved). |

What Step 1 built:
- **Operator desk.** A right pair connects the call. A wrong pair ends it as "Wrong number!". Patching with no call in hand is refused. Pulling a plug cut the call off (replaced by hold in Step 2b).
- **`CordBridge`** connects the game to Sergei's cords.
- **Wallet and earnings.** Money is paid live, with a carried fraction so nothing is lost to rounding.
- **Content.** 12 people on 12 jacks, two per row (36 from Step 3, D35).
- **UI.**
  - A ringing card over the first caller slot.
  - A ticket, the money counter and a Directory button in the right-hand column.
  - A temporary directory list.

## 1. The game

A casual simulator. The player is a telephone switchboard operator in the era of manual switchboards, plugging cords into a board to connect callers. There is no way to lose. The player earns money, buys upgrades and saves up for a final **mystery upgrade**. Progress is saved between browser sessions.

### Core loop
1. An incoming call appears as a **call card**.
2. The player takes the call. The caller says who they are and who they want.
3. The player looks both people up in the **directory book** to find their jacks.
4. The player connects the two jacks with a **cord**.
5. **Right connection:** the caller pays **per second** while connected, and the money counter rises live. When the call ends, the cord stays plugged until the player moves it (D45).
6. **Wrong connection:** the caller hangs up at once and pays nothing. Later they **call back** wanting the same person. The cord stays plugged (D45).
7. The player can **disconnect** a call at any moment by unplugging it. Payment stops at that moment.
8. The player spends money on upgrades and saves for the mystery upgrade.

### Rules
- **Cards.** Several can wait at once. They can't be skipped or dismissed. Regular calls wait as long as needed. Regular callers only hang up after a wrong connection.
- **Earnings** = (one-off connection fee + seconds connected × caller's rate per second) × earnings multiplier (D32).
  - The multiplier starts at 1.
  - Several calls pay at the same time.
  - No money is lost to rounding.
- **What the player sees.** The card shows the caller's rate. The call's duration is hidden.
- **Cords** are limited, and each connected call uses one. Cords may cross; crossing is purely visual.
- **Caller types** (Regular and Emergency now; VIP, long distance and others later) each set:
  - a rate range
  - a duration range
  - a callback delay
  - where relevant, a response time and fines
- **Emergency fines.** The player is fined for:
  - not connecting an emergency call correctly within its response time (a **missed** call)
  - connecting it wrong, which is fined the same amount as a missed call (the caller still calls back later)
  - disconnecting it before it finishes

  Fines and response times are set per caller type.
- **Money never goes below zero.**
- **The directory book** can be opened at any time.
- **Upgrades**, bought with money. Every upgrade is a one-time entry in the designer's `Assets/Configs/Economy.json` (D39, D40). Entries of the same type form a chain, bought in file order:
  - Extra income: sets the earnings multiplier (e.g. ×1.5, then ×2, then ×3).
  - Extra wires: sets the number of cords [5b].
  - Notepad: tapping a book entry notes its jack on every accepted call that person is part of (D23, D46) [5b].
  - Extra clients: sets the board size, so more jacks and more people [7].
  - The secret (mystery) upgrade: one entry, the most expensive, with an effect still to be decided (D15).

  New upgrades of an existing type are added to the file without code.
- **Goal UI.** The secret upgrade's shop button doubles as the savings bar (D41).

### Platform
- Unity 6000.3.6f1, URP with the 2D Renderer, 2D.
- Web build for desktop and mobile browsers.
- Mouse and touch through the Input System.
- Constraints:
  - a single thread
  - limited memory
  - saves stored in the browser
  - a UI that adapts to screen sizes and orientations

## 2. Principles
- **Rules live in a plain C# domain layer with no `UnityEngine`.** One tick advances it in a fixed order, so the rules are deterministic, unit-testable and can run headless for balancing.
- **ScriptableObjects hold data. Plain C# objects hold state.** Nothing writes to a ScriptableObject at runtime.
- **One composition root, no singletons.**
- **Views are thin.** They send player actions down, and they either listen to events or read values.
- **Behaviour comes from data.** Never write `if (type == Emergency)`. Use the type's fields: response time, fines, flags.
- **Build alongside the team, not over it.** New systems connect to the existing cord code through its public members. They don't edit it.

## 3. Team and ownership

| Area | Owner | Rule for agents |
|---|---|---|
| Cord and board code:<br>`Assets/Scripts/UI/`, `Assets/Scripts/Cables/`, `Assets/Editor/`, `Assets/Tests/Editor/` | Sergei | **Read-only.** Use only the public members listed below. Any change is a coordinated change, agreed with the project lead first. |
| The existing prefabs in `Assets/Prefabs/` and `Assets/Prefabs/UI/`, and the loose objects in `Final.unity` (`SwitchboardCanvas`, `EventSystem`) | Sergei | **Read-only.** `Final.unity` itself is shared (D38, see 4.2). |
| `Assets/Sprites/`, `Assets/Audio/` | Mia (art) and Sergei | **Read-only.** Our assets may reference these files. |
| Sergei's UI components (built after Step 1): phone book (`PhoneBookView`, `Configs/PhoneBook.json`), upgrade shop panel, `MainMenu.unity` and `Assets/Tools/rodrigo_menu_tools`, volume panel | Sergei | **Read-only.** We wire them to our rules (D30). A hook we need in his code is a coordinated change. |
| Everything listed in section 4 as ours | The project lead, through agents | Agents create and change these files. |

**Public members we rely on.** If any of these change, `CordBridge` breaks, so tell the team before relying on more.
- `CableController`:
  - `Phase`, `FirstSocket`, `SecondSocket`, `IsInTray`, `CableColor`
  - `TryRemove(SocketView)`, `PlayBlockedFeedback()`
  - `InsertionRevision`, `LastInsertionAnchor`, `LastInsertionWasManual` (Sergei, `2fc3810`; read by `CordBridge` for the wrong-number anchor, D45)
- `CableInputController` (5b, coordinated): `AddCable(Color)`, `MaxCables`.
- `PhoneBookView` (5b, coordinated): `ContactTapped(name, socket)`, `ContactTapsEnabled`.
- `CablePhase` values: `Ready` (in the tray), `Held` (picked up, nothing plugged in), `OneEndConnected`, `Connected`.
- `SocketView`: `Address` (e.g. `"C4"`), `Row`, `Column`, `PlugAnchor`, `IsOccupied`.
- The cord input model: click a cord in the tray to pick it up, then click a jack to plug in. Clicking a plugged jack pulls that plug out. If the player pulls one plug of a connected cord and then clicks empty space, the plug snaps back to its jack ("restore", `ReconnectSocket`).
- Sergei's objects in `Final.unity` are found **by component type, never by name**: `CableController`, `SocketView`, `CableInputController` and, from Step 5a, the shop's `UpgradeSlotView`s (D41, D44).
- Two exceptions are found by name, because they have no component of their own. Ask Sergei to keep these names:
  - The caller slots: the children of the `VerticalLayoutGroup` under `CallerQueuePanel`.
  - The money slot: `UpgradeShopPanel/HeaderSlot`, a plaque added in Step 1c with Sergei's panel approved as a coordinated change.

## 4. Architecture

### 4.1 Layers, assemblies and folders

| Layer | Folder | Assembly | Contents |
|---|---|---|---|
| Domain | `Assets/Scripts/Domain/` | `Switchboard.Domain`, `noEngineReferences: true` | All rules: calls, spawning, callbacks, the operator desk, the directory, the economy, penalties, stats, upgrades, progression, and the save data model. |
| Content | `Assets/Scripts/Content/` | `Switchboard.Content`, references Domain | ScriptableObject definitions. They implement small read-only Domain interfaces. |
| Content editor tools | `Assets/Scripts/Game/Editor/` | **no asmdef** (Assembly-CSharp-Editor) | The `Tools/Switchboard/Validate Content` menu (3). The validator itself, `ContentValidator`, is in `Switchboard.Content`. |
| Game | `Assets/Scripts/Game/` | **no asmdef** (Assembly-CSharp) | Composition root, game loop, `CordBridge`, views, input glue, audio, storage, dev tools. |
| Tests | `Assets/Tests/Domain/` | `Switchboard.Domain.Tests`, EditMode, references Domain and Content | Unit tests for Domain, plus content validation. |
| Game tests | `Assets/Tests/Game/Editor/` | **no asmdef** (Assembly-CSharp-Editor) | EditMode tests that need Assembly-CSharp types, such as Sergei's `PhoneBookView` (3). |
| Content assets | `Assets/Content/` | n/a | `.asset` files, plus our audio in `Assets/Content/Audio/` (3). |
| Our prefabs | `Assets/Prefabs/Game/` | n/a | `GameUI.prefab` and our other prefabs. |
| Scenes | `Assets/Scenes/Final.unity`, shared (D38); `MainMenu.unity` is Sergei's | n/a | Our parts of `Final` are the `Game.prefab` and `GameUI.prefab` instances. |
| Team code (unchanged) | `Assets/Scripts/UI/`, `Assets/Scripts/Cables/Core/` | Assembly-CSharp, `Switchboard.CableLogic` | Cord interaction and graphics. |

- **Why Game has no asmdef:** an asmdef assembly can't reference Assembly-CSharp, and the team's cord code lives there. Game has to see both our assemblies and the cord code.
- **Dependency direction:** Domain ← Content ← Game. Domain references nothing, so it can't use `UnityEngine.Random`, `Time` or `Debug`.
  - Time comes from `Tick(dt)`.
  - Randomness comes from `IRandom`.
  - Logging, if needed, goes through an interface.
- **Namespaces** match the assemblies: `Switchboard.Domain`, `Switchboard.Content`, `Switchboard.Game`, `Switchboard.Domain.Tests`.

### 4.2 Scenes
- **`MainMenu.unity` (Sergei's; build index 0).** Start loads the next build index.
- **`Final.unity` (shared; build index 1; D38, formerly `Level.unity`, same GUID).** The one game scene, made of prefab instances:
  - Sergei's: `Main Camera` (prefab), `SwitchboardCanvas` (loose; holds the BackgroundPanel, CallerQueuePanel, CableTray, UpgradeShopPanel, PhoneBook and Cable prefabs) and `EventSystem` (loose). He may turn the loose ones into prefabs.
  - Ours: `Game.prefab` (`GameInstaller`, `GameLoopRunner`, `CordBridge`) and `GameUI.prefab`, at the root, with no overrides.
  - **Rule: the scene is shared, the prefabs are owned.** Each owner edits only their own prefabs. Adding or removing a root object is announced to the team first. No serialized links between owners' prefabs: `GameInstaller` finds the views and Sergei's objects by type in its own scene at start.
  - `Game.unity` and the additive loading were removed (D38).
- **`Boot.unity` (Step 6, then build index 0).** A tap-to-start screen, which unlocks browser audio, then loading.
- **`SampleScene.unity`** is not in the build list.
- **The game has one screen,** in one scene (D38).

### 4.3 UI layering
- Our UI lives in `Assets/Prefabs/Game/GameUI.prefab`: a Screen Space Overlay canvas with sorting order 10. Its CanvasScaler matches the team's `SwitchboardCanvas` (Scale With Screen Size, 1920×1080, Expand), so positions line up.
- **The right-hand column** (right of the board, about x 620–960 in the 1920×1080 reference with the canvas centre at 0) now holds Sergei's upgrade shop panel. Our money counter fills the `HeaderSlot` plaque at the top of Sergei's shop panel (Step 1c). It follows the plaque at runtime and auto-sizes from 28 to 64 pt, so "$99,999.99" fits. Our temporary Directory button and list are removed once Sergei's book is wired in (Step 1b). Anything else of ours (ticket, live calls) must go in free space; check against Sergei's panels before placing it.
- **Text glyphs.** Use only characters that exist in the static LiberationSans SDF atlas. "¢" and "—" are there; "→" is not. A missing glyph makes TextMeshPro write it into the fallback font asset at runtime, which dirties a file under `Assets/TextMesh Pro/`.
- **Turn off `raycastTarget` on anything that isn't meant to be clicked.** The cord input takes the topmost raycast hit across all canvases, so a stray raycast target over the board or tray blocks cords. Modal overlays (the open book, the shop) block clicks on purpose.
- Call cards line up with the caller panel's slots by reading the slots' positions at runtime. We never edit the panel prefab.
- **Popups by the money counter** (5a): the fine popup sits under the counter; the neutral "-$X" spend popup sits to its left, over the board's frame (briefly covering the "6" column label, never a jack), so the two never collide.
- **The "To be continued..." dialog** (5a) is the `Goal` object in `GameUI.prefab` with its own canvas at sorting order 15: above the open book (10), below the hand cursor and held plugs (20), so the pointer stays visible. Its backdrop blocks clicks while it is open (a deliberate modal); it is inactive otherwise.

### 4.4 Composition root and tick order
- **`GameSession`** (Domain, plain C#) creates every domain service in dependency order and owns the tick order. Tests and the balance simulator build it without Unity.
- **`GameInstaller`** (Game, `[DefaultExecutionOrder(-100)]`) is the only entry point. It:
  1. loads content
  2. loads the save (from Step 6)
  3. finds the views and the board in its own scene, by type
  4. builds `CordBridge` and `GameSession`
  5. calls `Bind(...)` on the views
- **`GameLoopRunner`** calls `session.Tick(Time.deltaTime)` from `Update`.

```csharp
public void Tick(double dt)
{
    clock.Advance(dt);
    idle.Tick(dt);       // idle guard: time since the last input (Step 4)
    earnings.Tick(dt);   // pay connected calls for min(dt, time left), so the last partial second is paid
    calls.Tick(dt);      // finish calls whose duration ran out; miss expired emergencies (Step 4)
    callbacks.Tick();    // re-queue due callbacks if there's room (Step 2)
    spawner.Tick(dt);    // new calls if there's room
    goal.Tick();         // raise GoalAffordable once, after this tick's money has moved (Step 5a)
}
```

### 4.5 CordBridge: how we connect to the team's cords
`CordBridge` (Game) implements the Domain port `ISwitchboard`. It touches the cord code only through the members listed in section 3.

- **Mapping:**
  - `SocketView.Address` → `JackId` (e.g. `"C4"`).
  - Each `CableController` → a `CordId` (its index, ordered by name at startup).
- **Observing changes.** Every `LateUpdate`, it compares each cable's `(Phase, FirstSocket, SecondSocket)` with the previous frame and reports the change to `OperatorDesk`:
  - first plug inserted → `OnPlugInserted(cord, jack)`
  - both ends in → `OnCircuitClosed(cord, jackA, jackB)`
  - a plug pulled from a closed circuit → `OnCircuitOpened(cord, jackStillIn, jackPulled)`
  - back in the tray → `OnCordReturned(cord)`

  The cord input runs in `Update`, so `LateUpdate` sees its result in the same frame.
- **Commands from the domain:**
  - `ReturnToTray(cord)`: call `TryRemove` on each plugged socket.
  - `Refuse(cord)`: `PlayBlockedFeedback()`, then return the cord to the tray.
- **Adding cords (5b).** `EnsureCords(total)` calls Sergei's `CableInputController.AddCable(color)` until the count is reached. It appends the new cable to its list, and records the cable's current state so the next `LateUpdate` reports nothing for it. New cords continue the index order (3, 4, 5). The spawner reads `CordCount` live.
- **Anything more is a coordinated change.** Example: touch input in the cord controller (before Step 6).

## 5. Systems
The step that introduces each part is shown in brackets.

### 5.1 Calls and their lifecycle [1, extended in 2 and 4]
- **Classes:**
  - `Call`: id, request, state, end reason, seconds left, cents earned, bound cord.
  - `CallRequest`: an immutable record of caller, recipient, type, cents per second, duration, and attempt number.
  - `CallState`, `CallEndReason`, `CallService`.
- **`CallService`** owns the live calls in arrival order and the timers. It knows nothing about cords, money or UI. Since D45, several calls can be accepted (Taken or Connecting) at once; `InHand` is only the most recently accepted one, kept for compatibility and never used to judge circuits.
- **Events:** `CallAdded`, `CallStateChanged(call, from, to)`, `CallEnded(call, reason)`.
- **Implementation:** an enum plus guarded methods (`Take`, `BeginConnecting`, `CancelConnecting`, `Connect`, `Hold`, `Resume`, `End(reason)`). Illegal transitions return `false`, and a table-driven test covers the whole matrix.
  - `OperatorDesk` drives `BeginConnecting` and `CancelConnecting` for every accepted call: it is Connecting exactly while a free half-patched cord has its one plug in that call's caller's or recipient's jack (D45).
  - The diagram below predates D45: the "swap" arrow from Taken back to Incoming no longer exists. Taking a card accepts it, and the other accepted calls stay as they are.
- **Enums:**
  - `CallState { Incoming = 0, Taken = 1, Connecting = 2, Connected = 3, OnHold = 4, Ended = 5 }`
  - `CallEndReason { None = 0, Finished = 1, WrongConnection = 2, DisconnectedEarly = 3, Missed = 4, Cleared = 5 }`
  - `Cleared` is used only by the dev panel's "clear calls". It pays nothing, fines nothing and never calls back, and the card leaves without a stamp.
- A scheduled callback is **not** a state of the old call. The scheduler keeps the request and later creates a new `Call` with a new id and attempt + 1.

```
   new call (spawner)          callback (same caller, same request)
           │                           │
           └──────┬────────────────────┘
                  ▼
            ┌────────────┐
            │  Incoming  │◄──────────────┐
            └─────┬──────┘               │ swap: the player
             take │                      │ takes another card
            ┌─────▼──────┐               │
            │   Taken    │───────────────┘
            └──┬─────▲───┘
    first plug │     │ that plug pulled out
            ┌──▼─────┴───┐
            │ Connecting │
            └──┬──────┬──┘
   right pair  │      │  wrong pair
               │      └─────────►┌──────────┐     ┌──────────────────────────┐
            ┌──▼──────────┐      │  HungUp  │────►│ CallbackScheduler:       │
            │  Connected  │      └──────────┘     │ re-enters as Incoming    │
            └──┬───────┬──┘                       │ after the type's delay   │
     duration  │       │ player pulls a plug      └──────────────────────────┘
     elapsed   │       └────────►┌───────────────────┐
          ┌────▼─────┐           │ DisconnectedEarly │   (via OnHold, D07)
          │ Finished │           └───────────────────┘
          └──────────┘

   Incoming, Taken or Connecting ── response time runs out ──► Missed
   (only types that define a response time, e.g. Emergency)
```
In the diagram, HungUp means `Ended` with reason `WrongConnection`, and Finished, DisconnectedEarly and Missed are `Ended` with that reason.

| From | Trigger | To | Effects |
|---|---|---|---|
| (new) | Spawner or callback creates the call | Incoming | The card appears. The response timer starts if the type has one. |
| Incoming | Player takes the card | Taken | Names are revealed. Any other call in hand goes back to Incoming (D03). |
| Taken | Player takes a different card | Incoming | Its timers keep running. |
| Taken | A cord has exactly one plug in (just plugged, or already in when taken) | Connecting | |
| Connecting | No cord is half-patched any more | Taken | |
| Taken or Connecting | A circuit closes on the right pair (D05: in either order) | Connected | The cord is bound to the call. The duration timer starts and the response timer stops. |
| Taken or Connecting | A circuit closes on a wrong pair | Ended (WrongConnection) | The cord returns to the tray. A callback is scheduled [2]. The missed fine applies if the type has one [4]. |
| (none in hand) | A circuit closes | n/a | `Refuse`: the cord bounces back with the blocked sound (D04). |
| Connected | The duration runs out | Ended (Finished) | The cord returns to the tray. The card shows a "Paid" stamp. |
| Connected | A plug of its cord is pulled | OnHold (D07) | Payment and the duration timer pause. |
| OnHold | The plug goes back into the same jack | Connected | Payment resumes. |
| OnHold | The plug goes into another jack, or the cord goes back to the tray | Ended (DisconnectedEarly) | The early-disconnect fine applies if the type has one [4]. If the plug went into another jack, the new circuit is judged as a fresh circuit. |
| Incoming, Taken or Connecting | The response timer runs out [4] | Ended (Missed) | The missed fine applies. A callback happens only if the type's flag is set (D10). |

- **How hold is implemented (2b).**
  - `OperatorDesk.OnCircuitOpened` on a bound cord calls `CallService.Hold` and remembers the cord's jack pair in a private `HeldCircuit` list. The cord is **not** marked half-patched, so its remaining plug doesn't put the call in hand into Connecting.
  - `OnCircuitClosed` on an on-hold cord: the same pair, in either order, resumes the call. This covers both the team's restore (click empty space) and clicking the pulled jack directly. Pay and the duration timer run only for Connected calls, and the `ConnectionFeePaid` flag stops a second fee.
  - Any other pair: the desk first unbinds the cord from the on-hold call (so ending it doesn't send the cord to the tray), ends it as `DisconnectedEarly`, then judges the new circuit as usual (connect, wrong number, or refuse).
  - The cord returning to the tray, including by pulling its second plug, ends the call as `DisconnectedEarly`.
  - Known: a call can earn for at most one frame between the click that pulls a plug and the bridge's report, because `GameLoopRunner.Update` may run before the cord input's `Update`. It is a fraction of a cent; accepted.

### 5.2 Spawning [1 minimal, 2 full, 7 pacing]
- **Classes:**
  - `CallSpawner`
  - `CallFactory`: resolves rate and duration. It uses the caller's override if set, otherwise a roll within the type's range. A roll is a whole number whenever the range contains one, so cards show readable rates and a finished call pays whole cents. Otherwise the roll is continuous.
  - `BusyTracker`
  - `IPacingProfile`, from `Economy.json`'s `pacing` (5c; was the `PacingProfile` asset)
  - `IRandom`, seeded so runs are reproducible
- **When** (built in 2a):
  - Each interval is `base × U(1 − jitter, 1 + jitter)`, at least 0.1 s. The first call comes after `firstCallDelaySeconds`.
  - The countdown runs at `dt / load`, where `load = 1 + loadFactor × max(0, waiting − freeCords)`:
    - waiting = live calls that aren't Connected or OnHold
    - freeCords = cord count − Connected and OnHold calls (an on-hold call keeps its cord in the board, 2b)
    - So the stretch applies live and eases as soon as the player catches up.
  - An attempt with no room, or with no free caller and recipient, is skipped. The next one comes a fresh interval later, and skipped calls are never made up.
  - **Cap (D17):** nothing spawns while `maxWaitingCalls` (6) calls are live. Every live call counts, connected ones included, because each one holds a caller slot.
  - Step 7 makes `base` depend on progress.
- **Which type.** A weighted pick at the current progress, respecting each type's `maxSimultaneous`.
- **Who.**
  - The caller is a weighted pick among callers on unlocked lines who aren't busy.
  - The recipient comes from the type's recipient rule: any subscriber, or people with a tag. They must be unlocked, not busy, and not the caller.
- **Busy (D18).** Anyone in a waiting, in-hand, connected or on-hold call can't be picked. Both the caller and the recipient of a pending callback can't be picked either.
- **Progress** = lifetime earnings ÷ the goal's price (D19). Pacing curves (in `Economy.json`, Step 7) turn it into the spawn interval and the type weights.
- **Opening calls.** The first few calls of a new game are scripted and double as the tutorial [7].
- **Idle guard (D16).** After 60 s without player input, types that carry fines stop spawning [4] Their due callbacks wait too. The timeout is `pacing.idleTimeoutSeconds` in `Economy.json` (5c).

### 5.3 Callbacks [2]
`CallbackScheduler` listens for `CallEnded`:
- `WrongConnection` always schedules a callback.
- `Missed` and `DisconnectedEarly` schedule one only if the type's flags say so (D10).
- The delay is rolled from the type's callback delay range.

Due callbacks enter the queue in due order when there's room; otherwise they wait. They go ahead of new spawns, because the scheduler ticks before the spawner. A plain list is enough.
- A callback keeps the original request: the same caller, recipient, rate, fee and duration, with attempt + 1. Its card shows a "Calling again" badge.
- A due callback whose party is in a live call is skipped for now, and later due callbacks may still ring.
- With 12 people, pending callbacks can hold the queue below 6. This is expected under D18.

### 5.4 Call cards [1 minimal, 2 queue, 3 full]
- A card is a view of one `Call`. There is no separate card model.
- **Classes** (Game layer):
  - `CallQueueView`: one card per live call (waiting, in hand, connected), in arrival order, laid over the caller panel's 6 slots.
    - When a card leaves after its stamp, the cards below slide up.
    - If a new call needs a slot while a stamp is still showing, the oldest stamp is cut short.
    - The ringing halo (`RingingPulse`) has a nested canvas with sorting order 9, so every halo draws behind every card's badges (order 10). It stays above Sergei's main canvas (0) and below his book (10, earlier sibling order) and held plugs (20). Fixed in 2b.
  - ~~`LiveCallsView`~~: superseded in 2a. The right-hand column is Sergei's shop, so connected calls keep their card in their slot.
  - `CallCardView`: pooled.
- **What the card shows in each state:**
  - Waiting: portrait, type badge, rate (e.g. "5¢/s"), and a countdown ring for types with a response time.
  - In hand: also the caller's name and the name of the person they want. From Step 1b they appear in a speech bubble next to the caller's portrait in the left column: "Ada Moss" / "wants Basil Thorne", and "On the line" once connected. This replaces the Step 1 ticket in the right column. From Step 3 the surnames are bold, because the book is sorted by surname, and the bubble text types out letter by letter (`charactersPerSecond`, 40 by default). "On the line" and "On hold" type out too.
  - Connected: the cord's colour (a ring around the portrait, read through `CordBridge.ColorOf`), the cents earned so far (a top badge), and an "On the line" bubble. Waiting cards show no bubble.
  - On hold: the cord ring and the earnings badge stay, the earnings frozen, and the bubble reads "On hold" (2b). From Step 3 the ring and earnings are dimmed.
  - Ended: a stamp, such as "Paid $1.20", "Wrong number!", "Cut off" or "Missed −$5". From Step 3 each stamp has its own ink: green for paid, red for wrong number, brown for cut off.

  The duration is never shown.
- **Interaction:** clicking or tapping a waiting card takes it. A successful take plays a 0.2 s squash and flash on the card and a soft click, a placeholder `card-take-click.wav` played on Sergei's Sfx channel (3). No UI path removes a card; only the rules end calls.
- **Emergencies** get a badge, a label and the ring, not colour alone.

### 5.5 Penalties [4]
`PenaltyService` listens for `CallEnded` and looks up the type's fine for the reason:
- `Missed` and `WrongConnection` use `fineMissedCents`, because the rules make them equal.
- `DisconnectedEarly` uses `fineEarlyDisconnectCents`.

`Wallet.ApplyFine` returns the amount actually taken (the balance floors at 0). Then `FineApplied(call, reason, requested, taken)` is raised for the popup.

### 5.6 Operator desk, switchboard port and connection rule [1]
- **`OperatorDesk`** handles the player's verb `TryTake(callId)` and the cord observations reported by `CordBridge`. It is the only class that coordinates calls with cords. Since D45 (persistent wiring, Sergei's `2fc3810`, adopted by the lead 2026-10-18):
  - It remembers every closed circuit on the board, whether or not a call uses it.
  - A closed circuit connects **any** accepted call it is correct for, whichever card the player last opened.
  - Taking a card connects it at once if a matching circuit is already plugged in.
  - A wrong number is judged only on a manual insertion: when the second plug goes in against an end that was already in place (the "anchor", from `CableController.LastInsertionAnchor`), and that anchor is a jack of an accepted call. Any other wiring is free.
  - Cords are never sent back to the tray when a call ends; a wrong-number circuit also stays plugged. Only the dev panel's "Clear calls" returns all cords (`ReturnAllCords`).
  - Hold and resume (D07) are unchanged.
- **`ConnectionRule`.** The default is that the two jacks equal the caller's and the recipient's jacks, in either order. A caller type may supply another rule later, for example long distance routed through a trunk jack.
- **`ISwitchboard`** (the port) has `CordCount`, `ReturnToTray(CordId)` and `Refuse(CordId)`. `CordBridge` implements it in the game, and a fake implements it in tests.

### 5.7 Directory [1 list, 3 book, 7 growth]
- **`PhoneDirectory`** (Domain) provides `JackOf(person)`, `PersonAt(jack)`, and `ListedEntries` sorted by surname. Only unlocked lines are listed [7].
- **Content:**
  - `PersonDefinition`: id, first name, last name, portrait, tags.
  - `PhoneDirectoryDefinition`: a list of `{ person, jackLabel }`.
  - People and jacks are linked only through this table.
- **Book view [3].**
  - The closed book on the desk (`Sprites/book/table_book.png`) opens an overlay drawn with the `cover`, `page` and `back` sprites.
  - Rows are generated from data, N per page, and pooled.
  - A–Z tabs (3, D34): `PhoneBookTab` components call `PhoneBookView.ShowLetter(char)`, which opens the spread with the first surname (the last word of the name) starting with that letter. Letters with no surname are greyed out. The tabs for the spread on show are near-white, bold, and stick out 8 px. **Hidden for now** (lead, 2026-10-18): `OpenBook/LetterTabs` is inactive in `PhoneBook.prefab`, with the code and objects kept.
  - Pages flip with tap or arrow keys (3). Swipe waits for `Pointer.current` in the cord input (Step 6).
  - Opening the book never pauses the game (D20).

### 5.8 Economy [1]
- **`Wallet`**:
  - `long Cents` plus a `double` fraction kept in [0, 1). Only whole cents move into the balance.
  - The fraction is carried forward, and saved [6], so nothing is lost to rounding.
  - Never use `float` for money: at $100,000, a float can't register the fraction of a cent a call earns each frame.
  - Floating-point noise: when the remainder is within 1e-9 ¢ of a whole cent, that cent is credited.
- **`EarningsService`** pays each connected call `min(dt, secondsLeft) × centsPerSecond × multiplier` every tick, so a finished call earns exactly duration × rate × multiplier. The multiplier is read live from `StatSheet.IncomeMultiplier`, so an upgrade bought mid-call applies at once (D12) [5a].
- **Display:** dollars and cents, e.g. `$12.34`, formatted with the invariant culture. `MoneyFormat` lives in Domain so it can be unit-tested. It formats `$1,234.56` and rates such as `5¢/s`.
- **Cheap UI updates:**
  - Views poll the wallet in `LateUpdate`.
  - They update the text only when the displayed value changes.
  - The HUD sits on its own nested canvas.

### 5.9 Upgrades and stats [5a, 5b, 7]
The designer's model (D39, D40) replaces the earlier plan of `UpgradeDefinition` assets with levels, a cost formula, stat modifiers and `UpgradeEffect` hooks.
- **Entries.** `Economy.json` holds a flat list of one-time upgrades. Every entry has:
  - `id`: stable forever, because saves store bought ids
  - `type`
  - `name` and `description`, shown in the shop
  - `costCents`

  Each type adds its own fields:

  | `type` | Fields | Effect | Step |
  |---|---|---|---|
  | `extra_income` | `incomeMultiplier` | **Sets** the earnings multiplier. | 5a |
  | `secret` | none | The goal. Exactly one. Its effect is code (D15); for now it shows "to be continued". | 5a |
  | `extra_wires` | `wires` | **Sets** the total number of cords. | 5b |
  | `notepad` | none | While owned, a tapped book entry notes its jack on every accepted call that person is part of (D23, D46). | 5b |
  | `extra_clients` | `boardSize {cols, rows}`, `clientSlots` | **Sets** the open part of the board and the number of caller slots (D42). | 7 |

- **Chains.** Entries of the same type form a chain in file order. Only the next unbought entry of a chain can be bought; there's no skipping. Values in a chain must grow.
- **`UpgradeService`** (Domain) owns the set of bought ids.
  - `CanBuy(id)` returns a `PurchaseCheck` (`Ok`, `Unknown`, `Owned`, `NotNext`, `TooExpensive`).
  - `TryBuy(id)` checks that the entry exists, isn't owned, is next in its chain and is affordable, then spends through `Wallet.TrySpend` and raises `UpgradeBought`. Otherwise it changes nothing and returns false.
  - A save restores the bought ids without spending [6].
- **`StatSheet`** recomputes every stat from the bought ids whenever they change. A stat is the value of the last bought entry in its chain, or the base value from `GameConfig` if none is bought (multiplier 1). Recomputing from scratch means nothing can be applied twice.
- **`ShopShelf`** (Domain) decides what the shop's buttons show (D41). It has 3 rotating slots, each holding one chain. A slot keeps its chain until the chain is bought out, then takes the next chain (in order of first appearance in the file) that isn't shown yet. The secret is never on the shelf; it has its own pinned button.
- **Multiplier.** It applies to the fee and the per-second pay, live, including calls already connected (D12, D32). Fines are never multiplied (D37).
- **Shop buttons as built (5a, accepted deviations from D41):**
  - Empty and too-expensive buttons stay clickable but do nothing; disabling Unity's `Button` would tint Sergei's "+" look. Too-expensive offers are dimmed instead (grey button image, faded text).
  - The owned secret shows its name and "Owned", with a full fill, no price, no frame, and does nothing on click.
- **Wires and Notepad as built (5b, accepted deviations):**
  - New cords are copies of the last cord and its tray slot, made while the game runs. The tray slot keeps its height and only narrows. With 6 cords, the previews are about half today's size.
  - Dev "Reset upgrades" keeps the added cords (a cord can't be removed safely mid-call). The shop then offers the first wire entry again, and buying it adds nothing.
  - A book row takes taps only while the Notepad is owned. A row tap keeps the book open; without the Notepad, a tap falls through and closes the book as before.
  - Validator rules: `wires` must be above `startingCords`, at most 6, and growing; there can be at most one `notepad`.
- **Checks.** The economy file's checks live in `EconomyFileLoader` (as the people-file checks live in `PeopleFileLoader`); `ContentValidator.ValidateEconomy` passes them on with the prefix "Economy file: ". There is no separate `EconomyValidator`.

### 5.10 Progression and the goal [5a]
- **`GoalProgress`** (Domain):
  - The goal is the one `secret` entry. Code reacts to the type, never to an id or the word "mystery".
  - `Progress = clamp01(balance ÷ secret cost)` drives the savings fill on the secret's button (D14, D41).
  - `GoalAffordable` fires the first time progress reaches 1.
  - Buying the secret raises `GoalPurchased`. For now the Game layer shows a "To be continued" panel, and play goes on.
- **`CareerStats`** [6]: lifetime earnings, calls connected, fines paid, time played.

### 5.11 Feedback: UI and audio [1 HUD, 8 full]
- **Views:** `MoneyCounterView`, `ShopView` (drives Sergei's shop buttons, including the savings fill; D41), `FloatingTextPool` (+$ and −$ popups).
- **`FeedbackDirector`** maps domain events to sound cues and popups.
- **`AudioService`:** a small pool of audio sources, looping sounds, and volume per category. Web builds have limited mixer and DSP support, so volume is handled in code.
- **Sounds:**
  - One ringing loop while any call waits.
  - One alarm loop while an emergency waits.
  - Coin ticks, rate-limited.
  - Distinct sounds for paid, hung up, missed and fined.
  - The team's plug sounds stay where they are.

### 5.12 Save and load [6]
- **Saved:**
  - version
  - balance and fraction
  - the bought upgrade ids (the secret among them)
  - career stats
  - settings and tutorial flags

  Career data is kept separate from run data, so a prestige reset or new-game-plus mode can reset one without the other.
- **Not saved:** calls, cords, the queue and callbacks. Each session starts with an empty board (D25).
- **Format:**
  - `JsonUtility`, with lists instead of dictionaries.
  - Content is referenced only by stable string ids. Unknown ids are ignored, and missing ones count as not bought.
  - The version number drives step-by-step migrations.
- **Storage.** `ISaveStorage` has two implementations:
  - `PlayerPrefsStorage` for the editor.
  - `BrowserStorage` for the Web build: a `.jslib` that writes `localStorage` under a fixed key such as `switchboard.save`, with a backup key.

  Unity's Web PlayerPrefs live in IndexedDB under a path tied to the page URL. Developers report that this loses saves when a host such as itch.io puts each upload at a new URL.
- **When to save:**
  - right after a purchase
  - every ~10 s while something has changed
  - on `OnApplicationFocus(false)`

  Never rely on `OnApplicationQuit` in a browser.

## 6. Data

### 6.1 ScriptableObjects (read-only at runtime)

| Asset | Fields, with the step that adds them | Notes |
|---|---|---|
| `GameConfig` | `maxWaitingCalls` = 6 [2]; card-style list `cardStyles` [4, renamed 5c]; the `PhoneBook.json` and `Economy.json` references [1b, 5a]; `extraCordColors` [5b]; portraits | The one asset `GameInstaller` references. Since 5c (D47) `startingCents`, `startingCords` and pacing live in `Economy.json`; only presentation and the caller panel slot count stay here. |
| `CallerTypeDefinition` | `id`, `cardStyle` (badge, accent) [4; trimmed 5c] | Card look only (D47): matched by id to a call type in `Economy.json`'s `calls`. Regular, Emergency. A type without one gets plain cards. The money and timing fields moved to the file. |
| `CallerDefinition` | `id`, `person`, `type`, `weight`, optional `centsPerSecondOverride` [1]; optional favourite recipients [7] | The "caller config". This is how some callers become worth much more. |
| ~~`PersonDefinition`~~, ~~`CallerDefinition`~~, ~~`PhoneDirectoryDefinition`~~ | Replaced in Step 1b by `Assets/Configs/PhoneBook.json` (D31). | Loaded and validated at startup. Errors are logged clearly, and a test runs the same check. |
| `PhoneDirectoryDefinition` | entries `{ person, jackLabel }` | Jack labels are the `SocketView.Address` values, A1–F6. |
| ~~`PacingProfile`~~ | Replaced in 5c by the `pacing` section of `Economy.json` (D47), read into the domain's `PacingSettings`. | Progress curves and opening calls [7] will extend that section. |
| `BoardLayoutDefinition` | sections: which jack labels each unlocks [7] | |
| ~~`UpgradeDefinition`~~, ~~`UpgradeEffect`~~ | Replaced by the `upgrades` section of `Assets/Configs/Economy.json` (D39, D40). | See 6.4. |
| `AudioCueLibrary` | event → cue [8] | Game layer only. |

- **Shared types** live in Domain as `[Serializable]` structs with public fields, so Unity can serialize them inside Content assets. Example: `FloatRange { public float min, max; }`.
- **Domain sees content through read-only interfaces:** `IGameContent`, `IPerson`, `ICaller`, `ICallerType`, `IDirectoryEntry` and so on. Tests use plain fakes.
  - `IGameContent` also exposes the `CallerTypes` and `People` catalogs, for the validator.
  - `IPerson` already has `Tags`, which stay empty until Step 4.
  - The directory reaches Domain as `IGameContent.Directory`. Each `DirectoryEntry` implements `IDirectoryEntry`; `PhoneDirectoryDefinition` itself implements no interface. Presentation-only fields (sprites, colours) sit on the same assets but stay out of the interfaces.

### 6.2 Runtime state (plain C#)

| State | Owner | Saved |
|---|---|---|
| Balance and fraction | `Wallet` | Yes |
| Bought upgrade ids | `UpgradeService` | Yes |
| Current stat values | `StatSheet` | No, recomputed |
| What the shop's buttons show | `ShopShelf` | No, rebuilt from the bought ids |
| Career stats | `CareerStats` | Yes |
| Settings | Game layer | Yes |
| Calls, queue, call in hand | `CallService` | No |
| Pending callbacks | `CallbackScheduler` | No |
| Cord state and jack occupancy | the team's cord code, mirrored by `CordBridge` | No |
| Spawn timer, random generator state | `CallSpawner` | No |

### 6.3 Data rules
- **Ids.** Every definition has a stable string `id`, used for saves. Never use the asset name, a list position or a GUID.
  - Format: lowercase with a prefix, e.g. `person.ada-moss`, `type.regular`, `upgrade.extra-cord`.
- **Enums.** Serialized enums use explicit numbers and are never reordered.
- **No runtime writes.** Nothing writes to a ScriptableObject at runtime. In the editor, changes made during Play Mode persist afterwards.
- **People's names:** period-appropriate first and last names (roughly 1920s–40s). Surnames are unique so the book sorts clearly. No famous real people.
- **`ContentValidator`** (editor menu item, also run as an EditMode test) checks:
  - ids are unique
  - every caller's person is in the directory
  - every jack label exists on the board and is used once
  - every range has min ≤ max
  - each type has enough possible recipients
  - `Economy.json` (6.4) [5a]

### 6.4 `Economy.json` [5a, 5c] (D39)
`Assets/Configs/Economy.json` belongs to the designer, who edits it by hand. Keep it readable, with one entry per line. The format is documented for him in `Docs/EconomyFile.md`.
- **Sections:**
  - `upgrades` [5a]: see 5.9.
  - `startingCents`, `startingCords` (3–6) [5c].
  - `pacing` [5c]: first-call delay, base interval, jitter, load factor and idle timeout.
  - `calls` [5c]: one entry per call type: `id`, `name`, `spawnWeight`, ranges `{min, max}` for rate, fee, duration and callback delay, response time, fines, callback flags, `maxSimultaneous`, `everyoneCalls`, `recipientTag`. Missing required fields are reported, never silently 0.

  Presentation stays in Unity assets, for example the emergency card's badge and colour.
- **Upgrade ids** follow the designer's style: lowercase letters, digits and `_`, such as `income_1`. This is an accepted exception to the `type.regular` id style.
- **Money** is whole cents (`costCents`), like the wallet.
- **The validator** (also an EditMode test and part of `Tools/Switchboard/Validate Content`) reports, each in plain words naming the entry and the fix:
  - a missing or unreadable file
  - an empty, badly formed or duplicate id
  - an unknown type (the message lists the known types)
  - a missing name
  - a cost of 0 or less
  - a chain value that doesn't grow (e.g. `incomeMultiplier` 1.2 after 1.5), or an income multiplier of 1 or less
  - not exactly one `secret`

## 7. Communication rules
- **Down: method calls.** Views call verbs on narrow interfaces such as `OperatorDesk.TryTake` or `IShop.TryBuy`. Services validate and return `bool`.
- **Up: C# events for things that happen.** Payloads are `readonly struct`s. Views subscribe in `Bind` and unsubscribe in `OnDisable`.
- **Up: polling for values that change every frame** (money, earnings per call, countdowns). Views compare one number in `LateUpdate`.
- **Between services: the same events.** For example, `PenaltyService` and `CallbackScheduler` both listen for `CallEnded`. `OperatorDesk` is the one deliberate mediator between calls and cords.
- **Not allowed:** singletons, static mutable state, service locators, `FindObjectOfType` or finding objects by name. The exceptions are the lookups of Sergei's objects in `Final` described in section 3: `CordBridge`'s typed lookup and the `CallerQueuePanel` slots.
- **Not used for core rules:** ScriptableObject event channels.
- **Event safety.** Raise events only after state is consistent, and don't iterate a list that a handler can change.

## 8. Web constraints
- **Single thread.** No threads, no `Task.Run`, no `async void`. All gameplay runs in the main loop.
- **Game time only.** Gameplay timers use `Time.deltaTime`, clamped by `maximumDeltaTime`, never the system clock. Run In Background stays off, so timers pause in hidden tabs.
- **Memory.** Pool cards, rows and popups. Avoid per-frame allocations and LINQ in per-frame code. Use sprite atlases and compressed textures [9].
- **Audio unlock.** Browsers block audio until the player interacts, so the Boot scene has a tap-to-start screen [6].
- **Input.** Nothing may depend on hover. The cord code needs `Pointer.current` instead of `Mouse.current` to work on touch; that's a coordinated change before Step 6.
- **Layout.** Landscape first. In portrait, show a "rotate your device" screen (D24) [9].

## 9. Extensibility
- **New caller:**
  1. Create a `PersonDefinition`.
  2. Add a directory entry.
  3. Create a `CallerDefinition`.

  The validator checks it. No code.
- **New caller type (VIP, long distance):** add a line to `calls` in `Economy.json` (5c), and optionally a `CallerTypeDefinition` asset for its card badge and colour. Only new behaviour needs code: a `ConnectionRule`, or a small behaviour hook that `CallService` calls. Code never checks which type a call is.
- **New upgrade:** add an entry to `Economy.json`. Only a new `type` needs code:
  - the type name and its fields in the loader and validator
  - the stat in `StatSheet`, with a base value
  - the system that reads it
- **Bigger board:** add a section to `BoardLayoutDefinition`, and add people for its jacks to the directory.
- **Mystery effect (D15):** react to `GoalPurchased` (once) and, from Step 6, to a loaded save with the secret bought (a lasting state). If it needs a new system, register that system in `GameSession`.

## 10. Decisions
Statuses:
- **decided:** agreed.
- **default:** we build this unless told otherwise.
- **confirm:** the default needs the lead's yes before the step that needs it.
- **open:** must be decided before that step.

| ID | Decision | Status | Needed by |
|---|---|---|---|
| D01 | Integrate with the cord code through `CordBridge`, with zero edits to team files. Our systems live in `Game.prefab` in the shared `Final.unity` (D38; until then, `Game.unity` loaded `Level.unity` additively). | default | 1 |
| D02 | Assemblies as in 4.1: Domain and Content are asmdefs; the Game layer is in Assembly-CSharp. | default | 1 |
| D03 | One call in hand at a time. Taking another card swaps; the old one goes back to the queue with its timers running. **Superseded by D45.** | superseded | 2 |
| D04 | A circuit closed with no call in hand bounces back with the blocked sound. | default | 1 |
| D05 | Plug order doesn't matter. | default | 1 |
| D06 | A wrong connection ends the call at once and the cord bounces back. The callback comes after the type's delay. | decided (spec) | 1/2 |
| D07 | Pulling a plug from a live call puts it **on hold**: no pay, timer paused. Putting the plug back resumes the call. Moving it elsewhere or returning the cord to the tray ends it as DisconnectedEarly. This uses the team's new restore feature. | decided (lead, 2026-10-17) | 2 |
| D08 | Every call end returns its cord to the tray. | default | 1 |
| D09 | An emergency's response timer runs until a correct connection, and it's visible as a ring on the card. | default | 4 |
| D10 | Callbacks after a miss or an early disconnect are per-type flags, off by default. | default | 4 |
| D11 | Rate comes from the type's range, with an optional per-caller override. | default | 1 |
| D12 | The multiplier applies live, including to connected calls. | default | 5 |
| D13 | Money is a `long` of cents plus a fraction, displayed as `$0.00`. | decided | 1 |
| D14 | Savings bar = balance ÷ goal price. It is drawn as a fill inside the secret upgrade's shop button (D41). | decided (lead, 2026-10-18) | 5a |
| D15 | What happens after the mystery upgrade (ending, prestige, or play continues). The save keeps career and run data apart either way. | open | before the real goal effect |
| D16 | Idle guard: types that carry fines stop spawning after 60 s without input. | default | 4 |
| D17 | Waiting-card cap = 6 (the caller panel has 6 slots). | default | 2 |
| D18 | A person can't be in two calls at once. | default | 2 |
| D19 | Difficulty is driven by lifetime earnings. | default | 7 |
| D20 | The book and the shop never pause the game. | default | 3 |
| D21 | Board growth uses pre-laid-out sections with cover plates. **Superseded by D42** (dimmed closed jacks; plates only if Mia draws them later). | superseded | 7 |
| D22 | Jack assignments are fixed in data. | default | 1 |
| D23 | Book QoL: tapping an entry in the phone book notes its jack on the call in hand (since D46: on every accepted call that person is part of) (e.g. "Ada Moss (C3) / wants Basil Thorne (A5)"). Not built in from the start: it is sold in the shop as a cheap one-entry upgrade, `type` `notepad` in `Economy.json` (D40). The designer can drop it by deleting the entry. Sergei's book gets a tapped-entry event as a coordinated change by our agent (D44). | decided (lead, 2026-10-18) | 5b |
| D24 | Portrait on phones shows a "rotate your device" screen. | default | 9 |
| D25 | Save progression only; no in-flight calls and no offline earnings. | default | 6 |
| D26 | Hosting target (itch.io, own site, …). It affects storage and compression settings. | open | 6 |
| D27 | Currency, number scale, and target time to the mystery upgrade. Placeholder numbers until then (D43). The designer tunes them in `Economy.json`. | open | 10 |
| D28 | Touch input in the cord code (`Pointer.current`). This is the cord owner's change. | coordinate | 6 |
| D30 | Division of work: Sergei builds the UI components (phone book, shop panel, main menu, visuals); our agents build the rules and data, and wire his components to them. Steps 3, 5 and 6 become wiring steps. | decided (lead, 2026-10-17) | 1b |
| D31 | One people file. `Assets/Configs/PhoneBook.json` is the single source of people, jacks and per-caller settings. Each contact has `name`, `socket`, `portrait` (sprite name), `type` (call type id) and optional `rate` / `fee` overrides (0 = use the type). Sergei's book reads only `name` and `socket` from it, unchanged; our game reads every field. The per-person ScriptableObjects from Step 1 are removed. Call types and game settings stay ScriptableObjects. Portraits appear on the call card, not in the book. Names are in English. | decided (lead, 2026-10-17) | 1b |
| D32 | Connection fee. On a correct connection the caller pays a one-off fee, then the per-second rate. The fee comes from a range on the caller type, with an optional per-caller override (the same simple config as the rate; no tiers). It is paid once per call: not on a wrong connection, not again after hold and resume. The multiplier applies: earnings = (fee + seconds × rate) × multiplier. The card shows both, e.g. "10¢ + 5¢/s". The "Paid" stamp includes the fee, and "+10¢" pops up on connection. | decided (lead, 2026-10-17) | 1b |
| D33 | Scene flow: `MainMenu` → `Game` → `Level` (build indices 0, 1, 2). Sergei's menu loads the next build index, which is `Game`; `Game` loads `Level` additively. Superseded by D38: `MainMenu` → `Final`. | superseded | 1b |
| D34 | Phone book A–Z tabs are added to Sergei's `PhoneBookView` by our Step 3 agent as a coordinated change (approved files are listed in the Step 3 brief). | decided (lead, 2026-10-18) | 3 |
| D35 | The directory grows to 36 people, one on every jack A1–F6, in Step 3. Portraits are reused until more are drawn. From Step 7 the game starts on a 3×3 part of the board and the book shows only the people on open jacks (D42). | decided (lead, 2026-10-18) | 3 |
| D36 | Emergency services (Police, Fire Brigade, Doctor) replace 3 of the 36 people on their jacks (33 people + 3 services). They are listed **first** in `PhoneBook.json`, so the book shows them at the top of page 1; the people follow, sorted by surname. The validator allows that leading block of `emergency-service` entries. Sergei's book code is unchanged (it shows file order). | decided (lead, 2026-10-18) | 4 |
| D37 | Step 4 numbers: an emergency must be connected within **30 s**. Fines: **$2** for a miss or a wrong patch, **$1** for cutting a connected emergency off early (not multiplied). Emergency pay: fee 20–40¢, 5–10¢/s, 10–20 s calls. About **1 call in 6** is an emergency (a temporary `spawnWeight` on each caller type: Regular 5, Emergency 1, until Step 7's `weightByProgress`), and at most one at a time. Any person can place an emergency call (`everyoneCalls`); services never call, and regular calls never go to a service (`recipientTag` empty = untagged people only). Services are JSON entries with `"tags": ["emergency-service"]` and no portrait or type. | decided (lead, 2026-10-18) | 4 |
| D38 | One final scene, `Final.unity` (formerly `Level.unity`), built from prefab instances: Sergei's prefabs plus our `Game.prefab` and `GameUI.prefab`. Build list: `MainMenu` (0), `Final` (1); `Game.unity` is removed. The scene is shared and the prefabs are owned: each owner edits only their own prefabs, and edits to the scene file itself (adding or removing a root prefab) are announced to the team first. No serialized links between owners' prefabs; our code finds Sergei's objects by type at start. Sergei's first merge (`1c20569`, unpacked copies) was reverted in `cdc5876`. This carries out D29. | decided (lead, 2026-10-18) | before 5 |
| D39 | Economy config file. The economy is controlled by the designer through one hand-edited file, `Assets/Configs/Economy.json`, with sections: `upgrades` [5a]; `calls`, `pacing` and `startingCents` [5c]. Presentation (card badges, colours) stays in Unity assets. Domain rules keep reading values through interfaces, so only Content loaders, the validator and tests change. A format guide for the designer lives in `Docs/EconomyFile.md`. | decided (lead, 2026-10-18) | 5a |
| D40 | Upgrade model (the designer's proposal, adapted): a flat list of one-time entries with `id`, `type`, `name`, `description`, `costCents` and the type's fields. Types: `extra_income` (`incomeMultiplier`), `extra_wires` (`wires`), `notepad`, `extra_clients` (`boardSize`, `clientSlots`), `secret` (exactly one). Every value **sets** the resulting state (wires total, not wires added). Entries of one type form a chain in file order; only the next unbought one can be bought; values in a chain must grow. Ids are never renamed once players have saves. No levels, cost formula or stat modifiers; this replaces the old 5.9 plan. The designer's `clientPool` is dropped (D42). | decided (lead, 2026-10-18) | 5a |
| D41 | The shop is Sergei's `UpgradeShopPanel` with its 4 buttons. The bottom button is pinned to the `secret` upgrade and shows the savings fill (D14). The top 3 rotate: each shows the next offer of one chain, and when a chain is bought out its button takes the next chain not yet shown, in file order (`ShopShelf`). A button with nothing to offer shows Sergei's placeholder look and can't be clicked. The name, description and price are drawn inside his button prefab by a small component in his folder, added as a coordinated change (D44); our `ShopView` drives it. | decided (lead, 2026-10-18) | 5a |
| D42 | Board growth [7]. The game starts on a 3×3 part of the board (rows A–C, columns 1–3). Exactly the people whose jack is on the open part are in the game, and the book shows only them, so the designer picks the starting group by placing people on those jacks. `extra_clients` sets the open board size and the caller slots. Closed jacks are dimmed and can't be clicked (cover plates only if Mia draws them). The emergency services join when the board opens over their jacks, so emergencies come later in a session. Needs coordinated changes to Sergei's board and book. Supersedes D21. | decided (lead, 2026-10-18) | 7 |
| D43 | Placeholder upgrade prices (until the designer's file; D27): income ×1.5 for $8, ×2.0 for $20, ×3.0 for $50; the secret $250. From 5b: wires 4, 5 and 6 for $5, $8 and $13 (start 3); Notepad $3. | default | 5a |
| D44 | Sergei's missing hooks are built by our agents as coordinated changes (lead, 2026-10-18), each brief listing the approved files: the shop button texts and savings fill [5a]; adding cords at runtime to his cord input and tray [5b]; a tapped-entry event in his phone book [5b]. | decided (lead, 2026-10-18) | 5a |
| D45 | Persistent wiring (Sergei's `2fc3810`, adopted by the lead 2026-10-18). Several calls can be accepted at once; taking a card no longer swaps. Cords stay plugged when calls end. A closed circuit connects any accepted call it is correct for; taking a card connects it at once if its circuit is already plugged. A wrong number needs a manual insertion against an anchored end that belongs to an accepted call. The bubble shows only on the card last clicked. Supersedes D03; see 5.1 and 5.6. | decided (lead, 2026-10-18) | 5b merge |
| D46 | Notepad under D45: a tapped book entry notes that person's jack on every accepted call still waiting to connect (Taken or Connecting) where they are the caller or the recipient, including duplicates. Ringing, connected, on-hold and ended calls are never noted. | decided (lead, 2026-10-18) | 5b fix |
| D47 | Step 5c details: `startingCents`, `startingCords` (3–6), `pacing` and `calls` live in `Economy.json`; the old asset fields are deleted, not kept as a fallback (`CallerTypeDefinition` keeps only `id` and the card style; `PacingProfile` and `Default.asset` go). Ranges are `{ "min", "max" }` objects. Call type ids keep the `type.` style because `PhoneBook.json` references them; the type's display `name` moves into the file. `maxWaitingCalls` and the cord colours stay in `GameConfig`. | decided (lead, 2026-10-18) | 5c |
| D29 | Merge into one scene. The `Game.unity` + `Level.unity` split is temporary; it keeps Sergei's actively edited scene conflict-free. Once his cord work settles, and with his agreement, move the `Game` object and the `GameUI` prefab into one scene, then remove `Game.unity` and the additive loading. Do it at the latest before the first Web build. **Done as D38 (2026-10-18).** | done | 6 |

## 11. Glossary
- **Jack / socket / line:** a hole in the board, labelled like `C4`. A line is a jack with a subscriber on it.
- **Cord / cable:** a two-plug cord kept in the tray. "Cable" is the team's code name.
- **Patch:** to plug a cord's ends into two jacks.
- **Circuit closed:** both plugs of a cord are in jacks.
- **Call in hand:** the one taken call (Taken or Connecting) that the next closed circuit is judged against.
- **Half-patched:** a cord with exactly one plug in.
