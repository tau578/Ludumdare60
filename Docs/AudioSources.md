# Audio sources

Every sound in `Assets/Audio/`, where it came from and its licence. Add a row whenever a sound is added (Step 11a, D60).

**Licences:**
- **ElevenLabs** sound effects are generated on the free plan. Commercial use needs a credit to elevenlabs.io, so the Credits screen names it (14 adds the line). A paid plan needs no credit; check the plan before the game ever becomes paid or takes donations.
- **Kenney** packs are CC0: free, no credit needed.
- **Pixabay** music is under the Pixabay Content License: commercial use is allowed and no credit is required. The game credits the current track anyway (`Credits.txt`).

**How the ElevenLabs clips were made:** the prompt and length are below. Each clip was trimmed of silence at both ends and levelled to a -1 dBFS peak as 16-bit WAV (`Builds/SoundTools/level.py`). The game sets each sound's volume. The candidates and prompts are in `Builds/SoundCandidates/` (git-ignored), so a clip can be regenerated.

## Added in Step 11a (2026-11-08)

| File | Slot | Used for | Source |
|---|---|---|---|
| `Sfx/EmergencyBell.wav` | S1 | Emergency arrives, and again every few seconds while it waits | ElevenLabs sound effects, 2.0 s: "Old telephone exchange alarm bell, urgent rapid hammer striking a brass gong, harsh metallic double ring, close microphone" |
| `Sfx/ClockTick.wav` | S2 | An emergency's last 10 s, ticking faster | ElevenLabs sound effects, 0.5 s: "Single loud tick of a mechanical kitchen timer, short click, dry" |
| `Sfx/MissedSting.wav` | S3a | A missed emergency | ElevenLabs sound effects, 3.0 s: "Sad trombone wah wah wah waaah, four descending notes, muted brass, comedic failure sound" |
| `Sfx/CoinsAway.wav` | S3b | A fine is taken | ElevenLabs sound effects, 1.5 s: "Small pile of coins scooped and dragged away across a wooden table, metallic clinks fading" |
| `Sfx/StampThunk.wav` | S4 | Every stamp (vary the pitch slightly) | ElevenLabs sound effects, 0.6 s: "Rubber stamp hitting paper on a wooden desk, one short soft thud" |
| `Sfx/CoinJingle.wav` | S5 | A call finishes and pays | ElevenLabs sound effects, 1.2 s: "A few coins clinking together in a hand, short cheerful jingle, close" |
| `Sfx/CoinTick.wav` | S5b | Small coins from connected calls (15) | ElevenLabs sound effects, 0.5 s: "One small coin clink, bright and soft, very short" |
| `Sfx/CutOff.wav` | S6 | A plug pulled early: line click and busy tone | ElevenLabs sound effects, 2.0 s: "Telephone handset hang-up click then three short busy tone beeps, vintage, lo-fi" |
| `Sfx/TipBell.wav` | S7 | The quick-connect tip | ElevenLabs sound effects, 1.0 s: "A single ding of a brass desk bell, bright, clear, loud" |
| `Sfx/CallerVoiceA.wav` | S8 | Caller talking (a man's voice); one of three voices for different characters | ElevenLabs sound effects, 2.0 s: "Muffled gibberish voice of a man speaking quickly through an old telephone earpiece, unintelligible syllables, lo-fi, tinny" |
| `Sfx/CallerVoiceB.wav` | S8 | Caller talking (a woman's voice) | ElevenLabs sound effects, 2.0 s: "Muffled gibberish voice of a woman chattering through a vintage telephone, unintelligible syllables, tinny, lo-fi" |
| `Sfx/CallerVoiceC.wav` | S8 | Caller talking (cartoon babble) | ElevenLabs sound effects, 2.0 s: "Cartoon gibberish babble, short mumbled syllables, like a character talking in a video game, muffled telephone filter" |
| `Sfx/BoardGrowth.wav` | S10a | The board grows | ElevenLabs sound effects, 2.5 s: "Big industrial switch thrown with a heavy clunk, then many relays clicking in sequence, old electromechanical machinery powering up" |
| `Sfx/CordIntoTray.wav` | S10b | A new cord in the tray | ElevenLabs sound effects, 1.5 s: "Telephone patch cord with a metal plug dropped into a wooden tray, rattling cable and a small metal clack" |
| `Sfx/SlotOpen.wav` | S10c | A new caller slot opens | ElevenLabs sound effects, 1.0 s: "Little brass hatch sliding open, quick metallic slide and latch click" |
| `Sfx/PencilTick.wav` | S12 | A book-row click marks a jack | ElevenLabs sound effects, 0.5 s: "Quick pencil tick mark on paper, short graphite scratch, close" |
| `Sfx/Fanfare.wav` | S15 | A big reward (was the victory; the redesign removes the victory scene) | ElevenLabs sound effects, 4.0 s: "1930s big band brass fanfare, triumphant celebration, short victory flourish with trumpets and trombones" |
| `Sfx/JackBlocked.wav` | S11 | A plug pushed into a closed jack (replaces the shared wrong-number buzz) | ElevenLabs sound effects, 0.5 s: "Dull muted thunk of a metal plug pushed against a closed socket, clearly audible, short, no ring" |
| `Sfx/PauseOpen.wav` | S14a | The pause menu opens | ElevenLabs sound effects, 0.5 s: "Single creamy mechanical keyboard switch press, low pitched soft thock, dampened" |
| `Sfx/PauseClose.wav` | S14b | The pause menu closes | ElevenLabs sound effects, 0.5 s: "Soft mechanical keyboard key release, muted click, clearly audible, single, warm" |
| `Music/AmbienceExchange.wav` | S16a | Ambience loop: a telephone exchange room. A seamless 20 s loop: levelled to a -6 dBFS peak, **not trimmed** | ElevenLabs sound effects, loop, 20 s: "1930s telephone exchange room ambience, distant operators murmuring, occasional switchboard plug clicks and very soft distant bell rings, warm room tone, continuous, no music" |
| `Music/AmbienceRain.wav` | S16a | Ambience loop: a cosy office with rain on the window. A seamless 20 s loop: levelled to a -6 dBFS peak, **not trimmed** | ElevenLabs sound effects, loop, 20 s: "Cosy old office room tone with gentle rain on the window, faint distant murmurs, a clock ticking far away, calm, continuous, no music" |
| `Music/AmbienceOffice.wav` | S16a | Ambience loop: a busy 1930s exchange office. A seamless 20 s loop: levelled to a -6 dBFS peak, **not trimmed** | ElevenLabs sound effects, loop, 20 s: "Busy 1930s telephone exchange office ambience, distant muffled operators talking, occasional typewriter clacks, soft room tone, no music" |
| `Music/DancingInSmoke.mp3` | S16b | Music rotation | Pixabay: "Dancing in Smoke" by mcanden (2:41), Pixabay Content License |
| `Music/SwingBabySwing.mp3` | S16b | Music rotation | Pixabay: "Swing Baby Swing - 1930s Upbeat Swing Music" by kaazoom (1:58), Pixabay Content License |
| `Music/FootTappersClub.mp3` | S16b | Music rotation | Pixabay: "The Foot Tappers Club - 1930s Upbeat Swing Music" by kaazoom (2:30), Pixabay Content License |

## Already in the game

| File | Source |
|---|---|
| `Music/evolvingvibes-bring-back-the-swing-upbeat-jazz-617230.mp3` | Pixabay: "Bring Back the Swing" by Evolving Vibes (`Credits.txt`) |
| `Sfx/bookOpen.ogg`, `bookClose.ogg`, `bookFlip1.ogg`, `bookFlip2.ogg`, `metalClick.ogg`, `metalLatch.ogg` | Kenney, RPG Audio (CC0): byte-identical to the pack's files (checked 2026-11-08) |
| `Sfx/click_003.ogg`, `close_001.ogg`, `error_004.ogg`, `switch_001.ogg`, `switch_002.ogg`, `toggle_001.ogg` | Kenney, Interface Sounds (CC0): byte-identical to the pack's files (checked 2026-11-08) |
| `Sfx/CardTake.wav`, `ConnectedRelay.wav`, `HoldLatch.wav`, `IncomingRing.wav`, `MenuClick.wav`, `PlugInsertWarm.wav`, `PlugRemoveWarm.wav`, `ReceiverDown.wav`, `WrongConnection.wav`, `PlugSource.mp3`, `RotaryRingSource.mp3`, `UnplugSource.mp3` | **Source not recorded** (added in `9eed19b`, "edited sfx") |
| `Sfx/CashRegister.wav` | **Source not recorded** (`f7918ab`) |
| `Sfx/PhoneBabble.wav` | **Source not recorded** (`2fc3810`) |
| `Sfx/SecretUpgrade.wav`, `ShopInsufficientFunds.wav`, `ShopPurchase.wav` | **Source not recorded** (`2c0d99d`) |
| `Cable/PlugBlocked.wav`, `PlugInsert.wav`, `PlugRemove.wav` | **Source not recorded** (`bf4f654`) |
| `../Content/Audio/card-take-click.wav` | The Step 3 placeholder, unused; 14 deletes it |

**Open:** whoever added the sounds marked "source not recorded" should fill in their source and licence before the game is published.

## Not picked yet

- **S9 (ta-da):** left for the loop redesign (lead, 2026-11-08). It was for the mystery upgrade, which D65 removes. The candidates are in `Builds/SoundCandidates/elevenlabs/S9-tada/`.
- The music rotation is the four tracks above plus `Bring Back the Swing`, which is already in the game.
