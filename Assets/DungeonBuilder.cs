using UnityEngine;
using System.Collections.Generic;

public class SimpleDungeonGenerator
{
    public List<RectInt> Rooms { get; private set; }

    public int[,] Generate(int width, int height, int minRooms, int maxRooms, int minRoomSize, int maxRoomSize)
    {
        int[,] map = new int[width, height];
        Rooms = new List<RectInt>();

        int roomCount = Random.Range(minRooms, maxRooms);
        int attempts = 0;
        int maxAttempts = 1000;

        // 1. Place Rooms
        while (Rooms.Count < roomCount && attempts < maxAttempts)
        {
            attempts++;
            
            int w = Random.Range(minRoomSize, maxRoomSize);
            int h = Random.Range(minRoomSize, maxRoomSize);
            int x = Random.Range(1, width - w - 1);
            int y = Random.Range(1, height - h - 1);

            RectInt newRoom = new RectInt(x, y, w, h);

            if (!IsOverlapping(newRoom, Rooms))
            {
                Rooms.Add(newRoom);
                
                // Carve room into map
                for (int rX = newRoom.x; rX < newRoom.x + newRoom.width; rX++)
                {
                    for (int rY = newRoom.y; rY < newRoom.y + newRoom.height; rY++)
                    {
                        map[rX, rY] = 1;
                    }
                }
            }
        }

        // 2. Connect Rooms (Simple L-corridors)
        for (int i = 0; i < Rooms.Count - 1; i++)
        {
            // --- FIX IS HERE ---
            // We use Vector2Int.RoundToInt() to convert the float center to an integer grid coordinate
            Vector2Int start = Vector2Int.RoundToInt(Rooms[i].center);
            Vector2Int end = Vector2Int.RoundToInt(Rooms[i + 1].center);
            
            CreateCorridor(map, start, end);
        }

        return map;
    }

    private bool IsOverlapping(RectInt newRoom, List<RectInt> existingRooms)
    {
        foreach (var room in existingRooms)
        {
            // Pad room by 1 to ensure walls between rooms
            RectInt padded = new RectInt(room.x - 1, room.y - 1, room.width + 2, room.height + 2);
            if (padded.Overlaps(newRoom)) return true;
        }
        return false;
    }

    private void CreateCorridor(int[,] map, Vector2Int start, Vector2Int end)
    {
        Vector2Int current = start;

        // Move X
        while (current.x != end.x)
        {
            if (current.x < end.x) current.x++;
            else current.x--;
            
            // Boundary Check to prevent IndexOutOfRange errors
            if (current.x >= 0 && current.x < map.GetLength(0) && current.y >= 0 && current.y < map.GetLength(1))
                map[current.x, current.y] = 1;
        }

        // Move Y
        while (current.y != end.y)
        {
            if (current.y < end.y) current.y++;
            else current.y--;

             // Boundary Check
            if (current.x >= 0 && current.x < map.GetLength(0) && current.y >= 0 && current.y < map.GetLength(1))
                map[current.x, current.y] = 1;
        }
    }
}