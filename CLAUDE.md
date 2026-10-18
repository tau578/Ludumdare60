# Switchboard Operator (Ludum Dare 60)

A casual telephone switchboard-operator game:
- The player takes call cards and looks callers up in a phone book.
- They patch jacks with a limited number of cords.
- Connected calls pay a one-off connection fee plus a per-second rate.
- Wrong connections make the caller hang up and call back later.
- Emergency calls bring fines (later step), but money never drops below 0 and there is no lose condition.
- The player buys upgrades and saves for a final "mystery upgrade" (the `secret` entry) whose effect is still undecided.
- The economy is driven by the designer's hand-edited `Assets/Configs/Economy.json` (D39, D40): one-time upgrade entries in chains, values that set the result, costs in cents.

Unity 6000.3.6f1, URP with the 2D Renderer, Web build for desktop and mobile browsers, mouse and touch through the new Input System. Team repo: `github.com/tau578/Ludumdare60`, branch `main`. Teammates push often.

## Read first, every session
1. `Docs/Progress.md`: status table (what's done, what's next), step reports, and verification and playtest notes.
2. `Docs/Plan.md`: the step workflow (§1), pre-flight (§2), rules for agents (§3), Unity MCP cheat sheet (§4), definition of done (§5), launch prompt (§6), report format (§7), team coordination (§8) and the step briefs (§9).
3. `Docs/Design.md`: the source of truth for game rules, architecture, ownership and decisions D01–D44. If the code or a brief disagrees with it, stop and ask; don't improvise.

**Next step:** see the first "not started" row in `Docs/Progress.md`. As of 2026-10-18, Step 5 is split (D39–D44): **5a** (the designer's `Economy.json`, Sergei's shop panel wired with real texts, income upgrades, the secret goal with a savings fill) is fully briefed in Plan.md; 5b (extra wires, Notepad) and 5c (calls and pacing into `Economy.json`) are outlines.

## Roles and workflow
- **The user is the project lead.** The main session is the **orchestrator**. It never implements a step itself; every step and every fix pass is built by a **subagent** launched with the Agent tool (`subagent_type: general-purpose`).
- **Effort:** run step agents at **high**, unless the lead asks otherwise. The lead has a limited token budget, so prefer small steps (split big ones, as Step 2 was split into 2a and 2b) and suggest fresh sessions over very long ones.
- **One agent at a time.** There is a single Unity editor. No parallel agents and no git worktrees, because the editor is bound to this folder.
- **One step cycle:**
  1. **Pre-flight (orchestrator):**
     - `git fetch`, then merge teammates' changes if any, and check what they touched.
     - Unity connected, idle and not in Play Mode.
     - Console cleared.
     - EditMode tests green.
     - The step's decisions and coordination items settled.
  2. **Launch:** use the prompt template in `Docs/Plan.md` §6. Add orchestrator notes on anything that changed since the brief was written (teammate merges, new decisions, playtest feedback). Mark the step "building" in `Progress.md`.
  3. **Verify when the agent reports:**
     - Re-run the EditMode tests through MCP and read the console.
     - Check `git status`. No team-owned file may be changed unless approved.
     - Look at the agent's screenshots in `Temp/Captures/`.
     - Skim the key code.
  4. **Update docs:**
     - `Progress.md`: status row, plus a verification note.
     - `Design.md`: the implementation status table, accepted deviations and new decisions.
     - `Plan.md`: refine the next brief, and add lessons to the cheat sheet.
     - The lead asked for the docs to be updated after every implemented piece.
  5. **Hand over:** give the lead the step's playtest script in plain language.
  6. **Fixes:** run them through a new subagent with the same brief plus the lead's notes.
- **Explain plainly.** The lead is learning Unity concepts (scenes, additive loading, ScriptableObjects and so on), so explain plainly and concretely, without jargon dumps.

## Git rules
- **Never commit or push unless the lead explicitly asks** ("commit", "commit locally", "push", "commit and push"). A playtest OK alone is not a request.
- **No AI attribution lines** (`Co-Authored-By: Claude…`, "Generated with Claude Code") in commits or PRs.
- **Before any push:**
  1. `git fetch`.
  2. If upstream moved, look at the incoming commits first: `git log HEAD..@{u}` and the diff stat.
  3. Merge with `git pull --no-rebase`.
  4. Resolve conflicts carefully, and re-run the tests.
- **Known conflict spots:**
  - `ProjectSettings/EditorBuildSettings.asset`: keep the order MainMenu (0), Final (1).
  - Untracked `.meta` files that Unity generated locally for files a teammate committed without metas. Remove ours and take theirs.
  - Duplicate imports, for example TextMeshPro. Compare GUIDs, then keep theirs.
- **Unity can re-save scenes:** the test runner can re-save an open scene in a newer format with no real changes, as happened with `SampleScene.unity`. Discard such format-only changes instead of committing them.

## Team ownership
| Owner | Area | Rule |
|---|---|---|
| Sergei | `Assets/Scripts/UI/`, `Assets/Scripts/Cables/`, `Assets/Editor/`, `Assets/Tests/Editor/` | Read-only unless the lead approves a specific change, which is then a "coordinated change". |
| Sergei | `Assets/Scenes/MainMenu.unity`, the prefabs in `Assets/Prefabs/` and `Assets/Prefabs/UI/`, the loose `SwitchboardCanvas` and `EventSystem` in `Final.unity` | Same. Never save `MainMenu.unity` when opening it to inspect. |
| Shared | `Assets/Scenes/Final.unity` (D38) | Scene shared, prefabs owned. Our part is the `Game` and `GameUI` prefab instances; we change our prefabs, not the scene. Adding or removing a root object needs the lead's OK and a heads-up to the team. |
| Sergei | Phone book (`PhoneBookView`), `UpgradeShopPanel`, main menu, `Assets/Tools/rodrigo_menu_tools`, volume panel | Same. |
| Mia (art) | `Assets/Sprites/`, `Assets/Fonts/` | Read-only. |
| Ours | `Assets/Scripts/Domain/`, `Assets/Scripts/Content/`, `Assets/Scripts/Game/`, `Assets/Tests/Domain/`, `Assets/Tests/Game/`, `Assets/Content/`, `Assets/Prefabs/Game/`, `Docs/`, `CLAUDE.md` | Agents change these. |
| Ours (designer edits) | `Assets/Configs/Economy.json` | The economy file (D39). One entry per line; the format guide is `Docs/EconomyFile.md`. |
| Shared | `Assets/Configs/PhoneBook.json` | The single people file (D31). Sergei's book reads `name` and `socket`; our game reads every field. Keep his format, `contactsPerPage`, and the surname sort order. |

**Division of work (D30):** Sergei builds the UI components; we build the rules and data and wire his components to them. Before building any UI, check whether Sergei already has one.

**Approved changes to his files so far** (from Step 5 on, D44: our agents build his missing hooks as coordinated changes, each brief listing the files):
- The `UpgradeShopPanel` prefab and `UpgradeShopPanelBuilder` constants (Step 1c: the `HeaderSlot` money plaque).
- Step 3 (D34, A–Z tabs in the phone book): `PhoneBookView.cs`, `PhoneBookPageView.cs`, `PhoneBookContactRow.cs`, a new tab component in `Assets/Scripts/UI/`, `PhoneBookBuilder.cs`, `PhoneBook.prefab`, and a test in `Assets/Tests/Editor/` for the tab jump. Used: `PhoneBookView.cs`, the new `PhoneBookTab.cs`, `PhoneBookBuilder.cs` (new menu `Tools/Switchboard/Update Phone Book Tabs`) and `PhoneBook.prefab`. The test went to our `Assets/Tests/Game/Editor/` instead.
- Step 5a (D41): `UpgradeSlotView.cs` (new, `Assets/Scripts/UI/`), `UpgradeShopPanelBuilder.cs` (menu `Update Upgrade Shop Buttons`), `UpgradePlaceholderButton.prefab`, `UpgradeShopPanel.prefab`: texts, price and savings fill inside his shop buttons.
- Step 5b (approved in principle, files to list in its brief): runtime cords in his cord input and tray; a tapped-entry event in his book.

## Architecture in brief (details in Design.md §4–§7)
- **Scene flow (D38).** `MainMenu` → `Final`. Sergei's menu loads the next build index. `Final.unity` (formerly `Level.unity`, same GUID) is the one game scene: Sergei's board plus our `Game.prefab` and `GameUI.prefab` instances. Sergei's first merge attempt (`1c20569`, prefab-less copies) was reverted in `cdc5876`.
- **Assemblies:**
  - `Switchboard.Domain` (`noEngineReferences`): all rules, plain C#, ticked by `GameSession.Tick(dt)` in a fixed order. Time only comes from ticks, randomness only from a seeded `IRandom`, and money is a `long` of cents plus a carried fraction.
  - `Switchboard.Content`: ScriptableObjects (`GameConfig`, `CallerTypeDefinition`) plus the `PhoneBook.json` loader and validator.
  - The Game layer (`Assets/Scripts/Game/`) is in **Assembly-CSharp on purpose**: asmdefs can't reference Assembly-CSharp, where Sergei's code lives.
  - Tests: `Switchboard.Domain.Tests` (EditMode).
- **Integration with Sergei's code:**
  - **`CordBridge`** observes his `CableController`s every `LateUpdate`, reading only public members (Design §3). It reports plugs and circuits to `OperatorDesk`, and returns cords to the tray with his `TryRemove`.
  - `GameInstaller` finds the views and Sergei's objects in its own scene **by component type**. The only name lookups are `CallerQueuePanel` (the caller slots) and `UpgradeShopPanel/HeaderSlot` (the money plaque).
- **Composition root:** `GameInstaller`. No singletons, statics with state, service locators, or `FindObjectOfType`.
- **Communication:** views call verbs (`OperatorDesk.TryTake`), services raise C# events, and values that change every frame are polled in `LateUpdate`.
- **UI:**
  - Our `GameUI.prefab` is a Screen Space Overlay canvas (sorting order 10, 1920×1080 Expand), laid over Sergei's canvas.
  - Anything not meant to be clicked has `raycastTarget` off, because his cord input takes the topmost hit. His book is also at sorting order 10, so nothing of ours may overlap it.
  - The money counter fills `HeaderSlot`. The call in hand shows as a speech bubble left of the caller column. Cards sit over the caller slots.
- **Data:** people, jacks and per-caller overrides live in `PhoneBook.json`; call types (rate, fee and duration ranges) live in ScriptableObjects. Rates and fees are rolled as whole cents when the range allows. Names are English.

## Code conventions
- Match Sergei's style:
  - `sealed` classes by default
  - `[SerializeField] private` fields
  - small focused types
  - few comments, only ones that explain why
  - namespaces `Switchboard.Domain`, `Switchboard.Content`, `Switchboard.Game`
- Every domain rule gets an EditMode test.
- Data gets stable string ids and explicit enum values, and nothing writes to ScriptableObjects at runtime.
- Web: single thread, so no `Task.Run`, threads or `async void`. Avoid per-frame allocations and LINQ in per-frame code.
- Text: use only glyphs in the static TMP atlas (LiberationSans SDF). "¢" and "—" are fine; "→" is missing.

## Unity MCP notes (full cheat sheet in Plan §4)
- **Readiness:**
  - Check `mcpforunity://instances` for `Ludumdare60@67eac6d9bbf6b918` and `mcpforunity://editor/state` with `ReadMcpResourceTool` (server `UnityMCP`).
  - **Subagents can't read MCP resources.** They use `refresh_unity` with `wait_for_ready: true` instead.
- **Tool groups:** turn on `testing`, `scripting_ext` and `docs` with `manage_tools activate` if they're off. Load tools with ToolSearch.
- **C# edits:**
  - MCP `apply_text_edits` corrupted edits in Step 1b, so C# may be edited with file tools.
  - Always compile afterwards: `refresh_unity` with `compile: "request"`, then `read_console`.
- **Tests:** `run_tests` with `mode: "EditMode"`, then `get_test_job` with `wait_timeout: 60–90`. Tests can't start while Unity is in Play Mode, which usually means the lead is playtesting, so don't stop it.
- **Play Mode testing:**
  - Run In Background is off, so Play Mode freezes while Unity isn't the front window. Drive frames by calling `EditorApplication.Step()` from `execute_code` (Plan §4).
  - **Never set `Application.runInBackground` in the editor**: it writes PlayerSettings.
  - To speed things up, set `Time.timeScale` temporarily and reset it afterwards.
  - Simulate clicks inside a one-shot `Canvas.willRenderCanvases` handler.
- **Screenshots:** `manage_camera screenshot` with `output_folder: "Temp/Captures"` (git-ignored). Never save under `Assets/`. Take them while Play Mode is paused or being stepped.
- **Leave the editor clean:** idle, not in Play Mode, compiled, `Final.unity` saved as the active scene, console clean, and no temporary callbacks or editor state left behind.

## Pending coordination with Sergei (Plan §8)
- **Step 5 (D44):** Sergei has none of the hooks, so our agents build them as coordinated changes: shop button texts (5a), runtime cords and the book's tapped-entry event (5b). Tell him before and after.
- **Step 7 (D42):** board growth from 3×3. His book shows only people on open jacks, and closed jacks are dimmed. Both touch his book and board.
- **Step 6:** touch input in `CableInputController` (`Pointer.current`).
- **Tell Sergei (scene merge):** `Level.unity` is now `Final.unity`; his `LevelBackgroundBuilder` and `GamePresentationBuilder` menus still name the old path; "scene shared, prefabs owned"; suggest making `SwitchboardCanvas` and `EventSystem` prefabs.
- **Ongoing:** keep the names `CallerQueuePanel`, `UpgradeShopPanel` and `HeaderSlot`, and keep the extra fields in `PhoneBook.json`.
- **His `PhoneBookBuilder`** points at a missing `Assets/Settings/PhoneBook.json`.
- **A–Z tabs are hidden** (lead, 2026-10-18): `OpenBook/LetterTabs` is inactive in `PhoneBook.prefab`; the code, objects and tests stay. His `Update Phone Book Tabs` menu rebuilds them active, so untick them again afterwards.
- **Feedback for Sergei (Step 3 playtest):** a click on a button or card while holding a cord drops the cord; the lead would prefer it kept in hand. Not changed by us.
- **Tell Sergei (Step 3):** his book has A–Z tabs now, built by the new `Tools/Switchboard/Update Phone Book Tabs` menu, which `CreatePrefab` also calls. The tabs for the spread on show are highlighted and nudged out (`PhoneBookTab.SetRest`). Our card click sound uses his `AudioChannelSource` (Sfx) unchanged.

## Open decisions (Design §10)
- D15: what the mystery upgrade does.
- D26: hosting target.
- D27: currency scale and target playtime.

**Designer:** edits `Assets/Configs/PhoneBook.json` and `Assets/Configs/Economy.json` by hand. Keep it human-readable and keep validator messages plain.

Don't assume answers to these; ask the lead when a step needs them.
