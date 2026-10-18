# The economy file: a guide for the designer

`Assets/Configs/Economy.json` is your file. You edit it by hand, and the game reads it every time it starts. Nothing in Unity needs to change when you edit it.

Keep it readable: one entry per line, in the order you want the player to meet them.

## Sections

| Section | What it holds | Ready? |
|---|---|---|
| `startingCents`, `startingCords` | The money and the cords a new game starts with. | Yes (5c) |
| `pacing` | How fast calls arrive, and the idle timeout. | Yes (5c) |
| `calls` | Each call type's money and timing: fee, rate and duration ranges, fines, response time, how often it rings. | Yes (5c) |
| `upgrades` | Everything the shop sells. | Yes (Steps 5a and 5b) |

The whole file looks like this (the calls and upgrades are shortened here):

```json
{
  "startingCents": 0,
  "startingCords": 3,
  "pacing": { "firstCallDelaySeconds": 2, "baseIntervalSeconds": 8, "intervalJitter": 0.3, "loadFactor": 0.5, "idleTimeoutSeconds": 60 },
  "calls": [
    { "id": "type.regular", "name": "Regular", ... },
    { "id": "type.emergency", "name": "Emergency", ... }
  ],
  "upgrades": [
    { "id": "income_1", ... }
  ]
}
```

**Cents and seconds.** Every money value is in **cents**: 500 is $5.00, 25 is 25¢. Every time value is in **seconds**. The field names say which: `...Cents`, `...Seconds`, `...CentsPerSecond`.

What stays in Unity: how cards look (the EMERGENCY badge and its red), the portraits, the cord colours and the number of caller slots. You never need to touch those to tune the economy.

## Starting money and cords

```json
"startingCents": 0,
"startingCords": 3,
```

| Field | Meaning | If left out |
|---|---|---|
| `startingCents` | The money a new game starts with, in cents. 500 starts the counter at $5.00. 0 or more. | 0 |
| `startingCords` | The cords in the tray when a game starts: **3 to 6**. | 3 |

- The tray is built with 3 cords, and cords can't be taken away, so `startingCords` can't go below 3. The tray holds at most 6.
- Above 3, the extra cords are in the tray from the start, in the same colours as bought cords.
- The `extra_wires` upgrades must start above `startingCords`. With `"startingCords": 4`, a `wires` of 4 is reported, and the first cord the shop offers is the one with `"wires": 5`.

## Pacing

How often new calls ring. One line:

```json
"pacing": { "firstCallDelaySeconds": 2, "baseIntervalSeconds": 8, "intervalJitter": 0.3, "loadFactor": 0.5, "idleTimeoutSeconds": 60 },
```

| Field | What it does | Allowed |
|---|---|---|
| `firstCallDelaySeconds` | Seconds before the first call of a game rings. | 0 or more |
| `baseIntervalSeconds` | The usual gap between two new calls, in seconds, while the player keeps up. Smaller means busier: 3 is hectic, 8 is calm. | 0.1 or more |
| `intervalJitter` | How much each gap varies. 0.3 means each gap is rolled between 30 % shorter and 30 % longer than the base (8 s becomes 5.6 to 10.4 s). 0 makes every gap the same. | 0 to 0.95 |
| `loadFactor` | Each waiting call beyond the free cords stretches the gap by this share. With 0.5, two calls more than the free cords make the gap twice as long (1 + 2 × 0.5), so a swamped player gets a breather. 0 turns this off. | 0 or more |
| `idleTimeoutSeconds` | After this many seconds with no input, calls that carry fines (emergencies) stop ringing, so a player who walks away isn't fined. 0 turns this off. | 0 or more |

A missing or wrong pacing value is reported, and the game uses the default for it (the values in the line above).

## Calls

Each call type is one line in `calls`. A new call goes like this: the game picks a **type** (by `spawnWeight`), then a caller of that type, then the person they want.

```json
{ "id": "type.regular", "name": "Regular", "spawnWeight": 5, "rateCentsPerSecond": { "min": 3, "max": 8 }, "feeCents": { "min": 5, "max": 15 }, "durationSeconds": { "min": 15, "max": 30 }, "callbackDelaySeconds": { "min": 20, "max": 40 }, "responseTimeSeconds": 0, "fineMissedCents": 0, "fineEarlyDisconnectCents": 0, "callbackAfterMiss": false, "callbackAfterDisconnect": false, "maxSimultaneous": 0, "everyoneCalls": false, "recipientTag": "" },
{ "id": "type.emergency", "name": "Emergency", "spawnWeight": 1, "rateCentsPerSecond": { "min": 5, "max": 10 }, "feeCents": { "min": 20, "max": 40 }, "durationSeconds": { "min": 10, "max": 20 }, "callbackDelaySeconds": { "min": 20, "max": 40 }, "responseTimeSeconds": 30, "fineMissedCents": 200, "fineEarlyDisconnectCents": 100, "callbackAfterMiss": false, "callbackAfterDisconnect": false, "maxSimultaneous": 1, "everyoneCalls": true, "recipientTag": "emergency-service" }
```

**Ranges** are written `{ "min": 3, "max": 8 }`. Each call rolls its own value between the two, in whole cents where it can. To fix a value, give both the same number: `{ "min": 5, "max": 5 }`.

### Fields every call type needs

| Field | Meaning |
|---|---|
| `id` | The type's permanent name, such as `type.regular`. Every id must be different. `PhoneBook.json` uses it in each person's `"type"`, so **don't rename an id** without changing the people file too. |
| `name` | A short name, such as "Regular". The dev panel shows it ("Emergency now"). |
| `spawnWeight` | How often this type rings **compared with the others**. 5 against 1 means five regular calls for each emergency. 0 means it never rings on its own. At least one type needs a weight above 0. |
| `rateCentsPerSecond` | What a connected call pays every second, in cents (a range). |
| `feeCents` | The one-off fee paid when the call connects correctly, in cents (a range). |
| `durationSeconds` | How long a connected call lasts, in seconds (a range above 0). |

### Fields you may leave out

Left out, each one means "off".

| Field | Meaning | If left out |
|---|---|---|
| `callbackDelaySeconds` | After a wrong connection, how many seconds until the caller rings again (a range). | 20 to 40 |
| `responseTimeSeconds` | Seconds the player has to connect the call before it counts as missed. | 0: waits as long as it takes |
| `fineMissedCents` | Cents taken for a missed call and for a wrong connection. | 0: no fine |
| `fineEarlyDisconnectCents` | Cents taken for pulling a cord before the call has finished. | 0: no fine |
| `callbackAfterMiss` | `true`: a missed caller rings again later. | `false` |
| `callbackAfterDisconnect` | `true`: a caller who was cut off rings again later. | `false` |
| `maxSimultaneous` | At most this many calls of the type at once, counting callers waiting to ring back. | 0: no limit |
| `everyoneCalls` | `true`: every person without tags can place this kind of call, besides their own type. | `false` |
| `recipientTag` | Only people with this tag receive the calls, such as `"emergency-service"` for the Police, Fire Brigade and Doctor. | `""`: any person without tags |

Money never drops below $0.00, however big the fines.

### Per-person prices

In `PhoneBook.json`, a person's `rate` and `fee` (when above 0) **override** the range of their own `type` for that person's calls. 0 means "use the type's range". So a rich caller can pay more than other Regular callers without a new type. Calls they place through `everyoneCalls` (emergencies) always use that type's ranges.

### A new call type

Add a line to `calls` with a new id, then give people that `"type"` in `PhoneBook.json`. No Unity work is needed: a type without a card style in Unity gets plain cards. (A card badge, like EMERGENCY, is set in a `CallerTypes` asset in Unity, matched by the id.) New *behaviour* beyond these fields still needs code.

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

**Never rename an id once players have played.** Saves remember bought upgrades by id (from Step 6), so a renamed id counts as never bought. You can change the name, description and price at any time.

### Types

| `type` | Extra fields | What it does | Ready? |
|---|---|---|---|
| `extra_income` | `incomeMultiplier` | **Sets** how much every call pays: 1.5 means half as much again. It applies at once, even to calls already connected. Fines are never multiplied. | Yes (5a) |
| `secret` | none | The mystery upgrade, the goal the player saves for. Its shop button fills up as money is saved. There must be **exactly one**. For now, buying it shows "To be continued...". | Yes (5a) |
| `extra_wires` | `wires` | **Sets** the total number of cords. The new cord appears in the tray at once, in a new colour. | Yes (5b) |
| `notepad` | none | While owned, tapping a name in the open phone book notes that person's jack on the call in hand, if they are the caller or the person wanted: "Ada Moss (C3)". There is **at most one**. Delete the entry to drop the feature. | Yes (5b) |
| `extra_clients` | `boardSize`, `clientSlots` | **Sets** how much of the board is open, and the caller slots. | Coming in Step 7 |

A type that isn't ready yet is reported by the validator and left out of the game, so add those entries when their step arrives.

### Extra cords: `wires`

`wires` is the **total** number of cords after buying, not the number added. The game starts with `startingCords` cords (3 in the shipped file), so:

```json
{ "id": "wires_1", "type": "extra_wires", "name": "Extra cord", "description": "4 cords", "costCents": 500, "wires": 4 },
{ "id": "wires_2", "type": "extra_wires", "name": "Extra cord", "description": "5 cords", "costCents": 800, "wires": 5 },
{ "id": "wires_3", "type": "extra_wires", "name": "Extra cord", "description": "6 cords", "costCents": 1300, "wires": 6 },
```

- Each `wires` must be **above `startingCords`**, the cords the game starts with.
- It can be **at most 6**: that's all the tray holds.
- Along the chain it must **grow**: 4, then 5, then 6.

### The Notepad

```json
{ "id": "notepad", "type": "notepad", "name": "Notepad", "description": "Tap a name: note its jack", "costCents": 300 },
```

- Keep at most one `notepad` entry; it has no extra fields.
- Without it, a tap on a name closes the book, as before. Deleting the entry removes the feature; nothing else needs to change.

### Chains

Entries of the same type form a **chain**, in the order they appear in the file. The player can only buy the next one in a chain; there's no skipping. In the example below, "Good rates" can only be bought after "Better rates".

```json
{ "id": "income_1", "type": "extra_income", "name": "Better rates", "description": "Earn x1.5", "costCents": 800, "incomeMultiplier": 1.5 },
{ "id": "income_2", "type": "extra_income", "name": "Good rates", "description": "Earn x2", "costCents": 2000, "incomeMultiplier": 2.0 },
```

**Values set the result; they don't add up.** `incomeMultiplier` 2.0 means "earn ×2 from now on", not "×1.5 and then another ×2". So values in a chain must **grow**: 1.5, then 2.0, then 3.0.

### The shop

- The bottom button always shows the `secret`.
- The 3 buttons above it each show the next offer of one chain, in the order the types first appear in the file.
- When a chain is bought out, its button takes the next chain that isn't shown yet, or goes back to the "+" look. The other buttons don't move.
- To add a new upgrade of a type that already exists, just add a line. No code is needed.

## Checking the file

In Unity, choose **Tools > Switchboard > Validate Content**. The Console then shows either "Validate Content: no problems found." or one plain message per problem, naming the entry and the fix. For example:

```
Economy file: Upgrade 2 "income_2": incomeMultiplier 1.2 is not higher than income_1's 1.5. Values in a chain must grow.
```

The validator reports:
- a missing file, or one that isn't valid JSON (usually a missing comma, quote or bracket)
- a negative `startingCents`, or a `startingCords` outside 3 to 6 (the game then starts with 3)
- a missing `pacing` section or value, or one out of its range (the game then uses the default for it)
- a missing or empty `calls` section (then no calls ring)
- a call type with an empty or duplicate id, an empty name, or a missing `spawnWeight`, `rateCentsPerSecond`, `feeCents` or `durationSeconds`
- a range with only a min or only a max, a min above its max, a negative min, or a `durationSeconds` min of 0
- a negative `spawnWeight`, `responseTimeSeconds`, fine or `maxSimultaneous`
- no call type with a `spawnWeight` above 0
- a person in `PhoneBook.json` whose `type` isn't a call type in this file
- a card style asset in Unity whose id isn't a call type in this file
- an empty id, an id with other characters than `a-z`, `0-9` and `_`, or an id used twice
- an unknown type (the message lists the known ones), or a type that isn't ready yet
- an empty name
- a `costCents` of 0 or less
- an `incomeMultiplier` of 1 or less, or one that doesn't grow along its chain
- a `wires` that isn't above `startingCords`, is more than 6, or doesn't grow along its chain
- more than one `notepad`
- no `secret`, or more than one

More example messages:

```
Economy file: Call type 2 "type.emergency": feeCents min 40 is above its max 20. Swap them.
Economy file: Call type 1 "type.regular": spawnWeight is missing. Add how often this type rings compared with the others, such as "spawnWeight": 1.
Economy file: Call type 1 "type.regular": durationSeconds is missing. Add it as "durationSeconds": { "min": 15, "max": 30 }.
Economy file: Call type 2 "type.emergency": fineMissedCents -5 is negative. Use 0 or more; 0 means off.
Economy file: No call type has a spawnWeight above 0, so no calls would ever ring. Give one a weight, such as 1.
Economy file: pacing: intervalJitter 1.2 is outside 0 to 0.95. Use a value such as 0.3. The game uses 0.3 for now.
Economy file: startingCords 7 is more than the tray holds. Use 3 to 6. The game starts with 3 for now.
People file: Contact 4 "Clara Whitlock": call type 'type.vip' is unknown. Use one of: type.regular, type.emergency.
Economy file: Upgrade 5 "wires_2": wires 4 is not higher than wires_1's 4. Values in a chain must grow.
Economy file: Upgrade 4 "wires_1": wires 3 is not above the 3 cords the game starts with. wires is the total number of cords, so use 4 or more.
Economy file: Upgrade 6 "wires_3": wires 7 is more than the tray holds. Use 6 or less.
Economy file: Upgrade 8 "notepad_2": there is already a notepad (upgrade 7). Keep at most one notepad entry.
```

An upgrade or call type with a problem is left out, and the game still starts with the others. A wrong starting or pacing value falls back to its default. The same check runs in the automated tests, so a broken file is noticed before a build.
