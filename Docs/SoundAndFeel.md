# Sound and feel: suggestions

The orchestrator's list of missing sounds and visual feedback (2026-10-22). The lead took every item (D60). The build briefs are in [Plan.md](Plan.md) §9, "Step 11" (11a–11d), which use the same numbers: **S1–S16** for sounds and **V1–V14** for visuals.

## What the game already has

**Sounds:**
- **Calls:** a bell per new order (emergencies are pitched up), the card-take click, a cash register on the first connection, a relay click on resume, a latch for hold and a wrong-number buzz.
- **Callers:** a short mumble when a call connects, and the receiver put down when a call ends.
- **Other:** plug in and plug out; book open, close and page flip; shop buy, can't afford and secret; menu clicks; the volume cords; the victory scene's wire clicks; one jazz music track.

The Step 3 placeholder `card-take-click.wav` is already replaced by `CardTake.wav` and is unused.

**Visual feedback:** the card press squash, stamps that pop on and slide off, typed speech, the emergency countdown ring with its urgent pulse, fee and tip popups, fee coins flying to the counter, green jacks when connected, the blinking caller jack, the fine and spend popups by the counter, the tutorial spotlight and the victory scene.

## Missing sounds

| # | Moment | Now | Suggested sound |
|---|---|---|---|
| S1 | **Emergency call arrives** | The normal bell, slightly higher | A harsher double bell, like an old fire-alarm bell, repeated every few seconds until the call is taken (emergencies only) |
| S2 | **Emergency, last 10 seconds** | Silent (only the ring on the card pulses) | A clock tick that speeds up, timed with the ring's pulse |
| S3 | **Missed call and fine** | The same "receiver down" as a good call | A sad, descending muted-trumpet "wah-wah" (fits the jazz) plus coins sliding away |
| S4 | **Stamps** ("New order!", "Paid", "Wrong number!", "Missed", "Cut off") | Silent, though they pop onto the card | A rubber-stamp **thunk**, slightly varied in pitch each time |
| S5 | **Call finished and paid** | Receiver down only | Receiver down plus a light coin jingle when "Paid $X" lands; also a soft single coin tick for the live-earnings coins (V7) |
| S6 | **Cut off** (a plug pulled early) | Receiver down | A line click plus a short busy tone ("beep-beep") |
| S7 | **Quick-connect tip** | Silent | A bright hotel desk-bell "ding!" |
| S8 | **Caller talking while the text types** | The hook exists in the code but is empty | Muffled telephone-voice blips: a few syllable sounds, a different pitch for each caller (Animal Crossing style) |
| S9 | **Mystery upgrade becomes affordable** | Nothing (the event exists, nothing reacts to it) | A short "ta-da" brass sting |
| S10 | **Board growth, new cord, new caller slot** | The generic purchase sound | Board: a heavy mechanical clunk plus relays clicking. Cord: a cord rattling into the tray. Slot: a cover plate sliding open. |
| S11 | **Plug into a closed jack** | Shares the wrong-number buzz | A dull thud (`PlugBlocked.wav`, in the project but unused) |
| S12 | **Clicking a book row** (marks the jack) | Silent | A pencil tick or a short scribble |
| S13 | **Page turns** | Always the same flip | Alternate `bookFlip1` and `bookFlip2` (the second is unused) |
| S14 | **Pause open and close** | Menu click | A switch or toggle click (`switch_001/002`, unused) |
| S15 | **Victory** | Wire clicks only, the music keeps going | A brass fanfare, with the music ducked under it |
| S16 | **Background** | One jazz track, nothing else | A quiet office ambience loop (distant chatter, typewriters, room tone) and a second music track |

**Sounds in the project but unused**, which could be reused: `RotaryRingSource.mp3` (a candidate for the emergency bell), `PlugBlocked.wav`, `bookFlip2.ogg`, `metalLatch`, `metalClick`, `switch_001/002`, `toggle_001`, `click_003`, `close_001`, `error_004`, and `PlugSource`/`UnplugSource.mp3`. Several look like they come from Kenney's packs.

## Missing visual feedback

**Biggest impact (Step 11c):**

| # | Idea |
|---|---|
| V1 | **Rolling money counter.** The counter is already built as separate number wheels, but the digits jump. Make them roll like an old odometer, with a small glow when money comes in. |
| V2 | **Fines you can feel.** The money plaque shakes and flashes red, and a few coins fly *out* of the counter toward the missed card (the fee coins in reverse). |
| V3 | **Emergencies that feel urgent.** The card wobbles while waiting and shakes harder in the last 10 seconds, and a red warning lamp flashes on the board. |
| V4 | **New calls arrive with a bang.** The card slides in from the side with a bounce and shakes when it rings. Today it just appears. |
| V5 | **Wrong number.** The card does a "no" head-shake, both jacks flash red, and a little spark appears at the plug. |
| V6 | **Connection.** A small flash ring at both jacks, and a tiny wiggle in the cord if the cord code allows it. |
| V13 | **Fix:** the "New order!" stamp covers the EMERGENCY badge and its countdown (Plan §8). |

**Nice extras (Step 11d):**

| # | Idea |
|---|---|
| V7 | While a call is connected, small coins or "+¢" ticks rise from its card now and then, so you see the money flowing (cosmetic). |
| V8 | A finished call's card slides out with a final coin burst (cosmetic). |
| V9 | **Shop.** The bought button punches with a sparkle burst. Board growth: the new jacks light up in a wave. A new cord drops into the tray with a bounce. A new caller slot opens. |
| V10 | When the mystery upgrade becomes affordable, it shimmers and the shop pulses once. |
| V11 | A wall clock on the board showing game time; it stops on pause. |
| V12 | Caller faces reacting: happy when connected, cross on a wrong number. Needs new drawings; a portrait bounce and a red tint stand in until then. |
| V14 | A sparkle on the "Quick! +X¢" tip popup. |

## Where the sounds come from

**Kenney** (free, CC0, no credit needed):
- **Casino Audio:** coins and chips.
- **Impact Sounds:** thuds and the stamp.
- **Interface Sounds** and **UI Audio:** ticks and toggles.
- **Music Jingles** (85 short jingles): the "ta-da", the victory fanfare and the sad "wah-wah".

**ElevenLabs sound effects**, for what Kenney doesn't have:
- **Sounds:** the emergency bell, the clock tick, the busy tone, the desk bell, the telephone voices, the board-growth clunk and the office ambience loop.
- **How it works:** a text description becomes a sound of 0.5–30 seconds through `POST /v1/sound-generation`. A loop option makes seamless loops for ambience. The output is MP3 by default; WAV is available.
- **Cost:** 40 credits per second of sound when the length is set.
- **The lead's account:** the free plan, with 10,000 credits a month (about 250 seconds of sound), renewing on the 15th. The whole list, with 2–3 versions per sound, needs about 5,000 credits.
- **Licence:** on the free plan, commercial use requires a credit to elevenlabs.io, so the Credits screen will name it. Paid plans need no credit. If the game ever becomes paid or takes donations, check the plan first.
- **The key** is in the environment variable `ELEVENLABS_API_KEY`. It is never written into the repo or printed.

**A second music track:** from a free library that needs no credit. The current track's filename suggests it came from Pixabay.

Sources: [ElevenLabs API: sound generation](https://elevenlabs.io/docs/api-reference/text-to-sound-effects/convert.md) · [ElevenLabs sound effects](https://elevenlabs.io/docs/overview/capabilities/sound-effects) · [ElevenLabs commercial use FAQ](https://elevenlabs.io/sound-effects/commercial) · [Kenney audio packs](https://kenney.nl/assets/category:Audio)

## Build order
1. **11a, gather the sounds.** The orchestrator, with the lead. Download Kenney, generate the ElevenLabs candidates and put them on a listening page. The lead picks; the picks go into `Assets/Audio/` and `Docs/AudioSources.md`.
2. **11b, wire the sounds** into the game: S1–S16 and the Credits line.
3. **11c, main visual feedback:** V1–V6 and V13.
4. **11d, extra visual feedback:** V7–V12 and V14.
