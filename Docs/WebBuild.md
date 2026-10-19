# Web build and itch.io: a guide for the team

The game ships as a Web build on itch.io, on the page `llazypixell/operator` ("Operator!", still a Draft). A new version takes one menu click and one command.

## Building

1. In Unity, choose **Tools › Switchboard › Build Web**.
2. Wait. The console says when it's done, for example: `Build Web: done in 0.3 min. The build is in Builds/Web (23.8 MB). Upload it with: butler push Builds/Web llazypixell/operator:html5`.

What the menu does:
- It sets the Web settings the game needs, every time, so a stray change can't ship a broken build: **Gzip** compression with **Decompression Fallback** on, and our own page template **Operator** (`Assets/WebGLTemplates/Operator/`). It leaves every other setting alone.
- It deletes `Builds/Web` and builds the scenes in the build list (`MainMenu`, then `Final`) into it as a release build. The dev panel (F1) isn't in release builds.
- If the editor isn't on the Web platform yet, it switches first.

How long it takes:
- **The first build on a computer takes about 10 minutes**: Unity reimports the project for the Web platform (about 5 minutes), then builds (about 5 minutes).
- After that, a build takes from under a minute to about 5 minutes, depending on what changed.
- **The editor stays on the Web platform** afterwards. That's on purpose: switching back and forth reimports everything each time. Play Mode and the tests work the same on Web. The platform is stored in your local `Library` folder, not in git, so it doesn't affect anyone else.

`Builds/` is ignored by git; never commit it.

**After a build, git shows three files you didn't change.** Every build writes them; undo them before you commit:

```
git checkout -- Assets/Settings/UniversalRP.asset Assets/UniversalRenderPipelineGlobalSettings.asset
```

and in `ProjectSettings/ProjectSettings.asset`, the `preloadedAssets:` line gains an entry (the Input System's actions). Put it back to `preloadedAssets: []`. If nothing else in that file changed, simply `git checkout -- ProjectSettings/ProjectSettings.asset`.

## Testing the build on your computer

The build won't run if you double-click `index.html`; it needs a small web server.

1. In a Terminal: `cd Builds/Web`, then `python3 -m http.server 8000`.
2. Open `http://localhost:8000` in Chrome.
3. Press Ctrl+C in the Terminal to stop the server.

This server sends no special headers, just like itch, so it's a fair test. The browser console may say "You can reduce startup time if you configure your web server to add Content-Encoding: gzip". That's expected: Unity's loader unpacks the files itself (the Decompression Fallback).

## Uploading to itch with butler

butler is itch's upload tool.

1. **Install it once:** download it from https://itch.io/docs/butler/ and put it somewhere on your path (the lead keeps it at `~/bin/butler`).
2. **Log in once:** run `butler login` in a real Terminal window. It opens the browser to confirm. (It can't run from an agent or a script.)
3. **Upload:** `butler push Builds/Web llazypixell/operator:html5`
   - `html5` is the channel name. Always use the same one, so itch replaces the old build instead of adding a second one.
   - butler only uploads what changed, so later uploads are quicker.
4. **Check:** `butler status llazypixell/operator` shows the channel and its latest build number. itch needs a minute or two to process a new build.

If the push says "Please verify your account's email address before uploading a build", the itch account's email address hasn't been confirmed yet: open itch.io › Settings › Email addresses, verify it, and push again.

## The manual fallback (without butler)

1. Open `Builds/Web` and select **everything inside it** (`index.html`, `Build`, `TemplateData`), not the folder itself.
2. Compress those into a zip. `index.html` must be at the top of the zip, not inside a folder.
3. On the itch page's edit screen, upload the zip and tick "This file will be played in the browser".

## The itch page settings

Set these once, on the page's edit screen, after the first upload:
- The `html5` upload: **This file will be played in the browser**.
- **Embed options:** Embed in page, viewport **1280 × 720**.
- **Mobile friendly** on, orientation **Landscape**.
- **Fullscreen button** on.
- **Scrollbars** off.
- **Automatically start on page load** off. The click on itch's "Run game" and the first menu tap let the browser play sound.
- **SharedArrayBuffer support** off (the build uses no threads).

## Where saves live

In the browser, the game saves to the page's **localStorage**, not to Unity's own Web storage. itch gives every upload a new address, and Unity's storage is tied to the address, so progress would vanish with each new upload. localStorage belongs to itch's game domain instead, so saves survive a reload, a closed tab and a new upload.

The keys:
- `operator.save`: the game (money, bought upgrades, the call queue's place, career stats)
- `operator.save.backup`: the save before it
- `Switchboard.AudioPatch.v1`: the volume patch in Options

All HTML games on itch share one domain, which is why the keys start with the game's name.

The game saves after every purchase, every 10 seconds while something changed, when the window loses focus, and **the moment the page is hidden or closed** (switching tabs or apps, locking a phone, closing the tab).

**To start fresh in a browser:** use the game's own **New Game** (it asks first and deletes the save). Or, in Chrome, open the developer tools (F12 or Cmd+Option+I) on the game page, choose **Application › Local storage**, pick the game's address (on itch it starts with `https://html-classic.itch.zone` or similar) and delete the three keys above.

If a browser blocks localStorage (some private windows, or "block third-party cookies and site data" with itch's iframe), the game still runs and saves to Unity's own storage instead, and logs one warning. Those saves may not survive a new upload.

In the Unity editor and desktop builds, saves stay in PlayerPrefs, as before.
