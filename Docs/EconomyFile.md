# The economy file: a guide for the designer

`Assets/Configs/Economy.json` is your file. You edit it by hand, and the game reads it every time it starts. Nothing in Unity needs to change when you edit it.

Keep it readable: one entry per line, in the order you want the player to meet them.

## Sections

| Section | What it holds | Ready? |
|---|---|---|
| `upgrades` | Everything the shop sells. | Yes (Steps 5a and 5b) |
| `calls` | Each call type's money: fee, rate and duration ranges, fines, response time, how often it rings. | Coming in Step 5c |
| `pacing` | How fast calls arrive, and the idle timeout. | Coming in Step 5c |
| `startingCents` | The money a new game starts with. | Coming in Step 5c |

Until Step 5c, call money and pacing still live in Unity assets.

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

`wires` is the **total** number of cords after buying, not the number added. The game starts with 3 cords, so:

```json
{ "id": "wires_1", "type": "extra_wires", "name": "Extra cord", "description": "4 cords", "costCents": 500, "wires": 4 },
{ "id": "wires_2", "type": "extra_wires", "name": "Extra cord", "description": "5 cords", "costCents": 800, "wires": 5 },
{ "id": "wires_3", "type": "extra_wires", "name": "Extra cord", "description": "6 cords", "costCents": 1300, "wires": 6 },
```

- Each `wires` must be **above 3**, the cords the game starts with.
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
- an empty id, an id with other characters than `a-z`, `0-9` and `_`, or an id used twice
- an unknown type (the message lists the known ones), or a type that isn't ready yet
- an empty name
- a `costCents` of 0 or less
- an `incomeMultiplier` of 1 or less, or one that doesn't grow along its chain
- a `wires` that isn't above the starting 3 cords, is more than 6, or doesn't grow along its chain
- more than one `notepad`
- no `secret`, or more than one

More example messages:

```
Economy file: Upgrade 5 "wires_2": wires 4 is not higher than wires_1's 4. Values in a chain must grow.
Economy file: Upgrade 4 "wires_1": wires 3 is not above the 3 cords the game starts with. wires is the total number of cords, so use 4 or more.
Economy file: Upgrade 6 "wires_3": wires 7 is more than the tray holds. Use 6 or less.
Economy file: Upgrade 8 "notepad_2": there is already a notepad (upgrade 7). Keep at most one notepad entry.
```

An entry with a problem is left out, and the game still starts with the others. The same check runs in the automated tests, so a broken file is noticed before a build.
