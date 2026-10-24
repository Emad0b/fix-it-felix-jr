# Fix-It Felix Jr. — Scratch arcade project

An editable Scratch 3 fan game with original procedural pixel-style costumes and synthesised arcade sounds. No film screenshots or extracted game assets are included.

## Play

1. Open https://scratch.mit.edu/projects/editor/ on your computer.
2. Choose **File → Load from your computer**.
3. Select **Fix-It Felix Jr.sb3** from this folder.
4. Click the **green flag**, then click the stage and use the keyboard. The fullscreen button makes it larger.

Scratch Desktop can open the same `.sb3` file offline. No Scratch account is needed to load or play locally. Nothing is automatically uploaded or shared.

## Controls and rules

- Arrow keys: move between three columns and four floors. Holding a key repeats movement; the grid prevents leaving the building.
- Space: swing the hammer and repair your current window.
- Each broken-to-repaired transition awards 100 points. Repeatedly repairing an intact window awards nothing.
- Ralph moves between roof positions, breaks windows and throws bricks down his current column.
- A brick hit costs one of three lives. You then have 1.1 seconds of protection, shown by a transparent Felix.
- Repair all 12 windows to advance. The next level adds 20 seconds, resets the windows and makes the bricks and attacks faster.
- The initial countdown is 60 seconds. Zero time or lives ends the game. Click the green flag to restart everything.

The game has four floors on every level; difficulty increases through enemy speed, not additional floors. It is a small original fan interpretation rather than a recreation of the commercial game's full rules.

## Learn and edit

See [the step-by-step guide](docs/step-by-step.md). All 702 Scratch blocks are editable in the Code tab; all SVG costumes can be opened in Scratch's costume editor. Three WAV sound effects are embedded.

- `Fix-It Felix Jr.sb3`: ready-to-open game; no development tools required.
- `project.json`: readable Scratch block graph.
- `assets/`: original costume and sound assets.
- `build_game.py`: reproducible Python generator, standard library only.
- `tests/game.cjs`: integration tests running the actual project in Scratch VM.
- `docs/preview.png`: starting-layout preview composed from the project's own costumes.

## Rebuild and test

```powershell
python build_game.py
npm ci
npm test
```

Python 3.11+ and Node.js **24** recommended for development. Scratch VM 5.0.300 is pinned as the published standalone package; the newer scoped package's Node bundle failed to load its stylesheet in this environment. The game itself is a standard Scratch 3 `.sb3` and does not depend on Node when opened in Scratch.

Ten headless runtime checks cover archive/assets, initialisation, grid limits, repair scoring, repeated damage, level progression, brick collisions, invincibility, game over, restart and timeout. `tests/results.json` records the latest result. Headless tests do not render graphics or play audio; the artwork is inspected separately as a composed preview. The sounds are generated WAV files with valid headers; in-editor audio playback has not been manually verified.

## Artwork and names

The pixel-style drawings, building and sound effects were created for this project. Fix-It Felix Jr., Felix, Ralph and Wreck-It Ralph are associated with Disney. This is an unofficial fan/learning project and is not affiliated with or endorsed by Disney or Scratch.

To regenerate the composed artwork preview, install a Playwright browser with `npx playwright install chromium`, then run `node tests/preview.cjs`. This previews the costumes, not a second game engine.
