using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public enum TileType
{
    Empty = 0,
    Player,
    Enemy,
    Wall,
    Door,
    Key,
    Dagger,
    End
}

public class LevelGenerator : MonoBehaviour
{
    public GameObject[] tiles;
    int width;
    int height;

    [Range(0, 100)]
    public int fillPercent;

    TileType[,] grid;

    TileType[] items = new TileType[5];

    [SerializeField]
    public int iterations;

    
    protected void Start()
    {
        width = 64;
        height = 64;

        items = new TileType[] {
            TileType.Player,
            TileType.Dagger,
            TileType.Enemy,
            TileType.Key,
            TileType.Door,
        };

        grid = new TileType[width, height];
        GenerateNoiseMap(grid);

        for (int i = 0; i < iterations; i++)
        {
            SmoothMap(grid);
        }

        // Get the rooms in the grid
        List<List<Vector2Int>> rooms = GetRooms(grid);

        //Connect the rooms
        ConnectRooms(rooms);

        //Spawn in items based on room number
        SpawnItems(rooms);

        Debugger.instance.AddLabel(32, 26, "Room 1");

        //use 2d array (i.e. for using cellular automata)
        CreateTilesFromArray(grid);
    }

    private void ConnectRooms(List<List<Vector2Int>> rooms)
    {
        if (rooms.Count <= 1) return;

        Vector2Int startRoomPos = rooms[0][0];

        // Calculate the distance between each room and the starting room
        List<KeyValuePair<List<Vector2Int>, int>> distances = new List<KeyValuePair<List<Vector2Int>, int>>();
        for (int i = 0; i < rooms.Count; i++)
        {
            int distance = Mathf.Abs(rooms[i][0].x - startRoomPos.x) + Mathf.Abs(rooms[i][0].y - startRoomPos.y);
            distances.Add(new KeyValuePair<List<Vector2Int>, int>(rooms[i], distance));
        }

        // Sort the rooms by distance
        distances.Sort((a, b) => a.Value.CompareTo(b.Value));


        for (int i = 0; i < rooms.Count - 1; i++)
        {
            MakePath(distances[i].Key[0], distances[i + 1].Key[0]);
        }
    }

    private void MakePath(Vector2Int currentRoom, Vector2Int nextRoom)
    {
        // Place walkable tiles along a straight line between the centers of the rooms
        foreach (Vector2Int position in GetLine(currentRoom, nextRoom))
        {
            //Dont break open map
            if(position.x <= 1 || position.x >= width - 1 || position.y <= 1 || position.y >= height) continue;
            // Fill a single tile with the "Empty" tile type
            FillBlock(grid, position.x, position.y, 1, 1, TileType.Empty);
            FillBlock(grid, position.x, position.y - 1, 1, 1, TileType.Empty);
            FillBlock(grid, position.x, position.y + 1, 1, 1, TileType.Empty);
            FillBlock(grid, position.x + 1, position.y, 1, 1, TileType.Empty);
            FillBlock(grid, position.x - 1, position.y, 1, 1, TileType.Empty);
        }
    }

    // Get a list of points that make up a straight line between two points
    private List<Vector2Int> GetLine(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> line = new List<Vector2Int>();
        int distance = (int)Vector2Int.Distance(start, end);
        for (int i = 0; i <= distance; i++)
        {
            float t = (float)i / distance;
            int x = Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(start.y, end.y, t));
            line.Add(new Vector2Int(x, y));
        }
        return line;
    }

    private void SpawnItems(List<List<Vector2Int>> rooms)
    {
        int roomCount = rooms.Count;
        int itemCount = items.Length;
        int itemsPerRoom = Mathf.CeilToInt((float)itemCount / roomCount);

        int itemIndex = 0;
        for (int roomIndex = 0; roomIndex < roomCount; roomIndex++)
        {
            int numRoomItems = Mathf.Min(itemsPerRoom, itemCount - itemIndex);
            for (int i = 0; i < numRoomItems; i++)
            {
                int randomIndex = Random.Range(0, rooms[roomIndex].Count);
                Vector2Int randomPos = rooms[roomIndex][randomIndex];
                FillBlock(grid, randomPos.x, randomPos.y, 1, 1, items[itemIndex]);
                itemIndex++;
            }
        }
    }

    private void SmoothMap(TileType[,] grid)
    {
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                int surroundingWallCount = GetSurroundingWallCount(grid, x, y);

                if (surroundingWallCount > 4) grid[x, y] = TileType.Wall;
                if (surroundingWallCount < 4) grid[x, y] = TileType.Empty;
            }
        }
    }

    int GetSurroundingWallCount(TileType[,] grid, int x, int y)
    {
        int wallAmount = 0;

        for (int neighbourX = x - 1; neighbourX <= x + 1; neighbourX++)
        {
            for (int neighbourY = y - 1; neighbourY <= y + 1; neighbourY++)
            {
                if (neighbourX != x || neighbourY != y)
                {
                    wallAmount += grid[neighbourX, neighbourY] == TileType.Wall ? 1 : 0;
                }
            }
        }

        return wallAmount;
    }

    private List<List<Vector2Int>> GetRooms(TileType[,] grid)
    {
        List<List<Vector2Int>> rooms = new List<List<Vector2Int>>();
        bool[,] visited = new bool[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!visited[x, y] && grid[x, y] == TileType.Empty)
                {
                    List<Vector2Int> room = new List<Vector2Int>();
                    FloodFill(x, y, grid, visited, room);
                    rooms.Add(room);
                }
            }
        }
        return rooms;
    }

    private void FloodFill(int x, int y, TileType[,] grid, bool[,] visited, List<Vector2Int> room)
    {
        if (x < 0 || x >= width || y < 0 || y >= height || visited[x, y] || grid[x, y] == TileType.Wall)
        {
            return;
        }
        visited[x, y] = true;
        room.Add(new Vector2Int(x, y));
        FloodFill(x - 1, y, grid, visited, room);
        FloodFill(x + 1, y, grid, visited, room);
        FloodFill(x, y - 1, grid, visited, room);
        FloodFill(x, y + 1, grid, visited, room);
    }

    private void GenerateNoiseMap(TileType[,] grid)
    {
        string seed = System.DateTime.Now.Ticks.ToString();
        System.Random pseudoRandom = new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //Always have wall on border
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    FillBlock(grid, x, y, 1, 1, TileType.Wall);
                }
                else
                {
                    TileType tile = pseudoRandom.Next(0, 100) < fillPercent ? TileType.Wall : TileType.Empty;
                    FillBlock(grid, x, y, 1, 1, tile);
                }
            }
        }
    }

    //fill part of array with tiles
    private void FillBlock(TileType[,] grid, int x, int y, int width, int height, TileType fillType)
    {
        for (int tileY = 0; tileY < height; tileY++)
        {
            for (int tileX = 0; tileX < width; tileX++)
            {
                grid[x, y] = fillType;
            }
        }
    }

    //use array to create tiles
    private void CreateTilesFromArray(TileType[,] grid)
    {
        int height = grid.GetLength(0);
        int width = grid.GetLength(1);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                TileType tile = grid[x, y];
                if (tile != TileType.Empty)
                {
                    CreateTile(x, y, tile);
                }
            }
        }
    }

    //create a single tile
    private GameObject CreateTile(int x, int y, TileType type)
    {
        int tileID = ((int)type) - 1;
        if (tileID >= 0 && tileID < tiles.Length)
        {
            GameObject tilePrefab = tiles[tileID];
            if (tilePrefab != null)
            {
                GameObject newTile = GameObject.Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity);
                newTile.transform.SetParent(transform);
                return newTile;
            }
        }
        else
        {
            Debug.LogError("Invalid tile type selected");
        }

        return null;
    }

}
