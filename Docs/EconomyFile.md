# The economy and calls files: a guide

Two files are yours. You edit them by hand, and the game reads them every time it starts. Nothing in Unity needs to change when you edit them.

- `Assets/Configs/Calls.json`: every call in the game, written out one by one.
- `Assets/Configs/Economy.json`: the starting money and cords, the board, **which calls play in which order** (the call queues), and everything the shop sells.

The phone book, `Assets/Configs/Book.json`, has its own guide: [BookFile.md](BookFile.md).

Keep both readable: one entry per line, in the order you want the player to meet them.

**Cents and seconds.** Every money value is in **cents**: 500 is $5.00, 25 is 25¢. Every time value is in **seconds**. The field names say which: `...Cents`, `...Seconds`, `centsPerSecond`.

**Text.** Write names and messages in English with straight quotes and apostrophes only (`'` and `"`). Curly quotes, arrows and other special signs may be missing from the game's font. To put a `"` inside a message, write `\"`; it's easier to use `'` instead: `"It's about the cricket."`.

What stays in Unity: how cards look (the EMERGENCY badge and its red), the portraits and the cord colours. You never need to touch those to tune the game.

## Calls.json

Every call is one line in `calls`:

```json
{
  "calls": [
    { "id": "call_001", "icon": "caller-01", "name": "Ada Moss", "message": "Connect me to <b>Mabel Dunmore</b>. It's about the bake sale.", "from": "C3", "to": "A2", "durationSeconds": 20, "connectCents": 10, "centsPerSecond": 5 },
    { "id": "call_103", "icon": "caller-02", "name": "Herbert Marlow", "message": "Connect me to <b>the doctor</b>. My wife has taken a fall!", "from": "D3", "to": "D4", "durationSeconds": 15, "connectCents": 30, "centsPerSecond": 7, "emergency": true, "responseTimeSeconds": 30 }
  ]
}
```

| Field | Meaning | Allowed |
|---|---|---|
| `id` | The call's permanent name, such as `call_001`. The queues in `Economy.json` list calls by id. Every id must be different. | required |
| `icon` | The caller's portrait: `caller-01` to `caller-12`. | a portrait name from GameConfig's list |
| `name` | The caller's name, shown under the portrait, such as "Ada Moss". | required |
| `message` | What the caller says when the player takes the card, in the speech bubble. It opens with **"Connect me to <b>Name</b>."**, naming the person wanted in bold as the book does ("Violet Fairweather", "Dr Ravi Sharma"; a service as "the police station"), then the caller's own words, kept short (D80). Without that opening the validator gives a note. | required |
| `from` | The caller's jack, such as `"C3"`. | a jack label (see below) |
| `to` | The jack of the person or service they want. | a jack label, not the same as `from` |
| `durationSeconds` | How long the call lasts once connected. | above 0 |
| `connectCents` | The one-off fee paid when the call connects. | 0 or more, whole cents |
| `centsPerSecond` | What the call pays every second while connected. Decimals are fine: 2.5 pays 2½¢ a second, and nothing is lost to rounding. | 0 or more |
| `emergency` | `true` makes it an emergency: a red EMERGENCY badge and a countdown ring. | leave it out for a regular call |
| `responseTimeSeconds` | Emergencies only: the seconds the player has to connect it. | above 0 |
| `patienceSeconds` | Regular calls only, and optional: how long this caller waits before hanging up. Leave it out to use `defaultPatienceSeconds` from `Economy.json`; set it only where a call needs its own. `0` means the caller never hangs up (reported as a note). | above 0 |

`penaltyCents` is gone (D82, step 13a): a missed emergency's fine now follows what the call pays (see "Pressure" under `Economy.json`). A leftover `penaltyCents` gets a plain note and is ignored.

**Jack labels** are written as on the board and in the book: a row letter **A to F**, then a column number **1 to 6**, such as `"C3"` (row C, column 3). The game starts on the `startingBoard` corner (4x4 in the shipped file: rows A to D, columns 1 to 4) and grows from there.

**What a call pays.** A correct connection pays `connectCents` at once, then `centsPerSecond` every second for `durationSeconds`: the shipped first call pays 25¢ + 12 × 5¢ = 85¢. Income upgrades multiply both. The card shows "25¢" and "5¢/s"; the duration stays hidden. Connecting quickly adds a tip (see "Quick-connect tip").

**What can go wrong for the player:**
- A wrong patch ends the call with "Wrong number!". There is **no fine**, and the next call in the queue takes the slot. The same call comes back when the queue comes round to it.
- A regular caller who waits longer than their patience hangs up: the card leaves with a "Hung up" stamp. No pay and **no fine**; the call comes back when the queue comes round to it.
- An emergency that isn't connected within `responseTimeSeconds` ends as "Missed" and is fined `emergencyFineFactor` times what connecting it would have paid (money never drops below $0.00). A wrong patch or a cut-off emergency is **not** fined. Emergencies never hang up: they use only their response time.

**The phone book is only for looking things up.** The calls carry their own jacks; the book (`Book.json`, see [BookFile.md](BookFile.md)) just has to list someone on every jack a call uses, or the player can't find it. The validator checks that.

## Economy.json

| Section | What it holds |
|---|---|
| `startingCents`, `startingCords` | The money and the cords a new game starts with. |
| `startingBoard`, `startingClientSlots` | How much of the board, and how many caller slots, a new game opens. |
| `quickConnectSeconds`, `quickConnectTipCents` | The quick-connect tip: how fast the player must connect a call after taking its card, and what that pays. |
| `defaultPatienceSeconds`, `emergencyFineFactor`, `maxBusyLineCards` | Pressure (step 13a): how long callers wait, how hard a missed emergency is fined, and how many calls to someone already on the line may wait at once. |
| `startPool` | The call queue of a new game. |
| `upgrades` | Everything the shop sells. Each `extra_clients` entry has its own `stagePool`. |

The whole file looks like this (the pools and upgrades are shortened here):

```json
{
  "startingCents": 0,
  "startingCords": 1,
  "startingBoard": { "cols": 4, "rows": 4 },
  "startingClientSlots": 3,
  "quickConnectSeconds": 5,
  "quickConnectTipCents": 5,
  "defaultPatienceSeconds": 45,
  "emergencyFineFactor": 1.5,
  "maxBusyLineCards": 1,
  "startPool": [ "call_001", "call_002", "call_003", ... ],
  "upgrades": [
    { "id": "board_1", "type": "extra_clients", ..., "stagePool": [ "call_101", "call_102", ... ] },
    { "id": "income_1", ... }
  ]
}
```

The old `pacing` and `calls` sections and the `notepad` upgrade are gone. If an old file still has them, the validator says they're no longer used.

## Call queues

Calls don't ring at random any more: they come from a queue, the list of call ids you write.

```json
"startPool": [ "call_001", "call_002", "call_003", "call_004", "call_005", "call_006", "call_007", "call_008" ],
```

- **Slots.** Each open caller slot (window) holds one **waiting** call: one that rings, is in hand or is half-patched. Whenever a slot is free, the next call in the queue appears in it **at once**: at the start, when a call connects or ends, and when an upgrade opens a slot. There is no timer. A connected or held call shrinks to a small tab under the windows and frees its slot (D83, 13a pass 2b), so with 3 slots and 4 cords up to 4 calls talk while 3 more wait. (A finished card keeps its slot, or a tab its place, while its "Paid" or "Wrong number!" stamp shows, about two seconds.)
- **Order.** Calls play in the order you list them and loop back to the first. Success or a wrong number doesn't change the order.
- **No call shows twice.** A call that is still on a card is skipped when the queue comes round to it. If every call in the queue is on a card, a free slot stays empty until one ends. So give a queue at least as many calls as it has slots, and preferably more.
- **Busy lines.** A call whose jack is already in use waits on its card until the player can connect it.
- **Bigger boards.** Each `extra_clients` upgrade has its own `stagePool`. Buying it **replaces** the queue, starting at its first call. Calls already on cards finish normally. The queue is always the last bought board's `stagePool`, or `startPool` before any.
- **Every call must fit its stage.** A call in `startPool` must use jacks on `startingBoard`; a call in a `stagePool` must use jacks on that entry's `boardSize`. A call outside is left out of the queue, because the player could never connect it. A `stagePool` may reuse calls from earlier stages.
- A new game begins at `startPool`'s first call, and deals **only that call** until its fee is paid or it ends: it is the tutorial's call (the spotlight guide walks the player through it). Make it a simple, regular call. Continue picks up where the save left off.
- **One live call per caller.** A call whose caller is already on a card is skipped for now and comes round again later.

The first draft wrote `start_pool` and `stage_pool`; the file uses `startPool` and `stagePool`, like its other names.

## Starting money and cords

```json
"startingCents": 0,
"startingCords": 1,
```

| Field | Meaning | If left out |
|---|---|---|
| `startingCents` | The money a new game starts with, in cents. 500 starts the counter at $5.00. 0 or more. | 0 |
| `startingCords` | The cords in the tray when a game starts: **1 to 6**. The shipped file uses 1. | 2 |

- The `extra_wires` upgrades must start above `startingCords`. With `"startingCords": 4`, a `wires` of 4 is reported, and the first cord the shop offers is the one with `"wires": 5`.

## Starting board and caller slots

```json
"startingBoard": { "cols": 4, "rows": 4 },
"startingClientSlots": 3,
```

| Field | Meaning | If left out or wrong |
|---|---|---|
| `startingBoard` | The part of the board a new game opens. `cols` are the columns 1 to 6, `rows` the rows A to F; each is **1 to 6**. The open part is always the top-left corner: `{ "cols": 3, "rows": 3 }` opens rows A to C and columns 1 to 3 (9 jacks). | The full 6x6 board, with a message |
| `startingClientSlots` | How many caller slots (windows for waiting calls) take calls in a new game: **1 to 4**. The others are hidden. | All 4, with a message |

- Closed jacks are dimmed and can't take a plug. The board in Unity has 6 by 6 jacks and 6 caller slots; those are the only fixed limits.
- **Keep it at 3 (D83, lead 2026-11-12).** The talking calls' tabs sit in a 2x2 grid in the 4th window's space, so the shipped file opens 3 windows at every stage. With 4 windows the tabs would cover the 4th card; at most 4 tabs fit, so more than 4 cords needs a new layout.

## Quick-connect tip

```json
"quickConnectSeconds": 5,
"quickConnectTipCents": 5,
```

A call connected within `quickConnectSeconds` of the player taking its card (clicking it, so its speech bubble shows) pays `quickConnectTipCents` once, on top of its connection fee. The card shows a small "Quick! +5¢" under the "+fee" popup.

| Field | Meaning | If left out or wrong |
|---|---|---|
| `quickConnectSeconds` | The seconds the player has after taking a card. Decimals are fine: 3.5 is three and a half seconds. 0 or more; **0 turns the tip off**. | Left out: 5, with a note. Negative: 5, with a message. |
| `quickConnectTipCents` | The tip in **cents**, whole cents only. 0 or more; **0 turns the tip off**. | Left out: 5, with a note. Negative or not whole: 5, with a message. |

- The clock starts when the card is taken, not when it starts ringing, and it stops while the game is paused.
- Income upgrades (More Profit) **multiply the tip**, like the fee: with "Earn x4", a 5¢ tip pays 20¢. The tip is never part of a fine.
- A call pays the tip at most once: reconnecting it after a pulled plug (on hold) pays neither the fee nor the tip again.
- A call whose two jacks are already patched when the card is taken connects at once, and so gets the tip.
- A wrong number never pays a tip.

## Upgrades

Every upgrade is bought **once**. An entry looks like this:

```json
{ "id": "income_1", "type": "extra_income", "name": "Better rates", "description": "Earn x1.5", "costCents": 800, "incomeMultiplier": 1.5 }
```

### Fields every entry has

| Field | Meaning |
|---|---|
| `id` | The entry's permanent name. Lowercase letters `a-z`, digits `0-9` and `_` only, such as `income_1`. Every id must be different. |
| `type` | What the upgrade does. See the types below. |
| `name` | The title on the shop button. Keep it short, about 14 letters, or it shrinks to fit. |
| `description` | One short line under the title, such as "Earn x1.5". |
| `costCents` | The price in **cents**: 800 is $8.00, 25000 is $250.00. Must be above 0. |

**Never rename an id once players have played.** Since Step 9a the game saves the ids of the upgrades a player bought, so a renamed id counts as never bought, and an entry you delete is simply forgotten. You can change the name, description and price at any time.

### Types

| `type` | Extra fields | What it does |
|---|---|---|
| `extra_income` | `incomeMultiplier` | **Sets** how much every call pays: 1.5 means half as much again. It applies at once, even to calls already connected. A missed emergency's fine follows it too (D82). |
| `extra_wires` | `wires` | **Sets** the total number of cords. The new cord appears in the tray at once, in a new colour. |
| `extra_clients` | `boardSize`, `clientSlots`, `stagePool` | **Sets** how much of the board is open, the caller slots and the call queue. The new jacks and slots open at once, and the queue switches to `stagePool`. |
| `secret` | none | The mystery upgrade, the goal the player saves for. Its shop button fills up as money is saved. There must be **exactly one**. For now, buying it shows "To be continued...". |

### Extra cords: `wires`

`wires` is the **total** number of cords after buying, not the number added. The game starts with `startingCords` cords (1 in the shipped file). In this example the game starts with 3, so:

```json
{ "id": "wires_1", "type": "extra_wires", "name": "Extra cord", "description": "4 cords", "costCents": 500, "wires": 4 },
{ "id": "wires_2", "type": "extra_wires", "name": "Extra cord", "description": "5 cords", "costCents": 800, "wires": 5 },
{ "id": "wires_3", "type": "extra_wires", "name": "Extra cord", "description": "6 cords", "costCents": 1300, "wires": 6 },
```

- Each `wires` must be **above `startingCords`**, at most 6 (all the tray holds), and **grow** along the chain.

### Bigger board: `boardSize`, `clientSlots` and `stagePool`

```json
{ "id": "board_1", "type": "extra_clients", "name": "Bigger board", "description": "4x4, 3 callers", "costCents": 1000, "boardSize": { "cols": 4, "rows": 4 }, "clientSlots": 3, "stagePool": [ "call_101", "call_102", "call_103" ] },
{ "id": "board_2", "type": "extra_clients", "name": "Bigger board", "description": "5x5, 4 callers", "costCents": 2500, "boardSize": { "cols": 5, "rows": 5 }, "clientSlots": 4, "stagePool": [ "call_201", "call_202", "call_203" ] },
{ "id": "board_3", "type": "extra_clients", "name": "Bigger board", "description": "6x6, 5 callers", "costCents": 6000, "boardSize": { "cols": 6, "rows": 6 }, "clientSlots": 5, "stagePool": [ "call_301", "call_302", "call_303" ] },
```

(The shipped pools are longer: 10, 12 and 14 calls.)

- `boardSize` is the **whole** open part after buying, written like `startingBoard`, not the jacks added.
- `clientSlots` is the **total** number of open caller slots after buying, 1 to 4. It may stay the same but never goes down, starting from `startingClientSlots`. The shipped `board_1` and `board_2` keep **3** (13a pass 2b, D83): the 4th window's space holds the talking calls' tabs, so a bigger board brings more people and calls, not more windows. Their descriptions no longer say "4 callers".
- Along the chain, starting from `startingBoard`, the board must **grow**: neither `cols` nor `rows` may go down, and at least one must go up.
- `stagePool` is the queue once this board is bought; see "Call queues". If it's missing or empty, the board can still be bought and the queue stays as it was, with a message.
- In the shipped files all three emergency services are on the 4x4 start (Hospital A2, Police station D3, Fire station D4), so emergencies can come from the first pool on.
- Write "x", not "×", in the description: the game's font has no "×".

### Chains

Entries of the same type form a **chain**, in the order they appear in the file. The player can only buy the next one in a chain; there's no skipping.

**Values set the result; they don't add up.** `incomeMultiplier` 2.0 means "earn ×2 from now on", not "×1.5 and then another ×2". So values in a chain must **grow**: 1.5, then 2.0, then 3.0.

### The shop

- The bottom button always shows the `secret`.
- The 3 buttons above it each show the next offer of one chain, in the order the types first appear in the file.
- When a chain is bought out, its button takes the next chain that isn't shown yet, or goes back to the "+" look. The other buttons don't move.
- To add a new upgrade of a type that already exists, just add a line. No code is needed.

## Pressure: patience, fines and busy lines (step 13a)

```json
"defaultPatienceSeconds": 45,
"emergencyFineFactor": 1.5,
"maxBusyLineCards": 0,
```

| Field | Meaning | If left out or wrong |
|---|---|---|
| `defaultPatienceSeconds` | How long a regular caller waits, in seconds, before hanging up, unless the call sets its own `patienceSeconds`. The clock runs while the card rings, is taken or has one plug in, and stops for good once the call connects. It stops while the game is paused, and while the tutorial guides its first call. | Left out, 0 or less: callers never hang up, with a message. |
| `emergencyFineFactor` | A missed emergency is fined this many times what connecting it would have paid: its `connectCents` plus `centsPerSecond` × `durationSeconds`, times the income multiplier in force when it is missed (the quick-connect tip doesn't count), rounded to the nearest cent. | Left out or negative: no fines, with a message. 0 also means no fines. |
| `maxBusyLineCards` | How many calls to someone who is talking (in a connected or held call) may wait on cards at once. Such a card looks like any other (no "Line busy" label, D83): the player either cuts the talking call off or lets the new caller wait on their patience. A call to someone whose own card is still waiting never rings. **0 for now:** busy-recipient calls come back by day in step 13b. | Left out or negative: 0, with a message: nobody calls a person who is on the line. |

**An example fine.** The shipped `call_004` pays 60¢ + 8¢/s for 12 s = 156¢. Missed with no income upgrade, it costs 1.5 × 156¢ = 234¢ ($2.34); with "Earn x2" bought, 468¢. Because the fine follows pay, it grows by itself when pay grows.

**Who may ring.** Nobody calls while they are on a call: the queue skips a call while its caller's jack is the caller's or the recipient's jack of any call on a card, and comes back to it later. A call **to** someone who is talking may still ring, at most `maxBusyLineCards` of them at a time (0 for now); a call to someone whose own card is still waiting never does. With 0, a call to a busy person waits until they are free, so a pool where one person is in many calls can keep a call waiting a long time: spread each person's calls through the pool.

## Checking the files

In Unity, choose **Tools > Switchboard > Validate Content**. It checks `Calls.json`, `Economy.json` and `Book.json` together. The Console then shows either "Validate Content: no problems found." or one plain message per problem, naming the file, the entry and the fix. For example:

```
Economy file: startPool: call_014 uses E5, but the startingBoard opens only 4x4. The player could never connect it, so it is left out. Move it to the pool of a bigger board, or give it jacks on this one.
```

**Calls.json** messages report:
- a missing or empty file, one that isn't valid JSON (usually a missing comma, quote or bracket), or one with no calls (the game still starts, with no calls)
- an empty or duplicate id
- an empty `name`, `icon` or `message`, or an icon that isn't a known portrait
- a note for a `message` that doesn't open with "Connect me to <b>Name</b>." (the call still plays)
- a missing `durationSeconds`, `connectCents` or `centsPerSecond` (missing numbers never silently become 0)
- a `durationSeconds` of 0 or less; a negative `connectCents` or `centsPerSecond`; a fee that isn't whole cents
- an empty `from` or `to`, a jack that isn't on the 6x6 board, or `from` and `to` the same
- an emergency without a `responseTimeSeconds` above 0
- a note when a regular call has `responseTimeSeconds` (only used for emergencies; the call still plays)
- a note when a call still has `penaltyCents` (no longer used; the fine follows pay)
- a negative `patienceSeconds` (the default is used instead), a note for a `patienceSeconds` of 0 (that caller never hangs up), and a note when an emergency has one (emergencies use only their response time)
- a jack a call uses that has no entry in the phone book (`Book.json`)

A call with a problem is left out; the others still play.

**Economy.json** messages report:
- a missing file, or one that isn't valid JSON
- a negative `startingCents`, or a `startingCords` outside 1 to 6 (the game then starts with 2)
- a note when `quickConnectSeconds` or `quickConnectTipCents` is missing; a negative one, or a `quickConnectTipCents` that isn't whole cents (the game then uses 5)
- a missing `defaultPatienceSeconds`, `emergencyFineFactor` or `maxBusyLineCards`, a `defaultPatienceSeconds` of 0 or less, or a negative factor or count (that rule is then off)
- a missing `startingBoard` or `startingClientSlots`, a `startingBoard` without both `cols` and `rows` or outside 1 to 6, or a `startingClientSlots` outside 1 to 4 (the game then opens the full board and all 4 slots)
- a missing or empty `startPool`; an `extra_clients` entry without a `stagePool`, or with an empty one
- a pool id that isn't in `Calls.json`, or a call that has a problem there
- a call that doesn't fit its stage's board (it is left out of that queue)
- a note when a pool has fewer different calls than its stage's slots (some slots would stay empty)
- the old `pacing` and `calls` sections and the `notepad` upgrade: no longer used
- an empty id, an id with other characters than `a-z`, `0-9` and `_`, or an id used twice
- an unknown type (the message lists the known ones)
- an empty name, or a `costCents` of 0 or less
- an `incomeMultiplier` of 1 or less, or one that doesn't grow along its chain
- a `wires` that isn't above `startingCords`, is more than 6, or doesn't grow along its chain
- an `extra_clients` entry with a missing `boardSize` or `clientSlots`, a size outside 1 to 6, a board that doesn't grow (or shrinks on one side) along its chain, or `clientSlots` that go down
- no `secret`, or more than one

More example messages:

```
Calls file: Call 3 "call_003": durationSeconds is missing. Add how many seconds the call lasts once connected, such as "durationSeconds": 20.
Calls file: Call 7 "call_007": to "G2" is not a jack on the board. Use a row letter A to F, then a column 1 to 6, such as "C3".
Calls file: Call 9 "call_103": an emergency needs responseTimeSeconds, the seconds the player has to connect it, such as "responseTimeSeconds": 30.
Calls file: Call 2 "call_002": responseTimeSeconds is only used for emergencies. Add "emergency": true, or remove it.
Calls file: Call 4 "call_004": penaltyCents is no longer used: a missed emergency's fine is now Economy.json's emergencyFineFactor times what the call pays. Remove it.
Calls file: Call 5 "call_005": the message should start with "Connect me to <b>Name</b>.", naming the person wanted in bold, then the caller's own words. A note only: the call still plays.
Calls file: call_201 uses A5, but no entry in Book.json is on A5, so the player can't look it up. Add an entry with that jack to the book, or give the call another jack.
Economy file: startPool: call_099 is not in Calls.json. Check the spelling, or add the call to Calls.json.
Economy file: Upgrade "board_1": stagePool: call_014 uses E5, but board_1 opens only 4x4. The player could never connect it, so it is left out. Move it to the pool of a bigger board, or give it jacks on this one.
Economy file: Upgrade "board_2": stagePool has 3 different calls for 4 caller slots (board_2's clientSlots), so 1 slot would stay empty. A note only: add more calls if you want every slot busy.
Economy file: The pacing section is no longer used: calls now come from the queues (startPool and each extra_clients's stagePool). Remove the section.
Economy file: Upgrade 7 "notepad": the notepad upgrade is no longer used, so this entry is left out. Remove it from the file.
Economy file: Upgrade 2 "income_2": incomeMultiplier 1.2 is not higher than income_1's 1.5. Values in a chain must grow.
Economy file: Upgrade 5 "board_2": boardSize 4x4 is the same as board_1's 4x4. The board must grow: raise cols, rows or both.
Economy file: quickConnectTipCents -1 is negative. Use 0 or more, in cents; 0 turns the tip off. The game uses 5 for now.
Economy file: startingBoard is missing. Add the part of the board a new game opens, such as "startingBoard": { "cols": 3, "rows": 3 }. The game starts on the full 6x6 board for now.
```

An upgrade with a problem is left out, and the game still starts with the others. A wrong starting value falls back to its default. The same checks run in the automated tests, so a broken file is noticed before a build.
