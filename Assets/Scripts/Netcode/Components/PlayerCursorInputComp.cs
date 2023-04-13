using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

// TODO: Probably change this to just "All"?
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public partial struct PlayerCursorInputComp : IInputComponentData {
    [GhostField(Quantization=1000)] public float2 Position;
}