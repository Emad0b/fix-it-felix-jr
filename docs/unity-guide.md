# Unity editing guide

## First run

Open `UnityGame` in Unity Hub using the recorded Unity 6 editor. Allow the initial import to complete. Open `Assets/Scenes/Arcade.unity`; if it is missing, use **Felix / Prepare scene and verify**. Press Play, then Enter. Audio may require unmuting the editor Game view.

The scene has one `Felix Game` component. At runtime it builds an orthographic camera, 2D SpriteRenderer scenery, window panes and characters. This avoids paid asset packages and external downloads. Changes to runtime-generated artwork belong in `BuildStage()` and `PixelSprite()`.

## Change the rules

Edit `GameState.cs` for lives, initial time, points, protection and level bonuses. The model is used directly by the Unity game. Update the editor checks if you intentionally change a rule. `FelixGame.cs` controls movement repeat, brick speed and attack cadence; `X()` and `Y()` position the fixed three-column/four-floor grid.

## Change the appearance

Edit the palette and pixel rows in `BuildStage()`. Each pixel row in a character sprite must have the same width. Window panes use a dark broken state and illuminated repaired state. `OnGUI()` draws the resolution-scaled HUD and menu; the world uses an orthographic camera. Keep the 960x720 reference layout in mind when adjusting the camera or HUD.

## Verify and build

Use **Felix / Prepare scene and verify** to run the rules and regenerate the scene. This replaces the generated scene, so save any custom scene under another filename before using it. **Felix / Build Windows** runs the checks again and builds a Windows standalone player into the ignored `Builds/Windows` directory. Double-click `Felix2D.exe` to play locally without Unity. Keep its adjacent Data directory and DLL files together when sharing a build.

The rule checks cover bounds, scoring, repeated repairs, enemy window damage, lives, invulnerability, level progression, timeout, restart and invalid elapsed time. The generated runtime screenshot demonstrates rendering, not a complete manual playthrough.

## Earlier Scratch edition

The previous `.sb3`, block JSON, generator, artwork, guide and Scratch VM tests remain in `Legacy/Scratch`. Run its original commands from that directory if needed. It is an archived edition, not a dependency of the Unity game.
