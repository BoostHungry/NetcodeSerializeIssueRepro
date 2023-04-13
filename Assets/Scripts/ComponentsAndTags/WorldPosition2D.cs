using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[GhostComponent()]
public struct WorldPosition2D : IComponentData
{
    [GhostField] public float2 value;
}