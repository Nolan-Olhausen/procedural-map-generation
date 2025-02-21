using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class ChunkHandler : MonoBehaviour
{
    // Public fields for water and wasteland tiles, player, and chunk size
    public Tile[] waterTiles;
    public Tile[] wastelands;
    public GameObject player;
    public int chunkSize = 16;
    public int loadDistance = 2;
    public Texture2D colorMap;

    // Colors for land and water (brown and blue)
    Color brown = new Color(0.373f, 0.243f, 0.114f, 1f);
    Color blue = new Color(0f, 0.275f, 0.667f, 1f);

    private bool chunksLoading = false;
    private Vector3Int lastLoadedChunk;
    private Dictionary<Vector3Int, GameObject> loadedChunks = new Dictionary<Vector3Int, GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        lastLoadedChunk = GetChunkPosition(player.transform.position);
        LoadChunksAroundPlayer();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3Int currentChunk = GetChunkPosition(player.transform.position);

        // Check if the player has moved to a new chunk
        if (!chunksLoading && (lastLoadedChunk != currentChunk))
        {
            UpdateChunks(currentChunk);
        }
    }

    // Update chunk loading/unloading based on player's position
    void UpdateChunks(Vector3Int currentChunk)
    {
        chunksLoading = true;
        UnloadChunksOutsideRange(currentChunk);
        LoadChunksAroundPlayer();
        chunksLoading = false;
    }

    // Load chunks in the area around the player
    void LoadChunksAroundPlayer()
    {
        Vector3Int playerChunk = GetChunkPosition(player.transform.position);

        // Loop through all chunks around the player within the load distance
        for (int xOffset = -loadDistance; xOffset <= loadDistance; xOffset++)
        {
            for (int yOffset = -loadDistance; yOffset <= loadDistance; yOffset++)
            {
                Vector3Int chunkPosition = new Vector3Int(
                    playerChunk.x + (xOffset * chunkSize), 
                    playerChunk.y + (yOffset * chunkSize), 
                    0
                );

                // Check if the chunk is already loaded
                if (!IsChunkLoaded(chunkPosition))
                {
                    LoadChunk(chunkPosition);
                }
            }
        }

        // Update the last loaded chunk position
        lastLoadedChunk = playerChunk;
    }

    // Unload chunks that are outside of the range
    void UnloadChunksOutsideRange(Vector3Int currentChunk)
    {
        var chunksToRemove = new List<Vector3Int>();

        // Check each loaded chunk
        foreach (var loadedChunk in loadedChunks)
        {
            Vector3Int chunkPosition = new Vector3Int(loadedChunk.Key.x, loadedChunk.Key.y, 0);

            // If chunk is outside of the range, schedule it for removal
            if ((Mathf.Abs(chunkPosition.x - currentChunk.x) > (loadDistance * chunkSize)) ||
                (Mathf.Abs(chunkPosition.y - currentChunk.y) > (loadDistance * chunkSize)))
            {
                UnloadChunk(chunkPosition);
                chunksToRemove.Add(chunkPosition);
            }
        }

        // Remove unloaded chunks from the dictionary
        foreach (var chunkToRemove in chunksToRemove)
        {
            loadedChunks.Remove(chunkToRemove);
        }
    }

    // Load a single chunk at the specified position
    void LoadChunk(Vector3Int chunkPosition)
    {
        GameObject chunkGameObject = new GameObject("Chunk_" + chunkPosition.x + "_" + chunkPosition.y);
        UnityEngine.Grid chunkGrid = chunkGameObject.AddComponent<UnityEngine.Grid>();

        // Create and set up tilemaps
        GameObject tilemap0Object = new GameObject("0");
        Tilemap chunkTilemap0 = tilemap0Object.AddComponent<Tilemap>();
        TilemapRenderer chunkRenderer0 = tilemap0Object.AddComponent<TilemapRenderer>();
        tilemap0Object.transform.SetParent(chunkGameObject.transform);

        GameObject tilemap1Object = new GameObject("1");
        Tilemap chunkTilemap1 = tilemap1Object.AddComponent<Tilemap>();
        TilemapRenderer chunkRenderer1 = tilemap1Object.AddComponent<TilemapRenderer>();
        tilemap1Object.transform.SetParent(chunkGameObject.transform);

        // Add the chunk to the loaded chunks dictionary
        loadedChunks.Add(new Vector3Int(chunkPosition.x, chunkPosition.y), chunkGameObject);

        chunkGameObject.transform.position = chunkTilemap0.GetCellCenterWorld(chunkPosition);
        chunkTilemap0.size = new Vector3Int(chunkSize, chunkSize, 1);
        chunkTilemap1.size = new Vector3Int(chunkSize, chunkSize, 1);

        // Get the top-left corner of the chunk in the colormap (chunkPosition is the center)
        int startX = (chunkPosition.x - (chunkSize / 2));
        int startY = (chunkPosition.y - (chunkSize / 2));

        // Loop through each tile in the chunk and set its type based on color in the colormap
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                int pixelX = startX + x;
                int pixelY = startY + y;

                // Check if pixel is within bounds of the colormap
                if (pixelX >= 0 && pixelX < colorMap.width && pixelY >= 0 && pixelY < colorMap.height)
                {
                    Color pixelColor = colorMap.GetPixel(pixelX, pixelY);

                    // Debug log to check pixel color
                    Debug.Log($"Tile ({x},{y}) -> Pixel ({pixelX},{pixelY}) | Color: {pixelColor} | Is Brown: {ColorEquals(brown, pixelColor)} | Is Blue: {ColorEquals(blue, pixelColor)}");

                    // Set tile based on color
                    if (ColorEquals(brown, pixelColor))
                    {
                        chunkTilemap0.SetTile(new Vector3Int(x, y, 0), wastelands[0]); // Land
                    }
                    else if (ColorEquals(blue, pixelColor))
                    {
                        chunkTilemap1.SetTile(new Vector3Int(x, y, 0), waterTiles[0]); // Water
                    }
                    else
                    {
                        chunkTilemap1.SetTile(new Vector3Int(x, y, 0), waterTiles[1]); // Default water
                    }
                }
                else
                {
                    Debug.Log($"Tile ({x},{y}) is out of bounds, setting water tile.");
                    chunkTilemap1.SetTile(new Vector3Int(x, y, 0), waterTiles[1]); // Out-of-bounds -> Water
                }
            }
        }
    }

    // Helper function to check if two colors are equal within a tolerance
    public static bool ColorEquals(Color a, Color b, float tolerance = 0.1f)
    {
        if (a.r > b.r + tolerance) return false;
        if (a.g > b.g + tolerance) return false;
        if (a.b > b.b + tolerance) return false;
        if (a.r < b.r - tolerance) return false;
        if (a.g < b.g - tolerance) return false;
        if (a.b < b.b - tolerance) return false;

        return true;
    }

    // Unload a single chunk by destroying its gameObject
    void UnloadChunk(Vector3Int chunkPosition)
    {
        GameObject chunkGameObject = GameObject.Find("Chunk_" + chunkPosition.x + "_" + chunkPosition.y);
        if (chunkGameObject != null)
        {
            Destroy(chunkGameObject);
        }
    }

    // Check if a chunk is already loaded
    bool IsChunkLoaded(Vector3Int chunkPosition)
    {
        return GameObject.Find("Chunk_" + chunkPosition.x + "_" + chunkPosition.y) != null;
    }

    // Get the chunk position based on a world position
    Vector3Int GetChunkPosition(Vector3 position)
    {
        return new Vector3Int(Mathf.FloorToInt(position.x / chunkSize) * chunkSize, Mathf.FloorToInt(position.y / chunkSize) * chunkSize, 0);
    }
}
