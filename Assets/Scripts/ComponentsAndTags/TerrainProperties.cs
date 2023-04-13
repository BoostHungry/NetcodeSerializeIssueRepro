using Unity.Entities;
using Unity.Mathematics;

public struct TerrainProperties : IComponentData
{
    public int2 TerrainDimensions;
    public int QuadSize;

    public int GetTotalSize() {
        return TerrainDimensions.x * TerrainDimensions.y;
    }

}