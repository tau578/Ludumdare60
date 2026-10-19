# Switchboard Operator: Implementation plan

This plan builds the game described in Design.md in small steps, each ending in something you can play. One agent builds each step through the Unity MCP server. Then we verify the result, you playtest it, and we move on.

Design: [Design.md](Design.md) · Progress: [Progress.md](Progress.md)

## 1. How a step runs

1. **Prep (orchestrator: the main Claude session).**
   - Pull teammates' changes.
   - Check that Unity is connected, idle, and has a clean console.
   - Make sure the step's decisions (Design §10) are settled, and check the open items (§8) the step could pick up.
   - Refine the step's brief with anything learned so far.
2. **Build (one implementer agent, run at high effort).** The agent gets the brief, works only through Unity MCP, and returns a report (§7).
3. **Verify (orchestrator).**
   - Re-run all EditMode tests and read the console.
   - Run a Play Mode smoke test and take a screenshot.
   - Review the diff for scope creep: files changed that the brief didn't name.
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
- [ ] The decisions the step needs are settled (Design §10).

## 3. Rules for implementing agents
1. **Read first:** Design.md; sections §3–§7 of this plan and your own step; Progress.md.
2. **Scope.** Build only your step. Put anything else you notice under "Follow-ups" in your report.
3. **Any file may change** (D48, Design §3), including the cord, board, book, shop and menu code. Change what the step needs, prefer extending existing components over duplicating them, and list every changed file in your report. Don't rewrite unrelated code.
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
| Tests on Assembly-CSharp types (3) | `Assets/Tests/Editor/` (the cable tests' asmdef) can't see Assembly-CSharp types such as `PhoneBookView`. Put such tests in our `Assets/Tests/Game/Editor/`, which has no asmdef and compiles into Assembly-CSharp-Editor. |
| Dev panel (2a) | F1 or the "Dev" button in the top-right corner: + $10, Call now, Emergency now (4), Speed ×5/×1, Show/Hide times, Clear calls. Editor and development builds only. Its IMGUI clicks also reach the cord input as "empty space" clicks. |
| Format-only re-saves (4) | Test runs and Play Mode can re-save `ProjectSettings/TimeManager.asset` (as well as `SampleScene.unity`) with only format changes. Discard them with `git checkout -- <file>` and mention it in the report. |
| Layout in prefab editing (5a) | Unity's layout groups don't run while a prefab is edited from code, so an editor menu that resizes children must set their `sizeDelta` directly (as `Update Upgrade Shop Buttons` does). |
| Disabled buttons (5a) | Setting `Button.interactable = false` applies the button's grey, half-transparent disabled tint and changes the shop's look. To make a button inert, ignore the click in our view and dim it ourselves. |
| Book row clicks (5b) | Row taps only reach `PhoneBookContactRow` while `ContactTapsEnabled` is on (Notepad owned); otherwise the open book's button gets the click and closes. Simulate them with EventSystem raycasts inside a `Canvas.willRenderCanvases` handler. |
| Buying in Play Mode (5a) | Connect a call through `OperatorDesk.OnCircuitClosed` to check fees and pay without plugging cords; the dev panel's "+ $100" and "Reset upgrades" call `GameSession` methods directly. |
| Art merges (6) | Re-exported images can keep a slice bigger than the new texture, and Unity then drops the sprite with no console message. Compare PNG sizes (`sips -g pixelWidth -g pixelHeight`) with the `.meta` rects, or load the path with `AssetDatabase.LoadAssetAtPath<Sprite>` and check it isn't null. |
| Dynamic TMP fonts (merge) | Test and Play runs fill glyphs into Sergei's dynamic font assets (`Boecklins Universe SDF.asset`, `Della Respira SDF.asset`): thousands of changed lines. Discard them with `git checkout -- <file>` like `TimeManager.asset`, and mention it. |
| Simulated hover vs. the real mouse (merge) | While Unity has focus, `CableInputController` reads the real mouse every frame and overrides a simulated hover. Disable the controller for the one captured frame. |
| Deleting files (7) | Delete through MCP (`delete_script`, `manage_asset` `delete`) or a plain `rm` followed by `refresh_unity`. Never use `git rm`: it stages the deletion. |
| Renaming an asset (8) | Rename or move it with `manage_asset` (Unity's `AssetDatabase.MoveAsset`), never in the file system: Unity keeps the GUID, so every prefab and asset that references it still finds it with no re-wiring (`PhoneBook.json` → `Book.json`). Git shows a delete plus a new file until both are staged. |
| Missing JSON fields (5c) | `JsonUtility.FromJson` keeps field initializers for fields the file leaves out, also inside nested objects in arrays. Use a marker initializer (NaN, or a named `Unset` constant) to tell "missing" from 0. |

## 5. Definition of done (every step)
- [ ] The project compiles. The console has no errors and no new warnings from our code.
- [ ] All EditMode tests pass: the team's existing tests plus new tests for every new rule.
- [ ] A 10-second Play Mode smoke test in `Final.unity` shows no errors or exceptions. The screenshot is saved to `Temp/Captures/step-NN.png`.
- [ ] Every changed scene and prefab is saved, with no missing references.
- [ ] `Final.unity` plays directly and from `MainMenu` (Start), and nothing in `Final` changed beyond what the brief needs.
- [ ] Every changed file is listed in the report; nothing unrelated was rewritten.
- [ ] The report is in `Docs/Progress.md`, including any deviations from Design.md.

## 6. Launch prompt (orchestrator → agent)
Launch the agent with the Agent tool (`subagent_type: general-purpose`) at **`effort: "high"`**, unless the lead asks otherwise.
```
You are implementing Step N of the Switchboard Operator game in this Unity project, using the Unity MCP server.
Read Docs/Design.md, Docs/Plan.md (sections 3–7 and "Step N") and Docs/Progress.md before changing anything.
Implement only Step N, following the rules in Plan §3 and the definition of done in Plan §5.
When you finish, append your report (Plan §7 format) to Docs/Progress.md and return it as your final message.
Notes from the orchestrator: <confirmed decisions, teammates' recent merges, playtest feedback to address>
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

## 8. Open items

Since D48 (lead, 2026-10-18) there is no ownership split, so nothing here needs anyone's approval. These are known issues and to-dos a step can pick up; the step that fixes one removes it.

| Item | Notes | Planned |
|---|---|---|
| Touch input in the cord code | Use `Pointer.current` instead of `Mouse.current` in `CableInputController.Update`, and hide the hand cursor on touch (D28). | Step 9 |
| A held cord drops on button clicks | Any click off a jack counts as "empty space" and returns the held cord. A call-card click already keeps it (`2fc3810`). The lead prefers that buttons keep it too. Book rows no longer take clicks since Step 7 (D54). | any step; Step 12 at the latest |
| `CableInputController` references `Switchboard.Game.CallCardView` | The UI code depends on the Game layer. A small interface or marker component on the card would keep the layers apart. | cleanup, any step |
| Stale paths in editor menus | `LevelBackgroundBuilder` and `GamePresentationBuilder` still name `Assets/Scenes/Level.unity` (now `Final.unity`, same GUID); `GamePresentationBuilder` opens it additively, so check what it does before changing the path. `PhoneBookBuilder.CreatePhoneBook`'s message still says "Open Level". (`PhoneBookBuilder`'s book path was fixed in Step 8.) | cleanup, any step |
| A–Z tabs are hidden | `OpenBook/LetterTabs` is inactive in `PhoneBook.prefab` (lead, 2026-10-18); the code, objects and tests stay. `Tools/Switchboard/Update Phone Book Tabs` rebuilds them active, so untick them again after running it. | |
| Loose scene objects | `SwitchboardCanvas` and `EventSystem` are loose in `Final.unity`; making them prefabs would keep scene merges small. | optional |
| Sound effects | Ring, alarm, coins, paid, fine, hang-up: find them or generate them. | before Step 11 |
| Art imports | Mia has no Unity, so her re-exports keep the old slices: `caller-04`/`05` (5b) and `caller-06`/`07`/`08` (6) came back at 190×190 with 191×191 slices, and Unity dropped them silently (all fixed to 190×190 in the `.meta`). After every art merge, compare each changed PNG's size with its `.meta` sprite rect. `Assets/Sprites/board/caller.png` (229×190, slice 190×191) has no sprite for the same reason; nothing uses it yet. | check after art merges |
| Closed caller-slot covers (6) | Closed slots are hidden since Sergei's `041fb90` (Step 6's dark discs are gone). A cover-plate drawing from Mia could show them as closed. | when art exists |
| Unused random helpers (7) | `IRandom`, `SeededRandom`, `RandomExtensions` and `FloatRange` in `Domain/Common` have no users since the spawner went. Delete them, or keep `IRandom` if a later step needs randomness. | cleanup, any step |
| "New order!" stamp over the emergency badge (7) | While a call rings, the stamp hides half the EMERGENCY badge and most of the countdown ring. This predates Step 7. | Step 11 or 12 |
| `PhoneBookContactRow.OnPointerDown` (8) | Rows have no raycast target since Step 8 dropped the tap image, so it can never be called. Harmless; remove it in a cleanup. | cleanup, any step |
| `SlowGuidePulse` material in EditMode tests | The guide's hidden book-glow material is freed only in `OnDestroy`, which EditMode tests don't run, so each test run leaves 2 materials in editor memory. Free it in `OnDisable` too, or in the test teardown. | cleanup, any step |

Names the code looks up: keep `CallerQueuePanel`, `UpgradeShopPanel` and `UpgradeShopPanel/HeaderSlot`, or update `GameInstaller` with the rename. `Book.json` keeps its format and `contactsPerPage`; the book sorts itself (D53), so the file's order doesn't matter.

## 9. Steps

| # | Step | Playable result |
|---|---|---|
| 1 | First call: take it, patch it, get paid | One call at a time, end to end, with live money |
| 1b | Connection fee, Sergei's book, layout | Fee on connect; the real phone book shows our people; no overlaps |
| 2a | Queue, callbacks, dev panel | Several callers, swapping, callbacks, a dev panel |
| 2b | Hold and resume | Pulled plugs put calls on hold; restore resumes |
| 3 | Phone book tabs, 36 people, real cards | Find anyone fast with A–Z tabs; the whole board in use; readable cards in every state |
| 4 | Emergencies and fines | Timed emergency calls and fair fines |
| 5a | Economy file, shop, income, secret goal | `Economy.json` drives Sergei's shop; income upgrades; the secret fills as a savings bar |
| 5b | Extra wires and the Notepad | Buy cords and the Notepad (changes to the cord input and the book) |
| 5c | Calls and pacing into `Economy.json` | The designer tunes the whole economy in one file |
| 6 | Board growth (built as "7a") | Start on 3×3; buy 4×4, 5×5, 6×6 |
| 7 | Hand-written calls | The designer's `Calls.json` and call queues replace random calls; 2 slots at the start |
| 8 | The new phone book | `Book.json`: name, profession, address, jack; everyone listed from the start |
| 9 | Saves, Boot scene, first Web build | Plays in a browser, and progress survives a reload |
| 10 | Tutorial | A guided first call |
| 11 | Sound and feel | Rings, alarms, coins and animations |
| 12 | Mobile and Web polish | Good on phones and desktop browsers |
| 13 | Balance | A tuned time to reach the mystery upgrade |

Steps 1–8 are built. Step 9 comes next (hosting: itch.io, D26). Steps 9–13 are outlines; the orchestrator turns each one into a full brief before launching it.

> **Renumbered (lead, 2026-10-18)** so the numbers follow the build order. Old → new: 7a → 6 (board growth, done), 7c → 7, 7d → 8, 6 → 9 (saves, Web build), 7b → 10 (tutorial), 8 → 11, 9 → 12, 10 → 13. The briefs of done steps, commit messages and older reports keep the numbers they were written with.

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

### Step 4: Emergencies and fines

**Goal.** Timed emergency calls to Police, Fire Brigade and Doctor, with fair fines, and an idle guard so walking away never drains your money. **Recommended effort:** high.

**Playable result.**
- About 1 call in 6 is an emergency (never more than one at a time). Its card looks different: an "EMERGENCY" badge, a red accent and a countdown ring around the portrait that empties over 30 s.
- Taking it shows, for example, "Ada Moss" / "wants Police". The services are at the top of page 1 in the book.
- Connected correctly in time: it pays a bigger fee (20–40¢) and 5–10¢/s for a short 10–20 s call.
- Missed (the ring runs out): the card stamps "Missed -$2.00" and $2 is taken. A wrong patch: "Wrong number!", $2 is taken, and the caller rings again later. Cutting a connected emergency off: "Cut off" and $1 is taken. Every fine pops up in red near the money counter.
- Money never goes below $0.00. After 60 s with no input, no new emergencies ring.

**Decisions for this step** (lead, 2026-10-18; recorded as D36 and D37 in Design §10):
- **D36:** Police, Fire Brigade and Doctor replace 3 people on their jacks (33 people + 3 services). They are listed first in `PhoneBook.json`, and the people follow, sorted by surname.
- **D37 (Step 4 numbers):** a response time of 30 s; fines of $2 for a miss or a wrong patch (200¢) and $1 for an early disconnect (100¢); emergency pay of fee 20–40¢, 5–10¢/s and 10–20 s duration; about 1 call in 6 is an emergency (type spawn weights Regular 5, Emergency 1).
- **Defaults confirmed** (Design §1, §5, D09, D10, D16):
  - The services never place calls, and regular calls never go to a service.
  - Any of the 33 people can place an emergency call.
  - A wrong patch gives the fine and a callback.
  - A miss or an early disconnect gives no callback (D10 flags off).
  - Fines are not multiplied by the earnings multiplier.
  - The idle timeout is 60 s.

**Build**
1. **People file (`Assets/Configs/PhoneBook.json`, shared, D31/D36).**
   - Replace 3 people with the services. The services must be on 3 different rows. Remove people whose surname letter is shared with another person (for example one of Kemble/Kettering), so the surnames stay spread across the alphabet. Remove those people from the file entirely; their jacks go to the services.
   - The services go **first**, in this order: Police, Fire Brigade, Doctor. A service entry looks like this, with no portrait, type, rate or fee:
     `{ "name": "Police", "socket": "C4", "tags": ["emergency-service"] }`
   - The people keep their exact format. Don't add a `tags` field to them; a missing `tags` means none.
   - Keep `contactsPerPage` 6 and the surname order for the people. Sergei's book reads only `name` and `socket` and ignores the other fields. Single-word names are fine for his parser.
   - The designer edits this file by hand. Keep one entry per line, as now.
2. **Content: loader and catalog.**
   - `PeopleFileContact` gets `tags` (a string array). Domain `IPerson.Tags` is filled from it.
   - Services go into the directory (so their jacks connect) but get **no** caller record, so they never ring.
   - Callers for "anyone can call" types: for a type with `everyoneCalls` set, Content adds one caller per untagged person for that type (weight 1, no overrides). People keep their own JSON `type` for their ordinary calls.
3. **Caller types (Design §6.1 [4]).**
   - Add these to `ICallerType` and `CallerTypeDefinition`:
     - `responseTimeSeconds` (0 = none)
     - `fineMissedCents`
     - `fineEarlyDisconnectCents`
     - `callbackAfterMiss`
     - `callbackAfterDisconnect`
     - `recipientTag` (empty = any untagged person)
     - `maxSimultaneous` (0 = no limit)
     - `spawnWeight` (temporary until Step 7's `weightByProgress`)
     - `everyoneCalls`
   - Add a presentation-only **card style** (badge text, accent colour). It stays out of the Domain interface; the Game layer looks it up by type id through `GameConfig`.
   - `Regular.asset`: `spawnWeight` 5; everything else in this list stays 0, false or empty.
   - New `Assets/Content/CallerTypes/Emergency.asset`, registered in `GameConfig`:
     - id `type.emergency`, display name "Emergency"
     - fee 20–40¢, rate 5–10¢/s, duration 10–20 s, callback delay 20–40 s
     - response time 30 s, fines 200 / 100
     - both callback flags off
     - `recipientTag` `emergency-service`, `maxSimultaneous` 1, `spawnWeight` 1, `everyoneCalls` on
     - badge "EMERGENCY", accent red
4. **Spawning (Design §5.2 "Which type").**
   - Pick the type first: a weighted pick among the types that are allowed right now. A type is allowed when:
     - it is under its `maxSimultaneous`, counting its live calls plus pending callbacks of that type
     - it isn't blocked by the idle guard
     - it has at least one free caller and one free recipient
   - Then pick a caller of that type, then a recipient by the type's `recipientTag`. The busy rule (D18) applies to everyone, services included.
   - Callbacks: a due callback also waits while its type is at `maxSimultaneous` (live calls), or while the idle guard blocks its type.
   - Use no LINQ and allocate nothing per tick.
5. **Response timer and Missed (D09, Design §5.1).**
   - `Call` gets the response time left. It counts down in Incoming, Taken and Connecting, from `CallService.Tick`.
   - It stops for good on Connected and doesn't restart after hold.
   - When it reaches 0 the call ends as `Missed`. A callback starts with a fresh full timer (it is a new call).
   - When the missed call is in hand, nothing is in hand afterwards and the bubble hides. A half-patched cord stays where it is, because it's the player's; closing it afterwards is refused, as usual. Make sure `OperatorDesk`'s half-patch tracking stays consistent.
   - `CallbackScheduler` honours `callbackAfterMiss` and `callbackAfterDisconnect` (D10).
6. **Penalties (Design §5.5).**
   - Add `PenaltyService` in Domain, which listens for `CallEnded`:
     - `Missed` and `WrongConnection` use `fineMissedCents`.
     - `DisconnectedEarly` uses `fineEarlyDisconnectCents`.
     - `Finished` and `Cleared` never fine.
   - It calls `Wallet.ApplyFine` (floored at 0) and raises `FineApplied(call, reason, requested, taken)`, a readonly struct.
   - Build it in `GameSession`.
7. **Idle guard (D16).**
   - Add `IdleGuard` in Domain, ticked from `GameSession.Tick` before the spawner. `ReportInput()` resets it. It is idle after `idleTimeoutSeconds`, a new `PacingProfile` field (default 60; set it in `Default.asset`).
   - While idle, types that carry a fine (either fine > 0) neither spawn nor ring as callbacks. Regular calls continue.
   - The Game layer reports input: any pointer press or pointer move, or any key press, through the new Input System (`Pointer.current`, `Keyboard.current`), with no per-frame allocations. Put it in `GameLoopRunner` or a small component of ours. Don't touch Sergei's input code.
8. **Cards and feedback (ours: `CallCardView`, `CallQueueView`, `GameUI.prefab`).**
   - **Emergency card:** the type's badge text ("EMERGENCY") and accent on the card, plus a countdown ring around the portrait. Use a radial-filled `Image`, polled in `LateUpdate` with a change check; it gets more urgent in the last 10 s, through a colour shift or a pulse.
     - The ring shows only for types with a response time, and hides once connected.
     - Not colour alone (Design §5.4): the badge text and the ring carry the meaning.
     - The cord-colour ring for connected calls still works. Make sure the two rings don't fight.
   - **Stamps:** "Missed -$2.00" (the amount actually taken; just "Missed" if nothing could be taken), in a dark red ink. "Wrong number!" and "Cut off" stay as they are; their fine shows as the popup.
   - **Fine popup:** on `FineApplied` with taken > 0, show "-$2.00" in red near the money counter. Reuse `FloatingTextView` the way the "+10¢" fee popup does. Check that no popups overlap when a fee and a fine land close together.
   - **Glyphs:** only those in the static LiberationSans SDF atlas. Check whether "−" (U+2212) is in it; if not, use "-".
   - **Halo and raycasts:** keep the halo's sorting order 9 and add no new raycast targets.
9. **Validator (`ContentValidator`, Design §6.3, D36).** Messages must be plain enough for a non-programmer, and each should name the contact and the fix. For example: `Contact 5 "Fire Brigade": emergency services must be listed at the top of the file, before the people.`
   - Services (entries tagged `emergency-service`) must form one block at the top of the file.
   - Services may have single-word names and need no portrait or type.
   - The surname rules (unique, sorted, period names) apply to people only.
   - Every type with a `recipientTag` has at least one entry with that tag.
   - Response time, fines, `maxSimultaneous` and `spawnWeight` are all ≥ 0.
   - At least one type has `spawnWeight` > 0.
   - Names stay unique across everyone, and the sockets stay valid and used once.
10. **Dev panel:**
    - An "Emergency now" button.
    - "Show times" also shows the response seconds left.
    - "Clear calls" must not fine anyone (it uses `Cleared`).
11. **Sergei's book: unchanged (D36).** Don't edit `PhoneBookView`. His `ShowLetter` takes the first name whose last word starts with the letter, so with the services first, "P" would jump to Police on spread 1. The tabs are hidden (lead, 2026-10-18), so leave it, and list it as a follow-up for when they're re-shown. The `OpenBook/LetterTabs` object stays inactive. Don't run his builder menus.

**Tests** (`Switchboard.Domain.Tests`, plus Content tests where they already live)
- The response timer:
  - counts down in Incoming, Taken and Connecting
  - stops on a correct connection and stays stopped through hold and resume
  - reaching 0 ends the call as Missed
- Missed clearing the call in hand: nothing in hand afterwards, the desk is consistent, and a half-patched cord closing afterwards is refused.
- Fines for each reason: Missed and WrongConnection 200, DisconnectedEarly 100, Finished and Cleared none, and a regular call never fines.
- The floor at zero: with a balance of 150¢, a 200¢ fine takes 150, and `FineApplied` reports requested 200 and taken 150.
- A wrong connection on an emergency gives both a fine and a callback. A miss gives no callback with the flag off, and one with the flag on.
- Type pick:
  - the weights are respected over many spawns, roughly 1 in 6
  - `maxSimultaneous` 1 is never exceeded, counting pending callbacks
  - emergency recipients are always services
  - regular calls never go to a service
  - services never call
- The idle guard:
  - after 60 s without input, no emergencies spawn or ring as callbacks, but regular calls still do
  - `ReportInput` re-enables them
  - two idle game minutes with an emergency already waiting cost at most one missed fine
- The validator: the real file passes. Each of these is reported: a service below the people, an unsorted person, a type whose `recipientTag` has no entries, and a negative fine.
- Update `PeopleFileTests` and `FullBoardTests` for 33 people and 3 services.

**Done when** §5 is met and every test above passes. Take screenshots of:
- an emergency card ringing with its ring part-empty (`step-04.png`)
- the in-hand bubble for an emergency
- the "Missed" stamp and a fine popup
- the book on spread 1 showing the 3 services at the top

**Playtest script (you)**
1. Play from `Game.unity`. Within a minute or two (or with dev panel F1, "Emergency now"), a card rings with an "EMERGENCY" badge and a ring around the portrait that slowly empties.
2. Open the book: Police, Fire Brigade and Doctor are at the top of page 1, and the people follow in surname order.
3. Take the emergency and connect it in time. The ring disappears, the fee pops up (20–40¢), and it pays for a short call.
4. Let the next emergency run out. The card stamps "Missed -$2.00", and "-$2.00" pops up in red by the money counter.
5. Patch an emergency wrong on purpose. You see "Wrong number!" and a $2 fine, and the same caller rings again later.
6. Connect an emergency, pull a plug and click the tray. You see "Cut off" and a $1 fine.
7. With less than $2, get fined. Money stops at $0.00 and never goes negative.
8. Leave the game alone for 2 minutes (don't touch the mouse). At most one emergency gets missed; no new ones ring while you're away, and regular cards still pile up.
9. Only one emergency is ever on the board at a time.
10. Run `Tools/Switchboard/Validate Content`: no problems. Break the file on purpose, for example by moving "Doctor" below a person, run it again, and read the message; then undo.

**Out of scope:**
- the alarm sound and ringing loop (Step 8)
- type weights by progress and emergencies arriving later in a session (Step 7)
- upgrades (Step 5)
- re-showing the A–Z tabs
- the 5-versus-6 caller window layout (Sergei)

---

### Scene merge: one `Final` scene built from prefabs (D29, D38)

**Goal.** One playable scene, `Assets/Scenes/Final.unity`, made of prefab instances, so the team can work in parallel without scene merge conflicts. MainMenu → Start lands in it. **Recommended effort:** high.

**Background.**
- Sergei's first attempt (`1c20569`) pasted standalone, prefab-less copies of our `Game` and `GameUI` into `Level.unity`. Later `GameUI.prefab` changes (Step 4) therefore never reached the scene, and MainMenu still loaded `Game.unity`. It was reverted in `cdc5876`.
- The lead decided (2026-10-18) on one scene built from prefabs, named `Final`: "the scene is shared, prefabs are owned".

**Approved changes** (lead, 2026-10-18):
- Rename `Assets/Scenes/Level.unity` to `Final.unity`, edit it, and save it.
- Edit `ProjectSettings/EditorBuildSettings.asset`.
- Delete `Assets/Scenes/Game.unity`.
- Nothing else of Sergei's: not his prefabs or scripts, and not `MainMenu.unity`. Never save `MainMenu.unity`.

**Build**
1. **Rename inside Unity** (`AssetDatabase.MoveAsset`, or `manage_asset` move) so the GUID is kept: `Level.unity` → `Final.unity`.
2. **`Game.prefab`.** Turn the `Game` object in `Game.unity` (`GameInstaller`, `CordBridge`, `GameLoopRunner`, the dev panel if it's there) into `Assets/Prefabs/Game/Game.prefab`, keeping its serialized references (the `GameConfig`, its own components).
3. **`GameUI` overrides.**
   - First check the `GameUI` instance in `Game.unity` for overrides that matter, such as layout values that no script sets. Apply those to `GameUI.prefab`, so the prefab alone looks the same as today.
   - Report what was applied, and drop the noise.
4. **`GameInstaller` in one scene.**
   - Drop the additive loading and the `levelScene` field.
   - Find the views (`MoneyCounterView`, `CallQueueView`, and anything else it needs) and the board objects in its own scene (`gameObject.scene`) by component type, through `LevelScene`. Log a clear error if one is missing or appears twice; Sergei's reverted version shows the idea.
   - No serialized references between `Game.prefab` and `GameUI.prefab`, or between them and Sergei's prefabs, because scene-level override links are exactly what breaks.
   - Keep the name lookups `CallerQueuePanel` and `UpgradeShopPanel/HeaderSlot`.
5. **`Final.unity`:**
   - Add a `Game.prefab` instance and a `GameUI.prefab` instance at the root, with no overrides beyond the root's position and name. Move nothing of Sergei's and delete nothing of Sergei's.
   - Exactly one camera and one EventSystem, which `Level` already provides.
   - Save it.
6. **Build list:** `MainMenu` (0), `Final` (1). `Game.unity` is removed from the list and deleted; `SampleScene` stays off the list.
7. **Loose objects report.** List every root object in `Final` that is not a prefab instance, with a one-line suggestion each. These are Sergei's to turn into prefabs; don't change them.
8. **Stale references.** Search our code, tests and editor tools for "Level" or "Game.unity" scene-name references and update them. Leave Sergei's code alone, and report anything of his that names a scene.

**Checks**
- EditMode tests all pass.
- Play `Final.unity` directly: there are no errors; the board, cords, book, shop panel and money counter are there; calls ring; and an emergency shows its ring, badge and fine popup (Step 4 visuals present).
- Play from `MainMenu.unity`: trigger its Start (the same method the button calls, or `SceneManager.LoadScene(1)` from inside Play Mode) and confirm `Final` loads with no errors. Don't save `MainMenu.unity`.
- Screenshots in `Temp/Captures/`:
  - `merge-final.png`: an emergency ringing
  - `merge-final-book.png`: the book open
  - `merge-from-menu.png`: after Start from the menu
- Editor left clean: not in Play Mode, `Final.unity` saved as the active scene, console clean.

**Playtest script (you)**
1. Open `MainMenu.unity`, press Play, then Start. You land on the board with calls ringing, and there are no errors in the console.
2. Play through an emergency: the ring, the "EMERGENCY" badge, the fine popup.
3. Open `Final.unity` directly and press Play: the same game.
4. In the Hierarchy of `Final`, our objects are two blue prefab entries, `Game` and `GameUI`.

**Out of scope:** turning Sergei's loose objects into prefabs (list them only), Step 5 work, touch input.

---

### Step 5a: Economy file, shop, income upgrades and the secret goal

**Goal.** The designer's `Economy.json` drives a working shop on Sergei's upgrade panel. Income upgrades raise earnings, and the secret upgrade's button fills as a savings bar; buying it shows "To be continued". **Recommended effort:** high.

**Playable result.**
- The right-hand panel's 4 buttons now show real offers, with a name, a one-line description and a price.
  - The top button sells "Better rates" (×1.5, $8.00).
  - The next 2 buttons keep Sergei's "+" placeholder look and can't be clicked. Wires and Notepad fill them in 5b.
  - The bottom button is the secret ("???", $250.00). It fills from the bottom as money is saved.
- An offer you can't afford is dimmed and can't be clicked.
- Buying deducts the price; "-$8.00" pops up by the money counter in a neutral ink (not the fine red). The button then shows the next entry in the chain (×2.0 for $20, then ×3.0 for $50). Once the chain is bought out, the button goes back to the placeholder look.
- With ×1.5 bought, a call's fee popup and its earnings are 1.5 times bigger, including on calls already connected.
- With $250 saved, the secret button is full and stands out. Buying it shows a "To be continued..." panel with a "Keep playing" button, and the secret button then reads "Owned".

**Decisions for this step** (lead, 2026-10-18; Design D14, D39–D44, Design 5.9, 5.10 and 6.4):
- **D39 and D40:** one hand-edited file, `Assets/Configs/Economy.json`. 5a builds only its `upgrades` section and the types `extra_income` and `secret`. Values set the result, chains follow file order, there's no skipping, and costs are in cents.
- **D41:** a pinned secret button at the bottom, with 3 rotating buttons above it driven by `ShopShelf`.
- **D43:** the placeholder prices listed below.
- **D44:** the button texts and the savings fill live inside Sergei's button prefab, as a coordinated change. The files are listed below.
- The old plan of `UpgradeDefinition`, levels, `StatModifier` and `UpgradeEffect` is dropped. Don't build it.
- D15 stays open. The secret's only effect is the "To be continued" panel.

**Approved coordinated changes** (Sergei's files, D44). Only these, and only for the button texts and the fill:
- `Assets/Scripts/UI/UpgradeSlotView.cs`: new, in his folder and his style (sealed, `[SerializeField] private`, no comments beyond the why).
- `Assets/Editor/UpgradeShopPanelBuilder.cs`:
  - Its button-creation code builds the new parts.
  - A new menu `Tools/Switchboard/Update Upgrade Shop Buttons` upgrades the existing prefabs in place, like Step 3's `Update Phone Book Tabs`.
- `Assets/Prefabs/UI/UpgradePlaceholderButton.prefab`. Keep its name; it is nested 4 times in the panel.
- `Assets/Prefabs/UI/UpgradeShopPanel.prefab`.
- Keep `HeaderSlot` and its position exactly as they are; our money counter lives there.
- Don't touch any other file of his, `Final.unity`'s loose objects, or `MainMenu.unity`.

**Build**
1. **`Assets/Configs/Economy.json`** (new; the designer's file). Use one entry per line, readable, in this order:
   ```json
   {
     "upgrades": [
       { "id": "income_1", "type": "extra_income", "name": "Better rates", "description": "Earn x1.5", "costCents": 800, "incomeMultiplier": 1.5 },
       { "id": "income_2", "type": "extra_income", "name": "Good rates", "description": "Earn x2", "costCents": 2000, "incomeMultiplier": 2.0 },
       { "id": "income_3", "type": "extra_income", "name": "Top rates", "description": "Earn x3", "costCents": 5000, "incomeMultiplier": 3.0 },
       { "id": "secret", "type": "secret", "name": "???", "description": "Something big", "costCents": 25000 }
     ]
   }
   ```
   Keep the names short enough for the button. Check them on screen and shorten if needed, but keep the ids. Reference the file from `GameConfig` through a new `economyFile` `TextAsset`, as with `peopleFile`.
2. **`Docs/EconomyFile.md`** (new): a short guide for the designer, in plain English.
   - Cover the sections, the common fields, each type's fields and whether it is built yet, the chain rule, "values set", and cents.
   - Explain that ids are never renamed once released.
   - Show how to run `Tools/Switchboard/Validate Content`, with an example message.
   - Mark `extra_wires`/`notepad` (5b), `extra_clients` (7), and `calls`/`pacing` (5c) as coming.
3. **Content: loader** (`Switchboard.Content`).
   - `EconomyFileData` / `UpgradeEntry` are `[Serializable]` classes for `JsonUtility`. `costCents` is a `long`, and `incomeMultiplier` a `float`.
   - `EconomyFileLoader` turns them into Domain `IUpgrade` records and collects errors the way `PeopleFileLoader` does. An entry with an error is left out, and the game still starts.
   - `IGameContent.Upgrades` (`IReadOnlyList<IUpgrade>`) is filled by `GameContent`. Update the test fakes.
4. **Domain** (`Assets/Scripts/Domain/Upgrades/`, no `UnityEngine`):
   - `UpgradeKind` enum with explicit values: `ExtraIncome = 1, ExtraWires = 2, Notepad = 3, ExtraClients = 4, Secret = 5`. Only `ExtraIncome` and `Secret` are loaded in 5a. The others are reserved, and the loader reports them as "not built yet".
   - `IUpgrade`: `Id`, `Kind`, `Name`, `Description`, `CostCents`, `IncomeMultiplier`.
   - `Wallet.TrySpend(long cents)`: returns false and changes nothing if cents ≤ 0 or the balance is short. It never touches the fraction.
   - `UpgradeService`:
     - Holds the bought ids, `IsOwned(id)`, and the chains (entries grouped by kind, in file order).
     - `NextInChain(kind)`; the secret is its own one-entry chain.
     - `CanBuy(id)` returns a reason enum (`Ok`, `Unknown`, `Owned`, `NotNext`, `TooExpensive`). `TryBuy(id)` spends and raises `UpgradeBought` (a readonly struct).
     - A `Restore(ids)` method for Step 6 is **not** in scope.
   - `StatSheet`: `IncomeMultiplier` is the `incomeMultiplier` of the last bought `extra_income` entry, or 1. It is recomputed from scratch on `UpgradeBought`.
   - `EarningsService` reads `() => stats.IncomeMultiplier`. Fines stay unmultiplied.
   - `ShopShelf` (Design 5.9, D41):
     - 3 slots, each holding a chain or empty, filled in order of each kind's first appearance in the file. The secret is never on the shelf.
     - When a slot's chain is bought out, that slot (only that one) takes the next chain not on the shelf, or goes empty. The other slots never move.
     - It raises `Changed`.
   - `GoalProgress`:
     - `Goal`: the single secret entry.
     - `Progress = clamp01(balance ÷ cost)`, polled.
     - `GoalAffordable` is raised the first time progress reaches 1, checked in `Tick`.
     - `GoalPurchased` is raised on buying the secret. Afterwards `Progress` reads 1.
   - Build all of these in `GameSession` and add them to the tick order where needed, keeping Design 4.4's order (goal check last). No allocations or LINQ per tick.
5. **Sergei's button: `UpgradeSlotView`** (coordinated, his folder).
   - Inside each `UpgradeButton_NN`:
     - a title label, a description label and a price label (TextMeshPro with LiberationSans SDF, like our money plaque; `raycastTarget` off)
     - a vertical fill `Image` behind the content, hidden unless set (`raycastTarget` off)
     - the existing "+" `PlaceholderIcon`, shown only in the empty state
   - Public API, small and in his naming style:
     - `Show(title, description, price, bool affordable)`
     - `ShowEmpty()`
     - `SetFill(float)` (0 hides the fill)
     - `SetHighlight(bool)`
     - `event Action Clicked`, wired to his `Button`
   - The button stays a raycast target. Nothing new may block it, and nothing outside the button's rect becomes a raycast target.
   - Layout:
     - Keep 4 buttons inside the 236×860 panel, below the untouched `HeaderSlot`.
     - The buttons may grow up to the plaque's width (204) and be re-spaced, as long as everything fits the panel.
     - Text must be readable at a 1280×720 Game view; check with a screenshot.
   - Colours: match his panel (greens and greys). The price may use the money counter's gold.
6. **Our `ShopView`** (`Assets/Scripts/Game/Views/`, on `GameUI.prefab`):
   - `GameInstaller` finds it with `FindSingle`. `ShopView` finds the `UpgradeSlotView`s by type in the scene: expect exactly 4, ordered top to bottom. Log a clear error otherwise.
   - Binding: slots 1–3 follow `ShopShelf`, and slot 4 is the secret.
   - States:
     - **Affordable:** normal, and a click calls `UpgradeService.TryBuy`.
     - **Too expensive:** dimmed, and a click does nothing beyond Sergei's button feel. Don't add a fine-style sound.
     - **Empty:** the placeholder look.
     - **Secret:** `SetFill(progress)`, polled in `LateUpdate` with a change check. Highlighted when affordable; "Owned" after purchase.
   - Affordability changes as money changes: poll the balance with a change check, and allocate nothing per frame.
   - Prices use `MoneyFormat.Dollars`. Glyphs come only from the static LiberationSans SDF atlas, so use "..." rather than "…" unless "…" is confirmed in it.
7. **Spending popup:** on `UpgradeBought`, show "-$8.00" with `FloatingTextView` by the money counter, in a neutral ink (not the fine red), the way the fine popup is placed. Check it doesn't overlap a fee or fine popup landing at the same time.
8. **"To be continued" panel:**
   - In `GameUI.prefab`: a centred panel saying "To be continued...", the secret's name, and a "Keep playing" button that closes it. It is shown on `GoalPurchased`.
   - It blocks clicks behind it while open (a deliberate modal), and is inactive otherwise.
   - It must not overlap the open book's sorting badly. Check a screenshot with the book open, too.
   - The game keeps running underneath (D20).
9. **Validator** (`ContentValidator` or a sibling `EconomyValidator` called from the same menu and test; Design 6.4):
   - Messages are plain, name the entry, and say the fix. For example: `Upgrade 2 "income_2": incomeMultiplier 1.2 is not higher than income_1's 1.5. Values in a chain must grow.`
   - Report each of:
     - the file is missing or unreadable
     - an id that is empty, uses characters other than a–z, 0–9 and `_`, or is a duplicate
     - an unknown type (list the known ones) or a type not built yet ("`extra_wires` arrives in Step 5b")
     - a missing name
     - `costCents` ≤ 0
     - `incomeMultiplier` ≤ 1 or not growing along the chain
     - not exactly one `secret`
10. **Dev panel:** add "+$100" (next to "+$10") and "Reset upgrades" (clears the bought ids, recomputes the stats, rebuilds the shelf, and resets the goal's affordable flag). Editor and development builds only, as now.

**Tests** (`Switchboard.Domain.Tests`, plus the Content tests where they live)
- **`Wallet.TrySpend`:** succeeds and deducts when enough; refuses with no change when short or ≤ 0; keeps the fraction.
- **`UpgradeService`:**
  - buys the next entry in a chain
  - refuses out of order, already owned, unknown, and too expensive, each with nothing changed
  - raises `UpgradeBought` once per purchase
  - the secret can be bought regardless of the other chains
- **`StatSheet`:**
  - the multiplier is 1 with nothing bought
  - it equals the last bought value, which is set and not stacked (1.5 then 2.0 gives 2.0)
  - recomputing twice gives the same result
- **Earnings:**
  - a multiplier bought mid-call applies to the rest of that call
  - the fee of a call connected after the purchase is multiplied
  - fines aren't multiplied
- **`ShopShelf`:**
  - chains are placed in file order
  - a bought-out slot takes the next chain while the other slots don't move
  - slots go empty when nothing is left
  - the secret is never shelved
- **`GoalProgress`:**
  - progress is the clamped ratio
  - `GoalAffordable` fires once
  - `GoalPurchased` fires on buying the secret, and progress stays 1 afterwards
- **Loader and validator:**
  - the real `Economy.json` loads 4 entries and has no problems
  - each problem listed in Build 9 is reported
  - a broken entry is left out while the others load
- **Session:** a `GameSession` built with the fakes still ticks with no upgrades in the content: multiplier 1, an empty shelf, and no goal (the goal check is skipped and logs nothing).

**Done when** Plan §5 is met and every test above passes. Screenshots in `Temp/Captures/`:
- `step-05a.png`: the shop at the start, with income in slot 1, slots 2–3 as placeholders and the secret part-filled
- `step-05a-bought.png`: after buying ×1.5, with slot 1 showing ×2.0 and the "-$8.00" popup
- `step-05a-goal.png`: the secret affordable (full fill, highlighted)
- `step-05a-continued.png`: the "To be continued..." panel
- `step-05a-720.png`: the shop at 1280×720, readable

**Playtest script (you)**
1. Play `Final.unity` (or MainMenu → Start). The right panel shows "Better rates / Earn x1.5 / $8.00" on top, two "+" placeholders, and "??? / $250.00" at the bottom with an empty fill.
2. Below $8 the top offer is dimmed and clicking it does nothing. Earn $8 (or press F1, then "+$10"). It lights up.
3. Buy it. "-$8.00" pops up, the money drops by $8, and the button now offers "Good rates / Earn x2 / $20.00".
4. Connect a call: the fee popup and the money rise about 1.5 times faster than before. A call already connected also speeds up.
5. Buy ×2 and ×3. After ×3 the top button returns to the "+" placeholder.
6. Watch the bottom button fill as money grows (use "+$100" to speed up). At $250 it's full and highlighted.
7. Buy it. "To be continued..." appears; click "Keep playing". The game goes on, and the bottom button reads "Owned".
8. Fines still take the plain amount ($2/$1), not multiplied.
9. Open `Assets/Configs/Economy.json`, change `income_2`'s `incomeMultiplier` to 1.2 and run `Tools/Switchboard/Validate Content`. You get a plain message about the chain. Undo the change.
10. Press F1, then "Reset upgrades": the shop is back to the start.

**Out of scope:**
- extra wires and the Notepad (5b)
- moving calls, pacing and starting money into `Economy.json` (5c)
- board growth (7)
- saving purchases (6)
- shop sounds (8)
- a real secret effect (D15)
- re-showing the A–Z tabs

### Step 5b: Extra wires and the Notepad (D23, D40, D43, D44)

**Goal.** Two new upgrade types from `Economy.json`, sold on Sergei's shop buttons: `extra_wires` adds cords to his tray while the game runs, and `notepad` lets a tap on a phone book entry note that person's jack on the call in hand. Both need small coordinated changes to Sergei's cord input and book. **Recommended effort:** high.

**Playable result.**
- The shop shows "Better rates" on top, "Extra cord / 4 cords / $5.00" in the second button and "Notepad / ... / $3.00" in the third. The secret is still at the bottom.
- Buying "Extra cord" puts a 4th cord, in a new colour, in the tray at once. The tray re-spaces so every cord fits. The new cord works like the others: patch, hold, restore, and back to the tray. The button then offers 5 cords ($8), then 6 ($13), then goes back to "+".
- After buying the Notepad, its button shows the next chain, or "+". From then on, tapping a name in the open book while a call is in hand shows that person's jack in the bubble, if they are the caller or the person wanted: "Ada Moss (C3) / wants Basil Thorne (A5)". The book stays open for the second tap.
- A tap on someone not on the call in hand, or with no call in hand, does nothing, and the book stays open.
- Without the Notepad, the book behaves exactly as today: a tap on a row closes the book.

**Decisions for this step** (lead, 2026-10-18; D23, D40, D43, D44, Design 5.9):
- **Prices (D43):** 4, 5 and 6 cords for $5, $8 and $13. The Notepad costs $3. The game starts with 3 cords, set by a new `GameConfig` field `startingCords` = 3.
- **`wires` sets the total** (D40). Values in the chain must grow, be above `startingCords`, and be at most 6, which is all the tray holds.
- **New cords are added while the game runs** by copying an existing cord and its tray slot. `Final.unity` and `CableTray.prefab` don't change.
- **The three new cord colours** are presentation, so they live in our `GameConfig` (`extraCordColors`) and are passed to Sergei's `AddCable`. Use green, violet and ivory, clearly different from his red, blue and yellow and from the emergency red. Check them in a screenshot.
- **Notepad scope** (lead, 2026-10-18; **replaced after the merge by D46**: a tap notes the jack on every accepted call still waiting to connect that the person is part of, including duplicates):
  - It notes only the caller or the person wanted on the **call in hand**.
  - A tap on anyone else, or with no call in hand, does nothing: no message, no sound, and the book stays open.
  - Notes belong to the call. They stay if the player swaps to another card and back, and go when the call ends.
- **Rows are tappable only while the Notepad is owned** (lead, 2026-10-18). Without it, a row tap falls through to the open book and closes it, as today.
- **Deleting the `notepad` entry from the file removes the feature.** Nothing in code names its id.
- **Dev "Reset upgrades"** keeps cords already added (a cord can't be removed safely mid-call) and turns row taps back off. Say so in the report.

**Approved coordinated changes** (Sergei's files, D44; the lead approved exactly these on 2026-10-18):
- **`Assets/Scripts/UI/CableInputController.cs`:**
  - New `public CableController AddCable(Color color)`, plus a constant `MaxCables = 6`. It:
    - copies the last cable in its list under the same parent, named `Cable_NN`
    - copies that cable's tray slot (`StoredCableView`) under the tray, named `StoredCable_NN`
    - links them with `SetTrayCableView`, sets the colour with `SetCableColor`, and adds the cable to its list
    - re-spaces all tray slots and returns the new cable, or null when the tray is full or nothing can be copied
  - The new cord must start in the tray whatever state the copied cord is in (held, half-patched, connected or highlighted), with no hover outline copied across.
  - **Tray layout:**
    - Slots are spaced evenly along today's row (y −140), with spacing = min(270, usable width ÷ count). Pick the usable width from the green felt, about 830 px.
    - The cord preview scales down with the spacing, and the hit area is narrowed so neighbours don't overlap.
    - **With 3 cords the positions and sizes must equal today's** (−270, 0, 270; 252×94.5; preview scale 0.45).
    - Put the spacing maths in a small public static method so an EditMode test can check it.
  - Nothing else in the file changes. Keep his style.
- **`Assets/Scripts/UI/PhoneBookContactRow.cs`:**
  - A full-row hit area (a transparent `Image` on the row root, assigned in `Configure`, with `raycastTarget` off by default).
  - `SetTappable(bool)`, which turns that raycast on or off.
  - `IPointerClickHandler`, raising `event Action<PhoneBookContact> Tapped` with the contact shown.
  - With taps off, it must not swallow clicks, so they reach the open book as now.
- **`Assets/Scripts/UI/PhoneBookPageView.cs`:** subscribes the rows it creates and forwards their taps (`event Action<PhoneBookContact> ContactTapped`); applies the tappable flag to new and existing rows.
- **`Assets/Scripts/UI/PhoneBookView.cs`:**
  - `public event Action<string, string> ContactTapped` (name, socket)
  - `public bool ContactTapsEnabled { get; set; }`, off by default and passed to both pages
  - Page turns (corners, arrow keys, tabs) stay unchanged.
- **`Assets/Prefabs/UI/PhoneBook.prefab`:** both `ContactTemplate` rows get the transparent `Image` (`raycastTarget` off), assigned to the row. Edit the prefab directly (`manage_prefabs` or `PrefabUtility.LoadPrefabContents` from `execute_code`). **Don't run `Update Phone Book Tabs` or any phone book menu.** `OpenBook/LetterTabs` must stay inactive; check this in the diff.
- **`Assets/Editor/PhoneBookBuilder.cs`:** `DirectoryPage` builds the same hit area on `ContactTemplate`, so a future rebuild by Sergei keeps it. Change nothing else, and don't run the menu.
- Don't touch any other file of his: `CableController`, `StoredCableView`, `CableBuilder`, `CableTrayBuilder`, `CableTray.prefab`, `Final.unity`'s loose objects or `MainMenu.unity`. If one of them turns out to be unavoidable, stop and report the exact change.

**Build**
1. **`Assets/Configs/Economy.json`:** add these entries after `income_3`, one per line, keeping the secret last:
   ```json
   { "id": "wires_1", "type": "extra_wires", "name": "Extra cord", "description": "4 cords", "costCents": 500, "wires": 4 },
   { "id": "wires_2", "type": "extra_wires", "name": "Extra cord", "description": "5 cords", "costCents": 800, "wires": 5 },
   { "id": "wires_3", "type": "extra_wires", "name": "Extra cord", "description": "6 cords", "costCents": 1300, "wires": 6 },
   { "id": "notepad", "type": "notepad", "name": "Notepad", "description": "Tap a name: note its jack", "costCents": 300 },
   ```
   Check that the texts fit the button at 1280×720. Shorten the description if needed, but keep the ids.
2. **`Docs/EconomyFile.md`:**
   - Mark `extra_wires` and `notepad` as ready (5b) and describe their fields.
   - Explain `wires`: it is the total, must grow, must be above the starting 3, and can be at most 6.
   - Explain that there is at most one `notepad`, and that deleting it removes the feature.
   - Give an example validator message for each new rule.
   - `extra_clients` stays "Coming in Step 7".
3. **Content:**
   - `UpgradeEntry.wires` (int); `IUpgrade.Wires`.
   - `EconomyFileLoader` loads `ExtraWires` and `Notepad`. `extra_clients` still reports "arrives in Step 7".
   - `GameConfig.startingCords` (default 3, `Min(1)`) and `extraCordColors` (3 colours).
   - `IGameContent.StartingCords`; update the fakes.
4. **Domain:**
   - **`StatSheet`:**
     - `Cords`: the `wires` of the last bought `extra_wires` entry, or `StartingCords`.
     - `HasNotepad`: true while any `notepad` entry is owned.
     - Raises `Changed` after every recompute, including `ResetUpgrades`.
   - **`Notepad`** (new, `Assets/Scripts/Domain/Upgrades/`):
     - `bool Note(JackId jack)` notes the jack on the call in hand only when `HasNotepad`, a call is in hand, and the jack belongs to its caller or its recipient, found through `PhoneDirectory`. Otherwise it changes nothing and returns false.
     - Notes live on the call: for example `Call.CallerNoted` and `Call.RecipientNoted`, or a per-call record. The agent may choose, but no per-tick allocation.
     - Raises `Noted(CallId)` once per new note. Tapping the same person again changes nothing.
   - The spawner already reads `ISwitchboard.CordCount` live. Check it, and don't change it unless a test shows a gap.
   - Build `Notepad` in `GameSession`.
5. **Game:**
   - **`CordBridge`:**
     - Finds the one `CableInputController` by type in its scene (log a clear error otherwise).
     - `EnsureCords(int total)` calls `AddCable` with the next colour from `extraCordColors` until the count is reached or `AddCable` returns null.
     - Appends each new cable to `cables` and `observed` with its current state, so the next `LateUpdate` reports nothing spurious.
     - New cord ids continue the index order (3, 4, 5).
   - **`GameInstaller`:**
     - Calls `EnsureCords(Stats.Cords)` at start and on `Stats.Changed`.
     - Warns if the scene's cord count differs from `startingCords`.
     - Finds the one `PhoneBookView` by type.
     - Sets `ContactTapsEnabled = Stats.HasNotepad` at start and on `Stats.Changed`.
     - Routes `ContactTapped` to `Session.Notepad.Note(new JackId(socket))`.
     - Unsubscribes on destroy. Keep the installer thin; a small plain class for the book wiring is fine. No new root objects.
   - **`CallCardView`:**
     - The in-hand bubble adds " (C3)" after a noted name, keeping the bold surname style.
     - A new note updates the bubble text at once, without re-typing the whole bubble.
     - Check that the longest names with jacks still fit the bubble, and shrink the jack text slightly if needed.
6. **Dev panel:** no new buttons. "Reset upgrades" behaves as described in the decisions.

**Tests** (`Switchboard.Domain.Tests`, plus Content tests where they live; Sergei-type tests in `Assets/Tests/Game/Editor/`)
- **`StatSheet`:**
  - `Cords` is `StartingCords` with nothing bought.
  - It is the last bought value, set and not stacked (4 then 5 gives 5).
  - `HasNotepad` is false, then true after buying.
  - `Changed` is raised on purchase and on reset.
- **Spawner:** with waiting calls, the interval stretches less once the switchboard's cord count grows (a fake switchboard whose count changes mid-session).
- **`Notepad`:**
  - It does nothing without the upgrade.
  - It notes the caller's jack and the recipient's jack, each once, and raises `Noted`.
  - It ignores a jack not on the call in hand, and does nothing with no call in hand.
  - Notes stay on a call after swapping away and back, and don't leak to another call.
  - It works for an emergency's service recipient.
- **Loader and validator:**
  - The real file loads 8 entries with no problems.
  - `wires` not growing; `wires` ≤ `startingCords`; `wires` > 6; a second `notepad`.
  - `extra_clients` still reports Step 7. Replace the 5a test that used `extra_wires` as the "not ready" example.
  - Each message names the entry and the fix.
- **Shelf with the real file:** `income_1`, `wires_1`, `notepad`. After the notepad is bought, slot 3 goes empty and slots 1 and 2 don't move.
- **Sergei-type tests** (`Assets/Tests/Game/Editor/`):
  - Tray spacing: 3 slots give today's positions, and 4–6 slots fit the usable width with no overlap.
  - `PhoneBookView` raises `ContactTapped` with the row's name and socket when a row is clicked with taps on.
  - With taps off, the row's hit area isn't a raycast target.

**Play Mode checks** (stepped frames; see the Plan §4 cheat sheet):
- Buy all three wire upgrades (dev "+ $100"): 6 cords in the tray, each picked up and patched with `ProcessPointer`.
- A 4th cord connects a real call and pays.
- Buy a wire while all 3 original cords are connected: the new cord appears in the tray, and the connected calls aren't disturbed.
- Notepad: with a call in hand, a raycast click on the caller's row and the recipient's row shows both jacks in the bubble. A click on another row changes nothing and the book stays open. Before buying, a row click closes the book.

**Done when** Plan §5 is met and every test above passes. Screenshots in `Temp/Captures/`:
- `step-05b.png`: the shop at the start, with income, Extra cord, Notepad and the secret
- `step-05b-cords.png`: 6 cords in the tray, with one of the new ones connected on the board
- `step-05b-notepad.png`: the book open with the bubble showing both noted jacks
- `step-05b-720.png`: the tray with 6 cords and the shop at 1280×720

**Playtest script (you)**
1. Play `Final.unity`. The shop shows Better rates ($8), Extra cord / 4 cords ($5), Notepad ($3) and the secret.
2. Earn $5 (or F1, then "+ $10") and buy Extra cord. A 4th cord in a new colour appears in the tray, the cords move closer together, and the button now offers 5 cords for $8.
3. Pick up the new cord and connect a call with it. It pays, can be put on hold and restored, and goes back to the tray like the others.
4. Buy 5 and 6 cords. All 6 fit in the tray and each one works. The button goes back to "+".
5. Before buying the Notepad: open the book and tap a name. The book closes, as before.
6. Buy the Notepad. Take a call, open the book and tap the caller's name: their jack appears in the bubble, e.g. "Ada Moss (C3)". Tap the wanted person: their jack appears too. The book stays open.
7. Tap someone else: nothing happens. Swap to another call and back: the notes are still there.
8. Known issue (Sergei's code, not changed): tapping a book row while holding a cord drops the cord back in the tray.
9. Open `Economy.json`, set `wires_2`'s `wires` to 4, and run `Tools/Switchboard/Validate Content`. You get a plain message about the chain. Undo the change.

**Out of scope:**
- removing cords
- more than 6 cords
- notes for people not on the call in hand
- re-showing the A–Z tabs
- keeping a held cord when a button or row is clicked (Sergei's call)
- moving calls and pacing into the file (5c)
- saving purchases (6)
- board growth (7)
- shop sounds (8)

### Step 5c: Calls, pacing and starting money into `Economy.json` (D39, D47)

**Goal.** The designer tunes the whole economy in one file. Each call type's money and timing values, the pacing, the starting money and the starting cords move from Unity assets into `Assets/Configs/Economy.json`. The game plays **exactly** as before. Only presentation (card badge and accent) stays in Unity. **Recommended effort:** high.

**Playable result.**
- Press Play: nothing looks or plays differently. Regular calls ring about every 8 s (the first after 2 s), pay 5–15¢ on connect plus 3–8¢/s for 15–30 s. Emergencies (red badge) still have the 30 s ring, the $2 and $1 fines, and at most one at a time.
- Edit a value in `Economy.json` (for example Regular's `spawnWeight`, or `startingCents` 500), press Play again: the change is live, with no Unity asset touched.
- Break a value (for example a `min` above its `max`) and run `Tools/Switchboard/Validate Content`: one plain message names the call type, the field and the fix.

**Decisions for this step** (lead, 2026-10-18; D47):
- **`startingCords` moves into the file** too, next to `startingCents`. The cord colours (`extraCordColors`) stay in `GameConfig` (presentation). `maxWaitingCalls` stays in `GameConfig` too: it is the caller panel's slot count, which Step 7's `clientSlots` will replace.
- **The old asset fields are deleted, not kept as a fallback.** The file is the only source:
  - `CallerTypeDefinition` keeps only `id` and `cardStyle`. It is no longer an `ICallerType`. Keep its class name, script file and the two assets (`Regular.asset`, `Emergency.asset`), because `GameConfig` references them by GUID. Update its summary: "How cards of one call type look."
  - `GameConfig` loses `startingCents`, `startingCords` and `pacing`. Its `callerTypes` list becomes the card-style list (`[FormerlySerializedAs("callerTypes")]` if renamed, so the two references survive).
  - `PacingProfile.cs`, `Assets/Content/Pacing/Default.asset` and the `Pacing` folder are deleted. The file's pacing uses the domain's existing `PacingSettings`.
- **Ranges are objects:** `"rateCentsPerSecond": { "min": 3, "max": 8 }`.
- **Type ids stay `type.regular` and `type.emergency`.** `PhoneBook.json` references them in each contact's `type`, and that shared file doesn't change. (Upgrade ids keep their `a-z0-9_` rule; call ids keep the `type.` style, an accepted difference.)
- **The display name moves into the file as `name`**, so a designer can add a new call type without any Unity asset. A type with no matching card-style asset gets plain cards, with no message. A card-style asset whose id isn't in the file gets a validator message.
- **Per-contact `rate` and `fee` overrides stay in `PhoneBook.json`**, unchanged.

**The file after this step** (one entry per line; key order as shown; values are today's, from `Regular.asset`, `Emergency.asset`, `Default.asset` and `GameConfig.asset`):
```json
{
  "startingCents": 0,
  "startingCords": 3,
  "pacing": { "firstCallDelaySeconds": 2, "baseIntervalSeconds": 8, "intervalJitter": 0.3, "loadFactor": 0.5, "idleTimeoutSeconds": 60 },
  "calls": [
    { "id": "type.regular", "name": "Regular", "spawnWeight": 5, "rateCentsPerSecond": { "min": 3, "max": 8 }, "feeCents": { "min": 5, "max": 15 }, "durationSeconds": { "min": 15, "max": 30 }, "callbackDelaySeconds": { "min": 20, "max": 40 }, "responseTimeSeconds": 0, "fineMissedCents": 0, "fineEarlyDisconnectCents": 0, "callbackAfterMiss": false, "callbackAfterDisconnect": false, "maxSimultaneous": 0, "everyoneCalls": false, "recipientTag": "" },
    { "id": "type.emergency", "name": "Emergency", "spawnWeight": 1, "rateCentsPerSecond": { "min": 5, "max": 10 }, "feeCents": { "min": 20, "max": 40 }, "durationSeconds": { "min": 10, "max": 20 }, "callbackDelaySeconds": { "min": 20, "max": 40 }, "responseTimeSeconds": 30, "fineMissedCents": 200, "fineEarlyDisconnectCents": 100, "callbackAfterMiss": false, "callbackAfterDisconnect": false, "maxSimultaneous": 1, "everyoneCalls": true, "recipientTag": "emergency-service" }
  ],
  "upgrades": [ ...unchanged... ]
}
```
The field names are a proposal. Rename one only if it reads clearly better to a designer, and say so in the report.

**How it fits together:**
- **Domain unchanged.** It already reads through `ICallerType`, `IPacingProfile` and `IGameContent`. If a domain change looks necessary, stop and report.
- **Content:**
  - `EconomyFileData` gains the new fields; `EconomyFileLoader` builds `CallTypeRecord`s (new, `ICallerType`), the `PacingSettings`, `StartingCents` and `StartingCords`, and `EconomyCatalog` exposes them.
  - Load the economy file first. `LoadPeople` then takes the file's call types, because people reference type ids.
  - `GameContent` takes these values from the economy catalog, not from `GameConfig`.
  - `GameConfig.CardStyleOf(typeId)` keeps working from the style assets.
- **The wires checks** use the file's `startingCords`.
- **`startingCords` may be 3 to 6.** Sergei's tray has 3 built-in cords, and removing cords isn't supported, so below 3 gets a message and falls back to 3. Above 3, the extra cords should appear at start the same way bought cords do. Check how `CordBridge`/`GameInstaller` sync cords to `StatSheet` (the installer's "scene has N cords" warning must not fire for 4–6). If that doesn't come for free, keep 3–6 valid in the validator, stop short of new wiring, and report.
- **Missing values must not silently become 0.** `JsonUtility` gives 0/false/null for a missing field (check whether field initializers survive `FromJson`, with a test).
  - Required in each call entry, with a plain message if missing: `id`, `name`, `spawnWeight`, `rateCentsPerSecond`, `feeCents`, `durationSeconds`. A call entry with a problem is left out, like an upgrade.
  - The rest may be left out and mean "off": no response time, no fines, no callbacks, no limit, no tag, and `callbackDelaySeconds` 20–40. `EconomyFile.md` says so.
  - Missing `pacing` (or one of its fields): use `PacingSettings.Default`'s value with a message. Missing `startingCents`: 0, no message. Missing `startingCords`: 3, no message.
- **Validator messages** follow the upgrades' style: `Call type 2 "type.emergency": feeCents min 40 is above its max 20. Swap them.` Move the current `ContentValidator.CheckTypes` and `CheckRange` rules into the economy loader:
  - unique ids
  - min ≤ max and no negatives
  - duration above 0
  - no negative response time, fines, `maxSimultaneous` or `spawnWeight`
  - at least one type with `spawnWeight` above 0
  - `intervalJitter` from 0 to 0.95
  - `baseIntervalSeconds` at least 0.1, and the other pacing values not negative (as the old asset's `Min`/`Range` attributes did)
  
  The people checks (unknown `type` in `PhoneBook.json`, enough recipients per type) stay in `ContentValidator`, fed the file's types. A message that used to say "GameConfig's caller types" now points at the file.
- **Sergei's persistent wiring (D45)** in `OperatorDesk`/`CallService` reads call types only through `ICallerType`. The orchestrator checked: `OperatorDesk` touches none of the moving fields. Confirm it for `CallService` and note it in the report.
- **Dev panel** "<type> now" labels use `DisplayName`, which now comes from the file's `name`.

**Prove it plays the same** (the 5c notes):
- A test `EconomyFileCallsMatchStep5bValues` (any name) reads the real `Assets/Configs/Economy.json` and asserts every value listed above for both types, plus pacing, `startingCents` 0 and `startingCords` 3. Write it **before** deleting the asset fields, and run it against the file you wrote.
- The real-file test also checks that `Validate Content` finds no problems on the shipped files (extend the existing real-file tests).
- Loader tests: the full sample parses; each required field missing gives its message; min > max; a negative fine; a duplicate id; no type with weight; jitter out of range; `startingCords` 2 and 7; a card-style id not in the file; a `PhoneBook.json` type not in the file; a file with no `calls` section (one message, and no calls ring). The wires checks use the file's `startingCords`: with `startingCords` 4, `wires_1` = 4 is reported.
- `Fakes.cs` keeps working (its fakes implement the domain interfaces), so existing domain tests shouldn't change.

**Files this step may change** (all ours):
- `Assets/Scripts/Content/*`: `EconomyFileData`, `EconomyFileLoader`, `EconomyCatalog`, `GameConfig`, `GameContent`, `CallerTypeDefinition`, `ContentValidator`, `PeopleFileLoader` (only if its signature needs it), plus a new `CallTypeRecord`. Delete `PacingProfile.cs`.
- `Assets/Scripts/Game/` (`GameInstaller`, `DevPanel`, the validator menu) only as needed.
- `Assets/Content/` assets, as described above.
- `Assets/Configs/Economy.json` (keep the upgrades exactly as they are).
- Tests in `Assets/Tests/Domain/`.
- `Docs/EconomyFile.md`: a new "Calls", "Pacing" and "Starting money and cords" section each, written for the designer, with examples and the validator's messages. Mark the sections "Yes (5c)" and drop "Until Step 5c...". Explain cents vs. seconds, what each pacing value does in plain words (for example, `loadFactor`: "each waiting call beyond the free cords stretches the gap by this share"), that `spawnWeight` is relative (5 vs 1 means five regular calls for each emergency), and that the per-person `rate`/`fee` in `PhoneBook.json` still override a type's.

No team-owned file changes. `Final.unity` and the prefabs shouldn't need to change: say so if they do.

**Playtest script:**
1. Press Play in `Final.unity` (and once from `MainMenu`). Calls ring and pay as before; an emergency appears now and then, with its badge, 30 s ring and fines.
2. Stop. In `Economy.json`, set `startingCents` to 500. Play: the counter starts at $5.00. Undo it.
3. Set Regular's `spawnWeight` to 0. Play: only emergencies ring (about one every 8 s, never two at once). Undo it.
4. Set `pacing.baseIntervalSeconds` to 3. Play: calls arrive much faster. Undo it.
5. Set `startingCords` to 4. Play: 4 cords in the tray from the start, and the shop's first cord offer is "5 cords". Undo it. (If the agent reported that this isn't wired, skip it.)
6. Set Emergency's `feeCents` to `{ "min": 40, "max": 20 }` and run `Tools/Switchboard/Validate Content`. A plain message names Emergency's fee. Undo it and validate again: "no problems found".
7. Open `Regular.asset` in the Inspector: only the id and the card look are left. `Default.asset` (pacing) is gone.

**Out of scope:**
- progress-driven pacing curves and weights (7)
- `extra_clients` and board growth (7)
- saves (6)
- balancing the values (10): keep them exactly
- moving card styles or portraits into a file
- changing `PhoneBook.json`

### Step 6: Board growth (D42, D48, D49), built as "7a"

**Goal.** The game starts on a small board and the player buys it bigger. A new game opens only rows A–C and columns 1–3 (9 jacks) and 3 caller slots. Three `extra_clients` upgrades open 4×4, 5×5 and 6×6, each also adding a caller slot. Only people on open jacks are in the game: they call, they're called, and they're in the book. Built before Step 6 (lead, 2026-10-18). **Recommended effort:** high.

**Playable result.**
- **Start.**
  - Press Play: only the top-left 3×3 of the board is live. The other jacks, and their row (D–F) and column (4–6) lamps and labels, are visibly dimmed. They don't react to hover or clicks, and a held cord can't go in.
  - Only 3 caller slots take calls. The other 3 look closed.
  - The book lists only the 9 people on A1–C3. There are no emergencies yet, because no emergency service is on the open board.
- **Buying "Bigger board" (4×4).**
  - Row D and column 4 light up at once, the 7 new people join the book and start calling, and a 4th slot opens.
  - The Doctor (D4) is now on the board, so emergencies can ring.
  - 5×5 brings the Fire Brigade (E5) and 6×6 the Police (B6), so emergencies come more often as the board grows.
- **The full board** at 6×6 plays exactly like today.

**Decisions** (lead, 2026-10-18; D49):
- **Three steps:** 4×4, 5×5, 6×6, as `extra_clients` entries in `Economy.json` (D40: values set the result, chains grow).
- **Caller slots grow with the board:** 3, then 4, 5 and 6. The same upgrade sets both.
- **Services, one per new ring:**
  - The Doctor stays on D4 (4×4).
  - The Fire Brigade moves from C2 to E5 (5×5), swapping with Reginald Underhill, who goes to C2.
  - The Police stays on B6 (6×6).
  - The services stay first in `PhoneBook.json`.
  - The starting nine are then Arthur Hollis A1, Mabel Dunmore A2, Mildred Stanhope A3, Lillian Yardley B1, Irene Godfrey B2, Gertrude Rook B3, Bernard Quigley C1, Reginald Underhill C2 and Ada Moss C3.
- **Pacing curves and the tutorial are Step 7b**, not this step.
- **No ownership split (D48).** Change the board, lamp, socket, cord-input, caller-panel and phone-book code directly where needed. List every changed file in the report.

**The board is controlled from `Economy.json`** (lead, 2026-10-18). Every board value comes from the file, and nothing about sizes is hard-coded:
- the starting board size and the starting caller slots
- each upgrade's board size, caller slots, price, name and description
- how many board upgrades there are: the designer adds or removes `extra_clients` lines, with no code change
- where people and services sit: `PhoneBook.json`, so who joins at which size follows from their jacks

No constant such as 3×3, "rows A–C" or "3 slots" may appear in code or tests outside test fixtures. The only fixed limit is the physical board in `Final.unity` (6×6 sockets) and the 6 caller slots in the scene. The game reads those from the scene; the validator uses the known 6×6 board (`ContentValidator.BoardJacks`) and 6 slots, and reports a size beyond them.

**Economy file additions** (the field names are a proposal, as in 5c):
```json
"startingBoard": { "cols": 3, "rows": 3 },
"startingClientSlots": 3,
...
{ "id": "board_1", "type": "extra_clients", "name": "Bigger board", "description": "4x4, 4 callers", "costCents": 1000, "boardSize": { "cols": 4, "rows": 4 }, "clientSlots": 4 },
{ "id": "board_2", "type": "extra_clients", "name": "Bigger board", "description": "5x5, 5 callers", "costCents": 2500, "boardSize": { "cols": 5, "rows": 5 }, "clientSlots": 5 },
{ "id": "board_3", "type": "extra_clients", "name": "Bigger board", "description": "6x6, 6 callers", "costCents": 6000, "boardSize": { "cols": 6, "rows": 6 }, "clientSlots": 6 },
```
- **Prices** are placeholders ($10, $25, $60) for the designer to tune.
- **Use "x", not "×"** in texts, unless the glyph is confirmed in the static LiberationSans atlas.
- **Where the entries go:** after `notepad`, before `secret`. Since the shop shows the first 3 chains in file order, "Bigger board" appears once a chain sells out: the $3 Notepad, usually. The designer can reorder the lines.
- **`cols` and `rows`:** columns are 1–6 and rows are A–F. The open area is always the top-left rectangle: rows A to the `rows`-th letter, and columns 1 to `cols`.
- **Validator rules,** in plain words like the others:
  - `startingBoard` and each `boardSize` are 1–6 per side. Along the chain, neither side shrinks and at least one grows. The first entry is bigger than `startingBoard`.
  - `startingClientSlots` and `clientSlots` are 1–6, the caller panel's slots, and never shrink along the chain.
  - Missing `startingBoard` or `startingClientSlots`: a plain message, and the game falls back to the full board and 6 slots, so it still starts. The shipped file always has both.
  - Remove the "arrives in Step 7" gate for `extra_clients`.
- **`GameConfig.maxWaitingCalls` goes away.** `startingClientSlots` and `clientSlots` replace it (D17's rule becomes "live calls never outnumber the open slots"). The installer warns if the scene's caller panel has fewer slots than the file can open.

**How it fits together:**
- **Domain:**
  - `StatSheet` gains `BoardCols`, `BoardRows` and `ClientSlots`: starting values plus the last bought `extra_clients`, raising `Changed`.
  - A small `OpenBoard` helper (or similar) answers `IsOpen(JackId)`.
- **Spawner and callbacks** read the live slot count instead of the fixed `maxLiveCalls`. The spawner only picks callers and recipients on open jacks. A type with no open recipient (an emergency before 4×4) or no open caller doesn't spawn, and `SpawnWeight` is shared among the types that can.
  - Callbacks are always for people already in the game. The board never shrinks in play, so they stay valid.
- **The board:**
  - A closed jack is dimmed and ignores hover, clicks and cord placement. That's a `SocketView` open flag, honoured by `CableInputController`'s jack hit-testing.
  - The lamps for closed rows and columns are dimmed and never light. The F label is plain text.
  - `CordBridge` or a small Game-layer view pushes the open area to the board whenever `StatSheet.Changed` fires.
- **The caller panel:** closed slots get a dark, translucent cover with raycasts off, and cards only use open slots. Check that the `CallerQueuePanel` art still reads well, and include a screenshot.
- **The book:**
  - The phone book shows only contacts on open jacks. Add a filter to `PhoneBookView`, such as `SetVisibleSockets(...)` or a predicate. Pages re-paginate, services stay first, and the surname order holds.
  - Hidden A–Z tabs stay hidden. `HasLetter` must respect the filter.
  - The Notepad keeps working.
- **People-file checks:**
  - "Each type has enough recipients" is judged on the **full** board. A call type whose recipients are all on closed jacks is fine.
  - New check: the starting board needs at least 2 untagged people, or no call could ever ring. Report it plainly.
- **Dev panel:**
  - "Emergency now" does nothing (or says why in its label) while no service is open.
  - Add "Open board" to buy the next `extra_clients` for free, like the other dev shortcuts.
  - "Reset upgrades" closes the board again. To stay safe, it also clears live calls and returns cords whose plugs sit in jacks that are closing. Report what it does.
- **No saves yet (Step 6):** a reload starts again on 3×3, as for all upgrades.

**Tests:**
- **Loader and validator:** every rule above, including defaults when the fields are missing.
- **`StatSheet`:** board and slot values follow purchases and the reset.
- **Spawner:**
  - It never picks a closed-jack caller or recipient.
  - Emergencies don't spawn until a service is open, then do.
  - Live calls never exceed the open slots, and callbacks respect them.
  - A 3×3 session of 10 minutes (seeded) only ever involves the starting nine.
- **People file:** the move (Fire Brigade E5, Reginald Underhill C2), and the real file passes `Validate Content`.
- **Book filter:** an EditMode test in `Assets/Tests/Game/Editor/` showing the visible contacts and pages for a 3×3 open area.
- **Board:** a closed `SocketView` refuses a cord (EditMode, if `CableConnectionTests` style allows; otherwise Play Mode smoke).

**Playtest script:**
1. **Start small.**
   - Press Play in `Final.unity`. Only A–C × 1–3 is bright. Dimmed jacks don't light on hover, and a held cord can't be plugged into them.
   - The other row and column lamps stay dark.
   - Three caller slots take calls; the other three look closed.
2. **The book.** Open it: only 9 people, on 2 pages, and no services. Calls only ever involve these 9.
3. **Buy 4×4.** Earn $10 (the dev panel's "+ $100" is fine) and buy "Bigger board". Row D and column 4 open at once, 7 new people appear in the book, a 4th slot opens, and the Doctor is listed first.
4. **Emergencies.** Within a minute or two (or with "Emergency now"), an emergency rings that wants the Doctor.
5. **Buy 5×5 and 6×6.** The Fire Brigade (E5) joins at 5×5 and the Police (B6) at 6×6. At 6×6 the board, book and slots are as before this step.
6. **Shop.** "Bigger board" showed up once a chain sold out, and is gone after 6×6.
7. **Validator.** In `Economy.json`, set `board_2`'s `boardSize` to `{ "cols": 4, "rows": 4 }` and run `Validate Content`: a plain message says the board must grow. Undo it.
8. **Reset.** "Reset upgrades" in the dev panel: the board shrinks back to 3×3 with no errors.

**Out of scope:**
- pacing curves and the tutorial (7b)
- cover-plate art (only if Mia draws it)
- animations and sounds for opening jacks (8)
- saving (6)
- balancing prices (10)

### Step 7: Hand-written calls and call queues (D51, D52, D54)

**Goal.** The designer writes every call by hand in a new `Assets/Configs/Calls.json`, and `Economy.json` decides which calls play, in which order, at each board size. Random calls, call types, callbacks, the idle guard, the call timer and the Notepad go away. The phone book stays as it is (`PhoneBook.json`) until Step 8. Built before saves and the Web build (Step 9) (lead, 2026-10-18). **Recommended effort:** high.

**Playable result.**
- **Start.** Press Play: the 3×3 board from Step 6 and **2** caller slots, both filled at once with the first two calls of `startPool`.
  - Each card shows the call's portrait, **the caller's name under it**, and its pay ("12¢ + 5¢/s").
  - Taking a card shows **the call's own `message`** in the speech bubble (typed out as today).
- **Connect.** Patch the call's `from` jack to its `to` jack. The fee is paid, then the per-second pay, until `durationSeconds` runs out ("Paid" stamp). The slot takes **the next call in the queue at once**.
- **Wrong patch.** "Wrong number!": the call ends with no fine and the next queued call takes the slot. The same call comes back only when the queue comes round to it.
- **Emergency.** A queued call with `"emergency": true` shows the red badge and its own countdown (`responseTimeSeconds`).
  - Missed: the "Missed" stamp and its `penaltyCents` fine.
  - No fine for a wrong patch or for cutting it off.
- **Bigger board.** Buying "Bigger board":
  - the board grows and one more slot opens
  - the queue switches to that upgrade's `stagePool`, starting from its first call
  - calls already on cards finish normally

**Decisions** (designer and lead, 2026-10-18; D51, D52, D54):
- **`Calls.json`** (new, the designer's file, one call per line):
  ```json
  {
    "calls": [
      { "id": "call_001", "icon": "caller-01", "name": "Ada Moss", "message": "Connect me with Mabel Dunmore, please.", "from": "C3", "to": "A2", "durationSeconds": 20, "connectCents": 10, "centsPerSecond": 5 },
      { "id": "call_002", "icon": "caller-08", "name": "Arthur Hollis", "message": "My neighbour is ill, get me the Doctor!", "from": "A1", "to": "D4", "durationSeconds": 15, "connectCents": 30, "centsPerSecond": 7, "emergency": true, "responseTimeSeconds": 30, "penaltyCents": 200 }
    ]
  }
  ```
  - The field names adapt the designer's draft to the file style used in `Economy.json`:
    - `fullname` → `name`
    - `duration` → `durationSeconds`
    - `connectIncome` → `connectCents`
    - `incomePerSecond` → `centsPerSecond`
    - `responceTime` → `responseTimeSeconds`
    - `penaltyCents` stays as it is
  - **All money is in cents.** `centsPerSecond` may have decimals (the wallet carries fractions); `connectCents` and `penaltyCents` are whole cents.
  - **Jacks are labels** such as `"C3"`: a row letter A–F, then a column 1–6, as on the board and in the book (designer: "from 1 and a").
  - **`icon`** is a portrait sprite name, as `portrait` is in `PhoneBook.json` today (`caller-01` … `caller-12`, `police`).
  - **`emergency`** defaults to false. `responseTimeSeconds` and `penaltyCents` are used only for emergencies.
- **`Economy.json`:**
  - **New `startPool`:** a list of call ids, the queue for a new game.
  - **Each `extra_clients` entry gains a `stagePool`:** the queue once that entry is bought. Values set the result (D40): the queue is the last bought `extra_clients`'s `stagePool`, or `startPool`.
  - **`startingClientSlots` becomes 2** (lead), and the board upgrades' `clientSlots` become 3, 4 and 5 (placeholders for the designer).
  - **Removed:** the `calls` section (call types), the `pacing` section and the `notepad` entry. If an old file still has them, the validator says plainly that they're no longer used.
  - **Names:** camelCase like the rest of the file: `startPool`, `stagePool` (the designer wrote `start_pool`, `stage_pool`; tell him in `EconomyFile.md`).
- **The queue (D52):**
  - **Slots.** Each open caller slot holds one call. Whenever a slot is free and the queue has a call, the next call appears in it **at once**: at the start, when a call ends, and when an upgrade opens a slot. There is no spawn timer.
  - **Order.** Calls play in pool order and loop back to the first. Success or wrong number doesn't change the order.
  - **Pool change.** Buying `extra_clients` replaces the queue and starts it at its first call.
  - **Busy lines.** A call whose `from` or `to` jack is in use simply waits on its card until the player can connect it (a plug can't go into an occupied jack anyway). No skipping for busy lines.
  - **No duplicate cards** (orchestrator rule, needed because the queue loops): a call that is still on a card is skipped when the queue comes round to it. If every call in the pool is on a card, the free slot stays empty until one ends.
- **Rules that stay:** hold and resume (2b), persistent wiring (D45), the income multiplier (5a, on fee and pay, never on fines), cords (5b), and the board growth from Step 6 (closed jacks dimmed and refused, slots growing). The connection rule now compares the plugged pair with the call's own `from` and `to`, in either order, as today.
- **Removed systems:**
  - random spawning: `CallSpawner`, `CallFactory`, `PacingSettings`/`IPacingProfile`
  - call types: `ICallerType`, `ICaller`, `CallTypeRecord`, type weights
  - callbacks ("Calling again"): `CallbackScheduler`
  - `BusyTracker` and `IdleGuard`
  - the early cut-off fine and the wrong-patch fine
  - the Notepad (D54): domain `Notepad`, `StatSheet.HasNotepad`, the `notepad` upgrade kind, `NotepadBookLink`, the noted jacks in the bubble, and the book's tap hooks if nothing else uses them

  Delete the code and its tests rather than leaving it unused. Keep `UpgradeKind`'s other explicit values unchanged.

**How it fits together:**
- **Domain:**
  - A call script type: id, name, icon, message, from, to, duration, fee, rate, emergency, response time, penalty.
  - A small queue class that knows the current pool and position, follows `StatSheet` (the pool comes from the last bought `extra_clients`, like the board and slots), and fills free slots in `GameSession.Tick`.
  - `CallRequest`/`Call` carry the script (or its values) instead of `ICaller`/`IPerson`/`ICallerType`.
  - `ConnectionRule` uses the call's jacks.
  - `PenaltyService` fines only missed emergencies.
  - `OpenBoard.IsInGame(person)` goes; `IsOpen(jack)` stays.
  - If `PhoneDirectory`, `IPerson` and the people catalog end up used only by the book, leave the book's own loading alone; Step 8 replaces it.
- **Content:**
  - A `Calls.json` loader and validator, next to `EconomyFileLoader`.
  - `GameConfig` references `Calls.json` the way it references `Economy.json`.
  - The card look: emergency calls use today's emergency card style (red badge and accent), regular calls the regular one. Keep the two `CallerTypeDefinition` assets as card styles, or simplify to two style fields; the look must not change.
- **Game:**
  - `CallQueueView`/`CallCardView`: the portrait from `icon`, the caller's name under the portrait, the bubble shows `message`. Remove the "wants" text, the Notepad notes and "Calling again".
  - The rate badge, stamps, coins, halos, the hold look and the emergency countdown ring stay.
  - `FirstOrderGuide` must still guide the first call.
- **Dev panel:**
  - Remove "Call now" and "Emergency now".
  - Keep "+ $10/$100", "Speed", "Show times", "Clear calls" (the slots refill from the queue), "Open board" and "Reset upgrades".
  - "Reset upgrades" also returns the queue to `startPool`'s first call.
- **No saves yet (Step 9):** a reload starts the queue again from `startPool`'s first call.

**Validator** (plain messages naming the call or entry and the fix, like the others):
- **`Calls.json`:**
  - a missing or unreadable file (the game still starts, with no calls)
  - unique, non-empty ids
  - required `name`, `icon` (a known portrait), `message`, `from`, `to`
  - `durationSeconds` above 0; `connectCents`, `centsPerSecond` and `penaltyCents` not negative
  - jack labels on the 6×6 board, and `from` different from `to`
  - an emergency needs `responseTimeSeconds` above 0
  - `responseTimeSeconds` or `penaltyCents` on a regular call gets a note that they're only used for emergencies
- **`Economy.json`:**
  - `startPool` present, not empty, and every id known
  - every `extra_clients` has a non-empty `stagePool` of known ids
  - every call in `startPool` fits inside `startingBoard`, and every call in a `stagePool` fits inside that entry's `boardSize`. Name the call and the jack: "call_014 uses E5, but board_1 opens only 4x4."
  - a pool with fewer calls than its stage's slots gets a note (some slots would stay empty)
  - the removed sections and the `notepad` type get a "no longer used" message
- **Across files:** every jack used by a call has someone listed on it in the phone book, or the player can't find it. Until Step 8 that's `PhoneBook.json`.
- **Missing values must not silently become 0** (the 5c lesson, Plan §4).

**Shipped content** (placeholders the designer replaces):
- Write `Calls.json` from today's people and jacks in `PhoneBook.json`, with portraits as there.
- `startPool`: about 8 calls among the starting nine (A1–C3), no emergencies.
- `board_1`'s `stagePool`: about 10 calls on 4×4, 2 of them emergencies to the Doctor (D4).
- `board_2`'s `stagePool`: about 12 on 5×5, adding the Fire Brigade (E5).
- `board_3`'s `stagePool`: about 14 on 6×6, adding the Police (B6).
- **Values in today's ranges:**
  - regular calls: fee 5–15¢, 3–8¢/s, 15–30 s
  - emergencies: fee 20–40¢, 5–10¢/s, 10–20 s, `responseTimeSeconds` 30, `penaltyCents` 200
- **Messages:** short, in English, in period voice ("Connect me with Ada Moss, please.", "Fire! Get me the Fire Brigade, quick!").
- The first call of `startPool` should be a simple one for the guide.

**Tests:**
- **`Calls.json` loader:** the sample parses; each rule above has a test.
- **Economy:** each pool rule; the removed sections; the real files pass `Validate Content`.
- **Queue:**
  - fills every free slot at once, in order
  - loops
  - skips a call still on a card
  - is replaced on purchase, starting at its first call
  - a wrong number frees the slot and the next call appears
  - a connected call holds its slot until it ends
  - a new slot from an upgrade fills at once
  - `ResetUpgrades` returns to `startPool`
- **Emergency:** the countdown; a miss fines `penaltyCents`, not multiplied; no fine for a wrong patch or a cut-off.
- **Connection rule:** the call's own jacks, in either order.
- **Cleanup:** remove the tests of deleted systems and list them in the report. Adapt Sergei's `MergedSystemsTests`, `FirstOrderGuideTests` and `CallAudioTests` to hand-written calls without weakening them.

**Files this step may change:**
- `Assets/Scripts/Domain/**`, `Assets/Scripts/Content/**`
- in the Game layer: `GameInstaller`, `CallQueueView`, `CallCardView`, `DevPanel`, `OpenBoardLink`, `FirstOrderGuide` (only as needed); delete `NotepadBookLink`
- `PhoneBookView` only to remove Notepad tap hooks nothing else uses
- `Assets/Configs/Calls.json` (new) and `Economy.json`
- `GameConfig.asset` and the two card-style assets
- tests
- `Docs/EconomyFile.md`: a "Calls.json" section and a "Call queues" section for the designer, with examples and the validator's messages. Remove the "Calls", "Pacing" and "Notepad" sections. Explain cents vs. seconds and jack labels, and that the book is only for looking things up.

`PhoneBook.json` doesn't change. `Final.unity` and the prefabs shouldn't need to; say so if they do.

**Playtest script:**
1. **Start.** Press Play in `Final.unity`: 2 cards at once. Each shows a portrait with the caller's name under it; taking one shows its own request text.
2. **Connect.** Patch the right pair: fee, per-second pay, "Paid". The slot fills with the next call at once.
3. **Wrong number.** Patch a wrong pair: "Wrong number!", no fine, the next call appears.
4. **Bigger board.** Dev "+ $100", buy "Bigger board" (or "Open board"): 4×4, a 3rd slot that fills at once, and calls from the new queue.
5. **Emergency.** Within the new queue, an emergency for the Doctor shows the red badge and countdown. Let one run out: "Missed" and the $2 fine. Patch another wrong: no fine.
6. **Edit the file.** In `Calls.json`, change the first call's message. Play: the new text shows. Undo it.
7. **Validator.** Set a call in `startPool` to use `"from": "E5"` and run `Validate Content`: a plain message says it's outside the 3x3 start. Undo it.
8. **No Notepad.** The shop never offers the Notepad.

**Out of scope:**
- `Book.json` and the new book layout (8)
- saves (9)
- the tutorial (10)
- sounds (11)
- balancing (13)
- writing the real call list (the designer)

### Step 8: The new phone book (D53)

**Goal.** A new `Assets/Configs/Book.json` replaces `PhoneBook.json`. The book becomes a lookup only, with each entry's name, profession, address and jack. Every entry shows from the start: the emergency services first, then everyone alphabetically by name. Built after Step 7, before Step 9. **Recommended effort:** high.

**Playable result.**
- **Opening the book** shows every entry from the first minute, whatever the board size: services at the top, then names A–Z ("BELLAMY, Frank", "CRANE, Dr. Otis" …).
- **Each row** shows name, profession, address and, last, the jack.
- **Looking up a call** ("Connect me with Ada Moss") still works: find her by surname, read her jack, patch it.

**Decisions** (designer and lead, 2026-10-18; D53):
- **Format** (one entry per line):
  ```json
  {
    "contactsPerPage": 6,
    "entries": [
      { "name": "Fire Brigade", "profession": "Fire station", "address": "Station Rd. 1", "emergencyService": true, "jack": "E5" },
      { "name": "MOSS, Ada", "profession": "Seamstress", "address": "Oak St. 4", "emergencyService": false, "jack": "C3" }
    ]
  }
  ```
  - `contactsPerPage` is optional (default 6); the designer's draft left it out.
  - `jack` is the column the designer forgot ("let's do it last column"), a label as in `Calls.json`.
  - Several entries may share a jack, like a household line ("BELLAMY, Frank" and "BELLAMY, Miss Rose" at the same address).
- **Order:** the book sorts itself. Services first, in file order; then everyone else by `name`, alphabetically and ignoring case. The designer writes "SURNAME, First", so this is the real-directory order of D50. The file's order doesn't matter.
- **All entries show from the start.** Remove Step 6's book filter: `SetVisibleSockets` and `OpenBoardLink`'s book part. The board itself still grows.
- **Letter lookup:** `HasLetter`/`ShowLetter` go by the first letter of `name`. The A–Z tabs stay hidden.
- **Text in English.** Check that every glyph the shipped file uses is in the book's fonts.

**How it fits together:**
- `PhoneBookView`/`PhoneBookConfig` read `Book.json`. The domain doesn't read the book at all: calls carry their own jacks since Step 7.
- Remove `PhoneBook.json` and whatever is left of the people model: `PeopleFileLoader`, `PeopleFileData`, `PeopleCatalog`, `PersonRecord`, `DirectoryRecord`, `IPerson`, `PersonExtensions` and `IDirectoryEntry`. Check that nothing else uses each one first. Step 7 already deleted `PhoneDirectory` and Step 6's people checks, and the people loader no longer reads `portrait`, `type`, `rate` or `fee`.
- `GameConfig` references `Book.json` instead.
- **Layout:**
  - Show four columns, Name, Profession, Address, Jack, readable at 1920×1080 and at 1280×720 (Plan §4, Game view size).
  - If four don't fit on Sergei's page, put profession and address on a smaller second line under the name, and keep the jack as the last column.
  - Keep page turning, the hand cursor and the guide's book glow working.
  - Drop the rows' unused tap image (`PhoneBookContactRow.tapArea` in `PhoneBook.prefab`), left over from the Notepad (D54).
  - Include screenshots of both sizes.
- **Validator** (plain messages):
  - the file is missing or empty
  - a missing `name` or `jack`
  - a jack that isn't on the 6×6 board
  - duplicate names
  - every call's `from` and `to` jack in `Calls.json` has at least one book entry (moved here from Step 7's interim check, `ContentValidator.ValidateCallJacks`)
  - `profession` and `address` may be empty
- **Shipped `Book.json`:** convert today's 36 contacts with their jacks. Write names as "SURNAME, First" and keep the three services. Professions and addresses are English placeholders in period style, families sharing an address now and then. The designer replaces them.
- **Docs:** a "Book.json" section in `Docs/EconomyFile.md` (or a sibling `Docs/BookFile.md` linked from it). The orchestrator updates the CLAUDE.md lines that name `PhoneBook.json`.

**Tests:**
- the loader and each validator rule
- the order: services first, then names alphabetically, whatever the file's order
- every entry visible on a 3×3 board
- `HasLetter`
- the page count with `contactsPerPage`
- the jack column's text
- the real file passes `Validate Content`
- Remove the Step 6 book-filter tests and replace them with "all entries show".

**Playtest script:**
1. **Open the book at the start (3×3).** All entries show: services first, then A–Z. Each row has a profession, an address and a jack.
2. **Look up a call.** Take a call, find the person in the book, patch their jack: it connects.
3. **Turn pages.** Page turning works through the whole book.
4. **Edit the file.** In `Book.json`, change someone's profession. Play: the new text shows. Undo it.
5. **Validator.** Remove the entry on a jack a call uses and run `Validate Content`: a plain message names the call and the jack. Undo it.

**Out of scope:**
- portraits in the book
- the A–Z tabs (stay hidden)
- saves (9)
- the real book content (the designer)

### Step 9: Saves and first Web build (outline). The `MainMenu` is the start screen: it replaces our planned `Boot.unity` and doubles as the tap-to-start audio unlock (D30, D33).
- **Saving:**
  - `SaveData` with a version number and migrations; `SaveService`.
  - `PlayerPrefsStorage` and `BrowserStorage` (a `.jslib` writing `localStorage`, plus a backup key).
  - Autosave as described in Design §5.12.
- **No `Boot.unity`:** `MainMenu` is the start screen, and its first tap unlocks audio (see the heading). The build goes through the `Build Web` menu below.
- **Hosting (D26, lead 2026-10-19): itch.io.**
  - Player Settings › Publishing Settings: Compression Format **Gzip**, **Decompression Fallback** on. itch can't send the compression headers, and Brotli unpacked in JavaScript is slower.
  - The lead creates the itch page (Draft) once; an agent can't log in. The playtest runs from a Draft or Restricted page first.
  - itch page settings: "This file will be played in the browser", mobile friendly, landscape, fullscreen button on, no auto-start (the MainMenu's tap is the audio unlock).
- **Build and upload (lead, 2026-10-19):** polishing goes on after Step 9, so every new version must be one menu click and one command.
  - **`Tools/Switchboard/Build Web`:** an editor menu that builds the build list (`MainMenu`, `Final`) for Web into `Builds/Web/` (already git-ignored), with the D26 settings, and logs where the build went and how big it is.
  - **Uploading with itch's `butler`:**
    - Installed on the lead's Mac (2026-10-19): `~/bin/butler` v15.32.0, the Apple-chip build (`darwin-arm64`), with `~/bin` on PATH in `~/.zprofile`. Homebrew's `butler` is an unrelated app. The lead is logged in (credentials in `~/Library/Application Support/itch/butler_creds`), so `butler push` works from an agent's shell too. `butler login` needs a real Terminal window; the `!` prefix has none.
    - The page is `https://llazypixell.itch.io/operator` ("Operator!", Draft, created by the lead 2026-10-19; `butler status llazypixell/operator` reaches it, no uploads yet). Each upload is `butler push Builds/Web llazypixell/operator:html5`. The draft's secret preview link stays out of the repo.
    - Page settings advised to the lead (check them after the first push): embed in page at 1280×720, mobile friendly (landscape), fullscreen button, no auto-start, no scrollbars, no SharedArrayBuffer.
    - butler sends only the changed files and replaces the same upload, so the itch page settings are set once. On the first push, tick "This file will be played in the browser" for the `html5` upload.
    - **Pushing publishes the game.** Agents run it only when the lead asks; the lead can also run it with `! butler push …`.
  - **Manual fallback:** zip the *contents* of `Builds/Web`, so `index.html` sits at the zip's root, and upload the zip on the itch page.
  - **`Docs/WebBuild.md`** for the team: build, push, the itch page settings, and the manual fallback.
- Includes the touch-input open item (§8).
- **From Step 7:** the save also needs the queue position: the index of the next call in the current pool (the pool itself follows from the bought upgrades). Live calls aren't saved; on load the slots fill from that position.
- **From Step 8:** the book has no state to save; it is rebuilt from `Book.json` at every start.
- **From Step 6:** the board size and caller slots are ordinary upgrade ids (`board_1`…), so a save restores them through `StatSheet` with no extra data; `OpenBoardLink` then reopens the jacks. Test that a loaded `board_2` opens 5×5 before the first call rings. `GameSession.ResetUpgrades` (clears calls, returns cords from closing jacks) is dev-only; a "new game" button would need the same care.
- **Tests:**
  - Saving and loading gives back the same data.
  - Migration from a v1 test file.
  - Unknown ids are ignored.
  - A corrupt main save falls back to the backup.
- **Playtest (browser):**
  - Earn and buy, then reload: progress (the balance and the bought upgrade ids) is restored.
  - Push a second build with butler, then reload the itch page: progress survives the new upload.
  - Close and reopen the tab: progress is restored.
  - On a phone, tap to patch.

### Step 10: Tutorial (outline)
- **Pacing is no longer part of this step:** since D51/D52 the designer sets the difficulty through the call queues in `Economy.json` (hand-written calls, one queue per board size), so the progress curves and type weights (D19, D37) are dropped.
- **Tutorial:** hints for the first calls, such as "Click the ringing card", "Find them in the book" and "Connect the two jacks". **Sergei's `FirstOrderGuide` (merged `e7acec5`, `6226ead`) already pulses the portrait, the book and the cords on the first call; build on it instead of adding a second guide**, and check with the lead and the designer what else is needed (the first calls of `startPool` are the tutorial calls).
- **Tests:** the tutorial sequence.
- **Playtest:** a new player gets through the first calls without help.

### Step 11: Sound and feel (outline)
- **From Step 3:** replace the placeholder `card-take-click.wav` (set on `UiClickSound` in `GameUI.prefab`) with a real sound, and fill `CallCardView.OnCharacterShown` with the callers' talking sound (lead request).
- **Audio:** `AudioService`, `AudioCueLibrary` and `FeedbackDirector` (Design §5.11):
  - One ringing loop and one alarm loop.
  - Coin ticks. (Sergei's merge `e7acec5` added a cash-register sound and fee coins flying to the counter; build on them.)
  - Cues for paid, hung up, missed and fined.
- **Animation:**
  - Cards: a ringing wobble, sliding in and out, and stamps.
  - A rolling money counter.
  - Popups.
- **Depends on** the sound-effects open item (§8).
- **Playtest:** it sounds and feels good without getting noisy.

### Step 12: Mobile and Web polish (outline)
- **Layout:**
  - Aspect ratios 16:9, 19.5:9 and 4:3.
  - A portrait "rotate your device" screen (D24).
  - Touch target sizes.
- **Performance:** nested canvases for the HUD and cards; a sprite atlas and texture compression.
- **Build settings:**
  - Remove unused packages (Visual Scripting, Multiplayer Center, Collab), once the team agrees.
  - Set compression for the host, with Decompression Fallback if needed.
- **Playtest:** on iOS Safari, Android Chrome and a desktop browser.

### Step 13: Balance (outline)
- **Simulation:** a headless EditMode simulation with a bot operator.
  - Its skill (lookup time, error rate) is configurable.
  - It runs hours of game time in seconds.
  - It reports the time to reach the mystery, the income curve and the fines.
- **Tuning:** adjust prices, rates and pacing in `Economy.json` toward the target in D27, with the designer.
- **Playtest:** a full run from a new save feels right.
