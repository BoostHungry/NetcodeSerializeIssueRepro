using Unity.Entities;
using UnityEngine;

public class TreePrefabMono : MonoBehaviour {
    public float SpawnChance;
    public GameObject Prefab;
}

public class TreePrefabBaker : Baker<TreePrefabMono> {
    public override void Bake(TreePrefabMono authoring) {
        Entity entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new TreeSpawnerComponent {
            Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.None),
            SpawnChance = authoring.SpawnChance,
        });
    }
}