# Switchboard Operator: Implementation plan

This plan builds the game described in Design.md in small steps, each ending in something you can play. One agent builds each step through the Unity MCP server. Then we verify the result, you playtest it, and we move on.

Design: [Design.md](Design.md) · Progress: [Progress.md](Progress.md)

## 1. How a step runs

1. **Prep (orchestrator: the main Claude session).**
   - Pull teammates' changes.
   - Check that Unity is connected, idle, and has a clean console.
   - Make sure the step's decisions (Design §10) and coordination items (§8) are settled.
   - Refine the step's brief with anything learned so far.
2. **Build (one implementer agent).** The agent gets the brief, works only through Unity MCP, and returns a report (§7).
3. **Verify (orchestrator).**
   - Re-run all EditMode tests and read the console.
   - Run a Play Mode smoke test and take a screenshot.
   - Review the diff for scope creep and for edits to team files.
   - Send you the result and the playtest script.
4. **Playtest (you).** Follow the step's script in Unity and say what's broken or feels off.
5. **Fix (agent).** The agent gets the same brief plus your notes. Then go back to 3.
6. **Update docs (orchestrator).**
   - In `Progress.md`: the step's status, verification notes and playtest notes.
   - In `Design.md`: mark what is now built, and record any new decisions or accepted deviations.
   - In `Plan.md`: turn the next step's outline into a full brief, using what this step taught us.
7. **Commit (orchestrator, after your OK).** One commit per step, including the doc updates, with no AI attribution lines. You push, or tell the orchestrator to.
8. **Next step.**

Ground rules:
- **One agent at a time.** There is a single Unity editor. Parallel agents would fight over compilation and Play Mode.
- **No git worktrees.** The editor is tied to this folder.
- **Don't touch Unity while an agent works.** Save your own changes before a step starts.

## 2. Pre-flight checklist (before every step)
- [ ] Unity is open on this project with MCP for Unity connected. The orchestrator checks that `mcpforunity://instances` lists `Ludumdare60`. The editor is idle and not in Play Mode.
- [ ] Your own work in Unity is saved, and the console shows no errors.
- [ ] `git pull` is done, and there are no unexpected local changes.
- [ ] The step's coordination items are agreed with teammates (§8).
- [ ] The decisions the step needs are settled (Design §10).

## 3. Rules for implementing agents
1. **Read first:** Design.md; sections §3–§7 of this plan and your own step; Progress.md.
2. **Scope.** Build only your step. Put anything else you notice under "Follow-ups" in your report.
3. **Team files are read-only** (Design §3). If your step can't be done without changing one, stop and report the exact change you'd need.
4. **Placement.** Put code in the folders and assemblies of Design §4.1. Use the namespaces `Switchboard.Domain`, `Switchboard.Content`, `Switchboard.Game` and `Switchboard.Domain.Tests`.
5. **Style: match the existing code in `Assets/Scripts/UI`.**
   - Classes are `sealed` by default.
   - Fields are `[SerializeField] private`.
   - Types are small and focused.
   - Comments are few, and only explain why.
   - Use expression-bodied members where they read well.
6. **Domain rules.**
   - No `UnityEngine`.
   - Time only through `Tick(dt)` or `IGameClock`.
   - Randomness only through `IRandom`.
   - Money as `long` cents.
   - Every rule has an EditMode test.
7. **Data rules.**
   - Stable string ids.
   - Explicit enum values.
   - Never write to ScriptableObjects at runtime.
   - Our assets go only under `Assets/Content/` and `Assets/Prefabs/Game/`.
8. **Use Unity MCP for every project change:** scripts, assets, prefabs, scenes, build settings and tests.
   - Files that MCP can't create, such as `.asmdef` and `.jslib`, may be written with file tools. Run `refresh_unity` afterwards.
   - Use file tools for `Docs/*.md`.
9. **Keep the editor healthy.**
   - Compile and read the console after every batch of script changes.
   - Never leave compile errors.
   - Never leave Play Mode running.
   - Save every scene and prefab you change.
10. **Don't discard other people's work.** If a scene you need has unsaved changes you didn't make, stop and report.
11. **Check Unity APIs you're not sure of** with `unity_reflect` or `unity_docs`. Training data is often wrong about the Input System, TextMeshPro and Unity 6.
12. **Hands off git and settings.** Don't commit or push. Don't change packages or ProjectSettings unless your step says so.
13. **When blocked, stop and report.** That includes MCP disconnecting, tests you can't fix, or a gap in the design. Don't invent design.
14. **Finish** by appending your report (§7) to `Docs/Progress.md`. Return the same report as your final message.

## 4. Unity MCP cheat sheet (for agents)
Load the tools you need with ToolSearch first, for example `select:mcp__UnityMCP__create_script,mcp__UnityMCP__refresh_unity,…`.

| Need | Tool |
|---|---|
| Tool groups | `manage_tools` `list_groups`. If `testing`, `scripting_ext` or `docs` is off, turn it on with `activate`. |
| Check an API | `unity_reflect`, `unity_docs` |
| Create or edit C# | `create_script`, `script_apply_edits` / `apply_text_edits`, `validate_script`. Read files with `manage_script`. |
| Compile and wait | `refresh_unity` with `compile: "request"` and `wait_for_ready: true`, then `read_console` with `types: ["error","warning"]` |
| Readiness | Subagents can't read MCP resources such as `mcpforunity://editor/state`. Use `refresh_unity` with `wait_for_ready` and `manage_scene` `get_active` instead. |
| ScriptableObject assets | `manage_scriptable_object`:<br>• `create`: pass `type_name` and `folder_path`.<br>• `modify`: pass `patches` with SerializedObject paths. Object references look like `{"ref":{"guid":…}}`; sprite references also include `spriteName`. |
| Folders, moves, asset info | `manage_asset` |
| Prefabs | `manage_prefabs` |
| GameObjects and components | `manage_gameobject`, `manage_components`, `find_gameobjects` |
| Scenes | `manage_scene`: `create`, `load`, `save`, `get_hierarchy`, `get_active`, `validate` |
| Build scene list | `manage_build` `scenes` |
| Menu items (e.g. importing TextMeshPro essentials) | `execute_menu_item` |
| Many small operations at once | `batch_execute` (up to 25 commands) |
| Editor automation nothing else covers | `execute_code` |
| Tests | `run_tests` with `mode: "EditMode"`, then `get_test_job` with `wait_timeout: 60` and `include_failed_tests: true` |
| Play Mode smoke test | `manage_editor` `play` → `read_console` a few times, take a screenshot → `manage_editor` `stop` |
| Screenshot | `manage_camera` `screenshot` with `include_image: true` and `output_folder: "Temp/Captures"`. Temp is git-ignored; never save screenshots under `Assets/`. Take screenshots while Play Mode is paused or being stepped: captures during free-running play can come out blank. |
| Play Mode while Unity is in the background | Run In Background is off, so Play Mode stops advancing whenever Unity isn't the front window. Step 1 learned this. To drive frames:<br>1. From `execute_code`, add a self-removing `EditorApplication.update` callback that calls `EditorApplication.Step()`.<br>2. Optionally set `Time.captureDeltaTime`, for example to 0.05.<br>3. Set it back to 0 afterwards.<br>**Never set `Application.runInBackground` in the editor: it writes PlayerSettings.** |
| Simulate input (cards, buttons, cords) | `execute_code` runs outside the player loop, where `Screen` has the size of an editor view, so raycasts made from it miss.<br>1. Queue a one-shot `Canvas.willRenderCanvases` handler.<br>2. Inside it, run the clicks, using EventSystem raycasts or `CableInputController.ProcessPointer(screenPosition, true)`. The team built that method for integration checks.<br>3. Pass results back through `SessionState`, and remove the handler and its keys afterwards. |

## 5. Definition of done (every step)
- [ ] The project compiles. The console has no errors and no new warnings from our code.
- [ ] All EditMode tests pass: the team's existing tests plus new tests for every new rule.
- [ ] A 10-second Play Mode smoke test in `Game.unity` shows no errors or exceptions. The screenshot is saved to `Temp/Captures/step-NN.png`.
- [ ] Every changed scene and prefab is saved, with no missing references.
- [ ] `Level.unity` still plays on its own, and the cord sandbox is unchanged.
- [ ] No team-owned file was changed, unless the brief lists it as a coordinated change.
- [ ] The report is in `Docs/Progress.md`, including any deviations from Design.md.

## 6. Launch prompt (orchestrator → agent)
```
You are implementing Step N of the Switchboard Operator game in this Unity project, using the Unity MCP server.
Read Docs/Design.md, Docs/Plan.md (sections 3–7 and "Step N") and Docs/Progress.md before changing anything.
Implement only Step N, following the rules in Plan §3 and the definition of done in Plan §5.
When you finish, append your report (Plan §7 format) to Docs/Progress.md and return it as your final message.
Notes from the orchestrator: <confirmed decisions, coordination results, playtest feedback to address>
```

## 7. Report format
```
## Step N report: <date>
Result: done | partial | blocked
Playable now: <one or two sentences>
How to test: <numbered steps, if they differ from the plan's playtest script>
Created: <files and assets>
Changed: <files and assets>
Scene and prefab changes: <...>
Tests: <passed>/<total> EditMode (new: <list>)
Console: clean | <warnings>
Screenshot: Temp/Captures/step-NN.png
Deviations from Design/Plan: <none | list with reasons>
Follow-ups: <ideas, debts, questions>
```

## 8. Coordination with the team

| When | What | With |
|---|---|---|
| Before Step 1 | `Game.unity` will load `Level.unity` additively. We rely on the public members listed in Design §3 and on the name `CallerQueuePanel`. Please say before renaming any of them. | Sergei |
| Before Step 2 | Done (2026-10-17): the lead confirmed D07. Pulling a plug puts the call on hold, using the new restore feature. | Lead (and Sergei) |
| Before Step 5 | A way to add cords at runtime: a cable prefab plus a tray slot, and a way to register them with `CableInputController` and the tray. | Sergei |
| Before Step 6 | Touch input in the cord code: use `Pointer.current` instead of `Mouse.current` in `CableInputController.Update`, and hide the hand cursor on touch. | Sergei |
| Before Step 6, or earlier if Sergei's cord work settles | Merge into one scene (D29). Agree a time when Sergei isn't editing `Level.unity`; the merge itself takes about 5 minutes. | Sergei |
| Before Step 7 | Decide how locked board sections look: cover plates in our overlay, or a change to the board prefab. | Sergei, Mia |
| Before Step 8 | Sound effects (ring, alarm, coins, paid, fine, hang-up): find them or generate them. | Mia, team |

## 9. Steps

| # | Step | Playable result |
|---|---|---|
| 1 | First call: take it, patch it, get paid | One call at a time, end to end, with live money |
| 1b | Connection fee, Sergei's book, layout | Fee on connect; the real phone book shows our people; no overlaps |
| 2 | A busy switchboard | Several callers, callbacks, hold and disconnect, a dev panel |
| 3 | The phone book and real cards | Look people up in the book with tabs; readable cards in every state |
| 4 | Emergencies and fines | Timed emergency calls and fair fines |
| 5 | Upgrades, shop and the mystery goal | Buy cords and multipliers; save up toward the mystery |
| 6 | Saves, Boot scene, first Web build | Plays in a browser, and progress survives a reload |
| 7 | Board growth and pacing | The board and the difficulty grow over a session |
| 8 | Sound and feel | Rings, alarms, coins and animations |
| 9 | Mobile and Web polish | Good on phones and desktop browsers |
| 10 | Balance | A tuned time to reach the mystery upgrade |

Steps 1 and 2 are fully specified. Steps 3–10 are outlines. The orchestrator turns each one into a full brief before launching it.

---

### Step 1: First call: take it, patch it, get paid

**Goal.** The core loop end to end, one call at a time, on the new architecture.

**Playable result.**
- Press Play in `Game.unity`. The team's board and cords appear unchanged.
- A call rings in the top caller slot.
- You take the call, look up the two jacks in a simple list, and patch them.
- Money counts up live until the call ends. The cord then returns to the tray and the next call rings.

**Before launch.** Complete the coordination item for Step 1 (§8).

**Build**
1. **Assemblies and folders** (Design §4.1):
   - `Assets/Scripts/Domain/Switchboard.Domain.asmdef`: name `Switchboard.Domain`, rootNamespace `Switchboard.Domain`, `noEngineReferences: true`.
   - `Assets/Scripts/Content/Switchboard.Content.asmdef`: references `Switchboard.Domain`, rootNamespace `Switchboard.Content`.
   - `Assets/Tests/Domain/Switchboard.Domain.Tests.asmdef`: copy the settings of `Assets/Tests/Editor/Switchboard.CableTests.asmdef`, referencing `Switchboard.Domain` and `Switchboard.Content`.
   - `Assets/Scripts/Game/` has no asmdef.
2. **Domain** (`Switchboard.Domain`):
   - **Common:**
     - `IRandom`, and `SeededRandom` wrapping `System.Random`.
     - `IGameClock` and `GameClock`.
     - `FloatRange`: `[Serializable]`, with public `min` and `max`.
     - `JackId`, `CordId` and `CallId`: `readonly struct`s with value equality.
   - **Content interfaces:** `IGameContent`, `IPerson`, `ICallerType`, `ICaller` and `IDirectoryEntry`. Include only the Step 1 fields from Design §6.1.
   - **Calls:**
     - `CallState` and `CallEndReason`, with the full enums from Design §5.1.
     - `CallRequest`, `Call` and `CallService`. Step 1 uses only Incoming, Taken, Connected and Ended.
   - **Spawning:**
     - `CallFactory`.
     - `CallSpawner`: when no call is live, spawn one 2 s later. The caller is a random caller; the recipient is a random other person in the directory.
   - **Directory:** `PhoneDirectory`.
   - **Desk:** `ISwitchboard`, `OperatorDesk` and `ConnectionRule`.
     - The right pair, in either order → Connected.
     - A wrong pair → Ended(WrongConnection), and the cord returns to the tray.
     - A circuit closed with no call in hand → `Refuse`.
     - Ended(Finished) → the cord returns to the tray.
     - A plug pulled from a connected call → Ended(DisconnectedEarly), and the cord returns to the tray. This is temporary until Step 2 (Design §5.1).
   - **Economy:** `Wallet` and `EarningsService`. The multiplier is the constant 1.
   - **`GameSession`:** built from `IGameContent`, `IRandom` and `ISwitchboard`. It uses the tick order from Design §4.4, without callbacks.
3. **Content** (`Switchboard.Content`): `GameConfig`, `PersonDefinition`, `CallerTypeDefinition`, `CallerDefinition` and `PhoneDirectoryDefinition`. Each has `[CreateAssetMenu(menuName = "Switchboard/<Name>")]` and implements its Domain interface.
4. **Assets** (`Assets/Content/`):
   - `CallerTypes/Regular.asset`: id `type.regular`, 3–8 ¢/s, duration 15–30 s.
   - `People/`: 12 people. Use the portraits `Assets/Sprites/callers/caller-01.png` … `caller-12.png`, with names that follow Design §6.3.
   - `Callers/`: one Regular caller per person, weight 1.
   - `PhoneDirectory.asset`: the 12 people on 12 different jacks spread over A1–F6, with at least one per row.
   - `GameConfig.asset`: references all of the above, with `startingCents` = 0.
5. **Game layer** (`Switchboard.Game`, in `Assets/Scripts/Game/`):
   - `GameInstaller` and `GameLoopRunner`, as described in Design §4.4. Load `Level.unity` additively if it isn't already loaded.
   - `CordBridge`, as described in Design §4.5.
   - `MoneyCounterView`: TextMeshPro. It polls the wallet and updates only when the displayed value changes.
   - `CallCardView` and `CallQueueView`: one card for now, aligned to the first caller slot (Design §3).
     - Waiting: the card shows the portrait, the rate and a ringing pulse.
     - Clicking it takes the call and shows a ticket such as "Ada Moss → Basil Hart".
     - When the call ends, a stamp shows for 1.5 s, e.g. "Paid $1.20" or "Wrong number!".
   - `DirectoryListView` (temporary): a "Directory" button and the D key toggle an overlay that lists "Surname, First — C4", sorted by surname.
   - `Assets/Prefabs/Game/GameUI.prefab`: the canvas described in Design §4.3, with the HUD, the card and ticket, and the directory button and overlay.
   - Import the TextMeshPro essentials through `Window/TextMeshPro/Import TMP Essential Resources`; the project doesn't have them yet.
6. **Scene and build:**
   - `Assets/Scenes/Game.unity` contains the `Game` object (`GameInstaller`, `GameLoopRunner`, `CordBridge`) and a `GameUI` instance. No camera and no EventSystem.
   - Build list: `Game` (0), `Level` (1). Remove `SampleScene` from the list, but keep the file.

**Tests** (`Switchboard.Domain.Tests`)
- **Wallet:**
  - 1,000,000 × `Earn(0.0001)` adds exactly 100 cents.
  - The fraction stays in [0, 1).
  - `ApplyFine` floors the balance at 0 and returns the amount actually taken.
- **Earnings:** a 20 s call at 5 ¢/s, ticked with uneven dt between 0.016 and 0.05, pays exactly 100 cents. Nothing is paid after the call finishes.
- **CallService:**
  - Incoming → Taken → Connected → Ended(Finished) works.
  - Rejected: connecting a call that wasn't taken, taking a call twice, taking an ended call.
- **OperatorDesk**, with a fake switchboard:
  - The right pair connects, in both orders.
  - A wrong pair ends the call and returns the cord.
  - A circuit closed with no call in hand is refused.
  - A finished call returns its cord.
  - A pulled plug ends the call as DisconnectedEarly.
- **CallSpawner:** one call after the delay, and never two at once.
- **CallFactory:** values stay within the type's ranges, and a caller's override wins.

**Done when** §5 is met and every test above passes.

**Playtest script (you)**
1. Open `Assets/Scenes/Game.unity` and press Play.
2. Within about 2 s, a caller rings at the top of the left column, showing a portrait and a rate.
3. Click the card. The ticket shows who's calling and who they want.
4. Press **Directory** (or D), find both names and their jacks, and close the list.
5. Patch the two jacks with a cord. Money starts counting up at once and keeps rising smoothly.
6. Wait. The call ends by itself and the stamp shows what you earned. The cord returns to the tray, and a new call rings.
7. Take the next call and patch a wrong jack on purpose. You see "Wrong number!", the cord bounces back, and no money is paid.
8. With no call taken, patch any two jacks. The cord bounces back with the blocked sound.
9. During a call, click one of its plugs. The call ends ("Cut off"), and payment stops at that moment.
10. Open `Level.unity` on its own and press Play. Sergei's cord sandbox works exactly as before.

**Out of scope:** several waiting calls, callbacks, on hold, the real book, emergencies, upgrades, saves, new sounds.

---

### Step 1b: Connection fee, Sergei's book, layout fixes

**Goal.** Add the connection fee (D32), show our directory in Sergei's phone book (D30, D31), and fix the overlaps found in the Step 1 playtest.

**Playable result.**
- A ringing card shows "10¢ + 5¢/s".
- Connecting it correctly adds the fee at once, with a "+10¢" popup, then money rises per second. The "Paid" stamp includes the fee.
- Sergei's phone book lists our 12 people with their jacks, in English. Our temporary Directory button and list are gone.
- The money counter sits above Sergei's upgrade panel, and nothing of ours overlaps his UI.

**Before launch.** The book hook described below must be agreed with Sergei. **Recommended effort:** high.

**Build**
1. **Fee: domain and content.**
   - `CallerTypeDefinition` gets `connectionFeeCents` (a `FloatRange`, default 5–15). `CallerDefinition` gets an optional `connectionFeeCentsOverride` (0 = use the type).
   - `CallFactory` rolls the fee like the rate. `CallRequest` gets `ConnectionFeeCents`.
   - The fee is paid once, in the wallet, at the moment `OperatorDesk` connects the call correctly. It is multiplied by the earnings multiplier (still the constant 1). `Call.CentsEarned` includes it.
   - Wrong connections pay nothing. Paying must not depend on the tick order.
   - `MoneyFormat` gets a format for the card: `10¢ + 5¢/s`.
2. **Fee: UI.**
   - The card's rate badge shows fee plus rate.
   - A "+10¢" popup on connection: simple, pooled or reused, fading over about 1 s, near the money counter or the card.
3. **Sergei's book shows our directory** (coordinated change):
   - Add one public method to `Assets/Scripts/UI/PhoneBookView.cs`, e.g. `public void SetContacts(PhoneBookContact[] contacts)`. It replaces `directory.contacts`, keeps `contactsPerPage`, resets to spread 0 and refreshes the pages. That's about 5 lines, and nothing else in his files changes.
   - `GameInstaller` finds the `PhoneBookView` in `Level` by type, builds `PhoneBookContact { name = "Ada Moss", socket = "C3" }` entries from `PhoneDirectory.ListedEntries` (sorted by surname, as now), and calls `SetContacts`.
   - Leave `Configs/PhoneBook.json` untouched. Our data simply replaces it at runtime.
4. **Layout.**
   - Remove our `DirectoryListView`, the Directory button and the D-key toggle, from both the prefab and the code.
   - Move the money counter to the top of the right-hand column, above Sergei's `UpgradeShopPanel`, with no overlap.
   - Move the ticket to free space that doesn't overlap the shop panel, the book or the board. Report where.
   - Check by taking a screenshot of the merged scene with the book both closed and open.
5. **Docs and plan notes from Step 1:** `GameInstaller` logs an error if `Level` fails to load.

**Tests**
- Fee within its range, and the override wins.
- Fee paid exactly once on a correct connection.
- No fee on a wrong connection.
- Multiplier applied to the fee.
- A finished call earns fee + duration × rate.
- Card text format.

**Playtest script (you)**
1. Start from `MainMenu`: Play, then Start. You land in the game, and the board, cords and book appear.
2. A ringing card shows a fee and a rate, e.g. "10¢ + 5¢/s".
3. Take it and open Sergei's book. It lists our English names with jacks, and both names from the ticket are in it.
4. Patch correctly. Money jumps by the fee with a "+10¢" popup, then rises per second. The stamp at the end includes the fee.
5. A wrong patch adds no money.
6. Nothing of ours overlaps the upgrade panel or the book. The money counter sits above the upgrade panel.

**Out of scope:** everything in Step 2.

---

### Step 2: A busy switchboard

**Goal.** All the rules for regular calls, with several callers at once.

**Playable result.**
- Up to 6 callers wait in the left column, and you juggle them with 3 cords.
- Wrong numbers call back later, marked "calling again".
- Pulling a plug puts the call on hold; putting the plug back resumes it.
- A dev panel lets you speed things up.

**Before launch.** None. D07 was confirmed on 2026-10-17, so build the OnHold behaviour.

**Build**
- **Queue:**
  - Up to `maxWaitingCalls` (6) waiting cards, in arrival order. `CallQueueView` uses all 6 slots.
  - Taking a card while another is in hand swaps them (D03).
  - A call is `Connecting` while a cord is half-patched.
- **`BusyTracker`** (D18).
- **`CallbackScheduler`** (Design §5.3):
  - Uses `callbackDelaySeconds` on the caller type (default 20–40 s).
  - A callback card shows a "calling again" badge.
  - Callbacks wait while the queue is full or a party is busy.
- **Spawner:**
  - A `PacingProfile` asset: base interval (default 8 s), jitter ±30%, and a load factor.
  - No spawn while the queue is at its cap. Callbacks come first.
- **OnHold** (D07):
  - The bridge reports the pulled plug, and the call pauses its pay and duration.
  - If the same pair closes again, the call resumes.
  - If the plug goes into another jack, or the cord goes back to the tray, the call ends as DisconnectedEarly.
  - What the bridge sees:
    - Restore: the team's code snaps the plug back when you click empty space. That comes through `CordBridge` as `OnCircuitClosed` on the same pair while the cord is still bound to the on-hold call, so the desk resumes the call. It is not judged as a new circuit.
    - Replacing Step 1's cut-off: today `OperatorDesk.OnCircuitOpened` cuts the call off at once. That path becomes Hold.
- **`LiveCallsView`:** connected and on-hold calls, shown in the right-hand HUD column under the Directory button (Design 4.3). There is no room above the tray. When a call connects, its card leaves the caller slot. Each entry shows:
  - the portrait
  - a chip in the cord's colour (`CableController.CableColor`, read through the bridge)
  - the cents earned
  - "on hold" when relevant
- **Stamps:** "Paid", "Wrong number!", "Cut off". Use only glyphs in the static font atlas (Design 4.3).
- **Notes from Step 1:**
  - The top caller slot is partly off-screen. That is the team's panel layout; leave it.
  - `CallService.Take` currently refuses a second call; make it swap (D03).
  - `GameInstaller` should log an error if `Level` fails to load. Today it fails silently.
- **Dev panel** (`Assets/Scripts/Game/DevTools/`, compiled only under `#if UNITY_EDITOR || DEVELOPMENT_BUILD`). Toggle it with F1 or a small corner button. It offers:
  - add $10
  - spawn a call now
  - time ×1 or ×5
  - show hidden durations
  - clear calls

**Tests**
- The full transition table in Design §5.1, as a table-driven test (without Missed).
- Swap rules.
- Entering and leaving Connecting.
- Callbacks: the delay, waiting while the queue is full or a party is busy, and priority over new spawns.
- The busy rule.
- The spawner cap and load factor.
- OnHold: pay and duration pause; restoring resumes; moving the plug or returning the cord ends the call.
- Several calls paying at the same time.

**Playtest script (you)**
1. Play `Game.unity` and let 4–6 callers pile up. No more than 6 ever wait.
2. Take one card, then another. The first goes back and keeps its place.
3. Patch a wrong number. About 30 s later the same caller rings again, marked "calling again".
4. Connect three calls at once. Money rises faster, and each live call shows its own earnings.
5. Pull one plug of a live call. It goes on hold and its earnings freeze. Click empty space: the plug snaps back and the earnings resume.
6. Pull a plug and click the tray. The call is cut off.
7. Open the dev panel. Set time to ×5, spawn a call, and add $10.

**Out of scope:** the real book, emergencies, upgrades, saves, sounds.

---

### Step 3: Real cards (outline). The book itself was wired in Step 1b; this step polishes it with Sergei.
- **Book:** `DirectoryBookView` (Design §5.7). The book on the desk opens an overlay with:
  - pages
  - A–Z tabs
  - pooled rows
  - flipping by tap, swipe or keys

  Add D23 only if it has been decided.
- **People:** the directory grows to 36 people, one per jack. Portraits are reused, or silhouettes are used. Names follow Design §6.3.
- **Cards:** every state from Design §5.4, in TextMeshPro, styled to match the caller panel, with a ringing animation.
- **`ContentValidator`:** an editor menu item plus an EditMode test (Design §6.3).
- **Playtest:**
  - Anyone can be found in under 10 s using the tabs.
  - Cards are readable at 1280×720 and in a phone-sized Game view.

### Step 4: Emergencies and fines (outline)
- **Caller types:** the fields that Design §6.1 marks [4].
- **Emergency type:** a short response time, fines, and `maxSimultaneous` 1.
- **Emergency services** in the directory, tagged `emergency-service`: Police, Fire Brigade, Doctor.
- **Rules and feedback:**
  - The response timer and Missed.
  - `PenaltyService`, with fines floored at 0.
  - `FineApplied` popups.
  - Emergency card style with a countdown ring (D09).
  - The idle guard (D16). Our Game layer reports input activity.
- **Tests:**
  - Fines for each reason.
  - The floor at zero.
  - The timer stopping on a correct connection.
  - Missed clearing the call in hand.
  - The idle guard.
  - A wrong connection giving both a fine and a callback.
- **Playtest:**
  - Emergencies feel urgent but fair.
  - Leaving the game idle for 2 minutes doesn't drain your money.

### Step 5: Upgrades, shop and the mystery goal (outline). Wire Sergei's `UpgradeShopPanel` to our `UpgradeService` instead of building our own `ShopView` (D30).
- **Stats and upgrades:**
  - `StatSheet`, `UpgradeService`, `UpgradeDefinition`, `StatModifier` and `UpgradeEffect` (Design §5.9).
  - Assets: Extra Cord, Earnings Multiplier, and Mystery (`isGoal`, with `MysteryPlaceholderEffect`).
- **Shop:**
  - `ShopView`: the store tab on the right (`Sprites/store/store.png`) opens a list showing cost, level and a buy button.
  - The savings bar (D14).
  - Buying the goal shows "to be continued".
- **Extra cords at runtime** depend on the Step 5 coordination item (§8).
- **Tests:**
  - The cost curve.
  - Max level.
  - Purchase validation.
  - Stat recompute gives the same result when repeated.
  - Goal progress and the affordable event.
  - Effect hooks: `Apply` on load versus `OnPurchased` once.
- **Playtest:**
  - Buy a cord and see it in the tray.
  - Buy the multiplier and see income rise.
  - Watch the savings bar fill.
  - Buy the mystery and see "to be continued".

### Step 6: Saves and first Web build (outline). Sergei's `MainMenu` is the start screen: it replaces our planned `Boot.unity` and doubles as the tap-to-start audio unlock (D30, D33).
- **Saving:**
  - `SaveData` with a version number and migrations; `SaveService`.
  - `PlayerPrefsStorage` and `BrowserStorage` (a `.jslib` writing `localStorage`, plus a backup key).
  - Autosave as described in Design §5.12.
- **`Boot.unity`:** tap to start, then load `Game`.
- **Web build** through `manage_build`, to `Builds/Web/` (git-ignored).
- **Depends on** D26 (hosting) and the touch coordination item (§8).
- **Tests:**
  - Saving and loading gives back the same data.
  - Migration from a v1 test file.
  - Unknown ids are ignored.
  - A corrupt main save falls back to the backup.
- **Playtest (browser):**
  - Earn and buy, then reload: progress is restored.
  - Close and reopen the tab: progress is restored.
  - On a phone, tap to patch.

### Step 7: Board growth and pacing (outline)
- **Board growth:**
  - Sections in `BoardLayoutDefinition`: rows A–B at the start, then C–D, then E–F.
  - Cover plates over locked jacks in our overlay (D21).
  - A Bigger Board upgrade.
  - The directory lists only unlocked people.
- **Pacing:**
  - `PacingProfile` curves driven by progress (D19).
  - Caller type weights, with emergencies appearing later.
- **Tutorial:** scripted opening calls with hints such as "Click the ringing card", "Find them in the book" and "Connect the two jacks".
- **Tests:**
  - The spawner respects unlocked lines.
  - Weights follow progress.
  - The tutorial sequence.
- **Playtest:** a 30-minute session ramps up noticeably.

### Step 8: Sound and feel (outline)
- **Audio:** `AudioService`, `AudioCueLibrary` and `FeedbackDirector` (Design §5.11):
  - One ringing loop and one alarm loop.
  - Coin ticks.
  - Cues for paid, hung up, missed and fined.
- **Animation:**
  - Cards: a ringing wobble, sliding in and out, and stamps.
  - A rolling money counter.
  - Popups.
- **Depends on** the Step 8 coordination item for audio assets (§8).
- **Playtest:** it sounds and feels good without getting noisy.

### Step 9: Mobile and Web polish (outline)
- **Layout:**
  - Aspect ratios 16:9, 19.5:9 and 4:3.
  - A portrait "rotate your device" screen (D24).
  - Touch target sizes.
- **Performance:** nested canvases for the HUD and cards; a sprite atlas and texture compression.
- **Build settings:**
  - Remove unused packages (Visual Scripting, Multiplayer Center, Collab), once the team agrees.
  - Set compression for the host, with Decompression Fallback if needed.
- **Playtest:** on iOS Safari, Android Chrome and a desktop browser.

### Step 10: Balance (outline)
- **Simulation:** a headless EditMode simulation with a bot operator.
  - Its skill (lookup time, error rate) is configurable.
  - It runs hours of game time in seconds.
  - It reports the time to reach the mystery, the income curve and the fines.
- **Tuning:** adjust prices, rates and pacing toward the target in D27.
- **Playtest:** a full run from a new save feels right.
