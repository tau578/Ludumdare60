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

## What players see while it loads

- **No "Made with Unity" splash.** It is off in the Player settings (Unity 6 allows that on Personal), so the page's own loading screen is the only one.
- **The loading screen** (our template) is a dark cover over the whole window, phones included, with the title "Operator!" and a gold bar. The bar fills while the files download; when the download is done it is full and "Starting..." appears under it while Unity starts up.
- **It waits for the game.** The cover doesn't drop when Unity's loader finishes (that could show a frame or two of black). It fades out (0.3 s) only when the main menu has drawn its first frame: the `PageReady` object in `MainMenu` calls the page's `window.operatorGameReady()` through `Assets/Scripts/Game/GameReady.jslib`. In a local test the signal came about 0.3 s after the loader finished.
- **Fallback:** if that signal never comes, the page lifts the cover anyway 8 seconds after the loader finishes, so nobody is stuck on the loading screen. If loading fails, Unity's red error banner shows above the cover.
- **No "Click to play" button** (lead, 2026-10-20): the menu plays no sound when it opens, and the first menu click unlocks the browser's audio.

**The build tidies up after itself.** Unity rewrites a few files during every build that nobody actually changed: the URP settings (`Assets/Settings/UniversalRP.asset`, `Assets/UniversalRenderPipelineGlobalSettings.asset`), the two dynamic fonts (`Boecklins Universe SDF.asset`, `Della Respira SDF.asset`), `ProjectSettings/TimeManager.asset`, and the `preloadedAssets:` line in `ProjectSettings/ProjectSettings.asset` (the Input System's settings). Since 2026-10-20 the menu saves first, remembers those files, and puts them back when the build ends, even if it fails.

Still look at `git status` before you commit. If one of them shows up anyway, undo it:

```
git checkout -- Assets/Settings/UniversalRP.asset Assets/UniversalRenderPipelineGlobalSettings.asset
```

(for `ProjectSettings.asset`, put the line back to `preloadedAssets: []`), and tell the team, because the clean-up missed it.

**Never commit the dynamic fonts' glyph changes.** Pressing Play in the editor also adds letters and kerning pairs to the two dynamic fonts and changes two numbers (`_ScaleRatioA`, `_ScaleRatioC`) in the `SDF Static` font materials; saving the project writes them out (thousands of lines). TextMeshPro has no setting that stops this. If git shows those font files changed and you didn't mean to change a font, undo them with `git checkout -- <file>`.

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
