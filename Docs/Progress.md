# Progress

| Step | Status | Built | Verified | Playtested | Commit |
|---|---|---|---|---|---|
| 1. First call | done | ✅ 2026-10-17 (54 min) | ✅ 72/72 tests, scope clean | ✅ all 9 checks | e6f44e5 |
| 1b. Fee, book, layout | done | ✅ 2026-10-17 (20 min) | ✅ 91/91 tests, scope clean | ✅ | 60c7a88 |
| 1c. Money slot in shop header | done | ✅ 2026-10-17 (8 min) | ✅ 91/91 tests | ✅ | 47735c1 |
| 2a. Queue, callbacks, dev panel | done | ✅ 2026-10-17 (24 min) | ✅ 156/156 tests, scope clean | ✅ | 2b00ac7 |
| 2b. Hold and resume | done | ✅ 2026-10-18 (12 min) | ✅ 187/187 tests, scope clean | ✅ | 4c5ac50 |
| 3. Phone book and cards | done | ✅ 2026-10-18 (16 min + 8 min fix + 3 min fix) | ✅ 211/211 tests, scope clean | ✅ | |
| 4. Emergencies and fines | done | ✅ 2026-10-18 (26 min) | ✅ 255/255 tests, scope clean | ✅ | 14fd59c |
| Scene merge: `Final` (D38) | done | ✅ 2026-10-18 (11 min) | ✅ 255/255 tests, scope clean | ✅ | 3002ea8 |
| 5a. Economy file, shop, income, secret | done | ✅ 2026-10-18 (23 min) | ✅ 304/304 tests, scope clean | ✅ | e115c59 |
| 5b. Extra wires and Notepad | done | ✅ 2026-10-18 (22 min) | ✅ 333/333 tests, scope clean | ✅ | 667adda |
| 5c. Calls and pacing into Economy.json | not started | | | | |
| 6. Saves, Boot, Web build | not started | | | | |
| 7. Board growth and pacing | not started | | | | |
| 8. Sound and feel | not started | | | | |
| 9. Mobile and Web polish | not started | | | | |
| 10. Balance | not started | | | | |

A step moves through these statuses: not started → building → verifying → playtesting → done. It can also be blocked.

## Baseline (2026-10-17)
- **Unity and MCP:** Unity 6000.3.6f1 with MCP for Unity v10.3.0, connected as `Ludumdare60@67eac6d9bbf6b918`.
- **Tool groups:** `testing`, `scripting_ext` and `docs` are turned on. Subagents can use the MCP tools but not MCP resources (a probe agent confirmed this).
- **Tests:** 27/27 EditMode tests pass, all of them the team's `CableConnectionTests`. The console is clean.
- **Scenes:** the build list contains only `SampleScene`. The game scene `Level.unity` is Sergei's.
- **TextMeshPro:** the essentials are not imported yet.

## Log
<!-- Agents append their reports below, newest last. The orchestrator adds verification and playtest notes under each report. -->

### Level artwork: background and lamps (2026-10-18)
- Requested directly by the lead: add `main_background_0` to a Canvas container, hide replaced placeholder visuals, add lamp sprite states, and save a prefab.
- `BackgroundPanel.prefab` contains the background and the existing switchboard as a nested prefab. The Level scene uses the new prefab instance. Socket positions and all 36 socket identities are preserved.
- Twelve row/column lamps use `bulb-01` (lit), `bulb-02` (unlit), and the matching glyph pairs. Hovering or occupying a socket lights its row and column. Shared rows/columns stay lit until the last relevant plug is removed. F retains a text label because the artwork only supplies A–E.
- Disabled caller panel decoration, old row/column badges and their strips as instance overrides; original source prefabs remain available. Six caller slot anchors are retained; aligning them to the five painted portrait windows is a separate layout task.
- Verified prefab reload and internal socket references, on/off artwork, hover, occupancy, multiple plugs and release, and raycast passthrough. Render previews: `Temp/Background-off.png` and `Temp/Background-on.png`.
- Play Mode smoke check passed: picked up a real cable, inserted it into B3, observed B and 3 lit, then removed it and observed both unlit. All three cables and 36 sockets remain available. Exited Play Mode; temporary verification script removed.

### Step 1 prep (orchestrator, 2026-10-17)
- **Decisions:** the lead confirmed D07 (hold). It is built in Step 2; Step 1 keeps the temporary immediate cut-off.
- **Coordination:** the lead was asked to tell Sergei about `Game.unity`, which loads `Level.unity`, and about the public members we rely on.
- **Pre-flight:**
  - `git fetch`: up to date with upstream.
  - Discarded Unity's format-only re-save of `SampleScene.unity`, which my test run caused.
  - Editor idle, console cleared.

### Step 1 report: 2026-10-17
Result: done

Playable now: Press Play in `Game.unity`. `Level.unity` loads additively with the team's board and cords unchanged. About 2 s later a call rings over the top caller slot (portrait, pulsing amber halo, "5¢/s"). Click it to see the ticket, look the names up with **Directory (D)**, and patch the two jacks. Money counts up live until the call ends with a "Paid $X" stamp; the cord returns to the tray and the next call rings 2 s later.

How to test: the Plan's playtest script, with two notes:
1. The money counter, the ticket and the Directory button sit in the free column right of the board. The ticket reads "Ada Moss / wants Basil Thorne" on two lines, not "Ada Moss → …" (deviation 1).
2. The directory list also closes when you click outside its page.

Created:
- **Assemblies:** `Assets/Scripts/Domain/Switchboard.Domain.asmdef` (`noEngineReferences`), `Assets/Scripts/Content/Switchboard.Content.asmdef`, `Assets/Tests/Domain/Switchboard.Domain.Tests.asmdef` (the CableTests settings plus references to Domain and Content).
- **Domain** (`Assets/Scripts/Domain/`):
  - `Common/`: IRandom, SeededRandom, IGameClock, GameClock, FloatRange, JackId, CordId, CallId
  - `Definitions/`: IGameContent, IPerson, ICallerType, ICaller, IDirectoryEntry, PersonExtensions
  - `Calls/`: CallState, CallEndReason, CallRequest, Call, CallEvents (readonly struct payloads), CallService
  - `Spawning/`: CallFactory, CallSpawner
  - `Directory/`: PhoneDirectory
  - `Desk/`: ISwitchboard, ConnectionRule, OperatorDesk
  - `Economy/`: Wallet, EarningsService, MoneyFormat
  - `GameSession.cs`
- **Content** (`Assets/Scripts/Content/`): GameConfig, PersonDefinition, CallerTypeDefinition, CallerDefinition, PhoneDirectoryDefinition, DirectoryEntry.
- **Game** (`Assets/Scripts/Game/`, Assembly-CSharp): GameInstaller, GameLoopRunner, CordBridge, LevelScene (the only two lookups into Level: cords and sockets by type, and the `CallerQueuePanel` slots), `Views/` MoneyCounterView, CallCardView, CallQueueView, DirectoryListView.
- **Tests** (`Assets/Tests/Domain/`): Fakes.cs (fakes plus a three-person TestWorld) and nine test classes.
- **Content assets** (`Assets/Content/`):
  - `CallerTypes/Regular.asset`: `type.regular`, 3–8 ¢/s, 15–30 s.
  - `People/` ×12 and `Callers/` ×12 (Regular, weight 1).
  - `PhoneDirectory.asset`; `GameConfig.asset` (`startingCents` 0).
  - People, portraits `caller-01`…`caller-12` in this order, and jacks (two per row A–F): Ada Moss C3, Basil Thorne A5, Clara Whitlock E1, Edgar Pennington B4, Florence Abbott F6, Walter Pryce D2, Mabel Dunmore A2, Harold Lacey E4, Ivy Crandall C6, Otis Bramble F3, Lillian Yardley B1, Norman Tulley D5.
- `Assets/Prefabs/Game/GameUI.prefab`, `Assets/Scenes/Game.unity`.
- `Assets/TextMesh Pro/` (TMP Essential Resources; 82 files including metas).

Changed: `ProjectSettings/EditorBuildSettings.asset` only. The build list is now `Game` (0), `Level` (1); SampleScene was removed from the list, and the file is kept.

Scene and prefab changes:
- **`Game.unity`** (new, saved; left open as the active scene): the `Game` object holds GameInstaller (wired to GameConfig and the three views), GameLoopRunner and CordBridge, next to a `GameUI` prefab instance. It has no camera and no EventSystem.
- **`GameUI.prefab`** (new):
  - Root: Screen Space Overlay canvas, sorting order 10; CanvasScaler set to Scale With Screen Size, 1920×1080, Expand; GraphicRaycaster.
  - Children: `Calls` (CallQueueView; `CallCard` with pulse, portrait, rate badge and stamp), `Ticket`, `Hud` (nested canvas, MoneyCounterView), `Directory` (DirectoryListView; modal `Overlay` and `DirectoryButton`).
  - Raycast targets: only the card portrait, the Directory button, and the overlay's backdrop and page. The overlay is modal on purpose.
- **Team scenes:** `Level.unity` was opened once for the regression check and closed without saving; it never became dirty. SampleScene was closed unmodified. No team file changed (`git diff` is empty for every team path).

Tests: 72/72 EditMode (27 team + 45 new). New:
- WalletTests: 4
- EarningsServiceTests: 3
- CallServiceTests: 8
- OperatorDeskTests: 8
- CallSpawnerTests: 5
- CallFactoryTests: 4
- PhoneDirectoryTests: 3
- MoneyFormatTests: 7
- GameSessionTests: 3

Console: clean. Compiling, running the tests, a 10.7 s Play Mode run of `Game.unity` (including a scripted take and a correct patch) and a solo `Level.unity` run all logged zero errors and zero warnings.

Tool noise, not from our code:
- Every `manage_camera screenshot` logs two Metal lines: "Ignoring depth surface load/store action as it is memoryless".
- One capture taken while play mode was running unpaused came out blank. The same capture logged the engine error "PlayerLoop internal function has been called recursively".

Screenshot: `Temp/Captures/step-01.png` shows a connected call: cord C6–E4, ticket "On the line", $0.21. Extras in the same folder: `step-01-ringing.png`, `step-01-directory.png`, `step-01-cutoff.png`.

Checked in Play Mode through real EventSystem raycasts and `CableInputController.ProcessPointer`:
- **Take:** the card is the topmost raycast hit over the slot and takes the call; the ticket shows.
- **Directory:** opens from the button and closes from the backdrop.
- **Right pair:** connects in both plug orders and pays live. A 17 s call at 5 ¢/s paid exactly $0.85 with a "Paid" stamp, and Cable_01 went back to the tray.
- **Wrong pair:** "Wrong number!", the cord goes back, nothing is paid.
- **No call taken:** a patch bounces back (Refuse).
- **Pulled plug:** "Cut off", the cord goes back and payment stops at that moment. The wallet held 85 + 1.028 = 86.028 exactly.
- **`Level.unity` alone:** one scene, none of our objects, and a sandbox patch A1–F6 stays connected.

Deviations from Design/Plan:
1. **Ticket arrow.** The ticket uses two lines ("Ada Moss / wants Basil Thorne") instead of "Ada Moss → Basil Hart". The "→" glyph isn't in the static LiberationSans SDF atlas, so TMP would add it to the dynamic fallback asset at runtime and dirty a file under `Assets/TextMesh Pro`. "¢" and "—" are in the static atlas.
2. **Whole-number rolls.** CallFactory rolls whole numbers within the type's range when the range contains one, and falls back to a continuous roll otherwise. Cards then show readable rates, and a finished call pays exactly rate × duration. Overrides are used as they are.
3. **Wallet tolerance.** Wallet credits a whole cent when the remainder is within 1e-9 ¢ of it. Without this, floating-point noise holds back the last cent; the "exactly 100 cents" tests need it. The fraction stays in [0, 1).
4. **PhoneDirectoryDefinition** implements no interface itself, because the brief lists none for it. Its `DirectoryEntry` items implement `IDirectoryEntry`, and GameConfig exposes them through `IGameContent.Directory`.
5. **Extra interface members.** IGameContent also exposes `CallerTypes` and `People` (the "catalogs" of §6.1) for the Step 3 validator. IPerson includes `Tags`, because PersonDefinition's row in §6.1 has no step marker; every person's tags are empty.
6. **Take.** `CallService.Take` refuses a second call while one is in hand. Step 1 has only one call; the D03 swap is Step 2.
7. **OperatorDesk.**
   - It also cuts off a connected call whose cord returns to the tray without a reported pull (for example, a disabled cable). This matches D07's tray rule.
   - `OnPlugInserted` does nothing until calls can be Connecting (Step 2).
8. **MoneyFormat** ("$1,234.56", "5¢/s", invariant culture) lives in Domain so it is unit-tested.
9. **Layout.** These choices aren't in the docs:
   - The money counter, the ticket and the Directory button sit in the free column right of the board (x 620–960 of the 1920×1080 reference).
   - Stamps show on the card for 1.5 s.
   - The directory overlay is modal and also closes on a backdrop click.
10. **TMP import.** The menu item opens Unity's interactive import dialog, which an agent can't click. I closed the dialog and ran the same import silently with `TMP_PackageResourceImporter.ImportResources(true, false, false)`.

Follow-ups:
- **Plan §4 (simulated input).** `execute_code` runs in an editor context, where `Screen.width/height` give an editor view's size (819×694 here). So `ProcessPointer` and EventSystem raycasts called directly from it hit nothing. What works: queue a one-shot `Canvas.willRenderCanvases` handler from execute_code, run the clicks inside it (inside the player loop, `Screen` is 1920×1080), and read results back through `SessionState`.
- **Plan §4/§5 (smoke test).** With Run In Background off, as the design requires, Play Mode stops advancing whenever Unity isn't the foreground app.
  - What works: a self-removing `EditorApplication.update` callback that calls `EditorApplication.Step()` drives frames without touching settings. Each step is about 0.02 s; I set `Time.captureDeltaTime` to 0.05 at runtime and reset it to 0 afterwards.
  - Don't use `Application.runInBackground` for this, because in the Editor it writes `PlayerSettings.runInBackground`. I flipped it once by mistake and set it back to false within seconds. `ProjectSettings.asset` on disk is unchanged (`runInBackground: 0`).
- **Screenshots** are reliable when taken while play mode is paused or stepping; see the blank capture above.
- **Clipped top slot.** The top caller slot is partly off-screen: the team's panel at y 8 × 1.2404 reaches y 541 of 540. The ringing halo is clipped at the top edge. Raise with Sergei if it bothers the playtest.
- **Stale held cable.** After the desk returns a cord that `CableInputController` still holds (a pulled plug cuts the call off), the controller clears its stale `heldCable` only on its next `ProcessPointer`. A socket click in that exact frame could plug the returned cord without picking it up first. This is unlikely, and D07 in Step 2 replaces this path with restore.
- **Inactive cables.** CordBridge numbers inactive CableControllers too. Revisit with the Step 5 runtime-cords coordination.
- **Placeholder portraits.** All 12 portraits are the same circle, so callers differ only by rate and ticket until real art lands.
- **Arrow glyph.** If the "→" ticket is wanted, add the glyph to a static font asset in Step 3.

#### Step 1 verification (orchestrator, 2026-10-17)
- **Tests:** an independent EditMode run passed 72/72. The console is clean.
- **Scope:** the only tracked change is `ProjectSettings/EditorBuildSettings.asset` (Game 0, Level 1), as the brief asked. No team file changed. `ProjectSettings.asset` is unchanged, so `runInBackground` is still 0. Screenshots stayed in `Temp/Captures`.
- **Code review:** the bridge, installer, session, call service, spawner, factory, earnings and views match the design.
  - The tick order is right.
  - Events are raised after state changes, and calls end outside the iteration over the live list.
  - Views poll and redraw only when a value changes.
  - The bridge skips stale reports when the desk moves a cord mid-report, and re-reads it the next frame.
- **Deviations:** all accepted and recorded in Design.md (status table, 4.3, 5.2, 5.4, 5.8, 6.1).
- **Not done by the orchestrator:** a separate Play Mode run. Play Mode freezes while Unity is in the background, and the agent already stepped every scenario through real raycasts. The lead's playtest covers it.
- **Follow-ups, added to the Step 2 brief:**
  - D03 swap in `CallService.Take`.
  - Hold instead of cut-off.
  - `LiveCallsView` in the HUD column.
  - An error log when `Level` fails to load.
- **Testing techniques** (Play Mode stepping, input simulation, screenshot timing) were added to the Plan §4 cheat sheet.

#### Step 1 playtest (lead, 2026-10-17)
- All 9 checks passed.
- **New request: a connection fee.** On a correct connection, the caller pays a fixed amount once, then the per-second rate. This is not in the original spec. It is planned as Step 1b, before Step 2.

#### After Step 1 (orchestrator, 2026-10-17)
- **Commit:** Step 1 committed as `e6f44e5`, then merged with 9 teammate commits (`8af640d`, no conflicts).
- **TextMeshPro:** Sergei had also imported it. His copy has the same asset GUIDs, so our duplicate was dropped before the merge.
- **After the merge:** compiles cleanly, 72/72 EditMode tests pass.
- **New team work that overlaps the plan:**
  - Phone book: `PhoneBookView` with `Configs/PhoneBook.json`, 24 contacts on A1–D6. Overlaps Step 3 and our directory data.
  - Upgrade shop panel: overlaps Step 5.
  - Main menu scene and menu tools: overlap the Step 6 Boot scene.
  - A sorting helper: book pages at sort order 10, held plug and cursor at 20.
  - The division of work needs agreeing with the lead and Sergei before Step 3.

#### Decisions after the Step 1 playtest (lead, 2026-10-17)
- Recorded in Design.md as D30–D33:
  - D30: Sergei builds the UI, we build the rules and wire his components.
  - D31: one directory (our data, shown in his book, English names, portraits on the card).
  - D32: connection fee, kept as simple config.
  - D33: scene flow `MainMenu` → `Game` → `Level`.
- **Second merge (`9881486`):** the only conflict was the build list. Resolved to `MainMenu` (0), `Game` (1), `Level` (2), because his menu loads the next build index.
- **Playtest overlaps:** our money counter, ticket and Directory button overlapped Sergei's upgrade panel. Step 1b fixes this and replaces our Directory list with his book.

### Step 1b report: 2026-10-17
Result: done

Playable now: Start from `MainMenu` (Play) or `Game.unity`. A call rings with a fee and a rate on its badge (e.g. "9¢ + 3¢/s"). Take it, and a speech bubble left of the caller's portrait says "Walter Pryce / wants Lillian Yardley". Sergei's phone book lists our 12 English names with their jacks. A correct patch pays the fee at once with a "+9¢" popup over the card, the bubble switches to "On the line", and money rises per second. The "Paid" stamp includes the fee. The temporary Directory button, list and D key are gone.

How to test: the Plan's Step 1b playtest script, with these notes:
1. The money counter sits at the top of the right-hand column, in the empty header strip of the upgrade panel above its first "+" button (see deviation 1).
2. The speech bubble sits in the dark margin left of the caller column, its tail pointing at the top slot's portrait. Long names wrap onto several lines.
3. The "+N¢" popup rises over the card, not near the money counter.

Created:
- **Content** (`Assets/Scripts/Content/`):
  - `PeopleFileData` (the JSON format: Sergei's `contactsPerPage`/`name`/`socket` plus `portrait`, `type`, `rate`, `fee`)
  - `PeopleFileLoader` (parse and validate with `JsonUtility`), `PeopleCatalog` (valid entries plus error messages)
  - `PersonRecord`, `CallerRecord`, `DirectoryRecord` (plain classes implementing `IPerson`, `ICaller`, `IDirectoryEntry`)
  - `GameContent` (`IGameContent` = GameConfig settings + loaded people)
- **Game:** `Views/FloatingTextView` (the reused "+N¢" popup).
- **Tests:** `ConnectionFeeTests`, `PeopleFileTests`.

Changed:
- **Domain:**
  - `ICallerType.ConnectionFeeCents` (range), `ICaller.ConnectionFeeCentsOverride`.
  - `CallRequest.ConnectionFeeCents` (constructor: `…, connectionFeeCents = 0, attempt = 1`).
  - `Call.ConnectionFeePaid`.
  - `CallFactory` rolls the fee like the rate; an override > 0 wins.
  - `EarningsService` pays the fee × multiplier once, the moment the call enters Connected (it listens to `CallStateChanged`, so tick order doesn't matter), adds it to `CentsEarned`, and raises `ConnectionFeePaid`. It takes an optional multiplier delegate (default 1) for Step 5.
  - `GameSession.Earnings` is public.
  - `MoneyFormat.Cents` ("10¢") and `MoneyFormat.CardRate` ("10¢ + 5¢/s"; just "5¢/s" when the fee is 0).
  - `PersonExtensions.FullName`/`ListingName` handle a one-word name.
- **Content:** `CallerTypeDefinition.connectionFeeCents` (default 5–15). `GameConfig` now holds `startingCents`, `callerTypes`, `peopleFile` (TextAsset) and `portraits` (name → Sprite), with `LoadPeople(boardJacks)` and `Portrait(name)`.
- **Game:**
  - `GameInstaller` builds the content from the people file and logs each validation error with `Debug.LogError`. It reads the board's jack labels from the `SocketView`s in Level. It logs an error if `Level` fails to load. It passes portraits and earnings to the queue.
  - `CallQueueView` forwards `ConnectionFeePaid` to the card.
  - `CallCardView`: fee + rate badge, speech bubble instead of the ticket, fee popup.
- **Assets:**
  - `Assets/Configs/PhoneBook.json` rewritten (D31): our 12 people, sorted by surname, `contactsPerPage` 6, with `portrait` `caller-NN`, `type.regular`, `rate` 0, `fee` 0. Jacks and portraits are the same as in Step 1.
  - `Regular.asset`: fee 5–15.
  - `GameConfig.asset`: references `PhoneBook.json` and the 12 portraits.
- **Tests:** `Fakes` (fee fields; TestWorld's Regular has a fee of 5–15), `CallFactoryTests`, `MoneyFormatTests`, `GameSessionTests` (expects fee + rate × duration).
- **Deleted:**
  - `PersonDefinition`, `CallerDefinition`, `PhoneDirectoryDefinition`, `DirectoryEntry`, `DirectoryListView` (scripts and metas)
  - `Assets/Content/People/`, `Assets/Content/Callers/`, `PhoneDirectory.asset`

Scene and prefab changes:
- **`GameUI.prefab`:**
  - Removed `Directory` (overlay and button) and `Ticket`.
  - `Hud/MoneyPlate` moved to (762, 486) at 212×52, with the amount auto-sized 22–40. That is x 1616–1828 and y 1000–1052 in the 1920×1080 reference. Sergei's `WorkArea` is a fixed 1920×1080 rect centred on the canvas, so centre-anchored offsets line up with his panels.
  - `RateBadge` widened to 152×40, label auto-sized 16–28.
  - New `CallCard/SpeechBubble` (124×140, right edge 38 px left of the card, so x 16–140 for the top slot; a rotated-square tail and a cream rounded body; label auto-sized 14–24).
  - New `CallCard/FeePopup` (CanvasGroup with FloatingTextView: rises 48 px and fades over 1 s).
  - Every new graphic has `raycastTarget` off.
- **`Game.unity`:** re-saved. The stale `directoryList` field and the stripped DirectoryListView reference are gone. TMP added `m_AdditionalShaderChannelsFlag: 25` overrides on the GameUI and Hud canvases.
- **Team scenes:** `Level.unity` and `MainMenu.unity` were opened for Play Mode checks and closed unsaved; neither was ever dirty. `git diff` is empty for every team path, ProjectSettings and Packages. `runInBackground` is still 0.

Tests: 91/91 EditMode (72 before, 19 new). New:
- ConnectionFeeTests: 5
  - fee paid once, at the moment of connection, before any tick
  - no fee on a wrong connection
  - multiplier applied to the fee
  - a finished call earns fee + duration × rate (112¢ with uneven frames)
  - a zero fee raises no event
- CallFactoryTests: 2 (fee within range, override wins)
- MoneyFormatTests: 5 (Cents ×2, CardRate ×3)
- PeopleFileTests: 7
  - the real file through the real GameConfig: 12 valid, no errors
  - the real file is sorted by surname
  - the real file keeps the book's format
  - parsing names, jacks, types and overrides
  - invalid entries reported and left out: unknown jack, duplicate jack, unknown portrait, unknown type, empty name
  - empty file and broken JSON reported

Console: clean. Compiling, both test runs and every Play Mode run logged zero errors and zero warnings from our code. The only lines are the known Metal screenshot noise ("Ignoring depth surface load/store action as it is memoryless") and one MCP WebSocket line during a domain reload.

Screenshot: `Temp/Captures/step-01b.png` shows a connected call: cord D2–B1, bubble "On the line", popup "+9¢", money counter $0.09 above the upgrade buttons, book closed. Extras: `step-01b-ringing.png` (book closed, ringing card "9¢ + 3¢/s") and `step-01b-book-open.png` (book open showing our 12 people; bubble "Walter Pryce wants Lillian Yardley").

Checked in Play Mode with real EventSystem raycasts and `CableInputController.ProcessPointer`, run inside the player loop:
- **Ring:** Walter Pryce → Lillian Yardley, fee 9, rate 3, portrait caller-06 (Walter's).
- **Take:** the card is the topmost hit; the bubble shows the names at x 16–140, y 912–1052, clear of the board and the other slots.
- **Book:** opens from a click on `ClosedBook` and lists all 12 people across one spread. Raycasts at four points over the open book all hit `OpenBook` (order 10), so nothing of ours covers it. A click on it closes it.
- **Right pair** D2–B1: the wallet went from 0 to 9 in the same frame (fee 9 plus a fraction); "+9¢" popup; "On the line". The call finished at 99¢ exactly (9 + 3 × 30), with the stamp "Paid $0.99" and wallet 99.
- **Wrong pair** (Basil Thorne wants Edgar Pennington, patched A5–F6): "Wrong number!", wallet stayed 99, call earned 0, no fee event.
- **Flow:** `MainMenu` → Play → `Game` (index 1) → `Level` (index 2) loads additively; the session has 12 people and the book has 12 contacts.
- **`Level.unity` alone:** one scene, none of our objects, a sandbox patch A1–F6 stays connected, and the book shows the 12 people.

Deviations from Design/Plan:
1. **Money counter is not strictly above the shop.** At 1920×1080, Sergei's `UpgradeShopPanel` spans y 204–1064, which leaves 16 px above it, and the closed book fills the column below it. I put the counter at the top of the right-hand column, inside the panel's empty header strip (y 1000–1052) above the first upgrade button (top at 994). It covers no button and has raycasts off, but it does sit on his panel's faceplate. A strict "above, no overlap" needs Sergei to move the panel down by about 60 px (a coordinated change), or a different spot for the counter.
2. **Bubble side and size.** There are only 86 px between the caller column and the board, so the bubble goes in the 140 px margin left of the column, pointing right at the portrait. At 124 px wide, names wrap ("Walter / Pryce / wants / Lillian / Yardley"). It replaces the names with "On the line" once connected, as the brief says.
3. **`GameConfig` no longer implements `IGameContent`.** The people come from JSON, and caching parsed data on a ScriptableObject would be runtime state on an asset. A plain `GameContent` (Content) combines GameConfig with the loaded `PeopleCatalog`. Design 6.1 still says GameConfig implements it.
4. **Validation extras beyond the brief:** an empty jack, a duplicate name (the id is derived from the name), a negative rate or fee, and an empty or unreadable file. Invalid entries are left out and the game runs with the rest. Ids are `person.<name-words>` / `caller.<name-words>`, the same ids as Step 1.
5. **Board jacks for validation** come from the live `SocketView.Address` values at runtime. The EditMode test uses an A1–F6 grid matching the current board.
6. **Card badge** shows only "5¢/s" when the fee is 0.
7. **Tooling: C# edits made with file tools.** MCP `apply_text_edits` corrupted three edits: it duplicated a line in CallFactory, dropped the new property from CallRequest, and reported a false no-op on Call.cs. After that I wrote C# with file tools, followed each batch with `refresh_unity` compile and a console read, and checked every file on disk. Assets, the prefab, the scene, deletions and tests all went through MCP. `delete_script` isn't accepted inside `batch_execute`; I used `manage_asset delete` instead.
8. **Canvas order.** `GameUI` stays at order 10, the same as the book. The book is still on top only because nothing of ours overlaps it.

Follow-ups:
- **Tell Sergei:**
  - `PhoneBook.json` now carries the game's people. Keep the extra fields and the sorting by surname.
  - His `PhoneBookBuilder` menu loads `Assets/Settings/PhoneBook.json`, which no longer exists, so "Update Phone Book Directory" would throw.
  - Ask whether the shop panel can leave room above it (deviation 1).
- **Sorting:** consider order 5 for `GameUI` (above the board at 0, below the book at 10) before Step 2 adds live calls in the right column, so the open book is always on top.
- **Plan §4 testing notes:**
  - `Time.captureDeltaTime` seemed to have no effect while stepping; each `EditorApplication.Step()` advanced about 0.017–0.02 s. A temporary `Time.timeScale` (5–10, reset to 1) ran a 30 s call in 150 steps.
  - `GameObject.Find("A/B/C")` also returns inactive children, so probes should check `activeInHierarchy`.
- **Bubble readability:** with only 140 px of margin, long names wrap. Options: a smaller fixed font with one name per line, or shorter text such as "Pryce → Yardley" once an arrow glyph is in the atlas.
- **Placeholder portraits:** all 12 are still the same grey circle.

#### Step 1b verification (orchestrator, 2026-10-17)
- **Tests:** an independent EditMode run passed 91/91.
- **Console:** clean.
- **Scope:** no team scripts, prefabs, scenes, ProjectSettings or Packages changed. `PhoneBook.json` was rewritten as agreed in D31.
- **Screenshot `step-01b.png` shows:**
  - the fee badge and "+9¢" popup
  - the "On the line" bubble left of the card
  - the money counter in the shop panel's header strip
  - the phone book on the desk
- **Docs:** Design status updated; Plan §3/§4 now allow C# file edits (because of the MCP edit corruption) and include the stepping notes.
- **Open layout decisions for the lead:**
  - the money counter sits in the shop panel's header strip (strictly above it would need Sergei to move the panel)
  - the bubble sits in the left margin
- **For Sergei:**
  - `PhoneBook.json` now carries game fields.
  - His `PhoneBookBuilder` points to a missing `Assets/Settings/PhoneBook.json`.

### Step 1c report: 2026-10-17
Result: done

What changed: the upgrade panel's top now has a 204×96 header plaque. It is empty of buttons and serves as the money counter's slot. The counter fills that plaque, centred, in the same gold bold LiberationSans as before. "$0.00" now shows at 63 pt (it was 40, so about 1.6×). The four "+" buttons sit lower, with tighter spacing. The panel's outer rect is unchanged: 236×860 at (762, 94), so x 1604–1840 and y 204–1064 at 1920×1080. It still clears the board (right edge 1580) and the closed book (top 198).

Panel layout at 1920×1080 (screen px, y up):
- Header plaque `HeaderSlot`: x 1620–1824, y 952–1048. It sits 8 px inside the faceplate top, in the frame colour (46, 50, 46). A 3 px dark shade line (25, 28, 26) runs along its top, so it reads as recessed. A 2 px highlight (145, 149, 137) runs along its bottom, the same colour as the frame highlight. It has no raycast target.
- Buttons: 144 px each, 32 px apart (was 48). The first top is at 920, 32 px below the plaque. The last bottom is at 248, 36 px above the faceplate bottom (212).

Money counter:
- `Hud/MoneyPlate` lost its own dark background, because the plaque is the background now.
- In the prefab it sits at (762, 460), 204×96, the plaque's reference position.
- `Amount` auto-sizes from 28 to 64 with margins of 14 px left and right and 8 px top and bottom. "$99,999.99" fits at 31 pt with no overflow (text bounds 178 px of 176 + margins).

Runtime alignment, the same way `CallQueueView` follows the caller slots:
- `MoneyCounterView` has a new `plate` field and `Bind(wallet, headerSlot)`. Each `LateUpdate`, it reads the slot's world corners (screen px, since both canvases are Screen Space Overlay) and sets the plate's centre and size whenever they change.
- **Lookup:** `LevelScene.FindShopHeader(level)` searches the Level scene's roots for the object named `UpgradeShopPanel`, then its direct child named `HeaderSlot`. The panel has no component of its own, so this is a name lookup, like `CallerQueuePanel`. If either name is missing, it logs a warning and the counter keeps its fixed prefab position (which still matches at 1920×1080).
- **Measured in Play Mode:** `MoneyPlate` equals `HeaderSlot` to 0.1 px at four sizes:
  - 1920×1080
  - 1366×768 (scale 0.711)
  - 2560×1440 (scale 1.333)
  - 2560×1080 (21:9, a 2560-wide canvas): slot x 1940–2144, panel 1924–2160, board right edge 1900, book x 1830–2190 y 12–198.

Files touched (ours):
- `Assets/Scripts/Game/Views/MoneyCounterView.cs`: `plate` field, `Bind(Wallet, RectTransform)`, `FollowSlot()`.
- `Assets/Scripts/Game/LevelScene.cs`: `FindShopHeader` plus the two name constants.
- `Assets/Scripts/Game/GameInstaller.cs`: passes `LevelScene.FindShopHeader(level)` to the counter.
- `Assets/Prefabs/Game/GameUI.prefab`: the `MoneyPlate` rect, its Image and CanvasRenderer removed, `Amount` size/margins, the `plate` reference.
- `Docs/Progress.md` (this report).

Team files touched (approved coordinated change, Sergei's panel):
- `Assets/Prefabs/UI/UpgradeShopPanel.prefab`:
  - added `HeaderSlot` (with children `HeaderShade` and `HeaderHighlight`) right after `FrameHighlight`
  - `UpgradeSlots` VerticalLayoutGroup: padding top 0 → 144, spacing 48 → 32, alignment MiddleCenter → UpperCenter
  - The existing objects' file IDs are unchanged, so `Level.unity`'s instance still resolves.
- `Assets/Editor/UpgradeShopPanelBuilder.cs`:
  - new constants `HeaderSize` (204×96), `HeaderY` (366), `SlotsTopPadding` (144) and `SlotSpacing` (32)
  - `CreatePanelPrefab` builds the plaque and sets the same padding, spacing and alignment
  - Re-running the builder on a missing prefab gives the same result.
- `Level.unity`: **not changed.** Its panel instance overrides only the root rect (position 762, 94; size 236×860, the same as the prefab) and the layout-driven button rects (zeros, which the layout group recomputes). Nothing undoes the prefab change. `Game.unity` overrides only the GameUI root and canvas flags, so it wasn't touched either.

Tests: 91/91 EditMode passed. No new tests: this step is layout only, and the alignment is view code.

Console: clean after compiling and after the test run. The Play Mode run logged only MCP tooling lines:
- the known Metal "memoryless" warnings
- one "PlayerLoop internal function has been called recursively" error, whose stack trace is entirely inside the MCP `ScreenshotUtility.CaptureCompositedAfterFrame` (a screenshot taken while stepping), not our code

Screenshots (`Temp/Captures/`, taken while stepping):
- `step-01c-zero.png` (1920×1080): "$0.00" at 63 pt in the plaque, buttons below.
- `step-01c-large.png` (1920×1080): "$99,999.99" at 31 pt. I set it by calling `Wallet.Earn` through `execute_code` during Play Mode only. No code was added.
- Extras: `step-01c-qhd.png` (2560×1440) and `step-01c-wide.png` (2560×1080, 21:9). Both show the counter in the plaque.

Other Play Mode checks at 1920×1080, using EventSystem raycasts inside `willRenderCanvases`:
- Each "+" button centre hits that button first, with no hit from our canvas.
- The plaque centre hits only Sergei's `Faceplate`, so our counter catches no clicks.

Deviations:
1. **Runtime alignment rather than fixed position.** A fixed centre-anchored position would line up today, because both canvases use Expand and `WorkArea` is a centred 1920×1080 rect. Following the rect also survives Sergei moving or resizing the panel, and it matches how `CallQueueView` follows the caller slots.
2. **A second name lookup into Level** (`UpgradeShopPanel/HeaderSlot`). Design §3 lists `CallerQueuePanel` as the only exception to "find by type". Ask Sergei to keep both names.
3. **Large values shrink.** Auto-size goes from 64 down to 31 pt for "$99,999.99", and to about 28 pt for "$999,999.99". A fixed size that fits five-digit dollars would make "$0.00" small, so auto-size won.
4. **Wider-aspect test tooling.** To test 21:9, I added a temporary "Step1c temp 21:9" Game view size through `GameViewSizes` reflection. I removed it afterwards and re-selected Full HD (1920×1080). That list is per-user editor state, not project state.
5. `Level.unity` was not opened on its own this step. The prefab change only adds a raycast-off plaque and moves buttons inside the same panel, so the cord sandbox is unaffected.

Follow-ups:
- Tell Sergei: his panel now has a `HeaderSlot` plaque that holds our money counter. Keep the names `UpgradeShopPanel` and `HeaderSlot`, and keep the slot free of buttons. The builder constants are at the top of `UpgradeShopPanelBuilder`.
- Design §4.3 still says the counter sits "above" the shop panel. It now sits in the panel's header slot, so the lead may want to update it (and §3's name-lookup exception).

#### Steps 1b and 1c playtest (lead, 2026-10-17)
- All checks passed. Both steps are pushed.

### Step 2a report: 2026-10-17
Result: done

Playable now: Press Play in `Game.unity` (or start from `MainMenu`). The first caller rings after about 2 s. More callers then ring about every 8 s (±30 %), one card per caller slot, never more than 6 live calls; connected calls count too. Click any ringing card to take it. Taking another card swaps them: the old card rings again, and the speech bubble moves to the new one. Plugging the first end of a cord puts the call in hand into Connecting. A wrong patch shows "Wrong number!", and the same caller rings again 20–40 s later with a "Calling again" badge. Several calls can be connected at once. Each connected card shows a ring in its cord's colour, its own earnings ("$0.29") and an "On the line" bubble. F1 or the small "Dev" button in the top-right corner opens the dev panel.

How to test: the Plan's Step 2a playtest script, with these notes:
1. The dev panel is a narrow column of small buttons at the far right edge, right of the shop panel: Close, + $10, Call now, Speed ×5 (or ×1), Show times (or Hide times), Clear calls. While the panel is closed and time is ×5, the corner button reads "Dev ×5".
2. "Show times" puts the hidden duration on every card. Waiting cards show the full duration; connected cards count down.
3. When a card leaves (after its stamp), the cards below it slide up one slot, so the column always stays in arrival order.

Created:
- **Domain:**
  - `Callbacks/CallbackScheduler.cs` (plus the `PendingCallback` struct)
  - `Spawning/BusyTracker.cs`, `Spawning/PacingSettings.cs` (plain defaults: 2 / 8 / 0.3 / 0.5)
  - `Definitions/IPacingProfile.cs`
  - `Common/RandomExtensions.cs` (`IRandom.Between`)
- **Content:** `PacingProfile.cs` (ScriptableObject: `firstCallDelaySeconds` 2, `baseIntervalSeconds` 8, `intervalJitter` 0.3, `loadFactor` 0.5).
- **Game:** `DevTools/DevPanel.cs`. The whole file is inside `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- **Assets:** `Assets/Content/Pacing/Default.asset`.
- **Tests:** `CallTransitionTests.cs`, `CallbackSchedulerTests.cs`, `BusyTrackerTests.cs`.

Changed:
- **Domain:**
  - `CallService`:
    - `Take` swaps (D03). The old call goes back to Incoming, and both state changes are raised after both states are set.
    - New `BeginConnecting` (Taken → Connecting) and `CancelConnecting` (Connecting → Taken).
    - `Connect` accepts Taken or Connecting.
    - New `IsInLiveCall(person)`.
  - `CallRequest`: `Involves(person)` and `Again()` (the same request, attempt + 1).
  - `CallEndReason.Cleared = 5`.
  - `ICallerType.CallbackDelaySeconds`; `IGameContent.MaxWaitingCalls` and `Pacing`.
  - `OperatorDesk`:
    - tracks half-patched cords from the bridge's reports
    - keeps the call in hand in Connecting exactly while any cord is half-patched, including straight after a take
    - new `HalfPatched` list and `ReturnHalfPatchedCords()`
    - Pulling a plug still cuts the call off, as in Step 1.
  - `CallSpawner` rewritten (pacing, cap, busy rule, `TrySpawn`, `Load`, `NextInterval`).
  - `GameSession`:
    - builds the scheduler, the busy tracker and the spawner
    - tick order: clock, earnings, calls, callbacks, spawner
    - new `ClearCalls()`
    - exposes `Callbacks`, `Busy` and `Spawner`
- **Content:**
  - `CallerTypeDefinition.callbackDelaySeconds` (default 20–40).
  - `GameConfig`: `maxWaitingCalls` (6) and `pacing`.
  - `GameContent` passes both through.
- **Game:**
  - `CordBridge.ColorOf(CordId)` returns `CableController.CableColor`, for presentation only.
  - `CallQueueView` rewritten. At bind it makes one card per slot by copying the template card. Cards are laid over all 6 slots in arrival order and slide up when a card leaves. `ShowDurations` is a property the dev panel sets.
  - `CallCardView`:
    - the bubble shows while the call is Taken, Connecting or Connected
    - cord ring, callback badge, earnings badge and dev duration badge
    - a `Finished` event when the stamp ends
    - Cleared calls leave at once.
    - Earnings and durations are polled in `LateUpdate`, and their text changes only when the shown number changes.
  - `GameInstaller`:
    - passes `cordBridge.ColorOf` to the queue
    - warns if the panel has fewer slots than `maxWaitingCalls`
    - adds `DevPanel` at runtime inside the `#if`, so no prefab or scene references a dev-only type
- **Assets:**
  - `Regular.asset`: `callbackDelaySeconds` 20–40.
  - `GameConfig.asset`: `maxWaitingCalls` 6, `pacing` → `Pacing/Default`.
- **Tests:** `Fakes` (a `Crowd(n)` helper; caller-type callback delay; content cap and pacing; a settable `CordCount`), `CallServiceTests`, `CallSpawnerTests` (rewritten), `OperatorDeskTests`, `EarningsServiceTests`, `GameSessionTests`.

Scene and prefab changes:
- **`GameUI.prefab`:**
  - New objects under `Calls/CallCard`, all with `raycastTarget` off:
    - `CordRing`: a Knob circle behind the portrait, 34 px larger than the card, tinted with the cord colour at runtime.
    - `CallbackBadge`: "Calling again" in amber.
    - `EarningsBadge`: gold money text.
    - `DurationBadge`: centred over the portrait, 84×34, for the dev panel.
    - The two top badges are 140×30, centred 30 px below the card's top, so they stay inside the card and clear of the rate badge of the card above.
  - Sibling order: RingingPulse, CordRing, Portrait, RateBadge, CallbackBadge, EarningsBadge, DurationBadge, Stamp, SpeechBubble, FeePopup.
  - `CallQueueView.card` was renamed to `cardTemplate` (with `FormerlySerializedAs`), and `slideSpeed` 14 was added.
- **`Game.unity`:** not changed; its overrides don't touch the renamed field.
- **Team scenes:** `Level.unity` was opened alone for the sandbox check and closed without saving; it was never dirty. `MainMenu.unity` and `SampleScene.unity` were not opened. The test runner did not re-save `SampleScene`. `git status` shows no team file, ProjectSettings, Packages or `PhoneBook.json` change. `runInBackground` is still 0.

Tests: 156/156 EditMode (91 before, 65 new):
- CallTransitionTests: 28
  - 25 matrix cases (6 verbs × 5 states, table-driven with `TestCaseSource`), including the refusals
  - swap from Taken and from Connecting
  - taking a card leaves connected calls alone
- CallbackSchedulerTests: 11
  - the delay and the same request with attempt + 1
  - the delay rolled within the range
  - only WrongConnection calls back (4 cases)
  - waits while every slot is used
  - waits while a party is on another call (the constructed guard case)
  - due order, and a blocked callback doesn't hold the others back
  - callbacks go ahead of spawns
  - Clear
- BusyTrackerTests: 6 (busy in Incoming, Taken, Connecting and Connected; free after the end; a pending callback makes both parties busy)
- CallSpawnerTests: 12 (7 more than Step 1)
  - the first-call delay and the base interval
  - jitter bounds
  - load values and the load factor stretching intervals (20 s, then 30 s for a 10 s base)
  - the cap (6 and 4) and connected calls counting toward it
  - no busy person picked over 4,000 ticks
  - an attempt with nobody free is skipped quietly and retried
  - pending-callback people are not picked
  - callers missing from the directory never call
- OperatorDeskTests: 9 more
  - first plug → Connecting, and back when the cord returns
  - taking a call while a cord is half-patched → Connecting
  - swapping while Connecting
  - right and wrong pairs from Connecting
  - stays Connecting while another cord is half-patched
  - an opened circuit with no call stays half-patched
  - `ReturnHalfPatchedCords`
  - several calls each hold their own cord
- EarningsServiceTests: 1 (three calls at 3, 5 and 7 ¢/s pay together, exactly 340 ¢ in all)
- GameSessionTests: 3 (the queue piles up to 6 and stops; a wrong number calls back through the session; `ClearCalls` ends everything as Cleared, with no pay, no callbacks, cords returned and half-patched cords returned)

Console: clean. There were no errors or warnings from our code while compiling, in both test runs, or in the Play Mode runs. The only lines were the known MCP noise: the Metal "memoryless" lines from screenshots, and one MCP WebSocket line during a domain reload.

Screenshots (`Temp/Captures/`, taken while paused or stepping):
- `step-02a.png` (main): three connected calls with red, blue and yellow cord rings, earnings $0.29, $0.25 and $0.24, "On the line" bubbles, the wallet at $0.78 (= 29 + 25 + 24), and two ringing cards below.
- `step-02a-full-queue.png`: 6 ringing cards; the cap held for about 100 s of game time.
- `step-02a-callback.png`: a "Calling again" card.
- `step-02a-dev-panel.png`: the open dev panel and durations on the cards.
- `step-02a-book-open.png`: the open book over the board, with 6 cards and a names bubble.
- `step-02a-before.png`: the layout before this step.

Checked in Play Mode with real EventSystem raycasts and `CableInputController.ProcessPointer`, one action per rendered frame, inside `Canvas.willRenderCanvases`, with a temporary `Time.timeScale` reset to 1 afterwards:
- **Queue:**
  - Callers rang at about 2, 10 and 19 s, all six with different people (busy rule).
  - Load rose to 2 and 2.5 as the queue filled.
  - The queue reached 6 at about 66 s and stayed at 6 until about 162 s.
  - Cards line up with `CallerSlot_01`…`_06` exactly: x 178–336, y 903–1062 down to 34–193.
- **Swap:** take card 2, then card 3. Card 2 went back to Incoming with no bubble; card 3 is Taken with the bubble "Ada Moss / wants Norman Tulley".
- **Connecting:** after the first plug the call in hand went Taken → Connecting, and to Connected on the right second jack.
- **Wrong patch:** "Wrong number!", a pending callback due 35 s later (inside 20–40). At the due time it rang as a new call (attempt 2) with the same people, rate and fee, showing "Calling again".
- **Three at once:** three calls on cords 0, 1 and 2, each card in its own cord colour with its own earnings. The wallet always equalled the floor of the three calls' total (33, 65 and 78 ¢ at three checks).
- **Bubbles:** every bubble sits at x 16–140, inside its own card's vertical band; for example the card at y 903–1062 has its bubble at y 912–1052.
- **Dev panel actions:**
  - + $10: 406 → 1406 ¢.
  - Call now: 4 → 5 calls. It respects the cap and the busy rule, because it is the spawner's `TrySpawn`.
  - Show times works.
  - Speed ×5 set `timeScale` 5; leaving Play Mode set it back to 1, through `OnDisable`.
  - Clear calls: with one call connected and one Connecting on a half-patched real cord, all three cables went back to Ready, the live list and the pending callbacks were empty, the wallet was unchanged, and every card left. The cord controller held no stale cable afterwards.
- **Layering:** with the book open, raycasts at five points over `OpenBook` (x 1050–1870, y 16–540) all hit `OpenBook`, and every card centre still hits its own portrait. Nothing of ours overlaps the book, so `GameUI` stays at sorting order 10.
- **`Level.unity` alone:** one scene, no `GameInstaller` and no `DevPanel`; a sandbox patch A1–F6 stays Connected.

Deviations from Design/Plan:
1. **No `LiveCallsView`** (orchestrator decision 1). Connected calls keep their card in their caller slot. Design 5.4's `LiveCallsView` is superseded.
2. **`CallEndReason.Cleared = 5`** is new. The dev panel's "clear calls" needed an ending that pays nothing, fines nothing (Step 4) and never calls back; `DisconnectedEarly` would be fined in Step 4. Cleared cards leave at once, without a stamp.
3. **`CallService.CancelConnecting`** (Connecting → Taken) is a new verb next to Design 5.1's list. `BeginConnecting` and `CancelConnecting` only act on the call in hand.
4. **Spawner pacing** (decision 6, my formula):
   - `load = 1 + loadFactor × max(0, waiting − freeCords)`, where waiting is the live calls that aren't Connected, and freeCords is `CordCount` minus the connected calls.
   - The timer runs at `dt / load`, so the stretch applies live and eases as soon as the player catches up.
   - Each interval is `base × U(1 − jitter, 1 + jitter)`, at least 0.1 s.
   - An attempt that finds no room, or no free caller and recipient, is skipped, and the next attempt comes a fresh interval later. Calls never queue up "owed".
   - `PacingProfile` also holds `firstCallDelaySeconds` (2).
5. **Slots** (decision 3):
   - Every live call holds a slot, in arrival order.
   - When a card leaves after its stamp, the cards below slide up, using an exponential lerp on unscaled time.
   - If a new call needs a slot while a stamp is still showing, the oldest stamp is cut short and gives up its slot.
6. **Callbacks:** due callbacks ring in due order. One that is blocked because a party is busy is skipped for now, and later due ones may still ring. Room is the same cap of 6 live calls.
7. **Fallbacks:** `GameSession` uses 6 calls when `MaxWaitingCalls` ≤ 0, and `PacingSettings.Default` when no profile is set.
8. **Dev panel is IMGUI**, at the far right edge (x about 1843–1917 at 1920×1080, from the top down to y about 800 px), clear of the board, the tray, the shop buttons and the open book.
   - IMGUI doesn't block uGUI raycasts. A click on a dev button therefore also reaches the cord input as an "empty space" click, so a held cord would restore or go back to the tray. Nothing clickable of Sergei's is under the column.
   - "×" is drawn by IMGUI's own font, not TMP.
9. **Card visuals:**
   - The connected cord colour is a ring around the portrait. Earnings ("$0.29") and "Calling again" share the top badge position; they never show together, because a callback card in Connected shows earnings.
   - Every connected card shows an "On the line" bubble (decision 8).
10. **F1 and the real IMGUI button clicks were not driven by me.** The project uses `editorInputBehaviorInPlayMode = PointersAndKeyboardsRespectGameViewFocus`, so keyboard events queued while the Game view isn't focused go to the editor, not to Play Mode. I opened the panel through its field for the screenshot and ran each button's action through the same call the button makes. The lead's playtest covers the real F1 and clicks.

Follow-ups:
- **Step 2b (hold):** `OnCircuitOpened` on a bound cord still ends the call. Once it puts the call on hold, decide whether that cord's single plug counts as half-patched for the call in hand; I'd say no, since the cord belongs to the on-hold call. Today the desk only marks a cord half-patched when no call is bound to it.
- **Ringing halos** (the big amber pulse) of neighbouring ringing cards touch in the 15 px gap between slots, and the top one is clipped by the screen edge, as before. This is cosmetic.
- **Pool of people:** with 12 people each live call uses two, so pending callbacks (which keep both parties busy) can hold the queue below 6. This is expected under D18; pacing in Step 7 may want more people.
- **Plan §4 notes:**
  - `execute_code` compiles with CodeDom (C# 6): no local functions, and `Object` is ambiguous (write `UnityEngine.Object`).
  - `manage_camera screenshot` adds "-1" to the name when the file exists, instead of overwriting it.
  - Simulated keyboard input doesn't reach Play Mode unless the Game view has focus (see deviation 10).
- **Design 5.2** says nothing spawns while the "queue" holds 6 cards. The cap now counts all live calls, connected ones included (decision 3).

#### Step 2a verification (orchestrator, 2026-10-18)
- **Tests:** an independent EditMode run passed 156/156. The console is clean (0 errors, 0 warnings).
- **Scope:**
  - Only our folders changed: Domain, Content, Game, Tests/Domain, Content assets and `GameUI.prefab`.
  - No team file changed. No scene was re-saved; `Game.unity` is unchanged on disk.
  - ProjectSettings, Packages and `PhoneBook.json` are untouched.
- **Code review:**
  - The tick order is right, and callbacks tick before the spawner, so they ring first.
  - A swap from Connecting is handled.
  - The desk keeps the call in hand in sync with half-patched cords.
  - Events are raised after state changes.
  - `ClearCalls` pays, fines and calls back nothing.
  - The dev panel file is wholly inside `#if UNITY_EDITOR || DEVELOPMENT_BUILD`, and `GameInstaller` adds it at runtime.
  - No LINQ or name lookups in the views.
- **Screenshots:**
  - `step-02a.png`: three connected cards with red, blue and yellow rings and their own earnings, which sum to the wallet ($0.78).
  - `step-02a-full-queue.png`: 6 ringing cards.
  - `step-02a-callback.png`: a "Calling again" badge.
  - `step-02a-dev-panel.png`: the panel and the duration badges.
- **Found in review (cosmetic, for the playtest):** a ringing card's amber halo overdraws the rate badge of the card above it, so "9¢ + 5¢/s" is partly hidden when several cards ring. The agent noted the halos touching; the badge being covered is the visible effect.
- **Not done by the orchestrator:** a separate Play Mode run, for the same reason as Step 1. The real F1 press and the dev button clicks are left to the lead's playtest (agent deviation 10).
- **Accepted deviations,** recorded in Design.md:
  - 1: no `LiveCallsView`.
  - 2: `Cleared = 5`.
  - 3: `CancelConnecting`.
  - 4: the pacing formula.
  - 5: slots slide up, and stamps are cut short.
  - 6: a blocked callback is skipped for now.
  - 8: an IMGUI dev panel.
  - 9: the cord ring.

#### Step 2a playtest (lead, 2026-10-18)
- Everything works as expected, including the dev panel. The lead asked to commit and push.
- **Docs:** Design.md (status, 5.1 `Cleared` and `CancelConnecting`, 5.2 pacing and cap, 5.3 callbacks, 5.4 cards, 6.1 `PacingProfile`) and Plan.md (2b notes, §4 lessons) were updated.

### Step 2b prep (orchestrator, 2026-10-18)
- **Pre-flight:**
  - `git fetch`: up to date with upstream, no teammate changes.
  - Editor idle, not in Play Mode, `Game.unity` active. Console cleared.
  - EditMode tests: 156/156 pass, console clean.
- **Decisions:** D07 (hold) is decided. For the spawner's load formula, an on-hold call counts as using a cord and not as waiting (the brief's suggestion).

### Step 2b report: 2026-10-18
Result: done

Playable now: Connect a call, then click one of its plugged jacks. The plug comes out, the card's bubble says "On hold", and its earnings badge and hidden duration freeze. Click empty space (the team's restore) or click the same jack again: the plug goes back in, the bubble says "On the line" again, and pay resumes with no second connection fee. Instead, click the tray, or pull the remaining plug too: the call ends with "Cut off". Or push the pulled plug into a different free jack: the on-hold call ends with "Cut off", and the new pair is judged against the call in hand. With nothing in hand it bounces back (refused); a wrong pair gives "Wrong number!". When the queue is full of ringing cards, every rate badge stays readable, because all the amber halos now draw behind all card content.

How to test: the Plan's Step 2b playtest script. Notes:
1. Clicking the same jack again also resumes the call. It is the same pair closing, just like the restore.
2. The plug you pulled follows the pointer, as in the team's sandbox. While you hold it, clicks on other plugged jacks are blocked (the team's rule). A dev-panel click also counts as an "empty space" click, so it restores a held plug (2a deviation 8).

Created:
- `Assets/Tests/Domain/HoldTests.cs` (+ `.meta`)

Changed:
- **Domain:**
  - `CallService`: new `Hold` (Connected → OnHold) and `Resume` (OnHold → Connected). `Tick` and `EarningsService` already act only on Connected calls, so hold pauses both pay and the duration. The fee isn't paid again, because `EarningsService` pays it only while `ConnectionFeePaid` is false.
  - `OperatorDesk`:
    - `OnCircuitOpened` on a bound cord now puts its call OnHold and remembers the cord's pair (a private `HeldCircuit` list) instead of ending the call. As in 2a, the cord isn't marked half-patched, so the call in hand doesn't go to Connecting because of it.
    - `OnCircuitClosed` on a cord bound to an on-hold call:
      - the same pair, in either order → `Resume`
      - any other pair → the old call is unbound (`Cord = null`, so ending it doesn't send the cord to the tray) and ended as DisconnectedEarly. The new circuit then goes through the normal `Judge`: it connects the call in hand, gives a wrong number, or is refused when no call is in hand.
    - `OnCordReturned` was unchanged; it already ends any bound call (OnHold included) as DisconnectedEarly.
    - `OnCallEnded` also forgets the held pair, so a reused cord starts clean.
  - `CallSpawner.Load`: an OnHold call counts as **using a cord and not as waiting** (orchestrator decision): `freeCords = cordCount − (Connected + OnHold)`, `waiting = live − (Connected + OnHold)`.
- **Game:** `CallCardView`. OnHold shows the cord ring, a frozen earnings badge and the bubble text "On hold". The stamps "Paid $X", "Wrong number!" and "Cut off" were already in place and use only atlas glyphs.
- **Tests:** `CallTransitionTests` (the matrix now has 7 verbs, adding Hold, Resume and End(DisconnectedEarly), and covers the OnHold state), `OperatorDeskTests` (removed the Step 1 "pulled plug cuts the call off" test), `BusyTrackerTests` (OnHold case), `GameSessionTests` (Clear calls with an on-hold call).

Scene and prefab changes:
- **`GameUI.prefab`:** `Calls/CallCard/RingingPulse` gets a nested `Canvas` with `overrideSorting` on and `sortingOrder` 9.
  - Every halo now draws below everything at order 10, which includes all cards and badges in our `GameUI` canvas. It still draws above Sergei's main canvas (order 0) and below his book (10) and held plugs (20), the same as before relative to his things.
  - The copies made at startup inherit it.
  - `overrideSorting` had to be set through `SerializedObject` (`m_OverrideSorting`). `manage_prefabs` and the `Canvas.overrideSorting` setter both left it off, because in prefab contents the nested canvas counts as a root canvas.
  - Nothing else in the prefab changed. No raycast targets were added; a Canvas without a GraphicRaycaster takes no clicks.
- **`Game.unity`:** not changed (not dirty, nothing on disk).
- **Team files:** not changed and not saved; `Level.unity` and `MainMenu.unity` were not opened. ProjectSettings, Packages and `PhoneBook.json` are untouched. `runInBackground` is still off.

Tests: 187/187 EditMode (156 before; 32 new, 1 removed):
- CallTransitionTests: 17 more matrix cases (Hold and Resume from every state, plus the OnHold row: only Resume and End are allowed).
- HoldTests: 13
  - a pulled plug holds the call and keeps its cord, which isn't half-patched
  - hold pauses pay and the duration (60 s held: wallet, earnings and seconds left unchanged)
  - restore in either jack order resumes with one fee only, and pay resumes (2 cases)
  - a held and resumed call pays exactly fee + rate × duration
  - hold, resume and hold again
  - a re-plug elsewhere with nothing in hand: cut off, then refused
  - a re-plug elsewhere that is the right pair for the call in hand: cut off, then the new call connects on that cord
  - a re-plug elsewhere on a wrong pair: cut off, then wrong number
  - cord to the tray: cut off, with no pay afterwards
  - a cord reused after a cut-off is judged, not mistaken for a restore
  - several calls with one on hold
  - spawner load with an on-hold call
- BusyTrackerTests: 1 (both parties of an on-hold call are busy)
- GameSessionTests: 1 (`ClearCalls` ends an on-hold call as Cleared, with no pay and no callback, its cord returned and nothing left on the cord)

Console: clean. No errors or warnings from our code while compiling, in two test runs, or in Play Mode. The only lines were the known Metal "memoryless" lines from screenshots. The console was cleared at the end.

Screenshots (`Temp/Captures/`, taken while paused or stepping):
- `step-02b.png` (main): the top card "On hold" (red ring, $0.19 frozen) next to the card below "On the line" (blue ring, $0.20), the wallet at $0.39, the pulled red plug following the pointer, and four ringing cards below.
- `step-02b-full-queue.png`: 6 ringing cards. Every rate badge ("7¢ + 5¢/s" … "12¢ + 4¢/s") is fully readable, with the halos behind the badges and portraits.

Checked in Play Mode with `CableInputController.ProcessPointer`, one action per rendered frame inside a one-shot `Canvas.willRenderCanvases` handler, driven by `EditorApplication.Step()`. `Time.timeScale` stayed at 1.
- **Connect two calls:** A on cord 0 (D5–E4), B on cord 1 (C3–F3). The first paid its 7¢ fee at once.
- **Pull A's E4 plug:** A went OnHold; cable 0 was OneEndConnected and held by the controller; half-patched 0; nothing in hand. Spawner load was 2.5, which is right for 4 waiting, 3 cords and 2 in use. Over 60 held frames A stayed at 19.50¢ with 19.50 s left, while B kept earning.
- **Click empty space** (between A1 and B2): the cable went back to D5–E4 Connected, A resumed, and 0 fees were paid since. 60 frames later A had 25.60¢.
- **Pull again, click the tray:** A ended as DisconnectedEarly, cable 0 was Ready, and the controller held nothing.
- **Take call C (B4–D2), pull B's F3 plug:** B went OnHold and C stayed Taken (the on-hold cord isn't half-patched).
- **Click free jack A6:** B ended as DisconnectedEarly, and the new circuit C3–A6 was judged against C, giving WrongConnection. Cable 1 went back to the tray, and one callback is pending for C (none for B).
- **Hold, then Clear calls (the dev panel's call)** while the controller held the on-hold cord: the call ended as Cleared, cable 0 was Ready, there were 0 live calls and 0 pending callbacks, and the wallet was unchanged. On the next `ProcessPointer` the controller dropped its stale cable, and nothing was left on cord 0.

Deviations from Design/Plan:
1. **Resuming also works by clicking the pulled jack directly**, not only through the restore click. Design 5.1 already says "the plug goes back into the same jack", and both look the same from outside (the same pair closes).
2. **The halo fix uses a sorting override** (halos at order 9) rather than shrinking the halo. Halos keep their size and pulse; they just never cover any card's badges, portrait, ring, stamp or bubble.
3. **Up to one frame of pay after the click.** `GameLoopRunner.Update` can tick before the cord input's `Update`, so a call earns for at most one frame between the click and the bridge's report in `LateUpdate`. In the run this was 0.1¢. Fixing it would mean changing script execution order, so I left it.

Follow-ups:
- **The stale `heldCable` edge case (Step 1)** now only matters for the dev panel's Clear calls. Pulling a plug no longer makes the desk return a cord the controller holds. Wrong numbers and refusals happen only on a closed circuit, which the controller has already let go of. Clear calls on a held cord was checked: the controller dropped it on its next `ProcessPointer`. A real click in that same frame could still plug the returned cord, but it's dev-only and harmless.
- **Step 4** will fine DisconnectedEarly through `PenaltyService`. A moved plug ends the old call before judging the new circuit, so a fine and a wrong number can both happen in one move.
- **Design 5.1** could note that the desk remembers each on-hold call's pair (`HeldCircuit`), and that a moved plug unbinds the old call before it ends.
- **`Level.unity` sandbox** was not re-run: no file it uses changed (only our prefab, our scripts and our tests).

#### Step 2b verification (orchestrator, 2026-10-18)
- **Tests:** an independent EditMode run passed 187/187. The console is clean (0 errors, 0 warnings).
- **Scope:**
  - Only our files changed: `CallService`, `OperatorDesk`, `CallSpawner`, `CallCardView`, four test files plus the new `HoldTests.cs`, and `GameUI.prefab` (one nested Canvas on `RingingPulse`).
  - No team file, scene, ProjectSettings, Packages or `PhoneBook.json` changed. `Game.unity` is unchanged on disk.
- **Code review:**
  - Hold and Resume are guarded transitions; pay and duration already tick only for Connected calls, and the fee flag prevents a second fee.
  - The desk remembers the held pair per cord, matches it in either order, and forgets it when the call ends.
  - On a re-plug elsewhere the cord is unbound before the old call ends, so it isn't sent to the tray while carrying the new circuit; then the new circuit is judged normally.
  - The on-hold cord's remaining plug isn't counted as half-patched (2a rule kept).
  - Spawner load counts OnHold as using a cord, not waiting (prep decision).
- **Screenshots:**
  - `step-02b.png`: "On hold" card (red ring, $0.19 frozen) above an "On the line" card (blue ring, $0.20); wallet $0.39 = the sum.
  - `step-02b-full-queue.png`: 6 ringing cards, every rate badge fully readable (compared with `step-02a-full-queue.png`, where halos covered them).
- **Not done by the orchestrator:** a separate Play Mode run; the agent drove real cord input through `ProcessPointer`. Real mouse play is left to the lead's playtest.
- **Accepted deviations,** recorded in Design.md:
  - 1: clicking the pulled jack directly also resumes (same pair closing).
  - 2: the halo fix uses a nested canvas at sorting order 9 instead of shrinking the halo.
  - 3: up to one frame of pay after the pulling click (a fraction of a cent).

#### Step 2b playtest (lead, 2026-10-18)
- Everything looks correct. The lead asked to commit.

### Step 3 prep (orchestrator, 2026-10-18)
- **Decisions:** D34 (book tabs as a coordinated change) and D35 (36 people) are decided. The lead approved changes to the Sergei files listed in the Step 3 brief and nothing else of his, and has told Sergei his book files are about to change.
- **Portraits:** reuse `caller-01`…`caller-12` three times each (lead confirmed).
- **Pre-flight:**
  - `git fetch`: up to date with upstream, no teammate changes.
  - Unity connected, idle, not in Play Mode, `Game.unity` active. Console cleared.
  - EditMode tests: 187/187 passed. The working tree is clean after the run.


### Step 3 report: 2026-10-18
Result: done

Playable now: Open the book. A–M tabs sit on the left page edge and N–Z on the right. Tapping a letter jumps to the spread with the first surname starting with it. Only X is greyed out, because no surname starts with X, and tapping it does nothing. The tabs for the spread on show are lighter. The left and right arrow keys also turn the pages while the book is open. All 36 jacks A1–F6 have a person, over 3 spreads, sorted by surname, and callers come from the whole board. Cards are easier to read:
- **In hand:** a bigger bubble with the surnames in bold ("Herbert **Marlow** / wants / Ada **Moss**"), because the book is sorted by surname.
- **On hold:** the cord ring and the earnings are dimmed.
- **Stamps:** a different ink for each outcome: green "Paid $X", red "Wrong number!" and brown "Cut off".
- **Fee popup:** the "+N¢" popup no longer covers the earnings badge.
- **Badges:** they now have a light outline, like the panel frame.

How to test: the Plan's Step 3 playtest script.

Created:
- `Assets/Scripts/UI/PhoneBookTab.cs`: one letter tab, a thin view over `PhoneBookView.ShowLetter`. Approved coordinated change.
- `Assets/Scripts/Content/ContentValidator.cs`: runs `PeopleFileLoader`'s own checks, then adds the checks that need the whole file or the type list:
  - caller types: ids present and unique; ranges min ≤ max; rate, fee and callback delay ≥ 0; duration > 0
  - `contactsPerPage` between 1 and 12
  - duplicate surnames, and the file sorted by surname
  - every caller type has someone to call
- `Assets/Scripts/Game/Editor/ContentValidatorMenu.cs`: the menu item `Tools/Switchboard/Validate Content`.
- Tests: `Assets/Tests/Domain/ContentValidatorTests.cs`, `Assets/Tests/Domain/FullBoardTests.cs`, `Assets/Tests/Game/Editor/PhoneBookTabTests.cs`.

Changed:
- **Sergei's files (approved coordinated change, D34):**
  - `PhoneBookView.cs`: new public `ShowLetter(char)` (returns false and does nothing when no surname starts with that letter), `HasLetter(char)` and `ConfigureTabs(...)`. Tabs refresh with the pages, and the arrow keys turn pages. The existing public API is unchanged.
  - `PhoneBookBuilder.cs`: a new menu item `Tools/Switchboard/Update Phone Book Tabs` (`UpdateTabs`), built the same way as his "Update Phone Book Layers". `CreatePrefab` also calls it, so a rebuilt book gets tabs.
  - `PhoneBookContactRow.cs` and `PhoneBookPageView.cs` were not changed.
- **`Assets/Configs/PhoneBook.json` (shared):** 24 new people, 36 in all.
  - One person on every jack A1–F6, still sorted by surname, `contactsPerPage` still 6, and his format kept.
  - Surnames cover every letter except X.
  - Portraits `caller-01`…`caller-12` are used three times each. They are all grey placeholder circles for now.
  - All are `type.regular` with no overrides.
- **Ours:**
  - `GameConfig.cs`: new `PeopleFileText` and `Validate()`.
  - `CallCardView.cs`:
    - surnames in bold in the bubble
    - on hold: ring and earnings at 45% opacity
    - stamp ink per end reason, with a new `stampOutline` field
  - `PeopleFileTests.cs`: the real file now has 36 people.

Scene and prefab changes:
- **`PhoneBook.prefab`:** gains `OpenBook/LetterTabs` (26 tabs).
  - Each tab is an Image, a Button, his uGUI `Text` with his body font, and a `PhoneBookTab`.
  - The tabs are the first child of `OpenBook`, so they draw under the pages and look tucked in.
  - They were built by running the new builder menu item. All 109 original objects keep their file IDs; the large diff is new objects plus git aligning moved blocks.
- **`GameUI.prefab` (card):**
  - The speech bubble is 150×150 (was 124×140) and sits at x −24.
  - The bubble text auto-sizes from 14 to 26 with tighter line spacing.
  - The fee popup starts at y −20 and rises 30 (was +24 and 48).
  - The four badges are a slightly lighter dark green with a cream `Outline`.
  - `stampOutline` is wired.
  - No new raycast targets.
- **Scenes:** `Game.unity` not changed. `Level.unity` was opened once, only to play it on its own, and was never dirtied or saved. `MainMenu.unity` was not opened.

Tests: 205/205 EditMode. That's 187 before, plus 18 new:
- PhoneBookTabTests (4):
  - jumps to the right spread
  - works on the first and last spread
  - ignores letters with no surname
  - takes the last word of the name as the surname
- ContentValidatorTests (13):
  - the real content passes
  - the board has 36 jacks
  - a valid file passes
  - each of these is reported: duplicate socket, unknown jack, missing portrait, unknown type, duplicate surname, unsorted file, bad `contactsPerPage`, nobody to call, bad type ranges, duplicate type ids
- FullBoardTests (1): 20 game minutes with the real 36 people, connecting every call. Nobody is ever in two live calls, and more than 30 different callers rang.
- One existing test was renamed and updated from 12 to 36 people.

Console: clean. No errors or warnings from compiling, two test runs, Play Mode in `Game.unity`, or a standalone `Level.unity` run. The only lines were the known Metal "memoryless" lines from screenshots. "Validate Content" logs "no problems found".

Screenshots (`Temp/Captures/`):
- `step-03.png` and `step-03-1280x720.png`: the book open on spread 2 (Hollis…Pryce), plus cards in hand, connected, on hold and "Calling again".
- `step-03-book.png`: spread 1 with all tabs (X greyed).
- `step-03-full-queue.png`: 6 ringing cards from jacks A6, B4, B6, D3, E1 and F5.
- `step-03-states.png`: in hand, connected, on hold, a "Calling again" callback and ringing cards.
- `step-03-stamps.png`: the "Wrong number!", "Paid $2.10" and "Cut off" stamps.
- `step-03-phone-844x390.png`: the phone-landscape view.

Checked in Play Mode:
- **Simulated taps:** real EventSystem taps (raycast, then click), run inside a one-shot `Canvas.willRenderCanvases` handler.
  - The tab was the topmost hit every time.
  - M opened spread 2 of 3, Z spread 3, A spread 1. X did nothing, and the book stayed open.
- **Card states:** set through the desk's own methods, with the domain advanced by `Session.Tick`, because stepping frames is slow while Unity is in the background.
- **Standalone `Level.unity`:** the book loads 36 contacts, `ShowLetter('Z')` works, and 3 cables are present.
- **Game view size:** changed with `PlayModeWindow.SetCustomRenderingResolution` and set back to 1920×1080. This leaves two extra custom sizes in the Game view's size list; that's an editor preference, not a project file.
- **Clean-up:** `Time.timeScale` is back to 1, and no callbacks or SessionState keys are left.

Deviations from Design/Plan:
1. **The tab test isn't next to `CableConnectionTests`.** That folder's asmdef (`Switchboard.CableTests`) can't see `PhoneBookView`, which lives in Assembly-CSharp. The test is in our new folder `Assets/Tests/Game/Editor/`. It has no asmdef, so it compiles into Assembly-CSharp-Editor and runs with the other EditMode tests. Nothing in `Assets/Tests/Editor/` was added.
2. **The menu item is in `Assets/Scripts/Game/Editor/`.** Design 4.1 names a `Switchboard.Content.Editor` asmdef, but the brief allowed either folder, and one editor folder of ours is simpler. The validator logic itself is in the Content assembly.
3. **Extra validator checks:** `contactsPerPage` 1–12 (his book's own limit) and the surname sort order. The book shows contacts in file order, so the tabs rely on that order.
4. **Arrow keys added; swipe left as a follow-up.** The arrow keys are 5 lines in his `PhoneBookView.Update`, using `Keyboard.current`.

Follow-ups:
- **Phone readability (Step 9).** At 844×390 everything fits and nothing overlaps, but all text is small (rate badges about 10 px). Sergei's canvas and ours both scale the 1920×1080 layout to 36%. Making it readable needs a phone layout or a different scaling rule for both canvases, which is beyond restyling cards. The same goes for touch-sized tabs.
- **In-hand bubble width.** The bubble is as wide as the gap left of the caller panel allows at 16:9 (150 reference px). Long names wrap onto two lines.
- **Swipe page-turning** for touch, when Sergei moves the cord input to `Pointer.current` (Step 6).
- **Portraits.** All 12 are identical grey placeholders, so callers can only be told apart by their bubble and their stamp until Mia draws them.
- **Tell Sergei** about the new builder menu item `Update Phone Book Tabs`. His `Update Phone Book Directory` still points at the missing `Assets/Settings/PhoneBook.json`.

#### Step 3 verification (orchestrator, 2026-10-18)
- **Tests:** an independent EditMode run passed 205/205. The console is clean (0 errors, 0 warnings). The editor is idle, not in Play Mode, with `Game.unity` active.
- **Scope:**
  - Sergei's files changed only within the approved list: `PhoneBookView.cs`, `PhoneBookBuilder.cs`, `PhoneBook.prefab`, plus the new `PhoneBookTab.cs`. `PhoneBookPageView.cs` and `PhoneBookContactRow.cs` are untouched, and nothing was added to `Assets/Tests/Editor/`.
  - `PhoneBook.prefab`: all 109 original objects are still there, and the only edits to them are additive: one new child (`LetterTabs`) under `OpenBook` and the `tabs` array on `PhoneBookView`. The rest of the 7,000-line diff is the 288 new tab objects.
  - The shared `PhoneBook.json` has 36 people on 36 different jacks, `contactsPerPage` 6, sorted by surname, each portrait used three times.
  - The rest is ours: `GameConfig`, `CallCardView`, `GameUI.prefab`, the new `ContentValidator`, the menu in `Assets/Scripts/Game/Editor/`, and tests. No scene, ProjectSettings or Packages file changed.
- **Code review:**
  - `ShowLetter` and `HasLetter` scan the sorted contacts for the first surname (the last word of the name) with that letter. The tabs are a thin view over `ShowLetter`, and his existing public API is unchanged.
  - The arrow keys use `Keyboard.current` only while the book is open.
  - The tab state refreshes on page changes, not every frame.
- **Screenshots:**
  - `step-03-book.png`: A–M tabs on the left page edge, N–Z on the right, and X greyed out.
  - `step-03-states.png`: the in-hand bubble with bold surnames, "On the line", and "On hold" with dimmed earnings.
  - `step-03-stamps.png`: green "Paid $2.10", red "Wrong number!", brown "Cut off".
  - `step-03-phone-844x390.png`: everything fits, but the text is about 10 px. This is left to Step 9.
- **Accepted deviations,** recorded in Design.md:
  - 1: the tab test is in our new `Assets/Tests/Game/Editor/` (Assembly-CSharp-Editor), because `Assets/Tests/Editor/`'s asmdef can't see `PhoneBookView`.
  - 2: the Validate Content menu lives in `Assets/Scripts/Game/Editor/`, not a `Switchboard.Content.Editor` asmdef.
  - 3: the validator also checks the surname sort order and `contactsPerPage`.
- **For the lead to pass on to Sergei:** his book now has a builder menu item `Tools/Switchboard/Update Phone Book Tabs`, which `CreatePrefab` also calls.
- **Raised for Step 4:** the 36 people now fill every jack, so the emergency services (Police, Fire Brigade, Doctor) have no free line. Decide before Step 4 (see Plan, Step 4).

#### Step 3 playtest (lead, 2026-10-18)
1. **Holding a cord and clicking the book sends the cord back to the tray.** This is Sergei's `CableInputController.ProcessPointer`: any click that isn't on a jack drops the held cord (or restores a pulled plug), and that includes buttons, book tabs and caller cards. The lead chose to **leave it as it is** and pass it to Sergei as feedback.
2. **X tab greyed out.** That's by design: no surname starts with X. The lead chose to keep it greyed.
3. **No character photos.** Expected: Mia's `caller-01`…`caller-12` are 12 identical grey placeholder circles (same file). The portraits are wired, so her drawings will show up when she replaces the files, with no code change.
4. **New request: feedback when a caller is clicked, and typed-out speech.** Visual and sound feedback on taking a caller. The sound is a generated placeholder click (lead's choice), until the team has real sounds. The bubble text appears letter by letter, like typing, not too slow. A talking sound per caller comes later.
5. **Tabs for the spread on show need to stand out more** (brighter or darker than the others).


### Step 3 fix report: 2026-10-18
Result: done

Playable now: Clicking a ringing card (or swapping to another one) gives a quick squash-and-bounce of the card with a short white flash on the portrait, plus a soft click sound that follows the Sfx volume slider. The speech bubble types out letter by letter at 40 characters a second (a full "Name / wants / Name" line takes under a second); "On the line" and "On hold" type out too. In the open book, the tabs of the spread on show are now near-white, bold and stick out 8 px from the page; the other live tabs are a darker tan, and X stays greyed.

How to test:
1. Play from `Game.unity`. Click a ringing card: the card squashes and bounces, the portrait flashes, a soft click plays. Click another ringing card: same again (swap).
2. Watch the bubble: the names type out quickly. Connect the call: "On the line" types out. Pull a plug: "On hold" types out.
3. Move the Sfx slider in the volume panel: the click gets quieter or louder.
4. Open the book and use the tabs: the letters for the spread on show are bright, bold and stick out; the others are darker; X is grey and does nothing.

Created:
- `Assets/Scripts/Game/Views/TypingReveal.cs`: plain helper that counts how many characters show (first one at once, then N per second).
- `Assets/Scripts/Game/Views/UiClickSound.cs`: plays a serialized `AudioClip` with a small random pitch (0.94–1.08) through `PlayOneShot`. Volume is left to Sergei's `AudioChannelSource` on the same object.
- `Assets/Content/Audio/card-take-click.wav`: generated 60 ms placeholder (a soft 520 Hz knock plus a quieter 1.65 kHz tick, fast decay, peak about 38%).
- `Assets/Tests/Game/Editor/TypingRevealTests.cs` (6 tests).

Changed:
- `CallCardView.cs`: `charactersPerSecond` (40), typed speech through TMP `maxVisibleCharacters` (restarts only when the words change; resets when the bubble hides or the card is reused), an empty `OnCharacterShown(int)` hook for the later talking sound, and `PlayPress()` (0.2 s squash to 86% and overshoot to 106%, flash from 45% alpha, unscaled time, no allocations).
- `CallQueueView.cs`: plays the press and the click only when `TryTake` succeeds; new `takeSound` field.
- Sergei's files (approved for this pass): `PhoneBookTab.cs` (colours: available 196,166,108, current 255,248,222, unavailable unchanged; current label bold; `SetRest` stores the rest position and an outward nudge), `PhoneBookBuilder.cs` (`TabNudge = 8`, calls `SetRest`). `PhoneBookView.cs` not changed.

Scene and prefab changes:
- `GameUI.prefab`: `CallCard/Portrait/PressFlash` (inactive Image, raycast off); `CardTakeSound` under the root with AudioSource (no play on awake, 2D), `AudioChannelSource` (Sfx, base 0.6) and `UiClickSound`; card `pressTarget`/`pressFlash` and queue `takeSound` wired. No new raycast targets.
- `PhoneBook.prefab`: tabs rebuilt with `Tools/Switchboard/Update Phone Book Tabs`, so the new colours and nudge are serialized (26 tabs, `PhoneBookView.tabs` re-linked).
- Scenes: none changed. `Level.unity` and `MainMenu.unity` not opened.

Tests: 211/211 EditMode (new: TypingRevealTests ×6).
Console: clean (only the known Metal "memoryless" lines from screenshots).
Screenshots: `Temp/Captures/step-03-fix-book-tabs.png` (spread 2: I–P stand out), `step-03-fix-card-press.png` (top card mid-squash), `step-03-fix-bubble-typing.png` ("Reginald Und…" mid-typing).
Checked in Play Mode (stepped frames): press returns to scale 1 and hides the flash after 0.2 s; typing reaches the full count; a swap plays the press and the click (source volume 0.3 = 0.6 × Sfx 0.5); re-taking a swapped-out card retypes its line. `Time.timeScale` 1, no callbacks or SessionState keys left.

Deviations from Design/Plan: none.
Follow-ups:
- Tell Sergei: his tabs now have a `SetRest` nudge and new colours; rebuilding the book with his builder keeps them.
- Step 8: replace `card-take-click.wav` with a real sound and fill `OnCharacterShown` with the talking sound.
- Typing uses centre-aligned final line positions, so a line grows from where its full text starts rather than re-centring as it types (intended, keeps the layout still).

#### Step 3 fix verification (orchestrator, 2026-10-18)
- **Tests:** an independent EditMode run passed 211/211. The console is clean.
- **Scope:**
  - Sergei's files: only `PhoneBookTab.cs`, `PhoneBookBuilder.cs` and `PhoneBook.prefab`, all approved for this pass. His 109 original book objects are still only changed by the Step 3 tab additions.
  - Ours: `CallCardView`, `CallQueueView`, `GameUI.prefab`, the new `TypingReveal`, `UiClickSound`, `card-take-click.wav` and `TypingRevealTests`.
  - His `AudioChannelSource` is used unchanged on our click sound, so his Sfx slider sets its volume. No scene, ProjectSettings or Packages file changed.
- **Code review:**
  - The press and the click play only when `TryTake` succeeds.
  - The typing uses `maxVisibleCharacters` through a small testable `TypingReveal`.
  - `OnCharacterShown` is the hook for the talking sound in Step 8.
- **Screenshots:**
  - `step-03-fix-book-tabs.png`: on spread 2, I–P are near-white, bold and stand out from the page; the others are darker tan, and X is grey.
  - `step-03-fix-bubble-typing.png`: "Reginald Und…" mid-typing.

### Step 3 fix 2 report: 2026-10-18
Result: done

Playable now: The phone book's A–Z tabs are hidden for the playtest. Nothing was removed: the 26 tabs and their code are still there. To bring them back, tick the `OpenBook/LetterTabs` object active in `PhoneBook.prefab`.

How to test:
1. Play from `Game.unity` and open the book: no letter tabs along the page edges.
2. Use the page arrows and the left/right arrow keys: the spreads still turn.

Changed:
- `Assets/Prefabs/UI/PhoneBook.prefab` (Sergei's, approved for Step 3): `OpenBook/LetterTabs` is inactive (`m_IsActive: 0`). Saved with `LoadPrefabContents`/`SaveAsPrefabAsset`. No builder menu was run, and no code changed.

Checks:
- `PhoneBookView` with the tabs inactive: `Awake` binds the inactive tabs and `RefreshPages` calls `Show` on them (both only set serialized fields), with no errors or logs. In stepped Play Mode, the book opened, the LetterTabs object was inactive in the hierarchy, and the page buttons moved spread 0, then 1, then 0.
- Arrow keys: couldn't be driven through MCP, because Unity drops queued keyboard events while the editor window isn't focused. They call the same `NextSpread`/`PreviousSpread` methods as the buttons, and that path doesn't touch the tabs, so the risk is low. The lead should press them once in the playtest.
- Tests: 211/211 EditMode. `PhoneBookTabTests` builds its own objects and doesn't load the prefab, so it doesn't depend on the tabs being active.
- Console: clean (only the known Metal "memoryless" lines from the screenshot).
- Screenshot: `Temp/Captures/step-03-tabs-hidden.png` (book open on spread 1, no tabs).
- Editor left clean: not in Play Mode, `Game.unity` active and not dirty, `timeScale` 1, console cleared.

Deviations from Design/Plan: none.
Follow-ups:
- **Builder re-shows the tabs.** `ConfigureTabs` destroys `LetterTabs` and builds a new, active one. Two menus run it, so either brings the tabs back:
  - `Tools/Switchboard/Update Phone Book Tabs`, every time it runs.
  - `Create Phone Book`, but only when the prefab is missing and `CreatePrefab` rebuilds it.
  After any rebuild, untick `LetterTabs` again. "Update Phone Book Directory" and "Update Phone Book Layers" don't touch the tabs.

#### Step 3 fix 2 verification (orchestrator, 2026-10-18)
- The lead asked to hide the A–Z tabs for the playtest and keep the code and assets. Only `OpenBook/LetterTabs` changed: it is now inactive in `PhoneBook.prefab`.
- An independent EditMode run passed 211/211. The console is clean, apart from the MCP plugin's own connection warning.
- **To show the tabs again,** tick `LetterTabs` active in the prefab. Sergei's `Update Phone Book Tabs` menu (and `Create Phone Book`, when the prefab is missing) rebuilds them active, so untick them again after running either.

#### Step 3 final playtest (lead, 2026-10-18)
- Looks good with the tabs hidden. The lead asked to commit, and to push if upstream has no new commits.

#### Merge with upstream (orchestrator, 2026-10-18)
- Merged 4 upstream commits: Mia's bulb sprites, the `caller-01` portrait, `tray.png` and F bulb letters, plus Sergei's level art pass.
- Mia's commit deleted `caller-01.png.meta`, which `GameConfig.asset` and `GameUI.prefab` reference. The old meta was kept in the merge, so the GUID `f1165ce5…` and the portrait links survive. The lead sent Mia a note to commit `.meta` files with her images.
- After the merge: EditMode tests 211/211, a clean compile, and a Play Mode check with no errors. `CallerQueuePanel` and `HeaderSlot` are still found, and the cards and the money counter show.
- **Follow-up:** Sergei's new background paints 5 portrait windows, but there are still 6 caller slot anchors, so our cards no longer line up with the painted windows. He lists this as a pending layout task.
- Unity created `.meta` files for Mia's `F01.png` and `F02.png` locally. They were left uncommitted, per the team rule.

#### Step 4 prep decisions (lead, 2026-10-18)
- **D36:** Police, Fire Brigade and Doctor replace 3 people on their jacks and are listed first in the book (top of `PhoneBook.json`).
- The designer edits `PhoneBook.json` by hand; that's already how it works (D31).
- **D37 (Step 4 numbers):** response time 30 s; fines $2 (missed or wrong patch) and $1 (cut off early); emergency pay fee 20–40¢ + 5–10¢/s for 10–20 s; about 1 call in 6 is an emergency. The lead took all four recommended options.

#### Step 4 pre-flight (orchestrator, 2026-10-18)
- `git fetch`: one upstream commit from Sergei (`8ec771f`, "edited cord UI"), fast-forwarded. It touches only `CableInputController.cs`, `SocketView.cs` and `SwitchboardLampView.cs` (`SetPointerState` now takes the held cable, and the row/column lamps light only for the hovered socket while a plug is held). None of our code calls the changed method.
- Unity: connected, idle, not in Play Mode, `Game.unity` active. Compiled after the merge; the console was cleared. EditMode tests passed 211/211.
- The brief is written in Plan.md (Step 4), and D37 is recorded in Design §10.
- **Not done, per D36:** `ShowLetter` skipping the service block would need an edit to Sergei's `PhoneBookView`. The tabs are hidden, so this is left as a follow-up.


### Step 4 report: 2026-10-18
Result: done

Playable now: About 1 call in 6 is an emergency, and only one is ever on the board or waiting to call back.
- **Ringing.** The card has a red halo, a red "EMERGENCY" badge, a red outline on the rate badge, and a countdown ring around the portrait: a cream arc on a dark track that empties over 30 s. In the last 10 s it turns orange-red and pulses.
- **In hand.** The bubble reads, for example, "Herbert **Marlow** / wants / **Police**". Police (B6), Fire Brigade (C2) and Doctor (D4) are at the top of page 1 of the book.
- **Connected in time.** The ring goes, the cord ring appears, the fee pops up (20–40¢), and it pays 5–10¢/s for 10–20 s.
- **Missed.** The card stamps "Missed -$2.00" in dark red, and "-$2.00" pops up in red under the money counter.
- **Wrong patch.** "Wrong number!", a $2 fine, and the same caller rings again 20–40 s later.
- **Cut off.** "Cut off" and a $1 fine.
- **Money and idling.** Money stops at $0.00. After 60 s with no mouse movement, click or key press, no new emergencies ring and due emergency callbacks wait; regular calls carry on.
- **Dev panel (F1).** A new "Emergency now" button. "Show times" now shows "25s / 14s" on an emergency (seconds left to connect / hidden duration).

How to test: the Plan's Step 4 playtest script, with one caveat on step 8 (see Follow-ups): at most one emergency is missed **after the guard trips**, but one more can ring in the first minute after you leave.

Created:
- **Domain:**
  - `Definitions/CallerTypeExtensions.cs`: `CarriesFines`, `FineFor(reason)`, `CallsBackAfter(reason)` (D10), and `Accepts(person)`, the recipient-tag rule.
  - `Pacing/IdleGuard.cs`: D16.
  - `Penalties/PenaltyService.cs`: `PenaltyService` plus the `FineAppliedArgs` readonly struct.
- **Content:** `CallCardStyle.cs`, presentation only: badge text and accent colour.
- **Assets:** `Assets/Content/CallerTypes/Emergency.asset` with the D37 numbers, badge "EMERGENCY" and accent red (200, 33, 33).
- **Tests:**
  - `ResponseTimerTests` (8)
  - `PenaltyServiceTests` (15 with test cases)
  - `EmergencySpawnTests` (5)
  - `IdleGuardTests` (7)
  - an `EmergencyWorld` fixture in `Fakes.cs`

Changed:
- **Domain:**
  - **The caller type** (`ICallerType`) has the 9 new fields from the brief.
  - **Pacing:** `IPacingProfile.IdleTimeoutSeconds`, also on `PacingSettings`, an optional parameter with default 60.
  - **People:** `PersonExtensions.HasTag`.
  - **The call** (`Call`) has `HasResponseTime`, `ResponseSecondsLeft` and `FineCents` (the amount actually taken).
  - **`CallService`:**
    - The response timer counts down in Incoming, Taken and Connecting.
    - When it reaches 0, the call ends as `Missed`.
    - New `CountLive(type)`.
  - **`CallbackScheduler`:**
    - The per-type flags decide callbacks.
    - A due callback waits while its type is at `maxSimultaneous` (live calls), or while the idle guard blocks it.
    - New `CountPending(type)`.
  - **`CallSpawner`:**
    - It picks the type first, by weight among the allowed types: under `maxSimultaneous` (live plus pending), not idle-blocked, and with a free caller who has a free recipient. Then it picks the caller, then the recipient by the type's tag.
    - New `TrySpawn(type)` for the dev panel, and a `Types` list.
    - No LINQ, and no allocations per tick.
    - With only one allowed type it skips the roll, so the old random sequences still hold.
  - **`GameSession`:** builds `IdleGuard` and `PenaltyService`. Tick order: clock, idle guard, earnings, calls, callbacks, spawner.
- **Content:**
  - **`CallerTypeDefinition`:** the new fields, with tooltips, plus `cardStyle`.
  - **`PacingProfile`:** `idleTimeoutSeconds`, 60 in `Default.asset`.
  - **The people file:** `PeopleFileContact.tags`, and `PersonRecord.Tags`.
  - **`PeopleFileLoader`:**
    - Services, the entries tagged `emergency-service`, need no portrait or type, get no caller record, and keep their whole name as the listing ("Fire Brigade", not "Brigade, Fire").
    - Types with `everyoneCalls` get one caller per untagged person, for example `caller.ada-moss.emergency`.
    - All messages are reworded to name the contact and the fix.
  - **`ContentValidator`:**
    - the services block at the top
    - surname rules for people only
    - each `recipientTag` has at least one contact
    - non-negative response time, fines, `maxSimultaneous` and `spawnWeight`
    - at least one type with `spawnWeight` > 0
    - plain messages that say how to fix
  - **`GameConfig`:** `CardStyleOf(typeId)`, and Emergency registered.
  - **`Regular.asset`:** `spawnWeight` 5; the rest 0, false or empty.
- **Game:**
  - **`GameLoopRunner`:** reports input to the idle guard: `Pointer.current` press or movement, or `Keyboard.current.anyKey`, with no allocations.
  - **`CallCardView`:**
    - the type badge and accent
    - the countdown ring, polled in `LateUpdate` and written only when the fill moves by 1/360
    - urgency in the last 10 s
    - the "Missed -$X" stamp in `missedInk`, rewritten by `RefreshStamp` once the fine lands
    - the dev times
  - **`CallQueueView`:** passes the style and the penalties.
  - **`MoneyCounterView`:** the fine popup.
  - **`GameInstaller`:** wiring.
  - **`DevPanel`:** one "<Type> now" button per type that has a response time.
- **Shared `Assets/Configs/PhoneBook.json` (D36):**
  - Police B6, Fire Brigade C2 and Doctor D4 are added at the top in that order, one line each, in the brief's format.
  - They replace Hazel Kettering, Ernest Fenwick and Cyril Sparrow, who shared a surname letter with Kemble, Fairweather and Stanhope.
  - The rows are B, C and D. The people's lines are untouched, and the file is still sorted, with `contactsPerPage` 6.
- **Tests updated:**
  - `PeopleFileTests`: 33 people + 3 services, 66 callers, and the services first. Plus a new service-parsing test.
  - `FullBoardTests`: also checks that services never call and that only emergencies go to services.
  - `ContentValidatorTests`: 8 new.

Scene and prefab changes:
- **`GameUI.prefab`:**
  - `CallCard/ResponseRing`: a dark 40 px track, a copy of `CordRing`, between `CordRing` and `Portrait`. Its child `Fill` is a cream Radial360 fill from the top.
  - `CallCard/TypeBadge`: a copy of `CallbackBadge`, at the top. The "Calling again" badge moves down 34 px when both show.
  - `Hud/MoneyPlate/FinePopup`: a copy of `FeePopup`, under the plate. Red text, 1.4 s, drifts down 30.
  - All new fields are wired.
  - Raycast targets: still only the portrait.
- **Scenes:** none changed. `Level.unity` was opened once to play it on its own. It was never dirtied or saved. `Game.unity` is active and not dirty.

Tests: 255/255 EditMode (was 211). New: ResponseTimerTests ×8, PenaltyServiceTests ×15, EmergencySpawnTests ×5, IdleGuardTests ×7, ContentValidatorTests ×8, PeopleFileTests ×1. Changed: 2 PeopleFileTests, FullBoardTests.

Console: clean. The only lines were the known Metal "memoryless" lines from screenshots and the MCP plugin's WebSocket warning.

Screenshots (`Temp/Captures/`):
- `step-04.png`: the emergency ringing, ring about 60% full.
- `step-04-in-hand.png`: "Herbert Marlow / wants / Police".
- `step-04-missed.png`: the "Missed -$2.00" stamp and the "-$2.00" popup, with $10.00 going to $8.00.
- `step-04-connected.png`: no countdown ring, blue cord ring, "+34¢".
- `step-04-book.png`: spread 1 with Police, Fire Brigade and Doctor on top. The book (lower right) doesn't reach the fine popup (top right).

Checked in Play Mode (stepped frames; the domain was advanced with `Session.Tick` for the long waits):
- An emergency rang, was taken, then missed: 200¢ taken, nothing in hand afterwards.
- Another one connected: the ring was hidden, the cord ring showed, the type badge was hidden, and the fee was paid.
- A 600-frame ×5 natural run gave no errors.
- `Level.unity` plays on its own: Sergei's book parses the new file, 3 cables, no errors.
- `Validate Content` reports "no problems found".
- Moving Doctor below a person (in memory only) gives: `Contact 4 "Doctor": emergency services must be listed at the top of the file, before the people. Move this line up to the other services.`
- Clean-up: `timeScale` is back to 1, Play Mode stopped, and no callbacks or SessionState keys are left.
- Unity re-saved `ProjectSettings/TimeManager.asset` with only a format change: "Fixed Timestep 0.02" became an equal rational. I discarded it with `git checkout`.

Deviations from Design/Plan:
1. **The countdown ring isn't in the accent colour.** A red ring disappeared into the red emergency halo (first screenshot). The ring is now a cream fill on a dark track, so it reads on any halo, and it turns orange-red in the last 10 s. The accent tints the badge, the halo and the rate badge outline.
2. **The type badge hides once the call is connected**, because the earnings badge takes the top of the card. The countdown ring hides at the same time, as the brief says.
3. **A service is defined as "tagged `emergency-service`"**, as in the brief. Services get no caller record, even if a `type` is given (it is ignored, with no error). "Untagged" means people for `everyoneCalls` and for an empty `recipientTag`.
4. **Design 4.4's tick snippet** should gain `Idle.Tick(dt)` after the clock, for the orchestrator to update.

Follow-ups:
- **Playtest step 8 can show two misses (needs a decision).** With the D16 timeout of 60 s and the D37 response time of 30 s:
  - An emergency ringing when you walk away is missed at most 30 s later.
  - Another one can still ring before the guard trips at 60 s, and be missed too. With 8 s spawns and a weight of 1 in 6, that's roughly a 40% chance.
  - After the guard trips, at most the one already waiting is missed. The test `TwoIdleMinutesWithAnEmergencyAlreadyWaitingCostAtMostOneFine` covers that case.
  - If the lead wants "at most one miss per absence", options are: an idle timeout no longer than the response time (for example 30 s), or blocking fined types while an emergency waits unattended. **Not changed: this is a design call.**
- **Re-showing the A–Z tabs.** Sergei's `ShowLetter('P')` would now jump to Police on spread 1, as the brief notes.
- **Dev panel IMGUI clicks.** They count as input for the idle guard (they move the pointer). That's harmless.
- **Sound (Step 8).** The alarm and ring loop, and fine and miss sounds.

#### Step 4 verification (orchestrator, 2026-10-18)
- **Tests:** an independent EditMode run passed 255/255. The console is clean (0 errors, 0 warnings).
- **Scope:**
  - Only our files plus the shared `PhoneBook.json` changed. No Sergei files, no scenes, no ProjectSettings.
  - The agent discarded Unity's format-only re-save of `TimeManager.asset`.
  - Mia's `F01/F02.png.meta` files are still untracked locally. She has been asked to commit her own.
- **`PhoneBook.json`:**
  - Police B6, Fire Brigade C2 and Doctor D4 sit first, one line each with `"tags": ["emergency-service"]`.
  - Hazel Kettering, Ernest Fenwick and Cyril Sparrow were removed.
  - The 33 people's lines are unchanged and still sorted, with 36 sockets in all.
- **Code review:**
  - `PenaltyService` fines on `CallEnded` through `Type.FineFor(reason)` and stores the amount taken on the call for the stamp.
  - `CallService.Tick` counts the response timer only in Incoming, Taken and Connecting, and ends calls as `Missed` after walking the list.
  - `IdleGuard` ticks right after the clock, and `GameLoopRunner` reports a pointer press or move or any key through the Input System with no allocations.
- **Screenshots:**
  - `step-04.png`: the red EMERGENCY badge, red halo and red rate outline. The countdown ring is a thin cream arc and fairly subtle at 1080p; the lead should judge it.
  - `step-04-in-hand.png`: "Herbert **Marlow** / wants / **Police**".
  - `step-04-missed.png`: the "Missed -$2.00" stamp and the red "-$2.00" popup. The popup sits over the top "+" slot of Sergei's upgrade panel for about 1 s; it's not a raycast target.
  - `step-04-book.png`: spread 1 starts with Police, Fire Brigade and Doctor, then Abbott, Bramble and Burrows.
- **Open question for the lead (from the agent):** with a 60 s idle timeout and a 30 s response time, walking away can cost two missed emergencies ($4), with roughly a 40% chance, before the guard trips. After that, nothing more is lost.

#### Step 4 playtest (lead, 2026-10-18)
- "Everything looks good." Accepted as built, including the subtle countdown ring and the fine popup over the shop's top slot.
- The idle guard stays at 60 s: walking away can cost up to two missed emergencies ($4), then nothing more.
- The lead asked to commit, and to push if upstream has no new commits.

#### Step 4 commit and the scene merge (orchestrator, 2026-10-18)
- Step 4 was committed locally as `14fd59c`. It was not pushed, because upstream had moved.
- **Upstream:**
  - Mia's `node.png`.
  - Sergei's "scene merging" (`1c20569`). It pasted prefab-less copies of our `Game` and `GameUI` into `Level.unity`, so the Step 4 visuals were missing there, and it changed `GameInstaller` to a single scene while MainMenu still loaded `Game.unity`. Checked: the commit contained nothing else of his work.
- **Merged** (`d849187`). Our local `F01/F02.png.meta` files were deleted in favour of the committed ones.
- **Reverted the scene merge** (`cdc5876`), keeping Mia's `.meta` files. After the revert: `GameInstaller` is identical to Step 4, `Level.unity` is identical to `8ec771f`, and EditMode tests pass 255/255.
- **Lead's decision (D38):** one `Final.unity` built from prefabs, done by a subagent. The brief is in Plan.md ("Scene merge").


## Scene merge report: 2026-10-18
Result: done

Playable now: `Final.unity` is the one game scene. It holds Sergei's board plus our `Game` and `GameUI` prefab instances. MainMenu → Start lands in it with calls ringing, and playing `Final.unity` directly gives the same game.

How to test: as in the plan's playtest script.

Created:
- `Assets/Prefabs/Game/Game.prefab`: `GameInstaller` (GameConfig, its own `CordBridge` and `GameLoopRunner`), `GameLoopRunner`, `CordBridge`. The dev panel is still added at runtime, as before.
- `Assets/Scenes/Final.unity`: `Level.unity` moved inside Unity, so it keeps GUID `4664235fe3d741145ae4fadc5024fbe4`. Git shows it as a delete plus an untracked file until it is staged, and then sees the rename.

Changed:
- `GameInstaller.cs`:
  - Removed the additive loading, the `levelScene` field, and the serialized `moneyCounter` and `callQueue` fields.
  - `Start` is now a plain method. It uses `gameObject.scene`, and finds `MoneyCounterView` and `CallQueueView` by type through `LevelScene.FindAll`.
  - If either is missing or there is more than one, it logs a clear error ("GameInstaller needs exactly one X in the scene 'Final', but found N.") and stops.
  - Serialized references stay only inside `Game.prefab`: the `GameConfig` asset, `cordBridge` and `loopRunner`.
- `LevelScene.cs`: summary comment only. The class name is kept, as the brief names it.
- `CordBridge.cs`: the parameter of `Attach(Scene)` is renamed from `level` to `scene`.
- `ContentValidator.cs`, `PeopleFileTests.cs`: the comment "board in Level.unity" now says `Final.unity`.
- `GameUI.prefab`: the overrides below.
- `ProjectSettings/EditorBuildSettings.asset`: MainMenu (0), Final (1).

Deleted: `Assets/Scenes/Game.unity` (and its `.meta`).

Scene and prefab changes:
- **`Final.unity`:**
  - Only additions: a `Game.prefab` instance and a `GameUI.prefab` instance at the root, plus their two entries in the scene-roots list.
  - Each instance's only overrides are the name and the root transform values. Those are the defaults Unity always writes for a root instance, and they equal the prefab's.
  - Nothing of Sergei's was moved, changed or deleted. A text diff against `HEAD:Level.unity` shows only the three added blocks.
  - There is exactly one camera (the `Main Camera` prefab) and one EventSystem. It is saved and is the active scene.
- **`GameUI` overrides applied to the prefab:**
  - `m_AdditionalShaderChannelsFlag` set from 0 to 25 on the `GameUI` and `Hud` canvases. TextMeshPro sets this itself (TexCoord1, Normal, Tangent), so it would otherwise reappear as a scene override.
  - `Amount` (TextMeshPro, the money counter): `m_fontColor32`, which is the same colour as `m_fontColor` (242,210,122), and `m_TextStyleHashCode`, the "Normal" style. TextMeshPro filled both in when the instance loaded.
  - No visual change.
- **`GameUI` overrides dropped as noise:**
  - The root RectTransform values: they equal the prefab's, and the Canvas drives them.
  - `Amount` `m_fontSize = 63`: an auto-size result (28–64 pt), which I reverted. It reappears only if the canvas lays out in edit mode at another Game view size. If it shows up as an override, revert it; it's harmless.
- **The old `Game.unity` instance** had no other overrides: no layout values, no added or removed components or objects.

**Loose (non-prefab) root objects in `Final`** (for Sergei; unchanged):
- `SwitchboardCanvas`: loose, with the loose children `WorkArea` and `WorkArea/CableInteraction` (and the inactive `HandCursor` under it). It holds the prefab instances BackgroundPanel, CallerQueuePanel, CableTray, UpgradeShopPanel, PhoneBook and Cable_01–03.
  - Suggestion: make one `Switchboard` prefab (canvas, scaler, `WorkArea`, `CableInteraction` with `CableInputController`, `HandCursor`) nesting those prefabs. Then board edits go to his prefab, not to the shared scene file.
- `EventSystem`: loose (EventSystem plus InputSystemUIInputModule).
  - Suggestion: a tiny prefab, or leave it, since it rarely changes.
- `Main Camera` is already a prefab instance. `Game` and `GameUI` are ours.

**Every place that referenced "Level" or "Game.unity":**
- Ours, updated: `GameInstaller.cs` (`levelScene = "Level"`, additive `LoadSceneAsync`), `LevelScene.cs` summary, `ContentValidator.cs` comment, `PeopleFileTests.cs` comment, `EditorBuildSettings.asset`. The parameter in `CordBridge.Attach` was renamed.
- Sergei's, not changed:
  - `Assets/Editor/LevelBackgroundBuilder.cs` (menu "Tools/Switchboard/Create Level Background And Lamps"): it requires the active scene path to be `Assets/Scenes/Level.unity`, so it now always throws "Open Level in Edit Mode first." It needs `Final.unity`.
  - `Assets/Editor/GamePresentationBuilder.cs` (menu "Apply English UI And Menu Cursor"): it opens `Assets/Scenes/Level.unity` additively after saving MainMenu, which now fails on the missing path. It needs `Final.unity`.
  - `Assets/Tools/rodrigo_menu_tools/Runtime/Scripts/Menu.cs` `onPlayButtonClick`: loads build index +1. That works, because `Final` is 1.
  - `PauseMenu.RestartButton`: reloads the active scene by name. That works.
  - `MainMenu.unity` itself names no scene.
- Docs (orchestrator's to update): `CLAUDE.md` lines 64, 77–78 and 123; `Design.md` §3 (lines 97 and 109), §4.1 "Our scenes", §4.2, §4.4 step 3, and D01; `Plan.md` §5 (the smoke test in `Game.unity`, "Level.unity still plays on its own"), §8, and older step scripts that say "Play from `Game.unity`".

Tests: 255/255 EditMode (none new: the change is scene wiring, with no domain rule).

Console: clean. The only lines were the known Metal "memoryless" lines from screenshots and the MCP WebSocket warning after the recompile. I cleared it at the end.

Screenshots (`Temp/Captures/`):
- `merge-final.png`: `Final` played directly. An emergency rings (EMERGENCY badge and red halo) under a regular call, with the money plaque, board, cords, shop panel and book.
- `merge-final-fine.png`: the emergency missed. The "Missed -$2.00" stamp and the "-$2.00" popup, with $10.00 going to $8.00.
- `merge-final-book.png`: the book open on spread 1 (Police, Fire Brigade, Doctor, Abbott…). `LetterTabs` is still inactive.
- `merge-from-menu.png`: after MainMenu → Start. A fresh session ($0.00) with an emergency ringing.

Checked in Play Mode (stepped frames):
- **`Final` direct:** one scene loaded, `Session` built, 3 cords attached, both caller types present.
- **Emergency:** spawned, ticked 12 s, then missed with the fine taken.
- **A 400-frame natural run at ×5:** no errors or warnings.
- **From the menu:** `SceneManager.LoadScene(0)` inside Play Mode, then `Menu.onPlayButtonClick()`. `Final` became active at index 1, with one scene, a new session and 3 cords. There is exactly 1 camera, 1 EventSystem, 1 AudioListener and 1 GameInstaller, and no errors. MainMenu was never saved.
- **Clean-up:** `timeScale` is back to 1, Play Mode is stopped and unpaused, and no callbacks or SessionState keys are left. `Final.unity` is active and not dirty. No format-only re-saves happened (`TimeManager.asset` and `SampleScene.unity` are untouched), so nothing was discarded.
- **Not checked by simulated clicks:** a full cord patch. The `CordBridge` code path is unchanged, and it found the same 3 cables.

Deviations from Design/Plan:
1. **The `GameUI` prefab gained data-sync values,** not layout values: none existed. They are listed above.
2. **`Start` no longer yields.** All objects are in one scene, so the views are bound in the same frame. `GameInstaller` (-100) still runs before Sergei's `Start` methods.

Follow-ups:
- **Tell Sergei:**
  - `Level.unity` is now `Final.unity` (same GUID). His two builder menus above still point at the old path.
  - The scene is shared and the prefabs are owned (D38). Announce root-level additions or removals.
  - Turn `SwitchboardCanvas` and `EventSystem` into prefabs.
- **Rename `LevelScene` to something like `BoardLookup`.** That needs only a code edit, but it touches Design §3 and §7 wording, so it's left for the orchestrator.
- **Old docs** describing Game/Level (listed above).

#### Scene merge verification (orchestrator, 2026-10-18)
- **Tests:** an independent EditMode run passed 255/255.
- **Build list:** `MainMenu` (0), `Final` (1).
- **`Final.unity` against the old `Level.unity`:** 156 lines added and 0 removed. The additions are the two root prefab instances (`Game`, `GameUI`) and their scene-roots entries; nothing of Sergei's changed.
- **`GameUI.prefab`:** only 4 lines changed (TextMeshPro data-sync values).
- **`GameInstaller`:** the additive loading is gone. It finds `MoneyCounterView` and `CallQueueView` by type in its own scene, with a clear error if either is missing or appears twice.
- **Screenshots:** `merge-from-menu.png` shows the full game after Start from the menu, with an emergency ringing at $0.00.
- **Docs updated:**
  - Design: §3, §4.1, §4.2, §4.4 and §7; D01, D29 and D33; the status table.
  - Plan: §5 and §8.
  - CLAUDE.md: ownership, the scene flow, the git conflict spots, and the next step.
  - The playtest scripts of older steps still say "Play from `Game.unity`"; read that as `Final.unity`.


#### Upstream merge and sfx revert (orchestrator, 2026-10-18)
- **Commits (lead approved):**
  - `3002ea8`: the scene merge.
  - `b5a6b68`: the merge of upstream (Mia `f623cd6`, Sergei `9eed19b`).
  - `25a2633`: reverts Sergei's "edited sfx".
- **Why the revert:** it was built on the reverted scene layout, and its call sounds sat on an unpacked copy of our `Game` object in `Level.unity`. The lead's call: Sergei redoes it on `Final.unity` with prefabs; his sounds stay in git history.
- **Net result:** the scene merge plus Mia's four art files.
- **Problem from Mia's art:**
  - Her new `caller-02.png` is 190×190, but its unchanged `.meta` still slices a 190×191 sprite (`caller-02_0`). Unity drops the out-of-bounds sprite, so `GameConfig`'s `caller-02` portrait is missing.
  - The 3 people who use it fail validation and are left out.
  - EditMode: 252/255. Failing: `ContentValidatorTests.RealContentHasNoProblems`, `PeopleFileTests.RealFileHasThirtyThreePeopleAndThreeServicesOnEveryJack`, and `FullBoardTests` (33 of 36 jacks used).
  - This is already on GitHub `main`. Not pushed; waiting for the lead.

#### caller-02 slice fix (2026-10-18)
- **Change (lead approved, Mia's file):** `Assets/Sprites/callers/caller-02.png.meta`, the `caller-02_0` sprite rect height 191 → 190, to fit her new 190×190 image. Same sprite name and internalID (`7311596053533559507`); every other import setting unchanged.
- **Check:** after the re-import, `caller-02_0` loads as 190×190 with the fileID and GUID that `GameConfig.asset` references, so the `caller-02` portrait is back.
- **EditMode:** 255/255 pass (the 3 failures are fixed). Console clean. No format-only re-saves to discard.
- Orchestrator check: an independent EditMode run passed 255/255. Lead playtest of `Final` from MainMenu: all good. The lead asked to push.

#### Caller portrait slices (2026-10-18)
- **Why:** Mia re-exported the portraits at 190 or 191 px, but each `.meta` kept its old single-sprite slice. `caller-03` (now 190×190, slice 190×191) was dropped by Unity, so its portrait was missing.
- **Change (lead approved, Mia's files):** the `caller-NN_0` rect width/height now equals the image size, x 0 and y 0, in `caller-02` (190×191), `caller-03` (190×190) and `caller-06`…`caller-10` (191×191). Sprite names, internalIDs, spriteMode and every other import setting are unchanged. 01, 04, 05, 11 and 12 already matched.
- **Check:** after a forced re-import, all 12 `caller-NN_0` sprites load, each rect equals its texture size, and each fileID/GUID is the one `GameConfig.asset` references.
- **Safety net:** `Assets/Scripts/Game/Editor/CallerPortraitSliceCheck.cs`, an `AssetPostprocessor` for `Assets/Sprites/callers/` only. When a single-sprite slice is bigger than the imported image, it logs a warning naming the file ("re-slice it in the Sprite Editor or ask the orchestrator"). It only warns and changes nothing. Tested on a temporary copy with an oversized slice (warning shown; the copy was deleted).
- **EditMode:** 255/255 pass. Console clean. No format-only re-saves to discard.
- **Seen, not changed:** `Assets/Sprites/board/caller.png` (Mia, 229×190) still slices `caller_0` at 190×191, so Unity logs "rect lies outside of texture" for it. Nothing references it yet; tell Mia or the lead before it gets used.

- Orchestrator check: an independent EditMode run passed 255/255. Seven one-line slice edits plus `CallerPortraitSliceCheck`. Pushed at the lead's request. Mia's `board/caller.png` (229×190, slice 190×191) is unused for now; flagged to the lead.

#### Step 5 prep (orchestrator and lead, 2026-10-18)
- **Pre-flight:** nothing new upstream (`main` = `origin/main` at `6487687`), Unity idle in `Final.unity`, console empty, EditMode 255/255.
- **Sergei:** no commits since his reverted "edited sfx" (`9eed19b`), so the call sounds and music haven't been redone on `Final.unity` yet, and there's no sign he has pulled the new layout. Nothing to stop for.
- **Step 5 coordination items: neither exists on his side.**
  - His cord code has no runtime add: `CableBuilder.CableCount` is a constant 3, and the tray has 3 fixed slots 270 px apart.
  - His book raises no event for a tapped entry; the rows are plain labels.
  - His `UpgradeShopPanel` is still the placeholder: the money plaque plus 4 blank "+" buttons, with no script or text.
- **The designer's upgrade config** replaced the planned `UpgradeDefinition` levels and cost formula. The lead's answers are recorded as D39–D44 in Design §10:
  - one hand-edited `Assets/Configs/Economy.json` with sections
  - one-time entries in chains
  - values set the result, and costs are in cents
  - Notepad as a removable entry
  - board growth by open jacks, starting at 3×3 (Step 7)
  - a pinned secret button with the savings fill and 3 rotating buttons
  - Sergei's missing hooks built by us as coordinated changes, including the text inside his shop buttons
- **Split:** 5a (file, shop, income, secret), 5b (wires and Notepad), 5c (calls, pacing and starting money into the file). Plan §9 has the full 5a brief.
- **Docs updated:** Design 1, 3, 5.9, 5.10, 5.11, 5.12, 6.1–6.4, 9 and 10 (D14, D21, D23, D27, D35; new D39–D44). Plan §8, §9, Steps 5a/5b/5c/6/7/10. CLAUDE.md.

### Step 5a report: 2026-10-18
Result: done

Playable now: the right-hand shop panel sells real offers from the designer's `Assets/Configs/Economy.json`. The top button sells "Better rates / Earn x1.5 / $8.00", then ×2 ($20) and ×3 ($50), and goes back to "+" once the chain is bought out. The two middle buttons keep the "+" look. The bottom button is the secret ("??? / Something big / $250.00"): it fills from the bottom as money is saved and gets a gold frame when affordable. Buying it shows "To be continued..." with a "Keep playing" button, and the button then reads "Owned". A purchase pops "-$8.00" left of the money plaque in a neutral ink. Income upgrades multiply the fee and the per-second pay, including calls already connected; fines are not multiplied.

How to test: as in the plan's playtest script. Two details:
- "+$100" and "Reset upgrades" are in the dev panel (F1) under "+ $10" and "Clear calls". Reset forgets purchases without refunding them.
- The "-$8.00" popup appears to the left of the money plaque, not under it (see deviation 2).

Created:
- `Assets/Configs/Economy.json`: the 4 entries of the brief, one per line, in the brief's order. The names fit the buttons as they are, so nothing was shortened.
- `Docs/EconomyFile.md`: the designer's guide (sections, fields, types and which step builds them, chains, "values set", cents, never renaming ids, Validate Content with an example message).
- Domain, in `Assets/Scripts/Domain/Upgrades/`:
  - `UpgradeKind` (`ExtraIncome = 1` … `Secret = 5`), `IUpgrade`, `PurchaseCheck` (`Ok`, `Unknown`, `Owned`, `NotNext`, `TooExpensive`)
  - `UpgradeService`, with `UpgradeBought` (readonly struct)
  - `StatSheet`, `ShopShelf`, `GoalProgress`
- Content: `EconomyFileData`/`UpgradeEntry` (JsonUtility), `UpgradeRecord`, `EconomyCatalog`, `EconomyFileLoader`.
- Game: `Views/ShopView.cs`, `Views/GoalPanelView.cs`.
- Sergei's folder (approved coordinated change): `Assets/Scripts/UI/UpgradeSlotView.cs`.
- Tests: `Assets/Tests/Domain/UpgradeTests.cs`, `Assets/Tests/Domain/EconomyFileTests.cs`.

Changed:
- `Wallet.TrySpend(long)`.
- `IGameContent.Upgrades`; `GameContent` takes an `EconomyCatalog`.
- `GameSession`:
  - builds `Upgrades`, `Stats`, `Shelf` and `Goal` before `Earnings`, which now reads `() => Stats.IncomeMultiplier`
  - `Tick` ends with `Goal.Tick()` (goal check last, Design 4.4)
  - new dev method `ResetUpgrades()`: clears the bought ids, recomputes the stats, resets the goal's affordable flag and rebuilds the shelf
- `EarningsService`: comment only.
- `GameConfig`: new `economyFile` `TextAsset`, `LoadEconomy()`; `Validate()` adds the economy problems, each prefixed "Economy file: ".
- `ContentValidator.ValidateEconomy(json)`: the economy rules live in `EconomyFileLoader`, which the game uses too, so the menu, the test and the game report the same messages.
- `GameInstaller`:
  - finds `ShopView` and `GoalPanelView` with `FindSingle`
  - finds the `UpgradeSlotView`s by type in the scene
  - loads the economy file and logs each problem as "Economy file: ..."
- `MoneyCounterView`: a `spendPopup`, shown on `UpgradeBought`.
- `DevPanel`: "+ $100" and "Reset upgrades".
- `Assets/Content/GameConfig.asset`: `economyFile` set to `Economy.json`.
- `Fakes.cs`: `FakeContent.Upgrades`, plus a new `FakeUpgrade` with the D43 placeholder shop.
- `FullBoardTests.cs`: passes the real economy to `GameContent`.
- Sergei's `Assets/Editor/UpgradeShopPanelBuilder.cs` (approved):
  - `CreateButtonPrefab` now builds the new parts too, through a shared `BuildSlotParts`.
  - New menu `Tools/Switchboard/Update Upgrade Shop Buttons`. It rebuilds the parts in `UpgradePlaceholderButton.prefab` in place (it is safe to run again), and sets the 4 nested buttons in `UpgradeShopPanel.prefab` to the new size. It touches nothing else: no scene, and no other prefab.

Scene and prefab changes:
- **`UpgradePlaceholderButton.prefab`** (Sergei's, approved; built by the new menu):
  - The button is now 204×144 (was 144×144), with `LayoutElement` 204×144.
  - The root gets `UpgradeSlotView`.
  - New children:
    - `FillArea/Fill`: green, inset 6 px, starts inactive. It is sized by its anchors (`anchorMax.y` = progress), so it needs no sprite.
    - `Details` (CanvasGroup, `blocksRaycasts` off): `Title` 26 bold cream, `Description` 21 light grey-green with wrapping, `Price` 26 bold gold. All TextMeshPro with LiberationSans SDF, auto-size down to 14–16, `raycastTarget` off. Starts inactive.
    - `Highlight`: 4 gold 4 px bars. Starts inactive.
  - The "+" `PlaceholderIcon`, the `Button`, its colours and the `Shadow` are unchanged.
  - The only raycast target is still the button's own image.
- **`UpgradeShopPanel.prefab`** (Sergei's, approved): only the 4 nested buttons' `m_SizeDelta.x` overrides, 144 → 204. `HeaderSlot`, the spacing, the padding and the names are untouched. The 4 buttons still fit inside the 236×860 panel below the plaque (the same vertical layout, with heights unchanged).
- **`GameUI.prefab`** (ours):
  - `Hud/MoneyPlate/SpendPopup`: a copy of `FinePopup`, 190×48, ink (236,230,210), anchored to the plaque's left edge, falling 24 px. Wired to `MoneyCounterView.spendPopup`.
  - `Shop`: a plain `RectTransform` with `ShopView`, and no graphics.
  - `Goal`: a stretched nested Canvas (override sorting, order 15, set through `SerializedObject`) with a `GraphicRaycaster` and `GoalPanelView`. Its child `Dialog` starts inactive and holds:
    - `Blocker`: full screen, dark at 60 %, a raycast target
    - `Panel`: 640×320, dark with a gold outline, holding "To be continued...", the secret's name in gold, and a green "Keep playing" button
- **`Final.unity`:** not changed (git shows no diff). Its existing layout-driven overrides on the shop buttons are untouched, and the runtime layout sizes the buttons from the new `LayoutElement`. MainMenu was never saved. No phone book menu was run, so `LetterTabs` stays hidden.

Tests: 304/304 EditMode (255 before; 49 new):
- `WalletSpendTests` (5): spends when covered; refuses when short, 0 or negative with nothing changed; keeps the fraction.
- `UpgradeServiceTests` (8): buys the next entry; refuses out of order, owned, unknown and too expensive, each with nothing changed; `UpgradeBought` once per purchase; the secret is buyable regardless of the chains; file order and kinds.
- `StatSheetTests` (4): 1 with nothing bought; set, not stacked (1.5 then 2.0 gives 2.0); recompute is stable; the secret leaves it alone.
- `UpgradeEarningsTests` (3): bought mid-call applies to the rest of that call (exactly 50 + 75); the fee after a purchase is ×1.5; a missed emergency is still fined 200 at ×3.
- `ShopShelfTests` (6): file order and the secret never shelved; a bought-out slot takes the next chain while the others stay; empty when nothing is left; buying the secret moves nothing; rebuild after clearing; a secret-only shop has an empty shelf.
- `GoalProgressTests` (4): the clamped ratio; `GoalAffordable` fires once (even after dipping below and recovering); `GoalPurchased` on buying the secret, with progress staying 1; no secret means no goal and no event.
- `UpgradeSessionTests` (2): a session with no upgrades ticks with multiplier 1, an empty shelf and no goal; a session with the shop pays ×2 after two buys, and `ResetUpgrades` resets ids, stats, shelf and affordable flag without a refund.
- `EconomyFileTests` (17): the real file loads 4 entries with no problems; `GameConfig` references it; a valid file passes; one test for each problem in Build 9 (missing file, invalid JSON, empty id, badly formed id with a suggestion, duplicate id, unknown type listing the known ones, `extra_wires` "arrives in Step 5b", empty name, cost 0 and -5, multiplier 1, the exact brief message for 1.2 after 1.5, no secret, a second secret); a broken entry is left out while the others load.

Console: clean. Only the known MCP WebSocket line appeared after recompiles. No `CallerPortraitSliceCheck` warning appeared. No format-only re-saves happened (`TimeManager.asset` and `SampleScene.unity` are untouched), so nothing was discarded.

Screenshots (`Temp/Captures/`):
- `step-05a.png`: the start state at $60. Better rates in slot 1, two "+" placeholders, the secret about 24 % filled and dimmed.
- `step-05a-bought.png`: after a real raycast click on slot 1. Slot 1 shows "Good rates / Earn x2 / $20.00", "-$8.00" sits left of the plaque, and a "-$2.00" fine popup was forced at the same moment to show that the two don't overlap.
- `step-05a-goal.png`: at $252. The secret full and framed in gold.
- `step-05a-continued.png`: the "To be continued..." dialog after a raycast click on the secret. The bottom button reads "??? / Owned". This shot predates the popup widening, so "-$250.00" is clipped in it.
- `step-05a-continued-book.png`: the same dialog with the book open. The book's pages are dimmed under the backdrop, and the dialog is on top.
- `step-05a-720.png`: 1280×720 after "Reset upgrades": the shop back at its start, readable.
- `step-05a-spend-wide.png`: "-$250.00" fitting the widened 190 px popup.

Checked in Play Mode (stepped frames):
- Shop binding: exactly 4 `UpgradeSlotView`s, ordered top to bottom, each 204×144.
- Raycast clicks:
  - The top button's top hit is `UpgradeButton_01`, and clicking it buys ×1.5 ($60 → $52).
  - The secret's click buys it.
  - With the dialog open, the top hit over jack F1 is `Blocker`, so cords can't be used behind it.
  - "Keep playing" is hit and closes the dialog.
- `GoalAffordable` fired exactly once.
- A real call connected after buying ×1.5 paid a 22.5¢ fee (15¢ base) and finished at exactly 180¢ = (15 + 5 × 21) × 1.5.
- About 30 s of game time at ×5 with no errors or warnings.
- MainMenu → `Menu.onPlayButtonClick()` lands in `Final` (index 1) with a session, 4 slots, the shelf on `income_1` and the goal `secret`. No errors.
- Validator: with `income_2` set to 1.2, `Tools/Switchboard/Validate Content` logged "Economy file: Upgrade 2 "income_2": incomeMultiplier 1.2 is not higher than income_1's 1.5. Values in a chain must grow." The file was restored afterwards, and the menu then reports no problems.
- Editor left clean: not in Play Mode, `Final.unity` active and not dirty, `timeScale` 1, Game view 1920×1080, no SessionState keys or callbacks left, console cleared.

Deviations from Design/Plan:
1. **Empty and too-expensive buttons stay interactable.** A click gives Sergei's press tint and nothing else: `ShopView` buys only when `CanBuy` is `Ok`. Setting `interactable` off would switch to Unity's disabled tint (grey at 50 % alpha), and disabling the `Button` resets it to a white tint. Either would change his placeholder look. Too-expensive offers are dimmed instead: the button image at 150 grey, and the texts at 45 % alpha.
2. **The spend popup sits left of the money plaque, not under it.** Under the plaque, it covered the next offer's title right after a purchase, and it would collide with a fine popup landing at the same time. Now it floats over the board's top-right frame margin, briefly covering the "6" column label. It is never over a jack, and it is not a raycast target.
3. **The goal dialog uses sorting order 15:** above the open book (10), below the hand cursor and held plugs (20), so the pointer stays visible while the dialog is open.
4. **The owned secret** shows its name and "Owned", with no price, a full fill and no frame. Clicking it does nothing.
5. **The economy validation rules live in `EconomyFileLoader`**, like the people file's per-entry rules in `PeopleFileLoader`. `ContentValidator.ValidateEconomy` forwards to it. There is no separate `EconomyValidator` class.
6. **The update menu sets the nested buttons' size directly.** `LayoutRebuilder` does nothing inside `LoadPrefabContents`, so the menu writes the size itself. Positions are left to the runtime layout, as before.

Follow-ups:
- **Tell Sergei (D44, after):**
  - his shop button prefab now has `UpgradeSlotView`, `FillArea`, `Details` and `Highlight`
  - the buttons are 204 wide
  - the new menu `Tools/Switchboard/Update Upgrade Shop Buttons` exists, and `CreateButtonPrefab` builds the same parts
  - keep the names `UpgradeShopPanel`, `HeaderSlot` and the 4 buttons
- **Design 4.4:** the tick snippet should end with `goal.Tick(); // goal check last (5a)`. Add `PurchaseCheck` to 5.9 and the dialog's order 15 to 4.3 (orchestrator's docs).
- **"-$250.00" at 34 pt** fills about 160 of the popup's 190 px. Bigger prices from the designer, such as "-$2,500.00", would need a wider popup or auto-sizing.
- **Old screenshots:** `step-05a-continued.png` still shows the old 150 px popup. Retake it in a later pass if needed.
- **Not checked by a real cord patch:** the in-play fee and earnings check connected the call through `OperatorDesk.OnCircuitClosed` with cord 2, not by plugging Sergei's cords. The multiplier path is the same code that the unit tests cover.

#### Step 5a verification (orchestrator, 2026-10-18)
- **Tests:** an independent EditMode run passed 304/304 (255 before, 49 new). Console clean, with no `CallerPortraitSliceCheck` warning.
- **Scope (`git status`):**
  - Sergei's files: only the four approved ones (`UpgradeSlotView.cs` new, `UpgradeShopPanelBuilder.cs`, `UpgradePlaceholderButton.prefab`, `UpgradeShopPanel.prefab`).
  - `UpgradeShopPanel.prefab`'s diff is just the 4 buttons' width, 144 → 204, so `HeaderSlot` is untouched.
  - `Final.unity`, `MainMenu.unity`, `PhoneBook.prefab` (tabs still hidden) and `PhoneBook.json` are unchanged. No format-only re-saves.
- **`Economy.json`:** the brief's 4 entries, one per line, in order. `GameConfig.asset` gained only the `economyFile` reference.
- **Code skim:** stats are recomputed from the bought ids (values set, never stacked); `ShopShelf` refills only the bought-out slot; `GoalProgress.Tick` is last in `GameSession.Tick`; `ShopView` polls with change checks and allocates nothing per frame.
- **Screenshots checked:** `step-05a.png`, `-bought`, `-continued-book` and `-720` look right. Two small visual points for the playtest:
  - The fine popup ("-$2.00", from Step 4, under the counter) now lands on the top offer's title.
  - "-$250.00" sits tight against the left edge of its dark box.
- **Docs updated:** Design status table, 4.3 (popups, dialog order 15), 4.4 (goal tick), 5.8, 5.9 (`PurchaseCheck`, accepted button deviations, where the checks live). Plan §4 lessons, §8. CLAUDE.md.

#### Step 5a playtest (lead, 2026-10-18)
- All checks passed ("all works well"). The lead asked to commit and push.

#### Step 5b prep (orchestrator and lead, 2026-10-18)
- **Pre-flight:**
  - Nothing new upstream (`main` = `origin/main` at `e115c59`), so Sergei hasn't touched his cord, tray or book files.
  - Unity is idle in `Final.unity`, and the console is clean, with no `CallerPortraitSliceCheck` warning.
  - EditMode tests pass 304/304.
- **What Sergei's code has today:**
  - His cord input uses only the cables in its own list that are active (`Owns`/`Usable`), and `Configure` replaces the whole list.
  - The 3 cords live in `Final.unity`, and the 3 tray slots in `CableTray.prefab` (270 px apart). The tray's felt is about 830 px wide, so 6 slots need closer spacing.
  - The book's row labels aren't raycast targets, so a tap on a row hits the open book's button and closes the book.
- **What our code has today:**
  - The spawner already reads `CordCount` live.
  - `ShopShelf` and `ShopView` take the new chains as they are.
- **Approach:**
  - New cords are added while the game runs by copying an existing cord and its tray slot, so the scene and the tray prefab don't change.
  - The three new colours live in our `GameConfig`.
- **The lead approved 6 of Sergei's files (D44):**
  - `CableInputController.cs`
  - `PhoneBookContactRow.cs`
  - `PhoneBookPageView.cs`
  - `PhoneBookView.cs`
  - `PhoneBook.prefab` (the row hit area as a prefab part)
  - `PhoneBookBuilder.cs` (builds the same part; the menu is not run)
- **Design gaps answered by the lead:**
  - A Notepad tap on someone not on the call in hand, or with no call in hand, does nothing, and the book stays open.
  - Rows are tappable only while the Notepad is owned. Before that, a row tap closes the book as today.
- **Plan:** §9 has the full 5b brief.

### Step 5b report: 2026-10-18
Result: done

Playable now: the shop sells "Extra cord / 4 cords / $5.00" in slot 2 and "Notepad / Tap a name: note its jack / $3.00" in slot 3. Buying Extra cord puts a new cord in the tray at once: green, then violet, then ivory, up to 6. The tray closes up so all of them fit. The new cords patch, hold, restore, pay and go back to the tray like the originals. After the Notepad is bought, tapping the caller's or the wanted person's name in the open book adds their jack to the bubble, e.g. "Otis **Bramble** (F3) / wants / Sidney **Hargreaves** (D6)", and the book stays open. A tap on anyone else does nothing. Without the Notepad, a row tap closes the book as before.

How to test: as in the plan's playtest script. Notes:
- Dev "Reset upgrades" keeps cords already added, so after a reset the shop offers "4 cords" again while 4–6 cords are in the tray. Buying it adds nothing until the chain passes the current count. Reset also turns row taps off.
- Known issue (Sergei's input, not changed): tapping a book row, or any button, while holding a cord drops the cord back in the tray.

Created:
- Domain: `Assets/Scripts/Domain/Upgrades/Notepad.cs`.
- Game: `Assets/Scripts/Game/NotepadBookLink.cs`, a plain class that wires the book to the Notepad and is owned by `GameInstaller`.
- Tests:
  - `Assets/Tests/Domain/WiresAndNotepadTests.cs`
  - `Assets/Tests/Game/Editor/CableTrayLayoutTests.cs`
  - `Assets/Tests/Game/Editor/PhoneBookTapTests.cs`

Changed (ours):
- `Assets/Configs/Economy.json`: `wires_1`–`wires_3` and `notepad` after `income_3`, one per line, with the secret still last. The texts fit at 1280×720; "Tap a name: note its jack" wraps to two lines, so nothing was shortened.
- `Docs/EconomyFile.md`:
  - `extra_wires` and `notepad` marked ready, with their fields
  - `wires` explained as a total: above the starting 3, at most 6, growing
  - at most one `notepad`, and deleting it removes the feature
  - an example message for each new rule
  - `extra_clients` still "Coming in Step 7"
- Domain:
  - `IUpgrade.Wires`; `IGameContent.StartingCords`.
  - `Call.CallerNote` and `Call.RecipientNote` (`JackId?`): notes live on the call.
  - `StatSheet`:
    - new members `Cords`, `HasNotepad`, `MaxCords = 6` and `DefaultStartingCords = 3`
    - the `Changed` event, raised after every `Recompute`, including `ResetUpgrades`
    - takes `startingCords`
  - `GameSession`: builds `Notepad`, and passes `content.StartingCords` to `StatSheet`. The spawner already read `CordCount` live, so it is unchanged.
- Content:
  - `UpgradeEntry.wires`, `UpgradeRecord.Wires`.
  - `EconomyFileLoader.Load(json, startingCords)` loads `extra_wires` and `notepad`. It checks that `wires` is above the starting cords, is at most 6 and grows along the chain, and that there is at most one notepad. `extra_clients` still reports "arrives in Step 7".
  - `ContentValidator.ValidateEconomy(json, startingCords)`.
  - `GameConfig`: `startingCords` (3, `Min(1)`) and `extraCordColors` (green 92,184,92; violet 156,104,214; ivory 240,232,206), both passed to the loader and the validator. `GameContent.StartingCords`.
- Game:
  - **`CordBridge`:**
    - `Attach(scene, extraCordColors)` also finds the one `CableInputController` by type, and logs a clear error otherwise.
    - `EnsureCords(total)` calls `AddCable` with the next colour until the total is reached or `AddCable` returns null.
    - Each new cable is appended to `cables` and to `observed` with its current state. New cord ids are 3, 4, 5.
  - **`GameInstaller`:**
    - Warns if the scene's cord count differs from `startingCords`.
    - Calls `EnsureCords(Stats.Cords)` at start and on `Stats.Changed`.
    - Finds the one `PhoneBookView` and builds `NotepadBookLink`, which sets `ContactTapsEnabled = HasNotepad` and routes `ContactTapped` to `Notepad.Note(new JackId(socket))`.
    - Unsubscribes in `OnDestroy`.
  - **`CallQueueView.Bind`** takes the Notepad (optional) and forwards `Noted` to the card.
  - **`CallCardView`:**
    - The in-hand bubble adds " (F3)" after a noted name at 85 % size (`jackTextSize`), keeping the bold surname.
    - `ShowNotes()` swaps in the new text without re-typing: a finished bubble stays finished, and one still typing goes on from where it was (`TypingReveal.Resize`).
- `Assets/Content/GameConfig.asset`: `startingCords` 3 and the 3 colours.
- Tests: `Fakes.cs` (`FakeContent.StartingCords`, `FakeUpgrade.Wires`, `WireUpgrade`, `NotepadUpgrade`, `FullShop`), `EconomyFileTests.cs`, `TypingRevealTests.cs`.

Changed (Sergei's, the 6 approved coordinated changes, D44):
- **`Assets/Scripts/UI/CableInputController.cs`:**
  - `MaxCables = 6` and `public CableController AddCable(Color)`. It works in these steps:
    1. copies the last cable under an inactive holder, so the copy's `OnEnable` can't bind the source's tray slot
    2. copies that cable's `StoredCableView` under the tray
    3. destroys any hover `Outline` copied across
    4. names the copies `Cable_NN` / `StoredCable_NN`
    5. calls `SetTrayCableView` and `SetCableColor`
    6. reparents the cable, which starts it with a fresh connection in the tray
    7. appends it to the list, re-spaces the tray and returns it (or null when full or there is nothing to copy)
  - Before copying, the source slot's highlight is cleared.
  - Tray maths in public statics: `TraySpacing(count)` = min(270, 830 ÷ count), `TraySlotPosition`, `TraySlotSize` (width scales with the spacing; the height stays 94.5) and `TrayPreviewScaleFor` (0.45 × spacing ÷ 270). With 3 cords these give exactly today's −270/0/270, 252×94.5 and 0.45.
  - Nothing else in the file changed.
- **`PhoneBookContactRow.cs`:**
  - An optional third `Configure` argument for the `tapArea` image, plus `SetTappable(bool)`, `IsTappable`, and `event Action<PhoneBookContact> Tapped`.
  - It implements `IPointerClickHandler` (left button, only while tappable) and an empty `IPointerDownHandler` (see deviation 1).
- **`PhoneBookPageView.cs`:** subscribes the rows it creates, forwards `ContactTapped`, and applies `SetTappable` to new and existing rows.
- **`PhoneBookView.cs`:** `event Action<string, string> ContactTapped` (name, socket) and `ContactTapsEnabled` (off by default), passed to both pages. Pages are subscribed in `Awake` and in `ConfigureDirectory`, and unsubscribed in `OnDestroy`. Page turns are unchanged.
- **`PhoneBook.prefab`** (edited with `PrefabUtility.LoadPrefabContents`; no menu was run):
  - Both `ContactTemplate` rows get a `CanvasRenderer` and a transparent `Image` (`raycastTarget` off), assigned to `tapArea`.
  - The diff has only these additions; `OpenBook/LetterTabs` is still inactive.
- **`PhoneBookBuilder.cs`:** `DirectoryPage` builds the same image and passes it to `Configure`. The menu was not run.

Scene and prefab changes:
- `Final.unity`: no diff. `CableTray.prefab`, `CableController`, `StoredCableView`, `CableBuilder`, `CableTrayBuilder` and `MainMenu.unity` are untouched.
- `GameUI.prefab` and `Game.prefab`: unchanged. The new `jackTextSize` uses its code default of 85.

Tests: 333/333 EditMode (304 before; 29 new):
- `CordStatTests` (5): starting cords with nothing bought; set, not stacked (4 then 5 gives 5); `HasNotepad` false, then true; `Changed` on purchase and on reset; a session reset turns the Notepad off.
- `GrowingCordsSpawnTests` (1): with 5 waiting calls, the load drops from 2 to 1.5 to 1 as the fake switchboard's cord count goes 3, 4, 6.
- `NotepadTests` (8):
  - does nothing without the upgrade
  - notes the caller and the recipient once each and raises `Noted`
  - ignores a jack not on the call
  - does nothing with no call in hand
  - notes survive a swap and don't leak
  - a callback starts with no notes
  - works for an emergency's service recipient
  - a reset turns it off
- `EconomyFileTests`:
  - The real file loads 8 entries with no problems, and `GameConfig` validates clean.
  - The real shelf is `income_1`, `wires_1`, `notepad`; after buying the notepad, slot 3 goes empty and slots 1–2 stay.
  - Checks for wires and the Notepad: they load; wires not growing (the exact message); wires ≤ starting cords (also with 4 starting cords); wires > 6; a second notepad.
  - "Not built yet" now uses `extra_clients`, which reports Step 7.
- `CableTrayLayoutTests` (4): 3 slots match today's layout; 4, 5 and 6 slots fit 830 px with no overlap, centred, with the preview no wider than its slot.
- `PhoneBookTapTests` (3): with taps on, a click on a row raises its name and socket (the book's own upper-case socket); with taps off, the hit area isn't a raycast target and a click raises nothing; rows made after taps are on are tappable too.
- `TypingRevealTests` (2): a finished line stays finished when it grows; a line still typing goes on.

Console: clean. The only messages during the work:
- the known MCP WebSocket line after recompiles
- two URP "Ignoring depth surface load/store action as it is memoryless" lines after switching the Game view to 1280×720 (engine, not our code)
- one "FindAllObjectsOfType: Invalid Type" from a failed probe of mine (a wrong assembly name for the menu's `Menu` type), not from game code

No `CallerPortraitSliceCheck` warning appeared. No format-only re-saves happened (`TimeManager.asset` and `SampleScene.unity` untouched), so nothing was discarded.

Screenshots (`Temp/Captures/`):
- `step-05b.png`: the shop at the start: Better rates, Extra cord / 4 cords, Notepad, the secret; 3 cords in the tray.
- `step-05b-six-connected.png`: all 6 cords connected to 6 real calls, each card ringed in its cord's colour.
- `step-05b-cords.png`: 6 cords; the new violet one connected on the board, the other 5 in the tray.
- `step-05b-notepad.png`: the book open, and the bubble "Otis Bramble (F3) / wants / Sidney Hargreaves (D6)".
- `step-05b-720.png`: 1280×720, the tray with 6 cords (5 in the tray, 1 connected), all readable.
- `step-05b-720-shop.png`: 1280×720 after dev "Reset upgrades": the shop back at its start (Extra cord and Notepad readable), the 6 cords kept.

Checked in Play Mode (stepped frames, clicks inside `Canvas.willRenderCanvases`; `timeScale` set to 10 once, reset to 1):
- **Wires bought by raycast clicks** on shop button 2 (`UpgradeButton_02` was the top hit), three times: 4, 5, then 6 cords. Money fell $5, $8, then $13; the button went to "+".
- **Tray:** 6 slots at ±345.8 / ±207.5 / ±69.2, each 129×94.5 with preview scale 0.23. None carries an outline.
- **All 6 cords** picked up from the tray and patched with `ProcessPointer` onto 6 real calls. All 6 connected, paid their fee and earned.
- **Hold and restore on a new cord:** pulling a plug of Cable_04 put its call on hold; a click off the board restored it, and pay resumed with no second fee. Earlier, a test click 75 px from B6 landed on jack A6's hit area and re-patched a cord to the wrong jack, which correctly cut that call off. That was my test's mistake, not a bug.
- **Back to the tray:** pulling both plugs of Cable_06 returned it to the tray and cut the call off. Cable_04's call finished (Paid 156¢) and the cord went back to the tray.
- **Buying while cords are busy** (fresh session): with all 3 originals connected, buying wires_1 put Cable_04 in the tray, and the 3 calls stayed connected. Buying wires_2 while Cable_04 was held, and wires_3 while it was half-patched, gave fresh tray cords and disturbed nothing.
- **Hover highlight:** with Cable_03's tray slot highlighted by hover, buying wires_1 gave a new slot that isn't highlighted. The copied outlines were destroyed by the next frame (0 left), and the source's highlight was cleared.
- **Notepad:**
  - Before buying: a raycast click on a row hits `OpenBook`, and the book closes.
  - After buying via shop button 3, with a call in hand: clicks on the caller's row and the recipient's row (the top hits were the rows themselves) noted F3 and D6, and the book stayed open. A click on another row changed nothing, and the book stayed open.
  - Swapping to another call and back kept both notes; the other call had none.
  - The violet cord then connected that call using the noted jacks.
- **Bubble fit:** the 4 longest names with jacks (e.g. "Reginald Underhill (E5)") fit the 134×134 bubble with no overflow, auto-sizing to about 20 pt (about 22 pt without notes). No shrink beyond the 85 % jack size was needed.
- **Dev "Reset upgrades":** taps off; 6 cords kept; `Stats.Cords` back to 3; shelf back to income_1, wires_1, notepad.
- **MainMenu → Start** (`Menu.onPlayButtonClick`) lands in `Final` (index 1) with 3 cords, taps off and the shelf on wires_1. No errors.
- **Validator:**
  - With `wires_2` set to 4 (in memory only), the check reports `Economy file: Upgrade 5 "wires_2": wires 4 is not higher than wires_1's 4. Values in a chain must grow.`
  - `Tools/Switchboard/Validate Content` on the real file reports "no problems found".
- **Editor left clean:** not in Play Mode, `Final.unity` active and not dirty, `timeScale` 1, Game view 1920×1080, no SessionState keys or callbacks left, console cleared.

Deviations from Design/Plan:
1. **`PhoneBookContactRow` also implements an empty `IPointerDownHandler`.** Without it, the press on a tappable row bubbles up to the open book's `Button`, which flashes its pressed tint over the whole book. The click itself goes to the row either way.
2. **`PhoneBookContactRow.Configure`** takes the hit area as an optional third argument, so existing callers still compile.
3. **Only the tray slot's width narrows** with more cords; the height stays 94.5. The brief asked for a narrower hit area; keeping the height keeps the slots easy to tap.
4. **The tray slot's preview is found by its builder name, `Preview`** (`rect.Find("Preview")`), because `StoredCableView.preview` is private and that file wasn't approved.
5. **The cords stat after a reset:** Dev "Reset upgrades" sets `Stats.Cords` back to 3 while the added cords stay in the tray. As the brief allows, no cord is removed.

Follow-ups:
- **Tell Sergei (D44, after):**
  - `CableInputController.AddCable(Color)`, `MaxCables = 6` and the `Tray*` statics
  - the book's `ContactTapped` / `ContactTapsEnabled`, the rows' `tapArea` and `Tapped`
  - the new `Image` on both `ContactTemplate`s
  - `PhoneBookBuilder.DirectoryPage` builds it
  - Keep `StoredCable_NN`'s child name `Preview`; the re-spacing scales it.
- **Design docs** (orchestrator): 5.9 (`StatSheet.Cords` / `HasNotepad` / `Changed`, `Notepad`), 4.5 (`EnsureCords`), 6.1 (`startingCords`, `extraCordColors`) and 3 (the new public members used).
- **The 830 px usable width** is a constant in Sergei's file. If Mia's tray art changes, it needs updating.
- **With 6 cords** each tray preview is about 51 % of today's size. That is readable at 720, but a playtest should confirm it on a phone (Step 9).

#### Step 5b verification (orchestrator, 2026-10-18)
- **Tests:** an independent EditMode run passed 333/333 (304 before, 29 new). The console has no errors or warnings, and there is no `CallerPortraitSliceCheck` warning.
- **Scope (`git status`):**
  - Sergei's files: exactly the 6 approved (`CableInputController.cs`, `PhoneBookContactRow.cs`, `PhoneBookPageView.cs`, `PhoneBookView.cs`, `PhoneBook.prefab`, `PhoneBookBuilder.cs`).
  - `PhoneBook.prefab`'s diff only adds the transparent `Image` (not a raycast target) to the two `ContactTemplate` rows. `LetterTabs` is untouched.
  - `Final.unity`, `MainMenu.unity`, `CableTray.prefab`, `GameUI.prefab`, `Game.prefab` and `PhoneBook.json` are unchanged. There were no format-only re-saves.
- **Code skim:**
  - `AddCable` copies the last cord under an inactive holder, so the copy can't bind the source's tray slot. It drops copied outlines and re-spaces the tray with public static maths, which give today's layout for 3 cords.
  - `CordBridge.EnsureCords` grows its arrays only on a purchase and records each new cord's current state, so nothing spurious is reported.
  - `Notepad` notes only the caller's or recipient's jack on the call in hand, with the notes stored on the `Call`.
  - `NotepadBookLink` turns row taps on and off with `HasNotepad`.
- **Screenshots checked:**
  - `step-05b-six-connected.png`: 6 cords connected on the board, in green, violet and ivory alongside Sergei's 3.
  - `step-05b-notepad.png`: the bubble reads "Otis Bramble (F3) / wants / Sidney Hargreaves (D6)" with the book open. A long surname pushes its jack onto its own line, which is still readable.
  - `step-05b-720.png`: the tray with 6 cords at 1280×720. The previews are small but distinct.
- **Accepted deviations:** the 5 in the report. Two are worth knowing:
  - The tray preview is found by its child name `Preview`, so Sergei should keep that name.
  - After dev "Reset upgrades", the shop offers "4 cords" again while 6 are in the tray (dev only).
- **Docs updated:** Design status table, §3 (new public members), 4.5 (adding cords), 5.9. Plan §4 lesson and §8. CLAUDE.md.

#### Step 5b playtest (lead, 2026-10-18)
- All checks passed ("all good"). The lead asked to commit.

### Step 5b fix report: 2026-10-18
Result: done

Playable now: after the team merge (a173e75), the Notepad follows the new "accept many calls" rules. A tapped book name notes that person's jack on every accepted call still waiting to connect (Taken or Connecting) where they are the caller or the person wanted. Each card's bubble shows its own notes when it is clicked. Callers 04 and 05 have their portraits again.

How to test:
1. Play `Final.unity`. F1, then "+ $10" a few times; buy the Notepad.
2. Accept two cards, ideally two that share a person. Open the book and tap that person: both calls get the jack. Tap the other names on each call too.
3. Click each accepted card. Each bubble shows its own jacks, e.g. "Doris Cobbold (D1) / wants / Rose Ellery (A4)", and nothing needs tapping again.
4. Tap the name of someone on a ringing (not yet accepted) card, or on a connected or held call: nothing changes, and the book stays open.
5. Buy Extra cord, connect a call with the new green cord: it pays, and the cord stays plugged after the call ends.

Created: none.

Changed:
- `Assets/Scripts/Domain/Upgrades/Notepad.cs`:
  - `Note(jack)` walks `CallService.Live` (on a tap, so no per-tick work) and notes the jack on every call in `Taken` or `Connecting` whose caller or recipient is listed at that jack.
  - It no longer uses `CallService.InHand`.
  - It returns true if at least one call got a new note, and raises `Noted(CallId)` once per call that got one. A person who is both caller and recipient on the same call is impossible, but would still raise once.
  - Ringing, connected, on-hold and ended calls are never noted. Without the Notepad, or with no matching accepted call, nothing happens.
- `Assets/Tests/Domain/WiresAndNotepadTests.cs` (`NotepadTests`):
  - The swap test is gone.
  - Two tests are renamed for the new rule: `IgnoresAJackNotOnAnyAcceptedCall`, and `DoesNothingWithoutTheUpgrade`, which now has two accepted calls.
  - New: `TwoAcceptedCallsSharingAPersonBothGetTheNote` (Noted once per call), `TappingTheSamePersonTwiceAddsNothingNew`, `ARingingCallGetsNoNote` (it replaces "no call in hand"), `AConnectingCallGetsNotes`, `ConnectedAndOnHoldCallsGetNoNote` and `NotesStayWhenAnotherCardIsAccepted` (no leak to the later call; a shared person then notes both).
  - Kept: the caller and recipient noted once each, a callback starts with no notes, an emergency's service recipient, and a reset turning it off.
- **Mia's folder (the one approved coordinated change):** `Assets/Sprites/callers/caller-04.png.meta` and `caller-05.png.meta`. The slice rect height is now 190 instead of 191; the diff is one line in each. Sprite names (`caller-04_0`, `caller-05_0`), `spriteID` and `internalID` are unchanged. Both were force-reimported and each loads its sprite again. `CallerPortraitSliceCheck` logged nothing.

Scene and prefab changes: none. `Final.unity`, `GameUI.prefab` and `Game.prefab` didn't need changes. The merged `CallCardView` works as it is: bubbles are built from `InHandText()` when a card's details turn visible, so notes written while another card's bubble is open show when that card is clicked. `ShowNotes()` updates an open bubble in place.

Tests: 361/361 EditMode (357 after the merge, with 5 failing). All 5 now pass:
- 4 from the portrait fix: `RealContentHasNoProblems`, `GameConfigReferencesTheRealFile`, `RealFileHasThirtyThreePeopleAndThreeServicesOnEveryJack` and `CallersComeFromTheWholeBoardAndNobodyIsInTwoCalls`.
- The swap test was replaced.
- Count: 2 removed (the swap test and `DoesNothingWithNoCallInHand`), 6 added. `NotepadTests` went from 8 to 12.

Console: clean. The only messages during the work were:
- the known MCP WebSocket line after a recompile
- two URP "Ignoring depth surface load/store action as it is memoryless" lines in Play Mode (engine)

Screenshots (`Temp/Captures/`):
- `step-05b-fix-notepad.png`: book open; card 1's bubble reads "Doris Cobbold (D1) / wants / Rose Ellery (A4)".
- `step-05b-fix-notepad-card2.png`: the same moment after clicking card 2; its bubble reads "Otis Bramble (F3) / wants / Doris Cobbold (D1)".
- `step-05b-fix-cords.png`: the green 4th cord still plugged in D1–A4 after its call finished; 3 cords in the tray; Notepad slot at "+".

Checked in Play Mode (stepped frames; clicks with EventSystem raycasts inside `Canvas.willRenderCanvases`):
- **Setup:** bought `notepad` and `wires_1` (4 cords, row taps on). Added three calls with people on the open spread: Doris (D1) wants Rose (A4); Otis (F3) wants Doris; and a third, Stanley (B5) wants Doris, left ringing.
- **Accepting:** clicked cards 1 and 2. The top hit was each card's `Portrait`, and both calls went to Taken.
- **Tapping rows:** the top hits were the rows themselves.
  - Doris noted D1 on both accepted calls: caller on call 1, recipient on call 2.
  - Rose noted A4 on call 1, and Otis noted F3 on call 2.
  - Ivy (C6, on no call) changed nothing.
  - The ringing call 3 got no note, although Doris is on it.
  - The book stayed open throughout.
- **Bubbles:** clicking card 1, then card 2, showed each one's own jacks, fully typed: 41 of 41 and 42 of 42 characters. Only the clicked card shows a bubble.
- **Cords:** picked up `Cable_04` from the tray with `ProcessPointer` and patched D1, then A4. The call connected on cord id 3 and paid its 50¢ fee. At timeScale 5 (reset to 1 afterwards) it finished: "Finished", 20¢ earned. `Cable_04` stayed Connected in D1/A4 afterwards, as the new rules want.
- **Editor left clean:** not in Play Mode, `Final.unity` active and not dirty, `timeScale` 1, Game view 1920×1080, SessionState keys erased, no callbacks left, console cleared.
- One leftover existed only for that Play session: a test subscription to `CallEnded`. It went when Play Mode stopped.

Format-only re-saves: none. `TimeManager.asset` and `SampleScene.unity` are untouched, so nothing was discarded. Sergei's files: none changed. Mia's: only the two `.meta` files above.

Deviations from Design/Plan: none from the lead's new Notepad rule. The 5b brief's "call in hand" wording (Plan, Design 5.9) is now out of date; the orchestrator is updating those docs.

Follow-ups:
- **Tell Mia:** `caller-04` and `caller-05` were re-exported at 190×190 while their slices stayed 190×191. We set the slice height to 190. Next time, re-slice in the Sprite Editor, or keep the image size.
- **A person in two calls:** the spawner normally avoids putting one person in two live calls ("nobody is in two calls"), so a shared person mostly happens through callbacks or emergencies. The Notepad handles it either way.
- **Stale hidden bubble text:** a pooled card that has never shown its bubble keeps an old label text while hidden (seen on a ringing card). It is never visible, because `Refresh` rewrites the text when the bubble opens. Cosmetic only; no action needed.

#### Upstream merge and Step 5b fix verification (orchestrator, 2026-10-18)
- **Merge `a173e75`:** brought in 7 upstream commits.
  - Mia's art, and Sergei's `2fc3810` "added sfx, edit view, edit game balance".
  - The 2 text conflicts (`GameInstaller.cs`, `CallQueueView.cs`) were one Bind argument each from both sides; both were kept.
  - Unity asked to reload `Final.unity` (the lead clicked Reload).
  - The merge compiled. EditMode was 352/357: 4 portrait failures and our Notepad swap test.
- **Lead decisions:**
  - Sergei's persistent wiring is adopted as the design (D45, replacing D03).
  - The Notepad notes on every accepted call that person is part of, including duplicates (D46).
  - Fixing the two portrait slices in Mia's folder is approved as a one-off.
- **Portraits:** Mia's new `caller-04`/`caller-05` are 190×190, but their slices were 190×191, so Unity dropped both sprites. The fix pass set the height to 190 (one line per `.meta`; the ids are unchanged).
- **Verification:**
  - An independent EditMode run passed 361/361, and the console is clean.
  - `git status`: only `Notepad.cs`, `WiresAndNotepadTests.cs`, the two `.meta` files and the docs changed.
  - `Notepad.Note` loops over `Calls.Live` on a tap only and notes Taken/Connecting calls.
  - `step-05b-fix-notepad.png` shows "Doris Cobbold (D1) / wants / Rose Ellery (A4)" with two accepted cards and two "New order!" cards.
- **Docs:** Design (D45, D46, core loop, 5.1, 5.6, 5.9, §3, status table), CLAUDE.md (notes for Sergei), Plan §8 (tell Mia).

