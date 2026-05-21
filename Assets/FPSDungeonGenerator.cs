using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation; // Required for NavMeshSurface
using System.Collections.Generic;
using System.Linq;

public class FPSDungeonGenerator : MonoBehaviour
{
    /* ===================== SETTINGS ===================== */

    [Header("Generation Settings")]
    public int numberOfRooms = 15;
    public float spawnRadius = 30f;
    public int maxAttempts = 1000;

    [Header("Multi-Story Settings")]
    public int numberOfStories = 1;
    public float storyHeight = 6f; 

    [Header("Room Dimensions")]
    public int minOddSize = 1; 
    public int maxOddSize = 3; 
    [Min(1)] public int hallwayWidth = 1;
    public float tileSize = 4f;

    [Header("Wall Settings")]
    public GameObject[] wallPrefabs; // Assign multiple wall prefabs for random variety
    public float wallHeight = 4f;
    public int wallStackCount = 3;

    [Header("Ceiling Settings")]
    public GameObject ceilingTilePrefab;
    public float ceilingHeight = 4f;

    [Header("Stairs Settings")]
    public GameObject stairsPrefab;
    [Range(0, 10)] public int stairsPerStory = 1;
    public Vector2Int stairsFootprint = new Vector2Int(2, 3); 

    [Header("Enemy Settings")]
    public GameObject enemyPrefab;
    [Range(0f, 1f)] public float roomSpawnChance = 0.75f; 
    public int minEnemiesPerRoom = 1;
    public int maxEnemiesPerRoom = 4;
    public float enemyHeightOffset = 0.5f; // Lowered to ensure they touch the floor
    public float agentBaseOffset = 0f; // New: Controls visual height offset on the NavMesh
    public float spawnJitter = 1.0f; 

    [Header("Prefabs")]
    public GameObject floorTilePrefab;
    public GameObject roomFloorPrefab;

    [Header("Player Spawn Settings")]
    public GameObject spawnPrefab;
    public float spawnHeightOffset = 2f;
    public enum SpawnLocationPreference { RandomRoom, FirstRoom, LargestRoom, SmallestRoom }
    [Tooltip("How to choose which room to spawn the player in.")]
    public SpawnLocationPreference spawnPreference = SpawnLocationPreference.RandomRoom;
    [Tooltip("If true, spawns at room center. If false, spawns at a random valid tile in the room.")]
    public bool spawnAtRoomCenter = true;
    [Tooltip("Minimum distance from stairs for spawn point.")]
    public float minDistanceFromStairs = 2f;

    /* ===================== INTERNAL DATA ===================== */

    private readonly List<GameObject> spawnedObjects = new();
    private readonly HashSet<Vector3> globalFloorPositions = new();
    private readonly List<Rect> currentStoryRoomRects = new();
    private readonly List<Vector3> currentStoryRoomCenters = new();
    private readonly HashSet<Vector3> currentStoryFloorPositions = new();
    private readonly HashSet<Vector3> stairCutoutPositions = new();
    
    // Store ground floor room data for player spawn selection
    private readonly List<Rect> groundFloorRoomRects = new();
    private readonly List<Vector3> groundFloorRoomCenters = new();
    private readonly HashSet<Vector3> groundFloorPositions = new();
    private readonly HashSet<Vector3> globalStairPositions = new();
    private NavMeshSurface navSurface;
    private int lastWallPrefabIndex = -1;

    private void Start()
    {
        navSurface = GetComponent<NavMeshSurface>();
        GenerateDungeon();
    }

    [ContextMenu("Generate Dungeon")]
    public void GenerateDungeon()
    {
        ClearDungeon();
        lastWallPrefabIndex = -1; // Reset for fresh generation

        // 1. Generate all geometry (Floors and Walls)
        for (int i = 0; i < numberOfStories; i++)
        {
            float currentYLevel = i * storyHeight;
            GenerateStoryGeometry(currentYLevel);
        }

        // 2. BAKE NAVMESH (Crucial for AI to move)
        if (navSurface != null)
        {
            navSurface.BuildNavMesh();
        }
        else
        {
            Debug.LogError("No NavMeshSurface found on this object!");
        }

        // 3. Generate Player and Enemies (After NavMesh is ready)
        GeneratePlayerSpawn();
        
        for (int i = 0; i < numberOfStories; i++)
        {
            float currentYLevel = i * storyHeight;
            GenerateEnemiesForStory(currentYLevel);
        }
    }

    private void StoreGroundFloorData()
    {
        groundFloorRoomRects.Clear();
        groundFloorRoomCenters.Clear();
        groundFloorPositions.Clear();
        
        groundFloorRoomRects.AddRange(currentStoryRoomRects);
        groundFloorRoomCenters.AddRange(currentStoryRoomCenters);
        foreach (var pos in currentStoryFloorPositions)
        {
            groundFloorPositions.Add(pos);
        }
    }

    // Split your original GenerateStory into two parts: Geometry vs Entities
    private void GenerateStoryGeometry(float yLevel)
    {
        currentStoryRoomRects.Clear();
        currentStoryRoomCenters.Clear();
        currentStoryFloorPositions.Clear();
        stairCutoutPositions.Clear();

        GenerateRooms(yLevel);
        ConnectRooms(yLevel);
        
        // Store ground floor data before stairs are placed (for player spawn)
        if (Mathf.Approximately(yLevel, 0f))
        {
            StoreGroundFloorData();
        }
        
        GenerateStairs(yLevel);
        
        // Track all stair positions globally
        foreach (var pos in stairCutoutPositions)
        {
            globalStairPositions.Add(pos);
        }
        
        GenerateWalls(yLevel);
        GenerateCeiling(yLevel);
    }

    /* ===================== ROOMS & CORRIDORS ===================== */
    // (Logic remains largely the same as your original, just called by the new order)

    void GenerateRooms(float yLevel)
    {
        int placed = 0;
        int attempts = 0;
        while (placed < numberOfRooms && attempts < maxAttempts)
        {
            attempts++;
            Vector2 rnd = Random.insideUnitCircle * spawnRadius;
            float x = Mathf.Round(rnd.x / tileSize) * tileSize;
            float z = Mathf.Round(rnd.y / tileSize) * tileSize;
            Vector3 center = new Vector3(x, yLevel, z) + transform.position;

            int sizeX = Random.Range(minOddSize, maxOddSize + 1);
            int sizeZ = Random.Range(minOddSize, maxOddSize + 1);
            float width = (2 * sizeX - 1) * tileSize;
            float depth = (2 * sizeZ - 1) * tileSize;
            Rect rect = new(center.x - width / 2, center.z - depth / 2, width, depth);

            if (IsOverlapping(rect)) continue;

            FillRoom(rect, yLevel);
            currentStoryRoomRects.Add(rect);
            currentStoryRoomCenters.Add(center);
            placed++;
        }
    }

    void FillRoom(Rect r, float yLevel)
    {
        GameObject prefab = roomFloorPrefab != null ? roomFloorPrefab : floorTilePrefab;
        for (float x = r.x + tileSize / 2; x < r.x + r.width; x += tileSize)
        {
            for (float z = r.y + tileSize / 2; z < r.y + r.height; z += tileSize)
            {
                SpawnFloor(prefab, new Vector3(x, yLevel, z), "RoomTile");
            }
        }
    }

    /* ===================== ENEMIES (REWRITTEN) ===================== */

    void GenerateEnemiesForStory(float yLevel)
    {
        if (enemyPrefab == null) return;

        foreach (Rect room in currentStoryRoomRects)
        {
            if (Random.value > roomSpawnChance) continue;

            List<Vector3> validTileCenters = new List<Vector3>();
            for (float x = room.x + tileSize / 2; x < room.x + room.width; x += tileSize)
            {
                for (float z = room.y + tileSize / 2; z < room.y + room.height; z += tileSize)
                {
                    validTileCenters.Add(new Vector3(x, yLevel, z));
                }
            }

            ShuffleList(validTileCenters);
            int count = Mathf.Min(Random.Range(minEnemiesPerRoom, maxEnemiesPerRoom + 1), validTileCenters.Count);

            for (int i = 0; i < count; i++)
            {
                Vector3 basePos = validTileCenters[i];
                float jitterX = Random.Range(-spawnJitter, spawnJitter);
                float jitterZ = Random.Range(-spawnJitter, spawnJitter);
                
                // Use a safe, standard height for instantiation to ensure SamplePosition finds the floor
                // We'll let the agent.baseOffset handle the specific visual height preference
                float safeSpawnHeight = 0.1f; 
                Vector3 finalPos = basePos + new Vector3(jitterX, safeSpawnHeight, jitterZ);

                GameObject enemy = Instantiate(enemyPrefab, finalPos, Quaternion.Euler(0, Random.Range(0, 360), 0), transform);
                enemy.name = "Enemy";
                spawnedObjects.Add(enemy);

                // Ensure the enemy agent snaps to the newly baked NavMesh
                NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    // Set the base offset (controls visual height relative to navmesh surface)
                    agent.baseOffset = agentBaseOffset;

                    if (NavMesh.SamplePosition(finalPos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                    {
                        agent.Warp(hit.position);
                    }
                }
            }
        }
    }

    /* ===================== HELPER METHODS ===================== */
    // (Included the rest of your original logic for walls, corridors, and clearing)

    void ConnectRooms(float yLevel)
    {
        if (currentStoryRoomCenters.Count == 0) return;
        List<int> connected = new() { 0 };
        List<int> remaining = Enumerable.Range(1, currentStoryRoomCenters.Count - 1).ToList();

        while (remaining.Count > 0)
        {
            float bestDist = float.MaxValue;
            int from = -1, to = -1;
            foreach (int a in connected)
            {
                foreach (int b in remaining)
                {
                    float d = Vector3.Distance(currentStoryRoomCenters[a], currentStoryRoomCenters[b]);
                    if (d < bestDist) { bestDist = d; from = a; to = b; }
                }
            }
            CreateCorridor(currentStoryRoomCenters[from], currentStoryRoomCenters[to], yLevel, hallwayWidth);
            connected.Add(to);
            remaining.Remove(to);
        }
    }

    void CreateCorridor(Vector3 start, Vector3 end, float yLevel, int width)
    {
        Vector3 current = start;
        current.y = yLevel;
        int startOffset = -(width / 2);

        void SpawnCorridorSteps(Vector3 pos, bool alongX)
        {
            for (int i = 0; i < width; i++)
            {
                int offset = startOffset + i;
                Vector3 spawnPos = pos;
                if (alongX) spawnPos.z += offset * tileSize;
                else spawnPos.x += offset * tileSize;
                
                SpawnFloor(floorTilePrefab, spawnPos, "CorridorTile");
            }
        }
        
        void SpawnCorner(Vector3 pos) {
             for (int x = 0; x < width; x++) {
                 for (int z = 0; z < width; z++) {
                     Vector3 p = pos;
                     p.x += (startOffset + x) * tileSize;
                     p.z += (startOffset + z) * tileSize;
                     SpawnFloor(floorTilePrefab, p, "CorridorTile");
                 }
             }
        }

        while (Mathf.Abs(current.x - end.x) > 0.1f)
        {
            current.x += Mathf.Sign(end.x - current.x) * tileSize;
            SpawnCorridorSteps(current, true);
        }
        
        if (Mathf.Abs(current.z - end.z) > 0.1f) {
             SpawnCorner(current);
        }

        while (Mathf.Abs(current.z - end.z) > 0.1f)
        {
            current.z += Mathf.Sign(end.z - current.z) * tileSize;
            SpawnCorridorSteps(current, false);
        }
    }

    void SpawnFloor(GameObject prefab, Vector3 pos, string name)
    {
        if (currentStoryFloorPositions.Contains(pos)) return;
        GameObject tile = Instantiate(prefab, pos, Quaternion.Euler(0, 0, 90), transform);
        tile.name = name;
        spawnedObjects.Add(tile);
        currentStoryFloorPositions.Add(pos); 
        globalFloorPositions.Add(pos);      
    }

    void GenerateWalls(float yLevel)
    {
        foreach (Vector3 pos in currentStoryFloorPositions)
        {
            TryWall(pos, Vector3.forward, 90, yLevel);
            TryWall(pos, Vector3.back, 270, yLevel);
            TryWall(pos, Vector3.right, 180, yLevel);
            TryWall(pos, Vector3.left, 0, yLevel);
        }
    }

    void TryWall(Vector3 floorPos, Vector3 dir, float rot, float yLevel)
    {
        if (currentStoryFloorPositions.Contains(floorPos + dir * tileSize)) return;
        for (int i = 0; i < wallStackCount; i++)
        {
            Vector3 wallPos = floorPos + dir * (tileSize / 2);
            wallPos.y = yLevel + (wallHeight / 2) + (i * wallHeight);
            
            GameObject wall;
            if (wallPrefabs != null && wallPrefabs.Length > 0)
            {
                // Pick a random index that is different from the last one
                int newIndex;
                if (wallPrefabs.Length == 1)
                {
                    newIndex = 0;
                }
                else
                {
                    do
                    {
                        newIndex = Random.Range(0, wallPrefabs.Length);
                    } while (newIndex == lastWallPrefabIndex);
                }
                lastWallPrefabIndex = newIndex;
                
                GameObject selectedPrefab = wallPrefabs[newIndex];
                wall = Instantiate(selectedPrefab, wallPos, Quaternion.Euler(0, rot, 0), transform);
            }
            else
            {
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.position = wallPos;
                wall.transform.rotation = Quaternion.Euler(0, rot, 0);
                wall.transform.localScale = new Vector3(0.5f, wallHeight, tileSize);

                // Clone the shared mesh into a new instance so it's readable at runtime
                // (Built-in primitive shared meshes are not readable in players)
                var mf = wall.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    mf.mesh = Object.Instantiate(mf.sharedMesh);
                }
            }
            wall.name = "Wall";
            spawnedObjects.Add(wall);
        }
    }

    void GeneratePlayerSpawn()
    {
        if (groundFloorPositions.Count == 0) return;
        
        // Collect all valid floor tile positions from the ground floor
        List<Vector3> validFloorTiles = new List<Vector3>();
        foreach (Vector3 pos in groundFloorPositions)
        {
            // Skip positions too close to stairs
            bool tooCloseToStairs = false;
            foreach (var stairPos in globalStairPositions)
            {
                if (Vector3.Distance(new Vector3(pos.x, 0, pos.z), new Vector3(stairPos.x, 0, stairPos.z)) < minDistanceFromStairs)
                {
                    tooCloseToStairs = true;
                    break;
                }
            }
            
            if (!tooCloseToStairs)
            {
                validFloorTiles.Add(pos);
            }
        }
        
        // If no valid positions, use any floor tile
        if (validFloorTiles.Count == 0)
        {
            validFloorTiles.AddRange(groundFloorPositions);
        }
        
        // Pick a random floor tile
        Vector3 randomFloorTile = validFloorTiles[Random.Range(0, validFloorTiles.Count)];
        Vector3 spawnPoint = randomFloorTile + Vector3.up * spawnHeightOffset;
        
        // Validate with NavMesh
        if (NavMesh.SamplePosition(spawnPoint, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            spawnPoint = hit.position + Vector3.up * spawnHeightOffset;
        }
        
        // Instantiate spawn prefab
        if (spawnPrefab != null)
        {
            GameObject obj = Instantiate(spawnPrefab, spawnPoint, Quaternion.identity, transform);
            obj.name = "Player_Spawned";
            spawnedObjects.Add(obj);
        }
        
        Debug.Log($"Player spawned at random floor tile: {spawnPoint}");
    }
    
    int SelectSpawnRoom()
    {
        if (groundFloorRoomRects.Count == 0) return 0;
        
        switch (spawnPreference)
        {
            case SpawnLocationPreference.FirstRoom:
                return 0;
                
            case SpawnLocationPreference.LargestRoom:
                float maxArea = 0f;
                int largestIndex = 0;
                for (int i = 0; i < groundFloorRoomRects.Count; i++)
                {
                    float area = groundFloorRoomRects[i].width * groundFloorRoomRects[i].height;
                    if (area > maxArea)
                    {
                        maxArea = area;
                        largestIndex = i;
                    }
                }
                return largestIndex;
                
            case SpawnLocationPreference.SmallestRoom:
                float minArea = float.MaxValue;
                int smallestIndex = 0;
                for (int i = 0; i < groundFloorRoomRects.Count; i++)
                {
                    float area = groundFloorRoomRects[i].width * groundFloorRoomRects[i].height;
                    if (area < minArea)
                    {
                        minArea = area;
                        smallestIndex = i;
                    }
                }
                return smallestIndex;
                
            case SpawnLocationPreference.RandomRoom:
            default:
                return Random.Range(0, groundFloorRoomRects.Count);
        }
    }
    
    Vector3 FindBestSpawnInRoom(Rect room, float yLevel)
    {
        List<Vector3> validPositions = new();
        Vector3 roomCenter = new Vector3(room.x + room.width / 2, yLevel, room.y + room.height / 2);
        
        // Collect all valid floor positions in this room
        for (float x = room.x + tileSize / 2; x < room.x + room.width; x += tileSize)
        {
            for (float z = room.y + tileSize / 2; z < room.y + room.height; z += tileSize)
            {
                Vector3 pos = new Vector3(x, yLevel, z);
                
                // Check if position is valid (not on stairs)
                bool tooCloseToStairs = false;
                foreach (var stairPos in globalStairPositions)
                {
                    if (Vector3.Distance(new Vector3(pos.x, 0, pos.z), new Vector3(stairPos.x, 0, stairPos.z)) < minDistanceFromStairs)
                    {
                        tooCloseToStairs = true;
                        break;
                    }
                }
                
                if (!tooCloseToStairs)
                {
                    validPositions.Add(pos);
                }
            }
        }
        
        // If no valid positions, fall back to room center
        if (validPositions.Count == 0)
        {
            return roomCenter;
        }
        
        // Return a random valid position
        return validPositions[Random.Range(0, validPositions.Count)];
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    bool IsOverlapping(Rect rect)
    {
        foreach (Rect r in currentStoryRoomRects)
        {
            Rect expanded = new(r.x - tileSize, r.y - tileSize, r.width + tileSize * 2, r.height + tileSize * 2);
            if (rect.Overlaps(expanded)) return true;
        }
        return false;
    }

    public void ClearDungeon()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj) { if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }
        }
        spawnedObjects.Clear();
        globalFloorPositions.Clear();
        groundFloorRoomRects.Clear();
        groundFloorRoomCenters.Clear();
        groundFloorPositions.Clear();
        globalStairPositions.Clear();
    }

    void GenerateStairs(float yLevel)
    {
        if (stairsPrefab == null || stairsPerStory <= 0) return;
        if (currentStoryRoomRects.Count == 0) return;

        List<Rect> candidateRooms = currentStoryRoomRects
            .Where(r => r.width >= stairsFootprint.x * tileSize && r.height >= stairsFootprint.y * tileSize)
            .ToList();

        ShuffleList(candidateRooms);
        int stairsPlaced = 0;

        foreach (Rect room in candidateRooms)
        {
            if (stairsPlaced >= stairsPerStory) break;

            // Find a valid position within the room for the stairs
            float stairsX = room.x + tileSize / 2 + Random.Range(0, (int)((room.width - stairsFootprint.x * tileSize) / tileSize) + 1) * tileSize;
            float stairsZ = room.y + tileSize / 2 + Random.Range(0, (int)((room.height - stairsFootprint.y * tileSize) / tileSize) + 1) * tileSize;
            Vector3 stairsPos = new Vector3(stairsX, yLevel, stairsZ);

            // Spawn stairs
            GameObject stairs = Instantiate(stairsPrefab, stairsPos, Quaternion.identity, transform);
            stairs.name = "Stairs";
            spawnedObjects.Add(stairs);

            // Mark cutout positions (tiles covered by stairs footprint)
            for (int x = 0; x < stairsFootprint.x; x++)
            {
                for (int z = 0; z < stairsFootprint.y; z++)
                {
                    Vector3 cutoutPos = new Vector3(stairsX + x * tileSize, yLevel, stairsZ + z * tileSize);
                    stairCutoutPositions.Add(cutoutPos);
                }
            }

            stairsPlaced++;
        }
    }

    void GenerateCeiling(float yLevel)
    {
        if (ceilingTilePrefab == null) return;

        foreach (Vector3 floorPos in currentStoryFloorPositions)
        {
            // Skip if this position is a stair cutout
            if (stairCutoutPositions.Contains(floorPos)) continue;

            // Spawn ceiling at total wall height (wallHeight * wallStackCount)
            Vector3 ceilingPos = new Vector3(floorPos.x, yLevel + (wallHeight * wallStackCount), floorPos.z);
            GameObject ceiling = Instantiate(ceilingTilePrefab, ceilingPos, Quaternion.Euler(0, 0, 90), transform);
            ceiling.name = "CeilingTile";
            spawnedObjects.Add(ceiling);
        }
    }
}
