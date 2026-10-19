# The phone book file: a guide for the designer

`Assets/Configs/Book.json` is the phone book the player opens on the desk. You edit it by hand, and the game reads it every time it starts. Nothing in Unity needs to change when you edit it. (The calls and the shop have their own guide: [EconomyFile.md](EconomyFile.md).)

The book is **only for looking things up**. The rules never read it: each call in `Calls.json` carries its own jacks. The player reads a name in a call ("Connect me with Mabel Dunmore"), finds "DUNMORE, Mabel" in the book, reads her jack and patches it.

## The format

One entry per line:

```json
{
  "contactsPerPage": 6,
  "entries": [
    { "name": "Fire Brigade", "profession": "Fire station", "address": "Station Rd. 1", "emergencyService": true, "jack": "E5" },
    { "name": "MOSS, Ada", "profession": "Seamstress", "address": "Oak St. 4", "emergencyService": false, "jack": "C3" },
    { "name": "MOSS, Harriet", "profession": "Shop girl", "address": "Oak St. 4", "emergencyService": false, "jack": "C3" }
  ]
}
```

| Field | Needed | What it is |
|---|---|---|
| `contactsPerPage` | no | Entries per page, 1 to 12. Left out, it is 6. |
| `name` | yes | As it is printed: "SURNAME, First" for people ("MOSS, Ada"), the whole name for a service ("Fire Brigade"). Every name must be different. |
| `profession` | no | Printed small under the name. May be empty. |
| `address` | no | Printed after the profession. May be empty. |
| `emergencyService` | no | `true` for the Police, the Fire Brigade and the Doctor. Left out, it is `false`. |
| `jack` | yes | The jack the player patches, as on the board: a row letter A to F, then a column 1 to 6, such as `"C3"`. |

**Several entries may share a jack**, like a household on one line: "MOSS, Ada" and "MOSS, Harriet" both on C3, at the same address.

**Every entry shows from the start**, whatever the board size: the book lists the closed jacks too.

## The order

The book sorts itself, so the order in the file doesn't matter:
1. The emergency services first, in the order you wrote them.
2. Then everyone else by `name`, alphabetically, ignoring upper and lower case. Because names are written "SURNAME, First", this is the surname order of a real directory: "MOSS, Ada" comes before "MOSS, Harriet", which comes before "MOSSOP, Kit".

Each row shows the name, then a smaller line with the profession and address ("Seamstress, Oak St. 4"), and the jack as the last column.

**Text.** Write in English with plain letters, digits and straight quotes only. Curly quotes, dashes and other special signs may be missing from the book's font.

## Checking the file

**Tools > Switchboard > Validate Content** checks the book together with the other files (see [EconomyFile.md](EconomyFile.md#checking-the-files)). Book messages start with "Book file:". They report:
- a missing or empty file, one that isn't valid JSON, or one with no entries (the book is then empty and shows the message on its first page)
- a `contactsPerPage` outside 1 to 12 (the book then uses 6)
- a missing `name` or `jack`
- a jack that isn't on the 6x6 board
- a name listed twice (compared ignoring case and extra spaces)
- the old `PhoneBook.json` names: `contacts` instead of `entries`, and `socket` instead of `jack`

An entry with a problem is left out; the rest of the book still shows.

The calls are checked against the book too: **every jack a call uses needs at least one entry in the book**, or the player could never look it up. Removing the only entry on a jack a call uses gives a message such as:

```
Calls file: call_101 uses D1, but no entry in Book.json is on D1, so the player can't look it up. Add an entry with that jack to the book, or give the call another jack.
```

More example messages:

```
Book file: Entry 12 "THORNE, Basil": jack G9 is not on the board. Use a jack from A1 to F6, such as C3.
Book file: Entry 14 "moss, ada": this name is already listed as entry 13. Every name must be different; give one of them another first name.
Book file: Entry 4: the name is missing. Write it "SURNAME, First", such as "MOSS, Ada".
Book file: Entry 7 "COBBOLD, Doris": "socket" is the old name. Rename it to "jack", such as "jack": "D1".
Book file: contactsPerPage is 20; the book shows 1 to 12 entries per page. Using 6.
```
