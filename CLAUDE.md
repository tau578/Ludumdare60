# Switchboard Operator (Ludum Dare 60)

A casual telephone switchboard-operator game:
- The player takes call cards and looks callers up in a phone book.
- They patch jacks with a limited number of cords.
- Connected calls pay a one-off connection fee plus a per-second rate.
- Wrong connections make the caller hang up; the same call returns only when the queue comes round to it (D52).
- A missed emergency brings a fine, but money never drops below 0 and there is no lose condition.
- The player buys upgrades and saves for a final "mystery upgrade" (the `secret` entry) whose effect is still undecided.
- The economy is driven by the designer's hand-edited `Assets/Configs/Economy.json` (D39, D40): one-time upgrade entries in chains, values that set the result, costs in cents.

Unity 6000.3.6f1, URP with the 2D Renderer, Web build for desktop and mobile browsers, hosted on itch.io (D26), mouse and touch through the new Input System. Team repo: `github.com/tau578/Ludumdare60`, branch `main`. Teammates push often.

## Read first, every session
1. `Docs/Progress.md`: status table (what's done, what's next), step reports, and verification and playtest notes.
2. `Docs/Plan.md`: the step workflow (§1), pre-flight (§2), rules for agents (§3), Unity MCP cheat sheet (§4), definition of done (§5), launch prompt (§6), report format (§7), open items (§8) and the step briefs (§9).
3. `Docs/Design.md`: the source of truth for game rules, architecture and decisions D01–D55. If the code or a brief disagrees with it, stop and ask; don't improvise.

**Next step:** see the first row in `Docs/Progress.md` that isn't "done". As of 2026-10-18, Step 5 is split (D39–D44): **5a** (the designer's `Economy.json`, the shop panel, income upgrades, the secret goal), **5b** (extra wires up to 6 cords, the Notepad) and **5c** (calls, pacing, starting money and cords in `Economy.json`, D47) are done, and so is **7a** (board growth from a 3×3 start, D49). Step 6 (board growth, built as "7a") is done too. Step 7 (hand-written calls and call queues, D51, D52, D54) is done too. Step 8 (the lookup-only phone book, `Book.json`, D53) is done too. Step 9 is split (D55): **9a** (saves, New Game menu, pause menu, touch input; D55, D56) is done (`580c610`); **9b** (the first Web build on itch.io, D26) is built, pushed to the itch Draft page, and in the lead's playtest; the double pointer waits for Sergei's cursor fix (his branch, `07678dc`) (team guide: `Docs/WebBuild.md`). Then 10 (tutorial), 11 (sound), 12 (mobile and Web polish), 13 (balance). Steps were renumbered on 2026-10-18 to follow the build order; older reports and commits use the old numbers.

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
     - The step's decisions settled; check Plan §8 for open items it could pick up.
  2. **Launch:** use the prompt template in `Docs/Plan.md` §6. Add orchestrator notes on anything that changed since the brief was written (teammate merges, new decisions, playtest feedback). Mark the step "building" in `Progress.md`.
  3. **Verify when the agent reports:**
     - Re-run the EditMode tests through MCP and read the console.
     - Check `git status`: every changed file is one the step needed, and the report lists it.
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

## Team
**No ownership split (D48, lead 2026-10-18).** The whole team works on the whole project together. There are no owners, read-only folders, coordinated changes or approval lists, and nothing needs to be asked about or reported to a teammate before changing it. Any step may change any file it needs (cord, board, book, shop, menu, scenes, prefabs), as long as its brief names the files and its report lists them. Prefer extending an existing component over building a second one.

Teammates still push to `main`, so pre-flight always merges first (see the git rules).

- **The designer** edits `Assets/Configs/Economy.json` (D39; one entry per line, guide in `Docs/EconomyFile.md`) and `Assets/Configs/Book.json` (D53; guide in `Docs/BookFile.md`) by hand. Keep both human-readable, keep validator messages plain, and keep the book's format and `contactsPerPage`. The book sorts itself (services first, then names A–Z), so the file's order doesn't matter.
- **Mia** makes the art (`Assets/Sprites/`, `Assets/Fonts/`).

## Architecture in brief (details in Design.md §4–§7)
- **Scene flow (D38).** `MainMenu` → `Final`. The menu loads the next build index. `Final.unity` (formerly `Level.unity`, same GUID) is the one game scene: the board prefabs plus `Game.prefab` and `GameUI.prefab` instances.
- **Assemblies:**
  - `Switchboard.Domain` (`noEngineReferences`): all rules, plain C#, ticked by `GameSession.Tick(dt)` in a fixed order. Time only comes from ticks, randomness only from a seeded `IRandom`, and money is a `long` of cents plus a carried fraction.
  - `Switchboard.Content`: the `GameConfig` ScriptableObject plus the loaders and validator for `Calls.json`, `Economy.json` and `Book.json`.
  - The Game layer (`Assets/Scripts/Game/`) is in **Assembly-CSharp on purpose**: asmdefs can't reference Assembly-CSharp, where the cord, board and UI code lives.
  - Tests: `Switchboard.Domain.Tests` (EditMode).
- **Integration with the cord code:**
  - **`CordBridge`** observes his `CableController`s every `LateUpdate`, reading only public members (Design §3). It reports plugs and circuits to `OperatorDesk`, and returns cords to the tray with his `TryRemove`.
  - `GameInstaller` finds the views and the board's objects in its own scene **by component type**. The only name lookups are `CallerQueuePanel` (the caller slots) and `UpgradeShopPanel/HeaderSlot` (the money plaque).
- **Composition root:** `GameInstaller`. No singletons, statics with state, service locators, or `FindObjectOfType`.
- **Communication:** views call verbs (`OperatorDesk.TryTake`), services raise C# events, and values that change every frame are polled in `LateUpdate`.
- **UI:**
  - Our `GameUI.prefab` is a Screen Space Overlay canvas (sorting order 10, 1920×1080 Expand), laid over the board's `SwitchboardCanvas`.
  - Anything not meant to be clicked has `raycastTarget` off, because his cord input takes the topmost hit. His book is also at sorting order 10, so nothing of ours may overlap it.
  - The money counter fills `HeaderSlot`. The call in hand shows as a speech bubble left of the caller column. Cards sit over the caller slots.
- **Data:** every call (caller name, portrait, message, jacks, pay) is hand-written in the designer's `Assets/Configs/Calls.json` (D51) and played from the queues in `Economy.json` (D52). The book reads `Book.json` (D53): name, profession, address and jack, every entry from the start; the rules never read it. Names are English.

## Code conventions
- Match the existing style:
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

## Open items (Plan §8)
Known issues and to-dos, such as touch input in the cord code, held cords dropping on button clicks, stale editor-menu paths and the hidden A–Z tabs, are listed in `Docs/Plan.md` §8. A step that fixes one removes it there.

## Open decisions (Design §10)
- D15: what the mystery upgrade does.
- D27: currency scale and target playtime.

**Designer:** edits `Assets/Configs/Calls.json`, `Assets/Configs/Economy.json` and `Assets/Configs/Book.json` by hand. Keep it human-readable and keep validator messages plain.

Don't assume answers to these; ask the lead when a step needs them.
