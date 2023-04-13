using Unity.Entities;

public struct TreeSpawnerComponent : IComponentData {
    public Entity Prefab;
    public float SpawnChance;
}