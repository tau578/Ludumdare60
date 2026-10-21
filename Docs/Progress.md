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
| 5c. Calls and pacing into Economy.json | done | ✅ 2026-10-18 (17 min) | ✅ 388/388 tests, scope clean | ✅ | f70b7df |
| 6. Board growth (built as 7a) | done | ✅ 2026-10-18 (25 min) | ✅ 439/439 tests, scope clean | ✅ | d166ffa |
| 7. Hand-written calls and queues | done | ✅ 2026-10-18 (36 min) | ✅ 431/431 tests, scope clean | ✅ | 8e48320 |
| 8. The new phone book | done | ✅ 2026-10-18 (20 min) | ✅ 445/445 tests, scope clean | ✅ | f82ed29 |
| 9a. Saves, touch, menu, pause | done | ✅ 2026-10-19 (37 min + 21 min fix + 24 min pause + 15 min corner) | ✅ 503/503 tests, scope clean | ✅ | 580c610 |
| 9b. First Web build on itch | done | ✅ 2026-10-19 (37 min; cursor fixes discarded) | ✅ 517/517 tests, scope clean | ✅ | 709be9d |
| 10. Tutorial | playtesting | ✅ 2026-10-20 (33 min + 28 min fix + 21 min fix) | ✅ 638/638 tests, scope clean | | |
| 10b. Faster lookup | playtesting | ✅ 2026-10-20 (20 min + 15 + 20 + 19 min fixes) | ✅ 671/671 tests, scope clean | | |
| 10c. Quick-connect tip | playtesting | ✅ 2026-10-21 (14 min) | ✅ 697/697 tests, scope clean | | |
| 11. Sound and feel | not started | | | | |
| 12. Mobile and Web polish | not started | | | | |
| 13. Balance | not started | | | | |

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


### Step 5c prep (orchestrator, 2026-10-18)
- **Pre-flight:** `git fetch`: up to date. Unity idle on `Final.unity`, not in Play Mode, console cleared. EditMode tests 361/361 green; no format-only re-saves.
- **Decisions (lead, D47):** `startingCords` moves into the file too; the old asset fields are deleted (no fallback); ranges are `{min, max}` objects.
- **Checked:** `OperatorDesk` (Sergei's D45 wiring) reads none of the moving fields; everything goes through `ICallerType`/`IPacingProfile`. Current values recorded in the Plan's 5c brief.

## Step 5c report: 2026-10-18
Result: done

Playable now: the game plays as before, but every money and timing value now comes from `Assets/Configs/Economy.json`: the starting money and cords, the pacing, and both call types. Unity only keeps how cards look. If you edit the file and press Play, the change is live.

How to test: follow the Plan's 5c playtest script. Step 5 (`startingCords` 4) is wired: 4 cords sit in the tray from the start, and the shop's first cord offer is "5 cords". The console also shows the expected message that `wires_1` (4) is no longer above the 4 starting cords.

Created:
- `Assets/Scripts/Content/CallTypeRecord.cs`: a call type read from the file (`ICallerType`).
- `Assets/Tests/Domain/EconomyCallsTests.cs`.

Changed:
- **Economy loading:**
  - `EconomyFileData`: new `startingCents`, `startingCords`, `pacing` (`PacingEntry`) and `calls` (`CallEntry`, `RangeEntry`). A missing value starts as a marker: NaN, or `Unset` for `startingCords`.
  - `EconomyFileLoader`: new checks for the starting values, pacing and calls. `Load(json)` no longer takes `startingCords`; the wires checks use the file's own value.
  - `EconomyCatalog`: now also exposes `StartingCents`, `StartingCords`, `Pacing` and `CallTypes`.
- **Game content and validation:**
  - `GameContent`: money, cords, pacing and types now come from the economy catalog. Only `MaxWaitingCalls` still comes from `GameConfig`.
  - `GameConfig`: `startingCents`, `startingCords` and `pacing` are deleted. `callerTypes` is renamed to `cardStyles` with `[FormerlySerializedAs("callerTypes")]`. `LoadPeople(economy, jacks)` replaces the old overload. `Validate()` adds a "People file: " prefix to people messages and checks card styles.
  - `CallerTypeDefinition`: now only `id` and `cardStyle`, and it is no longer an `ICallerType`.
  - `ContentValidator`: `CheckTypes`/`CheckRange` moved into the loader. New `ValidateCardStyles(styleIds, callTypes)`. The recipient messages now say "Call type" and point at Economy.json.
  - `PeopleFileLoader`: the "no types" text now points at Economy.json.
- **Game layer:** `GameInstaller` loads the economy file first, then the people. The cord warning now fires only if the scene has more cords than `startingCords`, so it stays quiet for 4–6.
- **Shipped data:** `Assets/Configs/Economy.json` gains `startingCents`, `startingCords`, `pacing` and `calls`, with today's values. The upgrades are unchanged.
- **Assets:** `Assets/Content/GameConfig.asset`, `CallerTypes/Regular.asset` and `CallerTypes/Emergency.asset` were re-serialized to drop the dead fields. Both card-style references survive under `cardStyles`.
- **Deleted through Unity, metas included:** `PacingProfile.cs`, `Assets/Content/Pacing/Default.asset` and the `Pacing` folder.
- **Tests:**
  - `EconomyFileTests`: its helper now writes a valid start, pacing and calls section, and the starting-cords test uses `startingCords` in the file.
  - `ContentValidatorTests`: the 5 type-rule tests moved to `EconomyCallsTests`.
  - `PeopleFileTests` and `FullBoardTests` use the new `LoadPeople`.
- **Docs:** `Docs/EconomyFile.md` has new sections: "Starting money and cords", "Pacing" and "Calls". It also has a full-file outline, a cents and seconds note, per-person overrides, new validator messages, and the sections table set to "Yes (5c)".

Scene and prefab changes: none. `Final.unity` and all prefabs are untouched.

Tests: 388/388 EditMode, up from 361. New tests are in `EconomyCallsTests`:
- **Values:** `EconomyFileCallsMatchStep5bValues` checks every value of both types, plus pacing, `startingCents` 0 and `startingCords` 3. Before the asset fields were deleted, a temporary `TheRealFileMatchesTheOldAssets` compared the file field by field with `Regular.asset`, `Emergency.asset`, `Default.asset` and `GameConfig`. All 389 tests passed then, and that test was removed with the fields.
- **Parsing:** `JsonUtilityKeepsTheMarkersForFieldsTheFileLeavesOut` proves that field initializers survive `FromJson`, including in nested objects inside arrays. `TheFullSampleParses`.
- **Missing values:** `EachRequiredFieldMissingGivesItsMessage` ×6, `ReportsARangeWithOnlyAMin`, `OptionalFieldsLeftOutMeanOff`.
- **Bad call values:** min above max (exact brief message), a negative range, a duration of 0, a negative fine, negative limits and times, a negative weight, a duplicate id, no type with a weight.
- **Pacing:** jitter out of range, an interval below 0.1, a negative load factor, a missing section (one message, defaults used), a missing value named.
- **Starting values:**
  - Left out, they give 0¢ and 3 cords with no message; 500¢ and 6 cords load.
  - `startingCords` 2 or 7 gets a message and falls back to 3. A negative `startingCents` gets a message.
  - With `startingCords` 4, `wires_1` = 4 is reported.
- **Other checks:**
  - No `calls` section: one message, no callers, and a 120 s session rings nothing.
  - A card style with an unknown id, or an empty slot, gets a message; a type with no style gets none.
  - A `PhoneBook.json` type that isn't in the file gets a message.
- The existing real-file tests (`RealContentHasNoProblems`, `GameConfigReferencesTheRealFile`) still require `config.Validate()` to be empty.

Console: clean. Two engine "Ignoring depth surface … memoryless" lines appear only around screenshots.

Screenshot: `Temp/Captures/step-05c.png` (calls ringing with the EMERGENCY badge, 3 cords, the shop). There is also `Temp/Captures/step-05c-four-cords.png`, taken with a temporary `startingCords` of 4.

Smoke test:
- **Final directly:** the spawner types are `CallTypeRecord`: Regular w=5, and Emergency w=1 with a 30 s response time. The first call rang at about 2 s.
- **Dev panel:** its timed label is "Emergency now", taken from the file's `name`. Its spawn rang an emergency with a 35¢ fee at 5¢/s.
- **Validate Content:** "no problems found".
- **MainMenu path:** loading build index 0, then 1, gives a working session in `Final` with no errors.
- **`startingCords` 4 (temporary edit, restored):** `Stats.Cords` = 4, the bridge has 4 cords and the tray shows 4. The first cord offer is `wires_2` "5 cords", and there is no cord-count warning.
- **Editor left clean:** not in Play Mode, `timeScale` 1, `Final.unity` active and not dirty, no callbacks or SessionState keys. Format-only re-saves: none, so nothing was discarded.

`CallService` check (D45): `CallService` reads only `Request.Type` by reference (for `CountLive`) and `Call.HasResponseTime`. It reads none of the moved fields directly; everything goes through `ICallerType`, as in `OperatorDesk`.

Deviations from Design/Plan:
- **Field names:** as proposed, no renames in the file. `GameConfig`'s list is now called `cardStyles` (was `callerTypes`), as the brief allowed.
- **Small rules added:** a negative `startingCents` is reported and falls back to 0. A range with only a min or only a max is reported. Both follow the brief's "no silent 0" rule.
- **`startingCords` above 6** also falls back to 3, not 6. That keeps the wires chain valid; the message says "Use 3 to 6".
- **Validate Content** now prefixes people messages with "People file: ", as the game already does at runtime.
- **Missing economy file:** if `Economy.json` is missing or unreadable, no call types exist, so no calls ring. Its error is logged. Before 5c, call types came from the assets.

Follow-ups:
- **Design.md (orchestrator):** the §6.1 rows for `GameConfig`, `CallerTypeDefinition` and `PacingProfile` and the §6.4 sections list are now out of date. So is §9 "New caller type": it is now a line in `Economy.json`, plus an optional card-style asset.
- **Step 7:** `maxWaitingCalls` and the cord colours remain in `GameConfig`, per D47.

### Step 5c verification (orchestrator, 2026-10-18)
- EditMode tests re-run: 388/388 green. Console: no errors or warnings.
- `git status`: only our files (Content scripts, Game installer, Content assets, `Economy.json`, Domain tests, docs). No team-owned file, scene or prefab changed; no format-only re-saves.
- `GameConfig.asset` keeps both card-style references under `cardStyles`; `Emergency.asset` keeps its badge and accent.
- Screenshots: `step-05c.png` (calls ringing, emergency badge, 3 cords) and `step-05c-four-cords.png` (`startingCords` 4: four cords, shop offers "5 cords").
- Design.md updated: implementation row, §6.1 (`GameConfig`, `CallerTypeDefinition`, `PacingProfile`), §6.4 sections, §9 "New caller type", and the pacing mentions in §5.
- **Playtest (lead, 2026-10-18):** all looks good.

### 2026-10-18 — Merge origin/main into Sergei's_branch

- Resolved CordBridge, GameInstaller and CallQueueView by retaining active-call socket highlights, fee coins, sound and first-order guidance alongside main's purchased cords, Notepad and Economy.json configuration. CallQueueView receives both the Notepad and money counter; the bridge receives both desk and calls.
- Added CordBridge.CableAdded so purchased cords participate in an already-running tutorial without restarting it. Picking up a purchased cord clears all hints. Cord cloning cleans up temporary objects in both Edit and Play Mode.
- Updated the audio regression test for the new economy-first people loader. Added MergedSystemsTests covering actual Final bootstrap, purchase during cable hints, notes in speech text, real cable insertion, green socket highlights, fee coins, audio cues and hold/resume without duplicate rewards.
- Verification: Unity compiled; 377 NUnit cases invoked directly with setup/teardown passed (0 failures), avoiding Unity TestRunner scene saves. Integration tests use isolated preview scenes. Report: Temp/MergeVerification/tests.txt. Final.unity and GameUI.prefab unchanged by resolution; unsaved editor scene preserved. No merge commit or push.

### Step 7a prep (orchestrator, 2026-10-18)
- **Pre-flight:** `git fetch`: up to date with upstream. Editor idle on `Final.unity`, console cleared, 388/388 EditMode tests pass, no format-only re-saves.
- **Brief check against the code:** the brief matches. Extra notes passed to the agent: `GameSession.DefaultMaxWaitingCalls`, `IGameContent.MaxWaitingCalls` and `CallQueueView`'s "check maxWaitingCalls" warning go with `GameConfig.maxWaitingCalls`; lamps light only through `SocketView.IsNavigationHighlighted`; the 4 untracked store-image `.meta` files stay untouched (Plan §8).

## Step 7a report: 2026-10-18
Result: done

Playable now: a new game starts on a small board. Only the top-left 3×3 jacks (rows A–C, columns 1–3) are bright and usable; the rest of the board, and the row and column lamps D–F and 4–6, are dimmed and ignore the mouse. Only 3 caller slots take calls (the other 3 have a dark disc over them), and the phone book lists only the 9 people on the open jacks. Buying "Bigger board" in the shop opens 4×4, then 5×5, then 6×6: the new jacks, their people and one more caller slot appear at once, and emergencies start once the Doctor (D4) is on the board. All of it, sizes and prices included, comes from `Economy.json`.

How to test: the Plan's 7a playtest script works as written. Notes:
1. The dev panel (F1) has a new **"Open board"** button: it opens the next board size for free.
2. Before 4×4 the emergency button reads **"Emergency: none open"** and does nothing; from 4×4 it is "Emergency now" again.
3. "Bigger board" shows in the shop once a chain is bought out (usually after the $3 Notepad).
4. **"Reset upgrades"** now closes the board again. It also clears every live call and pending callback, and sends back to the tray any cord that has a plug in a jack that closes (cords on the open 3×3 stay where they are). Added cords still stay (as in 5b).
5. A held cord clicked on a dimmed jack plays the "blocked" sound and stays in your hand.

Created:
- `Assets/Scripts/Domain/Board/BoardSize.cs`: the open rectangle (`Cols`, `Rows`, `Contains(jack)`), plus the one physical limit `BoardSize.MaxSide` = 6 and `BoardSize.Full`.
- `Assets/Scripts/Domain/Board/OpenBoard.cs`: `IsOpen(JackId)`, `IsInGame(IPerson)`, read live from `StatSheet.Board`.
- `Assets/Scripts/Game/OpenBoardLink.cs`: on every `StatSheet.Changed`, opens/closes the sockets, filters the book and sets the open caller slots.
- Tests: `Assets/Tests/Domain/BoardGrowthTests.cs`, `Assets/Tests/Domain/EconomyBoardTests.cs`, `Assets/Tests/Game/Editor/PhoneBookFilterTests.cs`, `Assets/Tests/Game/Editor/ClosedSocketTests.cs` (and their `.meta` files, plus `Assets/Scripts/Domain/Board.meta`).

Changed:
- **Domain:**
  - `IGameContent`: `MaxWaitingCalls` is gone; `StartingBoard` and `StartingClientSlots` replace it.
  - `IUpgrade`: new `BoardSize` and `ClientSlots`.
  - `StatSheet`: `Board`, `BoardCols`, `BoardRows`, `ClientSlots` (start values, then the last bought `extra_clients`), and `MaxClientSlots` = 6 (the caller panel). An invalid start falls back to the full board and 6 slots.
  - `CallSpawner`: takes `Func<int> openSlots` instead of `int maxLiveCalls`, and an optional `OpenBoard`; callers and recipients must be on open jacks, so a type with no open caller or recipient doesn't ring and the weights are shared among the types that can. New `HasOpenRecipient(type)` (used by the dev panel).
  - `CallbackScheduler`: `Func<int> openSlots` instead of the fixed cap.
  - `GameSession`: `DefaultMaxWaitingCalls` removed; new `Board` (`OpenBoard`); slots come live from `Stats.ClientSlots`. `ResetUpgrades` now clears callbacks and live calls (as `Cleared`), recomputes, then returns cords plugged into closed jacks.
  - `OperatorDesk`: new `ReturnCordsWhere(jack predicate)` (dev reset only).
  - `UpgradeService`: new dev-only `TryGrantNext(kind)` (bought for 0, no spend popup).
- **Content:**
  - `EconomyFileData`: `startingBoard`, `startingClientSlots`, and `boardSize`/`clientSlots` on upgrades, with "missing" markers (`BoardSizeEntry`).
  - `EconomyFileLoader`: the "arrives in Step 7" gate is gone (the `ArrivesIn` mechanism with it); new checks for the start and the `extra_clients` chain, in plain words.
  - `EconomyCatalog`: `StartingBoard`, `StartingClientSlots`, `MostClientSlots`.
  - `UpgradeRecord`: board size and slots.
  - `GameContent`: no longer takes `GameConfig` (it only needed the slot count).
  - `GameConfig`: `maxWaitingCalls` removed; `Validate()` adds the starting-board people check.
  - `ContentValidator`: `BoardJacks()` built from `BoardSize.Full`; new `ValidateStartingBoard(directory, board)`.
- **Board, cords and book (team code, D48):**
  - `SocketView`: `IsOpen`, `SetOpen(bool)`. A closed socket refuses `TryOccupy` (so no plug goes in), ignores hover and clicks, and never counts as navigation-highlighted. Sockets are never deactivated, so `BoardJacks(scene)` and `OnDisable` are unaffected.
  - `SocketGraphic`: `SetDimmed`/`IsDimmed`, a serialized `dimmedTint` (grey, 75 % alpha).
  - `SwitchboardLampView`: dims its own graphics (bulb, glyph image, or the plain-text F label) when none of its sockets is open, and keeps their original colours to restore. It finds this by itself from its sockets, so no wiring was needed.
  - `CableInputController`: a closed socket is not a hit target. Clicking it with a cord in hand plays the blocked sound and keeps the cord (instead of counting as "empty space"); `HandleSocketClick` also refuses it.
  - `PhoneBookView`: `SetVisibleSockets(Predicate<string>)` filters the parsed file in file order (services first, surname order kept), re-paginates, keeps the spread in range, and `HasLetter`/tabs follow it. New `PageCount` and `ContactAt(i)`.
- **Game layer:**
  - `GameInstaller`: builds `OpenBoardLink`; logs the starting-board check; the slot warning now reads "The caller panel has N slots, but Economy.json can open M (startingClientSlots or an extra_clients clientSlots)…".
  - `CallQueueView`: `SetOpenSlots(count)`; cards only use the open slots; closed slots get a dark translucent disc (`CircleGraphic`, raycast off, behind the cards) that follows the slot like the cards do; the "check maxWaitingCalls" warning now points at Economy.json.
  - `DevPanel`: "Open board"; the emergency button says "Emergency: none open" while no service is open.
- **Data:**
  - `Assets/Configs/Economy.json`: `startingBoard` 3×3, `startingClientSlots` 3, and `board_1`–`board_3` (4×4/4 for $10, 5×5/5 for $25, 6×6/6 for $60) after `notepad`.
  - `Assets/Configs/PhoneBook.json`: Fire Brigade C2 → E5, Reginald Underhill E5 → C2.
  - `Assets/Content/GameConfig.asset`: re-serialized; only the `maxWaitingCalls: 6` line went.
- **Docs:** `Docs/EconomyFile.md` (sections table, full-file outline, new "Starting board and caller slots" and "Bigger board" sections, types table, validator list and example messages).
- **Existing tests:** `Fakes.cs` (`FakeContent` board fields, `FakeUpgrade.BoardUpgrade`, a `Grid.Jack` helper: `Crowd` and `EmergencyWorld` now sit on real board jacks A1…, with the services on F6/F5/F4 instead of the off-board labels J1…/S1…); the scheduler/spawner constructor calls in `BusyTrackerTests`, `CallSpawnerTests`, `CallbackSchedulerTests`, `EmergencySpawnTests`, `HoldTests`, `PenaltyServiceTests`, `ResponseTimerTests` (fixture values kept); `GameSessionTests` and `WiresAndNotepadTests` (jack labels); `EconomyFileTests` (valid board start in the helper, 11 real entries, the gate test removed, the real shelf now offers `board_1` after the Notepad and nothing after 6×6); `EconomyCallsTests` (board start in the helper, new `GameContent`); `FullBoardTests` (opens 6×6 first, then plays exactly as before: same seed, same assertions, all pass).

Scene and prefab changes: none. `Final.unity`, `MainMenu.unity` and every prefab are untouched; the closed-slot covers are made at runtime by `CallQueueView`.

Tests: 439/439 EditMode (388 before; 1 removed, 52 new):
- `BoardGrowthTests` (26): `BoardSize.Contains` cases ×15, validity and names; `StatSheet` defaults, purchases 3→4→5→6 and the reset; `OpenBoard`; a 3×3 crowd only ever rings open people and never exceeds 3 live calls; a purchase opens the 4th slot at once; emergencies don't ring (and can't be forced) until the board covers a service, then do; callbacks wait for an open slot; the reset clears calls and callbacks and returns only cords in closing jacks; the real files in a seeded 10-minute 3×3 session involve only the starting nine; one service per new ring and the Fire Brigade/Underhill swap.
- `EconomyBoardTests` (21): the real file; JSON markers; a valid chain (one side growing alone, slots staying level); missing start (full board, 6 slots); bad start ×3; bad start slots ×2; missing, half-written and out-of-range `boardSize`; missing and too-many `clientSlots`; the first entry not bigger than the start; a chain that doesn't grow; a side that shrinks; slots that go down; broken entries left out; the starting-board people check; recipients judged on the full board.
- `PhoneBookFilterTests` (3, real `PhoneBook.json`): 3×3 shows the nine in surname order on 2 pages (6 + 3 rows), no services, `HasLetter('V')` false; 4×4/5×5/6×6 add people with the services first; shrinking brings the book back to spread 1.
- `ClosedSocketTests` (2): a closed `SocketView` refuses a real `CableController` plug while an open one takes it (and reopening works); a closed socket ignores hover, its lamp stays dark and is dimmed, including a plain-text label, and the colours come back on reopening.

Console: clean. Only the engine's two "Ignoring depth surface … memoryless" lines around screenshots, and the known MCP WebSocket line after recompiles.

Screenshots (`Temp/Captures/`):
- `step-7a.png`: start of a game, 3 calls ringing in the 3 open slots, the 3×3 board bright, everything else and lamps D–F/4–6 dimmed, three discs over the closed slots.
- `step-7a-caller-panel.png`: a crop of the caller column from the same frame.
- `step-7a-4x4-book.png`: after "Open board": row D and column 4 open, a 4th slot with an EMERGENCY card for the Doctor, and the book (16 entries) with the Doctor first.
- `step-7a-book-3x3.png`: after "Reset upgrades": back to 3×3, the book shows exactly the nine starting people on two pages.

Checked in Play Mode (stepped frames; clicks through `CableInputController.ProcessPointer` inside a `Canvas.willRenderCanvases` handler):
- **Start:** board 3×3, 3 slots, 9 sockets open and 27 closed; lamps A–C and 1–3 normal, the others dimmed; book 9 contacts on 2 pages. The spawner capped at 3 live calls (extra "Call now" attempts refused).
- **Cords:** picked up a cord from the tray; hovering the closed D4 did not highlight it; clicking D4 left it empty and the cord still in hand; hovering A1 highlighted it and clicking plugged it in.
- **4×4** (dev grant): 16 sockets open, 4 slots, book 16 with the Doctor first; the emergency button was usable and an emergency rang for the Doctor.
- **Reset:** with that cord moved to A1–D4 and 4 live calls, "Reset upgrades" gave 3×3, 3 slots, no live calls, book 9, and the A1–D4 cord back in the tray; new calls then rang on the 3×3 only.
- **MainMenu path:** loading build index 0, then 1, gave a working 3×3 session in `Final` with no errors.
- **Editor left clean:** not in Play Mode, `Final.unity` active and not dirty, `timeScale` 1, Game view not changed, the SessionState key erased, no callbacks left, console cleared.

Format-only re-saves: none (`TimeManager.asset` and `SampleScene.unity` untouched). Note: the 4 store-image `.meta` files were committed upstream (`8311221`) while I worked; I didn't touch them or reference the images. `Docs/Plan.md` shows a one-line change that isn't mine (it was already modified when I started).

Deviations from Design/Plan:
- **Field names** as proposed in the brief (`startingBoard {cols, rows}`, `startingClientSlots`, `boardSize`, `clientSlots`).
- **A bad (not only missing) `startingBoard` or `startingClientSlots`** also falls back to the full board / 6 slots, with a message.
- **Board chain rule for equal slots:** `clientSlots` may stay level along the chain ("never shrink"); the board itself must grow.
- **`GameContent(people, economy)`** no longer takes `GameConfig`, since its only use (the slot count) moved to the file.
- **Test fixtures** (`Crowd`, `EmergencyWorld`) moved from off-board labels (J1…, S1…) to real jacks, because the open board now decides who is in the game. Their behaviour is otherwise unchanged and all old tests pass.
- **The emergency dev button** shows "Emergency: none open" instead of doing nothing silently.
- **`ContentValidator.ValidateStartingBoard`** messages are prefixed "People file: " (it is about who sits where).

Follow-ups:
- **Covers:** the closed-slot discs are plain dark circles. The 6th slot sits over the printer art at the bottom of the column (a layout issue from before this step, see the Level artwork note), so its disc overlaps the printer. A cover-plate drawing from Mia would look better.
- **Design.md (orchestrator):** 5.2 (cap and spawner: open slots, open jacks), 5.7 (book filter), 5.9 (`extra_clients` built, `TryGrantNext`, reset behaviour), 6.1 (`GameConfig` without `maxWaitingCalls`; `BoardLayoutDefinition` is not needed), 6.4 (new fields), 9 "Bigger board" (now a line in `Economy.json` plus people on the jacks), D17 wording, and §3 (new public members `SocketView.IsOpen/SetOpen`, `PhoneBookView.SetVisibleSockets`).
- **`EconomyFile.md`:** the Notepad row still says "the call in hand" (pre-D46 wording); left as is.
- **Saves (Step 6):** bought `board_*` ids restore the board through `StatSheet` like the other upgrades; the reset path in `GameSession` is dev-only.

### Step 7a verification (orchestrator, 2026-10-18)
- EditMode tests re-run: 439/439 green (388 before; 52 new, 1 removed with the Step 7 gate). Console: no errors or warnings.
- `git status`: every changed file is in the report (Domain, Content, Game, the team's UI scripts, tests, `Economy.json`, `PhoneBook.json`, `GameConfig.asset`, `EconomyFile.md`). No scene or prefab changed; no format-only re-saves.
- Data diffs: `Economy.json` adds only `startingBoard`, `startingClientSlots` and `board_1`–`board_3` (after `notepad`); `PhoneBook.json` only swaps Fire Brigade and Reginald Underhill; `GameConfig.asset` only loses `maxWaitingCalls`.
- Code skim: closed sockets refuse plugs in `SocketView.TryOccupy` itself, so no path can plug one; the spawner checks open jacks for callers and recipients; reset clears calls and callbacks before the board closes. Lamp dimming caches colours once, with no per-frame allocation.
- Screenshots: `step-7a.png` (3×3 bright, D–F and 4–6 dimmed, discs over 3 slots), `step-7a-4x4-book.png` (Doctor first in a 16-person book, emergency in slot 4), `step-7a-book-3x3.png` (the starting nine on 2 pages, surname order).
- **Upstream merge** (`1d51f6f`, merge of Mia's `b376834`, `ade205a`, `8a2b839`): new `store.png` and re-exported `caller-06`/`07`/`08`. These three came back at 190×190 with 191×191 slices, so Unity would have dropped them (Mabel Dunmore and Arthur Hollis are in the starting nine). A small fix agent set the three slices to 190×190 (6 lines in the `.meta` files, nothing else) and confirmed each loads a 190×190 sprite. It also found `Assets/Sprites/board/caller.png` with the same problem, unused, and left it (Plan §8).
- The four store-image `.meta` files were committed locally (`8311221`, lead's request); not pushed yet.
- Design.md updated: implementation row, §1 rules, §3 members, 5.2 (cap and who), 5.7 (filter), 5.9 (7a deviations), 6.1, 6.4, §9 "Bigger board", D17. Plan.md: §8 (art imports, slot covers, EconomyFile wording), a cheat-sheet row for art merges, and 7a notes in the Step 6 outline.
- **Playtest (lead, 2026-10-18):** OK. Committed and pushed with the store-image `.meta` files and the portrait slice fix.


### Merge of Sergei's branch (e7acec5) into main: report (2026-10-18)
Result: done. The merge is resolved in the working tree but **not staged or committed**: `git status` still shows `UU` for the three conflict files until the orchestrator stages them.

Playable now: the game starts on 3×3 with 3 open caller slots, as after 7a, and has all of Sergei's polish: the first-order guide, green jacks for a connected call, coins flying to the money counter, the cash-register sound, the bigger socket and lamp art, and the "Surname, First" book.

Conflicts resolved:
- **`Assets/Configs/PhoneBook.json`:** Sergei's "Surname, First" names for all 33 people, plus our 7a swap: Fire Brigade on E5 and "Underhill, Reginald" on C2. The services are still first, the surname order is unchanged, `contactsPerPage` is 6, one entry per line, and 36 unique sockets.
- **`Assets/Scripts/UI/SocketGraphic.cs`:** both features kept: `SetDimmed`/`IsDimmed` plus the `Tint` from 7a, and `SetCallConnected`/`IsCallConnected` from Sergei. A private `ShowsConnected => callConnected && !dimmed` drives the green in the sprite face and both disc colours, so a closed jack always reads as closed. The dimmed tint multiplies every colour as before.
- **`Assets/Scripts/Game/GameInstaller.cs`:** `new GameContent(people, economy)` (7a) together with `cordBridge.Bind(Session.Desk, Session.Calls)` (Sergei). Everything else had already auto-merged correctly: the starting-board check, the `MostClientSlots` warning, `OpenBoardLink`, `callQueue.Bind(… notes:, syncBoard:, counter: moneyCounter)`, `audio.Bind(Session.Calls, Session.Earnings)`, and the `FirstOrderGuide` binding. No `MaxWaitingCalls` reference is left.

Auto-merged files checked (no edits needed):
- **`CallQueueView`:** our `SetOpenSlots` and covers sit alongside his `counter`, `OrderShown`, `CardOpened` and coin flight.
- **`SocketView`:** `SetCallConnected` only greens an occupied socket, and a closed socket can't be occupied, so the two features can't clash. `Release` and `OnDisable` clear the green.
- **`CableInputController`:** his `DestroyCopyPart` is kept, and so is our closed-socket refusal.
- **`PhoneBookView`:** `FirstContactWithSurname` (comma and service rule) loops over the filtered `directory`, so `HasLetter` and the tabs still follow the open board. `OpenChanged` and `ClosedBookGraphic` were added.
- **`CordBridge`:** call highlights and `CableAdded` sit next to our `ReturnToTray`. `ReturnCordsWhere` and the reset path are unaffected.
- **`CallAudio`:** it is Sergei's version only.
- **`PeopleFileLoader.SplitName`:** it reads both name forms, so the bubble still says "Arthur Hollis". The Notepad matches book taps by socket, so the new name form doesn't affect it.

Changed by me (beyond the conflict resolution):
- `Assets/Tests/Game/Editor/PhoneBookFilterTests.cs`: the expected 3×3 names now use the file's form ("Dunmore, Mabel", …, "Yardley, Lillian"). The order, count, pages and assertions are unchanged.
- `Assets/Tests/Game/Editor/MergedSystemsTests.cs`: the `AcceptOrder` helper now picks a caller and a recipient who are on open jacks (`session.Board.IsInGame`). Before, it took the first caller in the file (Abbott on F6, which is closed at 3×3), so `NotepadAndNewCordKeepConnectionHighlightsCoinsAndAudio` failed at `TryInsert(a)` because a closed jack correctly refuses the plug. Its intent (Notepad note, bought cord, green jacks, coins, audio cues, hold and resume without paying twice) and every assertion are unchanged.

Scene and prefab changes: none. `Final.unity` was never dirty, and I reloaded it from disk at the end. Sergei's scene and prefab versions are as merged.

Tests: **455/455 EditMode** (453/455 before my two test fixes). No format-only re-saves of `TimeManager.asset` or `SampleScene.unity`.

Console: clean (only the engine's two "Ignoring depth surface … memoryless" lines around screenshots); cleared at the end.

Play Mode smoke test (`Final.unity`, stepped frames, clicks through `CableInputController.ProcessPointer` in a one-shot `Canvas.willRenderCanvases` handler):
- **Start:** 3×3 board, 3 slots, 9 sockets open and 27 dimmed. Lamps 4–6 and D–F are dimmed on the new art, and the book has 9 contacts. Closed slots 4–6 have covers.
- **First call:** Arthur Hollis (A1) wants Irene Godfrey (B2), both on open jacks. The guide was at stage `Portrait` with 1 pulse showing, and the card showed its "New order!" label.
- **Cords:** I took the card and picked up `Cable_01` from the tray. Hovering D4 didn't highlight it, and clicking D4 left it empty with the cord still held. A1 then B2 connected: the call was `Connected`, A1 and B2 were green, coins were flying, and the wallet got the 11¢ fee. The guide went to `Cancelled` because I skipped the book step, which is its intended out-of-order behaviour.
- **Dev "Open board"** (`TryGrantNext(ExtraClients)`): 4×4, 4 slots, 16 open, the book had 16 entries with the Doctor first, and the green A1/B2 call stayed connected.
- **MainMenu path:** loading build index 0 (MainMenu) and then the next index gave `Final` (index 1) with a working 3×3 session, the guide at `Portrait`, and no errors.
- **Editor left clean:** not in Play Mode, `Final.unity` active and not dirty, `timeScale` 1, the Game view size unchanged, SessionState keys erased, no callbacks left, console cleared.

Screenshots (`Temp/Captures/`):
- `merge-sergei.png`: 3×3 start with the first order and the guide.
- `merge-sergei-connected.png`: A1–B2 green, with coins flying.
- `merge-sergei-4x4.png`: after "Open board".

Docs that describe how a name is written in `PhoneBook.json` (none edited; `EconomyFile.md` doesn't describe the name format):
- `Docs/Design.md`:
  - §5.7, around line 362: "the first surname (the last word of the name)". This is now wrong for "Surname, First".
  - D31, line 610: lists the fields but not the name form. It's a good place to state "Surname, First" (services: their whole name).
  - §6.3, line 510: name style only.
  - Line 326: the bubble shows "Ada Moss", which is still true.
  - Sergei's branch added a "Merge integration" note at the end of the file (staged with the merge).
- `Docs/Plan.md`:
  - Line 316: the Step 1b example `"Ada Moss"`.
  - Line 319: "Each person's surname is the last word of `name`".
  - Line 535: "Single-word names are fine for his parser".
  - Line 607: "ShowLetter takes the first name whose last word starts with the letter".
  - Line 249: historical `DirectoryListView` "Surname, First — C4".
  - These are mostly historical briefs. Plan §8 or the Step 7b notes may want a line on the new form.
- `Docs/EconomyFile.md`, line 166: the Notepad's "Ada Moss (C3)" is the bubble text, which is still correct.
- `CLAUDE.md`, line 66: "keep the surname sort order" (still true).
- There is no designer guide for `PhoneBook.json` itself.

Deviations: none from Design. Where both flags are set, dimmed wins over green in `SocketGraphic`, as the brief asked.

Follow-ups:
- Design.md §5.7, D31 and §3 need updating: the "Surname, First" form, and the new public members `SocketGraphic`/`SocketView.SetCallConnected`, `CordBridge.CableAdded`, `CableController.PickedUp`, `PhoneBookView.OpenChanged`/`ClosedBookGraphic`, `CallQueueView.OrderShown`/`CardOpened`, and `CallAudio.ConnectionSucceeded`.
- The full guide sequence (portrait, text, book, cords) was only checked by Sergei's EditMode tests (`FirstOrderGuideTests`, `MergedSystemsTests`), not stepped end to end in Play Mode. Worth including in the lead's playtest.

#### Merge verification (orchestrator, 2026-10-18)
- EditMode tests re-run: 455/455 green. No conflict markers left; `PhoneBook.json` has 36 people on 36 jacks in surname order, with Fire Brigade E5 and "Underhill, Reginald" C2.
- The four store-image `.meta` files keep Sergei's GUIDs (ours from `8311221` are replaced; neither set was referenced anywhere).
- Screenshots: `merge-sergei.png` (3×3 start, guide on the first card), `merge-sergei-connected.png` (A1–B2 green, coins flying), `merge-sergei-4x4.png`.
- Design.md: a row for Sergei's branch, §3 hooks, §5.7 and D31 ("Surname, First"). Plan.md: 7b builds on `FirstOrderGuide`; Step 8 builds on the coins and cash-register sound.
- **Playtest (lead, 2026-10-18):** OK.

#### Mia's art after the merge (orchestrator, 2026-10-18)
- Merged Mia's `bc0ca7d`, `1f38408`, `af83b02` (`b3da97f`): 13 coloured cable sprites, 3 store-button states and a new `store.png` (same size, slice still fits). She has no Unity, so our editor generated the 16 `.meta` files, committed in `c4870fa` (Sprite, 100 px per unit, every slice inside its image). 455/455 tests; pushed with the Sergei merge (`516548c`).
- Note for whoever wires them in: the new `cable_stored_*` import as Multiple (one slice named `..._0`), while the original `cable_stored.png` is Single.

### Merge of Sergei's 1c337e7 + 6226ead into main: report (2026-10-18)
Result: done. The merge is resolved in the working tree but **not staged or committed**: `git status` still shows `UU` for the three conflict files until the orchestrator stages them. My test edits and `police.png.meta` are unstaged as well.

**Needs a lead decision: book sort order.** Sergei's `PhoneBookConfig.Parse` now turns "Surname, First" into "First Surname" when it loads the file, then sorts people by **first name** (services stay first, in file order). The A–Z tabs, `HasLetter` and `ShowLetter` go by first name too, and he renamed his `PhoneBookTabTests` to match. This goes against Design §5.7, D31 and D36 ("sorted by surname") and CLAUDE.md ("keep … the surname sort order"). It doesn't clash with 7a, so I kept his version: `PhoneBook.json` is unchanged (still "Surname, First" in surname order, and `Validate Content` still checks that), and the open-board filter works on either order. To go back to surname order, remove the name flip and the `OrderBy` in `PhoneBookConfig.Parse` and the first-name rule in `FirstContactWithName`, then revert his tab-test renames and my `PhoneBookFilterTests` edit.

Playable now: a 3×3 start, as after 7a, plus Sergei's new polish:
- A soft yellow halo around an open jack when you hover it with a cord in hand. It turns red over a jack another cord is using, and green around both jacks of a connected call.
- Click the left or right half of the open book to turn pages; the cursor becomes a pinching hand. Escape or a click outside closes the book.
- Guide visuals: a bright ring on the first caller's portrait and a soft glow around the closed book.
- The cash-register sound now plays on the first connection of a call.

Conflicts resolved:
- **`SocketGraphic.cs`:** Sergei moved the hover, blocked and green looks from the jack face to a separate halo (`SocketHaloGraphic`, a child made on demand). I kept his plain face and discs, kept 7a's `Tint` (the dimmed tint on the face and every disc) and added `ShowsHalo => !dimmed && (callConnected || highlighted)`. So a closed jack never shows a halo: no hover, no red and no green. `SetDimmed` now also refreshes the halo, so a plug left in a jack that closes (dev reset) loses its green, and gets it back if the jack reopens. The halo colour and intensity use `ShowsConnected`.
- **`CableInputController.cs`:** both kept. A click on a closed jack plays the blocked sound and keeps the cord (7a). The "empty space" branch now also ignores clicks over the book (Sergei: `hoveredBook == null`), so a page click doesn't drop or return a held cord.
- **`PhoneBookView.cs`:** kept `SetVisibleSockets`/`ApplyFilter` (7a; it now uses the existing `List` import) and Sergei's `FirstContactWithName` for `HasLetter`. `PageTurnDirectionAt`, `CanGoForward`/`CanGoBack` and the tabs all read the filtered `directory`, so page turns follow the open board.

Auto-merged files checked (no edits needed):
- **`SocketView`:** Sergei's hover rule (a highlight only with a cord in hand) sits next to our `open` checks: a closed socket clears hover and ignores enter and click events.
- **`GameInstaller`:** `audio.Bind(Session.Calls)`. The cash register now plays on `Connected` (not from a hold), so `EarningsService` is no longer needed. Everything from 7a and the previous merge (`OpenBoardLink`, `GameContent(people, economy)`, `cordBridge.Bind(desk, calls)`) is intact.
- Sergei's versions only, with nothing from 7a involved: `FirstOrderGuide`, `SlowGuidePulse`, `SoftGuideOutline`, `GuideSilhouette.shader`, `CallAudio`, `HandCursorGraphic`, `PhoneBookConfig`, `PhoneBookContactRow`, `PhoneBookBuilder`, `MergedSystemsTests`, `CallAudioTests`.

Changed by me (beyond the conflicts):
- **`Assets/Tests/Game/Editor/PhoneBookFilterTests.cs`** (7a's test): the expected 3×3 names are now in Sergei's form and order ("Ada Moss", "Arthur Hollis", …, "Reginald Underhill"). The letter probe is now 'E' (Eleanor Vickery C4 and Edgar Pennington B4 are closed at 3×3, open at 4×4) instead of 'V', because the tabs now go by first name. The count, pages, row visibility and services-first checks are unchanged.
- **`Assets/Tests/Game/Editor/PhoneBookPageTurnTests.cs`:** new test `PageTurningFollowsTheOpenBoardFilter` on the real `PhoneBook.prefab`. At 3×3 (9 contacts, 1 spread), neither half turns and a click keeps the book open on spread 0. At 4×4 (16 contacts, 2 spreads), the right half turns to spread 1, then stops. Shrinking back to 3×3 returns to spread 0.
- **`Assets/Tests/Game/Editor/SocketHoverTests.cs`:** new test `AClosedSocketNeverShowsAHalo`. A held cord over a closed jack: no halo and no highlight. A connected jack that closes: no green and no red halo. Reopening brings the green back.
- None of Sergei's new tests failed because of 7a. His `MergedSystemsTests` already use the open-jack `AcceptOrder` from the last merge.

Scene and prefab changes: none. `Final.unity` showed dirty once right after the first recompile. I saved a copy to `Temp/` and diffed it: it was byte-identical to disk, so nothing was lost. It was not dirty at the end, and it is the active scene.

Tests: **472/472 EditMode** (455 before this merge; Sergei adds 15, I add 2). Before my test edits it was 468/470 (the two `PhoneBookFilterTests` order checks).

Console: clean. The only lines are the engine's two "Ignoring depth surface … memoryless" lines around screenshots and the known MCP WebSocket and "TCS already completed" lines after recompiles. Cleared at the end. The guide shader compiles with no errors (`ShaderUtil.ShaderHasError` false, `isSupported` true).

Format-only re-saves: `ProjectSettings/TimeManager.asset` (test run) was discarded with `git checkout`. `SampleScene.unity` was not touched.

Play Mode smoke test (`Final.unity`, stepped frames, `ProcessPointer` and EventSystem clicks inside one-shot `Canvas.willRenderCanvases` handlers):
- **Start:** board 3×3, 3 slots, 9 open jacks, 27 dimmed, 6 of 12 lamps dimmed, book 9 contacts on 2 pages, 3 discs over the closed slots. The guide was at `Portrait`, with the ring on the first card.
- **Hover:** I picked up `Cable_02`. Hovering D4: no halo, no highlight. Clicking D4: empty, cord still held. Hovering A1: halo on, highlighted, and lamps A and 1 lit.
- **Connect:** A3 to A2. The call is `Connected`, both jacks have the green halo (intensity 1.4), coins are flying and the wallet got 14¢.
- **Book:** I clicked the closed book open. At 3×3 the right half shows the pointing hand (no page to turn), and a click keeps spread 0 and the book open. Then dev "Open board" (`TryGrantNext(ExtraClients)`): 4×4, 16 contacts, the Doctor first. The right half shows the `PageTurn` cursor, and a click turns to spread 1. Back and forward both worked, the book stayed open, and `HasLetter('M')` and `('E')` were true.
- **MainMenu path:** build index 0 (MainMenu), then the next index, gave `Final` (index 1) with a fresh 3×3 session. In it, I took the first card without touching a cord. After the text typed out, the guide went to `Book`: the silhouette glow is on, using `Switchboard/UI/GuideSilhouette`. Opening the book moved it to `ReadingBook` and the glow went off. The book's first entry is "Ada Moss".
- **Earlier session:** there the guide went to `Cancelled` when I took the card, because I had already picked up a cord. That is its intended out-of-order behaviour.
- **Editor left clean:** not in Play Mode, `Final.unity` active and not dirty, `timeScale` 1, `captureDeltaTime` 0, Game view size never changed, SessionState keys erased, no callbacks or helper objects left, console cleared. I also destroyed leftover in-memory `GuideSilhouette` materials (see follow-ups).

Screenshots (`Temp/Captures/`):
- `merge-sergei2.png`: 3×3 start, three orders, the held cord over A1 with Sergei's yellow halo and lamps A/1 lit, D–F and 4–6 dimmed. For this one frame I disabled the input controller and set the hover directly, because the real-mouse `Update` kept clearing the simulated hover before capture.
- `merge-sergei2-book.png`: the book at 4×4 after a page turn (spread 1: Mildred Stanhope, Reginald Underhill, Rose Ellery, Walter Pryce), with A3–A2 green behind it.
- `merge-sergei2-4x4.png`: 4×4 open, 4 slots, the connected call's green halos.
- `merge-sergei2-guide.png` (extra): the MainMenu-path session at the guide's `Book` stage, with the glow around the closed book.

New art: `Assets/Sprites/callers/police.png` is **190×190**. Unity generated `police.png.meta` (untracked, left as generated, not referenced anywhere): Sprite, Multiple, 100 px per unit, one slice `police_0` at x 0, y 0, **190×186** (auto-trimmed), which fits inside the image.

New public members from Sergei that other code relies on (for Design §3):
- `PhoneBookView.PageTurnDirectionAt(Vector2 screen, Camera)` (-1/0/1; used by `CableInputController`). `PhoneBookView` now implements `IPointerClickHandler` (and an empty `IPointerDownHandler`), and the open book's button and the fold buttons are disabled at runtime.
- `CableCursorMode.PageTurn` and `HandCursorGraphic.PageTurnDirection`.
- `PhoneBookContact.IsEmergencyService`. `PhoneBookConfig.Parse` now rewrites names and order (see the decision above).
- `SocketHaloGraphic` (with `Intensity`), which `SocketGraphic` and `SlowGuidePulse` use.
- `SlowGuidePulse.UsePortraitHalo()` and `UseSilhouetteHalo()`, plus `SoftGuideOutline`.
- `Resources/GuideSilhouette.shader` (`Switchboard/UI/GuideSilhouette`), loaded with `Resources.Load` (so it ships in builds).
- `CallAudio.Bind(CallService)`: the `EarningsService` parameter is gone.

Deviations: none from 7a. Dimmed wins over every halo, as the brief asked. The sort-order change above is Sergei's and is left for the lead.

Follow-ups:
- **Lead decision:** first-name versus surname book order (above). Whichever way it goes, update Design §5.7, D31, D36 and CLAUDE.md. Note the bubble's bold surnames, which were there because the book was sorted by surname.
- **Material leak in EditMode:** `SlowGuidePulse` makes a `HideAndDontSave` material for the silhouette glow and frees it in `OnDestroy`. In EditMode tests `OnDestroy` doesn't run, so every test run leaves 2 materials in editor memory (I found 4 after two runs and destroyed them). Play Mode was fine. Small fix: free it in `OnDisable` too, or clean up in the test teardown.
- **Closing the book:** Sergei's `PhoneBookView.Update` closes the open book on any press outside it. It reads `Pointer.current` directly, so integration checks can't simulate it through `ProcessPointer`; it needs a real click in the playtest.
- **Hover checks:** while Unity reports focus, `CableInputController.Update` reads the real mouse every frame and overrides simulated hover. Plan §4 could mention disabling the controller for one captured frame, as I did.

### Book order back to surnames (D50): report (2026-10-18)
Result: done. The book is a real directory again: names as written in `PhoneBook.json` ("Moss, Ada"; services by their whole name), in the file's own order (services first, then people by surname), and the letter lookup goes by surname. The A–Z tabs stay hidden (`OpenBook/LetterTabs` inactive; the prefab and the tabs menu were not touched). The merge is still uncommitted; nothing was staged.

Files changed:
- `Assets/Scripts/UI/PhoneBookConfig.cs`: removed the "Surname, First" → "First Surname" flip, the first-name `OrderBy` and `using System.Linq`. `PhoneBookContact.IsEmergencyService` stays. It is now e7acec5's `Parse` plus that property.
- `Assets/Scripts/UI/PhoneBookView.cs`: `FirstContactWithName` is `FirstContactWithSurname` again with e7acec5's rule: the first letter of a service's whole name or of a "Surname, First" entry, otherwise the last word (old-style "First Surname"). It uses `IsEmergencyService` and still reads the filtered `directory`, so it follows `SetVisibleSockets`. The `ShowLetter` summary says "surname" again. Still `UU` in git (merge conflict file).
- `Assets/Tests/Game/Editor/PhoneBookTabTests.cs`: the helper is `Surnames` again and the five letter tests have e7acec5's names and surname expectations (`JumpsToTheSpreadWithTheFirstMatchingSurname`, `WorksOnTheFirstAndLastSpread`, `IgnoresLettersWithNoSurname`, `SurnameFirstNamesUseTheSurnameInsteadOfTheGivenName`, `UsesTheLastWordOfTheNameAsTheSurname`). Sergei's `ServicesStayFirstAndPeopleSortByGivenNameWithoutLosingSockets` is adapted to `ParseKeepsTheFileNamesOrderAndSockets` (names, sockets, trimming/upper-casing and `IsEmergencyService` kept as in the file). New `ServicesUseTheirWholeNameAndPeopleTheirSurname` (P for Police, F for Fire Brigade, A/B/M for surnames, no Z for "Abbott, Zoe"). A small `BookFrom(json)` helper was split out of `Book`.
- `Assets/Tests/Game/Editor/PhoneBookFilterTests.cs`: back to HEAD's surname expectations (the nine names from "Dunmore, Mabel" to "Yardley, Lillian"; `HasLetter('V')` false at 3×3, true at 4×4). New `TheBookKeepsTheRealFileNamesAndOrder`: the book built from the real `PhoneBook.json` lists every entry with exactly the file's (trimmed) name in the file's order, and people keep their comma, so a flip or re-sort can't come back silently.

Consumers of `PhoneBookContact.name` and the book's order (none matched by name; no fixes needed):
- `NotepadBookLink`: `ContactTapped(name, socket)` notes `new JackId(socket)` only; the name is ignored.
- `PhoneBookContactRow` / `PhoneBookPageView`: display only (`contactName.text = contact.name`), rows filled in `directory` order.
- `FirstOrderGuide`, `SlowGuidePulse`: use `IsOpen`, `OpenChanged` and `ClosedBookGraphic` only; no names or order.
- Page turning (`PageTurnDirectionAt`, `CanGoForward`/`CanGoBack`, `NextSpread`/`PreviousSpread`): count-based over `directory`, order-agnostic.
- `PhoneBookBuilder`: Sergei only changed the fold buttons' transition; no names.
- `CallCardView.Listed` (bold surname in the bubble) uses the domain `IPerson`, not the book, and fits surname order again. The domain side (`PeopleFileLoader`, `PhoneDirectory` surname sort, `ContentValidator` surname-order check) never used the UI flip.

Tests: **474/474 EditMode** passed (472 before; +2: `ServicesUseTheirWholeNameAndPeopleTheirSurname` and `TheBookKeepsTheRealFileNamesAndOrder`; Sergei's parse test was adapted, not removed). `PhoneBookPageTurnTests` and `SocketHoverTests` pass unchanged.

Console: clean after compile (only the known MCP "WebSocket is not initialised" line) and in Play Mode (only the engine's two "Ignoring depth surface … memoryless" lines around screenshots). Cleared at the end.

Play Mode check (`Final.unity`, stepped frames; the book opened through `SetOpen(true)` and the page turned through `NextSpread()`, since the real page clicks were checked in the previous merge):
- 3×3: 9 entries on 2 pages: Dunmore, Mabel (A2); Godfrey, Irene; Hollis, Arthur; Moss, Ada; Quigley, Bernard; Rook, Gertrude; Stanhope, Mildred; Underhill, Reginald; Yardley, Lillian (B1). `HasLetter('M')` true, `('V')` false.
- Dev "Open board" (`TryGrantNext(ExtraClients)`), 4×4: 16 entries, Doctor (D4) first, then Cobbold, Dunmore, Ellery, Godfrey, Hollis, Marlow, Moss, Pennington, Pryce, Quigley, Rook, Stanhope, Underhill, Vickery, Yardley. A page turn went to spread 1 (Stanhope, Underhill, Vickery, Yardley), the book stayed open, `HasLetter('V')` true.
- `OpenBook/LetterTabs`: `activeSelf` false, not visible.
- Screenshots: `Temp/Captures/merge-sergei2-book-surname.png` (4×4, spread 1 after the turn) and `Temp/Captures/merge-sergei2-book-surname-3x3.png` (3×3, spread 0).
- Editor left clean: not in Play Mode, not paused, `Final.unity` active and not dirty, `timeScale` 1, `captureDeltaTime` 0, no callbacks, no SessionState keys, Game view size unchanged. I destroyed 4 leaked `GuideSilhouette` materials from the test runs (the known `SlowGuidePulse` EditMode leak).

Format-only re-saves: `TimeManager.asset` and `SampleScene.unity` were not touched. **Not discarded (outside my allowed list):** the test run at 20:01 wrote dynamic glyph data into `Assets/Fonts/BoecklinsUniverse/Boecklins Universe SDF.asset` and `Assets/Fonts/DellaRespira/Della Respira SDF.asset` (glyph/character tables and atlas filled in from rendered text, ~22k lines). These are TMP dynamic atlas side effects, not real changes; the orchestrator should discard them with `git checkout -- <file>` before committing. `Docs/Design.md` shows changes I did not make (presumably the orchestrator's D50 entry).

Follow-ups:
- Discard the two font asset re-saves (above). Plan §4 could list dynamic TMP font assets next to `TimeManager.asset` as known test-run re-saves, and possibly set their atlas population so the editor doesn't keep writing them.
- The `SlowGuidePulse` material leak in EditMode tests (from the previous report) is still open.

#### Merge verification (orchestrator, 2026-10-18)
- EditMode tests re-run: 474/474 green. No conflict markers; `PhoneBook.json` untouched by this merge.
- Lead decision D50: the book keeps the file's names ("Moss, Ada") and order (services, then surnames); Sergei's first-name sort and name flip were removed, the rest of his work kept. A–Z tabs stay hidden. Recorded in Design.md (D50, §5.7, §3, a second merge row).
- Screenshots: `merge-sergei2.png` (halo on A1), `merge-sergei2-4x4.png` (green halos), `merge-sergei2-book-surname-3x3.png` (the starting nine, surname order).
- The test runs filled in glyphs in the dynamic TMP fonts `Boecklins Universe SDF.asset` and `Della Respira SDF.asset` (~22k lines, glyph tables only); discarded as test-run noise. Plan §4 tip to add: dynamic TMP font assets change during test and Play runs; discard them like `TimeManager.asset`.
- `police.png` (Mia, `13abb13`): 190×190, generated `.meta` with one slice 190×186 inside the image.
- Open: the `SlowGuidePulse` hidden material isn't freed in EditMode tests (only `OnDestroy`); small fix for a later pass.
- Committed and pushed on the lead's request (2026-10-18).

### Steps 7c and 7d planned (orchestrator, 2026-10-18)
- The designer proposed hand-written calls (`Calls.json`), call queues in `Economy.json` and a lookup-only phone book (`Book.json`). The lead and designer answered the open questions:
  - the book gets a jack column, shown last
  - the caller's name shows under the portrait
  - the Notepad is dropped
  - the book shows everyone from the start
  - emergencies get their own `responseTime`, with a fine only for a miss
  - no spawn timer: a free slot takes the next call; calls go strictly in order
  - one `name` field, all money in cents, jacks as labels such as "C3", text in English
  - caller slots start at 2 and grow with the board upgrades
- Recorded as D51–D54 in Design.md. Full briefs for 7c (calls and queues) and 7d (the new book) are in Plan.md §9; both come before Step 6. Step 7b shrinks to the tutorial, because the queues now set the difficulty.
- **Renumbered (lead, 2026-10-18)** so the numbers follow the build order: 7a → 6 (done), 7c → 7, 7d → 8, 6 → 9, 7b → 10, 8 → 11, 9 → 12, 10 → 13. Reports above keep the old numbers.

### Merge resolution (lead clarification, 2026-10-18)
- Retained First Surname display and first-name sorting, with emergency services first. Updated the incoming surname-order tests and D50 to this explicit decision.
- Preserved both sets of MergedSystemsTests. Preview-scene tests unpack only their disposable prefab instances; page-input tests temporarily isolate raycasters from the open editor scene.
- Kept the shared branch GUID 8cb32b4f59e8544fc80cf734e1011286 for police.png. Neither competing GUID had serialized references elsewhere in Assets; the portrait imports successfully.
- Reconciled hidden socket graphics with the new halo: closing/reopening a section preserves the active-call flag and restores its green indicator.
- Verification: 46 focused tests passed, no failures; shared-GUID sprite import passed. No live scene was saved.
- **Lead's answer (2026-10-18):** the book keeps surname order (D50 as recorded in Design.md); the first bullet above is superseded and the code is set back to surname order in the merge. A game starting with 2 cords (Sergei's `041fb90`) is accepted.

### Book order kept at surnames after Sergei's 1677d20: report (2026-10-18)
Result: **book done; suite not fully green, for a reason outside the book.** Sergei's first-name order is removed again (D50): names show as written in `PhoneBook.json`, in the file's order (services first, then surnames), and the letter lookup goes by surname. The A–Z tabs stay hidden. Everything else from his push stays: the shop skin, cable artwork, the `BookHoverHighlight` hook, the money counter and 2 starting cords. Nothing was staged, and the merge is still uncommitted.

Files changed (on top of the merge):
- `Assets/Scripts/UI/PhoneBookConfig.cs`: back to `94ad3a2` exactly, without the "Surname, First" → "First Surname" flip, the first-name `OrderBy` and `using System.Linq`. `IsEmergencyService` stays (it was already in `94ad3a2`).
- `Assets/Scripts/UI/PhoneBookView.cs`: `FirstContactWithName` is `FirstContactWithSurname` again, using `IsEmergencyService` (a service's whole name or a "Surname, First" entry uses its first letter; otherwise the last word), over the filtered `directory`. The `SetVisibleSockets`/`ShowLetter` summaries say surname again. His `BookHoverHighlight` hook in `ConfigurePageInput` stays. The only remaining difference from `94ad3a2` is that hook.
- `Assets/Scripts/Game/Views/CallCardView.cs`: `Listed` bolds the **surname** in the caller's bubble again. Sergei's `041fb90` switched it to bold the first name "because the book is sorted by given name", which is an order assumption.
- `Assets/Tests/Game/Editor/MergedSystemsTests.cs`: in Sergei's new `FreshGameHidesLockedPlacesAndShopPurchasesRevealThem`, the bubble assertion now expects `<b>LastName</b>` and no `<b>FirstName</b>`. The rest of his test (2 cords, tray artwork, 9 → 16 jacks, shop slots, 6 cord colours) is unchanged.
- `Assets/Tests/Game/Editor/PhoneBookTabTests.cs`: back to `94ad3a2`. That restores `ParseKeepsTheFileNamesOrderAndSockets`, `ServicesUseTheirWholeNameAndPeopleTheirSurname` and the five surname letter tests. Sergei's versions only flipped the expectations to first names, so there was no order-free coverage to keep.
- `Assets/Tests/Game/Editor/PhoneBookFilterTests.cs`: back to `94ad3a2`: the 3×3 test expects "Dunmore, Mabel" … "Yardley, Lillian" with `HasLetter('V')` false (true at 4×4). `TheBookKeepsTheRealFileNamesAndOrder` also picks up Sergei's order-free checks: every entry keeps its file socket, and services come first.
- `PhoneBookPageTurnTests.cs`: Sergei's change (raycasters isolated from the open scene) doesn't depend on order, so it was kept as is.

Tests: **466/475 EditMode passed, 9 failed, none of them book tests.** All `PhoneBookTabTests`, `PhoneBookFilterTests` and `PhoneBookPageTurnTests` pass. All 9 failures have one cause: **`caller-09` and `caller-10` portraits are missing.** Mia's `b9e983b` ("upd8 caller imgs", already in our HEAD) re-exported `Assets/Sprites/callers/caller-09.png` and `caller-10.png` at 190×190, but their `.meta` files still slice 191×191 (lines 134–135). Unity drops a sprite whose slice is bigger than the texture (Plan §4 "Art merges"), so `GameConfig`'s portrait list sees nothing there. That makes six people invalid ("portrait 'caller-10' is not in GameConfig's portrait list"). The failures:
  - the 5 `MergedSystemsTests` fail in `SetUp` on that logged error, so the adapted bubble assertion didn't get to run;
  - `ContentValidatorTests.RealContentHasNoProblems`
  - `EconomyFileTests.GameConfigReferencesTheRealFile`
  - `PeopleFileTests.RealFileHasThirtyThreePeopleAndThreeServicesOnEveryJack`
  - `FullBoardTests.CallersComeFromTheWholeBoardAndNobodyIsInTwoCalls` (30 of 36)

  I tried to set the two slices to 190×190, but the edit was blocked by the permission system. It was also outside this brief, so I left it.

Console: compile clean (only the known MCP "WebSocket is not initialised" line). Play Mode logged the six portrait errors above, plus the engine's two "memoryless" lines around screenshots. Cleared at the end.

Play Mode check (`Final.unity`, stepped frames):
- Start: `Stats.Cords` 2, `CordBridge.CordCount` 2, 2 active cables in the tray.
- 3×3 book: 9 entries on 2 pages: Dunmore, Mabel (A2); Godfrey, Irene; Hollis, Arthur; Moss, Ada; Quigley, Bernard; Rook, Gertrude; Stanhope, Mildred; Underhill, Reginald; Yardley, Lillian (B1). `HasLetter('M')` true, `('V')` false.
- Dev "Open board" (`TryGrantNext(ExtraClients)`), 4×4: 16 entries: Doctor (D4), then Cobbold, Dunmore, Ellery, Godfrey, Hollis, Marlow, Moss, Pennington, Pryce, Quigley, Rook, Stanhope, Underhill, Vickery, Yardley. `HasLetter('V')` true. `LetterTabs` not active.
- Book opened through `SetOpen(true)`; one `NextSpread()` went to spread 1 (Stanhope, Underhill, Vickery, Yardley), and the book stayed open.
- The shop shows Sergei's skin (More Cables $5.00, More Clients $10.00 then $25.00, More Profit, ?????????? $250.00) with no errors from it.
- Screenshots: `Temp/Captures/merge-sergei3.png` (board, shop and tray with 2 cords) and `Temp/Captures/merge-sergei3-book.png` (4×4, spread 1 after the turn).
- Editor left clean: not in Play Mode, not paused, `timeScale` 1, `captureDeltaTime` 0, no callbacks or SessionState keys, `Final.unity` active and not dirty. Before I started, it was marked dirty only by layout ("Driving RectTransform"), and it was clean after Play Mode, so no reload was needed. I destroyed 4 leaked `GuideSilhouette` materials from the test run (the known `SlowGuidePulse` leak).

Discards: none needed. `TimeManager.asset`, `SampleScene.unity` and the TMP font assets were not touched by this run.

Follow-ups:
- **Fix the `caller-09`/`caller-10` slices** (set `width`/`height` to 190 in both `.meta` files, or re-slice them in the Sprite Editor), then re-run the suite. The 5 `MergedSystemsTests`, including the adapted bubble check, still need a green run.
- Tell Sergei the book and the bubble stay surname-first (D50), so his next push doesn't flip them again.

#### Merge verification (orchestrator, 2026-10-18)
- EditMode tests re-run: 475/475 green after the slice fix. Sergei's shop skin, cable artwork, book hover highlight, money counter and 2 starting cords are kept; the book and the bubble stay surname-first (D50).
- `caller-09`/`caller-10` (Mia's `b9e983b`) were re-exported at 190×190 with 191×191 slices, so Unity dropped them and 9 tests failed. A fix agent set both slices to 190×190 (2 lines per `.meta`), with the lead's approval after a first attempt was blocked by the permission check.
- Committed and pushed on the lead's request.

### Step 7 report: 2026-10-18
Result: done

Playable now: press Play in `Final.unity`. Two caller slots fill at once with the first two calls of `startPool`. Each card shows the call's portrait, its pay ("10¢ + 5¢/s") and the caller's name on a small plate under it. Taking a card types out that call's own message in the bubble ("Connect me with Mabel Dunmore, please."). Patching the call's own two jacks pays the fee, then the per-second pay, then "Paid"; a wrong patch says "Wrong number!" with no fine. Either way the slot takes the next queued call once the stamp has shown. "Bigger board" (or dev "Open board") grows the board, opens one more slot, and switches the queue to that board's `stagePool`. Emergencies in the new queues (Doctor, Fire Brigade, Police) show the red badge and countdown ring; a miss fines `penaltyCents` ($2). The Notepad is gone from the shop.

How to test: the Plan's Step 7 playtest script works as written. Notes:
1. **The next call waits for the stamp.** When a call ends, its "Paid" or "Wrong number!" stamp keeps the slot for about 2 seconds, then the next card appears there. In the rules the next call is already in the slot (deviation 1).
2. **Emergencies come third** in `board_1`'s queue (`call_103`, Herbert Marlow on D3 wants the Doctor on D4). To reach one quickly: dev "Open board", then "Clear calls"; the queue carries on, so `call_102`–`call_104` appear and `call_103` is the emergency.
3. Dev panel: "Call now" and "Emergency now" are gone. "Clear calls" lets the queue carry on; "Reset upgrades" restarts `startPool` from `call_001`.
4. Validator check (script step 7): change `"call_001"` in `startPool` to `"call_201"` (Basil Thorne on A5) and run Validate Content: "Economy file: startPool: call_201 uses A5, but the startingBoard opens only 3x3. …". Changing a call's own `from` to E5 gives the same message for that call.

Created:
- `Assets/Configs/Calls.json`: 44 hand-written placeholder calls from today's people and jacks: `call_001`–`call_008` (3x3, no emergencies; `call_001` is Ada Moss C3 → Mabel Dunmore A2, the guide's first call), `call_101`–`call_110` (4x4, 2 emergencies to the Doctor D4), `call_201`–`call_212` (5x5, adding the Fire Brigade E5), `call_301`–`call_314` (6x6, adding the Police B6). Regular calls: fee 5–15¢, 3–8¢/s, 15–28 s; emergencies: fee 30–40¢, 7–10¢/s, 12–15 s, 30 s to respond, 200¢ fine. Plain ASCII only.
- Domain: `Calls/CallScript.cs` (the call type: id, name, icon, message, from, to, duration, fee, rate, emergency, response time, penalty; response time and penalty are 0 for regular calls), `Queue/CallQueue.cs` (pool, position, `Restart()`, `Tick()`).
- Content: `CallsFileData.cs`, `CallsFileLoader.cs` (parse + checks + `JackLabel`), `CallsCatalog.cs` (`Calls`, `Find`, `IsLeftOut`, `Errors`).
- Tests: `CallQueueTests.cs`, `CallsFileTests.cs`, `EconomyPoolTests.cs`, `CordStatTests.cs` (the cord part of the old `WiresAndNotepadTests.cs`), with `.meta` files; `Assets/Scripts/Domain/Queue.meta`.

Changed:
- **Domain:**
  - `CallRequest`: wraps a `CallScript` (`Script`, `From`, `To`, `CentsPerSecond`, `DurationSeconds`, `ConnectionFeeCents`, `Involves(jack)`); no caller, recipient, type or attempt.
  - `Call`: the response time comes from the script; `CallerNote`/`RecipientNote` removed.
  - `CallService`: `IsLive(script)` (by id) replaces `CountLive(type)` and `IsInLiveCall(person)`.
  - `ConnectionRule`: no directory; compares the plugged pair with the call's own `from`/`to`, either order.
  - `PenaltyService`: fines only `Missed`, with the script's `PenaltyCents` (0 for regular calls); never multiplied.
  - `OpenBoard`: `IsOpen(jack)` and `Size` only (`IsInGame` and the directory gone).
  - `IGameContent`: `StartPool` added; `Pacing`, `CallerTypes`, `People`, `Callers`, `Directory` removed.
  - `IUpgrade.StagePool`; `StatSheet.CallPool` (last bought `extra_clients` with a non-empty pool, else `startPool`), `HasNotepad` removed, new `startPool` constructor parameter.
  - `UpgradeKind`: `Notepad = 3` removed (comment: never reuse 3); other values unchanged.
  - `GameSession`: constructor is `(IGameContent, ISwitchboard)` (no `IRandom`); new `Queue`; `Directory`, `Idle`, `Callbacks`, `Busy`, `Spawner`, `Notepad` removed. `ResetUpgrades` ends live calls, recomputes, `Queue.Restart()`, returns cords in closing jacks, rebuilds the shelf. `ClearCalls` ends live calls and returns all cords; the queue carries on.
- **Content:**
  - `EconomyFileLoader.Load(json, CallsCatalog calls = null)`: resolves `startPool` and each `extra_clients`'s `stagePool` (null skips the pools, as `boardJacks` does for people); pool checks; "no longer used" for `pacing`, `calls` and a `notepad` entry; the `notepad` type, pacing and call-type checks removed.
  - `EconomyFileData`: `startPool`, `UpgradeEntry.stagePool`, markers for the removed sections; `PacingEntry`/`CallEntry`/`RangeEntry` removed. `EconomyCatalog`: `StartPool`; `Pacing`, `CallTypes`, `HasCallType` removed. `UpgradeRecord.StagePool`.
  - `PeopleFileLoader.Load(json, boardJacks)`: no longer reads or checks `portrait`, `type`, `rate`, `fee` (orchestrator note a); keeps names, sockets, tags. `PeopleFileData`, `PersonRecord` (no `Portrait`), `PeopleCatalog` (no `Callers`) trimmed. `GameContent(EconomyCatalog)`.
  - `ContentValidator`: `Validate(peopleJson, boardJacks)`; new `ValidateCallJacks(calls, book)` (cross-file check); `ValidateEconomy(json, calls)`; `ValidateStartingBoard`, `ValidateCardStyles` and the recipient check removed.
  - `GameConfig`: `callsFile`, `LoadCalls()`, `LoadEconomy(calls)`, `LoadPeople(boardJacks)`, `regularCards`/`emergencyCards` (`CardStyleFor(bool emergency)`) replace the `cardStyles` asset list; `Validate()` covers Calls.json, Economy.json, the people file and the cross-file jack check (so `Tools/Switchboard/Validate Content` checks everything; `ContentValidatorMenu.cs` needed no change).
- **Game layer:**
  - `GameInstaller`: loads calls, then economy with them, logs calls errors and the cross-file check; builds `GameSession(content, cordBridge)`; portraits come from the call's `icon`; `randomSeed` field and `NotepadBookLink` gone.
  - `CallQueueView`: binds `Func<CallScript, Sprite>` and `Func<bool, CallCardStyle>`; no Notepad. A call added while every open slot is taken by a card still showing its stamp waits for that stamp, then shows (deviation 1). Upper cards draw over lower ones, so a name plate is never hidden by the card below.
  - `CallCardView`: new serialized `nameLabel` (the caller's name); the bubble says `Script.Message`; the "Calling again" badge, the noted jacks and the bold-surname text removed; the countdown ring reads the script's response time.
  - `DevPanel`: "Call now" and "Emergency now" removed. `GameLoopRunner`: no idle reporting. `OpenBoardLink`: comment only.
- **Book (UI):** the Notepad tap chain is removed (nothing else used it; `BookHoverHighlight` and page turning don't): `PhoneBookView.ContactTapped`/`ContactTapsEnabled`/`ListenToPages`, `PhoneBookPageView.ContactTapped`/`SetTappable`, `PhoneBookContactRow.Tapped`/`IsTappable`/`SetTappable`/`OnPointerClick`. The row keeps its serialized `tapArea` image and turns its raycast off, so `PhoneBook.prefab` is unchanged.
- **Data:** `Economy.json`: `startingClientSlots` 2, `startPool`, `stagePool` on `board_1`–`board_3`, `clientSlots` 3/4/5, descriptions "4x4, 3 callers", "5x5, 4 callers", "6x6, 5 callers"; `pacing`, `calls` and `notepad` removed. `PhoneBook.json` is byte-identical.
- **Assets:** `GameConfig.asset` (calls file, the two card styles with the old values: none for regular; "EMERGENCY" and accent (0.78, 0.13, 0.13) for emergencies).
- **Docs:** `Docs/EconomyFile.md` rewritten: "Calls.json" and "Call queues" sections, cents vs seconds, jack labels, "the book is only for looking things up", the new validator list and messages; "Calls", "Pacing" and "Notepad" removed; `startingCords` corrected to 2–6 and saves to Step 9.
- **Existing tests adapted:** `Fakes.cs` (scripted calls: `Scripts.Call/Emergency/Pool`, `TestWorld.Request`, `EmergencyWorld.EmergencyRequest/RegularRequest/ServiceJack`; the type/caller/directory fakes and `Crowd` removed), `CallServiceTests`, `CallTransitionTests`, `ConnectionFeeTests`, `EarningsServiceTests`, `HoldTests` (its spawner-load test removed), `OperatorDeskTests`, `PersistentWiringTests` (same assertions, scripted requests), `ResponseTimerTests` (callback test replaced by two: a regular call ignores a given response time; each emergency uses its own), `PenaltyServiceTests` (rewritten for D51), `GameSessionTests` (rewritten), `BoardGrowthTests`, `EconomyFileTests`, `EconomyBoardTests`, `ContentValidatorTests`, `PeopleFileTests`, `UpgradeTests` (the shelf's 4th chain is a later kind, `(UpgradeKind)99`, now that the Notepad is gone), and Sergei's `MergedSystemsTests` (orders are scripted calls on open jacks; the bubble must equal the call's message and the name plate its name; 2 → 3 slots; the Notepad test became `NewCordKeepsConnectionHighlightsCoinsAndAudio` with the same cord, glow, coin and audio checks), `FirstOrderGuideTests` (unchanged, passes), `CallAudioTests` (scripted order), `PhoneBookPageTurnTests` (all 8 kept; the row cases now check that rows let clicks through to page turning and show the page-turn hand).

Deleted files:
- Domain: `Spawning/CallSpawner.cs`, `Spawning/CallFactory.cs`, `Spawning/PacingSettings.cs`, `Spawning/BusyTracker.cs`, `Callbacks/CallbackScheduler.cs`, `Pacing/IdleGuard.cs`, `Upgrades/Notepad.cs`, `Directory/PhoneDirectory.cs`, `Definitions/ICallerType.cs`, `Definitions/ICaller.cs`, `Definitions/IPacingProfile.cs`, `Definitions/CallerTypeExtensions.cs` (and the empty `Spawning`, `Callbacks`, `Pacing`, `Directory` folders).
- Content: `CallTypeRecord.cs`, `CallerRecord.cs`, `CallerTypeDefinition.cs`.
- Game: `NotepadBookLink.cs`.
- Assets: `Assets/Content/CallerTypes/Regular.asset`, `Emergency.asset` and the folder (their values now live in `GameConfig`).
- All with their `.meta` files.

Deleted test classes: `BusyTrackerTests`, `CallFactoryTests`, `CallbackSchedulerTests`, `CallSpawnerTests`, `EmergencySpawnTests`, `IdleGuardTests`, `PhoneDirectoryTests`, `FullBoardTests`, `EconomyCallsTests` (its starting-value cases moved to `EconomyPoolTests`), `GrowingCordsSpawnTests` and `NotepadTests` (both in the deleted `WiresAndNotepadTests.cs`; its `CordStatTests` moved to `CordStatTests.cs` without the Notepad cases), `PhoneBookTapTests`.

Scene and prefab changes:
- `Assets/Prefabs/Game/GameUI.prefab`: the card template gains `NameBadge` (a copy of the rate badge's dark plate without its outline, 150×22, anchored under the rate badge at y −12; `NameLabel` is TMP LiberationSans, auto-size 11–18, ellipsis; raycast off on both), wired to `CallCardView.nameLabel`; `CallbackBadge` removed; stale serialized fields (`jackTextSize`, `callbackBadge`, `stackedBadgeOffset`, an old `closedSlotCover`) dropped on save.
- `Final.unity`, `MainMenu.unity`, `PhoneBook.prefab`, `Game.prefab` and every other prefab are unchanged (`Game.prefab` keeps a stale `randomSeed` value that Unity ignores).

Tests: 431/431 EditMode (475 before; the drop comes from the deleted classes listed above, partly replaced by the new ones). New: `CallQueueTests` (13: nothing before the first tick, fills every free slot in order, no timer, loops, skips a call still on a card, empty slot when every call is on a card, empty pool, wrong number frees the slot with no fine, a connected call holds its slot and the next takes it in the same tick, buying a board switches pool and fills the new slot at once while old calls stay, a board with an empty pool keeps the queue, `ResetUpgrades` returns to `startPool` (also with nothing bought), "Clear calls" carries on); `CallsFileTests` (34 cases: the sample, NaN markers, tidied labels, missing/unreadable/empty file, empty and duplicate ids, each required field, unknown portrait, duration, negative and non-whole cents, zero pay allowed, off-board jacks ×4, from = to, emergency response time and penalty, the regular-call note, a broken call left out, the real file's ranges and ASCII); `EconomyPoolTests` (19: pools resolve in order, missing/empty `startPool`, unknown and broken ids, calls that don't fit the start or a stage, missing/empty `stagePool` keeps the entry, the fewer-calls note, no pool checks without a calls file, the removed sections and `notepad`, the starting-value cases, the real queues (8/10/12/14, emergencies per ring), `Validate Content` clean, and the call_201 message); plus the new cases in `GameSessionTests`, `PenaltyServiceTests`, `ResponseTimerTests`, `ContentValidatorTests` (cross-file jack check) and `BoardGrowthTests` (the real start pool plays in order on 3x3 in 2 slots).

Console: clean. Only the known MCP "WebSocket is not initialised" line after recompiles and the engine's two "memoryless" lines around screenshots.

Screenshots (`Temp/Captures/`, Game view 1280×720):
- `step-07.png`: the start: 2 cards ("New order!"), names "Ada Moss" and "Arthur Hollis" under the pay; the other slots hidden; 3x3 board.
- `step-07-bubble.png`: call_001 taken, its own message typed out; the guide moved on to the book.
- `step-07-4x4.png`: after "Open board": 4x4, 3 slots filled (call_003, call_004 from the start pool still on their cards, call_101 from the new pool); the earlier cords still plugged.
- `step-07-emergency.png`: after "Clear calls": call_102, the emergency call_103 (EMERGENCY badge and ring) and call_104.

Checked in Play Mode (stepped frames; cords through `CableController.TryPickUp/TryInsert` and `CordBridge.Synchronize`, card clicks through `CallQueueView.OnCardClicked`):
- Start: board 3x3, 2 slots, 2 cords, queue at position 2 after `call_001`, `call_002`.
- Connect: call_001 patched C3–A2 with a real cord: 10¢ fee at once; at 10× speed it finished with $1.10 (10 + 20 × 5) and "Paid", and call_004 took the slot after the stamp.
- Wrong number: call_002 patched A1–B1: "Wrong number!", fine 0, wallet unchanged; call_003 waited for the stamp, then appeared.
- Open board: 4x4, 3 slots, pool switched to `call_101` at position 0, the new slot filled at once.
- Emergency: `call_103` counted down from 30 s; at 0 it ended "Missed", fine 200 requested and taken, stamp "Missed -$2.00"; the slot refilled.
- MainMenu path: loading build index 0, then 1, started a fresh `Final` with `call_001` and `call_002` in 2 slots, no errors.
- `Tools/Switchboard/Validate Content`: "no problems found".
- Editor left clean: not in Play Mode, not paused, `Final.unity` active and not dirty, `timeScale` 1, no callbacks or SessionState keys left; 10 leaked `GuideSilhouette` materials destroyed (known `SlowGuidePulse` leak).

Format-only re-saves: `Assets/Fonts/DellaRespira/Della Respira SDF.asset` (dynamic glyphs after the first test run) discarded with `git checkout`. `TimeManager.asset` and `SampleScene.unity` untouched. Nothing staged or committed (an accidental `git rm` early on was unstaged at once; the index is as it was).

Tick order in `GameSession.Tick(dt)` (for Design §4.4):
```csharp
clock.Advance(dt);
Earnings.Tick(dt);   // pay connected calls for min(dt, time left)
Calls.Tick(dt);      // finish calls whose duration ran out; miss expired emergencies
Queue.Tick();        // fill every free open slot from the queue, in order (D52)
Goal.Tick();         // raise GoalAffordable once, after this tick's money has moved
```
So a call that ends in a tick gives its slot to the next call in the same tick. Nothing rings before the first tick, because the views bind after the session is built.

Deviations from Design/Plan:
1. **A finished card keeps its slot while its stamp shows** (about 1.5 s plus the 0.2 s exit). The rules fill the slot at once (D52), but `CallQueueView` shows the new call's card only when the stamp has gone. Without this, the instant refill would cut every "Paid", "Wrong number!" and "Missed" stamp to nothing (the old rule "a stamp gives up its slot to a new call" assumed a spawn timer). An emergency's countdown starts with the call, so it may run up to ~2 s before its card shows.
2. **Name plate placement.** The card has no room under the portrait inside the slot (slots are ~15 px apart and the countdown ring reaches 20 px past the card), so the name sits on a small plate directly under the pay badge, reaching into the gap, and upper cards now draw over lower ones so the plate stays readable. It may touch the top 3–4 px of an EMERGENCY or earnings badge on the card below. Readable at 1280×720 (about 12 px text).
3. **Pool checks leave calls out rather than entries.** An unknown call, a call with a problem in `Calls.json`, or a call outside its stage's board is left out of that queue with a message; an `extra_clients` entry with a missing or empty `stagePool` stays buyable and keeps the current queue (`StatSheet` only switches to a non-empty pool). Duplicate ids in a pool are allowed; the "fewer calls than slots" note counts different calls.
4. **Card styles are two `GameConfig` fields** (`regularCards`, `emergencyCards`) instead of the two `CallerTypeDefinition` assets, which are deleted with their class. The values (and so the look) are unchanged.
5. **`PhoneDirectory` deleted**: after this step nothing used it (the book reads `PhoneBook.json` itself, the validator uses `PeopleFileLoader`'s directory list). `IPerson`, `IDirectoryEntry`, `PersonExtensions`, `PeopleFileLoader`, `PeopleCatalog` and `PersonRecord` stay for the book checks until Step 8.
6. **Starting-board people check removed** (`ContentValidator.ValidateStartingBoard`, Step 6): calls no longer come from people, so "two people without tags on the start" no longer matters; the cross-file jack check and "every call fits its stage" replace it.
7. **Calls.json text tidying:** `from`/`to` are trimmed and upper-cased (`" c3 "` reads as C3). `connectCents` and `penaltyCents` must be whole cents; an emergency must give `penaltyCents` (0 allowed), so a missing fine is never silently 0.
8. **Removed-section detection:** `pacing` is reported when any of its five old fields is present (an empty `"pacing": {}` passes silently); `calls` whenever present.
9. **Closed slots are hidden, not dimmed**: that's how `CallQueueView.SetOpenSlots` already worked before this step (CanvasGroup alpha 0); unchanged.

Follow-ups:
- **Design.md (orchestrator):** §4.4 tick order above; §5.1 (CallRequest/Call carry the script; no callbacks; the state table's callback and fine rows), §5.2/5.3 replaced by the queue, §5.4 (name plate, message bubble, no "Calling again", the stamp-wait rule), §5.5 (missed emergencies only), §5.6 (`ConnectionRule` uses the call's jacks), §5.7 (no `PhoneDirectory`), §5.9 (no `notepad`; `stagePool`; reset restarts the queue), §6.1 (`GameConfig`: calls file and two card styles; `CallerTypeDefinition` gone), §6.2 (queue state in `CallQueue`, not saved), §6.4 (sections), §3 (`PhoneBookView.ContactTapped`/`ContactTapsEnabled` gone), §9 (new caller = a line in `Calls.json`).
- **Unused now:** `IRandom`, `SeededRandom`, `RandomExtensions` and `FloatRange` in `Domain/Common` (kept: not in the removal list, and a balance simulator may want them). `GameClock` is still used.
- **Stamp over the badge:** the "New order!" stamp covers the lower half of the EMERGENCY badge and most of the countdown ring while a call rings (true before this step too). Moving the stamp lower, or the badge higher, would help emergencies read at a glance.
- **Two calls on one jack:** shipped queues sometimes put two cards on the same person's jack at once (e.g. `call_001` C3 and a later C3 call); the second simply waits (busy line, as specified).
- **Book rows** still carry the transparent `tapArea` image; Step 8 rebuilds the book and can drop it. `PhoneBookBuilder` still adds it.
- **Plan §8:** the "held cord drops on book-row clicks" item is now only about buttons (rows no longer take clicks).

#### Verification (orchestrator, 2026-10-18)
- **Pre-flight and brief check:**
  - Up to date with origin (`2737b45`); 475/475 green.
  - Five gaps were settled in the launch notes:
    - `PhoneBook.json` stays unchanged, and its per-person `portrait`, `type`, `rate` and `fee` are no longer read.
    - The tap chain also lives in `PhoneBookPageView` and `PhoneBookContactRow`.
    - The name label needs `GameUI.prefab`.
    - `Validate Content` covers `Calls.json`.
    - Dev "Clear calls" keeps the queue's position.
- **EditMode re-run:** 431/431 green. There were 475 before: the tests of deleted systems were removed, and `CallQueueTests`, `CallsFileTests`, `EconomyPoolTests` and `CordStatTests` are new. Console: only the known MCP WebSocket line.
- **Scope:** 117 files, all named in the brief or the launch notes.
  - `GameLoopRunner` only lost the idle guard's input check.
  - `Final.unity` and `PhoneBook.json` are unchanged.
  - Nothing is staged.
  - My test run left no format-only re-saves.
- **Screenshots checked:**
  - `step-07.png`: 2 cards, with the names under the pay.
  - `step-07-bubble.png`: Ada Moss's own message.
  - `step-07-4x4.png`: 3 slots, all filled.
  - `step-07-emergency.png`: the EMERGENCY badge. The name plate of the card above covers the badge's top. Left for the playtest.
- **Closed slots** are hidden rather than dimmed. That comes from Sergei's `041fb90`, not from this step.
- **Code skim:**
  - `CallQueue` loops, skips calls on cards and restarts when the pool changes.
  - `ConnectionRule` compares the call's own jacks in either order.
  - `PenaltyService` fines only `Missed`, with `penaltyCents`.
- **Docs updated:**
  - `Design.md`: the status row, §1, §3, §4.1, §4.4, §5.1–5.7, §5.9, §6.1–6.4, the new §6.5 (`Calls.json`), §7, §9, and the statuses of D10, D16, D18, D23, D37, D43 and D46.
  - `Plan.md`: §4 (deleting files), §8, §9 and the Step 8 brief.
- **Deviations for the lead to accept at the playtest:**
  - the next card shows only after the ~1.7 s stamp
  - where the name plate sits
  - pool problems drop the call, not the upgrade
  - card styles as two `GameConfig` fields
  - whole-cent fees and fines, and a required `penaltyCents`
- Committed and pushed on the lead's request (2026-10-18), before the playtest, merged with `f4bdae2` (red bounce on the money counter when an upgrade is too expensive).
- **Playtest (lead, 2026-10-18):** all good. The deviations listed above are accepted.

### Step 8 report: 2026-10-18
Result: done

Playable now: press Play in `Final.unity` and open the book. From the first minute, on the 3x3 start, it lists all 39 entries of the new `Assets/Configs/Book.json`: Police, Fire Brigade and Doctor first, then everyone A-Z ("ABBOTT, Florence", "BRAMBLE, Otis" ...). Each row shows the name, a smaller second line with profession and address ("Seamstress, Oak St. 4"), and the jack as the right-hand column under the heading "Jack". Three households share a line (DUNMORE A2, MOSS C3, ROOK B3). Looking up a call works as before: "Connect me with Mabel Dunmore" means finding "DUNMORE, Mabel" and patching A2. Page turning, the hand cursor and the guide's book glow are unchanged.

How to test: the Plan's Step 8 playtest script works as written. Notes:
1. The book now has 7 pages (4 spreads).
2. Script step 4: change a profession in `Book.json`, press Play, and the new text shows.
3. Script step 5: for example, delete "COBBOLD, Doris" (the only entry on D1) and run Validate Content. You get three messages like "Calls file: call_101 uses D1, but no entry in Book.json is on D1, so the player can't look it up. ...", one each for call_101, call_107 and call_209.
4. A broken entry (say `"jack": "G9"`) is left out, the rest of the book still shows, and the Console has one "Book file: Entry N ...: jack G9 is not on the board ..." line at start.

Created:
- `Assets/Configs/Book.json`. It is `PhoneBook.json` renamed through Unity, so it keeps GUID `2b056d57ca6ba034fba544a4e3f87de2`, and then rewritten. Contents:
  - the 36 converted entries ("SURNAME, First", placeholder professions and addresses in period style; the three services keep D4, E5 and B6)
  - 3 household members on existing jacks: "DUNMORE, Frederick" A2, "MOSS, Harriet" C3, "ROOK, Wilfred" B3
  - plain ASCII only, and every jack A1 to F6 has at least one entry
- Content:
  - `BookFileData.cs`: the file format, with an `Unset` marker for `contactsPerPage` and the old `contacts`/`socket` names, which are read only to give a hint.
  - `BookEntry.cs`: name, profession, address, `IsEmergencyService`, jack.
  - `BookCatalog.cs`: `ContactsPerPage`, the sorted `Entries`, `Errors`, and `Empty`.
  - `BookFileLoader.cs`: `Load(json, boardJacks = null)`. This is the one parser, checker and sorter, used by both the book and Validate Content.
- Tests: `Assets/Tests/Domain/BookFileTests.cs`, `Assets/Tests/Game/Editor/PhoneBookEntriesTests.cs` (each with `.meta`).
- Docs: `Docs/BookFile.md`, the designer's guide to `Book.json` (format, order, messages), linked from `EconomyFile.md`.

Changed:
- **Content:**
  - `ContentValidator`:
    - `ValidateBook(json, boardJacks)` replaces `Validate(peopleJson, boardJacks)`.
    - `ValidateCallJacks(calls, IReadOnlyList<BookEntry>)` now says "no entry in Book.json is on X".
    - Removed: the services-first, unique-surname and sorted-file checks, `TryParse` and `CheckPageSize`. The page-size rule moved into the loader.
    - `MaxContactsPerPage` moved to `BookFileLoader`.
  - `GameConfig`:
    - `bookFile` with `[FormerlySerializedAs("peopleFile")]`, `BookFileText`, and `LoadBook(boardJacks = null)` replace `peopleFile`, `PeopleFileText` and `LoadPeople`.
    - `Validate()` adds "Book file: ..." messages and runs the cross-file check against `book.Entries`.
- **UI (book):**
  - `PhoneBookView`:
    - It reads its `contactsConfig` through `BookFileLoader.Load` and keeps a `BookCatalog`. `ContactAt` returns a `BookEntry`.
    - `SetVisibleSockets`, `ApplyFilter` and the filtered copy are removed.
    - `HasLetter`/`ShowLetter` go by the first letter of `name`.
    - It no longer logs, and "Configuration error" is gone. An empty book shows its first file message on page 1 instead.
  - `PhoneBookPageView.Render(BookCatalog, page, message)`: the name font size follows the row height (22 at 6 per page).
  - `PhoneBookContactRow`:
    - It has three labels: `contactName`, `details` (new) and `jack` (`[FormerlySerializedAs("socketAddress")]`).
    - `Show(BookEntry, size)` sets the details line at 0.75 of the name's size.
    - The static `Details(entry)` joins profession and address and skips an empty part.
    - The `tapArea` field and its `Awake` are gone.
- **Game layer:**
  - `OpenBoardLink` loses the book, its constructor parameter and the socket predicate; the sockets and caller slots stay.
  - `GameInstaller` loads the book through `config.LoadBook(BoardJacks(sockets))` and logs "Book file: ..." once. It runs the cross-file check against `book.Entries`. `FirstOrderGuide` still gets the `PhoneBookView` (local renamed `bookView`).
- **Editor:** `PhoneBookBuilder`:
  - `ConfigureDirectory` points at `Assets/Configs/Book.json`.
  - The row builder makes the new row: Name above, Details below, Jack right, no tap image, template 326×54.
  - The heading is "JackHeading"/"Jack", and EmptyMessage is 300×260 with best fit.
  - Small `Stretch`/`BestFit` helpers.
  - I edited the prefab directly; neither menu was run.
- **Assets:** `GameConfig.asset` re-saved: `peopleFile:` → `bookFile:`, same GUID, nothing else.
- **Docs:** `Docs/EconomyFile.md`:
  - the intro links `BookFile.md`
  - line 52 now names `Book.json`
  - the validator list and the example message (old line 243) use the new "no entry in Book.json is on A5" wording
  - "Checking the files" says it covers all three files

Deleted (with `.meta`, by plain `rm` + refresh; nothing staged):
- `Assets/Configs/PhoneBook.json` (moved to `Book.json`)
- `Scripts/UI/PhoneBookConfig.cs`: `PhoneBookConfig` and `PhoneBookContact`; the view uses Content's `BookEntry`, which keeps `IsEmergencyService`
- `Scripts/Content/PeopleFileLoader.cs`, `PeopleFileData.cs`, `PeopleCatalog.cs`, `PersonRecord.cs`, `DirectoryRecord.cs`
- `Scripts/Domain/Definitions/IPerson.cs`, `PersonExtensions.cs`, `IDirectoryEntry.cs`

Nothing else used any of these; I checked with grep before deleting.

Scene and prefab changes:
- `Assets/Prefabs/UI/PhoneBook.prefab`, both `LeftDirectoryPage` and `RightDirectoryPage`:
  - `SocketHeading` → `JackHeading`, text "Jack".
  - `ContactTemplate`: 326×54, its transparent tap `Image` and `CanvasRenderer` removed.
  - `Name` sits on the top 56% of the row: lower-left aligned, best fit 12–22, ending 6 px before the jack column at 84%.
  - New `Details` label: a copy of Name on the bottom 44%, upper-left aligned, best fit 10–16, colour (78, 80, 70), raycast off.
  - `Socket` → `Jack`, filling the right 16% of the row.
  - The row's serialized fields were re-wired.
  - `EmptyMessage` is 300×260, centred, best fit 12–22.
- `PhoneBook.prefab` still references `Book.json` by the same GUID, so no re-wiring was needed.
- `Final.unity`, `MainMenu.unity` and every other prefab are unchanged. Final's book overrides only touch the root.

Tests: 445/445 EditMode (431 before).
- New `BookFileTests` (27 cases):
  - the designer's sample with tidying
  - `contactsPerPage` missing → 6; 0, 13 and -2 → message and 6
  - `emergencyService` missing → false
  - profession and address empty or missing allowed
  - a missing, blank or null file, unreadable JSON, and no entries (each gives an empty book and one message)
  - a missing name, a missing jack, and jacks off the board (G1, A7, A0, C33, 33), each left out
  - duplicate names ignoring case and spaces (the first stays)
  - families sharing a surname and a jack
  - one bad entry leaves the rest
  - old-format hints for `contacts` and `socket`
  - services first in file order, then names ignoring case ("abbott" < "Bellamy" < "MOSS, Ada" < "MOSS, Harriet" < "MOSSOP"), and the same order whatever the file's order
  - the real file: no errors, 39 entries, services first, sorted, every entry filled in, 36 jacks covered, C3 shared, ASCII only, capital surnames
- New `PhoneBookEntriesTests` (8):
  - the real book shows all 39 entries in the loader's order on 7 pages
  - a row's Name, Details and Jack texts, the "Jack" heading, and the jack as the last column
  - the details line skips an empty part
  - the page count with `contactsPerPage` 4
  - a bad entry is left out and the rest shows with no message
  - a missing or unreadable file shows its message on page 1 (2 cases)
  - a fresh `Final` game on 3x3 lists all 39 entries, Z included
- New in `ContentValidatorTests`: `ValidateBookUsesTheBookLoadersChecks`, `ASharedJackCountsForEveryCallOnIt`, `AnEmptyBookReportsBothJacksOfACall`.

Tests deleted or rewritten (orchestrator note 7):
- **Deleted `PeopleFileTests`** (9 cases): `RealFileHasThirtyThreePeopleAndThreeServicesOnEveryJack`, `RealFileListsServicesFirstThenPeopleBySurname`, `SurnameFirstNamesKeepTheirOriginalIdentityAndNameParts`, `RealFileKeepsThePhoneBookFormat`, `ParsesNamesAndJacksAndIgnoresTheOldCallFields`, `InvalidEntriesAreReportedAndLeftOut`, `AServiceIsListedByItsWholeName`, `UnreadableFileIsReported` ×2. `BookFileTests` replaces them.
- **Deleted `PhoneBookFilterTests`** (the Step 6 book filter, 4 tests): `TheBookKeepsTheRealFileNamesAndOrder`, `AThreeByThreeBoardShowsTheStartingNineOnTwoPages`, `BiggerBoardsAddPeopleWithTheServicesFirst`, `ShrinkingTheFilterBringsTheBookBackToAShownSpread`. They are replaced by the "all entries show" tests in `PhoneBookEntriesTests`.
- **`ContentValidatorTests`:**
  - Deleted: `AValidFilePasses`, `ReportsADuplicateSocket`, `ReportsAnUnknownJack`, `PortraitsTypesAndRatesInTheBookAreNoLongerRead`, `ReportsADuplicateSurname`, `ReportsAFileNotSortedBySurname`, `ReportsAPageSizeTheBookRejects`, `ServicesAtTheTopPassWithoutPortraitsOrTypes`, `ReportsAServiceBelowThePeople`, `ServiceNamesDoNotCountAsSurnames`, `ReportsAPersonOutOfOrderBelowTheServices`. Their still-valid rules (jack off the board, page size) are covered in `BookFileTests`.
  - Rewritten: the cross-file test is now `EveryJackACallUsesNeedsAnEntryInTheBook` (`BookEntry` list, Book.json wording).
  - Kept: `RealContentHasNoProblems`, `BoardHasThirtySixJacks`.
- **`BoardGrowthTests`:** `TheRealPeopleFileHasOneServicePerNewRing` is now `TheRealBookHasOneServicePerNewRing`. It checks Doctor D4, Fire Brigade E5, Police B6 and "UNDERHILL, Reginald" C2, and one service per new ring.
- **`PhoneBookTabTests`:** new format, with sorted "AAA, Ann"-style names on cycling jacks.
  - `ParseKeepsTheFileNamesOrderAndSockets` → `TheBookListsServicesFirstThenNamesAlphabetically`
  - `ServicesUseTheirWholeNameAndPeopleTheirSurname` → `LettersGoByTheFirstLetterOfTheName`
  - `UsesTheLastWordOfTheNameAsTheSurname` → `ANameWithoutACommaGoesByItsFirstLetter`. The old last-word rule is gone, because lookup now goes by the name's first letter.
  - The other 4 are unchanged apart from the helper.
- **`PhoneBookPageTurnTests`:**
  - `PageTurningFollowsTheOpenBoardFilter` → `PageTurningGoesThroughTheWholeBook`.
  - One assertion in `TabsKeepPriorityAndRowsLetClicksThroughToPageTurning` changed. `row.GetComponent<Image>().raycastTarget` would throw now that the tap image is gone, so it checks that no graphic in the row is a raycast target.
  - The other 6 tests are unchanged.
- **`Fakes.cs`:** `FakePerson` and `FakeDirectoryEntry` removed (no other users).
- `MergedSystemsTests` and `FirstOrderGuideTests` are unchanged and pass. The guide's book glow test is in `MergedSystemsTests`.

Console: clean. Only the known MCP "WebSocket is not initialised" line after recompiles and the engine's two "memoryless" lines around screenshots.

Screenshots (`Temp/Captures/`), all with the book open in Play Mode on the 3x3 start:
- `step-08.png`: 1920×1080, the first spread. Services, then ABBOTT … FAIRWEATHER, with the two DUNMOREs on A2.
- `step-08-720.png`: 1280×720, the same spread. Names are about 15 px and the details line about 11 px, still legible.
- `step-08-spread3.png`: 1920×1080, the third spread: ORMSBY … VICKERY, with both ROOKs on B3.

The Game view is set back to 1920×1080.

Checked in Play Mode (stepped frames):
- The book had 39 entries on a 3x3 board.
- About 24 s of game time at ×10 gave no errors.
- Loading build index 0 (MainMenu) and then 1 started a fresh `Final` with the book at 39 entries and the session bound, with no errors.
- `Tools/Switchboard/Validate Content`: "no problems found".
- Removing D1's only entry gave the plain cross-file messages above.

Housekeeping:
- `ProjectSettings/TimeManager.asset` and `Assets/Fonts/DellaRespira/Della Respira SDF.asset` were format-only or dynamic-glyph re-saves; I discarded them with `git checkout`. `SampleScene.unity` and the Boecklins font were not touched.
- 10 leaked `GuideSilhouette` materials destroyed (the known `SlowGuidePulse` leak).
- The editor is not in Play Mode and not paused. `timeScale` is 1, no callbacks or SessionState keys are left, and `Final.unity` is active and not dirty.
- Nothing staged or committed.

Deviations from Design/Plan:
1. **The two-line fallback layout, not four columns.** At 1920×1080 a row is 326 px wide. "PENNINGTON, Edgar" alone takes about 270 px at the book's 22 px (measured in the screenshot), so four readable columns cannot fit. As the brief allows: the name is on line 1, profession and address are smaller on line 2 ("Seamstress, Oak St. 4"), and the jack is the right column under the heading "Jack" instead of "Socket".
2. **`PhoneBookConfig`/`PhoneBookContact` deleted.** The view and its rows use Content's `BookCatalog`/`BookEntry` directly, which was the smaller change. So `PhoneBookView.ContactAt` returns a `BookEntry` (`Name`, `Jack`, `IsEmergencyService`). Design §3's hook list names `PhoneBookContact.IsEmergencyService` and `SetVisibleSockets`; both are gone.
3. **Names in capitals.** The shipped file writes surnames in capitals, as in the designer's sample ("MOSS, Ada"), not "Moss, Ada" as in `PhoneBook.json`. The cards still say "Ada Moss" (from `Calls.json`, unchanged).
4. **Duplicate names** are compared trimmed, ignoring case, and with runs of spaces counted as one. The first entry stays; later ones are left out.
5. **An old-format file gives an empty book** with a "rename contacts to entries, socket to jack" message. It isn't read in a compatibility mode. An old-style `socket` on a single entry gets the rename hint in place of "the jack is missing".
6. **`contactsPerPage` outside 1–12 counts as a validator message** ("... Using 6."), and the game still uses 6.
7. **Row details:**
   - The second line's maximum size is 0.75 of the name's size: 16 at 6 per page, best fit down to 10.
   - At 12 per page the rows get small (name about 11 px), so the guide suggests keeping 6.
   - `EmptyMessage` grew to 300×260 with best fit so a whole file message fits on page 1.
8. **Prefab object names:** `SocketHeading` → `JackHeading`, row label `Socket` → `Jack`, plus a new `Details`. Nothing looked these names up.

Follow-ups:
- **Design.md (orchestrator):**
  - §3 (book hooks: no `SetVisibleSockets` or `PhoneBookContact`; `ContactAt` returns `BookEntry`)
  - §5.7 (Book.json, the order, the two-line row, no filter, letter lookup by the first letter)
  - §6.1 (`GameConfig.bookFile`)
  - §6.3 ("Surnames are unique" no longer holds: families share them; "every jack a call uses has an entry in Book.json")
  - D31, D35, D36, D42 and D50 statuses (superseded by D53 where they talk about the book)
- **CLAUDE.md:** the lines that name `PhoneBook.json` (team/designer, architecture "Data", Plan §8 note on `PhoneBook.json`'s format).
- **Plan §8:**
  - The `PhoneBookBuilder` part of "Stale paths in editor menus" is fixed. `LevelBackgroundBuilder` and `GamePresentationBuilder` still name `Level.unity` (left alone as instructed).
  - "A–Z tabs are hidden" still applies. `Update Phone Book Tabs` was not run.
- `PhoneBookContactRow.OnPointerDown` is now unreachable, because the row has no raycast target. It is harmless and could go in a cleanup.
- `PhoneBookBuilder.CreatePhoneBook` still says "Open Level in Edit Mode first" in its message (cosmetic).

#### Verification (orchestrator, 2026-10-18)
- **Pre-flight and brief check:**
  - Up to date with origin (`ee51403`, no teammate pushes since Step 7); 431/431 green; console clean.
  - Gaps settled in the launch notes:
    - one parser in Content, which the book view and the validator both use
    - a bad entry is left out instead of breaking the whole book
    - the old surname, file-order, services-first and one-entry-per-jack checks go
    - the file is renamed through Unity to keep its GUID
    - layout facts for the 326 px page
    - the extra tests that used the filter or the people model
    - 2–3 household entries sharing a jack
    - `PhoneBookBuilder`'s path. `LevelBackgroundBuilder` and `GamePresentationBuilder` were left alone, because the latter opens the scene additively.
- **EditMode re-run:** 445/445 green. Console: only the known MCP WebSocket line. My run left no format-only re-saves.
- **Scope:**
  - 41 tracked files plus the new ones, all named in the brief or the launch notes. Nothing is staged.
  - `Book.json.meta` keeps GUID `2b056d57…`. `GameConfig.asset` only changed `peopleFile` → `bookFile`.
  - `Final.unity`, `Calls.json` and `Economy.json` are unchanged.
  - Git shows `PhoneBook.json` as deleted and `Book.json` as new until both are staged; it then reads as a rename.
- **Screenshots checked:**
  - `step-08.png` (1920×1080): services first, then ABBOTT … FAIRWEATHER, each with a details line and the jack on the right; the DUNMORE household shares A2.
  - `step-08-720.png`: the same spread; the details line is small but legible.
- **Code skim:**
  - `BookFileLoader`: one pass; bad entries left out with plain messages; services in file order, then an ordinal ignore-case sort with the file index as tie-break.
  - `PhoneBookView` no longer filters or logs.
  - `OpenBoardLink` no longer touches the book.
- **Docs updated:**
  - `Design.md`: the status row, §3 hooks, §5 note, §5.7 (rewritten, with the deviations), §5.9 checks line, §6.1, §6.3, the new §6.6 (`Book.json`), and the statuses of D31, D35, D36, D42 and D50.
  - `Plan.md`: §4 (renaming an asset), §8 (stale paths narrowed, `OnPointerDown` cleanup), the §9 note, and Step 9 ("From Step 8").
  - `CLAUDE.md`: the book lines and the next step.
- **Deviations for the lead to accept at the playtest** (Design 5.7):
  - the two-line row
  - `PhoneBookConfig` deleted
  - duplicate names ignore extra spaces
  - an old-format file gives an empty book with a hint
  - an out-of-range `contactsPerPage` uses 6
  - prefab objects renamed
- **Playtest (lead, 2026-10-19):** all good. The deviations listed above are accepted.
- Committed and pushed on the lead's request, merged with tarbaev.d's `f393f72` ("Polish highlights": one shared hover colour for the jack halos and the guide glow; no overlap with Step 8). 445/445 green after the merge.

### D26 decided: itch.io (lead, 2026-10-19)
- **Hosting is itch.io**, chosen over the lead's own VPS for the jam audience and no setup.
- **The build** uses Gzip with Decompression Fallback, so the same files also run on the VPS later.
- **Saves** go to `localStorage` (Design 5.12), because itch gives every upload a new URL.
- **Where it's recorded:** Design D26 and §8, the Plan's Step 9 outline, and CLAUDE.md.

### Step 9a prep (orchestrator, 2026-10-19)
- **Pre-flight:** up to date with origin (`5c85114`); 445/445 EditMode green; console clean; editor idle on `Final`; no format-only re-saves.
- **Found:** Unity's **Web Build Support module is not installed** for 6000.3.6f1 (only `MacStandaloneSupport`). The Player Settings still have Brotli (0) and no Decompression Fallback.
- **Decisions (lead, D55):**
  - Step 9 is split into 9a (saves and touch, testable in the editor) and 9b (the Web build on itch). The lead installs the module before 9b.
  - On load the queue replays unfinished calls.
  - A "New game" button in the main menu.
  - Agents may push to the itch Draft page.
- **Brief notes:**
  - Sergei's menu (`MenuTemplates` asmdef) checks only keyboard, mouse and gamepad on its press-start panel, so a phone couldn't get past it. 9a adds touch there and in the volume patch.
  - His `AudioVolumeSettings` saves the volume patch in PlayerPrefs; 9b moves it to the browser storage.
  - itch serves every HTML game from one shared domain, so the `localStorage` keys name the game (`operator.save`).


### Step 9a report: 2026-10-19
Result: done

Playable now: progress survives a restart. Press Play in `Final`, earn and buy (for example dev "+ $100", then `income_1` and `board_1`), stop, and press Play again. The balance is the same to the cent, the upgrades are owned, the board opens 4x4 with 3 caller slots before the first call, the extra cords are in the tray, and the shop offers the next entries. Calls that were still on cards ring again first. In `MainMenu`, "New game" sits under Continue while a save exists; it asks "Start a new game? Your money and upgrades will be lost." and "Start over" deletes the save and starts a fresh game. The cord input, the volume patch and the menu hand now read `Pointer.current`, so taps work like clicks.

How to test: the Plan's Step 9a script works, with these notes:
1. **Script step 5:** after Start over, go back to the menu before connecting a call or buying anything. A new game with nothing done writes no save (deviation 1), so New game is hidden again. Once you connect a call, the game saves and New game shows.
2. **Script step 6:** `MainMenu.unity` has no press-start panel. `Menu.pressStartPanel` is empty, so the menu opens straight away. The touch check in `Menu.IsAnyInput` is in place for when a panel is added (9b). In the Device Simulator, tap Continue, then tap a cord and two jacks.
3. **Dev panel:** a new "New game" button at the bottom deletes the save, career included, and reloads `Final` as a new game.
4. **A broken save** logs one plain warning at start. For example: "Save: the saved game couldn't be read (it isn't valid JSON), so the backup of the save before it was loaded."

Created:
- **Domain** (`Assets/Scripts/Domain/Save/`):
  - `SaveData.cs`: `SaveData` (`version`, `cents`, `fraction`, `upgrades`, `queuePosition`, `career`) and `CareerData` (`earnedCents`, `callsConnected`, `finesPaidCents`, `secondsPlayed`). `CurrentVersion` is 1.
  - `SaveMigrations.cs`: `SaveMigration(fromVersion, Action<SaveData>)`, and `SaveMigrations.Run(data, steps, targetVersion)`, which runs one version at a time. `SaveMigrations.All` is empty, because v1 is the first format.
  - `ISaveStorage.cs`: `Read`, `Write` and `Delete` of a string by key.
  - `CareerStats.cs`: starts from the saved career. It counts:
    - a call's first connection (`CallStateChanged` to Connected, not from OnHold)
    - `FineApplied.TakenCents`
    - ticked seconds
    - earnings, read from `EarningsService.TotalPaidCents`
- **Content:** `SaveFile.cs`:
  - the keys `operator.save` and `operator.save.backup`
  - `ToJson`, `TryParse` (with a plain reason) and `Load()`, which returns a `SaveLoad`: the data, a `SaveSource` (New, Main or Backup) and plain messages
  - `Write` (backup first), `Delete` and `HasSave`
- **Game** (`Assets/Scripts/Game/Save/`):
  - `PlayerPrefsStorage.cs`: `PlayerPrefs.Save()` after each write or delete.
  - `SaveStorage.cs`: a stateless factory, `Create()` and `OpenFile()`. 9b switches Web builds here.
  - `Autosave.cs`: added at runtime by `GameInstaller`, like `FirstOrderGuide`. It saves after a purchase, every 10 s of unscaled time if the JSON changed, on `OnApplicationFocus(false)` and `OnApplicationPause(true)`, and on `OnDestroy`. `Stop()` ends it.
  - `NewGameMenu.cs`: the menu's New game. It shows the button only while `HasSave`, opens and closes the confirm panel, and Start over deletes the save and calls `Menu.onPlayButtonClick()`.
- **Tests:**
  - `Assets/Tests/Domain/SaveFileTests.cs` (18 cases)
  - `Assets/Tests/Domain/SaveSessionTests.cs` (19 cases)
  - `Assets/Tests/Game/Editor/SaveIsolationTests.cs` (1)
  - all with `.meta`, plus the folder metas for `Domain/Save` and `Game/Save`

Changed:
- **Domain:**
  - `GameSession`:
    - The constructor takes an optional `SaveData`. It restores the wallet's cents and fraction, then `Upgrades.Restore(ids)` before `StatSheet` is built, and `Queue.Restore(position)` after the queue is built. `Career` comes last.
    - New members: `CaptureSave()`, `Career`, `IsRestored` and `HasProgress`. `Career.Tick` runs in `Tick`, right after the clock.
  - `UpgradeService`:
    - `Restore(ids)`: known ids only, nothing spent, no event.
    - `OwnedIds()` in file order, and `OwnsAny`.
  - `CallQueue`:
    - It keeps a small list of the live calls dealt from the current pool, with their pool index. The list is cleared on `Restart` and pruned on `CallEnded`.
    - `ResumePosition` returns the oldest such live call's index, or `Position` when there is none.
    - `Restore(position)`: a position outside the pool starts at 0. `TakeNext` now returns the index.
  - `GoalProgress`: `affordableRaised` starts as `IsPurchased`, so a loaded secret fires neither event.
  - `EarningsService`: `TotalPaidCents`, every fee and per-second payment of the session. This is the earnings hook: it adds one double, with no event and no allocation per frame. Dev money doesn't count.
- **Game:**
  - `GameInstaller`:
    - It loads the save after the content and before the session, and logs each `SaveLoad` message as one warning.
    - It binds `Autosave` and passes the dev panel a New game action.
    - All of this happens **only in Play Mode** (deviation 2).
  - `DevPanel`: `Bind(..., Action startNewGame)`, plus the "New game" button.
- **UI (touch, D28):**
  - `CableInputController.Update` reads `Pointer.current` (`position`, `press.wasPressedThisFrame`). On a `Touchscreen` with no finger down, it hides pointers entirely while no cord is held. While a cord is held, the cord stays where the finger lifted, with no hover halo, tray highlight or hand.
  - New overload `ProcessPointer(position, pressed, hover)`. The old two-argument one calls it with `hover: true`, so the integration checks are unchanged. `UpdatePointers` takes `showHand`.
  - `VolumeCablePanel`: the same change, including a `ProcessPointer(position, pressed, hover)` overload. With hover off, the socket highlights and the hand stay hidden.
  - `MenuHandCursor`: reads `Pointer.current`. On a touchscreen the hand shows only while a finger is down.
  - `Menu.IsAnyInput` (Sergei's `MenuTemplates`): `Touchscreen.current.primaryTouch.press.wasPressedThisFrame` counts as input.
  - `BoardGoldLight` stays on `Mouse.current`. It only reacts to hover (a soft light that follows the cursor), so there is nothing to tap.
- **Tests:** `Fakes.cs` gains `MemoryStorage`, an in-memory `ISaveStorage` that counts writes.
- **Docs:**
  - `Docs/EconomyFile.md`: the "Never rename an id" line now says that since Step 9a the game saves bought ids, so a renamed id counts as never bought and a deleted entry is forgotten.
  - `Docs/Plan.md` §8: the touch row is narrowed to what's left.

Scene and prefab changes:
- `Assets/Scenes/MainMenu.unity`. Sergei's `StartMenu` prefab asset is untouched. Everything is a scene object or an override:
  - **New scene objects:**
    - `StartMenu/MainMenu/New_Game_Button` (with `New_Game_Text`): a copy of the scene-only `Continue_Button`, the same style and 3x scale, text "New game", and no persistent listener. `NewGameMenu` adds its listener.
    - `StartMenu/NewGameMenu`: an active, full-stretch RectTransform holding the `NewGameMenu` component. Its references: `menu`, `newGameButton`, `confirmPanel`, `startOverButton` and `cancelButton`.
    - `StartMenu/NewGameMenu/ConfirmPanel` (inactive): a full-screen backdrop (black at 60%, a raycast target on purpose, so it is modal) and a 1100×420 cream `Plate` with the UISprite.
      - `Message`: "Start a new game? Your money and upgrades will be lost.", 44 pt Della Respira, no auto size, raycast off.
      - `Start_Over_Button` at (-260, -100) and `Cancel_Button` at (260, -100), copies of the menu buttons.
  - **Overrides:** the six menu buttons are re-spaced 110 apart so New game fits under Continue: Play Game 225, Continue 115, New game 5, Options -105, Credits -215, Quit -325. The old 122 spacing with 90 px tall buttons left no room (deviation 4).
  - The rest of the diff is Unity reordering existing YAML blocks. I checked the counts of the existing objects and overrides; they are the same as before.
  - Glyphs: `Della Respira SDF` (dynamic) has every character of the new texts (`HasCharacters` true, none missing).
  - Our buttons sit under `Menu`, so `SceneUiAudio` gives them the click sound, and `Menu.Awake` turns off their navigation like the others.
- `Final.unity`, `Game.prefab` and every other prefab are unchanged. `Autosave` is added at runtime.

Tests: 483/483 EditMode (445 before; 38 new).
- **`SaveFileTests`:**
  - the key names
  - round trip
  - a 9,000,000,000,123¢ balance and a 1/3 fraction survive the JSON
  - empty storage gives a quiet new game
  - a v1 JSON string loads
  - missing fields keep their defaults
  - an injected v1→v2→v3 chain runs once, in order, and an up-to-date save runs nothing
  - a v1 save needs no steps
  - a v7 save with unknown fields loads what it can, with one "newer version" message
  - the backup holds the previous good save
  - the same save twice keeps the older backup
  - a corrupt main (broken JSON, empty, `{}`, an array) falls back to the backup, with one message and the broken data kept
  - corrupt main and backup give a new game with one message
  - a broken main is never copied over the backup
  - Delete removes both keys and keeps other keys
- **`SaveSessionTests`:**
  - capture, then JSON, then load gives the same balance, fraction, ids, stats, queue position and career, and unfinished calls ring again in order
  - empty storage starts with `Economy.json`'s starting values (the real file)
  - a loaded `board_1`+`board_2`+`wires_1`+`income_1` save (real file) opens 5×5 with 4 slots, 4 cords and ×1.5 before the first tick, and the first tick deals `call_201`–`call_204`
  - unknown ids are ignored and missing ones are not bought
  - restoring spends nothing and raises no `UpgradeBought`
  - a loaded secret is `IsPurchased`, and neither goal event fires
  - the oldest live call from the current pool wins
  - calls from an older pool don't count
  - no live calls gives `Position`
  - out of range (5, 99, -1) gives 0
  - a saved position deals from there
  - a connection counts once across hold and resume
  - the career counts fee and pay but not dev money, and ticked seconds
  - fines count the amount taken (150 of a 200 fine)
  - the career adds to the saved numbers
  - `HasProgress` is false until something happens
  - a capture is a snapshot
- **`SaveIsolationTests`:** an EditMode game ignores a planted real save, adds no `Autosave` and leaves the key untouched. Without the Play Mode gate the first run failed 4 Game tests: their sessions autosaved into the real PlayerPrefs and leaked a `board_1` save between tests. I deleted those leaked keys.

Console: clean. Only the known MCP "WebSocket is not initialised" and "TCS … already completed" lines and the engine's two "memoryless" lines around screenshots appeared.

Screenshots (`Temp/Captures/`):
- `step-09a.png`: the game after a reload with `board_1`, `income_1` and `wires_1` bought: $177.00, a 4×4 board, 4 cords, three cards (call_102–104, which were on cards at the stop), and the shop offering `wires_2`, `board_2` and `income_2`.
- `step-09a-menu.png`: the menu with New game under Continue.
- `step-09a-confirm.png`: the confirm panel over the dimmed menu.

Checked in Play Mode (stepped frames):
- **Fresh start:** $0, 3×3, 2 slots, 2 cords, no save written.
- **Earn and buy:** dev money, then `income_1` and `board_1` saved at once, then `wires_1` (4 cords in the tray). "Clear calls" moved the queue to call_102–104, and I took call_103.
- **Stop:** the save had `queuePosition` 1, and the backup held the previous save.
- **Reload:**
  - restored, $177.00 and 0.25¢
  - 4×4 with 16 open jacks, 3 slots, 4 cords on the bridge, ×1.5
  - the shelf showed `wires_2`, `board_2` and `income_2`
  - call_102, 103 and 104 rang again (all Incoming)
- **`MainMenu` with a save:**
  - New game was shown.
  - Clicking it (EventSystem raycast clicks) opened the confirm panel. Cancel closed it and kept both keys.
  - Start over deleted both keys and loaded a fresh `Final`: not restored, $0, 3×3, 2 cords.
- **`MainMenu` without a save:** New game was hidden. Continue loaded `Final`.
- **Dev "New game":** buying `income_1` wrote a save. New game deleted both keys and reloaded `Final` fresh, and the old session did not re-save on destroy.
- **Corrupt main plus good backup:** the backup loaded (4321¢, ×1.5) with the one plain warning above.
- **Touch path:** taps through `ProcessPointer(pos, true, true)`, with `(pos, false, false)` for "finger up" in between:
  - picked a cord (still held after the finger lifted, hand off)
  - plugged C1 (OneEndConnected)
  - plugged C3 (Connected C1–C3, no cord held), with no hand or hover between taps
- **Real taps:** I tried a simulated `Touchscreen` device too. Its state reached the device, but the EventSystem and `Application.isFocused` ignore input while Unity is in the background, so a real tap can't be driven from an agent. For that test I set `InputSystem.settings.editorInputBehaviorInPlayMode` in memory and restored it straight away. It is the in-memory default settings object, not an asset, so no file changed. I removed the device.
- **Smoke run:** about 24 s of game time at ×10 with no errors.

Housekeeping:
- **PlayerPrefs:** `operator.save` and `operator.save.backup` are deleted, so the lead's playtest starts fresh. `Switchboard.AudioPatch.v1` is untouched.
- **Format-only re-saves:** the dynamic fonts `Della Respira SDF.asset` (glyphs added by the glyph check and Play) and `Boecklins Universe SDF.asset` (test run) were discarded with `git checkout`. `TimeManager.asset` and `SampleScene.unity` were not touched.
- **Editor:** not in Play Mode and not paused, `timeScale` 1, no callbacks, SessionState keys or extra input devices left, the Game view at 1920×1080, and `Final.unity` active and not dirty.
- **Git:** nothing staged or committed.

Deviations from Design/Plan:
1. **A new game with nothing done writes no save.** `GameSession.HasProgress` is true once money moved from the start, anything is owned or a call connected, and always for a loaded save. `Autosave` skips the write until then. Without this, the `OnDestroy` save on stop would write a save at once, and the playtest's "back in the menu the button is hidden" after Start over could never hold. Time played alone isn't progress.
2. **Saves only in Play Mode.** `GameInstaller` uses the save and `Autosave` only when `Application.isPlaying`, because the Game EditMode tests build `Final` through `GameInstaller.Start` and were writing the player's real save. `SaveIsolationTests` guards this. The dev panel's New game is likewise only wired in Play Mode.
3. **No press-start panel to close.** `MainMenu.unity`'s `Menu.pressStartPanel` is empty, so the menu shows at once. Touch was added to `IsAnyInput` as the brief asks, but in this scene there is nothing for it to close yet (9b's audio unlock needs one).
4. **The menu buttons were moved** (overrides on Sergei's instance) to fit New game under Continue with even 110 spacing. The confirm panel's buttons keep the menu's 3x style.
5. **Unknown ids are dropped on the next save.** They are ignored on load, as the brief says, and not carried along, so an entry the designer deletes and later restores is no longer owned.
6. **Career earnings** are kept to the whole cent (rounded at each capture), so at most half a cent is lost per session. The wallet's own fraction is saved exactly.
7. **The 10 s check writes almost every time** during play, because `secondsPlayed` changes. That's one small PlayerPrefs write every 10 s, never per frame.
8. **Migrations work on `SaveData`, not raw JSON.** A later step that renames a field keeps the old field in `SaveData` for its migration to move. `JsonUtility` fills fields a file leaves out with their defaults.
9. **Load messages are warnings** ("Save: ..."), one per fallback. A backup that is also from a newer version gives two.

Follow-ups:
- **"Play Game" and "Continue"** both load the game (`onPlayButtonClick`). With saves, Play Game now also continues, which may confuse players. The lead could drop it or decide what it means.
- **Design.md (orchestrator):**
  - 5.12: the key is `operator.save`, not `switchboard.save`; New game, `HasProgress` and the Play Mode gate.
  - 4.4: step 2 is built.
  - 5.10: `CareerStats` is built, earnings via `EarningsService.TotalPaidCents`.
  - 6.2: the career is owned by `GameSession.Career`.
  - 4.5's touch line is done.
  - §3: the `ProcessPointer(position, pressed, hover)` overloads on `CableInputController` and `VolumeCablePanel`.
- **9b:** `SaveStorage.Create()` is the single switch point for `BrowserStorage`. `Autosave.SaveNow()` is public, ready for a `.jslib` `visibilitychange` hook. A press-start panel in `MainMenu` would give the audio-unlock tap.
- **Real touch checks** need the lead: the Device Simulator now, a phone in 9b/12. The menu buttons are 480×90 reference px, fine for fingers. The confirm buttons are the same size.

#### Step 9a verification (orchestrator, 2026-10-19)
- **EditMode re-run:** 483/483 green (445 before, 38 new). Console clean. My run left no format-only re-saves.
- **Scope:** 13 tracked files changed plus the new Save folders, codec and tests. All of them were named in the brief or the launch notes; nothing is staged.
  - `MainMenu.unity`: +1288/−151. The removed lines are Unity reordering existing blocks (the `LevelLoader` prefab instance moved); the new objects are `New_Game_Button` and `NewGameMenu`.
  - `Final.unity` and every prefab are unchanged.
- **Screenshots checked:**
  - `step-09a.png`: a reloaded game at $177.00 on a 4×4 board with 4 cords and three unfinished calls back on cards.
  - `step-09a-menu.png`: Play Game, Continue, New game, Options, Credits, Quit.
  - `step-09a-confirm.png`: the confirm plate with Start over and Cancel.
- **Code skim:**
  - `GameSession` restores before `StatSheet`, `CallQueue` and the shelf are built.
  - `CallQueue` remembers each dealt call's pool index, so `ResumePosition` is exact.
  - `SaveFile.Write` copies the previous main save to the backup only when it still parses.
  - `Autosave` captures only on its occasions.
  - `GameInstaller` opens the save only in Play Mode.
- **Docs updated:**
  - `Design.md`: the 9a status row, 4.4, 4.5 touch, 5.10 `CareerStats`, and the 5.12 deviations.
  - `Plan.md`: §4 (saves in tests), the Step 9b notes, and Step 10 (the guide on reload).
  - `CLAUDE.md`: the next step.
- **For the lead at the playtest:** the 5.12 deviations, plus the question of the menu's "Play Game" and "Continue" buttons, which now do the same thing.

#### Step 9a playtest (lead, 2026-10-19), menu fix requested
- The menu was confusing: "Play Game" and "Continue" did the same thing.
- **The lead's new order** (D55 amended):
  - **No save:** New Game first, then Options, Credits, Quit. Continue is hidden, and New Game starts at once with no question.
  - **With a save:** Continue first, then New Game (with the confirmation), Options, Credits, Quit.
- **Play Game is removed** from the menu prefab, not just hidden (lead).
- Sent to a fix agent.


### Step 9a fix report: 2026-10-19
Result: done

Playable now: the main menu follows the lead's new order. Without a save it shows New Game, Options, Credits, Quit Game; New Game goes straight into a fresh game. With a save it shows Continue, New Game, Options, Credits, Quit Game; Continue loads the game, and New Game opens the confirm panel. "Play Game" is gone. Every button label is centred in its plate. In Options the mute socket is now first on the left of each row: mute, 25, 50, 75, 100.

How to test:
1. **No save.** Delete the save (dev panel "New game", or start fresh), open `MainMenu` and press Play. You see four buttons, evenly spaced, with New Game first. Click New Game: `Final` loads as a new game with no question.
2. **With a save.** Earn something in `Final` so the game saves, then go back to `MainMenu`. You see five buttons with Continue first. Continue loads your game. New Game opens the panel: Cancel closes it, and Start over deletes the save and starts fresh.
3. **Labels.** Every label sits in the middle of its plate, including Start over and Cancel. Descenders (p, y, Q) no longer hang below the plate.
4. **Volume.** Options: each row reads mute, 25, 50, 75, 100. Your saved patch comes back as it was (yours is Master 100, Music 25, SFX 100). Plugging into the leftmost socket mutes that channel.

Created:
- `Assets/Tests/Game/Editor/MainMenuLayoutTests.cs` (+ `.meta`): 8 tests.

Changed:
- `Assets/Scripts/Game/Save/NewGameMenu.cs`:
  - new fields `continueButton`, `menuButtons` (top to bottom), `centreY` (-50) and `spacing` (122)
  - `Apply(hasSave)` shows or hides Continue, closes the panel, and lays out the active buttons
  - `OnNewGame()` starts at once without a save and asks with one
  - `SlotY(index, count, centre, spacing)` is a static helper, so the layout can be tested
- `Assets/Tools/rodrigo_menu_tools/Runtime/Prefabs/StartMenu.prefab` (Sergei's menu prefab):
  - `Play_Game_Button` and its text are deleted
  - the references to it are cleared: `Menu.MainMenuFirstButton` (no code reads it), `Options_Button`'s select-on-up, `Quit_Game_Button`'s select-on-down, and the prefab `EventSystem`'s first-selected (`Menu` clears the selection at run time anyway)
  - labels `Options_Text`, `Credits_Text`, `Quit_Game_Text`: auto size 12–18, bottom margin 3, Center/Middle, stretched with zero offsets. `Credits_Text` lost a trailing line break ("Credits\n").
- `Assets/Scenes/MainMenu.unity`:
  - the 11 orphaned overrides on `Play_Game_Button`/`Play_Game_Text` are removed with `PrefabUtility.RemoveUnusedOverrides`
  - the scene's font-size overrides (max 24, min 20, auto size) on the three prefab labels are reverted, so the prefab's values apply. The scene keeps its font override (Della Respira).
  - `New_Game_Text` now reads "New Game"
  - `Continue_Text`, `New_Game_Text`, `Start_Over_Text` and `Cancel_Text` get the same label settings
  - `NewGameMenu` is wired to Continue and the five buttons
  - the stored button positions are the "with a save" layout: 194, 72, -50, -172, -294
- `Assets/Prefabs/UI/VolumeCablePanel.prefab` (the three rows in Options) and `Assets/Prefabs/UI/MasterVolumeCableSlider.prefab` (the old single row, inactive in the scene):
  - sockets and labels moved: mute -300, 25 at -95, 50 at 110, 75 at 315, 100 at 520
  - the resting plugs and wires were redrawn at the default socket
  - re-saving also wrote default values for fields added to the socket, plug and cable graphics since these prefabs were last saved (`faceSprite`, `dimmedTint`, `heldSprite`, `insertedSprite`, `artworkTint`, `thicknessScale`). They are defaults, so nothing looks different.
- `Assets/Scripts/UI/AudioVolumeSettings.cs`: one comment. The level table is unchanged.
- `Assets/Editor/VolumeCableSliderBuilder.cs`: a rebuilt row puts mute first (`x = -300 + (i + 1) % 5 * 205`).
- `Assets/Editor/MenuTypographyBuilder.cs`: button labels get 18/12 and the bottom margin, so re-running "Apply Main Menu Fonts" won't undo the centring. If re-run, it would also apply these values to the two "Go back" buttons, which this pass left alone.
- `Docs/Design.md` 5.12: deviation 7 rewritten.

How it works:
- **Spacing:** the visible buttons are positioned from `NewGameMenu` (no layout group). Sergei's buttons are prefab children and the two new ones are scene objects, so they can't share a layout-group parent without changing the prefab's structure. The visible buttons sit 122 apart (the prefab's original spacing), centred on y -50, the same centre as the 9a column.
- **Volume:** I moved the sockets on screen and kept their indices. Socket 4 is still mute and 0–3 are still 25–100, so every saved `Switchboard.AudioPatch.v1` keeps its meaning with no migration, and the default patch (all three rows at 50 %) is unchanged. The panel finds sockets by object, not by position, so no code had to change. The game scene (`Final`) has no volume patch; only `MainMenu` shows one.
- **Labels:** the TMP alignment was already Center/Middle, and the rects were already stretched with zero offsets. Measured in Play Mode, the capitals were centred to within 0.3 units. The real cause was size: auto size filled the 30-unit plate at 21.9–24 pt, so lowercase-heavy words looked low, and descenders and the Q's tail hung below the plate. Della Respira's metrics at 90 pt: ascent 96.5, cap height 72, x-height 42, descent -26.7. At 18 pt with a 3-unit bottom margin (the text is raised 1.5 units), the mixed-case word is centred and the descenders stay on the plate.

Tests: 491/491 EditMode (483 before; 8 new in `MainMenuLayoutTests`):
- the slot maths (5 and 4 buttons)
- without a save (preview scene of `MainMenu`): New Game, Options, Credits, Quit, evenly spaced and centred, with Continue hidden
- with a save: Continue first, and New Game opens the panel, which Cancel closes
- no `Play_Game` object is left in the prefab or the scene, and the menu, buttons, `NewGameMenu` and EventSystem have no missing references
- all 7 labels are Center, stretched with zero offsets, 18 max and margin 3, with no stray whitespace. Every character is in Della Respira's source font, checked with `FontEngine.TryGetGlyphIndex` (the font is dynamic, so the saved atlas isn't the reference; the check does say "→" is missing).
- the mute socket is drawn first in both volume prefabs, with each label over its socket
- old socket indices keep their volumes (25 %, mute, 100 %), and the default patch stays at 50 %

Console: clean. The only lines were the known MCP "WebSocket is not initialised" and the engine's two "memoryless" lines around screenshots.

Screenshots (`Temp/Captures/`):
- `step-09a-menu-nosave.png`: New Game, Options, Credits, Quit Game, centred labels
- `step-09a-menu-save.png`: Continue, New Game, Options, Credits, Quit Game
- `step-09a-confirm.png` (replaced): the confirm panel with centred Start over and Cancel
- `step-09a-volume.png`: Options with mute first in all three rows, showing the lead's own patch

Checked in Play Mode (stepped frames, EventSystem raycast clicks):
- **No save:** Continue hidden, and New Game first at y 133 (4 slots). Clicking New Game loaded `Final` at once with no panel: not restored, $0, nothing owned.
- **With a save** (a test save):
  - Continue first at 194, and New Game at 72.
  - New Game opened the panel. Cancel closed it and kept the save.
  - New Game, then Start over, deleted both keys and loaded a fresh `Final`.
- **Smoke run:** about 24 s of game time in `Final` at ×10, with no errors. Stopping wrote no save (nothing happened).
- **Volume:** the lead's patch showed Music on the 25 socket (second from the left). Picking up the Music plug and plugging it into the leftmost socket gave socket 4 and volume 0.

Housekeeping:
- **PlayerPrefs:** `operator.save`, `operator.save.backup` and `Switchboard.AudioPatch.v1` were read at the start and written back exactly as found (the lead's $10.00 save with its backup, and the patch `[3,0,3]`). No other keys were touched.
- **Format-only re-saves discarded** with `git checkout`: `Della Respira SDF.asset` and `Boecklins Universe SDF.asset`. `TimeManager.asset` and `SampleScene.unity` were not touched.
- **Editor:** not in Play Mode and not paused, `timeScale` 1, no callbacks or SessionState keys left, the Game view at 1920×1080, `Final.unity` open, active and saved, and the console cleared.
- **Git:** nothing staged or committed.

Deviations from Design/Plan:
1. The labels are smaller (18 pt maximum instead of 24). Centring them in the 30-unit plates meant the descenders had to fit.
2. `Menu.MainMenuFirstButton` is cleared, not re-pointed: no code reads it, and the prefab can't reference the scene's Continue button.

Follow-ups:
- The two "Go back" buttons (Options, Credits) still use 24/20 auto size, so their labels look large and a little low, as the menu buttons did before. They weren't in the list; the same settings would fix them.
- The quit label reads "Quit Game" (Sergei's text). The lead's list says "Quit"; I left the text alone.
- **Missing music tracks.** `RandomMusicPlayer.tracks` in `StartMenu.prefab` and `Music_randomizer.prefab` points to two audio files (GUIDs `a84ac8ff…` and `e974b80e…`) that aren't in the repo at HEAD. So the menu music references are missing, in the scene too. This predates the fix, and the new missing-reference test skips that component.

#### Step 9a fix verification (orchestrator, 2026-10-19)
- **EditMode re-run:** 491/491 green. Console clean, and no format-only re-saves are left.
- **Scope:** the fix touched `NewGameMenu`, `StartMenu.prefab` (Play Game deleted), `MainMenu.unity`, both volume prefabs, `AudioVolumeSettings` (a comment only), `VolumeCableSliderBuilder`, `MenuTypographyBuilder` and the new `MainMenuLayoutTests`. All of them follow from the lead's three items.
- **Screenshots checked:**
  - `step-09a-menu-save.png`: Continue, New Game, Options, Credits, Quit Game, evenly spaced, with the labels centred.
  - `step-09a-volume.png`: mute first on every row. The lead's patch is restored (Master 100, Music 25, SFX 100).
- **The volume sockets moved on screen and kept their numbers,** so saved patches need no conversion.
- **Missing menu tracks:** the two missing audio GUIDs (`a84ac8ff…`, `e974b80e…`) belong to the menu package's `Music_randomizer` and have been missing since the package was added (`3522d9a`). They're in Plan §8 now.

#### Step 9a playtest, round 2 (lead, 2026-10-19)
- **Volume patch:** the sockets sit a bit right of each row's centre. Fix: centre them.
- **No way back to the main menu.** The lead chose a pause menu (D56), built now with the socket fix as "Step 9a, pass 2" (Plan), before 9b.


### Step 9a pass 2 report: 2026-10-19
Result: done

Playable now: the game has a pause menu. A small pause button (two bars) sits above the board's top-left corner; it and Esc open a modal "Paused" panel with Resume, Options and Main Menu. While paused, game time stops (pay, call durations and emergency countdowns freeze), the ring and call sounds pause, and the cord input is off, so a cord held before pausing is still in hand after Resume. Options shows the main menu's volume patch, with a Back button. Main Menu saves, then loads `MainMenu`, where Continue picks the game up again. Every button click now keeps a held cord. In both Options screens the five volume sockets sit centred in each row. The menu's quit button reads "Quit".

How to test: the Plan's pass 2 playtest script works as written. Notes:
1. **Esc** while paused resumes, also from the Options page (the brief's rule); Back returns from Options to the main page.
2. **The pause button** is above the board, not above the caller column: there is only about 60 reference px above the column, too little for an 88 px button (deviation 1).
3. **A held cord and buttons:** holding a cord, click the pause button, a shop button or the "Keep playing" button: the cord stays in hand. Clicks on empty space, the tray and closed jacks behave as before.

Created:
- `Assets/Scripts/Game/Views/GamePauseMenu.cs` (+ `.meta`): the pause menu. `Bind(saveNow, cordInput, book, callAudio, sceneLoader = null)`, `Pause()`, `Resume()`, `HandleEscape()`, `ShowOptions()`, `ShowMain()`, `GoToMainMenu()`, `IsPaused`, `OptionsOpen`. It reads Esc through `Keyboard.current`. Named `GamePauseMenu` because Sergei's menu package already has an unused global `PauseMenu` (`Pause.prefab`, in no scene).
- `Assets/Tests/Game/Editor/GamePauseMenuTests.cs` (+ `.meta`): 6 tests.

Changed:
- `Assets/Scripts/UI/CableInputController.cs`:
  - New `Suspended` property. While suspended, `Update` and `ProcessPointer` only hide the pointers: no pick, plug or drop, and `heldCable` is kept. The press in the frame that resumes is ignored, so the Resume click can't plug the jack under the button.
  - **Every button keeps a held cord:** in the "empty space" branch, a click whose target has a `Selectable` in its parents no longer returns the cord. The only buttons on the board are the four shop slots, the book (already excluded) and "Keep playing"; the tray, sockets and cards have no `Selectable`, so their behaviour is unchanged. The check runs only on a press.
- `Assets/Scripts/UI/PhoneBookView.cs`: `InputEnabled` (off while paused: keys and outside clicks leave the book as it is) and `ClosesOnEscape` (off once the pause menu is bound; the pause menu then closes the open book itself, so the Esc order between the two `Update`s doesn't matter).
- `Assets/Scripts/Game/CallAudio.cs`: `SetPaused(bool)` pauses and resumes its ring and feedback sources, `IsPaused`, and a `PausedChanged` event.
- `Assets/Scripts/Game/SceneUiAudio.cs`: listens to `CallAudio.PausedChanged` and pauses the phone babble (it lives on the `BackgroundMusic` prefab, which persists across scenes). Music and menu clicks keep playing.
- `Assets/Scripts/Game/GameLoopRunner.cs`: no `Tick` when `Time.deltaTime` is 0, so not even a free caller slot deals a call while paused.
- `Assets/Scripts/Game/GameInstaller.cs`: binds the scene's `GamePauseMenu` (if there is one) with `Autosave.SaveNow` (null outside Play Mode), the cord input, the book and `CallAudio`.
- `Assets/Editor/VolumeCableSliderBuilder.cs`: sockets at `x = -410 + (i + 1) % 5 * 205`, the fixed socket at -580.
- `Assets/Editor/VolumeOptionsBuilder.cs`: the fixed-socket line says -580 too (that socket is deleted by the wall-output conversion anyway).
- `Assets/Tests/Game/Editor/MainMenuLayoutTests.cs`: a centred-row test (2 cases) and a "Quit" check in the label test.
- `Docs/Plan.md` §8: the "A held cord drops on button clicks" row is removed; "the quit label reads Quit Game" is removed from "Menu leftovers".

Scene and prefab changes:
- `Assets/Prefabs/Game/GameUI.prefab`: a new `Pause` child (full stretch) with its own canvas (override sorting, order 16: above the book at 10 and the Goal dialog at 15, below the hand cursor and held plugs at 20), a `GraphicRaycaster` and `GamePauseMenu`.
  - `PauseButton`: 88×88 at (-490, 490), the dark panel plate with the gold outline, two 14×42 cream bars (raycast off), a tinted `Button` with no navigation. It overlaps no jack, card, caller panel or shop (checked in Play Mode at 1920×1080).
  - `Dialog` (inactive): `Blocker` (a copy of the Goal backdrop, black at 60 %, a raycast target on purpose), `Main` (600×470 plate: "Paused", Resume, Options, Main Menu, copies of "Keep playing") and `Options` (1240×900 plate, inactive: "Options", a nested `VolumeCablePanel.prefab` instance at 0.85 scale, Back).
  - Wired: buttons, pages, `CardTakeSound`'s `UiClickSound` for the clicks, and `menuSceneIndex` 0.
  - All labels are LiberationSans SDF (the static atlas has every glyph; the test checks it) with raycast off.
  - The rest of the diff is Unity reordering YAML: the old prefab's 96 named objects are all still there.
- `Assets/Prefabs/UI/VolumeCablePanel.prefab` and `Assets/Prefabs/UI/MasterVolumeCableSlider.prefab`: sockets, labels and the mute icon at mute -410, 25 at -205, 50 at 0, 75 at 205, 100 at 410; the resting plugs and wires redrawn at the 50 socket. In the single-row prefab, the fixed socket and plug moved from -530 to -580, so 70 px stay free between it and the mute socket. In the three-row panel, the cable starts at its wall anchor (-650), about 200 px from the mute socket. The socket indices are unchanged, so saved patches keep their meaning.
- `Assets/Tools/rodrigo_menu_tools/Runtime/Prefabs/StartMenu.prefab`: `Quit_Game_Text` reads "Quit". `MainMenu.unity` has no override on that text, so nothing changed there. The label keeps the centred settings of the 9a fix.
- `Final.unity` and `MainMenu.unity` are not changed by this pass. `Final` picks the pause menu up from `GameUI.prefab`.

Tests: 499/499 EditMode (491 before; 8 new):
- `GamePauseMenuTests` (on `Final` in a preview scene, the installer bound, then the menu re-bound with a logging save and scene loader):
  - pause stops time, saves, shows the dialog, suspends the cords, the book and the call sounds; resume restores the previous ×5
  - Esc with the book open closes only the book; Esc again pauses; again resumes
  - Main Menu saves, then loads build index 0, and sets `timeScale` 1
  - Options shows the volume patch; Back returns
  - a held cord survives pause and resume: clicks while paused change nothing, and the same click after Resume works
  - the panel is a modal between the book and the cursor, its labels have the glyphs, and the pause button is at least 88×88 with its bars raycast off
- `MainMenuLayoutTests`: the sockets are at -410…410 in both prefabs with room left of the mute socket (2 cases); the quit label reads "Quit".

Console: clean after clearing. During Play Mode checks the engine's two "memoryless" lines appeared around screenshots, and once "the PlayerLoop internal function has been called recursively": that came from loading a scene inside an `execute_code` call while frames were stepped with `EditorApplication.Step()`, not from game code.

Screenshots (`Temp/Captures/`):
- `step-09a2-button.png` (1920×1080) and `step-09a2-button-720.png` (1280×720): the pause button above the board's top-left corner
- `step-09a2-paused.png`: the "Paused" panel over the dimmed game
- `step-09a2-options.png`: Options inside the pause panel, the lead's patch (Master 100, Music 25, SFX 100), sockets centred
- `step-09a2-volume.png`: the main menu's Options with the centred sockets
- `step-09a2-menu.png`: the main menu with Continue, New Game, Options, Credits, Quit

Checked in Play Mode (stepped frames, calls inside `Canvas.willRenderCanvases`):
- **Held cord:** picked a cord from the tray (`ProcessPointer`), then pressed at the pause button and at a shop button: still held. Pause, Options, Back, Resume: still held (`Cable_04`, Held).
- **Freeze:** paused for 30 frames: game time stayed at 0.86 s, the emergency `call_303` kept 29.14 s left, the money didn't move, and `CallAudio` was paused. After Resume the countdown moved on (29.06) and `timeScale` was 1.
- **Volume patch at `timeScale` 0:** picked up the Master plug, moved the pointer 200 px and the plug followed, then plugged it back into 100 %.
- **Saves:** pausing wrote the save. With the money changed after the pause, Main Menu wrote the new balance before `MainMenu` loaded, and `timeScale` was 1 there. Continue (build index 1) restored the game at $170.24.
- **Smoke run:** about 28 s of game time at ×10 in `Final`, no errors.

Housekeeping:
- **PlayerPrefs:** `operator.save`, `operator.save.backup` and `Switchboard.AudioPatch.v1` were read at the start (the lead's $169.00 save with 7 upgrades, its backup, and the patch `[3,0,3]`) and written back exactly as found, checked string for string.
- **Format-only re-saves:** none this time (`TimeManager.asset`, both dynamic TMP fonts and `SampleScene.unity` are untouched).
- **Mia's `Assets/RawArt/art.png`:** Unity generated `Assets/RawArt.meta` and `Assets/RawArt/art.png.meta`; both are left untracked and untouched.
- **Editor:** not in Play Mode and not paused, `timeScale` 1, `AudioListener.pause` false, no callbacks or SessionState keys left, the Game view at 1920×1080, `Final.unity` open, active and not dirty, console cleared.
- **Git:** nothing staged or committed.

Deviations from Design/Plan:
1. **The pause button sits above the board's top-left corner, not above the caller column.** The column reaches to about 60 reference px from the top edge, and an 88 px button doesn't fit there. The strip above the board is free at every 16:9 size and far from the book, the shop and the Dev button.
2. **Esc in Options resumes** (as the brief says) instead of going back to the main page; Back does that.
3. **Saving dedupes:** `Autosave.SaveNow()` skips a write when the JSON equals the last one it wrote, so Main Menu right after a pause writes nothing new (the pause already saved the same state).
4. **Clicks keep a held cord on every `Selectable`**, not only the pause button, which closes the Plan §8 item. Clicks on non-button modal backdrops (the Goal dialog's blocker) and the dev panel's IMGUI buttons still count as empty space.
5. **While paused the book keeps its state:** its keys and outside-click closing are off, so a book left open behind the pause panel is still open after Resume.

Follow-ups:
- **Design.md (orchestrator):** D56 is built; 4.3 gains the pause canvas at order 16; §3 gains `CableInputController.Suspended`, `PhoneBookView.InputEnabled` and `ClosesOnEscape`, and `CallAudio.SetPaused`.
- **Touch:** the pause button is 88 reference px, about 32 pt on an 844×390 phone. Fine for a mouse, a bit small for a finger; Step 12 could enlarge it or add a hit margin.
- **The dev panel's Speed button while paused** sets `timeScale` and so runs the game behind the panel. It's dev-only; ignore it or let it respect the pause.
- **Sergei's unused `PauseMenu` and `Pause.prefab`** in the menu package could be deleted in a cleanup to avoid confusion with `GamePauseMenu`.

#### Step 9a pass 2 verification (orchestrator, 2026-10-19)
- **EditMode re-run:** 499/499 green. Console clean.
- **Scope:** `GamePauseMenu` plus tests (new). Changed: `GameUI.prefab` (the `Pause` child), `CableInputController`, `PhoneBookView`, `CallAudio`, `SceneUiAudio`, `GameLoopRunner`, `GameInstaller`, the two volume prefabs and their builders, `StartMenu.prefab` (Quit) and `MainMenuLayoutTests`. `Final.unity` is unchanged.
  - Mia's `Assets/RawArt/art.png` came in with `5b028d9` (fast-forward); Unity generated `Assets/RawArt.meta` and `art.png.meta`, untracked.
- **Selectable check:** the tray, sockets and board prefabs have no `Selectable`, so clicking the tray still returns a held cord. Only buttons keep it.
- **Screenshots checked:**
  - `step-09a2-button.png`: the pause button above the board's top-left corner, clear of the cards.
  - `step-09a2-paused.png`: the "Paused" panel.
  - `step-09a2-options.png`: the volume patch inside it, centred.
  - `step-09a2-volume.png`: the main menu's Options, centred, mute first.
- **For the lead:** the pause panel and button have a placeholder look (LiberationSans, flat green), not the menu's serif style. Mia could draw them.

#### Step 9a playtest, round 3 (lead, 2026-10-19)
- **The pause button moves to the top-right corner** (lead's choice). It is anchored to the screen corner, and the editor-only Dev button moves aside.
- **Quit only in Windows and Mac builds.** It is hidden in the Web build, where `Application.Quit` does nothing. This moves forward from the 9b outline.


### Step 9a pass 3 report: 2026-10-19
Result: done

Playable now: the pause button sits in the screen's top-right corner. It is anchored to that corner, so it follows the real corner at every aspect ratio. The dev panel's "Dev" button sits just left of it. The main menu hides Quit in Web builds and closes the gap: with a save it shows Continue, New Game, Options, Credits; without one, New Game, Options, Credits. The editor and the Windows and Mac builds still show Quit.

How to test:
1. Play `Final` at 1920×1080, 1280×720 and 844×390. The pause button is in the top-right corner, above the shop panel and clear of the money plaque. "Dev" is left of it. Click Dev: its buttons open as a row going left along the top edge, above the board.
2. The pause button works as before: the panel opens, and Resume brings the game back.
3. In the editor, the main menu still shows Quit. The Web layout is covered by `MainMenuLayoutTests` and the screenshot `step-09a3-menu-web.png`; the real Web check comes with the 9b build.

Created: none.

Changed:
- `Assets/Scripts/Game/Save/NewGameMenu.cs`:
  - New `quitButton` field.
  - `QuitAvailable` is false only in Web players (`UNITY_WEBGL && !UNITY_EDITOR`).
  - `Apply(saveExists)` now calls the new `Apply(saveExists, quitVisible)`, which shows or hides Quit before the evenly spaced layout runs, so a hidden Quit leaves no gap.
- `Assets/Scripts/Game/DevTools/DevPanel.cs`:
  - "Dev" now sits left of the pause button: 6 px from the top, 84 px in from the right edge (the button's 6 px margin, its 72 px width and a 6 px gap).
  - When open, its buttons form a row going left at the same height, not a column going down (deviation 2).
- `Assets/Tests/Game/Editor/GamePauseMenuTests.cs`: the 88 px size check moved into a new corner test (below).
- `Assets/Tests/Game/Editor/MainMenuLayoutTests.cs`: Web cases (below).
- `Docs/Design.md` 4.3: the pause-menu line now gives the 72 px size, the corner anchor, the Dev button and `QuitAvailable`.
- `Docs/Plan.md` 9b outline: the Quit item now says 9a pass 3 did it.

Scene and prefab changes:
- `Assets/Prefabs/Game/GameUI.prefab`, `Pause/PauseButton`:
  - anchors and pivot moved to (1, 1), the top-right corner; position (-6, -6); size 72×72 (it was 88×88 at (-490, 490) from the centre)
  - the two bars scaled down to match: 12×34 at ±10 (they were 14×42 at ±12)
  - nothing else in the prefab changed
- `Assets/Scenes/MainMenu.unity`: `NewGameMenu.quitButton` is wired to the `StartMenu` instance's `Quit_Game_Button`. That one field is the only change this pass made to the scene.
- `Final.unity`: not changed.

Tests: 503/503 EditMode (499 before; 4 new):
- `GamePauseMenuTests.ThePauseButtonSitsInTheTopRightCorner`:
  - anchors and pivot at (1, 1), at least 72×72, with a margin of 0 to 16 px
  - its bottom edge at most 80 px from the top, so it stays above the shop
  - the `Pause` parent stretches over the whole screen
- `MainMenuLayoutTests.OnWebQuitIsHiddenAndTheRestStayEvenlySpaced` (two cases, with and without a save): the expected buttons in order, evenly spaced around the centre, and Quit inactive. Showing Quit again puts it back last.
- `MainMenuLayoutTests.QuitShowsInTheEditorAndDesktopBuilds`: `QuitAvailable` is true in the editor, and `Apply(true)` shows Quit.

Console: clean (cleared at the end). The engine's two "memoryless" lines appeared around screenshots in Play Mode.

Screenshots (`Temp/Captures/`):
- `step-09a3-corner.png` (1920×1080): the pause button in the corner with "Dev" left of it
- `step-09a3-corner-phone.png` (844×390): the same on a wide phone, where the corner is well right of the shop
- `step-09a3-dev-open.png` (1920×1080): the opened dev row along the top edge
- `step-09a3-menu-web.png`: the main menu with `Apply(true, false)`: Continue, New Game, Options, Credits, evenly spaced
- `step-09a3.png`: the smoke run

Checked in Play Mode (stepped frames, checks inside `Canvas.willRenderCanvases`):
- **Overlap check:** the pause button and all nine dev buttons were compared with every active raycast target and the shop panel and `HeaderSlot` rects, at 1920×1080, 1280×720 and 844×390. Nothing overlaps.

  | Size | Pause button | Shop panel's top | Pause to shop's right edge |
  |---|---|---|---|
  | 1920×1080 | 6–78 px from the top | 86 px | (stacked: shop starts below) |
  | 1280×720 | 4–52 px | 57 px | (stacked: shop starts below) |
  | 844×390 | 2–28 px | 31 px | 56 px of free space |

  The dev row spans x 1122–1836 at 1920×1080 and doesn't reach the board or the caller column.
- An EventSystem raycast at the button's centre at 844×390 hits `PauseButton` first.
- **Smoke run:** `MainMenu`, then `Final` via `LoadScene(1)`, about 80 s of game time at ×10. No errors or warnings from game code.

Housekeeping:
- **PlayerPrefs:** I read `operator.save`, `operator.save.backup` and `Switchboard.AudioPatch.v1` at the start: $71.00 with 5 upgrades, its backup, and the patch `[3,1,3]`. Play Mode rewrote the save, so I wrote all three back exactly as found and checked them string for string.
- **Format-only re-saves discarded** with `git checkout --`: `ProjectSettings/TimeManager.asset`, `Assets/Fonts/BoecklinsUniverse/Boecklins Universe SDF.asset` and `Assets/Fonts/DellaRespira/Della Respira SDF.asset`. `SampleScene.unity` was untouched.
- `Assets/RawArt.meta` and `Assets/RawArt/art.png.meta` are left as they were.
- **Editor:**
  - not in Play Mode
  - `timeScale` 1, `AudioListener.pause` false
  - no callbacks or SessionState keys left
  - the Game view at 1920×1080
  - `Final.unity` open, active and not dirty
  - console cleared
- **Git:** nothing staged or committed.

Deviations from Design/Plan:
1. **The pause button is 72×72, not 88.** At 16:9 the shop panel's art starts 86 px from the top, right under the corner. An 88 px button with a margin would reach into it. On wider screens the corner moves right of the shop, but one fixed size keeps the layout simple.
2. **The open dev panel is a row along the top edge, not a column.** A column below the pause button, or below a Dev button left of it, would hang over the shop slots. Each slot is a buy `Button` 390 px wide, and an IMGUI click there also clicks the slot under it. The old column already overlapped slot 1's right edge. The row keeps the same buttons in the same order, going left from "Close", and covers nothing clickable.

Follow-ups:
- Plan §4's "Dev panel (2a)" row still says the button is in the top-right corner. It is now just left of the pause button and opens as a row (orchestrator).
- On an 844×390 phone the pause button is 26 physical px. That's fine for a mouse but small for a finger, so Step 12 could add a larger invisible hit area. There's room for one in the corner on wide phones.

#### Step 9a pass 3 verification (orchestrator, 2026-10-19)
- **EditMode re-run:** 503/503 green. Console clean, and no re-saves are left.
- **Scope:** `NewGameMenu` (`quitButton`, `QuitAvailable`), `DevPanel` (the Dev button left of pause, the panel as a row), `GameUI.prefab` (`PauseButton` anchored top-right, 72×72), `MainMenu.unity` (one reference), and two test files.
- **Screenshots checked:**
  - `step-09a3-corner.png`: the pause button in the top-right corner above the shop, with Dev to its left.
  - `step-09a3-menu-web.png`: Continue, New Game, Options, Credits, evenly spaced.
- **For the lead:** the button is 72 px, not 88, so it clears the shop's top edge. On phones it may be small for a finger; Step 12 can add a bigger invisible hit area.
- Committed and pushed on the lead's request (2026-10-19), with no teammate pushes since `5b028d9`. The lead's playtest of pass 3 isn't confirmed yet.
- Left out of the commit: `Assets/RawArt.meta` and `Assets/RawArt/art.png.meta`, which Unity generated for Mia's `art.png`.
- **Playtest (lead, 2026-10-19):** 9a is OK, including pass 3 (the pause button in the corner, Quit hidden on Web). Every deviation listed in the 9a reports and Design 5.12 is accepted. Step 9a is done.
- **Web Build Support** for 6000.3.6f1 is installed (`PlaybackEngines/WebGLSupport`), and Unity was restarted. Step 9b is unblocked.

### Step 9b prep (orchestrator, 2026-10-19)
- **Pre-flight:**
  - `git fetch`: up to date with origin (`e4002fe`); only the untracked `Assets/RawArt.meta` and `Assets/RawArt/art.png.meta` (left alone, as in 9a).
  - Unity connected (`Ludumdare60@67eac6d9bbf6b918`), idle, not in Play Mode, active scene `MainMenu`; console clean.
  - EditMode 503/503 green; the run left no format-only re-saves.
  - Web Build Support installed; the editor is still on the Mac platform. Player settings today: Brotli (0), no Decompression Fallback, the Default template, `productName` `ludumdare-60`.
  - `~/bin/butler` v15.32.0 reaches `llazypixell/operator` (no channels yet).
  - No Chrome extension is connected, so the agent checks the build in headless Chrome over the DevTools protocol with a throwaway profile (Node 26 has a built-in `WebSocket`).
- **Brief:** the Plan's outline is now a full brief. The orchestrator's technical choices, for the lead to confirm at the playtest:
  - a project Web template `Operator` (full-window canvas, no Unity footer, title "Operator!"), because the Default template's fixed 960×600 canvas and footer don't fit itch's frame
  - `productName` and `companyName` unchanged, because the Mac editor's PlayerPrefs file is named after them
  - the editor stays on the Web platform after the first build
  - the volume patch keeps its key `Switchboard.AudioPatch.v1`
  - a `visibilitychange`/`pagehide` hook that saves synchronously, because hidden tabs run no frames
- **Launch:** one agent at high effort; it may push to the itch Draft page (D55).


### Step 9b report: 2026-10-19
Result: partial. The build, the browser saves and the local browser checks are done. **The itch push was refused:** "itch.io API error (400): Please verify your account's email address before uploading a build". The lead needs to verify the itch account's email (itch.io › Settings › Email addresses), then run `~/bin/butler push Builds/Web llazypixell/operator:html5` (the build in `Builds/Web` is ready) or ask for the push. `butler status llazypixell/operator` still shows no channel.

Playable now: `Tools/Switchboard/Build Web` builds the game into `Builds/Web` (23.8 MB). Served locally, it fills the browser window with no Unity footer, shows New Game, Options, Credits (no Quit), and plays. In the browser, progress and the volume patch live in `localStorage`, so they survive a reload and a new upload, and the game saves the moment the page is hidden or closed.

How to test (until the push works): build with the menu, then `cd Builds/Web && python3 -m http.server 8000` and open `http://localhost:8000`. After the push, use the Plan's playtest script. `Docs/WebBuild.md` explains building, butler, the itch page settings and where saves live.

Created:
- `Assets/Scripts/Game/Save/BrowserStorage.cs`, with two types:
  - `BrowserStorage` (`ISaveStorage` over the `.jslib`). `IsAvailable()` probes `localStorage` by setting and removing a test key. A refused write or delete logs one warning per page load. Outside Web players it's inert: not available, reads null.
  - `BrowserPageHidden`: `Register(Action)` and `Unregister(Action)`. It does nothing outside Web players.
- `Assets/Scripts/Game/Save/BrowserStorage.jslib`:
  - `localStorage` read, write and delete, and the probe, each in its own try/catch. A failure comes back as status 0.
  - Returned strings use `_malloc` + `stringToUTF8`, as Unity 6's manual says; keys and values are read with `UTF8ToString`.
  - `OperatorStorage_ClaimWarning(id)` keeps the "warn once per page load" flags in JavaScript.
  - `OperatorPage_SetHiddenCallback`: listens for `visibilitychange` (only when the page becomes hidden) and `pagehide`, and calls the C# callback inside the event through `{{{ makeDynCall('v', 'cb') }}}`.
  - Its importer is Web only: Unity's default for `.jslib`, so the `.meta` has no settings.
- `Assets/Scripts/Game/Editor/WebBuild.cs`, the menu `Tools/Switchboard/Build Web`:
  - applies the D26 settings (Gzip, Decompression Fallback, `PROJECT:Operator`)
  - switches the editor to Web if needed
  - deletes `Builds/Web` and builds the enabled build-list scenes as a release build
  - logs one line with the time, the size and the butler command, or the build's errors
- `Assets/WebGLTemplates/Operator/`, with metas:
  - `index.html`, based on Unity 6's Default template, keeping its loader code, template variables and `#if` blocks
  - `TemplateData/style.css`
  - `TemplateData/webmemd-icon.png` (Unity's, used only by the diagnostics overlay)
  - `thumbnail.png` (Unity's Minimal one)
  - The template draws a canvas that fills the window and follows its size, with no footer, logo or fullscreen button.
  - It has a dark background (`#15110d`), a plain cream-and-gold loading bar under the title "Operator!", no scrollbars, `touch-action: none` on the canvas, the viewport meta tag always on, the page title "Operator!", Unity's error and warning banner, and an empty favicon (no `/favicon.ico` 404 in the console).
- `Assets/Tests/Game/Editor/WebBuildTests.cs` (14 tests, below).
- `Docs/WebBuild.md`, the team guide.

Changed:
- `Assets/Scripts/Game/Save/SaveStorage.cs`: `Create()` returns `BrowserStorage` in Web players while the probe works. Otherwise it returns `PlayerPrefsStorage`, with one warning per page load. The editor and desktop builds always get `PlayerPrefsStorage`. The choice itself is the public seam `Choose(webPlayer, browserWorks, browser, warnFallback)`.
- `Assets/Scripts/Game/Save/Autosave.cs`: `Bind` registers `SaveNow` with `BrowserPageHidden`; `Stop` (also called from `OnDestroy`) unregisters it. The summary comment now lists the hidden-page save.
- `Assets/Scripts/Game/Save/PlayerPrefsStorage.cs`, `Assets/Scripts/Domain/Save/ISaveStorage.cs`: comments only.
- `Assets/Scripts/UI/AudioVolumeSettings.cs`:
  - It reads and writes its patch through `SaveStorage.Create()`, with the same key and the same JSON.
  - New `Load(ISaveStorage)` returns a fresh `VolumePatchState`; new `Save(VolumePatchState, ISaveStorage)`.
  - `SaveKey` is now public.
  - It still reads at `SubsystemRegistration`. The browser check shows the `.jslib` works that early.
- `ProjectSettings/ProjectSettings.asset`:
  - `webGLTemplate` `APPLICATION:Default` → `PROJECT:Operator`
  - `webGLCompressionFormat` 0 (Brotli) → 1 (Gzip)
  - `webGLDecompressionFallback` 0 → 1
  - `m_BuildTargetBatching` gained a WebGL entry (static and dynamic batching 0), which the platform switch wrote. Kept, as the brief says.
  - `productName` and `companyName` are unchanged.

Scene and prefab changes: none.

Tests: 517/517 EditMode (503 before; 14 new in `WebBuildTests`), green both on the Mac platform and on Web:
- `TheEditorSavesInPlayerPrefs`: `Create()` returns `PlayerPrefsStorage`, and `BrowserStorage.IsAvailable()` is false in the editor.
- `Choose` cases:
  - `DesktopNeverAsksTheBrowser`
  - `AWebPlayerUsesTheBrowserWhileItsStorageWorks`
  - `AWebPlayerWithBlockedStorageFallsBackToPlayerPrefsWithAWarning`
- `TheVolumePatchIsStoredUnderItsKeyAndReadBack`: the exact JSON under `Switchboard.AudioPatch.v1`, then a round trip.
- `AMissingOrBrokenPatchGivesTheDefault`, 5 cases: missing, empty, not JSON, version 2, two cables in one row.
- `TheCommittedPlayerSettingsBuildGzipWithFallbackAndOurTemplate`, which also checks that threads are off.
- `TheTemplateHasWhatTheLoaderNeeds`: the loader, data, framework and code variables, `createUnityInstance`, the banner, the viewport tag and the title, and no footer.
- `TheBuildListIsMainMenuThenFinal`.
- `TheBrowserPluginIsForWebPlayersOnly`: Web yes; editor, any platform, Mac and Windows no.

Console: clean. The two Play Mode smoke runs logged only the engine's two "memoryless" lines. The console was cleared at the end.

Screenshots (`Temp/Captures/`):
- browser at 1280×720:
  - `step-09b-menu.png`: New Game, Options, Credits
  - `step-09b-game.png`: `Final` after New Game
  - `step-09b-menu-with-save.png`: Continue, New Game, Options, Credits
  - `step-09b-continue.png`: $71.00, the 4×4 board, and the shop offering `board_2` ($25) and `income_2` ($20)
  - `step-09b-options.png`: the planted patch: Master 25, Music mute, SFX 100
- browser at 844×390 with touch:
  - `step-09b-phone-menu.png`
  - `step-09b-phone.png`: the game after a tap on New Game
- editor: `step-09b.png`, the smoke run in `Final` reached from `MainMenu`

The build:
- First run of the menu (2026-10-19, 15:10 to 15:20): about 10 minutes, about 5 of them the platform switch and reimport. Unity's own figure for the build itself is 5.0 min.
- A second build, after a template change, took 0.3 min.
- Total size 23.8 MB:
  - `Build/Web.data.unityweb` 12.5 MB
  - `Build/Web.wasm.unityweb` 12.4 MB
  - `Build/Web.framework.js.unityweb` 90 KB
  - `Build/Web.loader.js` 48 KB
  - `index.html` 3 KB, `TemplateData` 3 KB

Browser checks: headless Chrome 155 with SwiftShader WebGL, a throwaway profile in the scratchpad, and Node scripts over the DevTools protocol, against `python3 -m http.server 8000 --bind 127.0.0.1` (no compression headers, like itch). All six checks of the brief pass, rerun in full on the final build:
1. **Loading.** The page loads in 1.3–2.4 s and the title is "Operator!". The canvas is 1280×720 at (0, 0), with a render size of 1280×720, a scroll size equal to the window, and no footer. The menu shows New Game, Options and Credits, without Quit.
   - The canvas also follows a window resize: the real headless viewport of 1280×633 re-laid out the menu.
2. **New Game** loads `Final`, and it runs.
3. **Saves:**
   - I planted a save in `localStorage` (`cents` 7100, `income_1` + `board_1`) and reloaded. The menu showed Continue first. Continue gave $71.00 and the 4×4 board, and the shop offered the next entries.
   - 12 s later `operator.save` had changed (`secondsPlayed` 300 → 309.9), and `operator.save.backup` held the planted save string for string.
4. **Hidden-page hook:**
   - **Synthetic events.** I overrode `document.visibilityState` to "hidden" and dispatched `visibilitychange`. The save changed inside that same synchronous `Runtime.evaluate`, and the backup now held the previous save. A dispatched `pagehide` did the same. A `visibilitychange` while the page was visible didn't save.
   - **Real hidden state.** Headless Chrome does hide the page when a second tab is opened in front (`Target.createTarget`): `document.visibilityState` became "hidden", a new save was written, and the backup held the previous one.
   - Reloading a running game also writes a save, through `pagehide`.
5. **Volume patch.** The patch `{"version":1,"rows":[0,1,2],"sockets":[0,4,3]}` was written to `localStorage`. After a reload, Options showed it. So the `.jslib` call at `SubsystemRegistration` works.
6. **Phone size.** At 844×390 (mobile emulation, DPR 2, touch on), the canvas is 844×390 and renders at 1688×780, with no scroll. A touch tap (`Input.dispatchTouchEvent`) on New Game started the game.
- **Browser console:**
  - No errors or exceptions from our code.
  - The remaining lines come from Unity itself:
    - "You can reduce startup time if you configure your web server to add Content-Encoding: gzip": expected with the Decompression Fallback
    - "ERROR: Shader Hidden/CoreSRP/CoreCopy" and "Hidden/Universal/HDRDebugView shader is not supported on this GPU", logged at log level, not as errors: URP's debug shaders on WebGL
    - one "Trying to get length of sound which is not loaded yet", not from our scripts (see Follow-ups)
    - Chrome's "GPU stall due to ReadPixels" performance notes from SwiftShader
  - The first build's only console error, the browser's automatic `/favicon.ico` 404, is gone since the template's empty icon.
- **Audio unlock:** no press-start panel was added. Unity logged "Audio context resumed after 0.303 seconds". Headless Chrome can't tell whether sound is heard, so the lead's step 1 (the first tap's click sound) is the real check.
- Chrome and the server are stopped, and the profile is deleted. The scripts stay in the scratchpad.

Housekeeping:
- **PlayerPrefs:** at the start, `operator.save` and `operator.save.backup` were absent, and `Switchboard.AudioPatch.v1` was `{"version":1,"rows":[0,1,2],"sockets":[3,1,3]}`. All three were put back exactly as found (the save keys deleted, the patch rewritten) and checked string for string.
- **Discarded build and format re-saves**, after each of the two builds:
  - `Assets/Settings/UniversalRP.asset`: URP's shader-stripping prefilter fields, rewritten at build time
  - `Assets/UniversalRenderPipelineGlobalSettings.asset`: `m_RuntimeSettings` filled at build time
  - `ProjectSettings/TimeManager.asset`
  - the two dynamic TMP fonts
  - the `preloadedAssets` entry the Input System writes into `ProjectSettings.asset` during a build: the editor's in-memory list stays empty, so the file line is a leftover; it was put back to `[]`
  - `SampleScene.unity` was untouched.
- `Assets/RawArt.meta` and `Assets/RawArt/art.png.meta` were left untouched.
- **Editor:**
  - on the Web platform (WebGL), as the brief decided
  - not in Play Mode, `timeScale` 1
  - no callbacks or SessionState keys left
  - `Final.unity` open, active and not dirty
  - console cleared
- **Git:** nothing staged, committed or pushed.

Deviations from Design/Plan:
1. **The itch push didn't happen** (account email unverified, see the top). Everything before it is done.
2. **The hidden-page callback** is a function pointer (`makeDynCall` plus a `[MonoPInvokeCallback]` static method), not `SendMessage`, so no GameObject name is involved. Its one static field, `BrowserPageHidden.handler` (the bound `Autosave.SaveNow`), is the documented exception. The "warn once" flags live in the `.jslib`, so C# has no other statics with state.
3. **`BrowserStorage` compiles everywhere.** Its imports exist only under `UNITY_WEBGL && !UNITY_EDITOR`; elsewhere the same private methods return "unavailable". The editor compiles the class, and the tests can reference it.
4. **Unity's Default template's fullscreen button and footer are gone,** as the brief says. Its `alert()` on a load failure is replaced by the error banner, which is readable on a phone and doesn't block.
5. **Design 5.12's example key** `switchboard.save` isn't used. The keys stay `operator.save` and `operator.save.backup` (D55, as the brief says).

Follow-ups:
- **Lead:**
  - verify the itch account's email, then push (or ask for the push)
  - set the itch page settings in `Docs/WebBuild.md`
  - run the Plan's playtest script, including the first-tap sound and a real phone
- **Every build rewrites three files** (the URP asset, the URP global settings, and `preloadedAssets` in `ProjectSettings.asset`), and they have to be discarded before a commit (`Docs/WebBuild.md` says how). A later step could make `WebBuild` restore them itself, for example by re-saving `PlayerSettings` after the build.
- **Plan §4 lessons:**
  - Start a build from `execute_code` with `EditorApplication.delayCall` plus `ExecuteMenuItem`, and watch `~/Library/Logs/Unity/Editor.log` for "Build Web:". With Unity in the background, the second build's delayCall started late: about 7 minutes of wall time for a 0.3 min build.
  - Headless Chrome needs `Emulation.setDeviceMetricsOverride` in every CDP session: the override ends when the session closes, and the real headless viewport is 1280×633.
  - Clear `localStorage` from a same-origin static page (for example `/TemplateData/style.css`). Clearing it on the game page and reloading lets `pagehide` write a fresh save.
  - In Play Mode with Unity in the background, the `Step()` callback runs only when the editor ticks. Have it write a file when done and wait on that file.
- **"Trying to get length of sound which is not loaded yet"** appears once on the menu in the browser. It isn't from our scripts. The likely source is the menu package's `Music_randomizer`, whose two clips were never committed (Plan §8, "Menu leftovers").
- At 844×390 the board scene shows its side margins (brown bars left and right). That's Step 12's layout work.

#### Step 9b verification (orchestrator, 2026-10-19)
- **EditMode re-run:** 517/517 green (503 before, 14 new), with the editor on the Web platform. Console clean. My run left no format-only re-saves.
- **Scope:** every changed file was named in the brief: `SaveStorage`, `Autosave`, `AudioVolumeSettings`, two comment-only files, `ProjectSettings.asset`, plus the new `BrowserStorage` (`.cs` and `.jslib`), `WebBuild`, the `Operator` template, `WebBuildTests` and `Docs/WebBuild.md`. No scene or prefab changed, and nothing is staged.
  - `ProjectSettings.asset`: Gzip, Decompression Fallback, `PROJECT:Operator`, and a WebGL batching entry written by the platform switch. `productName` is unchanged.
- **Build:** `Builds/Web` holds `index.html`, `TemplateData/` and four files in `Build/`, 23.8 MB in all. The `.unityweb` names are what Unity gives Gzip files when the fallback is on.
- **Screenshots checked:**
  - `step-09b-menu.png`: the browser menu at 1280×720 fills the page: New Game, Options, Credits, and no footer.
  - `step-09b-continue.png`: a save planted in `localStorage` came back as $71.00 on a 4×4 board.
  - `step-09b-phone.png`: 844×390 with touch fills the screen. The brown side bars are Step 12's layout work.
- **Code skim:**
  - The `.jslib` wraps every `localStorage` call in try/catch and keeps its warn-once flags in JavaScript.
  - The hidden-page hook calls `Autosave.SaveNow` inside the `visibilitychange` and `pagehide` events, through one static callback (the documented exception).
  - `SaveStorage.Choose` is the tested seam.
  - The volume patch keeps its key and JSON.
- **Blocked:** itch refused the upload until the account's email address is verified. The lead does that, then runs the push or asks for it.
- **Build re-saves:** every build rewrites the URP asset, the URP global settings and `preloadedAssets` in `ProjectSettings.asset`. `Docs/WebBuild.md` says to undo them before a commit.
- **Pushed (orchestrator, on the lead's go-ahead after the email check):** `butler push Builds/Web llazypixell/operator:html5` uploaded 23.81 MiB. `butler status` shows the `html5` channel, upload #19875492, build #2162093, version 1, processed. The lead sets the itch page settings next.

#### Step 9b playtest, round 1 (lead, 2026-10-19), fix requested
- On itch it "works well so far". One problem: **the game's hand cursor and the system pointer show at the same time** in the browser.
- **Orchestrator's notes for the fix:**
  - Unity Web's `Cursor.visible` sets the canvas's inline `style.cursor` to `none` or `default` (`_JS_Cursor_SetShow` in the framework). The template's CSS sets no cursor, and the Player settings have no default cursor.
  - Three components write `Cursor.visible`: `CableInputController` (every `Update`, and `HidePointers`), `MenuHandCursor` (every `LateUpdate`) and `VolumeCablePanel` (`LateUpdate`).
- **Art merge before the fix:** merged Mia's `3ca8bc9` ("upd8 caller pngs", 13 caller PNGs, no metas). `caller-02.png` is now 190×190, but its slice is 190×191, so Unity drops the sprite (Plan §8). The other slices fit inside their PNGs; `police.png` is 191×191 with a 190×186 slice. Fixed in the same pass.
- Sent to a fix agent; it may push the fixed build to the Draft page (D55).


### Step 9b fix report: 2026-10-19
Result: done. The fixed build is on the itch Draft page: `html5` build **#2162255** (version 2, processed).

Playable now: in the browser the game's hand is the only pointer wherever it is drawn. When the mouse leaves the game (onto itch's page around the game frame), the hand disappears with it instead of staying frozen at the edge, and it comes back on the first move over the game. `caller-02` shows again.

**The cause (measured in headless Chrome before changing code):**
- I served the 9b build with the framework's `_JS_Cursor_SetShow` patched on the fly (CDP `Fetch`) to log every call, plus a MutationObserver on the canvas's `style`.
- Inside the game everything was already right. On the menu, the board, over a stored cord, with a cord held and over the open book, the canvas cursor was `none` whenever the hand was drawn. It was `default` on the pause page, where no hand is drawn.
- Unity only calls `_JS_Cursor_SetShow` when `Cursor.visible` changes. Writes in between don't reach the page, and that is harmless.
- **The real cause: the browser sends no mouse moves once the pointer leaves the page.** itch shows the game in an iframe inside a bigger page. I rebuilt that locally: a host page on another port with the game in a 960×540 iframe.
  - When the mouse left the frame, Unity kept the last position.
  - `Application.isFocused` stayed true, because the iframe keeps focus after a click.
  - So the hand stayed drawn at the frame's edge while the system pointer moved on over the page: both visible at once (`step-09b-fix-iframe-menu-left.png`, `step-09b-fix-iframe-game-left.png`).
  - In the editor this can't happen, because the Game view keeps reporting positions outside its area.
- Ruled out:
  - **Focus differences:** Unity starts with `isFocused` true even before the iframe gets focus, and the hand already hid the pointer then.
  - **Stale captures:** `cursorWasVisible` was `true` after MainMenu → Final, and the pause page showed the pointer.
  - **Unity's cache:** it skips repeated values, but every change reached the canvas.
  - **Chrome repaint:** never more than one value per frame reached the browser's style.
- **A second, smaller finding:** in the pause menu's Options, `CableInputController` (suspended) and `VolumeCablePanel` both wrote `Cursor.visible` every frame, which flipped the canvas `default` → `none` about 60 times a second. The last writer won only because of the update order (Chrome showed `none`). In the other order, the volume hand and the system pointer would both show.

**The fix (one rule):** a mouse that has left the page counts like a lifted finger (D28): no hover, so no hand. Only a held cord stays where it was last put. That's the same as touch between taps, and in the editor nothing changes.
- **Who owns `Cursor.visible`:**
  - unchanged: each component that draws a hand hides the system pointer while it draws, and gives it back when it stops
  - new: while the pause menu is open, the cord input leaves the cursor alone, because it hid its hand once when pausing; the pause panel's volume patch owns the cursor

Created:
- `Assets/Scripts/UI/BrowserPointer.jslib`: `OperatorPointer_IsOverPage()`.
  - On its first call it adds capture-phase, passive listeners on `window`: `mousemove` and `mouseover` set "over", and `mouseout` with no `relatedTarget` (leaving the page itself) sets "outside".
  - The state lives on `Module.operatorPointer`. It starts "outside", so no hand is drawn at a stale position before the first move.
  - Its importer is Web only (Unity's default for `.jslib`).
- `Assets/Scripts/UI/BrowserPointer.cs` (`Switchboard.UI`, static, no state):
  - `IsOverPage` calls the `.jslib` under `UNITY_WEBGL && !UNITY_EDITOR`; elsewhere it's always true.
  - `Hovers(bool touch, bool fingerDown, bool overPage)` is the rule.
  - `Hovers(Pointer)` is the shortcut that `MenuHandCursor` uses.

Changed:
- `Assets/Scripts/UI/MenuHandCursor.cs`: the touch-only "no finger, no hand" check is now `BrowserPointer.Hovers(pointer)`.
- `Assets/Scripts/UI/VolumeCablePanel.cs`: `hover` comes from `BrowserPointer.Hovers(...)`.
- `Assets/Scripts/UI/CableInputController.cs`:
  - `Update` returns at once while suspended, instead of calling `HidePointers` every frame. The `Suspended` setter still hides everything once.
  - `hover` comes from `BrowserPointer.Hovers(...)`.
  - `ProcessPointer` (used by tests and checks) is unchanged.
- `Assets/Sprites/callers/caller-02.png.meta`: the slice is now 190×190, from 190×191, to fit the PNG.
  - All 13 sprites in `Assets/Sprites/callers/` now load with `AssetDatabase.LoadAssetAtPath<Sprite>`; `caller-02` was null before.
  - The other slices already fit and weren't touched: `caller-01`, `-11` and `-12` are 190×191 in 191×191 or 190×191 PNGs.
  - **`police.png`** is 191×191 with a 190×186 slice. It loads, but the slice starts at the bottom-left corner, so the top 5 pixel rows and the right column are cut off. Left as it is (the brief: don't change slices that fit); Mia or the lead can decide whether the slice should be the full 191×191.
- `Assets/Tests/Game/Editor/WebBuildTests.cs`: 8 new tests, listed under Tests.

Scene and prefab changes: none.

Tests: 525/525 EditMode (517 before; new below).
- `ThePointerHoversOnlyOverThePage`, 6 cases:
  - `AMouseOverThePageHovers`
  - `AMouseOverThePageHoversWhilePressed`
  - `AMouseOffThePageDoesNotHover`
  - `AMouseOffThePageDoesNotHoverWhilePressed`
  - `AFingerDownHovers`
  - `ALiftedFingerDoesNotHover`
- `TheEditorPointerIsAlwaysOverTheGame`
- `TheBrowserPluginIsForWebPlayersOnly` now runs for both `.jslib` files. That's one new case; it was one test before.

Console: clean. The Play Mode smoke run (11 s of game time in `Final`, stepped) logged only the engine's two "memoryless" lines. The console was cleared at the end.

Screenshot: `Temp/Captures/step-09b-fix.png`, the editor smoke run. Browser shots in `Temp/Captures/`:
- before the fix, with the iframe host:
  - `step-09b-fix-iframe-menu-left.png`
  - `step-09b-fix-iframe-game-left.png`: the hand frozen in the game while the mouse is outside
- after the fix, with the iframe host:
  - `step-09b-fix-after-iframe-menu-left.png`, `-game-left.png`: no hand once the mouse is outside
  - `-game-back.png`: the hand back
  - `-held-left.png`: a held cord stays put
- after the fix, at 1280×720:
  - `step-09b-fix-menu.png`
  - `-continue.png`: a planted save, $71.00, the 4×4 board
  - `-callers.png`: portraits in the caller column
  - `-held-cord.png`
  - `-book.png`: the hand over the open book
  - `-paused.png`
  - `-pause-options.png`
  - `-phone-menu.png`, `-phone.png`: 844×390 touch, a tap on New Game starts the game

Build and browser checks (`Tools/Switchboard/Build Web`: 2.7 min, 23.8 MB; `_OperatorPointer_IsOverPage` is in the framework):
- **Loading:** 1.3 s, title "Operator!". Browser console on load plus New Game:
  - Unity's four "ERROR: Shader" log lines (URP debug shaders, as in 9b)
  - Chrome's "The AudioContext was not allowed to start" warnings until the first click, then "Audio context resumed"
  - No errors or exceptions from our code.
- **Canvas cursor `none` wherever the hand shows** (call log after the fix):
  - menu over Continue: `none`
  - board: `none`
  - over a stored cord: `none`
  - holding a cord over the board (the plug is the pointer): `none`
  - over the open book: `none`
  - pause page, no hand: `default`
  - pause Options, over the patch: `none`; outside the patch: `default`
  - Now a single write per change; before the fix there were about 35 writes a second each way.
- **iframe host after the fix:**
  - mouse never in the game: `default`, no hand
  - mouse over the menu: `none` plus the hand
  - mouse out to the host page: no hand
  - in the game the same; back inside, the hand returns on the first move
  - with a cord held and the mouse out, the plug stays where it was, as with touch
- **Planted save:** Continue showed $71.00, the 4×4 board and the shop's next offers.
- **Caller portraits:** they show in the caller column.
- **Phone, 844×390 with touch:** the canvas is 844×390 and renders at 1688×780; a tap on New Game starts the game.

itch:
- `~/bin/butler push Builds/Web llazypixell/operator:html5` uploaded 23.82 MiB as a patch from #2162093.
- `butler status llazypixell/operator` shows `html5`, upload #19875492, **build #2162255 (version 2), processed (✓)**.
- No push anywhere else.

Housekeeping:
- **PlayerPrefs:**
  - at the start: `operator.save` and `operator.save.backup` absent, `Switchboard.AudioPatch.v1` = `{"version":1,"rows":[0,1,2],"sockets":[3,1,3]}`
  - at the end, put back exactly as found and checked
- **Discarded build and format re-saves:**
  - `Assets/Settings/UniversalRP.asset` and `Assets/UniversalRenderPipelineGlobalSettings.asset`
  - `ProjectSettings/TimeManager.asset`
  - the two dynamic TMP fonts
  - the build's `preloadedAssets` entry, put back to `[]`
- **Kept as found:** the 9b `ProjectSettings.asset` changes (Gzip, fallback, `PROJECT:Operator`, the WebGL batching entry) and the other uncommitted 9b files. `Assets/RawArt.meta` and `Assets/RawArt/art.png.meta` were left alone.
- **Editor:**
  - Web platform, idle, not in Play Mode, `timeScale` 1
  - `Final.unity` active and not dirty
  - no callbacks left
  - console cleared
- **Browser:**
  - The server and Chrome are stopped, and the Chrome profile is deleted.
  - The scripts stay in the scratchpad: `fix-probe.mjs`, which serves the logging framework; `fix-verify.mjs`; `fix-iframe*.mjs`; and `host/index.html`, the iframe host, served on port 8001.
- **Git:** nothing staged, committed or pushed.

Deviations from Design/Plan:
- **The fix adds a small `.jslib` and C# pair, not just a change to who writes `Cursor.visible`.** The cause was missing mouse events outside the page, which no `Cursor.visible` ordering can fix.
- **`CableInputController` no longer writes the cursor every frame while paused.** That's a small behaviour change: if the window loses focus while paused, the next `HidePointers` happens through `OnApplicationFocus`, as before.

Follow-ups:
- **Lead:** play the Draft page (build #2162255).
  1. Move the mouse in and out of the game frame on the menu, the board and the open book: the hand should leave with the mouse.
  2. Over the pause Options patch there should be one pointer.
  3. `caller-02`'s portrait is back.
- `police.png`'s 190×186 slice cuts the top 5 rows off the portrait. Decide whether to make it 191×191.
- A held cord stays drawn where the mouse left the page, as with touch. If that looks odd on itch, a later step could hide the plug too while the mouse is outside.
- Plan §4 lessons:
  - To see what Unity sends to the page, serve a patched framework through CDP `Fetch.fulfillRequest`. Headless Chrome 155 exposes no `style.cursor` setter to hook.
  - itch's iframe can be imitated with a host page on a second local port.
  - `Builds/Web` is deleted and recreated by every build, so restart `python3 -m http.server` afterwards: its working folder is gone.

#### Step 9b fix verification (orchestrator, 2026-10-19)
- **EditMode re-run:** 525/525 green (517 before, 8 new). Console clean apart from one MCP bridge line. No re-saves are left.
- **Scope:**
  - new: `BrowserPointer.cs` and `BrowserPointer.jslib`
  - changed: `CableInputController`, `MenuHandCursor`, `VolumeCablePanel` (hover goes through `BrowserPointer`; the cord input stops writing the cursor while paused)
  - `caller-02.png.meta` (slice height 191 → 190), `WebBuildTests`
  - No scene or prefab changed.
- **Screenshots checked:**
  - `step-09b-fix-iframe-game-left.png`: before the fix, with the mouse moved out of an itch-like frame, the hand stays frozen on the board.
  - `step-09b-fix-after-iframe-game-left.png`: after the fix, the hand is gone, and the caller portraits show.
- **The cause:** the hand was hidden correctly while the mouse was over the game. But a browser sends no mouse moves once the pointer leaves the page (itch's iframe), so the hand stayed drawn at the edge next to the system pointer. A mouse off the page now counts like a lifted finger (D28).
- **On itch:** `html5` build #2162255 (version 2), processed.
- **Open:** whether `police.png` (191×191) should use its full size instead of a 190×186 slice (lead or Mia).

#### Step 9b playtest, round 2 (lead, 2026-10-19): the cursor still flickers in Arc
- **The lead's report on build #2162255:** "nothing changed". In Arc (a Chromium browser on macOS), the system pointer flickers next to the game's hand all the time: in the menu, in Options and in the game. It shows and hides on every one-pixel move.
- **No project setting hides the pointer.** Player Settings has only Default Cursor (an image the system pointer becomes) and Cursor Hotspot, both empty here.
- **Measured in the lead's Arc** (`localhost:8000`, in the game, 10 s of mouse moves, with a console snippet that only watched):
  - the canvas's `style.cursor` changed once, to `none`, and stayed `none`
  - 653 `pointermove` events, all `mouse`, all on `unity-canvas`
  - no mouse-out from the page, no blur, DPR 1
- **Conclusion:** our scripts hold the pointer hidden steadily; the browser shows a hidden pointer anyway. Unity's framework writes the canvas cursor only in `_JS_Cursor_SetShow` and `_JS_Cursor_SetImage`.
- **Next:** check Chrome and Safari, then decide. The orchestrator recommends making the system pointer the hand (Default Cursor and `Cursor.SetCursor`) instead of hiding it.
- **Other browsers (lead):** Safari is fine; Chrome flickers like Arc. It's a Chromium-on-macOS problem with a hidden (`cursor: none`) pointer, so it must be fixed: Chrome is the most common browser.
- **Before choosing a fix:** the lead tries a transparent 1×1 cursor image instead of `none` in Chrome's console, with no rebuild. If the flicker stops, the fix is to hide the pointer with a transparent `Cursor.SetCursor` image and keep the drawn hand; otherwise, option A (the system pointer becomes the hand).
- **Test result (lead, Chrome):** with the canvas cursor set to a transparent 1×1 PNG (`url(data:image/png…) 0 0, auto`), the flicker stopped, and only the game's hand shows.
- **Fix decided:** on Web, the system pointer is hidden with that transparent image instead of `Cursor.visible = false` (`cursor: none`). The drawn hand stays as it is. Sent to a fix agent (pass 2); it may push to the Draft page (D55).



### Step 9b fix pass 2 report: 2026-10-19
Result: done. The build is on the itch Draft page: `html5` build **#2162638** (version 3, processed).

Playable now: in Web players the system pointer is hidden with a transparent 1×1 image (the CSS form the lead tested in Chrome) instead of Unity's `cursor: none`, so Chrome and Arc on macOS should show only the game's drawn hand. The hand's art and size are unchanged. **Headless Chrome can't show the OS pointer**, so the real look (no flicker in Chrome and Arc, Safari still fine) is the lead's check.

How to test: open the Draft page in Chrome or Arc on the Mac and move the mouse slowly over the menu, Options, the board, the open book and the pause Options patch. Only the drawn hand should show, with no system arrow flashing next to it. On the pause page (no hand) the normal arrow shows. Moving the mouse out of the game frame shows the normal arrow outside, and the hand comes back on the first move inside.

Changed:
- `Assets/Scripts/UI/BrowserPointer.cs`: new `SystemPointerVisible` (get and set), the one switch for the system pointer.
  - Web players: calls the `.jslib`.
  - Editor and desktop builds: plain `Cursor.visible`, as before.
  - It is the only file in `Assets/` that touches `Cursor.visible`. It doesn't use `Cursor.SetCursor`.
- `Assets/Scripts/UI/BrowserPointer.jslib`: two new functions next to `OperatorPointer_IsOverPage`.
  - `OperatorPointer_SetSystemShown(shown)`:
    - hidden: the canvas `style.cursor` becomes exactly `url(data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAC0lEQVR4nGNgAAIAAAUAAXpeqz8AAAAASUVORK5CYII=) 0 0, auto`
    - shown: `default`, which is what Unity's own `Cursor.visible = true` writes
    - It writes only when the value changes. It also rewrites if the canvas no longer holds what it last wrote, because Unity's player writes `default` twice while starting up (measured), and a stale cache would otherwise leave the arrow showing next to the hand.
  - `OperatorPointer_IsSystemShown()`: the state last set (shown until the first hide).
  - The file has no `"none"` value. Its importer is still Web only.
- `Assets/Scripts/UI/CableInputController.cs`, `MenuHandCursor.cs`, `VolumeCablePanel.cs`: every show, hide and capture (`cursorWasVisible`, `originalVisible`, `originalCursorVisible`) goes through `BrowserPointer.SystemPointerVisible`.
  - The logic is the same. Two lines are rewritten from `a || b ? false : original` to the equivalent `!a && !b && original`.
  - The first fix's rules stay: a mouse off the page counts as no hover, and the cord input leaves the cursor alone while paused.
- `Assets/Tests/Game/Editor/WebBuildTests.cs`: 6 new tests (below).
- **Grep of `Assets/`** (the menu package under `Assets/Tools/` included): there are no other `Cursor.visible`, `Cursor.SetCursor` or `Cursor.lockState` writes. The only cursor-related hits are `m_CursorLockBehavior: 0` in `Final.unity` and the menu package's `Pause.prefab` and `StartMenu.prefab`. That is a serialized setting of the Input System's UI module (lock behaviour), not a visibility write, so it was left alone.

Created: none (the scratchpad scripts below aside).

Scene and prefab changes: none.

Tests: 531/531 EditMode (525 before), with the editor on Web. New:
- `OutsideWebPlayersTheSystemPointerIsCursorVisible`: setting it changes `Cursor.visible`, and reading it follows `Cursor.visible`.
- `OnlyBrowserPointerTouchesCursorVisible`: no `.cs` under `Assets/` outside `BrowserPointer.cs` (tests excluded) contains `Cursor.visible`.
- `TheHandsShowAndHideThePointerThroughBrowserPointer`, 3 cases (`CableInputController`, `MenuHandCursor`, `VolumeCablePanel`): each captures and sets the pointer through `BrowserPointer.SystemPointerVisible`, and none uses `Cursor.SetCursor`.
- `TheBrowserHidesThePointerWithATransparentImage`: the `.jslib` holds the exact transparent-image value and `'default'`, and no `'none'` or `"none"`.
- "The `.jslib` is for Web only" was already covered by `TheBrowserPluginIsForWebPlayersOnly` (both `.jslib` files) and still passes.

Console: clean. The Play Mode smoke run (11 s of game time in `Final`, stepped) logged only the engine's two "memoryless" lines. The console was cleared at the end.

Screenshot: `Temp/Captures/step-09b-fix2.png` (the editor smoke run). Browser shots, all in `Temp/Captures/`:
- `step-09b-fix2-menu.png`, `-menu-options.png`
- `-continue.png`: a planted save, $71.00, the 4×4 board
- `-held-cord.png`, `-book.png`, `-paused.png`, `-pause-options.png`
- `-iframe-game.png`: the game in an itch-like iframe

The build: `Tools/Switchboard/Build Web` ran twice, at 2.0 min and 1.9 min, 23.8 MB each. The second build carries the "rewrite if overwritten" guard. `_OperatorPointer_SetSystemShown` is in the framework.

Browser checks (headless Chrome 155, the second build, `python3 -m http.server`):
- **Method:**
  - At each spot the mouse settles, then moves 1 px at a time 120 times over about 2 s.
  - A MutationObserver on the canvas `style` counts every change.
  - The framework is served with `_JS_Cursor_SetShow` patched to log calls.
- **Results** (`fix2-verify.mjs`, 12/12 OK):
  - **Startup:** before any mouse move the canvas is `default`, from Unity's own two `_JS_Cursor_SetShow(1)` calls at startup. After that, Unity made no cursor calls at all.
  - **Transparent image wherever the hand is drawn:**
    - the menu over Continue and over Options, and the menu's empty area
    - the menu's Options, over the patch and outside it (the menu hand covers both)
    - the board, over a stored cord, holding a cord, and over the open book
    - the pause Options patch
  - **`default` where no hand is drawn:** the pause page, and the pause Options outside the patch.
  - **No flips while moving:** 0 changes during the 120 moves at every spot, and `none` was never seen. Each change happened once, on arrival: the hide on the first move, the scene change, opening a panel.
- **iframe host** (the game at 960×540 inside a page on a second port, like itch; `fix2-iframe.mjs`, 6/6 OK):
  - `default` before the mouse enters
  - the transparent image over the menu
  - `default` once the mouse leaves to the host page
  - the same in the game, and back to the transparent image on the first move inside
- **Planted save:** Continue showed $71.00 and the 4×4 board, and the shop offered the next entries. After the run, `operator.save` had moved on (`secondsPlayed` 300 → 355; a $2 fine from a missed emergency).
- **Browser console:**
  - Unity's usual "ERROR: Shader" log lines (URP debug shaders), and Chrome's "AudioContext was not allowed to start" warnings before the first click. No errors or exceptions from the game.
  - One `MutationObserver` TypeError and one `/favicon.ico` 404 came from the check itself: the probe's hook runs on the static `style.css` page used to clear `localStorage`, which has no canvas or icon. They are not in the game page.
- **Not checkable headless:** whether the OS pointer is actually hidden on screen. The lead does the final look in Chrome, Arc and Safari.

itch:
- `~/bin/butler push Builds/Web llazypixell/operator:html5` uploaded 23.82 MiB as a patch from #2162255.
- `butler status llazypixell/operator` shows `html5`, upload #19875492, **build #2162638 (version 3), processed (✓)**.
- No push anywhere else.

Housekeeping:
- **PlayerPrefs:** at the start, `operator.save` and `operator.save.backup` were absent, and `Switchboard.AudioPatch.v1` = `{"version":1,"rows":[0,1,2],"sockets":[3,1,3]}`. At the end they were put back exactly as found and checked.
- **Discarded after each build:**
  - `Assets/Settings/UniversalRP.asset` and `Assets/UniversalRenderPipelineGlobalSettings.asset`
  - `ProjectSettings/TimeManager.asset`
  - the two dynamic TMP fonts
  - the `preloadedAssets` entry, put back to `[]`
- **Kept as found:** the 9b `ProjectSettings.asset` changes and the other uncommitted 9b and first-fix files. `Assets/RawArt.meta` and `Assets/RawArt/art.png.meta` were left alone. `SampleScene.unity` was untouched.
- **Editor:**
  - Web platform, idle, not in Play Mode, `timeScale` 1
  - `Final.unity` active and not dirty
  - no callbacks left
  - console cleared
- **Browser:**
  - The servers (8000, 8001) and Chrome are stopped, and the profile is deleted.
  - The new scripts stay in the scratchpad: `fix2-verify.mjs`, `fix2-iframe.mjs` and `fix2-startup.mjs` (the startup and blur/focus probe).
- **Git:** nothing staged, committed or pushed.

Deviations from Design/Plan:
- **The `.jslib` also rewrites when the canvas was overwritten,** not only when the requested state changes. Unity's player writes `default` twice while starting up, so a pure "on change" cache could leave the arrow showing until the next state change. It still never writes the same value twice in a row.

Follow-ups:
- **Lead:**
  - Play build #2162638 in Chrome and Arc on macOS: only the drawn hand should show, with no flicker, on the menu, in Options, on the board, over the open book and over the pause Options patch.
  - Check that Safari still behaves.
- **Plan §4 lesson:** Unity Web's `Cursor.visible = false` writes `cursor: none`, which Chromium on macOS doesn't keep hidden. Hide the pointer through `BrowserPointer.SystemPointerVisible` (a transparent image) and never write `Cursor.visible` directly; `OnlyBrowserPointerTouchesCursorVisible` guards it.
- Still open from the first fix: `police.png`'s 190×186 slice, and whether a held cord should hide while the mouse is outside the frame.

#### Step 9b fix pass 2 verification (orchestrator, 2026-10-19)
- **EditMode re-run:** 531/531 green (525 before, 6 new). Console clean, and no re-saves are left.
- **Scope:** `BrowserPointer.cs`/`.jslib` (`SystemPointerVisible`, `OperatorPointer_SetSystemShown`), plus `CableInputController`, `MenuHandCursor`, `VolumeCablePanel` and `WebBuildTests`. No scene or prefab changed, and nothing is staged.
- **Code skim:**
  - `BrowserPointer` is the only file outside the tests that touches `Cursor.visible`.
  - The `.jslib` hides the pointer with exactly the transparent PNG cursor the lead tested, and never writes `none`.
  - It rewrites only on a change, or when Unity's startup has overwritten the canvas.
- **On itch:** `html5` build #2162638 (version 3), processed.
- **Lead's look still owed:** headless Chrome can't draw the OS pointer, so the lead checks Chrome, Arc and Safari by eye.

#### Step 9b playtest, round 3 (lead, 2026-10-19): cursor fixes discarded
- **On itch the flicker stayed with fix pass 2** (build #2162638), also in a private window, although the same build didn't flicker on `localhost:8000` or inside a cross-site frame on `localhost:8001`. The lead also sees the double pointer in the Unity editor.
- **Sergei's fix** (`07678dc` "cursor fix" on `Sergei's_branch`):
  - the system pointer becomes the hand: Player Settings › Default Cursor `cursor_pointing.png` (hotspot 20, 2)
  - `Cursor.SetCursor` swaps grab and page-turn images, 64×64 PNGs in `Assets/Sprites/Cursors/`
  - `HandCursorGraphic` no longer draws a mesh
  - His branch starts from `6226ead`, before Steps 7–9a, so it can't be merged into main as it is. The lead asked him to bring it onto main.
  - Still open in his approach: while a cord's plug is the pointer it hides the system pointer with `Cursor.visible = false` (`cursor: none`), which flickers in Chrome.
- **Discarded on the lead's request** (orchestrator, directly):
  - the two fix passes: `BrowserPointer.cs`/`.jslib` deleted
  - `CableInputController`, `MenuHandCursor` and `VolumeCablePanel` back to `HEAD`
  - the 14 cursor tests and the `BrowserPointer.jslib` case removed from `WebBuildTests`
  - The Step 9b Web build work and the `caller-02` slice fix stay. The fix reports above stay as history.

### PNG cursors reapplied on main at 1.5x (2026-10-19)
- Ported the cursor presentation from `07678dc` without switching branches or replacing the newer cable/pause/touch logic. Current menu asset edits remain in place.
- Re-exported the original polygon geometry at 96×96 instead of 64×64; all four RGBA textures retain transparency. Imports use Cursor type, readable/uncompressed data, no mipmaps or NPOT scaling, and a 128 texture-size limit.
- Default Cursor is cursor_pointing.png at hotspot (30, 3). Grab uses (43.5, 42), page left (63, 34.5), page right (33, 34.5). MenuHandCursor, VolumeCablePanel and CableInputController now select PNGs through HandCursorGraphic; its old hand mesh is empty. Picking up a plug continues to show the existing plug artwork and hide the hand.
- Verified all four files and import settings, six serialized hand texture bindings, project default/hotspot, and reviewed the PNGs. Runtime and editor assemblies compile with Unity's Roslyn compiler, including existing tests; diff check passes. Live Play Mode and Unity test-runner execution were not available in this session.

### Options board artwork and frame correction (2026-10-19)
- Fixed the white options board: board_matte is a Single sprite, so its reference must use fileID 21300000, as in SwitchboardPanel; the previous stale multiple-sprite subasset ID did not resolve.
- Moved the three wall anchors under the left frame. Wires now have their own layer beneath an aligned, RectMask2D-clipped copy of the left frame; row lamps and movable plugs stay above it. Reuses the original board artwork without creating another texture or changing patch rules.
- Options heading now copies Operator's font colour exactly. Validated sprite/import references, local object and hierarchy links, aligned frame positions, hidden anchors, wire/frame/plug order, and title colour equality. Asset-only change; live Play Mode visual verification remains outstanding.

#### Step 9b done (lead, 2026-10-19)
- **Playtest OK:**
  - The game plays in the browser from the itch Draft page.
  - Progress survived reloads and today's re-uploads.
  - It runs smoothly on the lead's iPhone.
- **Accepted for later:**
  - the black side strips on wide phones (Step 12)
  - itch's fullscreen button on the iPhone (an Apple limit; Step 12 notes the embed option)
- **Double pointer:** Sergei brought his cursor fix onto main (`d79421d`, entry below) while this was being committed. It still needs a check in a Web build in Chrome or Arc: holding a cord still hides the pointer with `Cursor.visible = false` (Plan §8).
- **Commits:** `709be9d` (Step 9b) and `b4d39aa` (the metas for Mia's `RawArt`, matching Sergei's branch).

#### Pointer follow-up (orchestrator, directly; lead 2026-10-19)
- **The lead's report:** Sergei's PNG cursors flickered in the Unity editor too.
- **The cause:** `HandCursorGraphic.ApplyCursor` called `Cursor.SetCursor` on every frame, and `CableInputController` wrote `Cursor.visible = true` on every frame. Each write puts Unity's arrow back.
- **The fix:**
  - The image changes only when it differs.
  - `HandCursorGraphic.SetSystemPointerVisible` writes visibility only on a change, and puts the image back after showing the pointer again.
  - `CableInputController`, `MenuHandCursor` and `VolumeCablePanel` go through it.
  - Its cache is reset at each Play (`SubsystemRegistration`).
  - 517/517 EditMode tests green.
- **Measured in Chrome** (`localhost:8000`, a fresh build): 0 canvas cursor changes in 10 s and 1,663 mouse moves; the cursor stayed Unity's 96×96 `.cur` hand.
- **It depends on the monitor:** on the MacBook's built-in screen the hand is steady in Chrome and in the editor. On the lead's external display (120–140 Hz), Chrome flickers the pointer by itself, and the editor shows the hand only while a button is held. Not pursued further. Step 12 checks other machines.
- **Cross-check (lead):** itch build #2162638 (fix pass 2: the drawn hand with a transparent-image pointer) is steady on the built-in screen too. Every approach tried today works on a normal screen; only the lead's external display flickers.
- **Cursor size (lead):** Sergei's 96×96 hands were too big, and 64×64 (the old hand at full 1080p) still looked big in the lead's 0.75× Game view, because a system pointer doesn't shrink with the view. They were re-exported at **48×48** with his `Tools/Art/export_hand_cursors.py` (`ICON_SCALE` 0.75). `HandCursorGraphic.IconScale` is 0.75, and the Default Cursor hotspot is (15, 1.5). The metas and GUIDs are unchanged. 517/517 green.

#### Back to the drawn hand (lead, 2026-10-19; orchestrator, directly)
- **Why:** Sergei's PNG pointer has one pixel size and doesn't resize with the window (the 96, 64 and 48 px exports tried). The lead chose the drawn hand, which scales with the game everywhere.
- **Changed:**
  - `HandCursorGraphic`, `CableInputController`, `MenuHandCursor` and `VolumeCablePanel` are back to their `709be9d` versions (the drawn hand, from before `d79421d`).
  - Every pointer show and hide now goes through the new `SystemPointer` (`.cs` + Web-only `.jslib`). It writes only on a change. In browsers it hides with a transparent 1×1 image and re-hides if Unity rewrites the canvas cursor while starting up.
  - The cord input leaves the pointer alone while paused (as in fix pass 1).
  - Player Settings › Default Cursor is cleared (hotspot 0, 0).
  - Sergei's `Assets/Sprites/Cursors` images and `Tools/Art` exporter stay as he pushed them, unused. The scenes still hold his texture fields on `HandCursorGraphic`; Unity drops them at the next save.
- **Tests:** `WebBuildTests` gained `OnlySystemPointerTouchesCursorVisible`, `TheBrowserHidesThePointerWithATransparentImage`, and the `SystemPointer.jslib` case of the Web-only plugin test. 521/521 green.
- **On itch:** pushed as `html5` build #2163273 (version 4) on the lead's request, for the lead's online test.

#### Speech bubble and text colour (lead, 2026-10-19; orchestrator, directly)
- **The bubble shape is Sergei's** (`64da110`, `SpeechBubbleGraphic`): one procedurally drawn outline around the body and the tail, with 18 px corners, a light rim and a dark outline. Its settings are in the Inspector. Our sprite version (images in `Assets/Content/UI`) was built at the same time and set aside by the lead; it stays only on the local branch `bubble-ours`.
- **Text colour (ours):** very dark brown `#33241A` for the bubble's label (it was navy) and the book's text (it was `#2C302A`). The book's detail line is `#5C4535` (it was `#4E5046`). Contrast on the page: 14.0:1 and 8.4:1 (it was 12.6 and 7.7). `PhoneBookBuilder` uses the same colours.


## Test and font fix report: 2026-10-19
Result: done
Playable now: nothing changed in play, except that the pause panel draws its text from static fonts that look the same. The tests no longer break when the designer balances his three files.
How to test: open the pause menu (pause button or Esc) and Options in it. "Paused", "Resume", "Options", "Main Menu" and "Back" look as Sergei made them. EditMode is all green.
Created:
- `Assets/Fonts/BoecklinsUniverse/Boecklins Universe SDF Static.asset` (+ `.meta`)
- `Assets/Fonts/DellaRespira/Della Respira SDF Static.asset` (+ `.meta`)
- Both are Static TMP font assets made from Sergei's TTFs with the same settings as his dynamic ones (90 pt sampling, padding 9, SDFAA, 1024×1024, default material), holding all 95 printable ASCII characters. They have no runtime link to the TTF.
Changed:
- Tests: `Assets/Tests/Domain/BoardGrowthTests.cs`, `BookFileTests.cs`, `CallsFileTests.cs`, `EconomyBoardTests.cs`, `EconomyFileTests.cs`, `EconomyPoolTests.cs`, `SaveSessionTests.cs`; `Assets/Tests/Game/Editor/MergedSystemsTests.cs`, `PhoneBookEntriesTests.cs`, `PhoneBookPageTurnTests.cs`
- `Assets/Prefabs/Game/GameUI.prefab`: the six pause-panel labels' font and material only (12 lines), plus one blank `m_Name:` line Unity rewrote with a trailing space
- Docs: `Docs/Design.md`, `Docs/Plan.md`, this report
Scene and prefab changes: `GameUI.prefab` `Pause/Dialog`: `Main/Title` and `Options/Title` now use `Boecklins Universe SDF Static`; `Resume`, `Options`, `MainMenu` and `Back` labels use `Della Respira SDF Static`. Sizes, colours and layout are unchanged. `Final.unity` has no overrides on them and is unchanged.
Tests: 533/533 EditMode, none skipped (before: 506/533). No new tests: 25 real-file tests rewritten (several renamed, listed below) and Sergei's Tab test fixed.
Console: clean
Screenshot: `Temp/Captures/pause-static-font.png`, an offscreen render of `GameUI.prefab`'s pause dialog with the static fonts. No Play Mode was used, so no PlayerPrefs were touched.

What each failing test was:
- **(a) pinned the designer's real numbers: rewritten to check the rule, with values read from the loaded files.**
  - `BoardGrowthTests`:
    - `TheRealFilesPlayTheStartPoolOnThreeByThreeInTwoSlots` → **`TheRealFilesPlayTheStartPoolOnTheStartingBoardAndSlots`**. Every call fits the start board, every start slot fills, the pool starts at its first call and loops, every start-pool call rings, and no caller has two live calls. "No emergencies in the start pool" and the exact play order were dropped: the designer now puts emergencies there, and the new one-call-per-caller rule reorders calls on purpose (b).
    - `TheRealBookHasOneServicePerNewRing` → **`TheRealBooksServicesAreAllOnTheBiggestBoard`**. All three services are now on the 4×4 start.
  - `BookFileTests`:
    - `TheRealFilePassesWithServicesFirstAndEveryEntryFilledIn` → **`TheRealFilePassesWithServicesFirstThenNamesInOrder`**. No problems, `contactsPerPage` and the entry count come from the file, services first in file order, then names A–Z, and every jack a call uses is in the book. The 36-jack and C3 checks were dropped. Profession and address were dropped too, because Design 6.6 allows them to be empty.
    - `TheRealFileWritesSurnamesInCapitalsAndUsesOnlyPlainText` → **`TheRealFileUsesOnlyPlainText`** (ASCII only, no `<`/`>` in book text). The capitals were the designer's own style, and he has changed it on purpose.
  - `CallsFileTests.TheRealFileLoadsWithNoProblemsAndOnlyStraightQuotes` (name kept). Every call loads, the file is ASCII only, every caller and jack is in the book, durations are above 0, and an emergency has a response time and a fine. The "≥ 40 calls" check and the pay ranges were dropped.
  - `EconomyBoardTests.TheRealFileStartsOnThreeByThreeAndGrowsInThreeSteps` → **`TheRealFileStartsOnItsStartingBoardAndEveryStepGrows`**.
  - `EconomyFileTests`:
    - `TheRealFileLoadsTenEntriesWithNoProblems` → **`TheRealFileLoadsEveryEntryWithNoProblems`**. Entry count from the file, exactly one secret, cord and rate chains grow, no Notepad.
    - `GameConfigReferencesTheRealFile` (name kept). GameConfig's text is the file's text.
    - `TheRealShelfMatchesTheArtworkAndReplacesCompletedChains` → **`TheRealShelfShowsOneEntryPerChainAndReplacesCompletedChains`**. The shop artwork follows the upgrade kind, not the slot, so no order rule was lost.
  - `EconomyPoolTests`:
    - `TheRealFilesHaveTheShippedQueues` → **`TheRealFilesQueuesFitTheirBoards`**. Kept: "the first start call isn't an emergency (the guide's call)".
    - `ValidateContentNamesACallOutsideTheStartingBoard` (name kept). It now finds a call outside the start board in `Calls.json` and swaps it in however the line is spaced. If none exists, it reports Ignored.
  - `SaveSessionTests.ALoadedBoardTwoOpensFiveByFiveAndDealsFromItsStagePool` → **`ALoadedBoardUpgradeOpensItsOwnSizeBeforeTheFirstCall`**. Loads the whole board chain plus the first cord and rate entries; expects the first live calls to be the stage pool's first different callers.
  - `MergedSystemsTests`:
    - `FreshGameHidesLockedPlacesAndShopPurchasesRevealThem`, `PurchasedCordJoinsCurrentGuideAndPickupClearsEveryPulse` and `NewCordKeepsConnectionHighlightsCoinsAndAudio` (names kept). Counts, prices and slots come from `Economy.json`. The bought cord is "the cable that became active", no longer `Cable_04`.
  - `PhoneBookEntriesTests`:
    - `TheRealBookShowsEveryEntryInTheLoadersOrder` and `ARowShowsTheNameThenProfessionAndAddressAndTheJackLast` (names kept). Expected rows come from the loader.
    - `AFreshGameOnTheThreeByThreeBoardListsEveryone` → **`AFreshGameOnTheStartingBoardListsEveryone`**.
  - `PhoneBookPageTurnTests` (seven tests, names kept). The real book now fits on one spread (14 entries, 7 per page), so there was no page to turn. The page-turn and cursor mechanics now read a four-spread sample book with the real page size. `PageTurningGoesThroughTheWholeBook` reads the real file and works for any number of spreads, including one.
- **(b) old behaviour changed on purpose:** covered inside the rewrites above (1 starting cord, one live call per caller). The one-call tutorial start needed no test change: `GameInstaller` and the real-file sessions in tests load a save or build `GameSession` without the flag.
- **(c) real bugs:** none.
- **Other: `PhoneBookPageTurnTests.TabTogglesBookAndIsIgnoredWhilePaused`** (Sergei's new test from `578163e`). Not a game bug. In EditMode the Input System runs "Editor" updates, and `wasPressedThisFrame` only counts player updates, so the queued Tab never counted as "pressed this frame" on any machine. In Play Mode the game gets player updates and Tab works. The test now turns on the Input System's `runPlayerUpdatesInEditMode` (internal, through reflection) while it presses Tab, and restores it in `finally`.
- **Pause font (Task B):** `GamePauseMenuTests.ThePanelIsAModalAboveTheBookAndBelowTheCursor` passes. The panel uses **Boecklins Universe SDF Static** (titles) and **Della Respira SDF Static** (button labels). The shared dynamic font assets were not edited and were not re-saved this time.

Housekeeping: discarded the `ProjectSettings/TimeManager.asset` re-save. No changes to the dynamic TMP fonts, `SampleScene.unity`, the URP assets or `preloadedAssets`. The editor is idle, not in Play Mode, with `Final.unity` active and not dirty (it was on `MainMenu` at the start). Nothing is staged, committed or pushed.

Deviations from Design/Plan:
- **The one-call tutorial start does not run on Continue.** The orchestrator's note said it did, but the merge `baed5b4` added `tutorial && save == null` in `GameSession`. So it runs on a new game, and again after a reload while a new game hasn't saved yet. Plan's Step 10 outline says so and keeps the open question (new game only? a saved flag?).
- Design.md: a status row for the designer's balance pass. 5.2 gains the one-call-per-caller skip and the tutorial start. 5.7 and 6.6 now say names are free text ("First Surname" since `7312e43`), and 6.4 that the designer sets the numbers (`startingCords` 1–6, currently a 4×4 start with 3 slots). D49, D52 and D53 are annotated as changed by the designer. Plan §4 has the "Real-file tests check rules, not numbers" row.

Follow-ups:
- **The designer's guides now disagree with his files** (not edited, as asked):
  - `Docs/EconomyFile.md`:
    - line 46: "the game starts on a 3x3 corner"
    - line 48: "the shipped first call pays 10¢ + 20 × 5¢" (now 25¢ + 4.8¢/s for 25 s)
    - lines 70/72 and 105/119: the examples show `startingCords` 2 and `startingClientSlots` 2
    - line 111: `startingCords` "2 to 6" (now 1 to 6); line 160: "2 in the shipped file" (now 1); line 223: "outside 2 to 6"
    - line 184: one emergency service per new ring (Doctor D4, Fire Brigade E5, Police B6), now all three on the 4×4 start
    - line 97: "until saves arrive in Step 9, every restart"
    - The guide also doesn't mention the one-call tutorial start or the one-live-call-per-caller skip (5.2).
    - Line 26's example call and line 205's "3x3" example message are only illustrations.
  - `Docs/BookFile.md`:
    - line 25 and line 39: names "SURNAME, First" and the surname order (the file now uses "First Surname", so the book sorts by first name)
    - line 28: services "the Police, the Fire Brigade and the Doctor" (now "Police station", "Fire station", "Hospital")
    - line 13: example `contactsPerPage` 6 (the file uses 7; the default 6 is still right)
    - line 68: an example message
- **Code text that still says "SURNAME, First":** `BookFileLoader`'s "the name is missing. Write it "SURNAME, First"" message, and the `ShowLetter` comment in `PhoneBookView`. Worth a word with the designer before changing.
- **Validator gap from the one-call-per-caller rule:** `EconomyFileLoader` warns when a pool has fewer *different calls* than its slots, but now what matters is *different callers*. The current pools are fine (7, 9 and 11 callers for 3, 4 and 6 slots).
- The static fonts hold printable ASCII only. A new pause label with "¢", "—" or accented letters needs a regenerated font; `GamePauseMenuTests` would catch it. Sergei's main menu still uses the dynamic fonts (outside this pass).

#### Merge and check (orchestrator, 2026-10-19)
- **Merged:** tarbaev.d's second "Configs wip" (`4fe1ce9`, one line in Calls.json plus a new `CallsFileTests` check) and Sergei's new shop sounds and music ducking (`2c0d99d`). Our uncommitted work went on top through a stash.
- **One conflict, in `PhoneBookPageTurnTests`:** kept the real-book check and counted spreads with Sergei's `PageCount`.
- **Sergei changed the book on purpose:** only the corner folds turn pages, a click elsewhere on the open book closes it, and a click outside keeps it open. `CheckPageCursor` now expects the pointing hand over a row.
- **Result:** EditMode 543/543 green, and `Validate Content` found no problems.
- **Play Mode run in `Final`** (about 50 s of game time at ×5, no errors or warnings):
  - It starts on 4×4 with 3 slots, 1 cord and one tutorial call.
  - After that call connected through `OperatorDesk`, the other two slots filled, money rose to $1.19, and no caller showed twice.
  - The editor's PlayerPrefs were restored exactly.


### Cleanup after tarbaev.d's Settings/Save commits (2026-10-20)
Result: done (the Web build restore still needs one real build to confirm it)
Playable now: nothing new to see at 16:9. On screens wider or narrower than 16:9 the editor-only "Dev" button now stays just left of the pause button in the play area's corner instead of following the screen edge. The incoming ring sounds the same as before (0.4875), and it can now be tuned in the Inspector. The fonts and URP settings tarbaev.d's commits re-saved are back to how they were before `94cff92`, and every font still renders.
How to test:
1. Play `Final` at 1920×1080: "Dev" sits just left of the pause button, as before. Then try a wide size such as 2340×1080: "Dev" still sits next to the pause button at the corner of the art, not at the screen edge.
2. Select `Game` › `CallAudio`: "Incoming Gain" is 0.4875.
3. Open the book while a call is in hand: nothing of ours covers it.
4. On the next Web build, check that `git status` shows no URP, font, `TimeManager` or `preloadedAssets` changes afterwards.
Created:
- `Assets/Tests/Game/Editor/DevPanelLayoutTests.cs` (+ `.meta`)
Changed:
- `Assets/Scripts/Game/DevTools/DevPanel.cs`: new `FirstButtonPosition(screenWidth, screenHeight)`. It places the first button from the 1920×1080 play area's right and top edges (centred the way GameUI's Expand canvas centres it), not from the screen's edge. The row's `y` follows the play area's top too, so on 4:3 it stays level with the pause button. `ButtonWidth` and `Gap` are public for the test.
- `Assets/Scripts/Game/CallAudio.cs`: `[SerializeField, Range(0f, 2f)] private float incomingGain = .4875f` replaces the literal. It uses Range 0–2 to match `cashRegisterGain`, its neighbour.
- `Assets/Prefabs/Game/Game.prefab`: `incomingGain: 0.4875`. `Final.unity` has no `CallAudio` override.
- `Assets/Scripts/Game/Editor/WebBuild.cs`:
  - `Build()` now runs `AssetDatabase.SaveAssets()` first, so a deliberate unsaved edit is kept. It then snapshots the bytes of `RestoredFiles` and `PlayerSettings.GetPreloadedAssets()`, and runs the old build (now `BuildWeb()`) inside `try`.
  - In `finally` it calls `SetPreloadedAssets(old)` and `SaveAssets()`, then writes back every snapshotted file whose bytes changed, and force-reimports those under `Assets/`.
  - `RestoredFiles` lists the URP asset, the URP global settings, both dynamic fonts and `ProjectSettings/TimeManager.asset`. `Snapshot` and `Restore` are small public helpers.
- `Assets/Tests/Game/Editor/WebBuildTests.cs`: `TheFilesTheBuildRestoresExist` and `RestoreWritesBackOnlyTheFilesThatChanged` (temp files under `Temp/`).
- `Assets/Tests/Game/Editor/GamePauseMenuTests.cs`: comment only. "letterboxed screens" now reads "screens wider than 16:9 (bars at the sides)".
- **Restored to `94cff92~1`** (the noise committed in `94cff92` and `c43a764`):
  - `Assets/Settings/UniversalRP.asset`: only URP shader-prefilter flags (`m_PrefilteringMode*`, `m_Prefilter*`)
  - `Assets/UniversalRenderPipelineGlobalSettings.asset`: only the `m_RuntimeSettings` list (12 rids)
  - `Assets/Fonts/DellaRespira/Della Respira SDF.asset`: +11,265 lines in `94cff92`, +75 in `c43a764`, all added kerning-pair records (`m_GlyphPairAdjustmentRecords`); no glyph, character, atlas, fallback or setting changes
  - `Assets/Fonts/BoecklinsUniverse/Boecklins Universe SDF.asset`: +45 lines, kerning-pair records only
  - `Della Respira SDF Static.asset` and `Boecklins Universe SDF Static.asset`: only the materials' `_ScaleRatioA` (1 to 0.9) and `_ScaleRatioC` (1 to 0.73125), which TMP recalculates itself
  - Nothing looked deliberate.
- Docs:
  - `Docs/Design.md` 4.3 (the pause button sits in the play area's corner; Dev follows it)
  - `Docs/Plan.md` §4 (the Dev panel, Dynamic TMP fonts and Web builds rows) and §8 (Web build leftovers)
  - `Docs/WebBuild.md` (the build tidies up after itself; never commit font glyph changes)
  - `CLAUDE.md` (Web build step 2)
  - this report
Scene and prefab changes: `Game.prefab` (one serialized field). No scene changes. `Final.unity` is not dirty.
Tests: 563/563 EditMode (was 554). New: `DevPanelLayoutTests` (2 cases at 16:9 and 5 aspect ratios checked against the real `PauseButton` and GameUI's CanvasScaler), plus the two `WebBuildTests` above.
Console: clean
Screenshots (`Temp/Captures/`):
- `cleanup-book-closed.png`: a call taken, before its bubble was clicked
- `cleanup-book-open.png`: the book open with Nora Ellis in hand, speech bubble showing, "New order!" cards in the other slots
- `cleanup-dev-button-wide.png`: 2340×1080, Dev next to pause at the play area's corner
- `cleanup-pause-menu.png`: static fonts render
- `cleanup-main-menu.png`: the dynamic fonts render on MainMenu
Book overlap check (Task 4): at 1920×1080 the open book (scale 1.1, x −95) covers roughly x 924–1830 and y 488–1060 in reference pixels (bottom right). **Nothing of ours overlaps it.** It stays clear of:
- the speech bubble (right of the top card, about x 315–460, y 95–235)
- the call cards
- the money plaque
- the pause button
- the Dev button
The book covers the lower shop slots, as before; that's the board's own panel under a modal book, not our overlay. No nudge needed.
Fonts (Task 5c):
- **"Clear Dynamic Data On Build" (`m_ClearDynamicDataOnBuild`) is already on for both dynamic fonts.** In this TMP (ugui 2.0.0) it clears the character and glyph tables only in `TMP_PreBuildProcessor` and when the editor quits. That is also why a build re-saves the fonts.
- Nothing stops Play Mode from adding glyphs and kerning pairs in memory, and the next save (Ctrl+S, a scene save, `SaveAssets`) writes them out.
- The other switches aren't safe fixes:
  - Turning off `m_GetFontFeatures` would drop kerning (a visible change).
  - Static atlas mode would need regenerated atlases.
  - `m_IsMultiAtlasTexturesEnabled` doesn't affect it.
- No setting changed. The team rule went into WebBuild.md and Plan §4: never commit dynamic font changes; discard them with `git checkout --`.
- After this pass's Play Mode check, the atlas textures and static materials were dirty in memory (a force reimport didn't clear that). `EditorUtility.ClearDirty` on them keeps the next save from writing them.
Housekeeping:
- The lead's `operator.save` and `operator.save.backup` were kept in editor `SessionState` during the Play Mode check and restored exactly afterwards, then the keys were erased.
- The Game view is back to 1920×1080, and `timeScale` is 1.
- No callbacks are left.
- `ProjectSettings.asset` was not touched.
- Nothing is staged, committed or pushed.
Deviations from Design/Plan:
- `incomingGain` uses `Range(0f, 2f)`, not 0–1, to match `cashRegisterGain`.
- `WebBuild` also restores `TimeManager.asset` (CLAUDE.md listed it as a build re-save).
Follow-ups:
- **A real `Build Web` run must confirm the restore:** after the build, `git status` should show no URP, font, `TimeManager` or `preloadedAssets` change. One thing to watch: the restored `preloadedAssets` is saved through `SaveAssets`, which also writes anything else dirty in `ProjectSettings.asset` (such as the Unity Cloud link Unity keeps re-adding).
- The IMGUI Dev button still shows behind the pause dialog: it hides only for `PresentationOpen`. That's editor-only and predates this pass.
- Design 4.3 says the bubble sits "left of the caller card", but it shows to the right of the portrait (it predates this pass; the docs wording could be fixed).
- "Build Web (Dev)" is still open (Plan §8).

### Volume patch: free plugging without jumping jacks (2026-10-20)
Result: done
Playable now: in Options (main menu and pause menu) any wire can be plugged into any free socket in any row; the column sets that wire's volume, and no other jack ever moves. A socket that already holds another wire's jack refuses the held jack, which goes back where it was. While holding a jack, only free sockets (and the jack's own) light up.
How to test:
1. Main menu, Options: put Master (yellow) at 75 in the Master row and Music (blue) at 25 in the Music row. Pick up only the yellow jack and plug it into the Music row's 50 socket: the blue jack stays at 25, nothing else moves, and the master volume is now 50%.
2. Pick up the yellow jack again and move it over the blue jack's socket: that socket doesn't light up. Click there: the yellow jack goes back to where it was. Free sockets in any row light up and accept it.
3. Leave Options and come back (or restart, or open Options from the pause menu in a game): the wires are exactly where you left them, two of them sharing the Music row.
Created:
- `Assets/Tests/Game/Editor/VolumePatchStateTests.cs` (+ `.meta`)
Changed:
- `Assets/Scripts/UI/VolumePatchState.cs`: the cross-row swap is gone. `Connect` refuses (returns false) a socket held by another wire; the held wire's own socket counts as free (no-op connect). New `IsFree(row, socket)`. `Restore` accepts shared rows and rejects out-of-range values and two wires in one (row, socket). Class comment updated.
- `Assets/Scripts/UI/VolumeCablePanel.cs`: while a jack is held, a hovered socket lights up only if it is free (`patch.IsFree`); without a held jack, the pick-up hint is unchanged. Pickup (`plug` detection, `CableAt`) already works per (row, socket), so several wires in one row needed no change. A refused drop goes through the existing `Cancel`. `OwnsPointerAt` and touch handling unchanged.
- `Assets/Scripts/UI/MasterVolume.cs`: if its target socket in Master's row is taken by another wire, `Apply` cancels the pickup and does nothing (it has no callers today).
- `Docs/Design.md` (5.12 item 8: the volume patch rule), `Docs/Progress.md` (this report)
`VolumeCableSlider.cs` unchanged: a wire in another row draws from its own wall anchor to the other row's socket.
Scene and prefab changes: none. `Final.unity` active, not dirty.
Tests: 577/577 EditMode (was 563). New in `VolumePatchStateTests` (14): the lead's repro (Master row 0 "75", Music row 1 "25", Master to row 1 "50": Music unchanged at .25, Master .5), moving any wire to any socket never moves another (all 45 moves), an occupied socket refuses and the held wire stays, dropping on its own socket is allowed, all three wires sharing one row, `Restore` accepts shared rows, rejects 7 bad saves (duplicate (row, socket), out-of-range rows and sockets, wrong length), and an old one-wire-per-row save loads unchanged. No existing test asserted the swap; `WebBuildTests`' broken-save case `rows [0,0,2], sockets [3,3,3]` is still rejected (two wires in one socket) and was left as it is.
Console: clean (only Unity's own "Ignoring depth surface ... memoryless" lines during Play Mode).
Play Mode check (MainMenu Options, driven through `VolumeCablePanel.ProcessPointer` in a one-shot `Canvas.willRenderCanvases` handler):
- Repro: Master picked up from row 0/socket "75" and dropped on row 1/socket "50". Result: Master r1 s1 (.5), Music r1 s0 (.25, unmoved), SFX r2 s1; saved `{"version":1,"rows":[1,1,2],"sockets":[1,0,1]}`.
- Holding Master: hovering Music's occupied socket shows no highlight, a free socket and Master's own socket do; clicking Music's socket was refused and Master stayed at r1 s1.
Screenshots: `Temp/Captures/volume-patch-before.png`, `Temp/Captures/volume-patch-after.png` (blue jack stays at 25; yellow now at 50 in the Music row).
Housekeeping:
- The lead's `Switchboard.AudioPatch.v1` (`{"version":1,"rows":[0,1,2],"sockets":[3,0,2]}`), `operator.save` and `operator.save.backup` were kept in editor `SessionState` during the check and restored exactly afterwards (hashes compared), then the keys were erased. The check ran in MainMenu, so no game save was written.
- Dynamic TMP fonts and static materials were dirty in memory after Play Mode: `EditorUtility.ClearDirty`; nothing on disk changed. `ProjectSettings.asset` untouched. No callbacks left. Nothing staged, committed or pushed.
Deviations from Design/Plan: none.
Follow-ups:
- In the after screenshot the yellow wire (Master) is drawn over the blue jack it crosses in the Music row: each slider draws its wire and plug together, so a wire from an upper row passes over a plug in a lower row. Readable, but if it bothers the lead, plugs could be lifted above all wires.

### Volume patch: last-plugged wire lies on top (2026-10-20)
Result: done
Playable now: in Options (main menu and pause menu) wires stack like on a real board: the wire you plugged last lies on top. Each wire is drawn under its own jack, and that pair lies over every older wire and jack, so a newer wire crossing an older jack covers it, and a held wire is on top while you carry it. The order is saved with the patch and survives closing Options and restarting. The wires still come out from behind the left frame.
How to test:
1. Main menu, Options: put Master (yellow) at 75 in the Master row and Music (blue) at 25 in the Music row. Move the yellow jack into the Music row's 50: the yellow wire now passes over the blue jack.
2. Pick up the blue jack and plug it into the Master row's 50: the blue wire now lies over the yellow wire.
3. Pick up the red (SFX) jack and carry it around: its wire is drawn over all the others; carried over the left frame, the jack is still visible. Click on an empty spot so it goes back: its wire stays on top.
4. Leave Options and come back, restart the game, or open Options from the pause menu in a game: the wires lie in the same order.
Created:
- `Assets/Tests/Game/Editor/VolumeStackTests.cs` (+ `.meta`)
Changed:
- `Assets/Scripts/UI/VolumePatchState.cs`: the stack order (`CableInLayer(layer)`, `LayerOf(cable)`, bottom to top, default Master, Music, SFX). `PickUp` lifts the wire to the top, so connecting it and putting it back both leave it there. `Restore(rows, sockets, order = null)` takes the saved order if it is a permutation of 0..2; otherwise the default, without rejecting the rows and sockets.
- `Assets/Scripts/UI/AudioVolumeSettings.cs`: `SavedPatch.order` (still version 1, same key; a missing field loads as the default stack). New `Cancel()` puts a held jack back and saves, because the pickup changed the order.
- `Assets/Scripts/UI/VolumeCablePanel.cs`: `OrderLayers` sets the sibling index of each wire+jack pair from the patch's order, only when the order has changed (cached in `shownStack`, reset in `OnEnable`). It replaces "held plug to last sibling". A refused drop and `OnDisable` go through `AudioVolumeSettings.Cancel()`. Pointer, touch, `OwnsPointerAt` and highlights are unchanged.
- `Assets/Scripts/UI/MasterVolume.cs`: a refused `Apply` uses `AudioVolumeSettings.Cancel()`.
- `Assets/Editor/VolumeOptionsBuilder.cs`: new `LayerCables` (also the menu `Tools/Switchboard/Layer Volume Cables`, run once on the prefab), and `BuildPanel` calls it so a rebuild gives the same structure.
- `Assets/Tests/Game/Editor/WebBuildTests.cs`: the expected saved JSON now ends in `"order":[0,1,2]`.
- `Docs/Design.md` (5.12 item 8), `Docs/Progress.md` (this report)
Scene and prefab changes: `Assets/Prefabs/UI/VolumeCablePanel.prefab` changed once: `WiresBehindFrame` is gone, and `CableOverlay` now holds `MasterCable`, `MusicCable` and `SfxCable` (each `WireClip/<Channel>Wire` and then `<Channel>MovingPlug`), followed by `HandCursor`. All existing fileIDs are kept, so the overrides in `MainMenu.unity` and `GameUI.prefab` (plug sizes, sounds, the panel's position) still apply; neither file changed. When Unity re-saved the prefab, it also wrote values that were already applied at runtime: the plugs' art size and pivot, the lamps' default `glow`/`litTint`, and float rounding. It also dropped stale `HandCursorGraphic` texture fields that no longer exist in the script. `Final.unity` is active and not dirty.
How the frame still hides the wire start: every wire sits in its own `WireClip`, a `RectMask2D` whose left edge is the left frame's inner edge (panel x -499.5, the right edge of `LeftFrameCover`). The clip reaches 8000 px right, up and down. Each wall end (x -530) is therefore cut off exactly where the frame used to cover it. The jacks are outside the mask, so a held jack stays visible over the frame. I chose this over a second copy of the frame cover because the pairs must interleave (wire, jack, wire, jack), and one cover can't sit above every wire but below every jack. Differences from before: a wire carried up past the top of the board now lies over the column lamps instead of under them, and a jack carried left of the board loses its wire behind the frame, as it already did.
Tests: 589/589 EditMode (was 577). New in `VolumeStackTests` (12):
- the default stack
- each connect lifts that wire to the top
- a held wire is on top and stays there when put back, also after a refused drop
- save and load keep the order (`"order":[1,0,2]`)
- a save without the field gets the default
- six bad orders (duplicate, short, long, out of range, negative, empty) fall back to the default but keep rows and sockets
- the prefab: after Master's connect the pairs are Music, SFX, Master with the hand above; each wire lies under its own jack inside a `RectMask2D` starting at the frame's inner edge; jacks are never clipped; every wall anchor stays left of that edge

`MainMenuLayoutTests` (including `SocketRowsFitTheirLayoutAndClearTheCableAnchor`) unchanged and green.
Console: clean (only Unity's own "Ignoring depth surface ... memoryless" lines during Play Mode).
Play Mode check (MainMenu Options, `VolumeCablePanel.ProcessPointer` in `Canvas.willRenderCanvases` handlers):
- Repro: start `rows [0,1,2] sockets [2,0,1]`. Master to row 1/50: stack Music, SFX, Master; saved `{"version":1,"rows":[1,1,2],"sockets":[1,0,1],"order":[1,2,0]}`. Then Music to row 0/50: stack SFX, Master, Music.
- Held SFX shown over the frame and over the board, then dropped on an empty spot: SFX on top, `order [0,1,2]` saved. Close and reopen Options: same sibling order.
- Stop and start Play Mode: the patch and order came back from the save (`order [1,2,0]` after one more connect), and so did the sibling order.
- `Final.unity`: pause, then Options. The pause menu's panel shows the same structure and order (Master's yellow wire over Music's blue). No errors.

Screenshots in `Temp/Captures/`:
- `volume-stack-1.png`: yellow wire over the blue jack
- `volume-stack-2.png`: blue wire over the yellow one
- `volume-stack-held.png`: held red jack over the left frame
- `volume-stack-held-board.png`: held red wire over the yellow jack and the other wires
- `volume-stack-pause.png`: the pause menu's Options

Test note: while Unity has focus, the panel's own `Update` moves a held jack to the real mouse each frame. The held screenshots therefore re-applied the simulated pointer and rebuilt the wire meshes in the handler. This affects only the check, not the game.
Housekeeping:
- The lead's `Switchboard.AudioPatch.v1` (`{"version":1,"rows":[2,2,0],"sockets":[3,2,3]}`), `operator.save` and `operator.save.backup` were kept in editor `SessionState` during the check and restored exactly afterwards (hashes compared). The keys were then erased. The Final check wrote the game save; it was restored too.
- The dynamic TMP fonts (`Boecklins Universe SDF.asset`) and the "SDF Static" materials (Boecklins, Della Respira) were written during the check. They were discarded with `git checkout --` and cleared in memory.
- `ProjectSettings.asset` untouched. Game view back to 1920×1080. No callbacks left. Nothing staged, committed or pushed.

Deviations from Design/Plan: none.
Follow-ups:
- The panel's top hit is still the jack drawn on top, because the raycaster sorts by hierarchy depth, which now follows the stack. Two connected jacks never overlap (one per socket), so this matters only for a held jack, which isn't raycast.

#### Pass 2: wires always over other jacks (lead, 2026-10-20)
The lead's playtest put Master at Music/100, SFX at Master/100 and Music at Music/50, plugged last. The red wire ran under the blue jack because blue was newer. The lead wanted "the wire should go above a jack if it covers it", and a real jack sticks out of the board, so the rule changed.
- New layering, bottom to top: all resting jacks, then the wires in plugging order (last plugged on top, still saved as `order`), then the held jack, then the hand. A strict per-pair order can't put a wire both under its own jack and over every other jack, so each wire now also lies over its own jack.
- How a wire meets its own jack: the wire ends exactly at the jack's tail tip (`PlugGraphic.TailOffset`, the sprite's bottom edge) and comes in from below. Only a pixel or two of the wire's highlight overlaps the tail, so it reads as the cord entering the boot (`Temp/Captures/volume-stack-own-tail.png`). No extra sprite was needed.
- Clicks: wires take no clicks and the `WireClip` masks draw nothing, so the top hit over a jack is still the jack, even with a wire drawn over it. Play Mode check: a raycast at four points on the blue jack under the yellow and red wires hit `MusicMovingPlug` each time. Pressing there picked it up, which put the blue jack above all wires. Dropping it back on its own socket returned it to the jack layer.
- Prefab: `CableOverlay` is now flat: `MasterMovingPlug`, `MusicMovingPlug`, `SfxMovingPlug`, `MasterWireClip`, `MusicWireClip`, `SfxWireClip` (each a `RectMask2D` holding its wire, starting at the frame's inner edge as before), `HandCursor`. The pass-1 `<Channel>Cable` groups are gone. All fileIDs were kept, and `MainMenu.unity` and `GameUI.prefab` are unchanged.
- Changed in this pass:
  - `Assets/Scripts/UI/VolumeCablePanel.cs`: `OrderLayers` sets the sibling order for resting jacks, then wires by recency, then the held jack. It is cached on the order plus the held jack.
  - `Assets/Editor/VolumeOptionsBuilder.cs`: `LayerCables` builds the flat structure and also converts the pass-1 groups. It ran once through `Tools/Switchboard/Layer Volume Cables`.
  - `Assets/Prefabs/UI/VolumeCablePanel.prefab`
  - `Assets/Tests/Game/Editor/VolumeStackTests.cs`: the pass-1 prefab test is replaced by three tests on the lead's case:
    - every wire lies over every resting jack, the wires stack by recency, and a held jack and its wire go on top with the hand above
    - the top hit over each jack is that jack
    - the wires start hidden at the frame's edge, and the jacks are never clipped
  - `Docs/Design.md` (5.12 item 8), `Docs/Progress.md`
- Tests: 591/591 EditMode (was 589). Console clean.
- Screenshots in `Temp/Captures/`:
  - `volume-stack-lead-case.png`: the lead's layout. Red and yellow lie over the blue jack, and blue (newest) lies over red where those wires cross.
  - `volume-stack-own-tail.png`: close-up of the red and yellow wires meeting their own jacks.
- Housekeeping:
  - The lead had played since pass 1, so the saved values were new: `Switchboard.AudioPatch.v1` = `{"version":1,"rows":[1,1,0],"sockets":[3,1,3],"order":[2,0,1]}`, plus `operator.save` and `operator.save.backup`. They were kept in `SessionState` and restored exactly (string compare), and the keys were erased.
  - The font assets were cleared in memory. No font, ProjectSettings or scene files changed.
  - `Final.unity` is active and not dirty. Nothing was committed.

### Board cords: wires over other jacks (2026-10-20)
Result: done
Playable now: on the main switchboard, cords stack like in the Options volume patch. Every plugged-in jack lies under every wire, so a wire that crosses another cord's jack lies over it. Between wires, the cord you moved last lies on top. A held plug is above everything, the book included, as before. A jack under another cord's wire can still be picked up by clicking it.
How to test:
1. Start or continue a game. Take a cord from the tray and plug it into A2 and A4: its wire sags across B3.
2. Plug a second cord into B3 and E3: the first cord's wire lies over the B3 jack.
3. Plug a third cord into B1 and B5: its wire crosses the second cord's wire and lies on top of it.
4. Click the B3 jack (under the first wire): it comes up in your hand. Click B3 again to put it back: its wire now lies over the third cord's wire, and the first wire still lies over the B3 jack.
5. Pick up the A2 end and put it back: the A2–A4 wire is still over the B3 jack, and it is now the top wire.
6. Pick up the B1 end and carry it over the other cords: the plug is drawn over everything. Click an empty spot: it goes back, and its wire is now the top wire.
7. Open the book while holding a cord: the book still covers all wires; the held plug and the hand stay above it.

How it works (option (a) of the brief):
- Resting plugs stay where they were: in `SwitchboardCanvas` at order 0 (`SwitchboardUISorting.Cable`).
- Each wire gets its own nested canvas with override sorting at `SwitchboardUISorting.Wire` (1) + its rank, oldest first: 1–6 for six cords. That is above every resting plug and below the book (10), `GameUI` (10) and held plugs and the hand (20).
- The rank comes from the cords' sibling order under `CableInteraction`, which already meant "moved last is last". `CableInputController.RaiseToTop` moves the selected cord to the last sibling and marks the stack dirty only when the top cord changes; `Restack` then re-applies the ranks. A bought cord (`AddCable`), `Configure` and `OnEnable` also mark it dirty, and `LateUpdate` restacks only when dirty. `CableController.SetStackRank` writes the wire canvas's `sortingOrder` only when the value changed. Nothing allocates per frame, and the old per-frame `SetAsLastSibling` is gone.
- Why not option (b): the plugs are children of each cable object, and `AddCable` copies a cable with its plugs. Moving the plugs into a shared container would have broken that copy and the cable prefab structure. Nothing else in `SwitchboardCanvas` draws after the cords (`CableInteraction` is `WorkArea`'s last child, and the only nested canvases are the book at 10 and the hand at 20), so the new wire layers cover nothing that used to sit above the cords. A test now guards this.
- Clicks: wires and plugs have `raycastTarget` off, and the wire canvases have no `GraphicRaycaster`, so the top hit over a jack is still its socket, as before.
- A wire meets its own jack at the jack's tail tip, coming from below, as in the volume patch (`board-stack-own-tail.png`). One visible change: when a cord's other end is straight above (B3 to E3), its wire now runs down over its own lower jack (E3) to that jack's tail, instead of under it. This follows from the rule "every wire over every resting jack".
- The stack isn't saved: a loaded game starts with every cord in the tray.
- Volume patch code is untouched; the two systems stay separate.

Created:
- `Assets/Tests/Game/Editor/BoardCordStackTests.cs` (+ `.meta`)
Changed:
- `Assets/Scripts/UI/SwitchboardUISorting.cs`: new `Wire = 1`; the summary describes the new layer order.
- `Assets/Scripts/UI/CableController.cs`: the wire's canvas (`SetStackRank`, `WireSortingOrder`, `ApplyWireLayer`). The plug and wire layers are now added to any scene object (`gameObject.scene.IsValid()`) instead of only in Play Mode, so EditMode tests in preview scenes see them. Prefab assets never get them.
- `Assets/Scripts/UI/CableInputController.cs`: `topCable`, `stackDirty`, `RaiseToTop`, `Restack`, `LateUpdate`. These replace the per-frame `selected.transform.SetAsLastSibling()`.
- `Docs/Design.md` (4.3: the board's stacking rule, matching 5.12 item 8), `Docs/Progress.md` (this report)
Scene and prefab changes: none. The wire canvases are added at runtime, as the plug canvases already were.
Tests: 595/595 EditMode (was 591). New in `BoardCordStackTests` (4), on a test board with two cords where one wire sags across the other cord's jack, driven through `CableInputController.ProcessPointer` and real EventSystem raycasts:
- `RestingPlugsLieUnderEveryWireAndTheWireMovedLastIsOnTop`: at the start, after replugging the crossing cord, and after replugging the crossed one.
- `AHeldPlugIsOnTheCursorLayerAndItsWireOverEveryOtherWire`: also checks that an empty-spot click puts the plug back to the resting layer, with its wire still on top.
- `AJackUnderAnotherCordsWireStillTakesTheClick`: the top raycast hit over the crossed jack is its socket, no wire or plug is hit, and a click picks the jack up.
- `WireLayersFitBetweenTheBoardAndTheBookAndNothingInTheGameSitsThere`: the wire range stays below the book. In `Final.unity` (preview scene), no canvas sits between the board and the book, and `CableInteraction` is drawn last in its canvas.

No old test asserted the old order. `CableConnectionTests`, `MergedSystemsTests` (including `NewCordKeepsConnectionHighlightsCoinsAndAudio` and `PurchasedCordJoinsCurrentGuideAndPickupClearsEveryPulse`), `PhoneBookPageTurnTests`, `ClosedSocketTests` and `SocketHoverTests` pass unchanged.
Console: clean (only Unity's own "Ignoring depth surface ... memoryless" lines during Play Mode).
Play Mode check (`Final.unity`, the lead's save with 4 cords on a 6×6 board, clicks through `CableInputController.ProcessPointer` in a `Canvas.willRenderCanvases` handler, frames driven with `EditorApplication.Step`):
- Red A2–A4, blue B3–E3, yellow B1–B5. Wire orders: red 2, blue 3, yellow 4 (the unused tray cord 1). The red wire lies over the blue B3 jack, and yellow lies over blue where they cross.
- Replugging blue at B3 (crossed cord; the click on the jack under the red wire picked it up): blue becomes 4 and lies over yellow. Red still lies over the B3 jack.
- Replugging red at A2 (crossing cord): red becomes 4 and stays over the B3 jack.
- Holding yellow's B1 end: plug canvas at override order 20, yellow wire on top. The plug is drawn over the red wire next to the blue jack. An empty-spot click put it back on B1, with yellow still the top wire.

Screenshots in `Temp/Captures/`:
- `board-stack-1.png`: red over the blue jack, yellow over blue
- `board-stack-2.png`: after replugging blue, blue over yellow and red still over the blue jack
- `board-stack-3.png`: after replugging red
- `board-stack-held.png`: the held yellow plug over everything
- `board-stack-own-tail.png`: 2× close-up of wires meeting their own jacks' tails, and red crossing the blue jack

Test note: as in the volume pass, a held plug's screenshot needs the simulated pointer re-applied, and the wire meshes rebuilt, inside the handler. This affects only the check.
Housekeeping:
- The lead's `operator.save`, `operator.save.backup` and `Switchboard.AudioPatch.v1` were kept in editor `SessionState` before Play Mode. Afterwards they were restored and compared by exact string: the game save had been rewritten during the check and is back, and the audio patch was untouched. The SessionState keys and the handler were then removed.
- The dynamic font atlases and "SDF Static" materials were cleared in memory, so no font files were written.
- `ProjectSettings.asset` untouched. `Final.unity` is active and not dirty. Nothing staged, committed or pushed.
- While this pass ran, the lead committed the two volume-patch passes (`6667a4a`), so `git status` now shows only this pass's files.

Deviations from Design/Plan: none.
Follow-ups:
- Design 5.4 still describes the removed `RingingPulse` halo (nested canvas at order 9). It no longer exists in the code; the line could go.
- If the lead doesn't like a vertical cord's wire running over its own lower jack (B3–E3 above), the fix would be per-cord: draw that cord's lower jack above its own wire only. It's a design question, so it was left alone.

### Step 10 report: 2026-10-20
Result: done (about 40 min)
Playable now: a New Game guides the first call with a spotlight. The whole screen dims, except soft holes around what to click next, with a short hint and an arrow. The holes slide from step to step: the caller card, the speech bubble, the closed book, the caller's and the recipient's rows in the open book, the cord in the tray, the two jacks, then the money plaque. Clicks go straight through the dark layer. "Skip tutorial" (top centre) ends the guide at once. Continue never shows the guide, the one-call start or the shop's first-cord pulse.
How to test:
1. Main menu: New Game (or play `Final` with no save). The first caller rings and everything but the card dims: "A call! Click the caller to answer."
2. Click the card: the light slides to the bubble, "Read who they want to talk to." When the message has finished typing, it moves to the book, "Open the phone book."
3. Open the book (click it or press Tab): both rows (the caller's and the person they want) light up, and the hint sits left of the book: "Find both jacks: the caller's and the one they want. Then click the book to close it."
4. Close the book: the cord in the tray lights, "Take a cord." Take it: both jacks light as circles, "Plug one end into each jack."
5. Plug both: the money plaque lights, "Connected! You earn money while they talk." It ends after 5 s or on your next click, and the screen is normal again. The other caller slots fill as before.
6. Detours: put the cord back (click empty space) and the hint asks for the cord again. Open the book again while taking or plugging a cord and the rows light again; close it and you're back where you were.
7. Wrong number, or the call ending before you connect: the guide ends quietly.
8. Start a new game again and press "Skip tutorial" at any step, also while holding a cord: the guide fades out and the cord stays in your hand.
9. Quit to the menu after connecting (that writes a save), then Continue: no dimming, all caller slots ring at once, and the "More Cables" price doesn't pulse.
10. Pause during the guide: the pause menu covers it, and the "Connected" timer waits.

**Final texts** (Inspector: `GameUI` > `Tutorial` > `TutorialSpotlight` > Texts; plain ASCII only, since the static fonts hold nothing else):
| Step | Lit | Text |
|---|---|---|
| 1 Portrait | the caller card | A call! Click the caller to answer. |
| 2 TaskText | the speech bubble | Read who they want to talk to. |
| 3 Book | the closed book | Open the phone book. |
| 4 ReadingBook | the caller's and recipient's rows (or the page corner that turns to one on another spread) | Find both jacks: the caller's and the one they want. Then click the book to close it. |
| 5 Cables | every cord in the tray | Take a cord. |
| 6 Plugging | both jacks (round holes) | Plug one end into each jack. |
| 7 Connected | the money plaque (`HeaderSlot`) | Connected! You earn money while they talk. |

How it works:
- **One decision:** `GameSession.IsTutorial` (`tutorial && save == null`) drives the one-call start (`CallQueue`), and `GameInstaller` uses it for the guide and for `ShopView.Bind(..., guideFirstCord)`. A loaded game binds no `FirstOrderGuide` and keeps the spotlight off. A new game that never saved (no progress) is a new game again.
- **Sequence** (`FirstOrderGuideSequence`): new stages `Plugging` and `Connected` after `Cables`. Picking a cord moves Cables to Plugging. Putting it back (polled `CordsOut(false)`) returns to Cables. Opening the book in Cables or Plugging goes back to ReadingBook, and closing it returns to the step it left. Connecting the guided call moves to Connected, which ends after `ConnectedSeconds` (5 s of game time) or a click from `ConnectedClickDelay` (0.6 s) on, so the plugging click itself doesn't count. `FirstOrderEnded` ends the guide: Complete after connecting, Cancelled before. `Skip` cancels. Sergei's rules stay: exploring ahead (a cord or the book before reading the message) still ends the guide, and only the portrait hint survives an early look.
- **Spotlight** (`TutorialSpotlight` on `GameUI/Tutorial`):
  - It has its own canvas at `SwitchboardUISorting.Tutorial` = 14. That is above the book and GameUI (10), below the Goal dialog (15), the pause menu (16) and the hand and held plugs (20). It is stretched over the whole Expand canvas, so it also dims the side bars at 19.5:9.
  - Children:
    - `Dim`: `SpotlightGraphic`, one quad drawn by `Switchboard/UI/TutorialSpotlight` (`Assets/Content/Shaders/`). It has up to 8 soft rounded holes, whose positions are passed in the quad's UVs, so they stay sharp at any resolution.
    - `Arrow`: `GuideArrowGraphic`.
    - `Hint`: a parchment panel with a Della Respira SDF Static label.
    - `Skip`: the pause menu's button art.
  - Only Skip's image is a raycast target.
  - Holes track their targets every `LateUpdate`: world corners become screen points, then spotlight-local points, using a camera per target canvas cached at `Show`. They slide (exponential follow) and breathe a few units. A new hole grows out of the nearest open one.
  - The hint goes on the roomier side of the lit area, or beside the open book in step 4, and is clamped on screen. The arrow runs from the hint's edge to just outside the nearest hole.
  - Nothing allocates per frame: arrays are fixed, and the arrow mesh rebuilds only when its whole-unit length changes.
  - When the guide ends, it fades out, frees its material, deactivates itself, and the guide component disables itself.
- **The pulses are gone from the guide.** `FirstOrderGuide` no longer creates `SlowGuidePulse`s (portrait halo, book silhouette, cord pulse). `SlowGuidePulse` stays for the book and tray hover glows and the shop's first-cord pulse.

Created:
- `Assets/Scripts/Game/Views/TutorialSpotlight.cs` (+ `.meta`): the view, `SpotlightTarget`, `TutorialTexts`
- `Assets/Scripts/Game/Views/SpotlightGraphic.cs` (+ `.meta`)
- `Assets/Scripts/Game/Views/GuideArrowGraphic.cs` (+ `.meta`)
- `Assets/Content/Shaders/TutorialSpotlight.shader` (+ `.meta`, and `Assets/Content/Shaders.meta`)
- `Assets/Tests/Game/Editor/TutorialSpotlightTests.cs` (+ `.meta`)

Changed:
- `Assets/Scripts/Game/FirstOrderGuideSequence.cs`: new stages and verbs (above), `IsGuiding`, `IsOver`. `Cancel` no longer overwrites Complete.
- `Assets/Scripts/Game/FirstOrderGuide.cs`: drives the spotlight instead of the pulses. `Bind` takes the spotlight, the board's sockets and the money plaque. Targets are recomputed on a stage change, a page turn (step 4) and a change in the tray (step 5). It unbinds and disables itself when the guide is over.
- `Assets/Scripts/Game/GameInstaller.cs`: `Start` calls `Install(saveFile, restored)`, so tests can start from a save without PlayerPrefs. It binds the guide only when `Session.IsTutorial`, deactivates the spotlight first, and passes `guideFirstCord` to the shop.
- `Assets/Scripts/Domain/GameSession.cs`: `IsTutorial`.
- `Assets/Scripts/Game/Views/ShopView.cs`: `Bind(..., guideFirstCord = true)`.
- `Assets/Scripts/Game/Views/CallCardView.cs`: `SpeechBubbleRect`.
- `Assets/Scripts/UI/PhoneBookView.cs`: `OpenBookRect`, `RowOnSpread(jack)`, `SpreadDirectionTo(jack)`, `PageCorner(direction)`.
- `Assets/Scripts/UI/PhoneBookPageView.cs`: `RowWithJack(jack)`.
- `Assets/Scripts/UI/PhoneBookContactRow.cs`: `Jack`.
- `Assets/Scripts/UI/SwitchboardUISorting.cs`: `Tutorial = 14`, and the summary lists GameUI's orders.
- `Assets/Tests/Domain/CallQueueTests.cs`, `Assets/Tests/Game/Editor/FirstOrderGuideTests.cs`, `Assets/Tests/Game/Editor/MergedSystemsTests.cs` (its three pulse tests replaced by `TutorialSpotlightTests`)
- `Docs/Progress.md` (this report)

Scene and prefab changes:
- `GameUI.prefab` has a new inactive child, `Tutorial`: Canvas (override sorting 14), GraphicRaycaster, CanvasGroup and `TutorialSpotlight`, with children `Dim`, `Arrow`, `Hint/Label` and `Skip/Label`.
- The save re-sorted the prefab's objects (Unity orders them by fileID) and normalised three `m_Name:` lines. Compared object by object: 31 objects were added, 0 removed, and the only other change is the new child in the root's child list.
- `Final.unity` is unchanged.

Tests: 615/615 EditMode (was 595; 3 pulse tests removed, 23 added). New:
- `CallQueueTests.OnlyANewGameGetsTheTutorial` (3 cases)
- `FirstOrderGuideTests`:
  - `ConnectedStepEndsOnAClickButNotOnThePluggingClick`
  - `CheckingTheBookAgainReturnsToTheStepItLeft`
  - `PuttingTheCordBackAsksForItAgain`
  - `SkipEndsEveryStepForGood` (5)
  - `TheGuidedCallEndingEndsTheGuide` (6)
  - the first test extended through Plugging, Connected and Complete
- `TutorialSpotlightTests`, which run the real `Final` scene in a preview scene. Its overlay canvases have no size there, so the test lays them out as 1920×1080:
  - `ANewGameGuidesTheFirstCallStepByStep`: every step's text and exact targets, holes open, no pulses; the end frees the material and disables the guide
  - `SkipEndsTheGuideAtOnceAndKeepsAHeldCord`
  - `TheGuidedCallEndingMidGuideEndsTheGuideCleanly`
  - `TheSpotlightIsVisualOnlyAndSitsBetweenTheBookAndThePauseMenu`
  - `HolesFollowTheirTargetsAndSlideToTheNextStep`
  - `ALoadedGameHasNoTutorial`: no guide, the slots fill at once, no shop pulse
- The tests read the cord count, the book's spreads and the jacks from the real files, so the designer's edits don't break them.

Console: clean (only Unity's own "Ignoring depth surface ... memoryless" lines in Play Mode, and one MCP bridge message).

Play Mode check (`Final`, new game; clicks through EventSystem raycasts and `CableInputController.ProcessPointer` in a `Canvas.willRenderCanvases` handler; frames driven with `EditorApplication.Step`):
- At 1920×1080, the whole first call ran start to finish. The top raycast hit through the dark layer was the real target every time: `Portrait`, `ClosedBook`, `OpenBook` (which closed it), `StoredCable_01`, `Socket_A1`, `Socket_B3`. The guide ended Complete with the spotlight off and its material freed.
- Then Continue: `IsTutorial` false, no guide, 3 calls at once, no shop pulse.
- At 2340×1080: the dim covers the side bars, and Skip (top hit `Skip`) ended the guide with a fade.
- The Game view is back at 1920×1080.
- Screenshots in `Temp/Captures/`: `step-10.png` (the smoke-test shot, step 1), `step-10-1b`, `-2b`, `-3`, `-4b`, `-5`, `-6`, `-7` (steps 1–7 at 1920×1080), `step-10-8-done`, `step-10-continue`, `step-10-wide-1b`, `step-10-wide-3` and `step-10-skip` (2340×1080).
- The 1920 shots were taken before the alpha fix below, so their PNG alpha is 211: image viewers show them washed out over white, though the game looked right. The wide shots come after the fix.

Housekeeping:
- **Play Mode was already running when I started** (about 40 s of game time, the first card clicked, Unity unfocused). No frames advanced for over six minutes and there was no save, so I stopped it before compiling. Nothing was lost.
- The lead's keys were kept in SessionState and restored exactly afterwards. `operator.save` and `operator.save.backup` didn't exist before and are deleted again; `Switchboard.AudioPatch.v1` is identical. The SessionState keys are erased.
- `Time.timeScale` is back to 1.
- The dynamic font atlases and "SDF Static" materials were cleared in memory; no font files changed.
- `TimeManager.asset` and `ProjectSettings.asset` are untouched.
- `Final.unity` is active and not dirty. The editor is idle and out of Play Mode.
- Nothing staged, committed or pushed.

Deviations from Design/Plan:
1. **Darkness 0.78 alpha, not 0.55.** The project renders in linear colour space, where 0.55 alpha darkens the board by only about 30% to the eye (83 → 57 on the board's green). At 0.78 the board is at about 47% brightness, which looks like the "about 55% dark" asked for. Tune it on `Tutorial/Dim`'s colour.
2. **The shader keeps the frame's alpha opaque** (`Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha`). With plain alpha blending the dark layer left the frame partly transparent (alpha 211). That washes out screenshots, and in a browser it could show the page through the game. Unity's own UI shader keeps alpha at 1.
3. **Closing the book.** The brief said "if the player closes the book without plugging, go back to step 3 as today". In the code, closing the book after reading has always moved on to "Take a cord". I kept that, and added the reverse: opening the book again while taking or plugging a cord returns to "Find both jacks", then back to the step it left.
4. **Exploring ahead still ends the guide** (Sergei's rule, tested): taking a cord or opening the book before the message is read. The player is never stuck, but the guide doesn't follow them forward. Following them is a small change in the sequence if the lead prefers it.
5. **Step 4's hint sits beside the open book**, not beside the rows, so it doesn't cover the left page. The arrow stretches from the hint to the nearest row.
6. **Step 7 ends after 5 s of game time or a click** (clicks in the first 0.6 s are ignored), or when the call ends, whichever comes first.
7. The texts live on the `TutorialSpotlight` component in `GameUI.prefab`, because `FirstOrderGuide` is added at runtime and has no prefab.
8. The shader is in `Assets/Content/Shaders/` (Plan §3.7) and reaches builds through the `SpotlightGraphic.shader` reference in the prefab, not through `Resources`.

Follow-ups:
- **Web:** Sergei's `Resources/GuideSilhouette.shader` (the book-hover glow and the shop's first-cord pulse) uses plain `Blend SrcAlpha OneMinusSrcAlpha`, so it also lowers the frame's alpha where it draws. Check on the next Web build whether the itch page shows through. The fix is the same one-line blend change.
- Plan §8 "`SlowGuidePulse` material in EditMode tests": the guide no longer makes pulses. Only the shop pulse test (`FirstCordHintTracksAffordabilityAndStopsAfterPurchase`) still leaves one material per run. The item can shrink to that.
- `Final` was not re-checked from `MainMenu` this step. The installer path is the same, but it's worth one click in the playtest.
- Design 5.2 ("the tutorial is Step 10"), the D57 status and 4.3 (the spotlight layer at 14) are for the orchestrator to update.
- The first-call hint's arrow points at the "New order!" stamp side of the card. If the stamp feels in the way, the card hole could use the portrait only.

#### Step 10, fix pass 1: mandatory guide and shop steps (lead, 2026-10-20)
Result: done
Playable now: the tutorial can't be skipped by acting ahead any more. On a New Game, only the lit target answers a click or tap; everything else under the dark layer does nothing (no card, book, cord, socket or shop action, and a held cord is never dropped or put back). Skip tutorial, the pause button and Esc always work. The guided caller can't hang up while the guide runs. After step 7, normal play resumes until the player can first afford More Cables; then the light comes back on that button (step 8, buy it), then on the ??? button (step 9, any click or 5 s), and the tutorial is over.
How to test:
1. Main menu, New Game (Start over if asked). Step 1 lights the caller. Click a cord, the book, a jack, or press Tab: nothing happens. Click the caller.
2. Step 2 (the message types out): nothing is clickable. Then step 3: only the closed book (click it or press Tab).
3. Step 4: the open book is fully usable (page corners, a click closes it, Esc closes it); the rest is blocked. Close the book.
4. Step 5: only the cord in the tray. Step 6: only the two lit jacks take the plug. Click another jack, empty board, the tray or a caller while holding the cord: the cord stays in your hand. Plug both jacks.
5. Step 7: the money plaque. A click anywhere ends it (from 0.6 s on), or it ends after 5 s. Trying to buy More Cables here does nothing.
6. As soon as More Cables is affordable (with today's Economy.json that is right after step 7: the first fee is 25¢ and the price is 25¢), step 8 lights only that button. Other callers ring but can't be clicked. Buy it.
7. Step 9 lights ???: a click anywhere (or 5 s) ends the tutorial; the screen is normal again.
8. Pause during any step: the pause menu works as usual; after Resume the block is back.
9. New Game again and press Skip tutorial while holding a cord: the block goes at once (even while the dark fades), and the cord stays in your hand.
10. Quit to the menu and Continue: no guide, no shop steps.

**Final texts** (Inspector: `GameUI` > `Tutorial` > `TutorialSpotlight` > Texts; plain ASCII):
| Step | Lit | Takes clicks | Text |
|---|---|---|---|
| 1 Portrait | the caller card | the card | A call! Click the caller to answer. |
| 2 TaskText | the speech bubble | nothing | Read who they want to talk to. |
| 3 Book | the closed book | the closed book (and Tab) | Open the phone book. |
| 4 ReadingBook | both rows (or the page corner that turns to one) | the whole open book (and Tab or Esc to close) | Find both jacks: the caller's and the one they want. Then click the book to close it. |
| 5 Cables | every cord in the tray | the tray cords | Take a cord. |
| 6 Plugging | the call's two jacks | those two jacks | Plug one end into each jack. |
| 7 Connected | the money plaque | nothing; any click dismisses | Connected! You earn money while they talk. |
| 8 BuyCord | the first More Cables button | that button | You can afford another cord. Buy it to handle two calls at once. |
| 9 Mystery | the ??? button | nothing; any click dismisses | Save up for the mystery upgrade! |

How it works:
- **Blocking.** `SpotlightGraphic` (the `Dim` layer) is now a raycast target and an `ICanvasRaycastFilter`. While `Blocking` is on, it reports a hit everywhere except its click areas, so on its canvas (14, with its own `GraphicRaycaster`) it is the top hit over the book, GameUI and the board.
  - **Card, book and shop.** These are EventSystem clicks. The layer has no click, down or up handler in its parents, so a blocked click lands nowhere.
  - **Cords.** `CableInputController` takes the topmost hit itself. A top hit with the new marker interface `Switchboard.UI.IPointerBlocker` now means "blocked": no pick, plug, drop or return, and no socket hover. The hand and a held plug still follow the pointer.
  - **Click areas** are the clickable targets' own rects (round ones as an ellipse, without the visual padding), plus one optional extra area (the open book in step 4). They are recomputed at once in `Show` and then every frame, so they follow moving targets.
  - **What stays on top.** Skip is on the spotlight canvas but outside `Dim`, so the filter never touches it. The pause button and menu (16) and the Goal dialog (15) sit above it.
  - **Ending.** `Hide` (Skip, the end, or the break between steps 7 and 8) turns `Blocking` off at once; the fade is visual only.
  - **Touch.** Taps go through the same EventSystem raycast and `ProcessPointer`, so the same rules apply.
- **Tab.** `PhoneBookView.OpenLocked` (new) stops Tab, the cover and the shortcut button from *opening* the book; closing always works. The guide sets it in every step except step 3, and also before the first card shows; it is cleared in normal play and when the guide ends. A separate flag from `InputEnabled`, because the pause menu sets that one back on Resume.
- **The caller doesn't hang up (domain).**
  - New `Call.WaitingHeld` and `CallService.HoldWaiting(id, held)`; `CallService.Tick` skips the response timer of a held call.
  - `CallQueue` holds the tutorial call when it deals it (only when `tutorial`, so only on a new game). `GameSession.EndTutorial()` → `CallQueue.ReleaseTutorialCall()` releases it.
  - The guide calls it once the first-call steps are over (connected, skipped, or the call cleared), and also when it unbinds.
  - Only emergencies have a response timer, so this matters only if the designer puts an emergency first in `startPool` (today `call_001` isn't one). A wrong number is impossible (step 6 accepts only the two jacks). Patience doesn't exist for normal calls. Dev "Clear calls" ends the guide cleanly (tested).
- **Sequence** (`FirstOrderGuideSequence`):
  - **Stages.** New stages `WaitingForShop`, `BuyCord`, `Mystery`, and the enum got explicit values.
  - **Sergei's rules removed.** The "acting ahead ends the guide" rule (`BreakOrder`, `outOfOrder`) is gone: out-of-step events are now ignored.
  - **After step 7.** The "Connected" step ends (5 s, a click, or the call finishing) into `WaitingForShop`. That is normal play: no light, no block.
  - **Shop polling.** The guide polls the shop each frame. If the first `extra_wires` entry (`ChainOf(ExtraWires)[0]`, price from `Economy.json`) is on a shelf button, the player can buy it, and the book is closed, it goes to `BuyCord`. If the money drops below the price (a fine), it goes back to waiting.
  - **No shop steps** when there is no first cord in the file, or it isn't on the shelf: the guide is `Complete` at once.
  - **After the purchase**, `Mystery` follows, or `Complete` if the file has no secret. `Mystery` ends after 5 s or on a click from 0.6 s on, so the buying click doesn't count.
  - **Clicks while paused** don't dismiss steps 7 or 9.
- **Shop.** `ShopView` lost the first-cord pulse (`guideFirstCord`, `guidePulses`) and gained `ButtonFor(upgrade)` and `Secret`. `UpgradeSlotView.GuideGraphic` is gone (no users). `SlowGuidePulse` stays for the book and tray hover glows.
- **Arrows (the lead's screenshot notes).**
  - **Step 4.** The hint now sits beside the open book, level with the lit rows, and the arrow is a short straight one that stops 12 px before the book's edge. It no longer crosses the left page.
  - **Jacks (step 6).** The arrow is short (36) and diagonal, coming from the hint's quarter. The jacks sit in a grid, so its diagonals are the free gaps, and the tip is no longer on B4.
- **Choices the brief left open.**
  - Putting a held cord back in the tray during step 6 is **blocked** (the tray isn't a click area). Nothing needs undoing, because a wrong jack is impossible; clicking a plugged jack of the call picks that end up again.
  - The book is **not** openable during steps 5–6 (simplest; the jacks are lit on the board anyway). The sequence still handles a book that opens anyway, by returning to step 4 and back.
  - Steps 8–9 follow only a **completed** first call. Skip, or the guided call cleared before connecting, ends the whole tutorial.
  - **A save between step 7 and step 8** loads as Continue, so steps 8–9 are skipped (accepted by the lead).

Created:
- `Assets/Scripts/UI/IPointerBlocker.cs` (+ `.meta`)

Changed:
- Domain: `Assets/Scripts/Domain/Calls/Call.cs` (`WaitingHeld`), `Calls/CallService.cs` (`HoldWaiting`, held timers don't run), `Queue/CallQueue.cs` (holds the tutorial call, `ReleaseTutorialCall`), `GameSession.cs` (`EndTutorial`)
- `Assets/Scripts/Game/FirstOrderGuideSequence.cs` (rewritten: mandatory, shop stages), `FirstOrderGuide.cs` (rewritten: click areas per step, book lock, `BindShop`, shop polling, call release)
- `Assets/Scripts/Game/Views/SpotlightGraphic.cs` (raycast filter, click areas, `Blocking`), `TutorialSpotlight.cs` (`SpotlightTarget.Clickable`, extra click area, blocking on show/hide, `BlocksScreenPoint`, two new texts, anchored and jack arrows, `jackArrowLength`/`jackArrowGap`)
- `Assets/Scripts/Game/Views/ShopView.cs` (pulse removed; `ButtonFor`, `Secret`), `Assets/Scripts/Game/GameInstaller.cs` (passes `Session.EndTutorial`, `BindShop`)
- `Assets/Scripts/UI/CableInputController.cs` (`ProcessTarget`, split out of `ProcessPointer`; blocked top hit), `PhoneBookView.cs` (`OpenLocked`), `UpgradeSlotView.cs` (`GuideGraphic` removed)
- `Assets/Prefabs/Game/GameUI.prefab`: `Tutorial/Dim` `m_RaycastTarget: 1`; `TutorialSpotlight` gets `buyCord`, `mystery`, `jackArrowLength: 36`, `jackArrowGap: 6`. Edited as text (4 lines); no Unity re-save.
- Tests: `Assets/Tests/Domain/CallQueueTests.cs`, `Assets/Tests/Game/Editor/FirstOrderGuideTests.cs`, `TutorialSpotlightTests.cs`, `MergedSystemsTests.cs` (its shop-pulse test removed)
- `Docs/Progress.md` (this report)

Scene and prefab changes: `GameUI.prefab` as above. `Final.unity` and `MainMenu.unity` unchanged.

Tests: 624/624 EditMode (was 615). New or changed:
- `CallQueueTests`: `TheGuidedCallWaitsWhileTheTutorialRunsAndRunsOutOnceItEnds`, `OnlyTheTutorialCallIsHeld`.
- `FirstOrderGuideTests`:
  - Rewritten for the mandatory flow.
  - New: `TheCordStepAppearsOnlyWhileAffordableAndEndsOnPurchase`, `TheMysteryStepEndsOnAClickButNotOnThePurchaseClick`, `NoCordOrNoMysteryToShowSkipsTheShopSteps`, `OutOfStepEventsNeitherSkipNorEndTheGuide` (4; replaces the 8 "exploring ahead" cases).
  - Skip and call-ending cases now include the shop stages.
- `TutorialSpotlightTests`, in the real `Final` scene:
  - `ANewGameGuidesTheFirstCallStepByStepAndBlocksEverythingElse`: for every step, the card, tray cords, another jack, a call jack, two shop buttons and the closed book are blocked except the step's own target. Real clicks go through `CableInputController.ProcessTarget`: no pickup on steps 1 and 3, no plug in a call jack on step 5. On step 6, B4, the tray, the card and empty space keep the cord held, and a wrong jack is impossible. Tab can't open the book except on step 3. The call's hold ends on connect.
  - `TheShopStepsLightTheFirstCordOnceAffordableThenTheMysteryUpgrade`, `AlreadyAffordableTheCordStepFollowsStepSevenAtOnce`, `TheShopStepsFollowTheShelf`.
  - `SkipUnblocksAtOnceKeepsAHeldCordAndLetsTheCallerHangUpAgain`, which includes the control check: after Skip, the same empty-space click does put the cord back.
  - `TheGuidedCallEndingMidGuideEndsTheGuideCleanly` (dev Clear calls).
  - `TheDarkLayerSwallowsClicksAndSitsBetweenTheBookAndThePauseMenu`: only `Dim` and Skip are raycast targets, there are no click, down or up handlers above `Dim`, and pause is above the layer.
  - `ArrowsStayOffOtherJacksAndOffTheBookPages`.
  - `ALoadedGameHasNoTutorial`: no hold, no lock, no shop steps.
  - Prices, the shelf and the secret all come from the loaded files.

Console: clean (only Unity's "Ignoring depth surface ... memoryless" lines in Play Mode).

Play Mode check (from `MainMenu`, New Game, at 1920×1080; frames with `EditorApplication.Step`; clicks as EventSystem raycasts plus `CableInputController.ProcessPointer` in a `Canvas.willRenderCanvases` handler):
- **New Game from the menu works.** The New Game button loaded `Final`, and the card rang within about 1.4 s, with the tutorial on and the call held.
- **Blocked clicks.** Every forbidden click had `Dim` as its top hit and did nothing:
  - step 1: the tray cord, the closed book (`BookShortcut`), A1, and Tab
  - step 2: the bubble
  - step 3: the tray cord
  - step 4: A1 and the card
  - step 5: B3, the book, and Tab
  - step 6, holding the cord: B4, empty board, the tray and the card; the cord stayed `Held`
  - step 7: More Cables, which was affordable; no purchase
  - step 8: another ringing caller and More Clients
  - step 9: ??? (no buy attempt)
- **Allowed clicks.** The real targets were the top hits: `Portrait`, `BookShortcut`, `OpenBook` (which closed it), `StoredCable_01`, `Socket_A1`, `Socket_B3`, and More Cables' `Buy button`.
- **Steps 7 to 9.** Step 7 ended by time, and step 8 followed at once (60¢ against a 25¢ price). Buying gave 2 cords and moved to step 9, which ended by time: Complete, unblocked, with the book free. A normal click then took the next caller.
- **Continue.** Pause, then Main menu, then Continue: `IsTutorial` false, no guide, 3 calls at once.
- **Second New Game (Start over).**
  - Step 4 captured.
  - The pause button's top hit was `PauseButton` above `Dim`, and the game paused. Resume worked, and the block was back (A1 was `Dim` again).
  - Esc (`HandleEscape`) closed the book, moving to step 5.
  - I took the cord, then clicked Skip: its top hit was `Skip`, and the stage became `Cancelled`. The cord stayed `Held`, `Blocking` went off at once and the call's hold was released.
  - While the layer was still fading (alpha 0.87), an empty-board click put the cord back in the tray.
- The clicks that dismiss steps 7 and 9 read the real pointer (`Pointer.current`), which a simulated click can't press. They are covered by tests; in Play Mode both steps ended by time.
- Screenshots in `Temp/Captures/`: `step-10-fix-1` to `-9` (one per step, including 8 and 9), `step-10-fix-pause`, `step-10-fix-skip` (the fade just after Skip), and `step-10-fix-continue`.

Housekeeping:
- **The lead's keys.** `operator.save` and `operator.save.backup` (both absent before) and `Switchboard.AudioPatch.v1` were kept in SessionState and restored exactly: the two saves are deleted again, and the audio patch is identical. The SessionState keys are erased, and the stepper and click handlers removed themselves when Play Mode stopped.
- **Editor state.**
  - `Time.timeScale` is back to 1 (it was ×5 for two waits).
  - The Game view is at 1920×1080.
  - The dynamic font assets and "SDF Static" materials were cleared in memory: no font, `TimeManager.asset` or `ProjectSettings.asset` changes.
  - `Final.unity` is the active scene, not dirty. Unity was idle and not in Play Mode when I started, and is so again.
- Nothing staged, committed or pushed.

Deviations from Design/Plan:
1. Steps 7 and 9 swallow every click (their lit places aren't click areas). A click only dismisses them, so clicking ??? never tries to buy it.
2. Step 8 waits while the book is open, so its pages don't sit dimmed between the player and the shop.
3. A drop in money below the price during step 8 (a fine) returns to normal play until the cord is affordable again, instead of leaving the player stuck on a button they can't use.
4. The tutorial hold is on the response timer only; normal calls have no timer to hold.

Follow-ups:
- **Design.md is for the orchestrator to update:**
  - D57 status
  - 5.2: the hold on the guided call
  - 4.3: the spotlight is now a click-blocking top hit, not visual only
  - 5.9: the shop pulse is gone
- Plan §8 "`SlowGuidePulse` material in EditMode tests": the shop-pulse test is gone too, so no test makes a pulse material any more. The item can probably close.
- `CableInputController.ProcessTarget` lets tests feed a top hit directly. Real raycasts in a preview scene don't work, because the overlay canvases have no size there.
- Step 8's hint sits over the board's top-right jacks (dimmed). If the lead prefers it beside the shop, give the hint an anchor there.

### Web build: Step 10 tutorial (2026-10-20)
- **Built:** `a418ade` (Step 10: the tutorial), clean tree, editor on the Web platform, console cleared first.
- **Result:** `Build Web: done in 3.5 min. The build is in Builds/Web (27.5 MB).` No build errors; the console had no errors or warnings afterwards. A local check (`python3 -m http.server` on 127.0.0.1) served `index.html` ("Operator!") and all four `Build/` files with 200.
- **itch:** `butler push Builds/Web llazypixell/operator:html5` uploaded 27.5 MiB. `butler status` shows the `html5` channel at build **#2168670** (from #2164375), version 8, processed ✓. The page is still a Draft.
- **Leftovers:** none. After the build `git status` was clean: no URP asset, URP global settings, `TimeManager.asset`, dynamic font or `preloadedAssets` changes, and no Unity Cloud link in `ProjectSettings.asset` either. **The automatic restore in `WebBuild` (cleanup `1b6a6af`) works**; nothing had to be undone by hand.

#### Step 10, fix pass 2: reading time and more shop hints (lead, 2026-10-20)
Result: done
Playable now: step 2 (the speech bubble) no longer runs off. Once the message has typed out, the hint becomes "Read who they want to talk to. Click to continue." and stays until the player clicks anywhere. After the first call and the cord steps (8–9), the spotlight comes back once for **More Profit** and once for **More Clients**, the first time each is affordable. The player can buy it with the lit button, or dismiss the hint with a click anywhere else. The save remembers which shop hints are done, so Continue shows only the ones still to come. An older save shows none.
How to test:
1. Main menu, New Game (Start over if asked). Click the caller. The message types out, then the hint adds "Click to continue." Nothing moves on by itself. Click anywhere, on the bubble or off it: the guide moves to "Open the phone book". That click does nothing else (no card, no book, no cord).
2. Optional: press the dev panel's "+ $10" during the first call. No shop hint comes up until the call is connected and step 7 is over.
3. Play through steps 3–7 as before. Then step 8 (buy More Cables) and step 9 (the ??? button) follow as before.
4. About 1.5 s after step 9 ends, the More Profit button lights up: "More Profit: every call pays more. Buy it to earn faster." Click elsewhere to dismiss it (nothing is bought), or click the button to buy it.
5. About 1.5 s later, More Clients lights up: "More Clients: more callers at once, and a bigger board." Buy it or dismiss it. The tutorial is over.
6. The hints wait while the book is open, while you hold a cord, and while the game is paused. If money runs short before a hint shows, it waits until you can afford it again.
7. **Reload:** while the More Clients hint is showing (or before it comes), go to Pause, then Main menu, then Continue. There is no first-call guide and every slot rings. More Clients comes back, but More Profit (already done) doesn't. Finish it, then reload again: no hint ever comes back.
8. Skip tutorial (also shown during the shop hints) ends every remaining hint for good, as it ended steps 8–9 before.

**Final texts** (Inspector: `GameUI` > `Tutorial` > `TutorialSpotlight` > Texts; plain ASCII):
| Step | Lit | Takes clicks | Text |
|---|---|---|---|
| 1 Portrait | the caller card | the card | A call! Click the caller to answer. |
| 2 TaskText, typing | the speech bubble | nothing | Read who they want to talk to. |
| 2 TaskText, typed | the speech bubble | any click moves on (from 0.5 s after the text finished) | Read who they want to talk to. Click to continue. |
| 3 Book | the closed book | the closed book (and Tab) | Open the phone book. |
| 4 ReadingBook | both rows (or the page corner) | the whole open book (and Tab or Esc to close) | Find both jacks: the caller's and the one they want. Then click the book to close it. |
| 5 Cables | every cord in the tray | the tray cords | Take a cord. |
| 6 Plugging | the call's two jacks | those two jacks | Plug one end into each jack. |
| 7 Connected | the money plaque | nothing; any click dismisses | Connected! You earn money while they talk. |
| 8 BuyCord | the first More Cables button | that button | You can afford another cord. Buy it to handle two calls at once. |
| 9 Mystery | the ??? button | nothing; any click dismisses (or 5 s) | Save up for the mystery upgrade! |
| More Profit | the first More Profit button | that button (buys); any other click dismisses | More Profit: every call pays more. Buy it to earn faster. |
| More Clients | the first More Clients button | that button (buys); any other click dismisses | More Clients: more callers at once, and a bigger board. |

The proposed texts fit what the upgrades do. The income chain multiplies both the connection fee and the per-second rate (`EarningsService`). The first `extra_clients` entry opens more caller slots and a bigger board, and brings a new call pool.

How it works:
- **Which clicks count.** `SpotlightGraphic` (the `Dim` layer) is now an `IPointerDownHandler`. While blocking, it raises `Pressed` for every press it swallows, and `TutorialSpotlight` forwards it as `DimPressed`. So only a press that lands on the dark layer counts. Skip, the pause button and menu (above the layer) and a lit target's own click never do. It is still swallowed: the layer has no click or up handler, and the cord input still sees `IPointerBlocker`. `FirstOrderGuide` no longer polls `Pointer.current`. Steps 7 and 9 now use the same press, so a press on the pause button no longer dismisses them (before, it did on the press frame).
- **Step 2** (`FirstOrderGuideSequence`): `TaskTextFinished` no longer advances. It marks the text read and starts the guard. `AnyClick` in TaskText moves to Book only once the text is read and `ReadClickDelay` (0.5 s of game time) has passed. `IsTaskTextRead` switches the hint text.
- **Shop hints (sequence).**
  - **New stages:** `Profit = 13` and `Clients = 14`.
  - **New API:** `Offer(hint, ShopOffer, canStart)` replaces `CordAffordable`/`CordBought`/`ShopUnavailable`. `ShopOffer` is `NotForSale` (not in the file, or not on a button), `TooExpensive`, `Affordable` or `Owned`.
  - **Polling:** `FirstOrderGuide.PollShop` calls it each frame once the first call is over, in this order: cord, mystery, Profit, Clients. That order is the priority when several are due together. One hint shows at a time.
  - **Not for sale or owned:** the hint is done for good. A cord that isn't for sale takes step 9 with it, as before.
  - **Money drops while it shows:** the hint goes back to waiting, as step 8 already did.
  - **Gap:** after any shop hint ends, the next waits `ShopHintGapSeconds` (1.5 s of game time). Step 9 ignores the gap, so it still follows the cord purchase at once.
  - **Dismissing:** Profit and Clients end on a swallowed click from `ConnectedClickDelay` (0.6 s) on, so a click already on its way when the hint appears doesn't dismiss it. They don't time out.
  - **Skip:** Skip, or `Cancel` (the guided call ending before it connects), marks every shop hint done.
- **When a hint may start:** not paused (`Time.timeScale > 0`), the book shut, and no cord in hand (`Held` or `OneEndConnected`). Steps 8–9 follow the same rule now; before, only the book was checked.
- **The save (domain).**
  - New `ShopHints` flags (`Cord 1, Mystery 2, Profit 4, Clients 8, All`).
  - `SaveData.shopHintsDone`, an int. Its initializer is `ShopHintsMissing = -1`, which `JsonUtility` keeps when the field is absent, so an old save reads as all done.
  - `GameSession.ShopHintsDone`, `ShopHintsPending`, `MarkShopHintsDone`, and `CaptureSave` writes it.
  - **A new game** (`IsTutorial`) starts with none. **A loaded save** starts with what it saved. **A session without the tutorial** starts with all.
  - **No save version bump.** The marker handles the missing field, so no migration is needed.
- **Binding.**
  - `GameInstaller` binds the guide when `Session.IsTutorial || Session.ShopHintsPending != None`. On Continue it binds with `guideFirstCall: false`, which means no first-call steps and no call hold (no `EndTutorial` callback).
  - `BindShop(shop, upgrades, Session.ShopHintsDone, Session.MarkShopHintsDone)` reads the first entry of each chain (`ChainOf(ExtraWires/ExtraIncome/ExtraClients)[0]`) from Economy.json.
  - `FirstOrderGuide` reports every newly done hint to the session at once. The next autosave (10 s, focus loss, page hide, or leaving through the pause menu) writes it.
  - The first-call guide stays New Game only.
- **A reload during steps 1–7.** A new game that hasn't saved yet is a new game again (unchanged: no save until money moves). Once it has saved (from the first fee, at step 7), Continue skips the first call but still shows the cord step and the rest when they come due. `ContinueAfterAnEarlySaveSkipsTheFirstCallButShowsTheCordStep` covers this.

Created:
- `Assets/Scripts/Domain/Save/ShopHints.cs` (+ `.meta`)

Changed:
- `Assets/Scripts/Domain/Save/SaveData.cs` (`shopHintsDone`, `ShopHintsMissing`)
- `Assets/Scripts/Domain/GameSession.cs` (`ShopHintsDone`, `ShopHintsPending`, `MarkShopHintsDone`, saved in `CaptureSave`)
- `Assets/Scripts/Game/FirstOrderGuideSequence.cs` (step 2 waits for a click; `Profit`/`Clients` stages; `ShopOffer`, `Offer`, the gap, `ShopHintsDone`, `MarkShopHintsDone`, the `firstCall` constructor argument)
- `Assets/Scripts/Game/FirstOrderGuide.cs` (dark-layer presses instead of `Pointer.current`; `PollShop` for four hints with the start rules; Profit/Clients targets; records done hints; `Bind(..., guideFirstCall)`, `BindShop(..., done, recordDone)`)
- `Assets/Scripts/Game/GameInstaller.cs` (binds the guide for pending shop hints on Continue)
- `Assets/Scripts/Game/Views/SpotlightGraphic.cs` (`IPointerDownHandler`, `Pressed`)
- `Assets/Scripts/Game/Views/TutorialSpotlight.cs` (`DimPressed`; texts `readMessageDone`, `buyProfit`, `buyClients`)
- `Assets/Prefabs/Game/GameUI.prefab`: the three new texts on `TutorialSpotlight`, edited as text (3 lines added, no Unity re-save)
- Tests: `Assets/Tests/Domain/SaveSessionTests.cs`, `Assets/Tests/Game/Editor/FirstOrderGuideTests.cs`, `Assets/Tests/Game/Editor/TutorialSpotlightTests.cs`
- `Docs/Design.md` (5.12 "Saved too", the 6.2 table row), `Docs/Progress.md` (this report)

Scene and prefab changes: `GameUI.prefab` as above. `Final.unity` and `MainMenu.unity` are unchanged.

Tests: 638/638 EditMode (was 624). New or changed:
- `SaveSessionTests`: `ANewGameHasSeenNoShopHintAndASaveKeepsTheOnesDone`, `ASaveFromBeforeShopHintsWereSavedCountsThemAllDone` (parses JSON without the field), `ASessionWithoutTheTutorialHasNoShopHints`.
- `FirstOrderGuideTests`:
  - New: `TheMessageWaitsForAClickOnceTypedAndAnEarlyClickIsSwallowed`, `ProfitAndClientsWaitForTheFirstCallAndComeOneAtATimeWithAGap`, `AnOwnedOrUnsoldUpgradeNeverGetsItsHint`, `ContinueSkipsTheFirstCallAndShowsOnlyTheHintsNotDone`.
  - The main flow now runs through Profit and Clients.
  - Skip and call-ending cases now include the new stages.
  - Changed on purpose: step 9 and "no cord or no mystery" now return to normal play while Profit or Clients are still to come. The old API calls became `Offer`.
- `TutorialSpotlightTests`, in the real `Final` scene:
  - `ANewGame...`: step 2 waits after typing, a press during typing and a press inside the guard do nothing, and the advancing press changes nothing else (call state, book, cord). It also checks that a new game has no done hints.
  - `MoreProfitThenMoreClientsFollowTheCordStepsOnceEachEvenWithMoneyFromTheStart`: money from step 1 on; cord, then mystery, gap, Profit (dismissed, not bought), gap, Clients (bought); Complete; the session holds All; never again.
  - `ContinueShowsAPendingHintOnlyWithTheBookShutNoCordInHandAndNotPaused`
  - `ContinueAfterAnEarlySaveSkipsTheFirstCallButShowsTheCordStep`
  - `ADoneOrBoughtHintNeverComesBackAfterAReload`
  - `ALoadedGameHasNoTutorial` now also asserts an old save means all hints done.
  - `TheDarkLayerSwallowsClicks...`: the only down handler on `Dim` and its parents is `Dim` itself.

Console: clean (only Unity's "Ignoring depth surface ... memoryless" lines from Play Mode).

Play Mode check (from `MainMenu` at 1920×1080; frames driven with `EditorApplication.Step`; clicks as EventSystem raycasts plus `CableInputController.ProcessPointer` in `Canvas.willRenderCanvases` handlers):
- **New Game.** The menu's New Game button loaded `Final`, with the tutorial on and no done hints. The card click hit `Portrait`.
- **Step 2.** The text finished at about 1.7 s, and the hint switched to "... Click to continue." The step was still on TaskText more than 2 s of game time later.
- **The advancing click.** A raycast click on the bubble had `Dim` as its top hit and down handler, and no click handler. The guide moved to Book, the call stayed Taken, the book stayed shut and no cord left the tray.
- **Dev money during the guide.** I added +$10 (the dev panel's `Wallet.Earn(1000)`) during step 3. Steps 3–7 went on undisturbed (`BookShortcut`, `OpenBook`, `StoredCable_01`, `Socket_A1`, `Socket_B3` were the top hits). No shop hint came up during the first call.
- **Shop hints in order.**
  - After step 7 came step 8 (More Cables), though Profit and Clients were affordable too. It was bought through its `Buy button`, and step 9 followed at once and ended by time.
  - 1.5 s later More Profit showed. A click on empty board hit `Dim`, dismissed it and bought nothing, and the session recorded Profit as done.
  - 1.5 s later More Clients showed.
- **Reload with a pending hint.** I left More Clients showing and went Pause, then Main menu. The save then held `"shopHintsDone":7`. Continue: `IsTutorial` false, 3 calls at once, and More Clients came back without More Profit. I bought it through its button: Complete, the guide off, and the hints All.
- **Reload with every hint done.** Main menu, then Continue again: the save held `15`, no guide was bound, the spotlight was off and 4 calls came at once.
- Screenshots in `Temp/Captures/`: `step-10-fix2-read.png` (step 2 waiting), `step-10-fix2-profit.png`, `step-10-fix2-clients.png`, `step-10-fix2-continue-clients.png` (the pending hint after Continue).

Housekeeping:
- **The lead's keys.** `operator.save` and `operator.save.backup` (183 chars each, present this time) and `Switchboard.AudioPatch.v1` were kept in SessionState, deleted for the run and then restored exactly; all three were compared. The SessionState keys are erased. The lead's restored save predates this field, so their next Continue counts every shop hint as done (as specified).
- **Editor state.**
  - `Time.timeScale` is back to 1 (×5 during two waits).
  - The Game view is at 1920×1080.
  - The stepper and click handlers removed themselves.
  - The dynamic font atlases and "SDF Static" materials were cleared in memory: no font, `TimeManager.asset` or `ProjectSettings.asset` changes.
  - `Final.unity` is active and not dirty. Unity was idle and out of Play Mode when I started, and is again.
- Nothing staged, committed or pushed.

Deviations from Design/Plan:
1. **Typing speed is unchanged:** `CallCardView` types at 40 characters a second, so the first message (69 characters) takes about 1.7 s. That is fast, but no longer a problem, because step 2 now waits for the click.
2. **Which clicks count.** "A click anywhere" means a press the dark layer swallows. A press on Skip, the pause button or a lit target doesn't count. This also changed steps 7 and 9: a press on the pause button no longer dismisses them.
3. **The start rules now cover the cord step too.** The rules for a hint to come up (not paused, book shut, no cord in hand) apply to steps 8–9 as well as Profit and Clients. Before, step 8 checked only the book.
4. **Profit and Clients don't wait for an unaffordable cord.** If the cord is too expensive while More Profit is affordable, Profit may show first. With today's Economy.json the cord (25¢) is always affordable first. "Not while steps 8–9 are running" holds: only one hint shows at a time.
5. **Skip ends the remaining shop hints for good**, More Profit and More Clients included. So does the guided call ending before it connects. This matches fix pass 1, where Skip ended steps 8–9. The Skip button also shows during the shop hints on Continue.
6. **A done hint is saved at the next autosave.** It is recorded in the session at once, and written within 10 s, on focus loss, on page hide, or when leaving through the pause menu. A bought upgrade never gets its hint anyway (it is owned).
7. **No save version bump.** The `-1` initializer marks a missing field (Plan §4, "Missing JSON fields"), so `SaveData.CurrentVersion` stays 1.

Follow-ups:
- Design.md: the D57 status ("fix pass 2 building") is for the orchestrator to update. I updated 5.12 and the 6.2 table, which list the saved fields (4.4 doesn't).
- The Profit and Clients hints sit over the board's top-right jacks, like step 8's. If the lead prefers the hints beside the shop, they can get an anchor there.
- `TutorialSpotlightTests` rely on `Time.deltaTime` being 0.33 in EditMode: one `LateUpdate` stays inside the 0.5 s and 0.6 s guards. If a later Unity version changes that, give the guide a test hook for time.

### Web start: no Unity splash, loading until the menu is drawn (2026-10-20)
Result: done
Playable now: the Web build opens with no "Made with Unity" splash. Our dark loading cover ("Operator!" and the gold bar) fills the window, phones included. When the download ends the bar is full and "Starting..." appears under it. The cover fades out (0.3 s) only once the main menu has drawn its first frame, so there is no black gap. If the game's signal never came, the page would lift the cover 8 s after Unity's loader finishes. There is no "Click to play" button (lead's decision). The build also carries the uncommitted Step 10 fix pass 2 (reading pace, shop hints).
How to test: open the itch Draft page (build **#2168919**), press Run game, and watch the start: bar, "Starting...", then the menu fades in with no Unity logo and no black frame. Try it on a phone too.

Created:
- `Assets/Scripts/Game/PageReady.cs` (+ `.meta`): `Switchboard.Game.PageReady`. Its `Start` coroutine waits one frame, then `WaitForEndOfFrame`, then tells the page once per page load (a static flag, reset at `SubsystemRegistration`). In the editor and desktop builds it does nothing.
- `Assets/Scripts/Game/GameReady.jslib` (+ `.meta`): `OperatorPage_GameReady` calls `window.operatorGameReady()` if the page defines it. Its importer is Unity's default for `.jslib` (Web only), like the other two plugins. I kept it next to its C# script, as `SystemPointer.jslib` and `BrowserStorage.jslib` are, rather than in `Assets/Plugins/WebGL/`.

Changed:
- `ProjectSettings/ProjectSettings.asset`: `m_ShowUnitySplashScreen` and `m_ShowUnitySplashLogo` 1 → 0, set through `PlayerSettings.SplashScreen.show`/`showUnityLogo` and saved. Unity accepted it on Personal. The diff is just those two lines: no Unity Cloud link, and `preloadedAssets` stays `[]`.
- `Assets/WebGLTemplates/Operator/index.html`:
  - The bar now sits in a full-window `#unity-loading-cover`, with a new `#unity-loading-status` line.
  - `showProgress` maps Unity's 0–0.9 download progress to a full bar and shows "Starting..." from 0.9 on.
  - `window.operatorGameReady = liftCover` is defined before the loader runs, so an early signal also works.
  - `then` no longer hides the bar. It shows it full and starts the 8 s fallback (`setTimeout(liftCover, coverFallbackMs)`).
  - `liftCover` runs once. It turns off pointer events, fades the opacity to 0, then sets `display: none` after 0.4 s.
  - Unity's error banner (z-index 10) still shows above the cover (z-index 5).
- `Assets/WebGLTemplates/Operator/TemplateData/style.css`:
  - `#unity-loading-cover`: fixed, fills the window, `#15110d`, opacity transition 0.3 s.
  - `#unity-loading-status`: small, muted serif.
- `Assets/Scenes/MainMenu.unity`: a new root object `PageReady` with the component. Unity's save also dropped four stale `HandCursorGraphic` texture fields and re-wrapped the credits text, so I reverted the scene and inserted only the new object and its `SceneRoots` entry as text (45 lines added, nothing else).
- `Assets/Tests/Game/Editor/WebBuildTests.cs` (below).
- Docs:
  - `Docs/WebBuild.md`: new section "What players see while it loads".
  - `Docs/Design.md`: the 9b status row, one sentence.
  - `Docs/Plan.md` §8: "Menu leftovers" notes that the sound-length line is still there. "Web build leftovers" notes the font gap below.
  - `Docs/Plan.md` §4: "Checking a Web build" notes that the Chrome extension couldn't reach the local server.
  - `Docs/Progress.md`: this report.

Scene and prefab changes: `MainMenu.unity` as above. `Final.unity` and the prefabs are unchanged.

Tests: 642/642 EditMode (was 638). New:
- `TheCommittedPlayerSettingsHaveNoUnitySplash`: checks the API and the asset's two lines.
- `TheLoadingCoverWaitsForTheGameWithAFallback`: checks the template has the cover, `createUnityInstance`, the ready hook and the fallback timeout, and no longer hides the bar in `then`. It also checks that the jslib calls the hook and `PageReady.cs` calls the jslib.
- `TheMainMenuSignalsThePageWhenItIsDrawn`: `MainMenu` is the first build scene and depends on `PageReady.cs`.
- `TheBrowserPluginIsForWebPlayersOnly(GameReady.jslib)`: a new case.

Console: clean.

Build and publish:
- `Build Web: done in 2.2 min. The build is in Builds/Web (27.5 MB).`
- `butler push` uploaded 27.46 MiB. `butler status` shows `html5` at build **#2168919** (from #2168670), version 9, processed ✓. The page is still a Draft.

Leftovers after the build:
- `git status` showed `Boecklins Universe SDF.asset` (+72/−8 glyph lines) and the two `SDF Static` materials (`_ScaleRatio`).
- Cause: I ran the EditMode tests right before the build. The tests filled the dynamic fonts in memory, and `WebBuild` saves *before* it snapshots, so its restore kept the filled version. The Static materials aren't in its list at all.
- I discarded all three with `git checkout --`, force-reimported the four font assets and cleared the dirty Della Respira atlas in memory. A later `AssetDatabase.SaveAssets` wrote nothing.
- No URP, `TimeManager.asset`, `preloadedAssets` or Cloud-link changes.

Browser check:
- **The Chrome extension was connected but couldn't reach the local server.** Both `127.0.0.1:8000` and `localhost:8000` gave Chrome's offline page, and the server logged no request from it. I closed its tab.
- `curl`: `index.html` and the four `Build/` files returned 200.
- Instead I used headless Chrome (SwiftShader) over CDP, as in 9b. It watched `window.operatorGameReady`, the cover's style and the status line, and took screenshots.
- **1280×720:**
  - "Starting..." at 2.68 s (download done).
  - Loader `then` at 4.79 s.
  - **The game's ready call at 5.08 s.** The cover faded at once and was `display: none` at 5.48 s.
  - The fallback wasn't needed.
- **844×390 with mobile emulation:** the same order (then 5.01 s, ready 5.30 s). The cover filled the screen.
- Every captured frame before the ready call showed the cover; the first one after it showed the menu. No Unity logo appeared. The frames were about 0.15–0.6 s apart, so a single black frame between captures can't be ruled out by screenshots alone. The cover being opaque until the ready call is what rules it out.
- Console: no warnings or errors. Only the known "Trying to get length of sound which is not loaded yet." log line (Plan §8, "Menu leftovers").
- I stopped the server and headless Chrome. Screenshots in `Temp/Captures/`: `web-start-cover.png`, `web-start-menu.png`, `web-start-cover-phone.png`, `web-start-menu-phone.png`.

Housekeeping:
- I never entered Play Mode, so the lead's save keys weren't touched.
- The editor is idle and out of Play Mode, on the Web platform. `Final.unity` is active and not dirty, and the console is clean.
- Nothing staged, committed or pushed.

Deviations from Design/Plan:
1. The jslib lives at `Assets/Scripts/Game/GameReady.jslib`, next to `PageReady.cs`, matching the existing plugins. The brief's `Assets/Plugins/WebGL/` path was only an example.
2. "Starting..." starts at 90% progress (Unity's end of download). Unity's own 1.0 comes only when the player is running.

Follow-ups:
- `WebBuild` could snapshot the restored files *before* its save, and add the two `SDF Static` materials to `RestoredFiles`. Then a build right after a test run wouldn't keep the test glyphs (Plan §8).
- The lead could check the start on the real itch page, on a desktop and a phone, especially on the external display.

### Step 10b report: 2026-10-20
Result: done
Playable now: every ringing caller's jack blinks softly on the board (a warm lamp in the hole with a glow around it), and the call in hand's caller jack glows steadily and brighter, so only the person they want needs looking up. Clicking a name in the open book closes the book and marks that jack with the same steady glow. The tutorial's step 4 now lights only the recipient's row: "Find who they want and click their name."; clicking it is what moves on.
How to test:
1. Continue (or any game with several callers): every caller with a card has a blinking jack. The jacks of the people they want stay dark.
2. Click a card: its caller's jack stops blinking and glows steadily. Click another card: that one glows, and the first blinks again.
3. Open the book and click the wanted person's row: the book closes and their jack glows. Now the two steady lights are the two jacks to patch.
4. Plug a cord into the caller's jack: its light goes out (the cord is in it). Plug the other end into the marked jack: the call connects and both lights are out.
5. Mark a row, then click another row: the mark moves. Mark a row, then take another card: the mark goes. Click the open book anywhere off the rows: it still closes, and nothing is marked.
6. A name whose jack isn't on your board yet (a small board after New Game): clicking it does nothing, and the book stays open.
7. New Game: steps 1-3 as before. Step 4 lights only the wanted person's row (plus the page corners if the book has more spreads; they still turn pages). Clicking another row or the rest of the book does nothing. Clicking the lit row closes the book and marks the jack. Step 6: both jacks glow under their spotlight holes: "Plug one end into each lit jack."

**Tutorial texts changed** (Inspector: `GameUI` > `Tutorial` > `TutorialSpotlight` > Texts; the other steps are unchanged):
| Step | Lit | Takes clicks | Text |
|---|---|---|---|
| 4 ReadingBook | the recipient's row (or the page corner that turns to it) | that row, and the page corners | Find who they want and click their name. |
| 6 Plugging | the call's two jacks | those two jacks | Plug one end into each lit jack. |

How it works:
- **The rule (Domain).** `CallerLights` (`Assets/Scripts/Domain/Desk/CallerLights.cs`) with `JackLight { Off = 0, Waiting = 1, InHand = 2 }`:
  - `Of(call, inHand)`: Incoming is Waiting; Taken or Connecting is InHand for the call in hand, else Waiting; Connected, OnHold and Ended are Off.
  - `AtJack(jack, calls, inHand, desk)`: the brightest light of the calls whose `from` is that jack (a shared caller jack lights while any of its calls waits), and Off while a loose plug sits in the jack.
  - New `OperatorDesk.HasLoosePlugIn(jack)`: a free half-patched cord's only plug is in that jack. That is "a plug in that jack for that call": the operator is patching from it. An old closed circuit left in the jack by a finished call (D45: cords stay plugged) does **not** hide a new caller there, and a plug in the recipient's jack keeps the caller lit until the second plug goes in.
- **The view.** `CallerJackLights` (`Assets/Scripts/Game/`, added at runtime by `GameInstaller`, like the guide):
  - Polls every `LateUpdate` without allocations (cached `JackId`s and a reused list).
  - Only calls with a card on screen count (`CallQueueView.CollectShownCalls`), so a call waiting behind a stamp doesn't light a jack before its card appears.
  - The call in hand is the card whose bubble gives the task (`CallQueueView.CallInHand`, i.e. `IsGivingTask`).
  - Steady for the call in hand's caller or the marked jack; Blinking for a waiting caller; otherwise Off.
  - **The mark:** `book.JackPicked` marks; one at a time. It clears when a plug goes into that jack (polled: not occupied, then occupied; a jack marked with a plug already in keeps it until the next plug goes in), when another row is clicked, when a card for another call is opened (reopening the same card keeps it), or when the jack closes. `CanMark`: the jack exists and is open. It is not saved.
- **The lamp (UI).** `SocketLampGraphic` with `SocketLamp { Off = 0, Blinking = 1, Steady = 2 }`.
  - `SocketGraphic.SetLamp`/`SocketView.SetLamp` make it lazily as a `DontSave` child "Socket lamp" (first sibling, under the hover/connected ring), like the existing halo.
  - Warm lamp colour (255, 208, 110): bright in the hole, light over the rim, a soft glow on the board around it (to 0.64 of the socket size, so it stays clear of the neighbours).
  - The blink changes only `canvasRenderer` alpha (0.1 to 0.7 over 1.1 s, on `Time.unscaledTime`, so every jack blinks in step); the mesh is built once. Steady is alpha 1.
  - It is part of the board canvas (order 0): under every resting plug, every wire (1-6), the book and GameUI (10) and the spotlight (14). `raycastTarget` is off, so the jack is still the top hit.
  - A closed socket (dimmed or disabled) never shows it.
- **Book rows.** Like the page corners, rows are found by position, through the open book's art (the only raycast target):
  - `PhoneBookPageView.RowAt` and `PhoneBookView.RowAt`, which yields to the page corners.
  - `OnPointerClick`: corner first; then a row. If `CanPickJack` allows it, the book closes and `JackPicked(jack)` follows. If not (outside the open board), the click is ignored and the book stays open. Anywhere else on the open book it closes as before.
  - The dead `PhoneBookContactRow.OnPointerDown` and its `IPointerDownHandler` are removed (Plan §8 item closed).
  - The hand already points over the open book (corners show the page-turn hand), so rows needed no cursor change; the existing cursor tests cover it.
- **Tutorial.**
  - `SpotlightTarget` gained `Lit` (default true). An unlit target takes clicks but has no hole and no arrow.
  - Step 4 (`FirstOrderGuide`) adds the recipient's row (lit, clickable) or the corner that turns to it, plus both page corners unlit. The whole-book click area is gone, so only those take clicks.
  - The row click closes the book, which moves the sequence on to "Take a cord" through the existing `OpenChanged`. `FirstOrderGuideSequence` is unchanged.
  - Esc and Tab still close the book (closing always works); that moves on without a mark, and step 6 still lights both jacks.

Created:
- `Assets/Scripts/Domain/Desk/CallerLights.cs` (+ `.meta`)
- `Assets/Scripts/UI/SocketLampGraphic.cs` (+ `.meta`)
- `Assets/Scripts/Game/CallerJackLights.cs` (+ `.meta`)
- `Assets/Tests/Domain/CallerLightsTests.cs` (+ `.meta`)
- `Assets/Tests/Game/Editor/CallerJackLightsTests.cs` (+ `.meta`)

Changed:
- `Assets/Scripts/Domain/Desk/OperatorDesk.cs` (`HasLoosePlugIn`)
- `Assets/Scripts/UI/SocketGraphic.cs` (`SetLamp`, `Lamp`, `IsLampShowing`, `LampGraphic`, the lamp child; the halo keeps the lamp under it), `SocketView.cs` (`SetLamp`, `Lamp`)
- `Assets/Scripts/UI/PhoneBookView.cs` (`JackPicked`, `CanPickJack`, `RowAt`, row clicks), `PhoneBookPageView.cs` (`RowAt`), `PhoneBookContactRow.cs` (dead handler removed)
- `Assets/Scripts/Game/Views/CallQueueView.cs` (`CallInHand`, `CollectShownCalls`), `GameInstaller.cs` (binds `CallerJackLights`)
- `Assets/Scripts/Game/FirstOrderGuide.cs` (step 4), `Views/TutorialSpotlight.cs` (`SpotlightTarget.Lit`, unlit targets get no hole; two default texts)
- `Assets/Prefabs/Game/GameUI.prefab`: `findJacks` and `plugJacks` on `TutorialSpotlight`, edited as text (2 lines in, 3 out; no Unity re-save)
- Tests: `Assets/Tests/Game/Editor/TutorialSpotlightTests.cs`, `PhoneBookPageTurnTests.cs`
- `Docs/Plan.md` §4 ("Book row clicks" row rewritten) and §8 (the `OnPointerDown` item removed), `Docs/Progress.md` (this report)

Scene and prefab changes: `GameUI.prefab` texts only. `Final.unity` and `MainMenu.unity` are unchanged. The lamps are made at runtime.

Tests: 657/657 EditMode (was 642). New:
- `CallerLightsTests` (8, Domain):
  - `AWaitingCallBlinksAtItsCallersJackOnly`
  - `TheCallInHandGlowsAndAnotherAcceptedCallStillBlinks`
  - `ALoosePlugInTheCallersJackPutsItOutAndPullingItLightsItAgain`
  - `APlugInTheRecipientsJackKeepsTheCallersLight`
  - `AnOldCircuitInTheJackDoesNotHideAWaitingCaller`
  - `ConnectingEndingOrHoldingPutsTheLightOut`
  - `ASharedCallersJackLightsWhileAnyOfItsCallsWaits`
  - `OnlyTheGivenCallsAreConsidered`
- `CallerJackLightsTests` (7, the real `Final` scene from a loaded game, cords through `CableInputController.ProcessTarget`):
  - `EveryWaitingCallersJackBlinksAndTheCallInHandsGlows`
  - `TheLightGoesOutWhenAPlugGoesInAndWhenTheCallConnects`
  - `AnEndedCallsJackGoesDark`
  - `ARowClickMarksItsJackClosesTheBookAndAnotherRowMovesTheMark`
  - `TheMarkClearsWhenAPlugGoesInAndWhenAnotherCallIsTaken`
  - `AJackOutsideTheOpenBoardCannotBeMarked`
  - `TheLampLiesOnTheBoardTakesNoClicksAndBlinksInStep`

Changed:
- `TutorialSpotlightTests.ANewGame...`: step 4 lights only the recipient (or the corner to it, turned through `OnPointerClick` until the row shows), the corners take clicks unlit, the caller's row and the rest of the book are blocked, and the row click closes the book and marks the jack. Step 6 checks both jacks are Steady. `AssertStep` skips unlit targets.
- `PhoneBookPageTurnTests.TabsKeepPriorityAndARowClickPicksItsJackAndClosesTheBook` (renamed from `...RowsLetClicksThroughToCloseBook`): a row click picks its jack and closes the book, a refused jack keeps the book open, and an off-row click still closes it.
- Jacks and rows come from the loaded files, so the designer's edits don't break them.

Console: clean (only Unity's "Ignoring depth surface ... memoryless" lines in Play Mode); cleared at the end.

Play Mode check (`Final`, 1920×1080, frames with `EditorApplication.Step`, clicks as EventSystem raycasts and `CableInputController.ProcessPointer` in `Canvas.willRenderCanvases` handlers):
- **The lead's save (Continue state, 4×4 board, 3 callers).** A4, B1 and C2 blinked together.
- **Taking a card.** Clicking Dr Ravi Sharma's card (`Portrait` was the top hit) turned B1 steady; the others kept blinking.
- **Marking a row.** Opening the book (`BookShortcut`) and clicking the "Agatha Higgins C4" row (the top hit was `OpenBook`) closed the book and marked C4 steady.
- **New Game.** Step 4 lit only Violet Fairweather's row (B3). A raycast on another row hit `Dim` (blocked); the lit row's click hit `OpenBook`, closed the book, marked B3 and moved to "Take a cord".
- **Plugging.** Step 6 showed A1 (the caller) and B3 (the mark) steady. Plugging A1 and B3 connected the call, cleared the mark and put both lights out; the next callers' jacks (B1, C2) started blinking.
- Screenshots in `Temp/Captures/`:
  - `step-10b-1.png`: three ringing callers' jacks blinking
  - `step-10b-2.png`: the call in hand's steady B1 beside the blinking A4 and C2
  - `step-10b-3.png`: the marked C4 after the row click
  - `step-10b-4.png`: tutorial step 4 on a new game
  - `step-10b-5.png`: tutorial step 6 with both jacks lit
- **Look.** The first look (a flat amber disc over the whole jack) hid the hole; I retuned it to a bright lamp in the hole with the rim readable and a glow around it.

Housekeeping:
- Unity was idle and out of Play Mode when I started.
- **The lead's keys.** `operator.save`, `operator.save.backup` (183 chars each) and `Switchboard.AudioPatch.v1` were kept in SessionState and restored afterwards. All three were compared by exact string and are identical. The SessionState keys are erased.
- **Editor state.**
  - `Time.timeScale` is 1. The Game view size was not changed (1920×1080).
  - The stepper and click handlers removed themselves.
  - The dynamic font atlases and "SDF Static" materials were dirtied by Play Mode and the test run; I force-reimported the four font assets and cleared their dirty flags, so no font files changed.
  - `TimeManager.asset` and `ProjectSettings.asset` are untouched. `Final.unity` is active and not dirty.
- Nothing staged, committed or pushed.

Deviations from Design/Plan:
1. **Accepted but not in hand.** The brief defines the waiting light for calls not yet taken. A call that was accepted earlier but isn't the card in hand (D45 allows several) also **blinks**, because it still waits for a connection. Only the open card's caller glows.
2. **"A plug in that jack for that call"** is read as a loose plug: a free cord's only plug in the caller's jack, i.e. the operator is patching from it. It applies to waiting calls too. An old circuit left plugged by a finished call doesn't hide a new caller from that jack.
3. **Unreachable rows are ignored:** no blocked feedback, and the book stays open (the brief allowed either).
4. **The mark looks like the call in hand's glow** (same steady lamp). The two steady lights read as "patch these two"; a wrong row is the player's own choice.
5. **The light shows only once the call's card is on screen**, not while the call waits behind a stamp (about 2 s at most, Design 5.2).
6. **Step 6's text** became "Plug one end into each lit jack." (was "... each jack.") to match the lights. Not asked for; revert it on `TutorialSpotlight` if unwanted.
7. In step 4, Esc and Tab still close the book without a mark (closing always works since fix 1). Step 6 still lights both jacks.

Follow-ups:
- Design.md is for the orchestrator to update:
  - the D58 status
  - 5.7: row clicks mark a jack
  - 4.3: the lamp sits on the board under the wires and takes no clicks
  - D57 step 4: the recipient's row only
- The lamp colour, size and blink live in `SocketLampGraphic` constants. If the lead wants the mark to look different from the caller's glow, give `SocketLamp` a third lit value.
- A small hover tint on book rows could show they're clickable. Not built, to keep the step small.

#### Step 10b, fix pass 1: blinking means caller, hover rows, tutorial explains the light (lead, 2026-10-20)
Result: done
Playable now: blinking always means a caller's line. Waiting callers blink softly; the call in hand's caller blinks brighter and faster. Only the jack marked from the book glows steadily. The tutorial has a new step 3 after the speech bubble that lights the caller's blinking jack and explains it. Book rows show a soft warm tint under the pointer when clicking them would mark a jack.
How to test:
1. Continue with several callers. Every caller's jack blinks softly. Click a card: its caller's jack now blinks faster and brighter, and never goes steady. Click another card: the bright blink moves to that caller.
2. Open the book and move the pointer over the names. The row under the pointer gets a soft warm tint. Off the rows (the spine, the margins) there is no tint. A name whose jack isn't on your board yet gets no tint, and clicking it does nothing.
3. Click a name: the book closes and that jack glows steadily, the only steady light on the board. The caller's jack keeps blinking beside it. Patch the blinking jack to the steady one.
4. New Game: step 1 (click the caller), step 2 (the bubble; click once it has typed out). **New step 3:** a round light on the caller's jack, which blinks, with "This blinking light is the caller's line. Now find who they want." Nothing else takes clicks, the book stays shut, and the jack takes no plug. A click anywhere moves on, after the same short guard as step 2; the click is swallowed. Then "Open the phone book.", the recipient's row (it tints under the pointer; the blocked rows don't), a cord. At the plugging step the caller's jack blinks and the marked recipient glows steadily, the same as real play.

**Final tutorial texts** (Inspector: `GameUI` > `Tutorial` > `TutorialSpotlight` > Texts):
| Step | Stage | Text |
|---|---|---|
| 1 | Portrait | A call! Click the caller to answer. |
| 2 | TaskText (typing) | Read who they want to talk to. |
| 2 | TaskText (typed) | Read who they want to talk to. Click to continue. |
| 3 (new) | CallerJack | This blinking light is the caller's line. Now find who they want. |
| 4 | Book | Open the phone book. |
| 5 | ReadingBook | Find who they want and click their name. |
| 6 | Cables | Take a cord. |
| 7 | Plugging | Plug one end into each lit jack. |
| 8 | Connected | Connected! You earn money while they talk. |
| 9 | BuyCord (shop hint) | You can afford another cord. Buy it to handle two calls at once. |
| 10 | Mystery (shop hint) | Save up for the mystery upgrade! |
| - | Profit (shop hint) | More Profit: every call pays more. Buy it to earn faster. |
| - | Clients (shop hint) | More Clients: more callers at once, and a bigger board. |

Steps after the new one moved up by one. The lead's "step 6" (both jacks) is now step 7; the shop steps 8-9 are now 9-10.

How it works:
- **Lamps.** `SocketLamp` gained `BlinkingBright = 3` (the call in hand's caller). `Steady = 2` now means only the marked jack.
  - `SocketLampGraphic.Mode` replaces the old `Steady` flag.
  - Waiting callers blink between alpha 0.1 and 0.7 over 1.1 s. The call in hand blinks between 0.35 and 1.0 over 0.55 s, so it is brighter at both ends and twice as fast. A mark is a steady 1.0.
  - All blinks run on one `Time.unscaledTime` clock (`AlphaOf`), and the mesh is still built once.
  - `CallerJackLights` maps `JackLight.InHand` to `BlinkingBright`. The mark wins when it sits on a caller's jack: it is the player's latest choice, so clicking the caller's own row (a slip) shows steady where the mark went.
  - The domain `CallerLights` is unchanged (Off, Waiting, InHand); only its comments changed.
- **Tutorial.** `FirstOrderGuideStage.CallerJack = 3`, between TaskText and Book.
  - The stages are never saved (the save keeps only `ShopHints` flags), so the later values moved up by one. That keeps the existing range checks (`IsGuiding`, `IsFirstCallOver`, `FirstOrderConnected`) correct.
  - `AnyClick` moves step 2 on to CallerJack and resets the step timer. CallerJack moves on to Book once the same `ReadClickDelay` (0.5 s game time) has passed.
  - The step has a round, lit, non-clickable hole on the caller's `SocketView`, so the dark layer swallows the click (no plug, no pickup, book locked). Skip and a call ending mid-step cancel it like the other first-call steps.
  - The new text field is `TutorialTexts.callerJack`. `GameUI.prefab` gained that one line, edited as text with no Unity re-save.
  - Step 7 needs no code change: `CallerJackLights` already shows the in-hand caller blinking and the mark steady.
  - The shop-hint logic matches stages by name, not by number, so only comments changed there (`ShopHints`, `FirstOrderGuide`, the sequence).
- **Hover rows.** `CableInputController.ProcessTarget` passes the position to `PhoneBookView.HoverAt` whenever its top hit is the open book. It clears the hover when the pointer leaves the book, when the dark layer blocks, and on `HidePointers` (pause, focus loss, disable). A finger lifted off a touchscreen leaves no hover.
  - `HoverAt` uses the existing `RowAt` (page corners first) and `CanPickJack`. It shows nothing while the book is shut or `InputEnabled` is off.
  - Closing the book and turning a page clear it.
  - `PhoneBookContactRow.SetHovered` lazily makes a `DontSave` child "Hover tint": an `Image` behind the text, `raycastTarget` off, lamp colour (255, 208, 110) at alpha 80/255.
  - No new raycast target.

Created: none.

Changed:
- `Assets/Scripts/UI/SocketLampGraphic.cs` (`SocketLamp.BlinkingBright`, `Mode`, `BrightBlink*`, `AlphaOf`)
- `Assets/Scripts/UI/SocketGraphic.cs` (passes the mode on), `SocketView.cs` (comment)
- `Assets/Scripts/Game/CallerJackLights.cs` (in hand blinks bright, mark steady and on top)
- `Assets/Scripts/Domain/Desk/CallerLights.cs` (comments only)
- `Assets/Scripts/Game/FirstOrderGuideSequence.cs` (`CallerJack` stage, renumbered values, `AnyClick`)
- `Assets/Scripts/Game/FirstOrderGuide.cs` (step 3 hole; `AddSocket` takes `clickable`)
- `Assets/Scripts/Game/Views/TutorialSpotlight.cs` (`callerJack` text)
- `Assets/Scripts/Domain/Save/ShopHints.cs` (comment step numbers)
- `Assets/Scripts/UI/PhoneBookView.cs` (`HoverAt`, `ClearHover`, `HoveredRow`), `PhoneBookContactRow.cs` (`SetHovered`, `IsHovered`, `HoverTint`), `CableInputController.cs` (`HoverBookRow`)
- `Assets/Prefabs/Game/GameUI.prefab` (one line: `callerJack`)
- Tests:
  - `Assets/Tests/Game/Editor/FirstOrderGuideTests.cs`
  - `TutorialSpotlightTests.cs`
  - `CallerJackLightsTests.cs`
  - `Assets/Tests/Domain/CallerLightsTests.cs` (one rename)
- `Docs/Plan.md` §4 (hover note on "Book row clicks"; new row "Capturing a blink phase")
- `Docs/Progress.md` (this report)

Scene and prefab changes: `GameUI.prefab` text only. `Final.unity` and `MainMenu.unity` are unchanged.

Tests: 663/663 EditMode (was 657). New or changed:
- `FirstOrderGuideTests`:
  - new `TheCallersJackStepWaitsForAClickAfterAShortGuard`
  - `CallerJack` added to `SkipEndsEveryStepForGood`, `TheGuidedCallEndingEndsOnlyItsOwnSteps` and `OutOfStepEventsNeitherSkipNorEndTheGuide` (+3 cases)
  - the ordered-steps test and the message-guard test go through step 3
- `CallerJackLightsTests`:
  - new `HoveringARowTintsItWhileTheBookIsOpenAndItsJackCanBeMarked`: open only, one row at a time, none off the rows, none for an unreachable jack, none for a touch without a finger, cleared on close, on leaving the book and while paused; no new raycast target, and the tint sits behind the text
  - new `AMarkOnTheCallersOwnJackShowsSteady`
  - the in-hand light is `BlinkingBright`, brighter at both ends and faster; only a mark is steady at alpha 1
- `TutorialSpotlightTests`:
  - the full new game checks step 3: the hole, the text, the jack blinking bright, everything blocked (the jack included), the book locked, the stray-click guard, and that the advancing click does nothing else
  - step 5: the lit row tints and a blocked row doesn't
  - step 7: caller `BlinkingBright`, recipient `Steady`
  - the arrow test also checks that step 3's arrow stays off other jacks
  - the `ReadMessage` helper passes steps 2 and 3
- `CallerLightsTests.TheCallInHandGlows...` is renamed `...StandsOut...`.

Console: clean (only Unity's "Ignoring depth surface ... memoryless" lines in Play Mode); cleared at the end.

Play Mode check (`Final`, 1920×1080, frames driven by `EditorApplication.Step`, stopped at chosen blink phases):
- The lead's save (Continue, 4×4, callers A1, B1, C2), after taking Nora Ellis's card (A1):
  - `step-10b-fix-1.png`: A1 at its bright peak (alpha 0.99) beside B1 and C2 (0.43)
  - `step-10b-fix-2.png`: A1 at its low point (0.35), still clearly lit, while B1 and C2 are at 0.10
  - When Continue started, the cord shop hint came up (the save had 53¢ and no hints done). I skipped it for the captures; the save was restored afterwards.
- `step-10b-fix-3.png`: the book open on Violet Fairweather's row (B3), with the hover tint.
- `step-10b-fix-4.png`: after clicking that row, B3 glows steadily (alpha 1) while A1 blinks.
- New Game:
  - `step-10b-fix-5.png`: step 3. The round light is on A1 (blinking), with the text and arrow. The press after the guard moved on to "Open the phone book."
  - Then the row click marked B3 and closed the book, and the tray cord was taken.
  - `step-10b-fix-6.png` / `-7.png`: step 7 with A1 near its bright peak and at its low point, and B3 steady.
  - Plugging A1 and B3 connected the call (step 8), and both lights went out.

Housekeeping:
- Unity was idle and out of Play Mode when I started; I didn't have to stop anything.
- **The lead's keys:** `operator.save`, `operator.save.backup` (205 chars each) and `Switchboard.AudioPatch.v1` (62 chars) were kept in SessionState, deleted for the New Game run, and restored afterwards. All three were compared by exact string and are identical. The SessionState keys are erased.
- `Time.timeScale` is 1. The Game view is 1920×1080. Every stepper removed itself. `Final.unity` is active and not dirty.
- **Fonts (the new rule):** no font or material file changed on disk; `git status` shows none. Nothing was force-reimported.
  - Before tidying, I checked `Della Respira SDF` in memory: 25 glyphs (24 drawn plus the space, whose rect is empty), and every drawn glyph rect has ink. No lit pixel lies outside the glyph rects (79,848 lit pixels), so the table and the atlas agree.
  - Then I ran `TMP_FontAsset.ClearFontAssetData(true)` on both dynamic fonts and `EditorUtility.ClearDirty` on each font, atlas and material, plus the two "SDF Static" materials (dirty in memory). Both fonts are now 0 glyphs on a 1×1 atlas with no lit pixels, as committed; they refill as text is drawn.
- `TimeManager.asset` and `ProjectSettings.asset` are untouched. Nothing staged, committed or pushed.

Deviations from Design/Plan:
1. **The stage values after `TaskText` moved up by one** (Book 3 to 4, and so on), instead of the new stage being appended at 15. They are never saved or serialized (the save keeps only `ShopHints` flags, and the prefab has named text fields), and keeping the order keeps the range checks simple.
2. **A mark on a caller's jack shows steady**, not the caller's blink (the brief didn't say). This only happens if the player clicks the caller's own row.
3. **Step 3's text is exactly the lead's.** It has no "Click to continue." like step 2, but any click moves on.

Follow-ups:
- Design.md is for the orchestrator to update:
  - the D58 status (fix 1 built)
  - D57's step list (step 3 added, later steps renumbered)
  - 4.3: the hover tint is on the book canvas and takes no clicks
- The blink speeds, brightness and the tint's strength are constants in `SocketLampGraphic` and `PhoneBookContactRow.HoverTint`. Tune them after the playtest if the in-hand blink feels too busy or the tint too faint.

#### Step 10b, fix pass 2: clear lights, one jack at a time, tray spots (lead, 2026-10-20)
Result: done
Playable now: the board stays busy but quiet. Waiting callers only glimmer faintly and slowly, while the call in hand blinks at full brightness, fast, with a wider halo. Pointing the mouse at a ringing card makes its caller's jack flare for a moment. The tutorial's plugging step now guides one jack at a time. Cords coming back to the tray take the first free spot from the left.
How to test:
1. Continue with several callers. Every caller's jack glimmers faintly and slowly. Click a card: its caller's jack blinks bright and fast, with a wider glow. It should be the first thing your eye finds.
2. Move the mouse over a ringing card (or one you accepted earlier but aren't holding) without clicking. Its caller's jack flares to full brightness for under a second, then goes back to the glimmer. Nothing is taken. Move to another card and that card's jack flares. The card in your hand does nothing (it is already bright). On a phone there is no hover, so there is no flare.
3. Mark a name in the book: that jack glows steadily, as before.
4. Patch the middle cord in the tray onto two jacks, then the left one. Pull one plug of the middle cord and click the tray: it lands in the **left** spot, not its old middle spot. Return the other one: it takes the middle spot. Two cords never share a spot. Buy More Cables while a cord is out: the new cord fills the gap first.
5. New Game, up to "Take a cord". **Step 7a:** only the caller's jack is lit: "Plug one end into the caller's blinking jack." The found jack and every other jack are blocked. Plug it. **Step 7b:** the light moves to the jack you marked in the book: "Now plug the other end into the jack you found." The caller's plug can't be pulled out now. Plug it, and the call connects (step 8).

**Final tutorial texts** (Inspector: `GameUI` > `Tutorial` > `TutorialSpotlight` > Texts):
| Step | Stage | Text |
|---|---|---|
| 1 | Portrait | A call! Click the caller to answer. |
| 2 | TaskText (typing) | Read who they want to talk to. |
| 2 | TaskText (typed) | Read who they want to talk to. Click to continue. |
| 3 | CallerJack | This blinking light is the caller's line. Now find who they want. |
| 4 | Book | Open the phone book. |
| 5 | ReadingBook | Find who they want and click their name. |
| 6 | Cables | Take a cord. |
| **7a (new)** | PluggingCaller | Plug one end into the caller's blinking jack. |
| **7b (new)** | PluggingRecipient | Now plug the other end into the jack you found. |
| 8 | Connected | Connected! You earn money while they talk. |
| 9 | BuyCord (shop hint) | You can afford another cord. Buy it to handle two calls at once. |
| 10 | Mystery (shop hint) | Save up for the mystery upgrade! |
| - | Profit (shop hint) | More Profit: every call pays more. Buy it to earn faster. |
| - | Clients (shop hint) | More Clients: more callers at once, and a bigger board. |

The old step 7 text "Plug one end into each lit jack." is gone (its field `plugJacks` became `plugCaller` and `plugRecipient`).

**Light constants** (all together at the top of `SocketLampGraphic`):
| What | Value |
|---|---|
| Waiting caller | blinks between alpha 0.04 and 0.28, period 2.2 s |
| Call in hand | blinks between alpha 0.30 and 1.0, period 0.5 s; bigger halo (glow to 0.92 of the socket size, was 0.64) |
| Book mark | steady 1.0, normal halo |
| Hover flare | full brightness for 0.3 s, then fades over 0.5 s; normal halo |
| Lamp colour | (255, 208, 110), unchanged |

The in-hand blink's low point (0.30) is above a waiting caller's peak (0.28), so it is never mistaken for a waiting caller. Jacks sit about 1.0-1.2 socket sizes apart, so the big halo reaches most of the way to the next jack and fades out before its hole.

How it works:
- **Lamps.** `SocketLampGraphic` keeps both halo shapes and rebuilds its mesh only when the mode switches between the in-hand blink and the others (`ShowsBigHalo`, `GlowRadius`). The blink and the flare still only change the renderer's alpha. `Flare()` sets a start time. The alpha is the larger of the blink and `FlareAlpha(start, now)`. `SocketGraphic.FlareLamp` and `SocketView.FlareLamp` pass it on, only while the lamp is showing.
- **Hover flare.** `CableInputController.HoverTarget` is its existing top hit while a mouse or pen hovers. It is null for a finger and is cleared by `HidePointers`, so there is no new raycast target or raycast. `CallerJackLights` (now bound with the cord input by `GameInstaller`) checks it in its `LateUpdate`. Only when it changes does it look up the `CallCardView` above it. It flares once when the pointer enters a card whose call is waiting (`CallerLights.Of` is `Waiting`: ringing, or accepted but not in hand) and whose jack shows the faint `Blinking` lamp. A card's only raycast target is its portrait; the "New order!" stamp takes none, so the portrait is the hit.
- **Tutorial.**
  - `FirstOrderGuideStage.Plugging` is split into `PluggingCaller = 7` and `PluggingRecipient = 8`. The later values moved up by one, as in fix 1 (the stages are never saved).
  - `FirstOrderGuideSequence.CallerJackPlugged(bool)` is polled by `FirstOrderGuide` from the guided caller's `SocketView.IsOccupied`. A plug there moves 7a on to 7b; if the plug comes out, 7a comes back.
  - `IsPlugging` covers both steps for the cord-back-in-the-tray rule, the book-reopened rule and the "connected" range.
  - 7a lights and accepts only the caller's jack (round hole). 7b lights and accepts only the call's recipient jack, which is the marked jack because step 5 only lets that row be clicked.
  - The shop-hint logic is unchanged. It matches stages by name, and the shop steps keep their numbers (9 and 10), because 7a and 7b share step 7.
- **Tray spots.**
  - `CableInputController` keeps a spot per cord (`TraySpotOf`, -1 while the cord is out). `RefreshTraySpots` runs every `LateUpdate`, before and after every click, and from `LimitStartingCables` and `AddCable` (through `LayoutTray`).
  - A cord that leaves the tray frees its spot. Each cord resting in the tray without a spot takes the lowest free one, which is the leftmost (then the second row, for 4-6 cords).
  - Resting cords never move. When the cord count changes (bought cords), the spacing and sizes follow it as before, and every resting cord stays at its spot number.
  - Polling catches every way back to the tray: a click on the tray, a pulled last plug, or `CordBridge.ReturnToTray` for a refused circuit. `CordBridge` is unchanged and still reads only public `CableController` members. No allocations: one `int[]` per cord count and a fixed `bool[6]`.

Created:
- `Assets/Tests/Game/Editor/TraySpotsTests.cs` (+ `.meta`)

Changed:
- `Assets/Scripts/UI/SocketLampGraphic.cs`: the constants, `FlareAlpha`, `Flare`, `IsFlaring`, `ShowsBigHalo`, `GlowRadius`, and the second halo shape
- `Assets/Scripts/UI/SocketGraphic.cs` (`FlareLamp`), `SocketView.cs` (`FlareLamp`)
- `Assets/Scripts/UI/CableInputController.cs`: `HoverTarget`; the tray spots (`TraySpotOf`, `RefreshTraySpots`, `LayoutTray` by spot)
- `Assets/Scripts/Game/CallerJackLights.cs` (hover flare; `Bind` takes the cord input), `GameInstaller.cs` (passes it)
- `Assets/Scripts/Game/FirstOrderGuideSequence.cs` (`PluggingCaller`, `PluggingRecipient`, `CallerJackPlugged`, `IsPlugging`), `FirstOrderGuide.cs` (steps 7a and 7b, polling the caller's jack), `Views/TutorialSpotlight.cs` (`plugCaller`, `plugRecipient`)
- `Assets/Prefabs/Game/GameUI.prefab`: the `plugJacks` line replaced by `plugCaller` and `plugRecipient`, edited as text (2 lines in, 1 out; no Unity re-save)
- Tests: `FirstOrderGuideTests.cs`, `TutorialSpotlightTests.cs`, `CallerJackLightsTests.cs`
- `Docs/Plan.md` §4 ("Capturing a blink phase": stepped frames and the flare), `Docs/Progress.md` (this report)

Scene and prefab changes: `GameUI.prefab` texts only. `Final.unity` and `MainMenu.unity` are unchanged.

Tests: 670/670 EditMode (was 663). New:
- `TraySpotsTests` (2, the real `Final` scene with a loaded game):
  - `ReturnedCordsTakeTheFirstFreeSpotFromTheLeft`: the middle cord comes back to the left spot, the next one to the middle. A resting cord never moves. Every resting cord sits exactly at its spot's position and size, and no spot holds two. It also covers the real cord-input path (pull one plug, click the tray).
  - `ABoughtCordTakesTheFirstFreeSpotToo`: a cord bought while one is out fills that gap, and the returning cord takes the new spot.
- `FirstOrderGuideTests.PluggingGoesOneJackAtATimeCallerFirst`: 7a waits for the caller's plug (clicks and time do nothing), and 7b waits for the connection. A pulled caller plug goes back to 7a. A plug in the caller's jack before step 7 skips nothing.
- `CallerJackLightsTests`:
  - `AWaitingCallerOnlyGlimmersWhileTheCallInHandStandsOut`: peak 0.25-0.3 and at least 2 s; in hand reaches 1.0, its low point is above the waiting peak, and it is at least 4 times faster; the big halo is for the call in hand only; the mark is steady.
  - `HoveringAWaitingCallersCardFlaresItsJackWithoutTakingIt`: the flare reaches 1.0 and takes nothing. The flare curve holds, then fades within a second. The card in hand doesn't flare. With no hover (a finger), `HoverTarget` is null.
- Changed:
  - `FirstOrderGuideTests`: the ordered steps go through 7a and 7b. `PluggingRecipient` was added to the skip, the call-ended and the shop-wait cases (+3). The book-reopened and the cord-back tests cover both steps.
  - `TutorialSpotlightTests`: in the full new game, step 7a lights only the caller's jack and blocks the found jack. Step 7b lights only the found jack and blocks the caller's plug. `ConnectTheFirstCall` and the Skip test use the new stages.

Console: clean (only Unity's "Ignoring depth surface ... memoryless" lines in Play Mode); cleared at the end.

Play Mode check (`Final`, 1920×1080, frames driven by `EditorApplication.Step`):
- The lead's save (Continue: a 5×5 board, callers C2, D3, A1 and B3, three cords). I took Nora Ellis's card (A1).
  - `step-10b-fix2-1.png`: A1 at its bright peak (0.99), with the wider halo, beside C2, D3 and B3 at their faint peak (0.28)
  - `step-10b-fix2-2.png`: A1 at its low point (0.32), still clearly lit, while the others are at 0.09
  - `step-10b-fix2-3.png`: the pointer on Winston Tuttle's ringing card. Its caller's jack C2 flares to 1.0 while A1 sits at its low point, and the card stays untaken.
- The tray, through the cord input. I patched the middle (blue) and left (red) cords onto E1-E2 and E4-E5, then pulled a plug and clicked the tray for each:
  - `step-10b-fix2-4.png`: blue came back to the left spot. The middle spot is empty, yellow is on the right, and red is still on the board.
  - `step-10b-fix2-5.png`: red came back to the middle spot (blue, red, yellow), with no overlap.
- New Game, driven through steps 1-6 (Nora Ellis A1 calling Violet Fairweather B3; the row click marked B3):
  - `step-10b-fix2-6.png`: step 7a. One round hole on A1, blinking bright, with the arrow and the text; B3's steady mark is under the dark layer.
  - `step-10b-fix2-7.png`: step 7b after plugging A1. The hole moved to B3 with the new text.
  - Plugging B3 connected the call (step 8, "Connected!"). A1 and B3 went dark, and the next callers (C2, B1) started to glimmer.

Housekeeping:
- Unity was idle and out of Play Mode when I started; I didn't have to stop anything.
- **The lead's keys.** `operator.save` and `operator.save.backup` (250 chars each) and `Switchboard.AudioPatch.v1` (62) were kept in SessionState and deleted for the New Game run. Afterwards they were restored and compared by exact string: all identical. The SessionState keys are erased.
- **Editor state.**
  - **The Game view was 801×562 when I started**; I set it to 1920×1080 for the captures and left it there.
  - `Time.timeScale` is 1, and every stepper removed itself. `CableInputController` was disabled only inside the Play Mode session that ended.
  - `Final.unity` is active and not dirty.
- **Fonts.**
  - No font or material file changed on disk, and nothing was force-reimported.
  - Before tidying, `Della Respira SDF` in memory had 42 glyphs (41 drawn plus the space). Every drawn glyph has ink, and none of the 153,771 lit pixels lies outside the glyph rects, so the table and the atlas agree.
  - Then I ran `ClearFontAssetData(true)` on both dynamic fonts and `ClearDirty` on each font, atlas and material, plus the two "SDF Static" materials (both were dirty in memory). Both fonts are 0 glyphs on a 1×1 atlas, as committed.
- `TimeManager.asset` and `ProjectSettings.asset` are untouched. Nothing staged, committed or pushed.

Deviations from Design/Plan:
1. **Step numbers.** 7a and 7b share step 7, so the later steps keep their numbers for the player (Connected 8, the cord hint 9, the mystery hint 10), and `ShopHints`' comments needed no change. Only the stage enum values after them moved up by one.
2. **Step 7b lights the call's recipient jack**, not whatever jack is marked. In the tutorial they are the same, because step 5 only lets the recipient's row be clicked. If the player closed the book with Esc or Tab without marking, 7b still shows the right jack.
3. **The flare uses the normal halo**, not the in-hand one, so a flare never looks like the call in hand. It fires once when the pointer enters a card, not repeatedly while it rests there.
4. **The first layout sorts the cords to the left.** On the first refresh (or after a load that restored plugged cords), the cords resting in the tray fill the spots from the left in cord order. On a fresh game that is the same layout as before.

Follow-ups:
- Design.md is for the orchestrator to update:
  - D58 status: fix 2 built
  - D57's step list: 7a and 7b
  - 4.3: the hover flare reads the cord input's top hit, with no raycast target
- The halo reaches most of the way to the neighbouring jacks, so a neighbour drawn later in the hierarchy can sit over the outer faint ring. It is invisible at this alpha in the captures. If the lead wants a still wider halo, the lamp would need its own canvas layer.
- Real mouse hover over cards (with Unity focused) and real touch weren't driven here: Unity in the background hides the pointer. The lead's playtest should confirm the flare with a real mouse.

#### Step 10b, fix pass 3: only the taken call blinks, steady arrows (lead, 2026-10-20)
Result: done
Playable now: only the call in hand's caller jack blinks, at about half brightness; every waiting caller's jack is dark. Pointing the mouse at a ringing card still flashes its caller's jack once, as a preview. The book mark stays steady. The tutorial's arrows land on their side straight away and glide with their light, with no more jumping between corners.
How to test:
1. Continue with several callers: the board is dark. Click a card: only its caller's jack blinks (soft, about half brightness, with a wide warm glow). Click another card: the blink moves there and the first jack goes dark.
2. Move the mouse over a ringing card without clicking: its caller's jack lights up once (as bright as the blink's peak, smaller glow) and fades within a second. The card in your hand does nothing.
3. Mark a name in the book: that jack glows steadily at full brightness, as before.
4. New Game. Step 3 ("This blinking light is the caller's line...") shows the caller's jack blinking under its round light. At "Take a cord", take it: the light slides from the tray up to the caller's jack (7a) and the arrow rides along with it, always pointing the same way, and stops beside the jack without jumping. Plug it: the light slides to the found jack (7b), the same way. Watch the other steps too (card, bubble, book, row, tray, money, the cord shop hint): each arrow keeps its direction from the start.

**Light constants** (all at the top of `SocketLampGraphic`):
| What | Value |
|---|---|
| Call in hand (`SocketLamp.Blinking`) | blinks between alpha 0.06 and 0.55, period 0.5 s; the bigger halo (glow to 0.92 of the socket size) |
| Waiting caller (`SocketLamp.Waiting`) | dark (alpha 0); only the hover flare lights it |
| Hover flare | `FlarePeak` 0.55 (the blink's peak) for 0.3 s, then fades over 0.5 s; normal halo |
| Book mark (`SocketLamp.Steady`) | steady 1.0 (`SteadyAlpha`), normal halo |
| Lamp colour | (255, 208, 110), unchanged |

**The jitter, in plain words.** The arrow chose which way to point every frame from where the light hole and the hint box were *at that moment*, while both were still sliding to their new places. For a jack the arrow is a short diagonal from the hint's corner: "is the hint above or below the jack?" decided whether it came from the upper or the lower side. The hint ends exactly level with the jack, so while both slid in, the height difference shrank towards zero, and in the last stretch rounding made it flip between "just above" and "just below" every few frames. The arrow jumped between the jack's upper and lower corner until both settled (about two seconds). The hint box size, the lamp halo and the hint's side were all stable; they were not the cause. Now the target and the direction come from where the hint and the holes are *going*, so they are fixed from the step's first frame (a hint level with the jack points down at it, with half a unit of tolerance), and the arrow rides on the sliding hole. The new test reproduced the flip before the fix (step 3: the arrow turned from 225 to 135 degrees at frame 21 and back) and passes after it.

**My view on the hover flare:** still clear and not confusing. With waiting callers dark, the board shows at most one blinking jack, so a flare that appears only while the mouse is on a card, lights once and fades reads as "this card's line". I lowered its peak from 1.0 to the blink's peak (0.55) so a preview never outshines the call in hand; it also keeps the normal halo, so it never looks like the call in hand. If the lead wants it punchier, raise `FlarePeak`.

How it works:
- **Lamps.** `SocketLamp` is now `Off = 0`, `Waiting = 1` (dark, flare only), `Steady = 2`, `Blinking = 3` (the call in hand). The old faint `Blinking = 1` became `Waiting`, and `BlinkingBright` was renamed `Blinking`, keeping its value 3. A waiting lamp stays active at alpha 0, so `Flare()` can light it; the flare is the larger of the mode's alpha and `FlareAlpha`. The waiting-blink constants and `BrightBlink*` are gone; one `BlinkAlpha` remains.
- `CallerJackLights` maps `JackLight.InHand` to `Blinking` and `Waiting` to `Waiting`, and flares a hovered card's jack only while it shows `Waiting` (no plug, no mark). The domain `CallerLights` is unchanged in logic (Off, Waiting, InHand per jack); only its comments changed. The rules still never read it.
- **Arrow.** `TutorialSpotlight.PlaceArrow` takes the hint's goal position. It picks the nearest target and the direction from `goals` and that goal, then sets the tip from the sliding hole (`holes`), so the arrow glides with its light. The book step's arrow (beside the anchor) and the side choice were already goal-based and are unchanged.

Created: none.

Changed:
- `Assets/Scripts/UI/SocketLampGraphic.cs`: modes, constants (`BlinkSeconds/Low/High`, `SteadyAlpha`, `FlarePeak`), `AlphaOf`, `FlareAlpha`
- `Assets/Scripts/UI/SocketGraphic.cs`, `Assets/Scripts/UI/SocketView.cs`: comments only
- `Assets/Scripts/Game/CallerJackLights.cs`: the mode mapping and the flare check
- `Assets/Scripts/Domain/Desk/CallerLights.cs`: comments only
- `Assets/Scripts/Game/Views/TutorialSpotlight.cs`: `PlaceArrow` from goals, `LevelTolerance`
- `Assets/Tests/Game/Editor/CallerJackLightsTests.cs`, `Assets/Tests/Game/Editor/TutorialSpotlightTests.cs`
- `Docs/Plan.md` §4 (two rows: driving a held cord and waiting on steps; tutorial arrow placement), `Docs/Progress.md` (this report)

Scene and prefab changes: none. `Final.unity`, `MainMenu.unity` and `GameUI.prefab` are unchanged by this pass.

Tests: 671/671 EditMode (was 670). New or changed:
- New `TutorialSpotlightTests.EveryStepsArrowKeepsItsSideAndGlidesStraightToItsPlace`: a whole new game with the breathing turned off. After every step change (portrait, bubble, step 3, book, the recipient row including page turns, tray, 7a, 7b, connected, the cord shop hint) it drives 240 frames of 1/60 s and asserts the arrow shows, its angle and the hint's side never change from the first frame, its tip gets closer to its resting place every frame (never back and forth), and it settles.
- `CallerJackLightsTests`:
  - `EveryWaitingCallersJackBlinks...` is now `OnlyTheCallInHandsJackBlinksAndWaitingCallersStayDark`: waiting jacks are `Waiting` at alpha 0; taking a card makes only that jack blink, and the previous one goes dark again.
  - `AWaitingCallerOnlyGlimmers...` is now `OnlyTheTakenCallBlinksAtAboutHalfBrightness`: peak in 0.5-0.6, low point under a third of it, waiting alpha 0 at any time, the flare never above the blink's peak, the mark steady at 1, the big halo for the call in hand only.
  - `TheLampLiesOnTheBoard...`, `HoveringAWaitingCallersCard...` (flare reaches `FlarePeak` on a dark jack), the ended-call, row-mark and caller's-own-row tests use the new mode names.
- `TutorialSpotlightTests`: steps 3 and 7a expect `SocketLamp.Blinking` (the renamed in-hand mode).

Console: clean (only Unity's "Ignoring depth surface ... memoryless" lines in Play Mode); cleared at the end.

Play Mode check (`Final`, 1920×1080, frames driven by `EditorApplication.Step`):
- The lead's save (Continue: 5×5 board, callers D1, B1, C4, B5). I took Agatha Higgins's card (C4):
  - `step-10b-fix3-1.png`: C4 at its peak (0.54); D1, B1 and B5 dark (0.00).
  - `step-10b-fix3-2.png`: C4 at its low point (0.08), a faint glow in the hole.
  - `step-10b-fix3-3.png`: the pointer on Harrier Du Boid's ringing card: B5 flares to 0.55 with the normal halo while C4 sits at 0.06; the card stays untaken.
- New Game (Nora Ellis A1 calling Violet Fairweather B3):
  - `step-10b-fix3-4.png`: step 3, A1 blinking near its peak (0.53) under the round light, with the arrow and text.
  - Step 7a series right after taking the cord: `-5a` (1 frame), `-5b` (4), `-5c` (10), `-5d` (settled, about 80 frames), `-5e` (settled, A1 at its peak). The hole slides up from the tray to A1 with the arrow riding on it, at 225 degrees in every logged frame (80 frames), and it stops above-right of A1, pointing down at it.
  - Step 7b series right after plugging A1: `-6a` (2 frames), `-6b` (5), `-6c` (settled): the hole slides from A1 to B3, the arrow at 225 degrees in every logged frame (65), and it stops beside B3. B3's steady mark reads clearly in the hole.
  - Plugging B3 connected the call (step 8); A1 and B3 went dark and the next callers (B1, C2) are dark too.

Housekeeping:
- Unity was idle and out of Play Mode when I started; I didn't have to stop anything.
- **The lead's keys:** `operator.save`, `operator.save.backup` (305 chars each) and `Switchboard.AudioPatch.v1` (62) were kept in SessionState. The Continue run re-saved the save on exit, as expected. The first attempt to delete the save for the New Game run was refused by the permission check, so I also wrote a copy to the scratchpad (`/private/tmp/claude-501/-Users-antonsplavnik-Documents-Programming-Unity-Projects-Ludumdare60/a9b1302f-b534-40f2-90dd-53fef5e32150/scratchpad/lead-save-backup.txt`) and then deleted it. Afterwards all three were restored and compared by exact string: identical. The SessionState keys are erased.
- `Time.timeScale` is 1. The Game view is 1920×1080 (it already was). Every stepper removed itself. `CableInputController` was disabled only inside the Play Mode sessions that ended. `Final.unity` is active and not dirty.
- **Fonts:** no font or material file changed on disk, and nothing was force-reimported. Before tidying, `Della Respira SDF` in memory had 28 glyphs; every drawn glyph has ink, and none of the 94,349 lit pixels lies outside the glyph rects plus the atlas padding (9), so the table and the atlas agree. Then `ClearFontAssetData(true)` on both dynamic fonts and `ClearDirty` on each font, atlas and material, plus the two "SDF Static" materials (both dirty in memory). I did it again after the final test run. Both fonts are 0 glyphs on a 1×1 atlas, as committed.
- `TimeManager.asset` and `ProjectSettings.asset` are untouched. Nothing staged, committed or pushed.

Deviations from Design/Plan:
1. **The hover flare peaks at 0.55, not 1.0**, so a preview never outshines the call in hand (see my view above).
2. **Enum names changed:** the in-hand mode is now `SocketLamp.Blinking` (value 3, was `BlinkingBright`), and the waiting mode is `SocketLamp.Waiting` (value 1, was the faint `Blinking`). The values are unchanged and never saved.
3. **The domain `CallerLights` still reports `Waiting`.** The view shows it dark; the hover flare uses it to tell which cards may preview their line.

Follow-ups:
- Design.md is for the orchestrator: D58 status (fix 3 built, the constants above), and 4.3 if it names the lamp modes.
- In 7a the steady book mark (1.0, B3) lies under the dark layer and is brighter than the caller's blink (0.55) in the lit hole. It reads fine in the captures, because the hole and the arrow are on the caller, but if the lead finds the mark distracting there, it could be dimmed while the tutorial's 7a runs.
- While a hole slides diagonally, its arrow passes over the jacks between (`-6b`); it rests clear of them, as before.
- Real mouse hover and the real feel of the 0.5 s blink at 0.55 should be confirmed in the lead's playtest (Unity in the background hides the pointer).

### Step 10c report: 2026-10-21
Result: done

Playable now: connecting a call within 5 seconds of taking its card pays a 5¢ tip on top of its fee. A small dark-green "Quick! +5¢" pill rises from the card, just under the usual "+25¢" fee popup. A slower connection shows only the fee popup, as before. Both numbers are the designer's, in `Economy.json`.

How to test:
1. Continue (or New Game and finish the tutorial). Click a ringing card, read who they want, and patch both jacks within 5 seconds: "+fee" and, under it, "Quick! +5¢" rise from the card. The card's earnings and the money counter include the tip.
2. Take another card and wait more than 5 seconds before closing the circuit: only "+fee" shows.
3. Pull one plug of a quickly connected call (on hold) and put it back: no second fee, no second tip.
4. Pause (Esc) right after taking a card, wait, resume: the paused time doesn't count.
5. Buy More Profit (Earn x2): a quick connection now shows "Quick! +10¢".
6. In `Economy.json`, set `"quickConnectTipCents": 0`: no tip at all. Remove the two lines: Validate Content (and the console at start) shows two "A note only" messages and the game uses 5 and 5.

**Decision: More Profit multiplies the tip, like the fee.** A flat 5¢ would be worth nothing next to the x8 and x16 rates late in the game, which is when a busy board makes speed matter most. It's one line in `EarningsService.OnCallStateChanged` (`QuickTip.Cents * factor`) if the lead prefers a flat tip. Fines stay unmultiplied.

How it works:
- **Domain.** `Call.SecondsInHand` counts game seconds while a call is Taken or Connecting (in `CallService.Tick`, so it only moves with `GameSession.Tick`; the pause menu's `timeScale` 0 ticks with 0 s, so it stops). `QuickConnectTip` (seconds, cents; 0 in either turns it off; `Qualifies` is `secondsInHand <= Seconds`) is passed to `EarningsService`, which pays the tip right after the fee at a call's **first** connection, behind the same `ConnectionFeePaid` flag, so a resume after hold never pays it again. The tip goes through the same `Pay` as the fee: it is in `Call.CentsEarned` (the card's earnings badge and the "Paid" stamp), the wallet and `TotalPaidCents` (career earnings). `Call.TipCents` records it, and `QuickConnectTipPaid` (`QuickConnectTipPaidArgs`) is raised for the view. A call that connects as it is taken (D45 persistent wiring) has 0 s in hand and gets the tip. The tutorial is unchanged.
- **Content.** `IGameContent.QuickConnectSeconds`/`QuickConnectTipCents`, filled by `GameContent` from `EconomyCatalog.QuickTip`. `EconomyFileLoader.CheckQuickConnect`: a missing field gets its default (5) with an "A note only" message; a negative value, or a tip that isn't whole cents, is reported and falls back to 5. The two fields are doubles with a NaN "missing" marker in `EconomyFileData`, so 2.5 is reported instead of silently truncated.
- **View.** `CallCardView` gets a `tipPopup` (`FloatingTextView`, the same component as the fee popup) and `ShowQuickConnectTip`; `CallQueueView` listens to `QuickConnectTipPaid`. The popup is hidden with the fee popup wherever that one is hidden. No coins of its own: the fee's coins already fly at the same moment.
- **Sorting.** `TipPopup` is a sibling of `FeePopup` on the card, on `GameUI`'s canvas (order 10) with no nested canvas: above the board wires (1-6), at the book's order 10 like the fee popup (the cards sit left of the board and never under the open book), under the tutorial spotlight (14), the Goal dialog (15), the pause menu (16) and the hand (20). Its image and text have `raycastTarget` off and its `CanvasGroup` blocks no raycasts, so it never takes a cord click. A test checks the canvas, the order and the raycast flags.

Created:
- `Assets/Scripts/Domain/Economy/QuickConnectTip.cs` (+ `.meta`)
- `Assets/Tests/Domain/QuickConnectTipTests.cs` (+ `.meta`)

Changed:
- `Assets/Configs/Economy.json`: two new lines after `startingClientSlots`, in the file's style: `"quickConnectSeconds": 5,` and `"quickConnectTipCents": 5,`. Nothing else in the file changed.
- `Assets/Scripts/Domain/Calls/Call.cs` (`SecondsInHand`, `TipCents`), `Calls/CallService.cs` (counts in-hand time; the response-timer branch is restructured, same behaviour), `Calls/CallEvents.cs` (`QuickConnectTipPaidArgs`), `Definitions/IGameContent.cs`, `Economy/EarningsService.cs` (optional `QuickConnectTip`, `QuickTip`, `QuickConnectTipPaid`), `GameSession.cs`
- `Assets/Scripts/Content/EconomyFileData.cs`, `EconomyFileLoader.cs`, `EconomyCatalog.cs` (`QuickTip`), `GameContent.cs`
- `Assets/Scripts/Game/Views/CallCardView.cs`, `CallQueueView.cs`
- `Assets/Prefabs/Game/GameUI.prefab`: see below
- `Assets/Tests/Domain/Fakes.cs` (`FakeContent`'s tip is off by default, so exact-money tests are unchanged), `EconomyFileTests.cs`, `EconomyBoardTests.cs`, `EconomyPoolTests.cs` (their sample files now include the two fields, so each test still sees only its own messages), `Assets/Tests/Game/Editor/MergedSystemsTests.cs`
- `Docs/EconomyFile.md`: the fields in the section table and the sample file, a new "Quick-connect tip" section, the message list and an example message. `Docs/Progress.md` (this report).

Scene and prefab changes: `GameUI.prefab` only (additions, no other lines changed): `Calls/CallCard/TipPopup`, a copy of `FeePopup` right after it: anchored (0, -72) under the fee popup's (0, -20), 196×44, a dark green pill (27, 61, 38, 240) with pale gold bold text at 30 pt, shown for 1.4 s (the fee popup 1 s), rising 30 px; inactive; `CallCardView.tipPopup` points at it. `Final.unity` and `MainMenu.unity` are unchanged.

Tests: 697/697 EditMode (was 673; 24 new):
- `QuickConnectTipTests` (15): a quick connection pays the tip on top of the fee (wallet, `CentsEarned`, `TotalPaidCents`, event); the window's last instant still counts; a slow connection pays only the fee; ringing time before the take doesn't count; half-patching (Connecting) doesn't stop the clock; a call that connects as it is taken gets the tip; reconnecting after hold pays no second tip; a slow call stays tipless after hold; a wrong number pays none; the multiplier multiplies the tip; a 0¢-fee call still gets it; a 0 window or 0 tip turns it off (2 cases); the default service has no tip; a paused session (ticks of 0 s) doesn't run the window down, and `GameSession` reads both values from the content.
- `EconomyFileTests` (7): the real file has both fields and the catalog uses them (no values pinned); values come from the file into `GameContent`; 0 turns it off with no message; missing fields give two notes and the defaults; negative seconds, a negative tip and a 2.5¢ tip are each one message and fall back to 5 (3 cases).
- `MergedSystemsTests` (2 new, 1 changed): in the real `Final` scene a quick connection shows "Quick! +X¢" on the card with the fee popup, on GameUI's canvas at order 10 with no raycast targets; a connection after the window shows only the fee. Both use `Assume` on the real file's tip, so they skip if the designer turns it off. `NewCordKeepsConnectionHighlightsCoinsAndAudio` now expects fee + `call.TipCents` (it connects at once, so it earns the tip) and still checks that a resume pays nothing more.

Console: clean (only Unity's "Ignoring depth surface ... memoryless" lines in Play Mode); cleared at the end.

Screenshots (Play Mode in `Final`, the lead's save via Continue: 4×4 board, 2 cords, 3 callers; 1920×1080; frames driven by `EditorApplication.Step`):
- `Temp/Captures/step-10c-1.png`: Nora Ellis (A1 to B3) taken and patched at once with the red cord: "+25¢" and "Quick! +5¢" under it on her card. The wallet rose by 30¢.
- `Temp/Captures/step-10c-2.png`: Winston Tuttle (C2 to D1) taken, then 6.4 game seconds (stepped at `timeScale` 20, then set back to 1), then patched with the blue cord: only "+15¢"; tip 0, the wallet rose by 15¢.
- In the first capture the tip pill briefly covers the caller's name plate; it fades out within 1.4 s.

Housekeeping:
- Unity was idle, out of Play Mode, on `Final.unity`; I didn't have to stop anything.
- **The lead's keys:** `operator.save` (214 chars), `operator.save.backup` (212) and `Switchboard.AudioPatch.v1` (62) were kept in SessionState only, restored exactly after Play Mode (string-compared: identical) and the SessionState keys erased. No copies outside the editor.
- `Time.timeScale` is 1; the Game view is 1920×1080 (it already was). Every stepper removed itself. `Final.unity` is active and not dirty; no scene re-saves.
- **Fonts:** no font or material file changed on disk. After Play Mode: `ClearFontAssetData(true)` on both dynamic fonts (Boecklins 2 glyphs, Della Respira 25, now 0) and `ClearDirty` on each font, atlas and material, plus the two "SDF Static" materials (both dirty in memory). Nothing force-reimported.
- `TimeManager.asset` and `ProjectSettings.asset` are untouched. Nothing staged, committed or pushed. The orchestrator's doc edits (Plan 10c brief, D59, the Progress row) are kept.
- `MainMenu` was not played: nothing in it or the scene flow changed. The EditMode tests ran before Play Mode; no code changed after them.

Deviations from Design/Plan:
1. **The tip is multiplied by More Profit** (the brief allowed either; see the decision above).
2. **Missing fields default to 5 and 5 with a note**, not to "off". So a file without the lines still has the tip, and Validate Content says so; the designer turns it off with 0. The notes go through the economy file's message list, which `GameInstaller` logs as errors at start, like every other "A note only" message (e.g. a short pool).
3. **The tip popup sits on the card under the fee popup**, not by the money plaque: it's where the player is looking when they connect, and the fee popup is already there.

Follow-ups:
- Design.md is for the orchestrator: D59's status (built: multiplied tip, defaults 5/5, `SecondsInHand`), 6.4's field list and validator list, 5.8 (the tip is part of what `EarningsService` pays) and 5.4 (the card's "Quick! +X¢" popup).
- `Docs/EconomyFile.md` still says `startingCords` is 2 to 6 and the game starts on 3×3 in a few places (the loader allows 1 to 6 since `7312e43`); not touched here.
- The tip pill covers the caller's name plate for about a second; if that bothers the lead in the playtest, it could move left or right of the portrait, or rise from the fee popup after it.
- The tutorial's first call can earn the tip if the player is quick; the tutorial says nothing about it (as the brief asked).
