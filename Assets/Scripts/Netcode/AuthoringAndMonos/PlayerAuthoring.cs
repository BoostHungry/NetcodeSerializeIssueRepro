using Unity.Entities;
using UnityEngine;

public class PlayerMono : MonoBehaviour {
}

public class PlayerBaker : Baker<PlayerMono> {
    public override void Bake(PlayerMono authoring) {
        AddComponent<PlayerCursorComp>();
        AddComponent<PlayerCursorInputComp>();
        AddBuffer<PlayerZoneInterestBufferComp>();
    }
}