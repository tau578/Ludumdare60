# Progress

| Step | Status | Built | Verified | Playtested | Commit |
|---|---|---|---|---|---|
| 1. First call | done | ✅ 2026-10-17 (54 min) | ✅ 72/72 tests, scope clean | ✅ all 9 checks | e6f44e5 |
| 1b. Fee, book, layout | done | ✅ 2026-10-17 (20 min) | ✅ 91/91 tests, scope clean | ✅ | 60c7a88 |
| 1c. Money slot in shop header | done | ✅ 2026-10-17 (8 min) | ✅ 91/91 tests | ✅ | 47735c1 |
| 2a. Queue, callbacks, dev panel | done | ✅ 2026-10-17 (24 min) | ✅ 156/156 tests, scope clean | ✅ | |
| 2b. Hold and resume | not started | | | | |
| 3. Phone book and cards | not started | | | | |
| 4. Emergencies and fines | not started | | | | |
| 5. Upgrades, shop, goal | not started | | | | |
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
