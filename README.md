# Week 9 Endless Runner

A small 2D Unity endless runner based on the Week 9 fake perspective lesson. The player, track, and obstacles are flat sprites. Three straight 2D lane guides converge toward the horizon. Obstacles use `focalLength / (focalLength + z)` to get bigger as they approach.

## Open and play

1. Open this folder in Unity Hub with Unity **2022.3 LTS**.
2. Open `Assets/Scenes/EndlessRunner.unity`.
3. Press **Play**.

The scene creates its sprites when Play starts, so the Scene view is simple before you run it. View the **Game** tab while playing.

## Controls

- **A / Left Arrow:** move one lane left
- **D / Right Arrow:** move one lane right
- **Space:** jump over an obstacle
- **R:** restart after game over

Lane changes ease between lane positions. Obstacles use a manual Z-distance and jump-height check for hits. The game sorts flat sprites from far to near, following the painter's algorithm from the lesson. A hit removes 5 HP. The player starts with 100 HP and regenerates 1 point per second. The game has no 3D models, Rigidbody, or physics collisions.

## Gameplay video

Upload your gameplay recording to Google Drive, set its sharing access so your professor can view it, then replace the placeholder below with the share link:

[Watch the gameplay video](PASTE_YOUR_GOOGLE_DRIVE_VIDEO_LINK_HERE)
