# The economy file: a guide for the designer

`Assets/Configs/Economy.json` is your file. You edit it by hand, and the game reads it every time it starts. Nothing in Unity needs to change when you edit it.

Keep it readable: one entry per line, in the order you want the player to meet them.

## Sections

| Section | What it holds | Ready? |
|---|---|---|
| `upgrades` | Everything the shop sells. | Yes (Step 5a) |
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
| `extra_wires` | `wires` | **Sets** the total number of cords. | Coming in Step 5b |
| `notepad` | none | Tapping a name in the phone book notes its jack on the call in hand. Delete the entry to drop the feature. | Coming in Step 5b |
| `extra_clients` | `boardSize`, `clientSlots` | **Sets** how much of the board is open, and the caller slots. | Coming in Step 7 |

A type that isn't ready yet is reported by the validator and left out of the game, so add those entries when their step arrives.

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
- no `secret`, or more than one

An entry with a problem is left out, and the game still starts with the others. The same check runs in the automated tests, so a broken file is noticed before a build.
