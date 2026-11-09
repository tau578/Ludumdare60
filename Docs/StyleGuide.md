# Style guide

How everything in the game should look and move, so new pieces match the hand-painted board. **Read it before any visual work** (effects, popups, signs, lamps, menus, new UI). The art in `Assets/Sprites/` is the reference; this guide writes down what the art already does, plus what the lead's playtests taught (2026-10-22).

Orchestrator, 2026-10-22; the lead settled the last open questions (reds, golds, fonts) on 2026-11-09 (D75).

**Status and what's next (2026-11-09):**
- **The guide is settled:** the look and materials (§1–§2), the palette with two red families and two golds (§3), the feedback language (§4), the fonts and the line between them (§5), the EMERGENCY sign (an engraved brass plate set into the frame, 11c fix pass 7), and text on the wood on brass plates (§5).
- **The "paint box", Step 12** (done 2026-11-09; Plan §9). One shared settings asset, `GameStyle` (`Assets/Content/GameStyle.asset`), holds the named colours, the font roles and the standard timings from this guide. New code reads names such as "Alarm" or "Money gold" instead of raw numbers. S1 moves only the pieces the 2026-11-09 decisions change; the other components move onto it gradually, when they are touched anyway or in a Step 16 cleanup, never as one big rewrite.

## 1. The look in one paragraph

A warm, hand-painted 1930s switchboard with an art-nouveau touch. Surfaces are **matte**: wood, felt, paper, pewter and brass. Shading is soft, with light from above, so tops are lighter and bottoms darker, and shadows are soft and fall slightly down. Glass is **frosted**: it glows from inside, milky and soft. Thin gold curls frame panels and lamps. Colours are muted and earthy. Only an alarm (orange-red), a mistake (crimson), money (gold) and the big titles (title gold) are allowed to shine.

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

Sampled from the game's art and code. Use these instead of inventing new colours. They are the named colours in `Assets/Content/GameStyle.asset` (Step 12): new code reads them from there, by name.

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
| **Money** (money and rewards only) | **Money gold**, bright: `#F2D27A` text, coin `#FFDA70` / `#D49231` / `#7E4B1B`, tip `#FAE68C` | The counter amount, fee popups, coins |
| **Decoration** (never money) | **Brass**, muted: `#96881E` curls; **light brass** `#D9BE72`; the brass plate `#C6A95E` to `#A88A42`, rim `#6E5A1E` | The frame's curls, rims, the EMERGENCY plate |
| **Titles** (screens without money) | **Title gold** `#FFDB1F`, bright | The main menu's titles, the pause titles |
| **Error** (wrong, fine, can't, missed) | Crimson: **Error glow** `#EE4450` for flashes on dark; **Error** `#C42F3A` for text on dark; **Error ink** `#A42626` on paper; **Error deep** `#74121E` on paper, the worst case | The wrong-number and closed-jack flashes; the fine and "can't afford" on the counter; the "Wrong number!" stamp; the "Missed" stamp |
| **Alarm** (emergency waiting) | Orange-red: **Alarm glow** `#FF6A3D` for lights on dark; **Alarm** `#E84A34`; **Alarm quiet** `#8E3426` | The countdown ring, the lamp's glow and the red light on the sign; the lit lamp and the card's EMERGENCY badge (cream letters); the unlit lamp |
| **Attention** (hover, guide) | Amber: `#F5D278` hover, `#FFD06E` lamp | Hovered jacks, the caller's blinking jack, book-row hover |
| **Neutral** (new order, cut off) | Browns: `#7B5220` "New order!", `#5C5044` "Cut off" | Stamps that are neither good nor bad |

**Rules for red and gold** (lead, 2026-11-09):
- **The same meaning gets the same colour, and a different meaning a clearly different one.** Several shades are fine when each has a defined job; near-copies that nobody can tell apart are not.
- **Orange-red always means "hurry", crimson always means "mistake".** Never use one family for the other's job. A wider split (fire orange `#FF8A2A` and rose crimson `#E6305A`, step 12 fix pass 1) was tried and rejected by the lead; these first values stay (fix pass 2).
- **Only money shines gold on the board.** Decoration there uses brass, so gold keeps meaning money. Big titles on screens without money (the main menu, pause) use the bright title gold: light brass made them vague (lead, 2026-11-09).
- **Accepted exception: the EMERGENCY badge.** Its cream letters on Alarm `#E84A34` measure about 3.2:1, under the 4.5:1 rule in §5. The lead chose this look over dark letters (2026-11-09), so leave it as it is.
- **The EMERGENCY sign's letters stay dark engraved ink on the brass** (`#3A140C` quiet, `#480B0D` lit), not alarm red. The alarm shows as Alarm-glow light on the plate (§5).

## 4. Feedback language: what each outcome looks like

The most important rule: **the player must read "good" or "bad" from colour and motion alone.**

| Outcome | Colour | Motion | Extras | Never |
|---|---|---|---|---|
| **Good** (connect, paid) | Green, money gold | Smooth: expanding rings, a gentle wave in the cord, a soft settle | Coins fly to the counter; a soft glow | Shaking, red |
| **Reward** (tip, upgrade, goal) | Money gold | A pop with a small bounce | **Sparkles and shimmer belong here only** | |
| **Bad** (wrong number, fine, missed) | Error crimson | Sharp and jerky: a "no" shake, a cord jolt, a plaque shake | A short circuit (white flash, falling streaks, an arc, dark smoke); coins leaving the counter | **Stars, confetti, sparkles, gold bursts:** they read as a reward (outside playtest feedback, 2026-10-22). Alarm orange-red |
| **Urgent** (emergency waiting) | Alarm orange-red | Flashing (2/s), wobbling, faster in the last 10 s | A ticking clock (14) | Anything that keeps going while paused. Error crimson |
| **Neutral** (new order, cut off, hold) | Browns, cream | A stamp pop; nothing more | | Celebration or alarm |
| **Attention** (where to look) | Amber | A slow blink or glow | | Red, unless it is urgent |

**Jack hover lights** (lead, 2026-11-09, D76). A drop on a taken jack replaces the cord that was there and sends it back to the tray, so the light tells the player what a drop would do:

| Hover over | Empty hand | Holding a cord |
|---|---|---|
| An empty open jack | nothing | amber: plugs in |
| A jack of a live (connected) call | nothing (stays green) | **red**: a drop would cut the call off |
| A jack of an unfinished cord (one end in) | amber | amber: replaces that cord |
| A jack whose call is over (cord still in) | nothing | amber: replaces that cord |
| A closed jack (not drawn) | nothing | nothing |

## 5. Fonts

The game uses three fonts. All of them are the **static** TMP assets (`… SDF Static` and `LiberationSans SDF`), which hold only printable ASCII; LiberationSans also has `¢` and `—`. A character outside these needs a regenerated atlas (Plan §4, "Dynamic TMP fonts").

| Font | Role | Used now (size in 1920×1080 units, colour) |
|---|---|---|
| **Boecklins Universe** | Display: big decorative titles only. Its decorative letters (the three-stem "M") stop being readable when small | Pause title (56, title gold `#FFDB1F`) |
| **Della Respira** | Period serif for short labels: stamps, signs, name plates, menus, guide text | Menu and pause buttons (38, `#FFFAE8`); tutorial hint (30, `#3C2C1C`); book rows (24); main menu; the EMERGENCY sign (engraved capitals on brass, `#3A140C` quiet / `#480B0D` lit; slightly thickened, because Della Respira's strokes fade under about 12 px); stamps and the cards' name plates (from Step 12) |
| **LiberationSans** | Plain text for anything read fast or full of numbers. The only font with `¢` | Money (64, `#F2D27A`); popups (34); card badges (28, `#F3E9CF`); speech bubble (24, `#33241A`); the goal dialog; shop buttons (21–26) |

**Rules:**
- **Contrast:** text needs at least **4.5:1** contrast with what is behind it (WCAG relative luminance), and at least 3:1 for big titles of 36 or more. Measure it; don't judge by eye. The EMERGENCY sign failed this by eye (fix pass 5).
- **Text on the frame wood:** the wood is mid-brown, so neither black nor a saturated colour reaches 4.5:1 on it. Put short labels on a **brass plate set into the wood** (`LabelStripGraphic`, recessed), with dark engraved letters, and measure the letters against the plate. The EMERGENCY sign does this (fix pass 7). A bright ivory strip was tried first: next to the grey lamps it was the only light thing on the frame and looked out of place.
- **A state change keeps its colour family.** Change brightness or add light, rather than switching to an unrelated colour. Ivory letters turning red looked like a different sign (lead, 2026-10-22). On the brass plate the letters stay dark, and the alarm shows as red light on the plate.
- **Minimum size:** 24 units at 1920×1080. On a phone (844×390) the game is about 0.36× as big, so small board text becomes hard to read there (Plan Step 16).
- **Don't change a shared font material.** For an outline or shadow, use offset copies of the label or a material instance made once.

**The line between the fonts** (lead, 2026-11-09): three fonts, each with one job, because one font for everything loses the hierarchy (titles look like labels).
- **Boecklins Universe:** big decorative titles only.
- **Della Respira:** short period labels, meaning stamps, signs, name plates and menus.
- **LiberationSans:** anything read fast or holding numbers: the speech bubble, money, prices, timers and rate badges. A label that needs `¢` stays here.

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
