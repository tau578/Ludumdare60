# The phone book file: a guide

`Assets/Configs/Book.json` is the phone book the player opens on the desk. You edit it by hand, and the game reads it every time it starts. Nothing in Unity needs to change when you edit it. (The calls and the shop have their own guide: [EconomyFile.md](EconomyFile.md).)

The book is **only for looking things up**. The rules never read it: each call in `Calls.json` carries its own jacks. The player reads a name in a call ("Violet Fairweather, please"), finds "Fairweather, Violet" in the book and clicks the row, which marks her jack on the board (or reads the jack and patches it). Some calls ask by profession instead ("The teacher, please"), so the profession matters too.

## The format

One entry per line:

```json
{
  "contactsPerPage": 7,
  "entries": [
    { "name": "Fire station", "profession": "", "address": "Station Rd. 7", "emergencyService": true, "jack": "D4" },
    { "name": "Higgins, Agatha", "profession": "Retiree", "address": "Church Ln. 3", "emergencyService": false, "jack": "C4" },
    { "name": "Sharma, Ravi (Dr)", "profession": "Doctor", "address": "Church Ln. 2", "emergencyService": false, "jack": "B1" }
  ]
}
```

| Field | Needed | What it is |
|---|---|---|
| `contactsPerPage` | no | Entries per page, 1 to 12. Left out, it is 6. The shipped file uses 7. |
| `name` | yes | As it is printed: "Surname, First" for people ("Ellis, Nora"), with only the first letter of each word capital, and a title after the first name in brackets ("Sharma, Ravi (Dr)", "Plodwell, Percy (PC)", or "Gnome, Mr" when there is no first name); the whole name for a service ("Fire station"). Every name must be different. |
| `profession` | no | Printed small under the name. May be empty. |
| `address` | no | Printed after the profession. May be empty. |
| `emergencyService` | no | `true` for the emergency services (in the shipped file: "Police station", "Fire station", "Hospital"). Left out, it is `false`. |
| `jack` | yes | The jack the player patches, as on the board: a row letter A to F, then a column 1 to 6, such as `"C3"`. |

**Several entries may share a jack**, like a household on one line: two people at the same address on the same jack.

**Every entry shows from the start**, whatever the board size: the book lists the closed jacks too.

## The order

The book sorts itself, so the order in the file doesn't matter:
1. The emergency services first, in the order you wrote them.
2. Then everyone else by `name`, alphabetically, ignoring upper and lower case. Because names are written "Surname, First", this is the surname order of a real directory: "Du Boid, Harrier" before "Ellis, Nora", and "Higgins, Agatha" before "Higgins, Garfield".

So the emergency services always sit at the top of the left page on page 1.

Each row shows the name, then a smaller line with the profession and address ("Librarian, High St. 6"), and the jack as the last column. Clicking a row marks its jack on the board and closes the book.

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
Book file: Entry 4: the name is missing. Write it "Surname, First", such as "Ellis, Nora".
Book file: Entry 7 "COBBOLD, Doris": "socket" is the old name. Rename it to "jack", such as "jack": "D1".
Book file: contactsPerPage is 20; the book shows 1 to 12 entries per page. Using 6.
```
