# Week 9 Endless Runner

A small Unity endless runner built around the Week 9 fake perspective idea. The player runs in three lanes while obstacles approach from the distance. The game scales obstacles and lane markers using `focalLength / (focalLength + z)` so they grow and spread out as they get closer.

## Open and play

1. Open this folder in Unity Hub with Unity **2022.3 LTS**.
2. Open `Assets/Scenes/EndlessRunner.unity`.
3. Press **Play**.

## Controls

- **A / Left Arrow:** move one lane left
- **D / Right Arrow:** move one lane right
- **Space:** jump over an obstacle
- **R:** restart after game over

Lane changes ease between lane positions. Obstacles use a manual Z-distance check and a jump-height check for hits. A hit removes 5 HP, the player starts with 100 HP, and health regenerates by 1 point per second up to 100. The game uses no Rigidbody and does not rely on Unity physics collisions.

## Gameplay video

Upload your gameplay recording to Google Drive, set its sharing access so your professor can view it, then replace the placeholder below with the share link:

[Watch the gameplay video](PASTE_YOUR_GOOGLE_DRIVE_VIDEO_LINK_HERE)
