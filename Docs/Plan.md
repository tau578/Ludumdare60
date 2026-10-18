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
7. **Commit and push only when the lead asks.** One commit per step, including the doc updates, with no AI attribution lines. A playtest OK alone is not a request to commit or push.
8. **Next step.**

Ground rules:
- **Every step and every fix pass is built by a subagent.** The main session (orchestrator) only prepares, launches, verifies and updates docs; it never implements a step itself.
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
   - **C# edits may also be made with file tools.** In Step 1b, MCP `apply_text_edits` corrupted three edits. After file edits, always compile through MCP (`refresh_unity`) and read the console.
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
| Play Mode while Unity is in the background | Run In Background is off, so Play Mode stops advancing whenever Unity isn't the front window. Step 1 learned this. To drive frames:<br>1. From `execute_code`, add a self-removing `EditorApplication.update` callback that calls `EditorApplication.Step()`.<br>2. Optionally set `Time.captureDeltaTime`, for example to 0.05.<br>3. Set it back to 0 afterwards.<br>**Never set `Application.runInBackground` in the editor: it writes PlayerSettings.**<br>`Time.captureDeltaTime` seemed to have no effect while stepping. To speed up long calls, set `Time.timeScale` temporarily and reset it to 1 afterwards.<br>`GameObject.Find` also returns inactive objects, so check `activeInHierarchy`. |
| Simulate input (cards, buttons, cords) | `execute_code` runs outside the player loop, where `Screen` has the size of an editor view, so raycasts made from it miss.<br>1. Queue a one-shot `Canvas.willRenderCanvases` handler.<br>2. Inside it, run the clicks, using EventSystem raycasts or `CableInputController.ProcessPointer(screenPosition, true)`. The team built that method for integration checks.<br>3. Pass results back through `SessionState`, and remove the handler and its keys afterwards. |
| `execute_code` quirks (2a) | It compiles with CodeDom (C# 6): no local functions, and `Object` is ambiguous, so write `UnityEngine.Object`. |
| Keyboard in Play Mode (2a) | The project uses `PointersAndKeyboardsRespectGameViewFocus`, so simulated keys (e.g. F1) don't reach Play Mode unless the Game view has focus. Call the action through the same method the key uses, and leave the real key press to the lead's playtest. |
| Screenshot names (2a) | `manage_camera screenshot` adds "-1" to the name instead of overwriting an existing file. Delete the old file first if the name matters. |
| Nested canvas sorting in a prefab (2b) | To give a child its own sorting order inside a prefab (e.g. the halo's order 9), set `m_OverrideSorting` through `SerializedObject`. `manage_prefabs` and the `overrideSorting` setter leave it off, because inside prefab contents the nested canvas counts as a root canvas. |
| Game view size (3) | Set it with `PlayModeWindow.SetCustomRenderingResolution`, for example 1280×720 or 844×390, and set it back to 1920×1080 afterwards. Each new size stays in the Game view's size list as an editor preference, not a project file. |
| Tests on Assembly-CSharp types (3) | `Assets/Tests/Editor/` (Sergei's asmdef) can't see Assembly-CSharp types such as `PhoneBookView`. Put such tests in our `Assets/Tests/Game/Editor/`, which has no asmdef and compiles into Assembly-CSharp-Editor. |
| Dev panel (2a) | F1 or the "Dev" button in the top-right corner: + $10, Call now, Speed ×5/×1, Show/Hide times, Clear calls. Editor and development builds only. Its IMGUI clicks also reach the cord input as "empty space" clicks. |

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
| Before Step 5 | Notepad upgrade (D23): Sergei's phone book reports when an entry is tapped (e.g. a public event with the contact's name and socket), so we can note the jack on the call in hand. | Sergei |
| Feedback (Step 3 playtest) | Holding a cord and clicking a button (the book, a tab, a caller card) drops the cord back in the tray, because any click off a jack counts as "empty space". The lead would find it easier if clicks on buttons kept the cord in hand. His call; we didn't change it. | Sergei |
| Before Step 6 | Touch input in the cord code: use `Pointer.current` instead of `Mouse.current` in `CableInputController.Update`, and hide the hand cursor on touch. | Sergei |
| Before Step 6, or earlier if Sergei's cord work settles | Merge into one scene (D29). Agree a time when Sergei isn't editing `Level.unity`; the merge itself takes about 5 minutes. | Sergei |
| Done in Step 1c (lead approved) | The upgrade panel's top header is now a 96 px `HeaderSlot` plaque holding our money counter. Sergei keeps the names `UpgradeShopPanel` and `HeaderSlot`. | Sergei |
| Before Step 7 | Decide how locked board sections look: cover plates in our overlay, or a change to the board prefab. | Sergei, Mia |
| Before Step 8 | Sound effects (ring, alarm, coins, paid, fine, hang-up): find them or generate them. | Mia, team |

## 9. Steps

| # | Step | Playable result |
|---|---|---|
| 1 | First call: take it, patch it, get paid | One call at a time, end to end, with live money |
| 1b | Connection fee, Sergei's book, layout | Fee on connect; the real phone book shows our people; no overlaps |
| 2a | Queue, callbacks, dev panel | Several callers, swapping, callbacks, a dev panel |
| 2b | Hold and resume | Pulled plugs put calls on hold; restore resumes |
| 3 | Phone book tabs, 36 people, real cards | Find anyone fast with A–Z tabs; the whole board in use; readable cards in every state |
| 4 | Emergencies and fines | Timed emergency calls and fair fines |
| 5 | Upgrades, shop and the mystery goal | Buy cords and multipliers; save up toward the mystery |
| 6 | Saves, Boot scene, first Web build | Plays in a browser, and progress survives a reload |
| 7 | Board growth and pacing | The board and the difficulty grow over a session |
| 8 | Sound and feel | Rings, alarms, coins and animations |
| 9 | Mobile and Web polish | Good on phones and desktop browsers |
| 10 | Balance | A tuned time to reach the mystery upgrade |

Steps 1–3 are fully specified. Steps 4–10 are outlines. The orchestrator turns each one into a full brief before launching it.

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

**Before launch.** Tell Sergei that `PhoneBook.json` now holds the game's caller data, with English names. **Recommended effort:** high.

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
3. **One people file** (D31). No change to Sergei's code is needed.
   - Rewrite `Assets/Configs/PhoneBook.json` with our 12 people in English: the names from Step 1, their jacks, and portraits `caller-01`…`caller-12`. Keep his format and add fields:
     `{ "name": "Ada Moss", "socket": "C3", "portrait": "caller-01", "type": "type.regular", "rate": 0, "fee": 0 }`.
     Keep `contactsPerPage`, and keep the contacts sorted by surname, since the book shows them in file order.
   - `GameConfig` references that `TextAsset` and holds a portrait list (sprite name → `Sprite`).
   - Add a loader in Content or Game that parses the file with `JsonUtility` into Domain data. Each person's surname is the last word of `name`.
   - Validation on load, with clear `Debug.LogError` messages:
     - jack exists on the board and is unique
     - portrait found
     - call type id known
     - name not empty
   - Add an EditMode test that validates the real file.
   - Delete the Step 1 assets under `Assets/Content/People`, `Assets/Content/Callers` and `PhoneDirectory.asset`, and the `PersonDefinition`, `CallerDefinition`, `PhoneDirectoryDefinition` and `DirectoryEntry` classes. Keep `CallerTypeDefinition` and `GameConfig`. Domain stays on its interfaces.
   - Sergei's book then shows the same people our calls use, because it reads the same file.

4. **Layout.**
   - Remove our `DirectoryListView`, the Directory button and the D-key toggle, from both the prefab and the code.
   - Move the money counter to the top of the right-hand column, above Sergei's `UpgradeShopPanel`, with no overlap.
   - **Replace the ticket with a speech bubble** next to the caller's card in the left caller column, pointing at the portrait.
     - It shows "Florence Abbott" / "wants Ada Moss" while the call is in hand, and "On the line" once connected.
     - It must not cover the board or other slots' cards.
     - Remove the old ticket from the right column.
   - Check by taking a screenshot of the merged scene with the book both closed and open.
5. **Docs and plan notes from Step 1:** `GameInstaller` logs an error if `Level` fails to load.

**Tests**
- People file: parses, all 12 entries are valid, and invalid entries (unknown jack, duplicate jack, unknown portrait, unknown type) are reported.
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
7. Taking a call shows a speech bubble next to the caller's portrait on the left, with who's calling and who they want.

**Out of scope:** everything in Step 2.

---

### Step 2a: Queue, callbacks and dev panel

**Goal.** Several regular callers at once: a queue, swapping, wrong numbers calling back, the busy rule. Plus a dev panel to speed up testing. **Recommended effort:** high.

**Playable result.**
- Callers pile up in the left column, one card per slot, never more than 6.
- You can take any card, and taking another swaps them.
- Wrong numbers call back later, marked "calling again".
- Several calls connect and pay at the same time.
- F1 opens a dev panel.

**Build**
- **Queue (D03, D17):**
  - Every live call (waiting, in hand, connected) has a card in one of the 6 `CallerQueuePanel` slots, in arrival order. `CallQueueView` uses all 6 slots, aligned the same way as slot 0 today.
  - Nothing spawns while all 6 slots are used.
  - Taking a card while another is in hand swaps them: the old one goes back to Incoming with its timers running. `CallService.Take` currently refuses; make it swap.
  - A call is `Connecting` while a cord is half-patched (Design 5.1).
  - Connected calls keep their card in their slot. The card shows the cord's colour (`CableController.CableColor`, read through the bridge), the cents earned, and the speech bubble's "On the line". The card leaves when the call ends, after its stamp.
- **`BusyTracker` (D18).** A person in any live call, or with a pending callback, can't be picked.
- **`CallbackScheduler` (Design 5.3):**
  - Add `callbackDelaySeconds` to the caller type (default 20–40 s).
  - A wrong connection schedules a callback for the same request, attempt + 1.
  - A callback card shows a "calling again" badge.
  - Callbacks wait while all slots are used or a party is busy, and they go ahead of new spawns.
- **Spawner:**
  - A `PacingProfile` asset: base interval (default 8 s), jitter ±30%, and a load factor (spawn slower when waiting cards outnumber free cords).
  - It replaces Step 1's "one call at a time" rule.
- **Speech bubble:** it belongs to the call in hand. With several cards, it points at the card in hand.
- **Dev panel** (`Assets/Scripts/Game/DevTools/`, compiled only under `#if UNITY_EDITOR || DEVELOPMENT_BUILD`). Toggle it with F1 or a small corner button. It offers:
  - add $10
  - spawn a call now
  - time ×1 or ×5
  - show hidden durations on cards
  - clear calls
- **Small fix:** `GameInstaller` logs an error if `Level` fails to load (check whether 1b already did this).
- **Unchanged in 2a:** pulling a plug still cuts the call off (Step 1 behaviour). Hold comes in 2b.

**Tests**
- Transitions Incoming ⇄ Taken (swap), Taken ⇄ Connecting, Connecting → Connected or WrongConnection, as a table-driven test.
- Callbacks: the delay, waiting while slots are full or a party is busy, and priority over spawns.
- The busy rule.
- The spawner cap, interval and load factor.
- Several calls paying at the same time.

**Playtest script (you)**
1. Play from `Game.unity`. Callers pile up, never more than 6.
2. Take one card, then another. The bubble moves to the new one, and the first goes back to ringing.
3. Patch a wrong number. 20–40 s later the same caller rings again, marked "calling again".
4. Connect three calls at once. Money rises faster, and each connected card shows its cord colour and its own earnings.
5. F1 opens the dev panel. Try time ×5, spawn a call, add $10, and show durations.

**Out of scope:** hold (2b), emergencies, upgrades, saves, sounds.

---

### Step 2b: Hold and resume (D07)

**Goal.** Pulling a plug from a live call puts it on hold instead of cutting it off. **Recommended effort:** high.

**Playable result.**
- Pull one plug of a connected call: its card shows "On hold" and its earnings freeze.
- Click empty space: the team's restore snaps the plug back, and the call resumes.
- Plug the pulled end into another jack, or send the cord back to the tray: the call is cut off ("Cut off").

**Build**
- **OnHold (Design 5.1):**
  - `OperatorDesk.OnCircuitOpened` on a bound cord puts its call OnHold (instead of cutting it off). Pay and the duration timer pause.
  - The same pair closing again resumes the call. This is the team's restore, reported by `CordBridge` as `OnCircuitClosed` on the same pair while the cord is still bound to the on-hold call. It is not judged as a new circuit, and the connection fee is not paid again.
  - The pulled plug going into another jack: the on-hold call ends as DisconnectedEarly, and the new circuit is judged against the call in hand (or refused if none).
  - The cord going back to the tray: DisconnectedEarly.
- **Card states:** "On hold" on the card. Stamps "Paid", "Wrong number!" and "Cut off", using only glyphs in the static font atlas (Design 4.3).
- **Check:** the stale `heldCable` edge case noted in Step 1 no longer matters, or is handled.
- **Notes from 2a:**
  - `OperatorDesk` now tracks half-patched cords and keeps the call in hand Connecting while any exists. Today it marks a cord half-patched in `OnCircuitOpened` only when no call is bound to it. Keep that: the single plug of an on-hold call's cord does **not** count as half-patched for the call in hand, because the cord belongs to the on-hold call.
  - An on-hold call still holds its caller slot and counts toward the 6-call cap and the busy rule. In the spawner's load formula, count it as neither waiting nor using a free cord? Decide, and document it. Suggested: it uses a cord, and isn't waiting.
  - The card already has a top badge (earnings or "Calling again") and the cord ring. "On hold" can replace the "On the line" bubble text, with the earnings badge frozen.
  - Use `CallEndReason.Cleared` only for the dev panel. The dev panel's "clear calls" must also clear on-hold calls.
- **Fix (lead, 2026-10-18): the ringing glow covers the price label of the card above.** When several cards ring, a card's amber halo overdraws the rate badge ("9¢ + 5¢/s") of the card above it. Every card's badges must stay fully readable. Either keep the halo inside its slot band, or draw badges above every halo. Check it with a screenshot of a full queue of 6 ringing cards.

**Tests**
- Hold pauses pay and duration.
- Restore resumes, without paying the fee again.
- A pull and re-plug elsewhere ends the call and judges the new circuit.
- Returning the cord to the tray ends the call.
- Several calls, with one on hold.

**Playtest script (you)**
1. Connect a call and let it earn a little.
2. Pull one of its plugs. The card says "On hold" and its earnings stop.
3. Click empty space. The plug snaps back and the earnings resume.
4. Pull a plug again and click the tray. The call is cut off.
5. Pull a plug and push it into a different jack. The old call is cut off, and the new connection counts as a fresh patch.
6. Let the queue fill with ringing cards (dev panel: Call now). Every card's price label stays readable, and no glow covers the card above.

**Out of scope:** emergencies, upgrades, saves, sounds.

---

### Step 3: Phone book tabs, 36 people, real cards

**Goal.** Finding anyone in the book is quick, the whole 6×6 board is in use, and every card state is readable. **Recommended effort:** high.

**Playable result.**
- The book has A–Z tabs. Tapping a letter jumps to the spread with the first surname starting with that letter. Letters with no surname are greyed out and do nothing.
- 36 people, one on every jack A1–F6, all listed in the book and all able to call.
- Cards are restyled and readable in every state, at 1280×720 and in a phone-sized Game view.
- `Tools/Switchboard/Validate Content` reports problems in `PhoneBook.json`; the same checks run as an EditMode test.

**Build**
- **Book tabs (D34, coordinated change to Sergei's book, approved by the lead 2026-10-18).**
  - Approved files: `Assets/Scripts/UI/PhoneBookView.cs`, `PhoneBookPageView.cs` and `PhoneBookContactRow.cs` if needed, a new tab component in `Assets/Scripts/UI/` if needed, `Assets/Editor/PhoneBookBuilder.cs`, and `Assets/Prefabs/UI/PhoneBook.prefab`. Nothing else of his.
  - Match his style exactly (namespace `Switchboard.UI`, uGUI `Text` like his rows, his builder pattern). Keep his public API working: `IsOpen`, `CurrentSpreadIndex`, `NextSpread`, `PreviousSpread`, `Toggle`, `SetOpen`, `ReloadContacts`.
  - Add a public `ShowLetter(char)` (or similar) and keep the tabs as a thin view over it, so it can be tested and reused.
  - Tabs sit on the open book's edge, inside his book canvas, so nothing of ours overlaps the book (both are at sorting order 10).
  - Add the tabs in the prefab through his builder's own code path, or by editing the prefab directly. **Don't run `Update Phone Book Directory`**: it points at a missing `Assets/Settings/PhoneBook.json`. Don't run anything that marks `Level.unity` dirty, and never save it.
  - Keyboard arrows and swipe flipping: only if they're small, using `Pointer.current` / `Keyboard.current`; otherwise list them as follow-ups.
- **36 people (D35).**
  - Add 24 people to `Assets/Configs/PhoneBook.json` so every jack A1–F6 is used exactly once. Keep Sergei's format, `contactsPerPage` (6) and the surname sort order. Names follow Design §6.3: 1920s–40s English names, unique surnames, no famous people. Spread surnames across the alphabet so most tabs are live.
  - Portraits: reuse `caller-01`…`caller-12`, each three times, until Mia draws more. All 36 use `type.regular` with no overrides.
  - Check that the book shows 3 spreads and the spawner uses the larger pool.
- **Cards (ours: `CallCardView`, `GameUI.prefab`).**
  - Restyle every state from Design §5.4 to match the caller panel: waiting (halo, rate and fee badge, "Calling again"), in hand (bubble with names), connected and on hold (cord ring, earnings, "On the line" / "On hold"), ended (stamps "Paid $X", "Wrong number!", "Cut off").
  - Use TextMeshPro and only glyphs in the static atlas. Keep the halo's nested canvas at sorting order 9 (Step 2b), so badges stay readable. Don't add raycast targets except the card portrait.
  - Check readability with screenshots at 1920×1080, 1280×720 and a phone-sized landscape view (e.g. 844×390).
- **`ContentValidator` (Design §6.3, adapted to D31).**
  - Content assembly code plus an editor menu item `Tools/Switchboard/Validate Content` (in `Assets/Scripts/Game/Editor/` or another folder of ours; `Assets/Editor/` is Sergei's).
  - It checks `PhoneBook.json` and the caller types: unique names and surnames, unique person ids, every socket a real jack (A–F × 1–6) used once, every portrait sprite exists, every type id exists, rate and fee ≥ 0, ranges min ≤ max, and each type has enough possible recipients.
  - Reuse the error collection that `PeopleFileLoader` already does instead of duplicating it.

**Tests**
- `ShowLetter`: jumps to the right spread, ignores letters with no surname, and works on the first and last spread. If it lives in Sergei's assembly, put the test next to his `CableConnectionTests` in `Assets/Tests/Editor/` (approved as part of this change); otherwise in ours.
- The validator: the real file passes, and broken samples (duplicate socket, unknown jack, missing portrait, unknown type, duplicate surname) are each reported.
- The spawner and busy rule still behave with 36 people (one test is enough).

**Playtest script (you)**
1. Play from `Game.unity`. Open the book: there are A–Z tabs, and letters with no names are greyed out.
2. Take a card, find both people with the tabs in under 10 s each, and connect them.
3. Flip through all three spreads: 36 names, sorted by surname, each with its jack.
4. Callers come from all over the board, not only the first 12 jacks.
5. Look at cards in every state (ringing, in hand, connected, on hold, each stamp). Everything is readable. Switch the Game view to 1280×720 and to a phone-sized landscape size and check again.
6. Run `Tools/Switchboard/Validate Content`: it reports no problems.

**Out of scope:** the Notepad upgrade (D23, Step 5), board sections and locked lines (Step 7), emergencies, sounds.

---

### Step 4: Emergencies and fines (outline)
- **Decide first (raised in Step 3):** the 36 people fill every jack A1–F6 (D35), so the emergency services have no free line. Options: they replace three people on existing jacks, or they get their own lines (for example a separate small panel, or a 7th row). The validator also needs to allow single-word names such as "Police" and keep the surname rules for people only.
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
  - Assets: Extra Cord, Earnings Multiplier, Notepad (D23: while owned, tapping a book entry notes its jack on the call in hand), and Mystery (`isGoal`, with `MysteryPlaceholderEffect`).
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
  - Buy the Notepad, then tap names in the book and see their jacks on the call in hand.
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
- **From Step 3:** replace the placeholder `card-take-click.wav` (set on `UiClickSound` in `GameUI.prefab`) with a real sound, and fill `CallCardView.OnCharacterShown` with the callers' talking sound (lead request).
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
