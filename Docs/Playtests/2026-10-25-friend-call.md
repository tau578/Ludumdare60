# Playtest: recorded call with a friend (2026-10-25)

A long voice call in which a friend played the itch Draft build while the lead watched and asked questions, and the lead then streamed the current `main` version. Written up on 2026-11-09 from the transcript. This playtest sent the game back to its loop: the loop redesign in Design §12 (D61–D74) and steps 13a–13k in Plan §9 grew out of it.

## The recording

| | |
|---|---|
| Recorded with | Meetily (github.com/Zackriya-Solutions/meetily), local recording and transcription |
| Where | `~/Movies/meetily-recordings/Meeting 25_10_26_13_37_37_2026-10-25_12-37/` on the lead's machine, outside the repo: `audio.mp4`, `transcripts.json`, `metadata.json` |
| Length | 2 h 14 min (8050 s); the game part runs from about minute 11 to the end |
| Language | Russian; the transcript has 1,241 segments, timestamps, no speaker labels, and stray mis-recognised fragments in other languages |
| Who | The lead; one friend who played (a casual gamer, not a native English speaker; called "the tester" below); a second friend who watched the first minutes and left |
| Build played | The itch Draft build of 2026-10-20 (Step 10), a few steps behind `main` at the time: no surname order in the book, no 11c visual feedback. The lead streamed the current `main` from about 1 h 56 min |

Moments worth re-listening to, as minutes into the recording:

| Time | What |
|---|---|
| 23:25 | The "connect A1 and B3" confusion and the tutorial click-through |
| 29:50 | The tester explains his lookup process and goes for "banker" instead of Dorothy Quinn |
| 51:30 | A missed emergency under the "New order!" stamp; the newest card at the bottom |
| 54:50 | The lead's own "it gets boring" account |
| 78:35 | The pattern-break idea (a regular caller asks for someone unexpected) |
| 86:40 | Upgrades don't pull; the mystery upgrade is forgotten |
| 96:08 | "Early game fine, mid game missing, endgame absent" |
| 110:46 | Fines, getting fired, narrative threads |
| 120:11 | The same person on two calls, seen on the stream |

## Onboarding and readability

**The tutorial can be clicked through at the one moment that matters.** The tester got "connect A1 and B3" and asked why B3, because he had clicked past the step that explains the blinking caller light. The lead said he would reorder it. Several earlier testers reacted the same way, so the sequence is the problem, not the players.

**Three things are missing from the tutorial**, named by the lead during the call:

- Pulling a plug out of a finished call to free the cord. The lead's estimate: most players never discover it and get stuck with "no cords left".
- What the card's pay badge means: the fee once, then the rate per second, and where the money goes. The tester read "25¢ here, 5¢ there" as noise.
- What More Clients does, and why anyone wants money at all.

**The bubble text sends people to the wrong jack.** The tester looked up "banker" for Dorothy Quinn and "fire" for Fairweather. The bold name is the right direction, but a non-native speaker reads the whole sentence and is caught by the joke. The lead wants it unmistakable who is being asked for. A fixed lead-in such as "Connect me to" before the bold name, or the name on its own line, would do it without killing the humour. The tester also asked for translations.

**Smaller UX notes.**

- Clicking a book row to mark the jack was the single biggest speed-up for the tester, and he only learned it because the lead told him. The spotlight step for it exists on `main`; check it survives a click-through.
- He expected the newest card at the top. New cards appear below the others and he missed an emergency because of it. The lead agreed. The emergency's countdown was also hidden behind the "New order!" stamp (fixed on `main` as V13 in 11c: emergencies show no stamp).
- The green "connected" jacks were not read as "that line is busy". He wanted to read the board, not the cards, to know who is free.
- The book: constant open and close annoyed him, but he also said it could be a deliberate skill. The lead described a vertical clipboard, already drawn, that docks right and covers the shop. D58 and D59 say the book is not docked, so that is a decision to write down.
- Surname sorting is on `main` but was not in the build he played. He hit "Higgins" on two different pages.
- Phone: everything works, the text is too small to read (in the mobile and Web polish step).

## Motivation and the loop

The important part of the call. Both agreed on it.

**It gets boring within minutes, even for a player who liked it.** The tester rated the style, music and plug sounds very highly ("10 out of 10" for the look; the plug sound could be more metallic). Once he had the mechanics, he described the game as "linear, same logic, plugging cords like a robot". The lead said the same about his own experience. The tester's phrase: the reward has to match the effort, and once the effort drops to zero nothing is left.

**Money has no purpose.** He never felt a pull toward the next upgrade, used three cords and did not want a fourth, and could not say what the mystery upgrade was or that he had clicked it. The lead's summary: early game is okay, mid game is as good as absent, endgame does not exist. This is D15 and D27 in practice. (D15 was closed afterwards: the mystery upgrade is removed, D65.)

**No pressure, no decisions.** Nothing happens if the player waits. Calls never conflict in a way that forces a choice. The original idea of overlapping calls, busy lines and choosing whom to connect first does not come through at the current call volume. The quick-connect tip did not register.

**Cosy or arcade was undecided.** A long stretch of the call. Slowing down toward reading and story makes it a narrative game. Speeding up and shortening the text makes it a clicker. The tester said this is the team's call, not his. The honest state at the time: halfway, and neither direction is felt.

## Ideas raised on the call

Grouped, with a one-line view on each.

- Regular callers who break a learned pattern: "the cat lady" calls, but this time for the doctor. Cheap, pure data in `Calls.json`, and it creates real attention play. The best idea of the call for the current build.
- A detective who rings every few minutes asking about a suspect, and small story threads across calls, ending in a spy or an affair reveal. Fits the hand-written calls. Mid-sized writing work.
- A workday structure with a wall clock and "day over" beats. A clock is already planned (V11); days would give the mid game a shape.
- A lose state: fired after three missed emergencies, or spending more than you earn. The design at the time said no lose condition, so this is a decision, not a tweak.
- Routine side tasks: a cash-register lever, a punch clock, pneumatic mail, wiping the glass when it gets busy, an intercom to call an electrician for a burnt cord. The tactile moments the tester loved. Pick one or two, not all.
- A right-click to leave a cord dangling from one jack, and one click on a finished call's plug returning the cord to the tray (the lead's proposal). Both make the game faster and more casual, so they depend on the direction.
- A longer game by adding board and lowering per-second rates. The tester asked whether more of the same would be fun. It would not.
- A multiplier for several simultaneous calls, a leaderboard, more rows beyond F, a shouting boss icon.

## Two things to check in the code

- **A person can be on two calls at once.** The tester saw Agatha Higgins talking to her cat while also calling the fire station, and Winston Tuttle on a call while calling someone. The queue skips a call whose caller is already live, but `HasLiveCaller` in `Assets/Scripts/Domain/Queue/CallQueue.cs` compares caller names only, not recipients. When Garfield calls Agatha, Agatha is still free to be dealt as a caller. The lead promised a fix (the queue fix in step 13a).
- **Cutting off an emergency gives no fine.** The lead expected one. D51 says only a missed emergency is fined, so the game is right and the expectation was off. No change unless wanted.

## Where each note went

**Already on `main` or in a brief at the time:** the stamp over the emergency badge (V13), the surname order, the emergency lamp and sign styling the lead disliked on stream (11c fix passes), the wall clock (V11), phone text size and the Android check (mobile and Web polish).

**New at the time, with no home:** the tutorial reorder and the three missing steps, the "who is asked for" wording, the newest-card position, the recipient-busy queue bug, the docked clipboard book, the pattern-breaking calls, and the big one: a mid and endgame.

**Recommendation given after the call** (2026-11-07, when the lead set the priority to "make the loop fun first, polish after"):

1. Decide the direction first, in one sentence: cosy-and-story or fast-and-pressured. From the call, the tactile and story ideas lit both of them up more than speed did.
2. Make reading matter again with data only: pattern-breaking calls, two callers for one busy line, a recipient already on a call.
3. Give the money a reason to exist: upgrades that change play rather than numbers, a workday structure, a real end.
4. Add gentle pressure: callers who eventually hang up, or a queue that costs something if ignored, while keeping "no lose condition" if the design keeps it.
5. Fix the two things that break the fiction: the same person on two calls, and the tutorial click-through plus the missing "free a cord" step.

The lead's answer is the loop redesign in Design §12 and steps 13a–13k.
