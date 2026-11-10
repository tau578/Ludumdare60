# Switchboard Operator: Design

This document is the source of truth for how the game works and how the code is organized. Implementing agents follow it. If the code or a step brief disagrees with it, they stop and report instead of improvising. Changes are agreed with the project lead first, then written here.

Last updated 2026-11-08 · Plan: [Plan.md](Plan.md) · Progress: [Progress.md](Progress.md)

> **Loop redesign (lead, 2026-11-07 and 2026-11-08):** the 2026-10-25 playtest sent the game back to its loop. Section 1 now describes the target game and §12 the redesign in detail (D61–D74, D77–D81); the steps that build it are **13a–13k** in Plan §9 and come before 14, 15, 16 and 17. Sections 4–6 still describe the build as it is; where a part changes, its paragraph says which part of step 13 changes it.

## Implementation status

> **Steps renumbered (lead, 2026-10-18)** so the numbers follow the build order. Old → new: 7a → 6 (board growth, done), 7c → 7 (hand-written calls), 7d → 8 (the new book), 6 → 9 (saves, Web build), 7b → 10 (tutorial), 8 → 11 (sound and feel), 9 → 12 (mobile and Web polish), 10 → 13 (balance). Commit messages and older reports keep the old numbers.

Level art pass (2026-10-18, lead requested): `BackgroundPanel.prefab` groups `main_background_0` with the existing 6×6 switchboard. Its twelve lamps indicate hovered/occupied rows and columns using supplied on/off sprites. This is presentation only; call rules and socket addresses are unchanged. Six caller anchors remain despite the five windows painted into the new background; their layout migration remains pending.

| Step | State | What exists |
|---|---|---|
| 1 | Done (`e6f44e5`) | One call at a time, through Incoming → Taken → Connected → Ended. See below. |
| 1b + 1c | Done (`60c7a88`, `47735c1`) | Connection fee (D32); one people file `PhoneBook.json`, read by the book and the game (D31); the money counter in the header strip of the shop panel; a speech bubble left of the caller card; a "+N¢" popup; the temporary Directory list removed. `GameConfig` references the JSON; a plain `GameContent` class implements `IGameContent`. |
| 2a | Done (`2b00ac7`) | Up to 6 live calls, one card per caller slot. Taking a card swaps (D03). Connecting while a cord is half-patched. `BusyTracker` (D18), `CallbackScheduler` with "Calling again" cards, and a `PacingProfile`-driven spawner. Connected cards stay in their slot with a cord-colour ring and their own earnings. A dev panel (F1). |
| 2b | Done (`4c5ac50`) | Hold and resume (D07): a pulled plug puts the call OnHold (pay and duration paused, card says "On hold" with frozen earnings); the same pair closing again resumes it with no second fee; the plug in another jack or the cord to the tray cuts it off. Ringing halos draw behind all card content. |
| 3 | Done (2026-10-18) | A–Z tabs in the book (D34): `PhoneBookTab` plus `PhoneBookView.ShowLetter`/`HasLetter`, with letters that have no surname greyed out, and arrow keys while the book is open. 36 people on every jack A1–F6 (D35). Restyled cards: bold surnames in the in-hand bubble, on-hold dimming, coloured stamp inks. `ContentValidator` (Content) with `Tools/Switchboard/Validate Content`. Accepted deviations: the tab test lives in `Assets/Tests/Game/Editor/`, the validator menu in `Assets/Scripts/Game/Editor/` (see 4.1), and the validator also checks the surname order and `contactsPerPage`. After the lead's playtest: a press reaction and a placeholder click when a card is taken, typed-out bubble text, and clearer tabs for the spread on show. |
| 4 | Done (2026-10-18) | Emergencies and fines (D36, D37): `type.emergency` (30 s response ring, $2 miss or wrong-patch fine, $1 cut-off fine, 20–40¢ + 5–10¢/s for 10–20 s, at most one at a time, spawn weight 1 against Regular 5, `everyoneCalls`, `recipientTag` `emergency-service`). Police B6, Fire Brigade C2 and Doctor D4 head `PhoneBook.json`. `PenaltyService` with `FineApplied`, the "Missed -$X" stamp and a red fine popup under the money counter. `IdleGuard` (60 s, `PacingProfile.idleTimeoutSeconds`) blocks fined types and their callbacks. A presentation-only `CallCardStyle` per type (badge, accent). Dev panel "Emergency now". Accepted deviations: the countdown ring is cream on a dark track, turning orange-red in the last 10 s (a red ring was invisible on the red halo); the type badge hides once connected; a `type` given on a service is ignored. |
| Scene merge | Done (2026-10-18) | One `Final.unity` built from prefabs (D38): `Game.prefab` and `GameUI.prefab` next to the board; build list `MainMenu` (0), `Final` (1); `Game.unity` removed; `GameInstaller` finds the views by type in its own scene. |
| 5a | Done (2026-10-18) | The hand-edited `Assets/Configs/Economy.json` (`upgrades` only) and `Docs/EconomyFile.md`. Domain `UpgradeService` (with `PurchaseCheck`), `StatSheet`, `ShopShelf`, `GoalProgress`, `Wallet.TrySpend`; the live income multiplier on fee and pay. `EconomyFileLoader` with plain-English checks, shown by `Validate Content`. The 4 shop buttons (D44) carry `UpgradeSlotView` (title, description, price, savings fill, gold frame) and are 204 px wide; menu `Tools/Switchboard/Update Upgrade Shop Buttons`. `ShopView`, a neutral "-$X" spend popup and the "To be continued..." dialog (`GoalPanelView`). Dev panel "+ $100" and "Reset upgrades" (no refund). Accepted deviations: see 5.9 and 4.3. |
| 5b | Done (2026-10-18) | `extra_wires` and `notepad` in `Economy.json` (D23, D40, D43). `StatSheet.Cords` (from `GameConfig.startingCords` = 3), `HasNotepad` and `Changed`; Domain `Notepad` (notes on the `Call`: `CallerNote`, `RecipientNote`). Changes to the cord and book code (D44): `CableInputController.AddCable(Color)` and `MaxCables` = 6, with tray re-spacing; `PhoneBookView.ContactTapped(name, socket)` and `ContactTapsEnabled`, with a transparent row tap area in `PhoneBook.prefab`. `CordBridge.EnsureCords` adds green, violet and ivory cords (`GameConfig.extraCordColors`); `NotepadBookLink` routes book taps; the bubble shows noted jacks. Accepted deviations: see 5.9. |
| 5c | Done (2026-10-18) | `Economy.json` holds `startingCents`, `startingCords`, `pacing` and `calls` (D47), read by `EconomyFileLoader` into `CallTypeRecord`s and `PacingSettings`; the domain is unchanged. `CallerTypeDefinition` is card look only; `PacingProfile` and `Default.asset` are gone. Values unchanged (a real-file test pins them). Accepted deviations: a negative `startingCents` and half-written ranges are also reported; `startingCords` above 6 falls back to 3; `Validate Content` prefixes people messages with "People file: ". |
| Upstream merge | Done (2026-10-18, `a173e75`) | `2fc3810`: persistent wiring (D45) in `OperatorDesk`/`CallService` with `PersistentWiringTests`; "New order!" stamp and `StampExitAnimation`; bubble only on the last-clicked card; `CallAudio` and `BackgroundMusic`; board gold light; `CordBridge.Synchronize` and insertion anchors. `CableInputController` keeps a held cord when a call card is clicked (it now references `CallCardView`). New art, including `caller-04`/`caller-05` (slices fixed to 190×190, lead-approved). |
| 6 (was 7a) | Done (2026-10-18, `d166ffa`) | Board growth (D42, D49). `Economy.json` gains `startingBoard` (3×3) and `startingClientSlots` (3); `board_1`–`board_3` (`extra_clients`) open 4×4, 5×5 and 6×6 with 4, 5 and 6 caller slots. Domain `BoardSize` (`MaxSide` = 6, the physical board) and `OpenBoard`; `StatSheet.Board` and `ClientSlots`; the spawner and callbacks use the open slot count and only people on open jacks. Closed jacks are dimmed and refuse hover, clicks and plugs (`SocketView.IsOpen`/`SetOpen`; a click bounces the held cord with the blocked sound); lamps of closed rows and columns are dimmed; the book shows only open jacks (`PhoneBookView.SetVisibleSockets`); closed caller slots get dark discs. `OpenBoardLink` (Game) pushes the open area on `StatSheet.Changed`. Fire Brigade C2 → E5, Reginald Underhill E5 → C2. `GameConfig.maxWaitingCalls` removed. Dev panel "Open board" and "Emergency: none open". Accepted deviations: see 5.9. |
| Upstream branch | Merged (2026-10-18, `e7acec5`) | The work since `61dae7a`: a cash-register sound and `CallAudio.Bind(calls, earnings)`; fee coins flying to the money counter (`IncomeCoinsView`, `IncomeCoinGraphic`); jacks of a connected call glow green (`SocketGraphic`/`SocketView.SetCallConnected`, driven by `CordBridge`); the **first-order guide** (`FirstOrderGuide`, `FirstOrderGuideSequence`, `SlowGuidePulse`: on the first call the card, the book and the cords pulse in turn, and acting out of order cancels it); bigger socket and lamp art in the prefabs and `Final.unity`; `PhoneBook.json` names written "Surname, First". Merged with 7a: a closed jack's dimmed look wins over green; `MergedSystemsTests` pick a call on open jacks. |
| Upstream branch, part 2 | Merged (2026-10-18, `6226ead`, with `1c337e7`), playtest pending | Hover halos on jacks (`SocketHaloGraphic`: yellow with a cord in hand, red over a used jack, green for a connected call; a closed jack never shows one); page turning by clicking either half of the open book, with a page-turn hand cursor; close on Escape or an outside click; guide visuals (`Resources/GuideSilhouette.shader`, `SoftGuideOutline`, a portrait ring and a book glow); the cash register plays on a call's first connection. The first-name book order was replaced by surname order (D50). |
| 7 | Done (2026-10-18, `8e48320`) | Hand-written calls and call queues (D51, D52, D54). The hand-written `Assets/Configs/Calls.json` (44 placeholder calls; `CallsFileLoader`, `CallsCatalog`). `Economy.json` gains `startPool` and a `stagePool` on each `extra_clients`, starts with 2 caller slots (then 3, 4, 5), and loses `pacing`, `calls` and `notepad`. Domain `CallScript` and `CallQueue` (5.2); `CallRequest` wraps the script; `ConnectionRule` compares the call's own jacks; `PenaltyService` fines only a missed emergency. Deleted: `CallSpawner`, `CallFactory`, `PacingSettings`, `BusyTracker`, `CallbackScheduler`, `IdleGuard`, `Notepad`, `PhoneDirectory`, the call-type interfaces and records, `CallerTypeDefinition` and its two assets (card styles are now two `GameConfig` fields), `NotepadBookLink` and the book's tap chain. Cards show the caller's name on a plate under the pay badge, and the bubble types out the call's own `message`; no "Calling again". Dev panel: no "Call now" or "Emergency now". `PhoneBook.json` and `Final.unity` unchanged; the people loader no longer reads `portrait`, `type`, `rate` or `fee`. Accepted deviations: see 5.2, 5.4 and 6.5. |
| 9a | Done (2026-10-19, `580c610`) | Saves (D25, D55). Domain `SaveData`/`CareerData`, `SaveMigrations`, `ISaveStorage`, `CareerStats`; `GameSession(content, switchboard, save)` restores the balance, bought ids (`UpgradeService.Restore`, silent) and queue position while it builds, and `CaptureSave()` takes a snapshot. Content `SaveFile`: JSON, migrations, keys `operator.save` + `operator.save.backup` (the previous good save). Game `PlayerPrefsStorage` behind `SaveStorage.Create()` (9b's switch point), `Autosave` (purchase, 10 s if changed, focus loss, pause, scene end), `NewGameMenu` in `MainMenu` (New game under Continue while a save exists, with a confirm panel), dev "New game". Touch: `CableInputController`, `VolumeCablePanel` and `MenuHandCursor` read `Pointer.current`; `Menu.IsAnyInput` counts a touch. Accepted deviations: see 5.12. |
| 9b | Done (2026-10-19, `709be9d`); a PNG cursor fix for the double pointer followed in `d79421d` | The Web build (D26). `BrowserStorage` (`.cs` + `.jslib`) keeps saves and the volume patch in `localStorage`, every access in try/catch; `SaveStorage.Create()` picks it in Web players and falls back to PlayerPrefs with one warning. `BrowserPageHidden` saves inside `visibilitychange` (hidden) and `pagehide`. `AudioVolumeSettings` reads and writes through `SaveStorage` (same key). `Tools/Switchboard/Build Web` (`WebBuild`) applies Gzip, Decompression Fallback and the `Operator` template, cleans `Builds/Web` and makes a release build of the build list. The project template `Assets/WebGLTemplates/Operator/` fills the window, with no footer, and the title "Operator!". Since 2026-10-20 Unity's "Made with Unity" splash is off, and the template's loading cover (title and bar, then "Starting...") stays up until `PageReady` in `MainMenu` reports the first drawn menu frame through `GameReady.jslib` (`window.operatorGameReady`), or 8 s after the loader finishes as a fallback. Team guide: `Docs/WebBuild.md`. |
| 8 | Done (2026-10-19, `f82ed29`) | The new phone book (D53). The hand-edited `Assets/Configs/Book.json` (renamed from `PhoneBook.json`, same GUID; 39 placeholder entries, three households sharing a jack) is read once, by Content `BookFileLoader` into a `BookCatalog` of `BookEntry`s, which both `PhoneBookView` and `Validate Content` use. The book lists every entry from the start: services in file order, then names A–Z. Each row shows the name, a smaller "profession, address" line and the jack on the right (heading "Jack"). `GameConfig.bookFile`. Removed: `PhoneBookConfig`/`PhoneBookContact`, the people model (`PeopleFileLoader`, `PeopleFileData`, `PeopleCatalog`, `PersonRecord`, `DirectoryRecord`, `IPerson`, `PersonExtensions`, `IDirectoryEntry`), `SetVisibleSockets` and `OpenBoardLink`'s book part, and the rows' tap image. Accepted deviations: see 5.7. |
| Balance pass | Merged (2026-10-19, `7312e43`, merge `baed5b4`); tests and the pause font fixed in the test and font fix pass | **The files set the numbers** (lead, 2026-10-19): anyone may change `Economy.json`, `Calls.json` and `Book.json` at any time, and the real-file tests check rules, not values. The "Configs wip" balance: a 4×4 start with 3 caller slots and 1 cord, 12 upgrades (5 cords, 2 boards: 5×5 with 4 slots and 6×6 with 6, 4 income steps, the secret), 33 calls, 14 book entries written "First Surname", services "Police station", "Fire station", "Hospital". Code changes (accepted): the **one-call tutorial start** (`CallQueue`'s `tutorial` flag, 5.2); **one live call per caller** (5.2); `startingCords` may be 1 (was 2); `ShopView` pulses the first extra-cord upgrade while it's affordable, until it is bought (`UpgradeSlotView.GuideGraphic`); the bubble label allows rich text (`<b>` in messages); a `police` portrait in `GameConfig`. Tests for these in `CallQueueTests`, `EconomyPoolTests` and `MergedSystemsTests`. The pause panel (`eb29845`) uses static copies of the display fonts (`Boecklins Universe SDF Static`, `Della Respira SDF Static`). |
| Cleanup after tarbaev.d's commits | Done (2026-10-20, `1b6a6af`) | The Dev button follows the pause button in the play area's corner (`DevPanel.FirstButtonPosition`); `CallAudio.incomingGain`; build re-saves restored; `WebBuild` writes back the files a build re-saves (4.3, `Docs/WebBuild.md`). |
| Volume patch | Done (2026-10-20, `6667a4a`) | Free plugging with no jumping jacks, wires over jacks, the last-plugged wire on top, the order saved (5.12 item 8). |
| Board cord stacking | Done (2026-10-20, `662c5e6`) | Wires over every resting jack, the last-moved cord on top (4.3). |
| 10 | Done (2026-10-20, `a418ade`, `1911348`) | The spotlight tutorial (D57): New Game only, mandatory, a Skip button, the guided call never times out; shop hints for the first cord, the mystery, More Profit and More Clients, remembered in the save (`shopHintsDone`). `FirstOrderGuide` + `FirstOrderGuideSequence` + `TutorialSpotlight` (4.3). |
| Web start | Done (2026-10-20, `c19fd9c`) | No Unity splash; the page's loading cover stays until the menu is drawn (9b row). |
| 10b | Done (2026-10-21, `c412194`) | Faster lookup (D58): the call in hand's caller jack blinks (`CallerLights`, `CallerJackLights`, `SocketLampGraphic`), waiting callers stay dark, hovering a ringing card flashes its jack; a book-row click marks its jack and closes the book, rows tint on hover (5.7); tutorial steps for the caller's line and one jack at a time; tray cords fill the first free spot. |
| WebBuild font fix | Done (2026-10-21, `082c5da`) | The dynamic fonts and their Static materials are snapshotted before the build's save and emptied in memory afterwards. |
| 10c | Done (2026-10-21, `845a1b3`) | The quick-connect tip (D59): `QuickConnectTip`, `Economy.json` `quickConnectSeconds`/`quickConnectTipCents` (6.4), paid by `EarningsService` (5.8), a "Quick! +X¢" pill on the card (5.4). The Operator's notebook is on hold. |
| 10d | Done (2026-10-22, `ed0e2ad`) | Static fonts: every text in the two display fonts uses the `… SDF Static` assets, now with the TTFs' kerning pairs; the dynamic `… SDF` assets are unused, so Play and test runs no longer write font files (4.3). `StaticFontTests` guards the references and the glyphs of `Economy.json`, `Book.json` and the tutorial texts. |
| 11c | Built, playtesting (2026-10-22) | Main visual feedback (D60, V1–V6, V13), presentation only: the money reels roll like an odometer and the plate glows when fee coins land; a fine shakes the plaque red and sends coins from the counter to the missed card (`IncomeCoinsView.FlyAway`); a waiting emergency wobbles (harder in its last 10 s, on the same game clock as the ring's pulse) and `EmergencyLampView` flashes a red lamp right of the column numbers; new cards slide in and shake as they ring; a wrong number shakes the card and flashes both jacks red with sparks (`SocketFlashGraphic`); a connection sends a ring out from both jacks. `CallCardView.StampShown` is the hook for 14's stamp thunk. **Fix pass 1 (lead's playtest):**
  - The rings are a stronger green and stay closer to the jack.
  - The decimal point has its own drum: "442.90" under $1,000, whole dollars up to $999,999, then "1.234M"-style, cut rather than rounded (`MoneyCounterView.FillReels`).
  - Coins fly in 1 s, and cards slide in over 0.55 s.
  - A connecting cord wiggles: `CableController.PlayWiggle`, through the new general `CableMotion` and `CableGraphic.SetWave`.
**Fix pass 2:**
  - An EMERGENCY sign under the red lamp, dark when no emergency waits and lit and flashing with the lamp while one does (`EmergencyLampView`).
  - The wrong-number sparks became a short circuit: a white flash, falling streaks, a zigzag arc and dark smoke, on their own `Spark` layer (9).
  - A wrong number jolts the cord with sharp kinks (`CableController.PlayJolt`).
**Fix pass 4:** the emergency lamp matches the board.
  - It uses the number lamps' own bulb art (`bulb-02` unlit, `bulb-01` lit), tinted a warm, muted red.
  - Lit, it glows softly from inside, with no glint, and casts a gentle glow on the frame.
  - "EMERGENCY" is cut into the frame in Boecklins Universe (the numbers' font), with no plate.
  - A glossy glass version (fix pass 3) is kept only on the local branch `step-11c-lamp-glossy-glass` (`e914ff8`).
**Fix pass 5:**
  - The bulb is half size (44 board units, `GameInstaller.emergencyLampSize`; 29.3 gives a third), level with the column numbers.
  - The sign is bigger on the frame's bottom strip: ivory with a near-black edge when unlit, warm red inside the edge when lit, never below 45% brightness. Both reach 4.5:1 contrast.
**Fix pass 6** (lead: the sign's font and the white-to-red switch looked off, and it was too big):
  - The sign is an ivory label strip with a brass rim and a soft shadow (`LabelStripGraphic`), centred under the bulb.
  - Its letters are Della Respira capitals in one red: dark oxblood when quiet, brighter with a red light on the strip when lit (floor 35%). Contrast is at least 4.5:1 in every state.
  - The strip and the text are sized from the bulb by ratios (`GameInstaller.emergencySign`).
**Fix pass 7** (lead: the ivory strip was too bright, like a sticker):
  - The sign is a matte brass plate (`#C6A95E` to `#A88A42`, rim `#6E5A1E`) set into the wood: an inner shadow, a light bottom line and no drop shadow (`LabelStripGraphic`, recessed).
  - Its letters are engraved and dark (`#3A140C` quiet, `#480B0D` lit), with a light lower edge, slightly thickened through one material copy.
  - The alarm shows as a red wash on the plate and a halo round it.
  - Contrast is 4.6–5.0:1.
  - Pulling a plug, hold and cut-off get no cord animation (lead).
  Accepted deviations: emergencies show no "New order!" stamp (V13: the badge and ring already say it); the lamp also lights for an emergency still waiting behind a stamp. |
| Loop redesign | Planned (2026-11-08) | The 2026-10-25 playtest and the lead's decisions of 2026-11-07 (§12, D61–D74): the busy operator is the heart; shifts with a day card; patience and 30 s emergencies; a score with streaks; stakes (burnt cords, complaints, quotas, fired); chores; nerves; the wall; residents as the unit of content with a call director (D77, 2026-11-09); the story between days. Built as Plan §9 steps 13a–13j. The mystery upgrade and the victory scene go (D65). |
| 12 paint box | Done (2026-11-09) | `GameStyle` (D75): one ScriptableObject in the Game layer, at `Assets/Content/GameStyle.asset`, with the StyleGuide's named colours, three font roles (Display, Label, Plain) and the §6 timings. `GameInstaller` passes it to the money counter, the queue and cards, the emergency lamp and the sockets (through `IBoardPalette`, so the board code stays free of the Game layer). Menu titles take it through `TitleInk`. Moved onto it: the Error and Alarm reds, the titles' colour, and Della Respira for stamps and name plates. Fix passes 1 and 2 (2026-11-09): titles back to gold; a wider split of the reds was tried and rejected, so step 12's reds stay; the EMERGENCY badge takes Alarm with cream letters (about 3.2:1, the lead's choice); three board settings read through `IBoardPalette` (the wrong-number ring: outer edge 0.54, strength 0.85; the blocked glow: intensity 1.5). Views read values when they apply them, so Play Mode edits to the asset show at the next event. Fix pass 3: the badge back to step 12, and the jack hover lights per D76 (red only on a live call's jack under a held cord; amber on other open jacks under a held cord and on an unfinished cord's jack under an empty hand), read from the socket's own state. Accepted deviations: `IBoardPalette`, `TitleInk`; `blockedGlowIntensity` keeps its name and now sets the red ring's strength. |

What Step 1 built:
- **Operator desk.** A right pair connects the call. A wrong pair ends it as "Wrong number!". Patching with no call in hand is refused. Pulling a plug cut the call off (replaced by hold in Step 2b).
- **`CordBridge`** connects the game to the cords.
- **Wallet and earnings.** Money is paid live, with a carried fraction so nothing is lost to rounding.
- **Content.** 12 people on 12 jacks, two per row (36 from Step 3, D35).
- **UI.**
  - A ringing card over the first caller slot.
  - A ticket, the money counter and a Directory button in the right-hand column.
  - A temporary directory list.

## 1. The game

A **busy-operator arcade game in a cosy skin** (D61; the redesign of 2026-11-08, §12). The player is a telephone switchboard operator in a small 1930s town and works in **shifts** (D62): punch in, connect everyone who calls, keep the callers patient and the supervisor off your back, punch out, read the day's news, and come back tomorrow for more cords, a bigger board, a new chore or a new face in town. **Money is the day's score.** The game has no ending (D65): the days go on, with a gently rising pace. A **career** can end, though: three missed quotas and the operator is fired, and a new career starts with the wall of frames intact (D68). Progress is saved between browser sessions.

Until step 13 is built, the game plays as Steps 1–11c left it: an endless stream of calls, money that buys upgrades in a shop, and no pressure beyond emergencies. Section 5 describes that build; this section and §12 describe the target and mark the step that builds each part in brackets.

### Core loop: one shift, about three minutes [13b]
1. **Punch in** [13b]. The pneumatic mail drops today's quota, the challenge and the news [13f, 13j].
2. A call rings as a **card**. The caller's own jack blinks on the board; the message says in one line who they want.
3. The player finds the recipient in the **book** and patches the two jacks with a **cord**.
4. **Right connection:** the fee, then per-second pay while connected; the **streak** grows [13d]. A quick connection earns the tip (D59); an emergency connected in its last seconds earns "Just in time!" [13d].
5. **Wrong connection:** a short circuit, the caller hangs up, the streak breaks, a complaint, and the cord is **burnt** for a while [13e]. The **electrician** can be called on the intercom to fix it [13e].
6. **Callers have patience** [13a]: wait too long and they hang up ("Hung up"), come back crankier and complain [13e]. An **emergency** must be connected within about 30 s or it is missed: a fine and a complaint [13a, 13e].
7. **Chores** compete for the hands [13h]: bank the drawer with the register lever, wipe smoke and steam off the glass, open the mail. **Nerves** rise with the chaos and shake the hand; coffee, a cigarette or the flask calm them, each at a price [13i].
8. **Rush hour** before closing: calls come faster and pay double [13d].
9. **Closing time:** punch out, or work **overtime** with crankier callers [13f]. The **day card** shows earned, connected, hung up, missed, complaints, best streak, the rating, the headline and tomorrow's unlock [13b, 13d, 13g, 13j]. Frames and warnings go up on **the wall** [13g].

### Rules
- **Cards.** Several wait at once, one per open caller slot. They can't be dismissed. A regular caller waits for their **patience** [13a], then hangs up; an emergency for its **response time** (about 30 s), then is missed with a fine. A hung-up or missed call comes back later, the caller crankier [13e]. Until 13a, regular callers wait forever.
- **Nobody calls while on a call** [13a]: a call is skipped while its caller is in any live call, as caller or recipient. This fixes "people call while already on a call". A call to a **busy recipient** is allowed, rarely: the card says "Line busy", the caller waits on their patience, and the player may hold the current call to serve them; a hang-up over a busy line is nobody's fault and counts no complaint [13a, 13e].
- **Residents** [13c]: one list of people (`Residents.json`, grown from `Book.json`) that the book shows and the rules read. Calls name people by id and take their jacks from them; everyday calls are assembled by the **call director** from templates; story beats stay hand-written (D77, §12.14).
- **Cards arrive at the top** [13a] (D80): the newest card is the top one and the others shift down, so a new emergency is never missed under the pile.
- **Who is asked for is unmistakable** [13a, 13c] (D80): every message opens with a fixed lead-in naming the person wanted, "Connect me to <b>Name</b>.", before any flavour; from 13c the card composes that line from the resident's name.
- **One click frees a finished cord** [13a] (D79): when a call is over, a click on either of its plugs pulls both and puts the cord in hand, ready for the next jack or the tray. A plug of a live call still puts the call on hold (D07).
- **The book is a clipboard** [13k] (D78): a vertical clipboard docked at the right that never covers a jack; the art exists.
- **Tutorial** [13a] (D81): one step is added now, freeing a finished cord; the rest of the tutorial is reworked after step 13, once its gameplay has been played.
- **Earnings** = (one-off connection fee + seconds connected × rate) × the day's bonuses: the streak bonus [13d], rush hour [13d] and overtime [13f]. The quick-connect tip (D59) stays. The shop's income multipliers are dropped (D66, confirm). No money is lost to rounding.
- **Score** [13d]. The money earned in a shift is the day's score, rated from "Rough day" to "Flawless". The best shift is remembered and shown in the menu. One **daily challenge** a day ("No wrong numbers", "Every emergency", "Streak of 8").
- **Stakes** [13e, 13f]. A wrong number burns the cord. Hang-ups, wrong numbers and misses are **complaints**: three bring the **supervisor's call**, five end the day early ("Sent home"). Each day has a **quota**; a missed quota is a **written warning** on the wall, cleared by beating the next quota; three warnings and the operator is **fired**, which ends the career with a career card. Money never goes below zero.
- **Cords** are limited. Each connected call uses one, and a burnt cord is out until the electrician fixes it. Cords may cross; crossing is purely visual.
- **The directory book** can be opened at any time and never pauses the game (D20).
- **Unlocks** come by the **day schedule** [13g], not from a shop: cords, board growth with new residents, chores, desk items. "Tomorrow: ..." is always in view. This replaces the bought upgrades of D40; the shop panel's fate is D66 (confirm).
- **The wall** [13g] shows the clock, the calendar, the frames ("Operator of the week", "Operator of the month", feats), the warnings and the desk items.
- **Narrative lives between days** [13j]: one-line calls during the shift, the story on the day card (headlines, a new face). Story calls play once, in order, written from `Docs/Story.md`. Arcs are chains of hand-written beats; the day's headline is the beat that resolved last, or the arc's holding line (§12.11).
- **Calls** are hand-written in `Assets/Configs/Calls.json` (D51). The day schedule, quotas and pace live in `Economy.json` (D52, D66).

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
- **One team, one codebase (D48).** Any file may be changed when a step needs it, including the cord, board, phone book, shop and menu code. Prefer extending an existing component over building a second one.

## 3. One shared project and the cord code

The whole project is one shared work (D48, lead 2026-10-18; restated 2026-10-22). Nothing in it belongs to anyone: there are no owners, no read-only folders and no approvals, and anyone may edit any part. A step changes whatever files it needs, and its brief lists them. The docs name no one as the owner or maker of a part. Teammates push to `main`, so pre-flight keeps merging their changes. **The config files too (lead, 2026-10-21):** anyone (including a step) edits `Economy.json`, `Calls.json` and `Book.json` and their guides (`Docs/EconomyFile.md`, `Docs/BookFile.md`), and nobody needs asking first. They stay hand-edited, so keep them readable with one entry per line and plain validator messages.

**Members `CordBridge` relies on.** Anyone changing these updates `CordBridge` in the same change.
- `CableController`:
  - `Phase`, `FirstSocket`, `SecondSocket`, `IsInTray`, `CableColor`
  - `TryRemove(SocketView)`, `PlayBlockedFeedback()`
  - `InsertionRevision`, `LastInsertionAnchor`, `LastInsertionWasManual` (`2fc3810`; read by `CordBridge` for the wrong-number anchor, D45)
- `CableInputController` (5b): `AddCable(Color)`, `MaxCables`; (9a) `ProcessPointer(position, pressed, hover)`, `Suspended` (pause; the press in the resuming frame is ignored). A click whose target has a `Selectable` parent keeps a held cord.
- `PhoneBookView` (9a pass 2): `InputEnabled`, `ClosesOnEscape`. `CallAudio` (9a pass 2): `SetPaused`, `IsPaused`, `PausedChanged`.
- `PhoneBookView`: `PageCount`, `ContactCount`, `ContactAt` (a `BookEntry` since 8); `OpenChanged`, `ClosedBookGraphic`. Step 6's `SetVisibleSockets` was removed in Step 8 (D53), the Notepad's `ContactTapped` and `ContactTapsEnabled` in Step 7 (D54).
- Hooks from the merge `6226ead`: `PhoneBookView.PageTurnDirectionAt(Vector2, Camera)` (used by `CableInputController`), `CableCursorMode.PageTurn`, `HandCursorGraphic.PageTurnDirection`, `PhoneBookContact.IsEmergencyService` (now `BookEntry.IsEmergencyService`, 8), `SocketHaloGraphic`, `SlowGuidePulse.UsePortraitHalo()`/`UseSilhouetteHalo()`, `SoftGuideOutline`; `CallAudio.Bind(CallService)` no longer takes `EarningsService`.
- Hooks from the merge `e7acec5`: `CableController.PickedUp`, `SocketView.SetCallConnected` / `SocketGraphic.SetCallConnected`, `CordBridge.CableAdded`, `CallQueueView.OrderShown` / `CardOpened`, `CallAudio.ConnectionSucceeded`. `FirstOrderGuide` uses them.
- `CablePhase` values: `Ready` (in the tray), `Held` (picked up, nothing plugged in), `OneEndConnected`, `Connected`.
- `SocketView`: `Address` (e.g. `"C4"`), `Row`, `Column`, `PlugAnchor`, `IsOccupied`; (6) `IsOpen`, `SetOpen(bool)`.
- The cord input model: click a cord in the tray to pick it up, then click a jack to plug in. Clicking a plugged jack pulls that plug out. If the player pulls one plug of a connected cord and then clicks empty space, the plug snaps back to its jack ("restore", `ReconnectSocket`).
- The board's objects in `Final.unity` are found **by component type, never by name**: `CableController`, `SocketView`, `CableInputController` and, from Step 5a, the shop's `UpgradeSlotView`s (D41, D44).
- Two exceptions are found by name, because they have no component of their own. Renaming them means updating `GameInstaller`:
  - The caller slots: the children of the `VerticalLayoutGroup` under `CallerQueuePanel`.
  - The money slot: `UpgradeShopPanel/HeaderSlot`, a plaque added in Step 1c.

## 4. Architecture

### 4.1 Layers, assemblies and folders

| Layer | Folder | Assembly | Contents |
|---|---|---|---|
| Domain | `Assets/Scripts/Domain/` | `Switchboard.Domain`, `noEngineReferences: true` | All rules: calls, the call queue, the operator desk, the economy, penalties, stats, upgrades, progression, and the save data model. |
| Content | `Assets/Scripts/Content/` | `Switchboard.Content`, references Domain | ScriptableObject definitions. They implement small read-only Domain interfaces. |
| Content editor tools | `Assets/Scripts/Game/Editor/` | **no asmdef** (Assembly-CSharp-Editor) | The `Tools/Switchboard/Validate Content` menu (3). The validator itself, `ContentValidator`, is in `Switchboard.Content`. |
| Game | `Assets/Scripts/Game/` | **no asmdef** (Assembly-CSharp) | Composition root, game loop, `CordBridge`, views, input glue, audio, storage, dev tools. |
| Tests | `Assets/Tests/Domain/` | `Switchboard.Domain.Tests`, EditMode, references Domain and Content | Unit tests for Domain, plus content validation. |
| Game tests | `Assets/Tests/Game/Editor/` | **no asmdef** (Assembly-CSharp-Editor) | EditMode tests that need Assembly-CSharp types, such as `PhoneBookView` (3). |
| Content assets | `Assets/Content/` | n/a | `.asset` files (`GameConfig`, `GameStyle`), plus audio in `Assets/Content/Audio/` (3). |
| Game prefabs | `Assets/Prefabs/Game/` | n/a | `GameUI.prefab` and the other game prefabs. |
| Scenes | `Assets/Scenes/Final.unity` (D38), `Assets/Scenes/MainMenu.unity` | n/a | `Final` is built from prefab instances, including `Game.prefab` and `GameUI.prefab`. |
| Cord, board and UI code | `Assets/Scripts/UI/`, `Assets/Scripts/Cables/Core/` | Assembly-CSharp, `Switchboard.CableLogic` | Cord interaction and graphics, the phone book, the shop panel, the menu. |

- **Why Game has no asmdef:** an asmdef assembly can't reference Assembly-CSharp, and the cord code lives there. Game has to see both the Domain and Content assemblies and the cord code.
- **Dependency direction:** Domain ← Content ← Game. Domain references nothing, so it can't use `UnityEngine.Random`, `Time` or `Debug`.
  - Time comes from `Tick(dt)`.
  - Randomness comes from `IRandom`.
  - Logging, if needed, goes through an interface.
- **Namespaces** match the assemblies: `Switchboard.Domain`, `Switchboard.Content`, `Switchboard.Game`, `Switchboard.Domain.Tests`.

### 4.2 Scenes
- **`MainMenu.unity` (build index 0).** Start loads the next build index.
- **`Final.unity` (build index 1; D38, formerly `Level.unity`, same GUID).** The one game scene, made of prefab instances:
  - `Main Camera` (prefab), `SwitchboardCanvas` (loose; holds the BackgroundPanel, CallerQueuePanel, CableTray, UpgradeShopPanel, PhoneBook and Cable prefabs) and `EventSystem` (loose). The loose ones may become prefabs.
  - `Game.prefab` (`GameInstaller`, `GameLoopRunner`, `CordBridge`) and `GameUI.prefab`, at the root, with no overrides.
  - Prefer changing prefabs over scene overrides, so teammates' scene merges stay small. `GameInstaller` finds the views and the board's objects by type in its own scene at start.
  - `Game.unity` and the additive loading were removed (D38).
- **No `Boot.unity` (Step 9).** `MainMenu` is the start screen. It has no press-start panel (9a, 9b): the first tap on a menu button unlocks browser audio.
- **`SampleScene.unity`** is not in the build list.
- **The game has one screen,** in one scene (D38).

### 4.3 UI layering
- The game UI lives in `Assets/Prefabs/Game/GameUI.prefab`: a Screen Space Overlay canvas with sorting order 10. Its CanvasScaler matches the board's `SwitchboardCanvas` (Scale With Screen Size, 1920×1080, Expand), so positions line up.
- **The right-hand column** (right of the board, about x 620–960 in the 1920×1080 reference with the canvas centre at 0) now holds the upgrade shop panel. The money counter fills the `HeaderSlot` plaque at the top of the shop panel (Step 1c). It follows the plaque at runtime and auto-sizes from 28 to 64 pt, so "$99,999.99" fits. Anything else (ticket, live calls) must go in free space; check against the board's panels before placing it.
- **Text glyphs.** Use only characters that exist in the static LiberationSans SDF atlas. "¢" and "—" are there; "→" is not. A missing glyph makes TextMeshPro write it into the fallback font asset at runtime, which dirties a file under `Assets/TextMesh Pro/`. **The display fonts (10d):** every text in Della Respira or Boecklins Universe (menu, credits, pause menu, tutorial, shop labels, the book's Tab label, the victory screen) uses the Static copies (`… SDF Static`), which hold printable ASCII only (no "¢", "—" or "×") plus the TTFs' kerning. The dynamic `… SDF` assets are unused, so Play Mode never writes into a font file; `StaticFontTests` checks the references and that `Economy.json`, `Book.json` and the tutorial texts fit the Static atlases. The book's rows draw with the TTFs through legacy `Text`, which never writes into a file.
- **Turn off `raycastTarget` on anything that isn't meant to be clicked.** The cord input takes the topmost raycast hit across all canvases, so a stray raycast target over the board or tray blocks cords. Modal overlays (the open book, the shop) block clicks on purpose.
- Call cards line up with the caller panel's slots by reading the slots' positions at runtime.
- **Board cords stack like the volume patch (lead, 2026-10-20; matches 5.12 item 8).** A jack sticks out of the board, so every resting (plugged-in) jack lies under every wire, the cord's own wire included: it ends at its jack's tail tip. Between wires, the cord moved last lies on top; picking a cord up counts as moving it, even if it goes back to the same socket. A held plug stays on the Cursor layer (20, above the book), and the carried cord's wire is the top wire. Wires and plugs take no clicks, so a jack under another cord's wire is still picked up by clicking it. How (`SwitchboardUISorting`): resting plugs draw in `SwitchboardCanvas` at order 0 (`Cable`); each wire has its own nested canvas at `Wire` (1) + its rank, oldest first (1–6 for six cords, below the book's 10 and `GameUI`'s 10). `CableInputController` keeps the order: the cord it selects goes to the top (last sibling under `CableInteraction`) only when the top cord changes, and the ranks are re-applied only then (also after a bought cord is added). Nothing else in `SwitchboardCanvas` draws after the cords (`CableInteraction` is `WorkArea`'s last child), so the wire layers cover nothing that used to sit above them. Not saved: a loaded game starts with the cords in the tray. The wrong-number short circuit (11c) draws on its own layer `Spark` (9): over every wire and plug, under the book.
- **Caller-jack lamps (10b, D58).** `SocketLampGraphic` is a runtime child of each socket on the board canvas (order 0), so it sits under every plug, wire, the book and the spotlight, and takes no clicks. Only the call in hand's caller jack blinks (alpha 0.06–0.55, 0.5 s); waiting callers stay dark; a jack marked from the book glows steadily; hovering a ringing card flashes its caller's jack once (mouse only, through `CableInputController.HoverTarget`). The constants sit at the top of `SocketLampGraphic`.
- **The tutorial spotlight (10, D57)** is `GameUI/Tutorial/TutorialSpotlight` with its own canvas at `SwitchboardUISorting.Tutorial` (14): above the book and GameUI (10), below the Goal dialog (15), the pause menu (16) and the hand (20). It dims the whole screen, side bars included, with soft holes, a hint and an arrow per step. The dark layer (`SpotlightGraphic`) is a raycast target outside the holes and carries `IPointerBlocker`, which `CableInputController` treats as "do nothing", so only the lit target, Skip, pause and Esc respond. Arrows are placed from where the hint and holes are going, so they never flip while sliding. `PhoneBookView.OpenLocked` keeps the book shut outside its steps.
- **The hand (lead, 2026-10-19):** the game draws its own hand (the `HandCursorGraphic` mesh), so it scales with the game picture: window, full screen, the editor's zoom and Retina. PNG system-pointer hands (`d79421d`; `Assets/Sprites/Cursors`, `Tools/Art`) were tried and set aside: a system pointer keeps one pixel size, so it looked too big in small windows. The system pointer next to the drawn hand is shown and hidden only through `SystemPointer`, only on a change. In browsers it is hidden with a transparent 1×1 image, not `cursor: none`.
- **The pause menu** (9a pass 2, D56) is the `Pause` object in `GameUI.prefab` with its own canvas at sorting order 16: above the book (10) and the Goal dialog (15), below the drawn hand (20). Its 72×72 button sits in the top-right corner of the 1920×1080 play area, 6 px in from it (tarbaev.d, 2026-10-20: centre-anchored at (954, 534)), so on phones wider than 16:9 it stays on the art instead of drifting into the side bars. Before that it was anchored to the screen's corner (lead, round 3, built in 9a pass 3; first built at 88×88 above the board's top-left corner). 88 px doesn't fit there at 16:9: the shop panel's art starts 86 px from the top. The dev panel's IMGUI "Dev" button follows it at every aspect ratio (`DevPanel.FirstButtonPosition`, cleanup 2026-10-20): just left of it, opening as a row going left along the play area's top edge. The main menu's Quit shows only in Windows and Mac builds and the editor, not on Web (`NewGameMenu.QuitAvailable`). Placeholder look: LiberationSans, flat green buttons.
- **Popups by the money counter** (5a): the fine popup sits under the counter; the neutral "-$X" spend popup sits to its left, over the board's frame (briefly covering the "6" column label, never a jack), so the two never collide.
- **The "To be continued..." dialog** (5a) is the `Goal` object in `GameUI.prefab` with its own canvas at sorting order 15: above the open book (10), below the hand cursor and held plugs (20), so the pointer stays visible. Its backdrop blocks clicks while it is open (a deliberate modal); it is inactive otherwise.

### 4.4 Composition root and tick order
- **`GameSession`** (Domain, plain C#) creates every domain service in dependency order and owns the tick order. Tests and the balance simulator build it without Unity.
- **`GameInstaller`** (Game, `[DefaultExecutionOrder(-100)]`) is the only entry point. It:
  1. loads content
  2. loads the save (9a; only in Play Mode, so EditMode tests never touch the real save)
  3. finds the views and the board in its own scene, by type
  4. builds `CordBridge` and `GameSession`
  5. calls `Bind(...)` on the views
- **`GameLoopRunner`** calls `session.Tick(Time.deltaTime)` from `Update`.

```csharp
public void Tick(double dt)
{
    clock.Advance(dt);
    earnings.Tick(dt);   // pay connected calls for min(dt, time left), so the last partial second is paid
    calls.Tick(dt);      // finish calls whose duration ran out; miss expired emergencies (Step 4)
    queue.Tick();        // every free caller slot takes the next queued call (Step 7, D52)
    goal.Tick();         // raise GoalAffordable once, after this tick's money has moved (Step 5a)
}
```
A call that ends gives its slot to the next queued call in the same tick. The idle guard, callbacks and the spawner were removed in Step 7 (D51).

### 4.5 CordBridge: how the rules see the cords
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
- **Adding cords (5b).** `EnsureCords(total)` calls `CableInputController.AddCable(color)` until the count is reached. It appends the new cable to its list, and records the cable's current state so the next `LateUpdate` reports nothing for it. New cords continue the index order (3, 4, 5). The spawner reads `CordCount` live.
- **Touch input** (D28, 9a): the cord controller reads `Pointer.current`; a tap is a click, and on a touchscreen there's no hand or hover while no finger is down. `ProcessPointer(position, pressed, hover)` is the touch-aware overload; the two-argument call is unchanged.

## 5. Systems
The step that introduces each part is shown in brackets.

> **Steps 7 (D51, D52, D54) and 8 (D53) are built.** Hand-written calls and call queues replaced random spawning, call types, callbacks, the idle guard and the Notepad; 5.1–5.6 describe the new build. `Book.json` replaced `PhoneBook.json` in Step 8; 5.7 describes the new book.

### 5.1 Calls and their lifecycle [1, extended in 2 and 4]
- **Classes:**
  - `Call`: id, request, state, end reason, seconds left, cents earned, bound cord.
  - `CallRequest`: wraps the call's `CallScript` from `Calls.json` (7, D51): id, name, icon, message, `From` and `To` jacks, duration, fee, rate, and for an emergency its response time and penalty.
  - `CallState`, `CallEndReason`, `CallService`.
- **`CallService`** owns the live calls in arrival order and the timers. It knows nothing about cords, money or UI. Since D45, several calls can be accepted (Taken or Connecting) at once; `InHand` is only the most recently accepted one, kept for compatibility and never used to judge circuits.
- **Events:** `CallAdded`, `CallStateChanged(call, from, to)`, `CallEnded(call, reason)`.
- **Implementation:** an enum plus guarded methods (`Take`, `BeginConnecting`, `CancelConnecting`, `Connect`, `Hold`, `Resume`, `End(reason)`). Illegal transitions return `false`, and a table-driven test covers the whole matrix.
  - `OperatorDesk` drives `BeginConnecting` and `CancelConnecting` for every accepted call: it is Connecting exactly while a free half-patched cord has its one plug in that call's caller's or recipient's jack (D45).
  - The diagram below predates D45: the "swap" arrow from Taken back to Incoming no longer exists. Taking a card accepts it, and the other accepted calls stay as they are. It also predates Step 7: calls come only from the queue (5.2), and a hung-up call schedules no callback (D51).
- **Enums:**
  - `CallState { Incoming = 0, Taken = 1, Connecting = 2, Connected = 3, OnHold = 4, Ended = 5 }`
  - `CallEndReason { None = 0, Finished = 1, WrongConnection = 2, DisconnectedEarly = 3, Missed = 4, Cleared = 5 }`
  - `Cleared` is used only by the dev panel's "clear calls". It pays nothing, fines nothing and never calls back, and the card leaves without a stamp.
- There are no callbacks since Step 7 (D51). An ended call's script plays again only when the queue comes round to it, as a new `Call` with a new id.

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
| (new) | The queue fills a free caller slot (5.2) | Incoming | The card appears. An emergency's response timer starts. |
| Incoming | Player takes the card | Taken | The bubble types out the call's message. Other accepted calls stay as they are (D45). |
| Taken | A cord has exactly one plug in (just plugged, or already in when taken) | Connecting | |
| Connecting | No cord is half-patched any more | Taken | |
| Taken or Connecting | A circuit closes on the right pair (D05: in either order) | Connected | The cord is bound to the call. The duration timer starts and the response timer stops. |
| Taken or Connecting | A manual insertion closes a wrong pair against the call's anchored jack (D45) | Ended (WrongConnection) | The cord stays plugged (D45). No fine and no callback (D51); the next queued call takes the slot. |
| (none in hand) | A circuit closes | n/a | `Refuse`: the cord bounces back with the blocked sound (D04). |
| Connected | The duration runs out | Ended (Finished) | The cord stays plugged (D45). The card shows a "Paid" stamp. |
| Connected | A plug of its cord is pulled | OnHold (D07) | Payment and the duration timer pause. |
| OnHold | The plug goes back into the same jack | Connected | Payment resumes. |
| OnHold | The plug goes into another jack, or the cord goes back to the tray | Ended (DisconnectedEarly) | No fine (D51). If the plug went into another jack, the new circuit is judged as a fresh circuit. |
| Incoming, Taken or Connecting | An emergency's response timer runs out [4] | Ended (Missed) | The call's `penaltyCents` fine applies, never multiplied (D37, D51). |

- **How hold is implemented (2b).**
  - `OperatorDesk.OnCircuitOpened` on a bound cord calls `CallService.Hold` and remembers the cord's jack pair in a private `HeldCircuit` list. The cord is **not** marked half-patched, so its remaining plug doesn't put the call in hand into Connecting.
  - `OnCircuitClosed` on an on-hold cord: the same pair, in either order, resumes the call. This covers both the cord code's restore (click empty space) and clicking the pulled jack directly. Pay and the duration timer run only for Connected calls, and the `ConnectionFeePaid` flag stops a second fee.
  - Any other pair: the desk first unbinds the cord from the on-hold call (so ending it doesn't send the cord to the tray), ends it as `DisconnectedEarly`, then judges the new circuit as usual (connect, wrong number, or refuse).
  - The cord returning to the tray, including by pulling its second plug, ends the call as `DisconnectedEarly`.
  - Known: a call can earn for at most one frame between the click that pulls a plug and the bridge's report, because `GameLoopRunner.Update` may run before the cord input's `Update`. It is a fraction of a cent; accepted.

### 5.2 Call queue [7] (D51, D52)
Random spawning (2a–6) was replaced in Step 7: `CallSpawner`, `CallFactory`, `BusyTracker`, `PacingSettings` and the idle guard are deleted.
- **Calls.** `CallScript` (Domain) is one hand-written call from `Calls.json` (6.5), loaded by `CallsFileLoader` into `CallsCatalog` (Content).
- **`CallQueue`** (Domain) knows the current pool and its position.
  - **Pool.** `StatSheet.CallPool`: the `stagePool` of the last bought `extra_clients`, or `startPool`. When it changes (a purchase, or dev "Reset upgrades"), the queue restarts at the pool's first call. Calls already on cards finish normally.
  - **Fill.** `Tick` fills every free open slot (`StatSheet.ClientSlots`) at once, in pool order, and loops. There is no spawn timer. Every live call holds a slot, connected and on-hold ones included.
  - **No duplicates.** A call still on a card is skipped, and so is a call whose caller already has a live call (names compared trimmed and ignoring case; `7312e43`). It comes round again later. If every call in the pool is skipped, the free slot stays empty until one ends.
  - **Busy lines.** A call whose jack is in use waits on its card; nothing is skipped for a busy line (a plug can't go into an occupied jack anyway). **13a (D63) changes this:** a call is skipped while its caller is in any live call, as caller or recipient (playtesters read a recipient who rings while on the line as a bug). A call to a busy recipient stays allowed but rare, waits on its card as "Line busy", and a hang-up over a busy line counts no complaint (§12.4).
  - Success, a wrong number or a miss doesn't change the order.
  - Dev "Clear calls" lets the queue carry on; dev "Reset upgrades" sends it back to `startPool`'s first call.
  - No saves yet: a reload starts at `startPool`'s first call (Step 9 decides what is saved).
- **Opening calls.** The first call of `startPool` is the one `FirstOrderGuide` guides with the spotlight (Step 10, D57), on a new game only.
  - **One-call tutorial start** (`7312e43`, accepted). A new game deals only `startPool`'s first call. The other slots fill once that call's connection fee is paid, or once it ends for any reason, and the tutorial call never returns early. `GameSession(content, switchboard, save, tutorial)` takes the flag; `GameInstaller` always passes true, and `GameSession` uses it only when there is no save, so **Continue** fills every slot at once. A new game that hasn't saved yet (`HasProgress` false) starts with the one call again after a reload.
- **As built (7, accepted deviations):**
  - A finished card keeps its place on screen while its stamp shows (about 1.7 s). The next call is already in the slot in the rules, and its card appears after the stamp, so an emergency's countdown can run about 2 s before its card shows.
  - Pool problems leave out the call, not the upgrade. Unknown or broken calls, and calls outside their stage's board, are dropped from that queue with a message. An `extra_clients` entry without a `stagePool` stays buyable and keeps the current queue.

### 5.3 ~~Callbacks~~ [2, removed in 7]
Removed by D51 with `CallbackScheduler` and the "Calling again" badge. A wrong number or a miss doesn't bring the caller back; the call plays again only when the queue comes round to it (5.2).

### 5.4 Call cards [1 minimal, 2 queue, 3 full]
- A card is a view of one `Call`. There is no separate card model.
- **Classes** (Game layer):
  - `CallQueueView`: one card per live call (waiting, in hand, connected), in arrival order, laid over the caller panel's 6 slots.
    - When a card leaves after its stamp, the cards below slide up.
    - If a new call needs a slot while a stamp is still showing, the oldest stamp is cut short.
    - ~~The ringing halo (`RingingPulse`, 2b)~~ is gone; the call in hand's caller jack lamp shows its line on the board (10b, D58).
  - ~~`LiveCallsView`~~: superseded in 2a. The right-hand column is the shop, so connected calls keep their card in their slot.
  - `CallCardView`: pooled.
- **What the card shows in each state:**
  - Waiting: portrait, the pay (e.g. "10¢ + 5¢/s"), the caller's name on a plate under the pay badge (7), and for an emergency the EMERGENCY badge, its accent and the countdown ring.
  - In hand: a speech bubble next to the portrait types out the call's own `message` from `Calls.json` (7, D51), letter by letter (`charactersPerSecond`, 40 by default). Only the card last clicked shows its bubble (D45). Until Step 7 the bubble read "Ada Moss" / "wants Basil Thorne" with bold surnames and the Notepad's jacks. "On the line" and "On hold" type out too.
  - Connected: the cord's colour (a ring around the portrait, read through `CordBridge.ColorOf`), the cents earned so far (a top badge), and an "On the line" bubble. Waiting cards show no bubble. On the first connection a "+fee" popup rises from the card, and a quick connect (10c, D59) adds a green "Quick! +X¢" pill (`TipPopup`) under it; both take no clicks.
  - On hold: the cord ring and the earnings badge stay, the earnings frozen, and the bubble reads "On hold" (2b). From Step 3 the ring and earnings are dimmed.
  - Ended: a stamp, such as "Paid $1.20", "Wrong number!", "Cut off" or "Missed −$5". From Step 3 each stamp has its own ink: green for paid, red for wrong number, brown for cut off.

  The duration is never shown.
- **Interaction:** clicking or tapping a waiting card takes it. A successful take plays a 0.2 s squash and flash on the card and a soft click, a placeholder `card-take-click.wav` played on the Sfx channel (3). No UI path removes a card; only the rules end calls.
- **Emergencies** get a badge, a label and the ring, not colour alone.
- **As built (7, accepted deviations):** the name plate sits under the pay badge and reaches into the gap between slots, because there is no free space under the portrait. Upper cards draw over lower ones so the plate stays readable, and it can cover the top few pixels of an EMERGENCY badge on the card below. The "Calling again" badge is gone (D51).

### 5.5 Penalties [4]
`PenaltyService` listens for `CallEnded` and fines only a `Missed` emergency, by that call's `penaltyCents` (7, D51). A wrong number or a cut-off is never fined; Step 4's wrong-patch and cut-off fines are gone. Fines are never multiplied (D37).

`Wallet.ApplyFine` returns the amount actually taken (the balance floors at 0). Then `FineApplied(call, reason, requested, taken)` is raised for the popup.

### 5.6 Operator desk, switchboard port and connection rule [1]
- **`OperatorDesk`** handles the player's verb `TryTake(callId)` and the cord observations reported by `CordBridge`. It is the only class that coordinates calls with cords. Since D45 (persistent wiring, `2fc3810`, adopted by the lead 2026-10-18):
  - It remembers every closed circuit on the board, whether or not a call uses it.
  - A closed circuit connects **any** accepted call it is correct for, whichever card the player last opened.
  - Taking a card connects it at once if a matching circuit is already plugged in.
  - A wrong number is judged only on a manual insertion: when the second plug goes in against an end that was already in place (the "anchor", from `CableController.LastInsertionAnchor`), and that anchor is a jack of an accepted call. Any other wiring is free.
  - Cords are never sent back to the tray when a call ends; a wrong-number circuit also stays plugged. Only the dev panel's "Clear calls" returns all cords (`ReturnAllCords`).
  - Hold and resume (D07) are unchanged.
- **`ConnectionRule`.** The two plugged jacks must equal the call's own `from` and `to` jacks from `Calls.json`, in either order (D05, D51). Another rule, for example long distance through a trunk jack, would be a new call field later.
- **`ISwitchboard`** (the port) has `CordCount`, `ReturnToTray(CordId)` and `Refuse(CordId)`. `CordBridge` implements it in the game, and a fake implements it in tests.

### 5.7 Phone book [1 list, 3 book, 8 Book.json]
- **Data (8, D53).** `Assets/Configs/Book.json` (6.6) is the book's only source. The rules never read it: calls carry their own jacks (D51). `Validate Content` checks that every jack a call uses has at least one entry in the book. **13c (D77) changes this:** the file grows into `Residents.json`, the one list of people that the book shows and the rules read; calls name people by id and take their jacks from them (§12.14).
- **Loading.** Content's `BookFileLoader.Load(json, boardJacks)` parses, checks and sorts the file once, into a `BookCatalog` (`ContactsPerPage`, `Entries`, `Errors`) of `BookEntry`s (`Name`, `Profession`, `Address`, `IsEmergencyService`, `Jack`). `PhoneBookView` and `GameConfig.Validate()` both use it, so the book and the validator can't disagree. An entry with a problem is left out with a plain message, and the rest still shows; a missing or unreadable file gives an empty book with its message on page 1. `GameInstaller` logs the "Book file: …" messages once at start.
- **Order (D53, replacing D50's file order).** The book sorts itself: services first, in file order; then every other entry by `name`, ordinal and ignoring case (the same on every platform), with ties kept in file order. Names are written "Surname, First" with normal capitals (lead, 2026-10-21; D53 had "SURNAME, First", and `7312e43` briefly "First Surname"), titles after the first name in brackets ("Sharma, Ravi (Dr)", "Gnome, Mr"), so the book is in surname order, with the emergency services first at the top of the left page.
- **Everyone from the start.** The book lists every entry whatever the board size; Step 6's open-board filter (`SetVisibleSockets`) is gone. The board itself still grows.
- **Book view [3].**
  - The closed book on the desk (`Sprites/book/table_book.png`) opens an overlay drawn with the `cover`, `page` and `back` sprites.
  - Rows are generated from data, `contactsPerPage` per page, and pooled. Each row has the name, a smaller details line ("Seamstress, Oak St. 4", 0.75 of the name's size) and the jack as the right-hand column, under the headings "Name" and "Jack".
  - Letter lookup: `HasLetter`/`ShowLetter` go by the first letter of `name`, services included. The A–Z tabs (3, D34) are **hidden** (lead, 2026-10-18): `OpenBook/LetterTabs` is inactive in `PhoneBook.prefab`, with the code and objects kept.
  - Pages turn by clicking either half of the open book or with the arrow keys (3). Swipe waits for `Pointer.current` in the cord input (Step 9).
  - Opening the book never pauses the game (D20).
  - **Row clicks (10b, D58).** A click on a row marks that row's jack on the board with a steady glow and closes the book in the same click; one mark at a time, cleared by a plug in that jack, another row or another call. A row whose jack is off the open board does nothing. Rows are found by position (`PhoneBookView.RowAt`, `JackPicked`, `CanPickJack`), with no raycast target, and tint on hover (`HoverAt`, `PhoneBookContactRow.HoverTint`). The rules still never read the book.
- **As built (8), accepted deviations (lead's playtest, 2026-10-19):**
  1. The two-line row: four columns don't fit the 326 px page ("PENNINGTON, Edgar" alone is about 270 px at size 22).
  2. `PhoneBookConfig` and `PhoneBookContact` were deleted; the view uses `BookEntry` directly.
  3. Duplicate names also ignore extra spaces; the first entry stays and later ones are left out.
  4. An old-format file (`contacts`, `socket`) gives an empty book with a rename hint; it isn't read in a compatibility mode.
  5. A `contactsPerPage` outside 1–12 is reported, and the game uses 6.
  6. In `PhoneBook.prefab` the row label `Socket` became `Jack`, `SocketHeading` became `JackHeading`, a `Details` label was added, and `EmptyMessage` grew to fit a file message. Nothing looked these names up.

### 5.8 Economy [1]
- **`Wallet`**:
  - `long Cents` plus a `double` fraction kept in [0, 1). Only whole cents move into the balance.
  - The fraction is carried forward, and saved [9], so nothing is lost to rounding.
  - Never use `float` for money: at $100,000, a float can't register the fraction of a cent a call earns each frame.
  - Floating-point noise: when the remainder is within 1e-9 ¢ of a whole cent, that cent is credited.
- **`EarningsService`** pays each connected call `min(dt, secondsLeft) × centsPerSecond × multiplier` every tick, so a finished call earns exactly duration × rate × multiplier. The multiplier is read live from `StatSheet.IncomeMultiplier`, so an upgrade bought mid-call applies at once (D12) [5a].
- **Quick-connect tip (10c, D59).** `Call.SecondsInHand` counts game seconds while a call is taken or connecting (pause stops it). On a call's first connection, right after the fee, `EarningsService` pays `QuickConnectTip.Cents × multiplier` if `SecondsInHand ≤ QuickConnectTip.Seconds`, and raises `QuickConnectTipPaid`. Never on a resume after hold or a wrong number; a call whose route was already plugged connects at 0 s and earns it. It counts in the call's earnings, the wallet and the career total.
- **Display:** dollars and cents, e.g. `$12.34`, formatted with the invariant culture. `MoneyFormat` lives in Domain so it can be unit-tested. It formats `$1,234.56` and rates such as `5¢/s`.
- **Cheap UI updates:**
  - Views poll the wallet in `LateUpdate`.
  - They update the text only when the displayed value changes.
  - The HUD sits on its own nested canvas.

### 5.9 Upgrades and stats [5a, 5b, 6]
The economy file's model (D39, D40) replaces the earlier plan of `UpgradeDefinition` assets with levels, a cost formula, stat modifiers and `UpgradeEffect` hooks.
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
  | ~~`notepad`~~ | none | Dropped in Step 7 (D54); an old entry gets a "no longer used" message. | 5b |
  | `extra_clients` | `boardSize {cols, rows}`, `clientSlots`, `stagePool` [7] | **Sets** the open part of the board, the number of caller slots (D42, D49) and the call queue (D52). | 6, 7 |

- **Chains.** Entries of the same type form a chain in file order. Only the next unbought entry of a chain can be bought; there's no skipping. Values in a chain must grow.
- **`UpgradeService`** (Domain) owns the set of bought ids.
  - `CanBuy(id)` returns a `PurchaseCheck` (`Ok`, `Unknown`, `Owned`, `NotNext`, `TooExpensive`).
  - `TryBuy(id)` checks that the entry exists, isn't owned, is next in its chain and is affordable, then spends through `Wallet.TrySpend` and raises `UpgradeBought`. Otherwise it changes nothing and returns false.
  - A save restores the bought ids without spending [9].
- **`StatSheet`** recomputes every stat from the bought ids whenever they change. A stat is the value of the last bought entry in its chain, or the starting value from `Economy.json` if none is bought (multiplier 1, `startingCords`, `startingBoard`, `startingClientSlots`). Recomputing from scratch means nothing can be applied twice.
- **`ShopShelf`** (Domain) decides what the shop's buttons show (D41). It has 3 rotating slots, each holding one chain. A slot keeps its chain until the chain is bought out, then takes the next chain (in order of first appearance in the file) that isn't shown yet. The secret is never on the shelf; it has its own pinned button.
- **Multiplier.** It applies to the fee and the per-second pay, live, including calls already connected (D12, D32). Fines are never multiplied (D37).
- **Shop buttons as built (5a, accepted deviations from D41):**
  - Empty and too-expensive buttons stay clickable but do nothing; disabling Unity's `Button` would tint the "+" look. Too-expensive offers are dimmed instead (grey button image, faded text).
  - The owned secret shows its name and "Owned", with a full fill, no price, no frame, and does nothing on click.
- **Wires and Notepad as built (5b, accepted deviations):**
  - New cords are copies of the last cord and its tray slot, made while the game runs. The tray slot keeps its height and only narrows. With 6 cords, the previews are about half today's size.
  - Dev "Reset upgrades" keeps the added cords (a cord can't be removed safely mid-call). The shop then offers the first wire entry again, and buying it adds nothing.
  - The Notepad's row taps were removed in Step 7 (D54); book rows no longer take clicks.
  - Validator rules: `wires` must be above `startingCords`, at most 6, and growing.
- **Board growth as built (6, accepted deviations):**
  - A wrong `startingBoard` or `startingClientSlots`, not only a missing one, also falls back to the full board and 4 slots, with a message.
  - Along the chain the board must grow (no side shrinks, at least one grows); `clientSlots` may stay level but never shrinks.
  - Step 6's people checks (enough recipients, 2 people on the starting board) were removed in Step 7; the calls' jacks are checked instead (6.5).
  - Closed caller slots are hidden (`041fb90`); Step 6's plain dark discs are gone.
  - Dev "Reset upgrades" also closes the board: it clears live calls and restarts the queue at `startPool`'s first call (7), then returns cords plugged into jacks that close. Added cords stay, as in 5b. Dev "Open board" grants the next `extra_clients` free (`UpgradeService.TryGrantNext`).
  - `GameContent` is built from the people and economy catalogs only; it no longer takes `GameConfig`.
- **Checks.** The economy file's checks live in `EconomyFileLoader` (as the book's checks live in `BookFileLoader`); `ContentValidator.ValidateEconomy` passes them on with the prefix "Economy file: ". There is no separate `EconomyValidator`.

### 5.10 Progression and the goal [5a]
> **Removed in 13g (D65):** the mystery upgrade, `GoalProgress`, the savings fill and the victory scene go. Progression becomes the day schedule and the wall (§12.9, §12.10). `CareerStats` stays and grows.

- **`GoalProgress`** (Domain):
  - The goal is the one `secret` entry. Code reacts to the type, never to an id or the word "mystery".
  - `Progress = clamp01(balance ÷ secret cost)` drives the savings fill on the secret's button (D14, D41).
  - `GoalAffordable` fires the first time progress reaches 1.
  - Buying the secret raises `GoalPurchased`. For now the Game layer shows a "To be continued" panel, and play goes on.
- **`CareerStats`** [9a]: lifetime earnings (`EarningsService.TotalPaidCents`, so dev money doesn't count), calls connected (once per call, not on resume), fines paid (the amount taken), seconds played. Owned by `GameSession.Career`, saved in the save's `career` section; nothing shows it yet.

### 5.11 Feedback: UI and audio [1 HUD, 11 full]
- **Style:** [StyleGuide.md](StyleGuide.md) holds the look and materials, the palette, the feedback language (what good, bad, urgent and reward look like), the fonts, motion timings and layers. Every visual follows it.
- **Views:** `MoneyCounterView`, `ShopView` (drives the shop buttons, including the savings fill; D41), `FloatingTextPool` (+$ and −$ popups).
- **`FeedbackDirector`** maps domain events to sound cues and popups.
- **`AudioService`:** a small pool of audio sources, looping sounds, and volume per category. Web builds have limited mixer and DSP support, so volume is handled in code.
- **Sounds (D60; the list is S1–S16 in Plan §9, Step 11):**
  - One bell when a new order arrives; no reminders for normal calls.
  - An emergency's own alarm bell, repeated every few seconds while it waits, and a speeding-up clock tick in its last 10 s.
  - Coin ticks, rate-limited.
  - Distinct sounds for paid, cut off, missed and fined; a stamp thunk; the quick-tip bell; caller talking blips; upgrade-specific shop sounds; an office ambience and a second music track.
  - The cord's plug sounds stay where they are.
- **Visual feedback (D60, Step 11c):** reels that roll, a fine shake with coins leaving the counter, the emergency wobble and red board lamp, card arrival and ring shakes, the wrong-number shake and red jack flash, connection rings and a cord wiggle, and a wrong number's short circuit (sparks, arc, smoke) and cord jolt. A cord can play short animations through `CableMotion`. An EMERGENCY sign under the board's red lamp explains it. All presentation: they read domain state and change no rules. Emergencies carry no "New order!" stamp.
- **Built on what exists:** `CallAudio`, `ShopAudio`, `SceneUiAudio` and `UiClickSound` (team, Steps 3–9) do the job of the planned `FeedbackDirector`/`AudioService`; Step 11 extends them and adds a shared cue type only if it removes duplication. Every source has an `AudioChannelSource` (Sfx, or Music for music and ambience).

### 5.12 Save and load [9]
- **Saved:**
  - version
  - balance and fraction
  - the bought upgrade ids (the secret among them)
  - career stats
  - settings and tutorial flags

  Career data is kept separate from run data, so a prestige reset or new-game-plus mode can reset one without the other.
- **Saved too (9a, D55):** the queue position, as the pool index of the oldest call still on a card, so unfinished calls ring again after a reload.
- **Saved too (Step 10 fix 2, D57):** `shopHintsDone`, the tutorial's shop hints already shown, as `ShopHints` flags (1 cord, 2 mystery, 4 More Profit, 8 More Clients). A new game starts with none; Continue shows only the rest. A save without the field (written before fix 2) reads as -1 (`SaveData.ShopHintsMissing`, kept by `JsonUtility` as the initializer) and counts as all shown. The first-call guide itself is not saved: it stays New Game only.
- **Not saved:** calls, cords and the call in hand. Each session starts with an empty board (D25).
- **New game (9a, D55):** a main-menu button, shown while a save exists, deletes the save after a confirmation.
- **Format:**
  - `JsonUtility`, with lists instead of dictionaries.
  - Content is referenced only by stable string ids. Unknown ids are ignored, and missing ones count as not bought.
  - The version number drives step-by-step migrations.
- **Storage.** `ISaveStorage` has two implementations:
  - `PlayerPrefsStorage` for the editor.
  - `BrowserStorage` for the Web build (9b): a `.jslib` that writes `localStorage` under `operator.save` and `operator.save.backup`. Every access is in try/catch; if storage is blocked, Web players fall back to PlayerPrefs with one warning. The volume patch (`Switchboard.AudioPatch.v1`) uses the same storage.

  Unity's Web PlayerPrefs live in IndexedDB under a path tied to the page URL. Developers report that this loses saves when a host such as itch.io puts each upload at a new URL.
- **When to save:**
  - right after a purchase
  - every ~10 s while something has changed
  - on `OnApplicationFocus(false)`
  - in the browser, inside the `visibilitychange` (hidden) and `pagehide` events (9b), because a hidden tab runs no frames and phones kill hidden tabs

  Never rely on `OnApplicationQuit` in a browser.
- **As built (9a), accepted deviations pending the lead's playtest:**
  1. A new game where nothing happened yet (no money moved, nothing bought, no call connected: `GameSession.HasProgress`) writes no save, so New game stays hidden right after Start over.
  2. Saves are read and written only in Play Mode; EditMode tests that run `GameInstaller` never touch PlayerPrefs (`SaveIsolationTests`).
  3. Unknown ids are dropped at the next save, not carried along.
  4. Career earnings are rounded to the whole cent at each save.
  5. The 10 s autosave writes almost every time while playing, because seconds played change.
  6. Migrations run on `SaveData` objects, not raw JSON; load problems are warnings.
  7. **Menu buttons (lead's 9a playtest).** "Play Game" is deleted from `StartMenu.prefab`. `NewGameMenu` shows Continue only while a save exists and re-spaces the visible buttons at run time: Continue, New Game, Options, Credits, Quit top to bottom, 122 apart, centred on y -50 under the title, so a hidden button leaves no gap. New Game starts at once without a save and asks first with one. Every menu label is centred: Center/Middle, stretched to its button, 18 pt maximum with a 3-unit bottom margin, so the mixed-case word sits in the middle and descenders stay on the plate.
  8. **Volume patch (Options, main menu and pause menu; lead, 2026-10-20).** Each wire is its channel (Master, Music, SFX) and the column of its socket is the volume (mute, 25, 50, 75, 100); a wire may sit in any row, and several wires may share a row. No jack ever moves on its own: a socket holding another wire's jack refuses the held jack, which goes back where it was. While a jack is held, only free sockets (and its own) light up. Saves are valid when every index is in range and no two wires share a socket, so old one-wire-per-row saves load unchanged. Replaces the cross-row swap (`VolumePatchState`). A jack sticks out of the board, so every wire lies over the resting jacks, whatever the plugging order. Between wires, the most recently plugged one lies on top. Picking a jack up counts as plugging it, even if it then goes back. A held jack and its wire are above everything while carried. A wire over a jack never stops that jack being picked up, because wires take no clicks. The order is saved with the patch (`order`, bottom to top; saves without it get Master, Music, SFX from the bottom).

## 6. Data

### 6.1 ScriptableObjects (read-only at runtime)

| Asset | Fields, with the step that adds them | Notes |
|---|---|---|
| `GameConfig` | card styles `regularCards` and `emergencyCards` [7; a list matched to call types before]; the `Calls.json` [7], `Book.json` [8; `PhoneBook.json` from 1b] and `Economy.json` [5a] references; `extraCordColors` [5b]; portraits, matched by `Calls.json`'s `icon` | The one asset `GameInstaller` references. Since 5c (D47) `startingCents`, `startingCords` and pacing live in `Economy.json`; since 7a (D49) `maxWaitingCalls` is gone too (`startingClientSlots`, `clientSlots`). Only presentation stays here. |
| ~~`CallerTypeDefinition`~~ | Deleted in Step 7 with the call types (D51), with `Regular.asset` and `Emergency.asset`. | Its card look moved into `GameConfig`'s two card-style fields. |
| `CallerDefinition` | `id`, `person`, `type`, `weight`, optional `centsPerSecondOverride` [1]; optional favourite recipients [7] | The "caller config". This is how some callers become worth much more. |
| ~~`PersonDefinition`~~, ~~`CallerDefinition`~~, ~~`PhoneDirectoryDefinition`~~ | Replaced in Step 1b by `Assets/Configs/PhoneBook.json` (D31), which Step 8 replaced with `Book.json` (D53). | Loaded and validated at startup. Errors are logged clearly, and a test runs the same check. |
| `PhoneDirectoryDefinition` | entries `{ person, jackLabel }` | Jack labels are the `SocketView.Address` values, A1–F6. |
| ~~`PacingProfile`~~ | Replaced in 5c by the `pacing` section of `Economy.json` (D47), read into the domain's `PacingSettings`. | Progress curves were dropped by D51/D52. |
| ~~`BoardLayoutDefinition`~~ | Not needed (6): the open area is the top-left rectangle that `Economy.json` sets, and calls carry their own jacks (D51). | |
| ~~`UpgradeDefinition`~~, ~~`UpgradeEffect`~~ | Replaced by the `upgrades` section of `Assets/Configs/Economy.json` (D39, D40). | See 6.4. |
| `AudioCueLibrary` | event → cue [11] | Game layer only. |

- **Shared types** live in Domain as `[Serializable]` structs with public fields, so Unity can serialize them inside Content assets. Example: `FloatRange { public float min, max; }`.
- **Domain sees content through read-only interfaces:** `IGameContent` (starting values, `StartPool`, `Upgrades`) and `IUpgrade` (with `StagePool`, 7). `CallScript` is a plain Domain class that the content loader builds. Tests use plain fakes. Presentation-only fields (sprites, colours) stay in `GameConfig`.

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
| Queue pool and position | `CallQueue` | The position (9a, D55); the pool follows from the bought ids |
| Tutorial shop hints already shown | `GameSession.ShopHintsDone` | Yes (`shopHintsDone`, Step 10 fix 2) |
| Cord state and jack occupancy | the cord code, mirrored by `CordBridge` | No |

### 6.3 Data rules
- **Ids.** Every definition has a stable string `id`, used for saves. Never use the asset name, a list position or a GUID.
  - Format: lowercase with a prefix, e.g. `person.ada-moss`, `type.regular`, `upgrade.extra-cord`.
- **Enums.** Serialized enums use explicit numbers and are never reordered.
- **No runtime writes.** Nothing writes to a ScriptableObject at runtime. In the editor, changes made during Play Mode persist afterwards.
- **People's names:** period-appropriate first and last names (roughly 1920s–40s). Families may share a surname, an address and a jack (D53); full names are unique. No famous real people.
- **`ContentValidator`** (editor menu item, also run as an EditMode test) checks:
  - ids are unique
  - `Book.json` (6.6) [8]
  - `Economy.json` (6.4) [5a]
  - `Calls.json` and the call pools (6.5) [7]
  - every jack a call uses has someone listed on it in the phone book [7]

### 6.4 `Economy.json` [5a, 5c, 6] (D39)
`Assets/Configs/Economy.json` is edited by hand by anyone (section 3). Keep it readable, with one entry per line. The format is documented in `Docs/EconomyFile.md`.
- **Sections:**
  - `upgrades` [5a]: see 5.9.
  - **The file sets the numbers** (lead, 2026-10-19). Anyone may rebalance it at any time; code reads every value from it, and the real-file tests check rules (the file loads, chains grow, pools fit their boards), never the values.
  - `startingCents`, `startingCords` [5c]: 1–6 since `7312e43` (2–6 since `041fb90`, when the tray began to be trimmed to the starting count; 3 before). Currently 1.
  - `startingBoard {cols, rows}` (1–6 per side) and `startingClientSlots` (1–4 since `477c607`; 1–6 before) [6]. Currently 4×4 and 3 (`7312e43`; 3×3 and 2 before). If either is missing or wrong, the game opens the full board and 4 slots, with a message.
  - `quickConnectSeconds` and `quickConnectTipCents` [10c, D59]: the tip window and amount, currently 5 s and 5¢; 0 in either turns the tip off; a missing field gets 5 with a note; a negative value or a tip that isn't whole cents is reported and falls back to 5.
  - `startPool` [7]: the call ids of a new game's queue, in play order (D52). Each `extra_clients` entry's `stagePool` replaces it once bought.
  - ~~`pacing`~~ and ~~`calls`~~ [5c] and the `notepad` upgrade were removed in Step 7 (D51, D54); an old file gets a plain "no longer used" message.

  Presentation stays in Unity assets, for example the emergency card's badge and colour.
- **Upgrade ids** follow the file's style: lowercase letters, digits and `_`, such as `income_1`. This is an accepted exception to the `type.regular` id style.
- **Money** is whole cents (`costCents`), like the wallet.
- **The validator** (also an EditMode test and part of `Tools/Switchboard/Validate Content`) reports, each in plain words naming the entry and the fix:
  - a missing or unreadable file
  - an empty, badly formed or duplicate id
  - an unknown type (the message lists the known types)
  - a missing name
  - a cost of 0 or less
  - a chain value that doesn't grow (e.g. `incomeMultiplier` 1.2 after 1.5), or an income multiplier of 1 or less
  - not exactly one `secret`

### 6.5 `Calls.json` [7] (D51)
`Assets/Configs/Calls.json` is edited by hand by anyone: one call per line, documented in `Docs/EconomyFile.md`.
- **Fields:** `id`; `icon` (a portrait name in `GameConfig`); `name` (shown under the portrait); `message` (the bubble text); `from` and `to` (jack labels such as "C3": row A–F, then column 1–6); `durationSeconds`; `connectCents`; `centsPerSecond` (may have decimals). An emergency adds `"emergency": true`, `responseTimeSeconds` and `penaltyCents`.
- **The validator** reports:
  - a missing or unreadable file (the game still starts, with no calls)
  - empty or duplicate ids, and missing required fields (never silently 0)
  - an unknown portrait, a duration of 0 or less, negative money
  - jacks off the 6×6 board, or `from` equal to `to`
  - an emergency without `responseTimeSeconds` or `penaltyCents`, and a note for either on a regular call
  - with `Economy.json`: unknown ids, an empty `startPool`, a call outside its stage's board ("call_014 uses E5, but board_1 opens only 4x4."), and a note for a pool smaller than its slots
- **As built (7, accepted deviations):** fees and fines must be whole cents; jack labels are trimmed and upper-cased (" c3 " reads as C3); a `"pacing": {}` with no fields isn't reported.

### 6.6 `Book.json` [8] (D53)
`Assets/Configs/Book.json` is edited by hand by anyone: one entry per line, documented in `Docs/BookFile.md` (linked from `Docs/EconomyFile.md`).
- **Fields:** `contactsPerPage` (optional, 6 if missing, 1–12); `entries`, each with `name` ("Surname, First" for people with normal capitals, a title after the first name in brackets, e.g. "Plodwell, Percy (PC)"; a service by its whole name; lead, 2026-10-21), `profession`, `address` (both may be empty), `emergencyService` (false if missing) and `jack` (a label such as "C3"). Several entries may share a jack.
- **The validator** reports:
  - a missing, empty or unreadable file, and a file with no entries
  - a missing `name` or `jack`, and a jack off the 6×6 board
  - a name listed twice (trimmed, ignoring case and extra spaces)
  - a `contactsPerPage` outside 1–12
  - the old `PhoneBook.json` names (`contacts`, `socket`), with the new ones
  - with `Calls.json`: a call's `from` or `to` jack with no entry in the book
- The file's order doesn't matter (5.7), so there are no order, surname or one-entry-per-jack checks any more.

## 7. Communication rules
- **Down: method calls.** Views call verbs on narrow interfaces such as `OperatorDesk.TryTake` or `IShop.TryBuy`. Services validate and return `bool`.
- **Up: C# events for things that happen.** Payloads are `readonly struct`s. Views subscribe in `Bind` and unsubscribe in `OnDisable`.
- **Up: polling for values that change every frame** (money, earnings per call, countdowns). Views compare one number in `LateUpdate`.
- **Between services: the same events.** For example, `PenaltyService` and the call views both listen for `CallEnded`. `OperatorDesk` is the one deliberate mediator between calls and cords.
- **Not allowed:** singletons, static mutable state, service locators, `FindObjectOfType` or finding objects by name. The exceptions are the lookups of the board's objects in `Final` described in section 3: `CordBridge`'s typed lookup and the `CallerQueuePanel` slots.
- **Not used for core rules:** ScriptableObject event channels.
- **Event safety.** Raise events only after state is consistent, and don't iterate a list that a handler can change.

## 8. Web constraints
- **Single thread.** No threads, no `Task.Run`, no `async void`. All gameplay runs in the main loop.
- **Game time only.** Gameplay timers use `Time.deltaTime`, clamped by `maximumDeltaTime`, never the system clock. Run In Background stays off, so timers pause in hidden tabs.
- **Memory.** Pool cards, rows and popups. Avoid per-frame allocations and LINQ in per-frame code. Use sprite atlases and compressed textures [12].
- **Audio unlock.** Browsers block audio until the player interacts. The first tap on a `MainMenu` button unlocks it; there is no press-start panel (9b).
- **Input.** Nothing may depend on hover. The system pointer is hidden next to the drawn hand through `SystemPointer` (only on a change; a transparent 1×1 image in browsers). On the lead's external display, Chrome still flickers any custom or hidden pointer; the built-in screen and Safari are fine (Plan §8).
- **Layout.** Landscape first. In portrait, show a "rotate your device" screen (D24) [12].
- **Hosting (D26): itch.io.** Gzip with Decompression Fallback; `index.html` at the root of the upload; the itch page set to mobile friendly, landscape, with a fullscreen button. Built in 9b: `Tools/Switchboard/Build Web`, then `butler push Builds/Web llazypixell/operator:html5` (`Docs/WebBuild.md`). The project's `Operator` template fills the itch frame. `productName` stays `ludumdare-60`, because the Mac editor's PlayerPrefs file is named after it.

## 9. Extensibility
- **New call:** add a line to `Calls.json` and its id to a pool in `Economy.json` (D51, D52). The validator checks it. No code.
- **New kind of call (VIP, long distance):** a new field in `Calls.json` and the rule that reads it, for example a `ConnectionRule` for a trunk jack.
- **New upgrade:** add an entry to `Economy.json`. Only a new `type` needs code:
  - the type name and its fields in the loader and validator
  - the stat in `StatSheet`, with a base value
  - the system that reads it
- **Bigger board:** add or change an `extra_clients` line in `Economy.json` (board size, caller slots, price), write its `stagePool` of calls, and place people on the jacks it opens in the phone book. No code change.
- **Mystery effect (D15):** react to `GoalPurchased` (once) and, from Step 9, to a loaded save with the secret bought (a lasting state). If it needs a new system, register that system in `GameSession`.

## 10. Decisions
Statuses:
- **decided:** agreed.
- **default:** we build this unless told otherwise.
- **confirm:** the default needs the lead's yes before the step that needs it.
- **open:** must be decided before that step.

| ID | Decision | Status | Needed by |
|---|---|---|---|
| D01 | Integrate with the cord code through `CordBridge`. The game's systems live in `Game.prefab` in the shared `Final.unity` (D38; until then, `Game.unity` loaded `Level.unity` additively). | default | 1 |
| D02 | Assemblies as in 4.1: Domain and Content are asmdefs; the Game layer is in Assembly-CSharp. | default | 1 |
| D03 | One call in hand at a time. Taking another card swaps; the old one goes back to the queue with its timers running. **Superseded by D45.** | superseded | 2 |
| D04 | A circuit closed with no call in hand bounces back with the blocked sound. | default | 1 |
| D05 | Plug order doesn't matter. | default | 1 |
| D06 | A wrong connection ends the call at once and the cord bounces back. The callback comes after the type's delay. | decided (spec) | 1/2 |
| D07 | Pulling a plug from a live call puts it **on hold**: no pay, timer paused. Putting the plug back resumes the call. Moving it elsewhere or returning the cord to the tray ends it as DisconnectedEarly. This uses the cord code's restore feature. | decided (lead, 2026-10-17) | 2 |
| D08 | Every call end returns its cord to the tray. | default | 1 |
| D09 | An emergency's response timer runs until a correct connection, and it's visible as a ring on the card. | default | 4 |
| D10 | Callbacks after a miss or an early disconnect are per-type flags, off by default. | superseded by D51 (no callbacks) | 4 |
| D11 | Rate comes from the type's range, with an optional per-caller override. | default | 1 |
| D12 | The multiplier applies live, including to connected calls. | default | 5 |
| D13 | Money is a `long` of cents plus a fraction, displayed as `$0.00`. | decided | 1 |
| D14 | Savings bar = balance ÷ goal price. It is drawn as a fill inside the secret upgrade's shop button (D41). | superseded by D65 (removed in 13g) | 5a |
| D15 | What happens after the mystery upgrade (ending, prestige, or play continues). The save keeps career and run data apart either way. | closed by D65: the mystery upgrade is removed; the game is endless with horizons | – |
| D16 | Idle guard: types that carry fines stop spawning after 60 s without input. | superseded by D51 | 4 |
| D17 | Waiting-card cap = 6 (the caller panel has 6 slots). Since Step 6 (D49): live calls never outnumber the open caller slots (`startingClientSlots`, then `clientSlots`). | default; updated by D49 | 2 |
| D18 | A person can't be in two calls at once. | superseded by D51/D52 (a call on a busy line waits on its card) | 2 |
| D19 | Difficulty is driven by lifetime earnings. | superseded by D51/D52 (the call queues) | – |
| D20 | The book and the shop never pause the game. | default | 3 |
| D21 | Board growth uses pre-laid-out sections with cover plates. **Superseded by D42** (dimmed closed jacks; plates only if they are drawn later). | superseded | 6 |
| D22 | Jack assignments are fixed in data. | default | 1 |
| D23 | Book QoL: tapping an entry in the phone book notes its jack on the call in hand (since D46: on every accepted call that person is part of) (e.g. "Ada Moss (C3) / wants Basil Thorne (A5)"). Not built in from the start: it is sold in the shop as a cheap one-entry upgrade, `type` `notepad` in `Economy.json` (D40). Deleting the entry drops it. The book gets a tapped-entry event (D44). | superseded by D54 | 5b |
| D24 | Portrait on phones shows a "rotate your device" screen. | default | 12 |
| D25 | Save progression only; no in-flight calls and no offline earnings. | default | 9 |
| D26 | Hosting on **itch.io** (lead, 2026-10-19): an HTML project page that plays the Web build in the browser, inside itch's frame. itch can't be set up to send compression headers, so the build uses Gzip with Unity's Decompression Fallback: the loader unpacks the files itself, and the same files also run on any other web server, such as the lead's VPS. Saves go to `localStorage` through `BrowserStorage` (5.12), not Unity's Web PlayerPrefs, because itch gives every upload a new URL. | decided (lead, 2026-10-19) | 9 |
| D27 | Currency, number scale and **pace**: shift length, patience, emergency time, quotas, bonuses and pay per day (the list in §12.13). Placeholder numbers until then, tuned in `Economy.json` and `Calls.json`, never in code. | open; each part of step 13 carries its numbers in its brief | 13a–13g, 13 |
| D28 | Touch input in the cord code (`Pointer.current`). Built by us like any other change (D48). | decided | 9 |
| D30 | Division of work between the UI components (phone book, shop panel, main menu, visuals) and the rules and data, with steps that wire the components to the rules. Steps 3, 5 and 6 become wiring steps. | superseded by D48 (wiring steps still apply; any split of who builds what does not) | 1b |
| D31 | One people file. `Assets/Configs/PhoneBook.json` is the single source of people, jacks and per-caller settings. Each contact has `name` (written "Surname, First", e.g. "Moss, Ada", since the merge `e7acec5`; services use their whole name, e.g. "Fire Brigade"; the loader still reads "Ada Moss", and the game shows "Ada Moss" on cards), `socket`, `portrait` (sprite name), `type` (call type id) and optional `rate` / `fee` overrides (0 = use the type). The book reads only `name` and `socket` from it, unchanged; the game reads every field. The per-person ScriptableObjects from Step 1 are removed. Call types and game settings stay ScriptableObjects. Portraits appear on the call card, not in the book. Names are in English. | superseded by D53 | 1b |
| D32 | Connection fee. On a correct connection the caller pays a one-off fee, then the per-second rate. The fee comes from a range on the caller type, with an optional per-caller override (the same simple config as the rate; no tiers). It is paid once per call: not on a wrong connection, not again after hold and resume. The multiplier applies: earnings = (fee + seconds × rate) × multiplier. The card shows both, e.g. "10¢ + 5¢/s". The "Paid" stamp includes the fee, and "+10¢" pops up on connection. | decided (lead, 2026-10-17) | 1b |
| D33 | Scene flow: `MainMenu` → `Game` → `Level` (build indices 0, 1, 2). The menu loads the next build index, which is `Game`; `Game` loads `Level` additively. Superseded by D38: `MainMenu` → `Final`. | superseded | 1b |
| D34 | Phone book A–Z tabs are added to `PhoneBookView` in Step 3. | decided (lead, 2026-10-18) | 3 |
| D35 | The directory grows to 36 people, one on every jack A1–F6, in Step 3. Portraits are reused until more are drawn. From Step 6 the game starts on a 3×3 part of the board and the book shows only the people on open jacks (D42). | decided; the book filter dropped by D53 | 3 |
| D36 | Emergency services (Police, Fire Brigade, Doctor) replace 3 of the 36 people on their jacks (33 people + 3 services). They are listed **first** in `PhoneBook.json`, so the book shows them at the top of page 1; the people follow, sorted by surname. The validator allows that leading block of `emergency-service` entries. The book code is unchanged (it shows file order). | decided; numbers and fines superseded by D51, the book order by D53 | 4 |
| D37 | Step 4 numbers: an emergency must be connected within **30 s**. Fines: **$2** for a miss or a wrong patch, **$1** for cutting a connected emergency off early (not multiplied). Emergency pay: fee 20–40¢, 5–10¢/s, 10–20 s calls. About **1 call in 6** is an emergency (a temporary `spawnWeight` on each caller type: Regular 5, Emergency 1, until Step 7's `weightByProgress`), and at most one at a time. Any person can place an emergency call (`everyoneCalls`); services never call, and regular calls never go to a service (`recipientTag` empty = untagged people only). Services are JSON entries with `"tags": ["emergency-service"]` and no portrait or type. | decided; numbers, fines and weights superseded by D51 | 4 |
| D38 | One final scene, `Final.unity` (formerly `Level.unity`), built from prefab instances: the board prefabs plus `Game.prefab` and `GameUI.prefab`. Build list: `MainMenu` (0), `Final` (1); `Game.unity` is removed. The game code finds the board's objects by type at start. A first merge (`1c20569`, unpacked copies) was reverted in `cdc5876`. This carries out D29. | decided (lead, 2026-10-18) | before 5 |
| D39 | Economy config file. The economy is controlled through one hand-edited file, `Assets/Configs/Economy.json`, with sections: `upgrades` [5a]; `calls`, `pacing` and `startingCents` [5c]. Presentation (card badges, colours) stays in Unity assets. Domain rules keep reading values through interfaces, so only Content loaders, the validator and tests change. A format guide lives in `Docs/EconomyFile.md`. | decided (lead, 2026-10-18) | 5a |
| D40 | Upgrade model (a team proposal, adapted): a flat list of one-time entries with `id`, `type`, `name`, `description`, `costCents` and the type's fields. Types: `extra_income` (`incomeMultiplier`), `extra_wires` (`wires`), `notepad`, `extra_clients` (`boardSize`, `clientSlots`), `secret` (exactly one). Every value **sets** the resulting state (wires total, not wires added). Entries of one type form a chain in file order; only the next unbought one can be bought; values in a chain must grow. Ids are never renamed once players have saves. No levels, cost formula or stat modifiers; this replaces the old 5.9 plan. The proposed `clientPool` is dropped (D42). | decided (lead, 2026-10-18) | 5a |
| D41 | The shop is the `UpgradeShopPanel` with its 4 buttons. The bottom button is pinned to the `secret` upgrade and shows the savings fill (D14). The top 3 rotate: each shows the next offer of one chain, and when a chain is bought out its button takes the next chain not yet shown, in file order (`ShopShelf`). A button with nothing to offer shows the placeholder look and can't be clicked. The name, description and price are drawn inside the button prefab by a small component (`UpgradeSlotView`, D44); `ShopView` drives it. | decided (lead, 2026-10-18) | 5a |
| D42 | Board growth [6]. The game starts on a 3×3 part of the board (rows A–C, columns 1–3). Exactly the people whose jack is on the open part are in the game, and the book shows only them, so the file picks the starting group by placing people on those jacks. `extra_clients` sets the open board size and the caller slots. Closed jacks are dimmed and can't be clicked (cover plates only if they are drawn). The emergency services join when the board opens over their jacks, so emergencies come later in a session.  Changes the board and book code directly (D48). Supersedes D21. | decided; the book part superseded by D53 | 6 |
| D43 | Placeholder upgrade prices (until the economy file; D27): income ×1.5 for $8, ×2.0 for $20, ×3.0 for $50; the secret $250. From 5b: wires 4, 5 and 6 for $5, $8 and $13 (start 3); Notepad $3. | default; Notepad dropped by D54 | 5a |
| D44 | Missing hooks in the cord, book and shop code are built by the steps that need them (lead, 2026-10-18): the shop button texts and savings fill [5a]; adding cords at runtime to the cord input and tray [5b]; a tapped-entry event in the phone book [5b]. | superseded by D48 (no approvals needed) | 5a |
| D45 | Persistent wiring (`2fc3810`, adopted by the lead 2026-10-18). Several calls can be accepted at once; taking a card no longer swaps. Cords stay plugged when calls end. A closed circuit connects any accepted call it is correct for; taking a card connects it at once if its circuit is already plugged. A wrong number needs a manual insertion against an anchored end that belongs to an accepted call. The bubble shows only on the card last clicked. Supersedes D03; see 5.1 and 5.6. | decided (lead, 2026-10-18) | 5b merge |
| D46 | Notepad under D45: a tapped book entry notes that person's jack on every accepted call still waiting to connect (Taken or Connecting) where they are the caller or the recipient, including duplicates. Ringing, connected, on-hold and ended calls are never noted. | superseded by D54 | 5b fix |
| D47 | Step 5c details: `startingCents`, `startingCords` (3–6), `pacing` and `calls` live in `Economy.json`; the old asset fields are deleted, not kept as a fallback (`CallerTypeDefinition` keeps only `id` and the card style; `PacingProfile` and `Default.asset` go). Ranges are `{ "min", "max" }` objects. Call type ids keep the `type.` style because `PhoneBook.json` references them; the type's display `name` moves into the file. `maxWaitingCalls` and the cord colours stay in `GameConfig`. | decided (lead, 2026-10-18) | 5c |
| D48 | One shared work (lead, 2026-10-18; restated 2026-10-22). The whole project is worked on together: no owners, no read-only folders and no approval lists; anyone may edit any part, and the docs name no one as the owner or maker of a part. A step changes whatever files it needs; its brief names them, and its report lists them. Teammates still push to `main`, so pre-flight merges their changes first. Supersedes D30 and D44, and parts of D38. | decided (lead, 2026-10-18) | now |
| D49 | Board growth details (lead, 2026-10-18): `extra_clients` upgrades grow the board, and each also sets the caller slots. **The start board is set in `Economy.json` (currently 4×4; changed in `7312e43`, 2026-10-19).** The numbers live in the file: currently 4×4 with 3 slots, then 5×5 with 4 and 6×6 with 4 (6 before `477c607`); as first built, 4×4, 5×5 and 6×6 from a 3×3 start, with 3 → 4 → 5 → 6 slots. The board is controlled from `Economy.json`: `startingBoard`, `startingClientSlots` and every upgrade's size, slots and price live there (no sizes in code), and they replace `GameConfig.maxWaitingCalls`. The physical 6×6 board and the caller slots are the only limits: at most 4 callers since `477c607` (2026-10-19, tarbaev.d: `StatSheet.MaxClientSlots` 6 → 4; `Economy.json` now goes 3 → 4 → 4). As first built, one emergency service per new ring (Doctor D4, Fire Brigade E5, Police B6); since `7312e43` the file puts all three services on the 4×4 start (Hospital A2, Police station D3, Fire station D4). Built as Step 6 (numbered 7a when it was built); the tutorial is Step 10. | decided (lead, 2026-10-18) | 6 |
| D50 | Phone book order (lead, 2026-10-18): the most realistic version. Names are written and shown "Surname, First" ("Moss, Ada"), services by their whole name; the book lists services first, then people in surname order, exactly the file's order (checked by `Validate Content`). The letter lookup goes by surname. The A–Z tabs stay hidden for now. Replaces the first-name order from `6226ead`. | superseded by D53 (the book sorts itself) | merge |
| D51 | Hand-written calls (team proposal, lead 2026-10-18). Every call is an entry in a new `Assets/Configs/Calls.json`: `id`, `icon` (portrait), `name` (shown under the portrait), `message` (the bubble text), `from` and `to` jacks as labels such as "C3", `durationSeconds`, `connectCents`, `centsPerSecond`, and for emergencies `responseTimeSeconds` and `penaltyCents`. All money in cents, all text in English. The only fine is for a missed emergency (no wrong-patch or cut-off fine). Replaces random spawning, call types and their weights, callbacks, the busy tracker, the idle guard and the pacing timer: supersedes D16, D18, D19, the spawning parts of D17, D36/D37's numbers and fines, and the `calls`/`pacing` parts of D47. | decided (lead, 2026-10-18) | 7 |
| D52 | Call queues (lead 2026-10-18). `Economy.json` has `startPool` (call ids for a new game) and a `stagePool` on each `extra_clients` entry; buying one replaces the queue and starts it at its first call. Calls play in order and loop; success or a wrong number doesn't change the order. Each open caller slot holds one call, and a free slot takes the next queued call at once (no timer). A call waiting on a busy line stays on its card. A call still on a card is skipped when the queue comes round to it (orchestrator rule, so no call shows twice). Caller slots start at `startingClientSlots` and grow with the board upgrades (2, then 3, 4, 5 as first built; 3, 4, 6 since `7312e43`). **Added in `7312e43` (accepted):** a call whose caller already has a live call is skipped too, and a new game starts with one tutorial call (5.2). | decided (lead, 2026-10-18); additions 2026-10-19 | 7 |
| D53 | The phone book is a lookup only (lead, 2026-10-18). A new `Assets/Configs/Book.json` replaces `PhoneBook.json`: each entry has `name` ("SURNAME, First"; `7312e43` briefly used "First Surname"; since 2026-10-21 the lead's "Surname, First" with normal capitals), `profession`, `address`, `emergencyService` and `jack` (shown as the last column); entries may share a jack. The book shows every entry from the start, services first, then names alphabetically (the real-directory order of D50); the rules don't read it. Supersedes D31 and the book part of D42. | decided (lead, 2026-10-18); the rules read the residents from 13c on (D77) | 8 |
| D54 | The Notepad upgrade is dropped (lead, 2026-10-18). Supersedes D23, D43 and D46. | decided (lead, 2026-10-18) | 7 |
| D55 | Step 9 details (lead, 2026-10-19). Step 9 is split: **9a** saves and touch input, testable in the editor; **9b** the Web build on itch.io, once the Web Build Support module is installed. On load the call queue **replays unfinished calls**: the saved position is the pool index of the oldest live call from the current pool, so calls on cards at the save ring again (one finished out of order may repeat). A **"New game"** button in the main menu, shown only while a save exists, asks for confirmation and deletes the whole save. **Menu order (lead's 9a playtest):** without a save, New Game comes first and starts at once with no question; with a save, Continue comes first and New Game second (with the confirmation); Continue is hidden without a save; "Play Game" (the same as Continue) is removed. Agents may push builds to the itch **Draft** page with butler. | decided (lead, 2026-10-19) | 9a, 9b |
| D56 | Pause menu (lead, 2026-10-19, at the 9a playtest). A pause button and Esc open a modal panel: Resume, Options (the main menu's volume patch) and Main Menu (saves, then loads `MainMenu`). While paused, game time stops (`Time.timeScale` 0: pay, durations and emergency countdowns freeze), call sounds pause, and the cord input is off. Esc closes an open book before it pauses. The book and the shop still never pause the game (D20). Built in 9a pass 2 (`GamePauseMenu`); every `Button` click now keeps a held cord. | decided (lead, 2026-10-19); built | 9a pass 2 |
| D57 | Tutorial (lead, 2026-10-20). It runs **only on a New Game** (no save loaded): the one-call start (5.2), the first-call guide and the shop's first-cord pulse; Continue never shows it. A new game that has not saved yet gets it again, because it is a new game again. The earlier pulses blend into the art, so the guide becomes a **spotlight**: one dark layer (about 55%) over the whole screen, with soft holes around the current target(s), a short text hint and an arrow beside them; the holes slide from step to step. **Clicks pass through** the dark layer (visual only), so the player can never get stuck. A **Skip tutorial** button ends it. It replaces the pulses rather than stacking on them, and builds on `FirstOrderGuideSequence`. Built in Step 10: `TutorialSpotlight` (in `GameUI.prefab`, its own canvas at `SwitchboardUISorting.Tutorial` 14: above the book and GameUI, below the Goal dialog, the pause menu and the hand), dimmed to 0.78 alpha because of linear colour space; `GameSession.IsTutorial` decides. **Changed at the first playtest (lead, 2026-10-20): the tutorial is mandatory.** Outside the lit holes nothing responds (clicks on the dark layer are swallowed: no pickup, no drop of a held cord); only the current target, Skip, pause and Esc work. Step 6 accepts only the call's two jacks, and the tutorial caller does not hang up while the guide runs. Two shop steps follow once the player can first afford More Cables: buy it (step 8, replaces the shop's first-cord pulse), then a look at the ??? mystery upgrade (step 9). **Second playtest (lead, 2026-10-20):** step 2 (the speech bubble) advanced too fast to read, so it now waits for a click once the text has finished. Two more shop hints: More Profit and More Clients, each shown once the first time it is affordable (after the first-call guide); the player can buy it or dismiss the hint. The shop hints (steps 8–9 and these two) are remembered in the save, so a reload no longer drops them; an old save without that record shows none. | decided (lead, 2026-10-20); built (Step 10 + fix passes 1 and 2; the shop hints are saved as `SaveData.shopHintsDone`, a `ShopHints` flag set, -1 for old saves). Blocking: the dark `SpotlightGraphic` is a raycast target outside the holes and carries `IPointerBlocker`, which `CableInputController` treats as "do nothing"; `PhoneBookView.OpenLocked` keeps the book shut outside step 3. The guided call's response timer is held (`CallService.HoldWaiting`, released by `GameSession.EndTutorial`). | 10 |
| D58 | Faster lookup (lead, 2026-10-20). Searching the book took too long for a busy board, so: (1) **the caller's jack lights up** on the board, as on a real switchboard: every waiting call's caller jack blinks softly, and the call in hand's caller jack glows steadily until a plug goes into it, so only the recipient needs looking up; (2) **clicking a row in the open book marks that jack** on the board (a steady glow, one mark at a time) and closes the book; the mark clears when a plug goes into that jack, another row is clicked, or another call is taken. View only: the rules still never read the book (D53), and any row can be marked, right or wrong. Then `Economy.json` can raise the call volume. Built in 10b: `CallerLights` (domain: Off, Waiting, InHand per jack; dark while a loose plug sits in it), `CallerJackLights` and `SocketLampGraphic` (a lamp in the socket on the board canvas, under plugs and wires); book rows are hit by position (`PhoneBookView.RowAt`, `JackPicked`); a row whose jack is off the open board does nothing. Tutorial step 4 lights only the recipient row. **Playtest changes (lead, 2026-10-20):** blinking always means a caller's line (the call in hand blinks brighter and faster, it no longer glows steadily); a steady glow means only the jack marked from the book. The tutorial gets a step after the speech bubble that lights the caller's blinking jack and explains it. Book rows highlight on hover. The open book stays where it is (not docked beside the board, revisit after playtesting; docked as a clipboard by D78, 2026-11-10). **Second playtest (lead, 2026-10-20):** the board stays busy, but waiting callers only glimmer faintly and slowly while the taken call blinks at full brightness, fast, with a bigger halo; hovering a ringing card makes its caller jack flare briefly. Tutorial step 7 guides one jack at a time (the caller's blinking jack, then the marked jack). Cords returning to the tray take the first free tray spot from the left (no fixed spot per cord, never two in one spot). A circuit left plugged still connects only when the card is taken, with no "ready" hint, so the book step stays. **Third playtest (lead, 2026-10-20):** too many lights; only the taken call's caller jack blinks, at a peak of 50–60% brightness; waiting callers stay dark; the hover flare on a ringing card stays as a preview. | decided (lead, 2026-10-20); built (fix pass 3: the call in hand blinks 0.06–0.55 over 0.5 s, waiting callers dark, the hover flare peaks at 0.55; tutorial arrows place from the hint's and holes' goals, so they never flip while sliding); earlier (10b + fix passes 1 and 2: waiting 0.04–0.28 over 2.2 s, in hand 0.30–1.0 over 0.5 s with a 0.92 halo, the hover flare through `CableInputController.HoverTarget`, tutorial stages `PluggingCaller`/`PluggingRecipient`, tray spots filled first-free by `RefreshTraySpots`: `SocketLamp.BlinkingBright` for the call in hand, steady only for the mark; tutorial step 3 "This blinking light is the caller's line..." with the later steps renumbered; the row hover tint `PhoneBookView.HoverAt`, no raycast target) | 10b |
| D59 | Speed and regulars (lead, 2026-10-20). **Quick-connect tip:** connecting a call within a few seconds of taking its card pays a tip, shown as a small "Quick! +X¢" popup; the seconds and the tip are fields in `Economy.json`. **Operator's notebook:** a shop upgrade (one `Economy.json` entry with its own price); once bought, a caller whose recipient you have connected before shows that recipient's jack on the card. The book is not docked beside the board for now (superseded by D78: the clipboard). | decided (lead, 2026-10-20); the notebook is on hold (lead, 2026-10-21); the tip is built (10c): `QuickConnectTip` (`Economy.json` `quickConnectSeconds` 5, `quickConnectTipCents` 5; 0 turns it off), paid by `EarningsService` right after the fee on a call's first connection, multiplied by More Profit like the fee; `Call.SecondsInHand` counts game time while taken or connecting; a "Quick! +X¢" pill (`TipPopup`) under the card's fee popup | 10c |
| D60 | Sound and feel (lead, 2026-10-22). Every item on the orchestrator's list is in: sounds S1–S16 and visuals V1–V14 (Plan §9, Step 11 overview). They are split into **11a** (gather the sounds: the orchestrator with the lead), **14** (wire them), **11c** and **15** (visual feedback). **Ringing:** a normal call keeps its single bell when its card arrives, with no reminders (as built; this replaces 5.11's "ringing loop while any call waits"). An **emergency** rings its own alarm bell when it arrives and again every few seconds while it waits, and in its last 10 s a clock ticks, speeding up. **Sources:** Kenney's CC0 packs and ElevenLabs sound effects (the lead's key is in `ELEVENLABS_API_KEY`, never in the repo). The lead is on the free plan, so the Credits screen names kenney.nl and elevenlabs.io. A second jazz track comes from a free library that needs no credit. `Docs/AudioSources.md` lists every clip's source and licence. Caller face reactions need new art; a bounce and a tint stand in until then. | decided (lead, 2026-10-22) | 11a, 11c, 14 and 15 |
| D29 | Merge into one scene. The `Game.unity` + `Level.unity` split is temporary; it keeps the actively edited board scene conflict-free. Once the cord work settles, move the `Game` object and the `GameUI` prefab into one scene, then remove `Game.unity` and the additive loading. Do it at the latest before the first Web build. **Done as D38 (2026-10-18).** | done | 9 |
| D61 | **Direction** (lead, 2026-11-07). The game mixed narrative, sim, cosy and incremental and none carried it. The **busy operator is the heart**: pressure, mistakes that cost, a score. Cosy is the skin (the board, the jazz, the faces). Incremental is demoted: money is the score, unlocks come by schedule. Narrative is framing, delivered between days, where there is time to read; nothing mid-shift needs reading. See §12.2. | decided (lead, 2026-11-07) | 13a |
| D62 | **Days.** Play runs in shifts of about 3 min of game time with a punch clock, closing time, a day card (earned, connected, hung up, missed, complaints, best streak, rating, headline, tomorrow's unlock) and a rising pace per day. The day count is saved; a reload starts the current day with an empty board. See §12.3. | default (lead liked, 2026-11-07); the shift length to confirm | 13b |
| D63 | **Patience.** Regular callers hang up after their patience (`patienceSeconds` per call, a default in `Economy.json`, about 40–60 s early): an amber ring, a "Hung up" stamp, no pay, no fine, back later. The queue skips a call while its caller is in any live call, as caller or recipient; a call to a busy recipient is allowed but rare, shown as "Line busy", and its hang-up is no complaint (lead, 2026-11-09). Supersedes "regular calls wait as long as needed" and refines D52's "a busy line waits on its card". See §12.4. | default (lead, 2026-11-07); the numbers to confirm | 13a |
| D64 | **Emergencies** respond in about 30 s (D37's first number), fine about one good call's pay, pay above regular calls, and get the alarm bell and the clock tick (S1, S2). Supersedes the 140/110/90 s and 50¢–$1.50 of the 2026-10-19 balance (data only). | default (lead, 2026-11-07); the numbers to confirm | 13a |
| D65 | **The mystery upgrade, the savings bar and the victory scene are removed** (lead, 2026-11-07: "throw it completely"). The game is **endless with horizons**: days, a visible next unlock, a soft "first week done" milestone on day 7, no cutscene. Supersedes D14 and closes D15; D41's pinned bottom button goes with the shop's rework (D66). | decided (lead, 2026-11-07) | 13g |
| D66 | **Unlocks by schedule, money as score.** `Economy.json` gets a day `schedule` (cords, board stages with their pools, chores, desk items, residents, quotas, challenges) in place of bought `upgrades`; "Tomorrow: ..." is always shown. The income multipliers are cut. The shop panel either shows what's unlocked and what's next, or leaves the board. See §12.10. | default (orchestrator, lead liked); the panel's fate and cutting the multipliers need the lead's yes | 13g |
| D67 | **Score and streak.** Earned per shift is the score; a streak of correct connections with a rising bell and a pay bonus, broken by a wrong number, hang-up or miss; the quick-connect window shown on the card; rush hour in the last minute (×2 pay, faster calls); "Just in time!" for an emergency connected in its last seconds; a day rating; the best shift on the menu; one daily challenge. See §12.5. | default (lead: "I like almost all", 2026-11-07) | 13d |
| D68 | **Stakes** in three layers (lead asked for real stakes, 2026-11-07): a wrong number **burns the cord** (fixed by the electrician); hang-ups, wrong numbers and misses are **complaints** (3: the supervisor calls; 5: "Sent home"); a missed **quota** is a written **warning**, cleared by beating the next quota; **three warnings: fired**, the career ends with a career card and a new one keeps the wall and the board. Rules: warn before punishing, hit the hands before the score, one good day recovers. Supersedes "no lose condition" at the career level only; no game over inside a day. See §12.6. | default; **firing needs the lead's yes** | 13e, 13f |
| D69 | **Chores** (a playtester, 2026-11-07): punch clock (with overtime), intercom for the electrician, pneumatic mail, register lever, wiping the glass. Each has a consequence or a payoff, one gesture and a sound; at most two or three active; they arrive one at a time through the schedule. See §12.7. | default (lead: "we could try all that") | 13b, 13e, 13f, 13h |
| D70 | **Nerves** (lead, 2026-11-07): a meter that rises with chaos and shakes the hand; coffee or water (small calm), a cigarette (big calm, smoke on the glass), the flask (full calm, a confidence bonus, then a swaying board and a risk). Never moralised; the assets are swappable for coffee and cough syrup because tobacco and alcohol raise age ratings outside itch. See §12.8. | default | 13i |
| D71 | **The wall** (lead, 2026-11-07): the clock, the calendar, frames for a week and a month worked and for feats, the warnings, desk items by schedule. Hung at day end, off the play area. See §12.9. | default | 13g |
| D72 | **Narrative between days** (lead, 2026-11-07): one-line ringing messages; no conversations during a call; the story on the day card (headlines, a new face); story calls once per career and in order, filler loops; a story bible (`Docs/Story.md`) approved by the lead before the calls are rewritten. **Added 2026-11-09:** arcs are chains of beats with a headline each and a holding line per arc, no branching; the day card shows the headline of the beat that resolved last, else the holding line, else a generic line. See §12.11. | default | 13j |
| D73 | **Failure is loud and funny** (the one streaming idea kept, lead 2026-11-07): rush-hour chaos, hang-up stamps, short circuits. Chat-named residents, shareable day cards and polls are dropped. | decided (lead, 2026-11-07) | 13d, 13e |
| D74 | **No deadline; the loop before polish** (lead, 2026-11-07). No exhibition. 13a–13j come before 14, 15, 16 and 17; 11c's fix passes 4–6 are committed first. | decided (lead, 2026-11-07) | now |
| D75 | **Style: two reds, two golds, three fonts, one paint box** (lead, 2026-11-09). Alarm orange-red means "hurry" (emergencies only); Error crimson means "mistake" (wrong numbers, fines, misses). Money gold is only for money and rewards; brass is for decoration; big titles on screens without money use a bright title gold (fix pass 1, after light brass looked vague). Boecklins Universe is for big titles; Della Respira for short period labels (stamps, signs, name plates, menus); LiberationSans for anything read fast or holding numbers. One shared `GameStyle` asset holds the named colours, font roles and timings. See StyleGuide §3–§6. | decided (lead, 2026-11-09) | 12 |
| D76 | **Jack hover lights** (lead, 2026-11-09). Red only on a live call's jack under a held cord, where a drop would cut the call off (a drop on a taken jack replaces its cord). Amber under a held cord on every other open jack, and under an empty hand on a jack holding one end of an unfinished cord. Nothing otherwise; a live call's jack stays green until a held cord hovers it. See StyleGuide §4. | decided (lead, 2026-11-09) | 12 |
| D77 | **Residents and the call director** (lead, 2026-11-09: "a good change"). The resident is the unit of content: `Residents.json` (grown from `Book.json`) is the one list of people, read by the book and the rules; calls name people by id and take their jacks from them; a runtime `Resident` holds state between calls; everyday calls are generated from `Errands.json` templates; a call director in the Domain decides what rings next, with its knobs in `Economy.json`. Story beats stay hand-written. **Nobody calls while on a call**; a call to a busy recipient is allowed but rare, and a hang-up over a busy line is no complaint. Supersedes D53's "the rules never read the book" and the pool loop of D52. See §12.14 and §12.4. | decided (lead, 2026-11-09) | 13c |
| D78 | **The clipboard book** (lead, 2026-11-10). The book becomes a vertical clipboard docked at the right, covering no jack; the art exists; it takes the shop panel's place, so its layout follows D66. Rows, order and the row click stay. Supersedes the "not docked" notes in D58 and D59. See §12.15. | decided (lead, 2026-11-10) | 13k |
| D79 | **One click frees a finished cord** (lead, 2026-11-10). A click on either plug of a cord whose call is over pulls both plugs and puts the cord in hand; a live call's plug keeps D07's hold. A right-click to leave a cord dangling from one jack is open, to try later. See §12.15. | decided (lead, 2026-11-10); the right-click open | 13a |
| D80 | **Who is asked for, and cards at the top** (lead, 2026-11-10). Every message opens with "Connect me to <b>Name</b>." (a data pass in 13a; composed by the card from the resident's name from 13c on). New cards arrive at the top and push the others down. See §12.15. | decided (lead, 2026-11-10) | 13a, 13c |
| D81 | **Tutorial: one step now, the rest later** (lead, 2026-11-10). 13a adds the step that frees a finished cord; the reorder, the pay badge and the why of money wait for a tutorial rework after step 13, when the gameplay exists. See §12.15. | decided (lead, 2026-11-10) | 13a; after 13 |

## 11. Glossary
- **Jack / socket / line:** a hole in the board, labelled like `C4`. A line is a jack with a subscriber on it.
- **Cord / cable:** a two-plug cord kept in the tray. "Cable" is the name in the code.
- **Patch:** to plug a cord's ends into two jacks.
- **Circuit closed:** both plugs of a cord are in jacks.
- **Call in hand:** the one taken call (Taken or Connecting) that the next closed circuit is judged against.
- **Half-patched:** a cord with exactly one plug in.

## 12. The loop redesign (lead, 2026-11-07 and 2026-11-08)

### 12.1 Why
The playtest of 2026-10-25 (a friend, and the lead's own account) found: no point and no motivation; boring soon after the first connection; people calling while already on a call; callers who never leave, so no pressure; emergencies that don't feel like emergencies; the mystery upgrade forgotten right after the tutorial and not rewarding when bought; money nobody wanted more of; call messages that feel random and aren't worth reading. The orchestrator's reading: no goal, no pressure, nothing that changes as you play, one real bug (the queue checks callers' names only, never recipients or jacks), and gags with no setup or payoff.

### 12.2 The heart and the ratio (D61)
The **busy operator is the heart**: pressure, mistakes that cost, a score. **Cosy** is the skin: the matte board, the jazz, the faces. **Incremental** is demoted: money is the score, unlocks come by schedule. **Narrative** is framing, delivered between days where there is time to read; during a shift nothing needs reading. The sub-genres only clash in the same minute, so each gets its own time slot. Failure is **loud and funny** (D73): short circuits, hang-up stamps, rush-hour chaos; never quiet, never a dead end.

### 12.3 Days (D62) [13b]
- A shift is about **3 min** of game time (confirm). The **punch clock** on the wall starts it (punch in) and ends it (punch out at closing time) [13b]; overtime is in 12.6.
- The **wall clock** (V11) shows shift time and the **calendar** the day number. Both stop on pause.
- **Closing time** clears the board: live calls end without pay or complaint (a "Closing time" stamp) and cords return to the tray.
- The **day card**: earned (the score), connected, hung up, missed, complaints, best streak, rating, the headline [13j], frames hung and warnings given [13f, 13g], "Tomorrow: ..." [13g], and a button for the next day. Autosave on the card.
- Saved: the day number and the career's totals. A reload starts at the beginning of the current day with an empty board.
- Pace rises gently with the day: call arrival, patience and quota, all in `Economy.json`.
- **Rush hour** [13d]: the last minute of the shift (confirm): calls come faster and pay double; the music picks up.

### 12.4 Pressure (D63, D64) [13a]
- **Patience.** Every regular call has `patienceSeconds` (a per-call field in `Calls.json`, with `defaultPatienceSeconds` in `Economy.json`; about 40–60 s early, confirm; 0 means it waits forever, for the tutorial call). The card shows it on the existing countdown ring in the attention amber, not the alarm red, with a gentle pulse in the last seconds. At 0 the call ends as **HungUp** (a new `CallEndReason`): a "Hung up" stamp in the neutral brown, no pay, no fine; the call returns later (12.6: crankier). Replaces "regular calls wait as long as needed".
- **Emergencies.** Response time about **30 s** (confirm), a fine that hurts (about one good call's pay, confirm), pay above regular calls, the alarm bell and the clock tick (S1, S2 from 11a; wired in 14, or in 13a if the picks are already imported). The 11c visuals stay. An emergency uses its response time, never patience.
- **The queue rule** (lead, 2026-11-09; refines D52): **nobody calls while on a call.** `CallQueue` skips a call while its caller's jack is the `from` or `to` of any live call. Today's name check catches only callers who are calling, not callers who are being called, which is the bug playtesters saw: Agatha, on the line with the doctor, rings the police. A skipped call comes round again; if everything is skipped the slot stays empty. **A call to a busy recipient is allowed**, because it is real and it creates a decision: the card shows "Line busy" until the recipient's jack is free, the caller waits on their patience, and the player can hold the current call (D07) to serve them. A hang-up over a busy line is not the operator's fault and counts no complaint (13e). The director (§12.14) keeps these rare: at most one busy-recipient card at a time (confirm). A jack can also be blocked by a stale plug (D45); pulling it is the player's job.
- **A denser start.** Early calls last **10–15 s** (confirm) instead of 25, and `startingCords` is 1 or 2 (confirm). The dead time after the first connection goes.

### 12.5 Score and streak (D67) [13d]
- **Money earned in the shift is the score.** Nothing spent lowers it.
- **Streak:** consecutive correct connections without a wrong number, hang-up or miss. Each step pitches the connection bell up and pay gets a streak bonus (`Economy.json`); a break plays a thud and the counter drops to 0. Where the counter lives (the caller panel's top or the wall) is for the brief.
- **Quick-connect window** shown shrinking on the card (D59's tip stays); three quick connections in a row: "Hot hands".
- **Near miss:** an emergency connected in its last 2 s (confirm) gets "Just in time!" and extra pay.
- **Rating** from "Rough day" to "Flawless", from earned against the quota and the complaints (formula in `Economy.json` or Domain constants, with tests).
- **Best shift** on the main menu: "Best shift: $48.20, Day 6".
- **Daily challenge:** one a day, told by the morning mail: "No wrong numbers", "Connect every emergency", "Streak of 8". A bonus and a stamp on the day card.

### 12.6 Stakes (D68) [13e within the day, 13f across days]
Three layers and three rules: **warn before you punish**, **hit the hands first, the score second**, **one good day recovers**.
- **Burnt cord** [13e]: a wrong number's short circuit leaves the cord dead and smoking in the tray for the rest of the day (or 30 s early on, confirm). The **electrician** fixes it: crank the intercom (a chore), he arrives after about 10 s, a small fee, a longer wait each time he is called.
- **Angry callbacks** [13e]: a hung-up caller comes back with shorter patience, a cross face and no tip. A second hang-up from the same person is a complaint at once.
- **Missed emergency** [13e]: the fine, a complaint, and the supervisor's line lit for the next minute.
- **Complaints** [13e]: hang-ups, wrong numbers and misses, shown as a row of stamps while the day runs. At **3** the **supervisor calls**: a special card that must be answered and holds a cord and the player's attention for a while. At **5** the day ends early: "Sent home", the quota missed.
- **Quota** [13f]: set by the morning's pneumatic mail, rising gently with the day. A missed quota puts a **written warning** on the wall; beating the next quota removes one. **Three warnings: fired.** The career ends with a career card (days worked, best shift, frames). A new career keeps the frames as history and the unlocked board, and skips the tutorial.
- **Overtime** [13f]: at closing time the player may keep working; callers get crankier every minute and a late punch-out earns a complaint after N minutes (confirm). Overtime pay counts for the score.
- "No lose condition" is superseded at the career level: no game over inside a day, but a career can end (confirm).

### 12.7 Chores (D69) [13b punch clock, 13e intercom, 13f mail, 13h the rest]
Every chore has a consequence or a payoff, one gesture and a sound. At most two or three are active in a day, and they arrive one at a time through the schedule (12.10), each taught by arriving alone.

| Chore | Gesture | Why it matters | Step |
|---|---|---|---|
| Punch clock | click to punch in and out | starts and ends the shift; overtime | 13b, 13f |
| Intercom | hold or crank to call the electrician | fixes a burnt cord, for a fee and a wait | 13e |
| Pneumatic mail | open the capsule | brings the quota, the challenge, warnings, headlines and frames; later mid-shift slips | 13f, 13j |
| Register lever | pull | banks the drawer into the counter and pays the streak bonus; unbanked money at closing counts half (confirm) | 13h |
| Wiping the glass | drag across | smoke from a short circuit and steam in rush hour hide jacks until wiped | 13h |

### 12.8 Nerves (D70) [13i]
- A **nerves** meter rises with hang-ups, emergency bells and rush-hour minutes and falls slowly on its own. Low nerves do nothing. High nerves shake the drawn hand, make plugs want a second try and flutter the book's pages. Funny, not fatal.
- **Coffee or water:** a one-second hold on the cup; calms a little; no downside but the second. Taught first.
- **A cigarette:** calms a lot, fast; the smoke hangs over the board and fogs the glass (12.7).
- **The flask:** calms everything and gives a confidence bonus (a bigger streak multiplier for a minute); then the board sways and the hand drifts; a second nip in one shift risks a wrong plug and, if the supervisor calls, "Sent home".
- Never moralised: the flask's downside is the swaying board, not text. Tobacco and alcohol raise age ratings outside itch, so they are a pair of swappable assets (coffee and "cough syrup, the strong kind") that change no rules.

### 12.9 The wall (D71) [13g]
- The wall above the board is the progress screen: the clock and calendar (12.3), empty hooks that fill, the warnings (12.6) and the desk items.
- **Frames:** "Operator of the week" on day 7, "Operator of the month" on day 30 with a brass plaque; feat frames for a flawless day, a streak of 10 and every emergency connected in a day. Each is hung once, at the end of the day, with a nail-and-swing moment and a sound.
- **Desk items** by schedule: a plant, a mug, a photo of the cat after that arc, a radio that adds the second music track.
- Rules: small, off the play area, covering no jack, lamp or card at any board size; hung at day end, never mid-shift; the StyleGuide applies.
- Art needed: the frames, the plaque, the warning slip, the desk items, the clock and calendar faces (Plan §8). Placeholder shapes until drawn.

### 12.10 Progression and unlocks (D66) [13g]
- The **unlock schedule** replaces purchases: `Economy.json` gets a `schedule` of days, each with what arrives (a cord, a board stage with its `stagePool`, a chore, a desk item, a resident), the day's quota and its challenge. "Tomorrow: ..." sits on the day card and the wall. Whether a good rating is also needed for an unlock is to confirm.
- Cords and board growth stay the meaningful unlocks; the `extra_clients` entries become schedule days and keep their pools.
- The **income multipliers** are cut (confirm): with money as the score they add nothing the player feels.
- **The shop panel** becomes a display of what's unlocked and what's next, or leaves the board (confirm). Until 13g the shop works as before.
- Removed (D65): the `secret` upgrade, `GoalProgress`, the savings fill, the "To be continued" dialog, `VictorySequenceView` and `VictoryTownGraphic`.
- The first week is a soft milestone: "First week done" on the day-7 card, with its frame. No cutscene.

### 12.11 Narrative between days (D72) [13j]
- **During the shift:** a ringing message is one short line that says who they want, with character in the wording and nothing that needs reading. No conversations during a call: that is reading under pressure.
- **Between days:** the day card's headline ("Garfield found in the chimney. Fire brigade thanked with fish."), a new resident moving in with a portrait and one line, a character returning changed.
- **Arcs are chains of beats** (lead, 2026-11-09). Each arc is a fixed line of beats: the cat goes missing, Agatha calls the police, the police call the detective, the gnome spots the cat, the cat is found. A beat is one hand-written call with its one-line message and a **headline** for the day it resolves. Arcs don't branch; the player controls only how fast an arc moves and how the arcs interleave. One fork in one arc is a deliberate exception written as two end beats, never a system.
- **Story calls** are marked `story` in `Calls.json`: they play once per career, in order within their arc, and never loop; a wrong number or a hang-up pushes a beat back and the arc waits. The played list is saved. Generated errands (§12.14) loop and never make headlines.
- **The headline picker:** the day card shows the headline of the beat that resolved last today; with none, the **holding line** of the arc furthest along ("Still no sign of Garfield.", one per arc); with no arc running, one of a few **generic headlines** ("Quiet day at the exchange. Mr Tuttle sold a lot of sardines."). That is the whole algorithm, and it is why the story seems to answer the player.
- **The story bible first:** `Docs/Story.md` with the cast (one want and one flaw each), three to five arcs that cross (the cat is the sardine thief; the detective is useless and the gnome finds him; Violet's novel is about the town; Clive and Dorothy's lunch; Nora's overdue books), and the beats day by day for the first week. The lead approves it before any call is rewritten. About 50–60 story calls for the first week (three to five arcs of eight to twelve beats), plus a holding line per arc and five to ten generic headlines, so nothing memorable repeats.

### 12.12 Build order and what waits
13a pressure and the queue fix → 13b days → 13c residents and the call director → 13d score and streak → 13e stakes within the day → 13f stakes across days → 13g unlocks and the wall → 13h chores → 13i nerves → 13j narrative (the story bible can be written any time after 13b, without Unity). 13k, the clipboard book, is independent of the others and can be built any time after 13a; its layout follows D66. Then 14 (sounds, with the new moments added to S1–S16), 15, 16 and 17. 13c was added on 2026-11-09 (D77) and moved the later parts one letter. No deadline (D74): the loop before polish.

### 12.13 Numbers to confirm
Shift length (3 min); patience (40–60 s early); emergency response (30 s) and fine (about one good call); early call length (10–15 s); starting cords (1 or 2); rush hour (the last minute, ×2); complaints (3 the supervisor, 5 sent home); warnings (3 fired); burnt cord (the rest of the day, or 30 s); overtime complaint after N min; unbanked money at closing (half); near-miss window (2 s); the shop panel's fate; cutting the income multipliers; unlocks by day only, or by day and rating; how often a busy-recipient card may appear (at most one at a time); the share of generated errands between story beats per day; a right-click to leave a cord dangling from one jack (open, D79); whether a cut-off emergency counts as a complaint (§12.15). The lead decides; each step's brief carries its numbers, and they land in `Economy.json` or `Calls.json`, never in code (D27).

### 12.14 Residents and the call director (D77) [13c]
Agreed with the lead on 2026-11-09. Today the unit of content is the call: each line in `Calls.json` repeats its caller's name, portrait and jack, the rules know no people, and the book reads a separate file the rules never open. Nothing can be remembered about a person between calls, and a hand-written pool can't promise that nobody calls while on a call.
- **`Residents.json`** replaces `Book.json` (the same file, grown): one entry per person with `id`, `name` ("Surname, First"), `portrait`, `jack`, `profession`, `address`, `emergencyService`, `arrivesOnDay` and a few personality numbers (`patienceFactor`, `temper`, `voice`). The book on screen shows it exactly as today (D53's order and format, `contactsPerPage` kept). The rules read it too.
- **Calls name people by id:** `caller` and `to` are resident ids, and the jacks come from the residents, so moving someone to another jack changes every call at once and the book can never disagree with the cards. `name`, `icon`, `from` and `to` jacks leave `Calls.json`. The validator checks the ids, that every resident's jack is on the board, and that a story call's people have arrived by its day.
- **A runtime `Resident`** (Domain) carries state between calls: on a call (as caller or recipient), times hung up on, anger, the day of the last call. 13e's angry callbacks, 13g's arrivals by day and 13j's story read and write it. Saved: anger and hang-ups per resident id (a migration).
- **Everyday calls are generated:** `Errands.json` holds about twenty templates ("needs {thing} from {recipient}") and a relationship list (who knows whom, with a weight). The director picks a caller who is present, free and not angry, a recipient they know, and a template, and makes a `CallScript` on the fly. Generated calls never make headlines.
- **The call director** (Domain, replacing `CallQueue`'s pool loop; its knobs in `Economy.json` per day): how many calls a minute today, when the next emergency is due, which story beat is next and whether its people are free, whether a busy-recipient card may appear (at most one), who should ring back angry. Story beats come first when due; errands fill the rest. Deterministic through `IRandom`, so a test can replay a day.
- **Migration:** the 33 calls and 14 book entries move to the new shape in the same step; `Docs/BookFile.md` becomes the residents guide, and `Docs/EconomyFile.md` describes the new `Calls.json`, `Errands.json` and the director's knobs. The file rules of §3 stay: one entry per line, plain validator messages, anyone edits.
- **What stays hand-made, on purpose:** the people, their faces and the story beats. Procedural characters would throw away the hand-drawn portraits and the voice. Hand-authored people and stories, generated errands, code-driven pace.

### 12.15 Readability, hands and the book (D78–D81; lead, 2026-11-10) [13a pass 2, 13c, 13k]
The answers to the notes of the 2026-10-25 playtest (`Docs/Playtests/2026-10-25-friend-call.md`) that had no home.
- **Who is asked for (D80).** Players didn't notice the caller's name under the portrait and read the message as naming the caller; a non-native speaker was caught by the joke and looked up "banker". So every message opens with a fixed lead-in, "Connect me to <b>Violet Fairweather</b>.", and the flavour follows. In 13a this is a data pass over `Calls.json`, and the validator notes a message without the lead-in; from 13c the card composes the line from the recipient resident's name and `message` holds only the flavour, so the name can never be wrong.
- **Newest card at the top (D80).** New cards come in at the top and push the others down; a gap closes from above. The tester missed an emergency that arrived at the bottom under the others.
- **One click frees a finished cord (D79).** Today a finished call's cord is pulled plug by plug, which is realistic and annoying. A click on either plug of a cord whose call is over pulls both plugs and puts the cord in hand, as if picked from the tray; the player then plugs it into the next call or returns it to the tray. A plug of a live call keeps D07's hold. A right-click to leave a cord dangling from one jack is an open question (§12.13), to try later.
- **Tutorial (D81).** Only one step is added now: freeing a finished cord, taught with the one-click pickup, because most players never discover it and get stuck with no cords. The click-through at the blinking-light step, the pay badge and the why of money wait for a tutorial rework after step 13, when the gameplay it has to teach exists.
- **The clipboard book (D78).** The book becomes a vertical clipboard docked at the right, where it covers no jack, so a lookup no longer hides the board. The art exists. It takes the shop panel's place, so its layout follows D66's answer on the shop. Rows, order and the row click (D58) stay. Supersedes the "not docked" notes of D58 and D59.
- **Reading the board.** The tester couldn't tell from the board who was free: a connected call's jacks glow green, which he didn't read as "busy". No change now: the "Line busy" card (12.4) says it where the player looks, and step 12's hover lights already mark a live call's jack red under a held cord. Revisit after 13a is played.
- **Pattern-breaking calls** (the cat lady asks for the doctor) need no feature: the director's relationship weights (12.14) produce an unexpected pair now and then.
- **A cut-off emergency** is not fined (D51). With 13e it can count as a complaint instead, which meets the lead's expectation without a money penalty (confirm).

#### Merge integration: purchased cords and first-order guidance (2026-10-18)

Purchased cords join the current first-order guide through CordBridge.CableAdded. During the cable-hint step their tray previews pulse too; picking up any original or purchased cord clears all hints. Purchases never restart a completed or cancelled guide. The merged composition supplies both Notepad notes and the money counter to the call queue, and keeps active-call socket highlighting for all cords.
