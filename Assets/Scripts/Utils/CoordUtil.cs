using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class CoordUtil {
    public static float3 GetCoordsFloat3(int index, int2 size, float z = 0f) {
        int x = index % size.x;
        int y = index / size.x;
        return new float3(x, y, z);
    }
    
    public static int2 GetCoordsInt2(int index, int2 size) {
        int x = index % size.x;
        int y = index / size.x;
        return new int2(x, y);
    }

    public static int GetIndex(int2 coords, int2 size) {
        return GetIndex(coords, size.x);
    }
    
    public static int GetIndex(int2 coords, int sizeX) {
        return (coords.y * sizeX) + coords.x;
    }

    public static int2 WorldPosToSegmentPos(int2 worldPos, int zoneSize) {
        return new int2(worldPos.x % zoneSize, worldPos.y % zoneSize);
    }
    
    public static int GetZoneId(int index, int2 size, int zoneSize) {
        return GetZoneId(GetCoordsInt2(index, size), zoneSize);
    }

    public static int GetZoneId(Vector3 coords, int zoneSize) {
        return GetZoneId(new int2((int)coords.x, (int)coords.y), zoneSize);
    }
    
    public static int GetZoneId(float3 coords, int zoneSize) {
        return GetZoneId(new int2((int)coords.x, (int)coords.y), zoneSize);
    }
    
    public static int GetZoneId(float2 coords, int zoneSize) {
        return GetZoneId(new int2((int)coords.x, (int)coords.y), zoneSize);
    }
    
    public static int GetZoneId(int2 coords, int zoneSize) {
        return GetZoneId(coords.x, coords.y, zoneSize);
    }

    public static int GetZoneId(int x, int y, int zoneSize) {
        // (0, 0) -> 0
        // (0, 1) -> 1
        // (1, 0) -> 10000
        // (1, 1) -> 10001
        return (10000 * (x / zoneSize)) + y / zoneSize;
    }

    public static NativeArray<int> getZoneAndNeighborZonesTempArray(int zoneId) {
        NativeArray<int> neighborZones = new(9, Allocator.Temp);

        int x = 1;
        int y = 10000;

        neighborZones[0] = zoneId; // Current Zone
        neighborZones[1] = zoneId + y; // N
        neighborZones[2] = zoneId + x + y; // NE
        neighborZones[3] = zoneId + x; // E
        neighborZones[4] = zoneId + x - y; // SE
        neighborZones[5] = zoneId - y; // S
        neighborZones[6] = zoneId - x - y; // SW
        neighborZones[7] = zoneId - x; // W
        neighborZones[8] = zoneId - x + y; // NW

        return neighborZones;
    }
    
    public static int GetZoneIdFromZoneCoords(int2 zoneCoords) {
        return GetZoneIdFromZoneCoords(zoneCoords.x, zoneCoords.y);
    }
    
    public static int GetZoneIdFromZoneCoords(int x, int y) {
        return (10000 * x) + y;
    }

    public static int2 GetWorldStartCordsFromZoneId(int zoneId, int zoneSize) {
        int x = (zoneId / 10000) * zoneSize;
        int y = (zoneId % 10000) * zoneSize;
        return new int2(x, y);
    }
    
}