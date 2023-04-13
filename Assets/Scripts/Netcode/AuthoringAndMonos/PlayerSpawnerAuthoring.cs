using Unity.Entities;
using UnityEngine;

public class PlayerSpawnerMono : MonoBehaviour {
    public GameObject PlayerPrefab;
}

public class PlayerSpawnerBaker : Baker<PlayerSpawnerMono> {
    public override void Bake(PlayerSpawnerMono authoring) {
        Entity playerPrefab = GetEntity(authoring.PlayerPrefab);
        AddComponent(new PlayerSpawner {
            PlayerPrefab = playerPrefab,
        });
    }
}