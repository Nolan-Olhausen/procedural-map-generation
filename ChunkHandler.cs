using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class ChunkHandler : MonoBehaviour
{
    public Tile[] waterTiles;
    public Tile[] wastelands;
    public GameObject player;
    public int chunkSize = 16;
    public int loadDistance = 2;
    public Texture2D colorMap;
    Color brown = new Color(0.373f, 0.243f, 0.114f, 1f);
    Color blue = new Color(0f, 0.275f, 0.667f, 1f);
    private bool chunksLoading = false;

    private Vector3Int lastLoadedChunk;
    private Dictionary<Vector3Int, GameObject> loadedChunks = new Dictionary<Vector3Int, GameObject>();

    void Start()
    {
        lastLoadedChunk = GetChunkPosition(player.transform.position);
        LoadChunksAroundPlayer();
    }

    void Update()
    {

        Vector3Int currentChunk = GetChunkPosition(player.transform.position);
        // Check if the player has moved to a new chunk
        if (!chunksLoading && (lastLoadedChunk != currentChunk))
        {
            UpdateChunks(currentChunk);
        }
    }

    void UpdateChunks(Vector3Int currentChunk)
    {
        chunksLoading = true;
        UnloadChunksOutsideRange(currentChunk);
        LoadChunksAroundPlayer();
        chunksLoading = false;
    }

    void LoadChunksAroundPlayer()
    {
        Vector3Int playerChunk = GetChunkPosition(player.transform.position);
        //Debug.Log("current chunk: " + playerChunk.x + ", " + playerChunk.y);
        for (int xOffset = -loadDistance; xOffset <= loadDistance; xOffset++)
        {
            for (int yOffset = -loadDistance; yOffset <= loadDistance; yOffset++)
            {
                Vector3Int chunkPosition = new Vector3Int((playerChunk.x + (xOffset * chunkSize)), (playerChunk.y + (yOffset * chunkSize)), 0);

                // Check if the chunk is already loaded
                if (!IsChunkLoaded(chunkPosition))
                {
                    LoadChunk(chunkPosition);
                }
            }
        }

        lastLoadedChunk = playerChunk;
    }

    void UnloadChunksOutsideRange(Vector3Int currentChunk)
    {
        var chunksToRemove = new List<Vector3Int>();

        foreach (var loadedChunk in loadedChunks)
        {
            Vector3Int chunkPosition = new Vector3Int(loadedChunk.Key.x, loadedChunk.Key.y, 0);

            // Check if the chunk is outside the unload distance
            if ((Mathf.Abs(chunkPosition.x - currentChunk.x) > (loadDistance * chunkSize)) || (Mathf.Abs(chunkPosition.y - currentChunk.y) > (loadDistance * chunkSize)))
            {
                UnloadChunk(chunkPosition);
                chunksToRemove.Add(chunkPosition);
            }
        }

        foreach (var chunkToRemove in chunksToRemove)
        {
            loadedChunks.Remove(chunkToRemove);
        }
    }


    void LoadChunk(Vector3Int chunkPosition)
    {
        GameObject chunkGameObject = new GameObject("Chunk_" + chunkPosition.x + "_" + chunkPosition.y);
        //Debug.Log("Loaded chunk: " + "Chunk_" + chunkPosition.x + "_" + chunkPosition.y);
        UnityEngine.Grid chunkGrid = chunkGameObject.AddComponent<UnityEngine.Grid>();
        GameObject tilemap0Object = new GameObject("0");
        Tilemap chunkTilemap0 = tilemap0Object.AddComponent<Tilemap>();
        TilemapRenderer chunkRenderer0 = tilemap0Object.AddComponent<TilemapRenderer>();
        tilemap0Object.transform.SetParent(chunkGameObject.transform);
        GameObject tilemap1Object = new GameObject("1");
        Tilemap chunkTilemap1 = tilemap1Object.AddComponent<Tilemap>();
        TilemapRenderer chunkRenderer1 = tilemap1Object.AddComponent<TilemapRenderer>();
        tilemap1Object.transform.SetParent(chunkGameObject.transform);
        loadedChunks.Add(new Vector3Int(chunkPosition.x, chunkPosition.y), chunkGameObject);

        chunkGameObject.transform.position = chunkTilemap0.GetCellCenterWorld(chunkPosition);
        chunkTilemap0.size = new Vector3Int(chunkSize, chunkSize, 1);
        chunkTilemap1.size = new Vector3Int(chunkSize, chunkSize, 1);

        // Get the top-left of the chunk in the colormap (since chunkPosition is the *center*)
        int startX = (chunkPosition.x - (chunkSize / 2));
        int startY = (chunkPosition.y - (chunkSize / 2));

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                // Convert to colormap pixel coordinates
                int pixelX = startX + x;
                int pixelY = startY + y;

                // Bounds check
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

    void UnloadChunk(Vector3Int chunkPosition)
    {
        GameObject chunkGameObject = GameObject.Find("Chunk_" + chunkPosition.x + "_" + chunkPosition.y);
        if (chunkGameObject != null)
        {
            //Debug.Log("Destroyed chunk: " + "Chunk_" + chunkPosition.x + "_" + chunkPosition.y);
            Destroy(chunkGameObject);
        }
    }

    bool IsChunkLoaded(Vector3Int chunkPosition)
    {
        return GameObject.Find("Chunk_" + chunkPosition.x + "_" + chunkPosition.y) != null;
    }

    Vector3Int GetChunkPosition(Vector3 position)
    {
        
        return new Vector3Int(Mathf.FloorToInt(position.x / chunkSize)*chunkSize, Mathf.FloorToInt(position.y / chunkSize)*chunkSize, 0);
    }
}
