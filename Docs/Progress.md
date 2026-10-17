# Progress

| Step | Status | Built | Verified | Playtested | Commit |
|---|---|---|---|---|---|
| 1. First call | done | ✅ 2026-10-17 (54 min) | ✅ 72/72 tests, scope clean | ✅ all 9 checks | see git log |
| 2. A busy switchboard | not started | | | | |
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
