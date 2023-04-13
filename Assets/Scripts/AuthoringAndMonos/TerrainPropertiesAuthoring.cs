using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class TerrainPropertiesMono : MonoBehaviour {
    public int2 TerrainDimensions;
    public int QuadSize;
}

public class TerrainPropertiesBaker : Baker<TerrainPropertiesMono> {
    public override void Bake(TerrainPropertiesMono authoring) {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(new TerrainProperties {
            TerrainDimensions = authoring.TerrainDimensions,
            QuadSize = math.min(authoring.QuadSize, authoring.TerrainDimensions.x)
        });
    }

}