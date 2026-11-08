# Style guide

How everything in the game should look and move, so new pieces match the hand-painted board. **Read it before any visual work** (effects, popups, signs, lamps, menus, new UI). The art in `Assets/Sprites/` is the reference; this guide writes down what the art already does, plus what the lead's playtests taught (2026-10-22).

Draft (orchestrator, 2026-10-22). Points marked **Question** wait for the lead.

## 1. The look in one paragraph

A warm, hand-painted 1930s switchboard with an art-nouveau touch. Surfaces are **matte**: wood, felt, paper, pewter and brass. Shading is soft, with light from above, so tops are lighter and bottoms darker, and shadows are soft and fall slightly down. Glass is **frosted**: it glows from inside, milky and soft. Thin gold curls frame panels and lamps. Colours are muted and earthy. Only an alarm (red) and money (gold) are allowed to shine.

## 2. Materials and drawing rules

| Do | Don't |
|---|---|
| Matte surfaces, soft gradients, light from above | Glossy plastic, hard white specular glints, chrome |
| Frosted glass that glows from inside | Clear glass with reflections, photo-real lenses |
| Pewter and brass, at most a thin soft highlight | Shiny machined metal, bevels like app buttons |
| Thin dark-brown outlines, gold art-nouveau curls on frames | Thick black outlines, plain geometric frames |
| Soft drop shadows, offset down, dark and low alpha | Hard shadows, shadows going up or sideways |
| Small soft glows, at most about 1.5× the object | Big bloom, lens flares, light over half the board |
| **Reuse the board's own art** when something similar exists, e.g. a lamp uses the number lamps' bulb sprites | Drawing a new look for a thing the board already has |
| Small signs on the frame are brass plates set into the wood, with dark engraved letters, sized from the thing they label | Text painted straight onto the wood; bright plates that sit on top like a sticker (an ivory strip was tried and looked out of place) |

**Lessons from playtests:**
- **A glossy, photo-real lamp felt off** next to the painted board (fix pass 3, kept on the branch `step-11c-lamp-glossy-glass`). Its board-style version uses the bulb art, tinted (fix pass 4).
- **Plain modern lettering on a black plate** looked pasted on. Signs are painted into the frame in the board's font.

## 3. Palette

Sampled from the game's art and code. Use these instead of inventing new colours; they will become the named colours of a shared `GameStyle` settings file.

### Surfaces (from the art)

| Name | Hex | Where |
|---|---|---|
| Wall | `#472C1E` | The dark wall around everything |
| Frame wood | `#916344` (to `#855C42` near the edges) | The board's frame, behind the number lamps and the EMERGENCY sign |
| Shop wood | `#713F2B` | The shop panel |
| Tray wood | `#724730` | The cord tray's rim |
| Gold line | `#96881E` | The thin curls on frames and panels |
| Board felt | `#4F885C` | The jack board |
| Caller panel | `#597C5B` | The caller column |
| Card teal | `#175351` (fill), `#346A59` (name plates) | Card badges and plates |
| Tray felt | `#114B49` | Inside the cord tray |
| Money plate | `#3A5234`; digits `#234C28` | The money counter |
| Paper | `#F3E9CF` (bubble), `#EAE0C7` (stamp) | Speech bubbles, stamps, the book's pages |
| Pewter | `#909295` | Unlit number lamps |
| Lamp cream | `#E6DAB9` | A lit number lamp |
| Jack | `#31241C` rim, `#182026` hole | Jacks |
| Ink | `#33241A` | Text on paper (the bubble) |
| Cream text | `#F3E9CF` (badges), `#FFFAE8` (menu labels) | Text on dark surfaces |

### Meanings

| Meaning | Colour | Used for now |
|---|---|---|
| **Good** (connected, paid) | Green: `#7DFF9B` light, `#68F086` ring, `#26683A` ink on paper | Connected jacks, the connection ring, the "Paid" stamp |
| **Money** | Gold: `#F2D27A` text, coin `#FFDA70` / `#D49231` / `#7E4B1B`, tip `#FAE68C` | The counter amount, fee popups, coins |
| **Bad** (wrong, fine, can't) | Red: `#F14034` for light and effects on dark, `#A42626` for ink on paper, `#74121E` for "Missed" | Wrong-number flash, fines, the "Wrong number!" stamp |
| **Urgent** (emergency waiting) | Alarm red: lit `#E84A34`, unlit `#A03E32`, countdown ring `#FF5C30` | The emergency lamp and sign, the countdown ring |
| **Attention** (hover, guide) | Amber: `#F5D278` hover, `#FFD06E` lamp | Hovered jacks, the caller's blinking jack, book-row hover |
| **Neutral** (new order, cut off) | Browns: `#7B5220` "New order!", `#5C5044` "Cut off" | Stamps that are neither good nor bad |

**Question:** the code has about eight slightly different reds for "bad". The proposal is to keep two: **danger light** `#F14034` (effects and glows on dark) and **danger ink** `#A42626` (text and stamps on paper), and move the others to them over time.

**Question:** the pause title's gold (`#FFDB1F`) is brighter and yellower than the money gold (`#F2D27A`). Should titles keep their own gold, or use the money gold?

## 4. Feedback language: what each outcome looks like

The most important rule: **the player must read "good" or "bad" from colour and motion alone.**

| Outcome | Colour | Motion | Extras | Never |
|---|---|---|---|---|
| **Good** (connect, paid) | Green, gold | Smooth: expanding rings, a gentle wave in the cord, a soft settle | Coins fly to the counter; a soft glow | Shaking, red |
| **Reward** (tip, upgrade, goal) | Gold | A pop with a small bounce | **Sparkles and shimmer belong here only** | |
| **Bad** (wrong number, fine, missed) | Red | Sharp and jerky: a "no" shake, a cord jolt, a plaque shake | A short circuit (white flash, falling streaks, an arc, dark smoke); coins leaving the counter | **Stars, confetti, sparkles, gold bursts:** they read as a reward (outside playtest feedback, 2026-10-22) |
| **Urgent** (emergency waiting) | Alarm red | Flashing (2/s), wobbling, faster in the last 10 s | A ticking clock (11b) | Anything that keeps going while paused |
| **Neutral** (new order, cut off, hold) | Browns, cream | A stamp pop; nothing more | | Celebration or alarm |
| **Attention** (where to look) | Amber | A slow blink or glow | | Red, unless it is urgent |

## 5. Fonts

The game uses three fonts. All of them are the **static** TMP assets (`… SDF Static` and `LiberationSans SDF`), which hold only printable ASCII; LiberationSans also has `¢` and `—`. A character outside these needs a regenerated atlas (Plan §4, "Dynamic TMP fonts").

| Font | Role | Used now (size in 1920×1080 units, colour) |
|---|---|---|
| **Boecklins Universe** | Display: big titles only. Its decorative letters (the three-stem "M") stop being readable when small | Pause title (56, `#FFDB1F`) |
| **Della Respira** | Readable serif for menus, guide text and short labels | Menu and pause buttons (38, `#FFFAE8`); tutorial hint (30, `#3C2C1C`); book rows (24); main menu; the EMERGENCY sign (engraved capitals on brass, `#3A140C` quiet / `#480B0D` lit; slightly thickened, because Della Respira's strokes fade under about 12 px) |
| **LiberationSans** | Plain text where size and the `¢` glyph matter | Money (64, `#F2D27A`); popups (34); card badges (28, `#F3E9CF`); stamps (34); speech bubble (24, `#33241A`); the goal dialog; shop buttons (21–26) |

**Rules:**
- **Contrast:** text needs at least **4.5:1** contrast with what is behind it (WCAG relative luminance), and at least 3:1 for big titles of 36 or more. Measure it; don't judge by eye. The EMERGENCY sign failed this by eye (fix pass 5).
- **Text on the frame wood:** the wood is mid-brown, so neither black nor a saturated colour reaches 4.5:1 on it. Put short labels on a **brass plate set into the wood** (`LabelStripGraphic`, recessed), with dark engraved letters, and measure the letters against the plate. The EMERGENCY sign does this (fix pass 7). A bright ivory strip was tried first: next to the grey lamps it was the only light thing on the frame and looked out of place.
- **A state change keeps its colour family.** Change brightness or add light, rather than switching to an unrelated colour. Ivory letters turning red looked like a different sign (lead, 2026-10-22). On the brass plate the letters stay dark, and the alarm shows as red light on the plate.
- **Minimum size:** 24 units at 1920×1080. On a phone (844×390) the game is about 0.36× as big, so small board text becomes hard to read there (Plan Step 12).
- **Don't change a shared font material.** For an outline or shadow, use offset copies of the label or a material instance made once.

**Question:** in-game labels (stamps, badges, the bubble, popups) use plain LiberationSans, while menus and signs use the decorative fonts. Should in-game labels move to Della Respira for a more period look? The catch: `¢` is only in LiberationSans, so money text would stay there unless the atlas is regenerated.

## 6. Motion

Timings in use now. New effects match these instead of inventing their own.

| Moment | Time | Shape |
|---|---|---|
| Stamp pop / exit / hold | 0.38 s (overshoot 1.16) / 0.22 s / 1.5 s | Ease-out with a small overshoot |
| Card press | 0.2 s | A squash |
| New card arriving | 0.55 s, then a 0.45 s ring shake (5°) | Ease-out-back from the left |
| Wrong-number "no" shake | 0.4 s, 12 px, 3 swings | A decaying sine |
| Emergency wobble | 2° at 1.2/s; 5° in the last 10 s at the countdown pulse (3/s) | Continuous; game time |
| Emergency lamp flash | 2 flashes/s | Cosine; game time |
| Coin flight | 1 s per coin, 0.055 s apart | Arc to the target |
| Money drums | 0.15 s roll; plate glow 0.6 s | Smoothstep |
| Fine plaque shake | 0.3 s, 6 px | A decaying sine |
| Connection ring / cord wave | 0.5 s / 0.5 s, 12 px | Expanding ring; a smooth wave pinned at both plugs |
| Wrong-number flash / short circuit / cord jolt | 0.5 s / 0.6 s / 0.35 s, 7 px | Sharp flicker; streaks fall with gravity |
| Popups (fee, tip, fine) | 1 s | Rise and fade |
| Typing in the bubble | 40 characters/s | |

**Rules:**
- **Clocks:** one-off effects run on unscaled time and may finish during pause. Anything tied to a waiting call (wobble, flash, ticking, the countdown) runs on game time and **freezes on pause** (D56).
- **Short and few:** most one-offs end within 0.6 s. When several of the same effect land in one frame, show one.
- **Web:** no per-frame allocations and no mesh rebuilds while something just fades; fade with `CanvasRenderer.SetAlpha`.

## 7. Layers and placement

| Layer | Sorting order |
|---|---|
| Board, resting plugs | 0 (`SwitchboardUISorting.Cable`) |
| Wires | 1 + rank (1–6, oldest lowest) |
| Short-circuit sparks | 9 (`Spark`) |
| Open book; `GameUI` (cards, HUD) | 10 |
| Tutorial spotlight | 14 |
| Goal dialog | 15 |
| Pause menu | 16 |
| Drawn hand, held plugs | 20 |

**Placement:**
- Nothing of `GameUI` overlaps the open book.
- Effects stay inside the 16:9 play area, and nothing that isn't meant to be clicked takes clicks (`raycastTarget` off), because the cord input takes the topmost hit.
- A new board element must cover no jack, number lamp, card or speech bubble **at every board size (3×3 to 6×6)**.

## 8. Sound (Step 11 rules)

- **Volume:** not noisy. Repeated sounds get about ±5% pitch and a rate cap. Nothing is louder than the cash register.
- **Channels:** effects on the Sfx channel, music and ambience on Music.
- **Meanings:** sounds follow section 4. A bad outcome never sounds like a reward.

## 9. Checklist for any new visual

1. **Reuse:** does the board already have this kind of thing? Reuse its art or style.
2. **Materials:** matte, frosted, soft light from above. No gloss, glints or photo-real look.
3. **Colours:** taken from section 3, with the meaning from section 4.
4. **Motion:** reads correctly as good, bad, urgent or neutral (section 4), with timings from section 6.
5. **Text:** in the font for its role, with contrast of at least 4.5:1, measured.
6. **Behaviour:** freezes on pause if it belongs to a waiting call. Takes no clicks. Covers nothing at any board size.
7. **Side by side:** a screenshot next to its neighbours (the number lamps, cards, the frame) shows it belongs, and the report includes that comparison.
