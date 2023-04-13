using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

// TODO: Need attributes here?
[GhostComponent()]
public partial struct PlayerCursorComp : IComponentData {
    
    [GhostField] public int PlayerNumber;
    [GhostField(Quantization=1000)] public float2 Position;
    // public Entity PlayerConnectionEntity;
}