# Explore the game step by step in Scratch

## 1. Building: complete

Select the Stage, then Backdrops. The original dark-blue skyline and brick building have three window columns and four floors. Each window is a separate sprite, so you can click it and inspect its behaviour without learning clones first.

## 2. Felix: complete

Select Felix → Code. The green-flag movement loop checks the arrow keys every 0.14 seconds. `Column` is limited to 0–2 and `Row` to 0–3. His position is calculated from those values, keeping him on the windows.

Try changing the 0.14-second wait to 0.20 for slower movement. Keep the bounds so he cannot leave the building.

## 3. Ralph: complete

Select Ralph → Code. His loop chooses a column, moves to the roof position, selects a window in that column to damage, then broadcasts `damage` and `drop`. The pause gets shorter with each level.

Increase the attack wait's 1.5 constant to make the game easier.

## 4. Windows: complete

Select Window 1 → Code. `Broken` is a **for this sprite only** variable. A `repair` broadcast checks whether Felix's Column and Row match and whether Broken equals 1. It changes costume, awards 100 points and decrements Remaining exactly once.

Each window's `damage` receiver checks its own index, so an already-broken window cannot increment Remaining twice. On `next level`, all windows become broken again.

## 5. Arcade rules: complete

The Stage initialises Score, Lives, Time, Level and Remaining. Its monitor loop advances levels or triggers game over. A separate timer loop subtracts one second while Playing equals 1.

The Brick sprite is hidden. A `drop` broadcast creates a clone; its clone script falls, compares coordinates with Felix and deletes itself on collision or leaving the stage. Setting Invincible before broadcasting `hit` prevents simultaneous bricks taking multiple lives. Felix handles the visual protection and clears it after 1.1 seconds.

## Small experiments

- Change starting Lives from 3 to 5 in the Stage's green-flag script.
- Change starting Time from 60 to 90.
- Change the repair reward in each window from 100 to 150.
- Edit Felix's hat or the repaired-window colours in Costumes.
- Add a fourth sound and play it when the `game over` broadcast arrives.

Use File → Save to your computer to preserve your edited `.sb3`. Editing the Scratch project does not update the Python generator; rebuilding with Python replaces generated files, so save your Scratch edits under a different filename.
