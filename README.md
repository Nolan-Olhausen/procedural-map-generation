# Unity Biome-Based Procedural Generation

## Overview

This project implements a biome-based procedural generation system in Unity. It utilizes a color-coded biome map (an image where each pixel represents a tile) to dynamically generate a 2D open-world game environment. The system loads chunks around the player and assigns tiles based on the corresponding pixel color from the biome map.

I plan to utilize a system like this for my planned open-world RPG game. I am aiming to have a very large 2D topdown map with a size comparable to that of a game like Skyrim. Hand-placing each tile would take ages, so I need to create a system that will ultimately create the map for me. This biome-based generation is only the first step, eventually I will get into the object, terrain, collision, enemy, and POI (points of interest) placement all within this system. I already have several ideas for how to accomplish this.

I originally researched various methods of map generation and started implementing a cellular automata generation method, which was great for randomized procedural generation. I had a working implementation, but realized to get a specifically designed map result, doing some sort of image pixel to unity tile method would be simpler, so the cellular automata method has been scrapped.

## How It Works

### 1. Biome Map Interpretation

This system can be expanded to several different biomes. 

**Biome Examples:**

- 🟩 Green → Forest
- ⬜ White → Snow
- 🟫 Brown → Wasteland
- 🔵 Blue → Water

In the following preview, I am simply using a small image that contains two colors (the first image below), blue and brown, to shape out a simple game map with land and water. I have an increased load distance to easily display that the tiles are correctly loading the shape of the land and water correctly, if you were to move the player object around, it would load/unload chunks following the location of the player.
![Example ColorMap](https://github.com/Nolan-Olhausen/procedural-map-generation/blob/main/pixelMapTest.png)
![Example Test](https://github.com/Nolan-Olhausen/procedural-map-generation/blob/main/ProjectTest.png)
Like mentioned above this is a simplified example, I plan to use a much more complex biome map in my open-world RPG game. The load distance I also only plan to keep at around 1 or 2, so the 5 that is set for this example was only to show both the coast and inner lake portion of the example map.

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

- **Biome Transitions** – Smooth blending between biomes (this really just comes down to the blend of the pixels in the ColorMap).
- **Object Placement** – Generate trees, rocks, and structures dynamically.
- **Weather & Seasonal Changes** – Adjust biomes based on game events.
- **Multi-Layer Tilemaps** – Support for ground, water, and terrain decorations.

## Setup & Usage

(See the test image from above for what the GameMaster inspector should look like)
1. Import the biome map into Unity.
2. Import the ChunkHandler.cs into Unity.
3. Create an empty game object to be the GameMaster.
4. Assign the image to the ColorMap.
5. Import or create your tiles to use for the map.
6. Configure the biome color-to-tile mappings in the in the inspector (biome arrays can be added in script).
7. Create an empty object to be the Player.
8. Assign the Player to the Player slot in GameMaster.
9. Run the game, and the tilemap will generate based on the biome map.
