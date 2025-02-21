# Unity Biome-Based Procedural Generation

## Overview

This project implements a biome-based procedural generation system in Unity. It utilizes a color-coded biome map (an image where each pixel represents a tile) to dynamically generate a 2D open-world game environment. The system loads chunks around the player and assigns tiles based on the corresponding pixel color from the biome map.

## How It Works

### 1. Biome Map Interpretation

A biome map (image) is provided, where each color represents a different biome.

**Example:**

- 🟩 Green → Forest
- ⬜ White → Snow
- 🟫 Brown → Wasteland
- 🔵 Blue → Water

In the following preview, I am simply using a small image that contains two colors, blue and brown, to shape out a simple game map with land and water. I have an increased load distance to easily display that the tiles are correctly loading the shape of the land and water correctly, if you were to move the player object around, it would load/unload chunks following the location of the player.
![Example Test](image-url)

### 2. Chunk Loading

The game world is divided into chunks for efficient rendering and memory management.

- Chunks are dynamically loaded and unloaded based on the player’s position.
- Associated values like chunk size and load distance can be modified in the inspector

### 3. Tile Assignment

When a chunk is loaded, the script reads the corresponding section of the biome map.

- Each pixel in the image corresponds to one tile in the Unity tilemap.
- The script determines the tile’s biome based on the color value of the pixel.
- The appropriate tile is set using `Tilemap.SetTile()`.

## Features

- ✅ **Efficient Chunk System** – Only nearby chunks are loaded, optimizing performance.
- ✅ **Biome-Based Terrain** – Tiles are assigned based on the biome map.
- ✅ **Scalability** – The system can be expanded with additional biomes, terrain features, and procedural elements.

## Future Enhancements

- **Biome Transitions** – Smooth blending between biomes.
- **Object Placement** – Generate trees, rocks, and structures dynamically.
- **Weather & Seasonal Changes** – Adjust biomes based on game events.
- **Multi-Layer Tilemaps** – Support for ground, water, and terrain decorations.

## Setup & Usage

1. Import the biome map into Unity.
2. Create an empty game object to be the GameMaster.
3. Assign the image to the ColorMap.
4. Configure the biome color-to-tile mappings in the in the inspector (biome arrays can be added in script).
5. Create an empty object to be the Player.
6. Assign the Player to the Player slot in GameMaster.
7. Run the game, and the tilemap will generate based on the biome map.
