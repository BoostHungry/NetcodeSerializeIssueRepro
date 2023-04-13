using Unity.Entities;
using UnityEngine;

public class TreeMono : MonoBehaviour {
    
}

public class TreeBaker : Baker<TreeMono> {
    public override void Bake(TreeMono authoring) {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<TreeComp>(entity);
        AddComponent<WorldPosition2D>(entity);
        AddSharedComponent(entity, new ZoneComp {
            Value = -1,
        });
    }
}