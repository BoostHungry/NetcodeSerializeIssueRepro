using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(GhostInputSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
public partial class PlayerCursorCheckPositionSystem : SystemBase {
    
    private Camera _camera;
    
    protected override void OnCreate() {
        RequireForUpdate<PlayerSpawner>();
        RequireForUpdate<PlayerCursorComp>();
        RequireForUpdate<NetworkId>();
        
    } 

    protected override void OnStartRunning() {
        _camera = Camera.main;
    }

    protected override void OnUpdate() {
        // TODO: Camera checking should only be done once so this can be a bursted ISystem
        Vector3 worldPosVec = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float2 worldPos = new(worldPosVec.x, worldPosVec.y);

        float2 terrainDimensions = SystemAPI.GetSingleton<TerrainProperties>().TerrainDimensions;

        if (worldPos.x < 0 || worldPos.x > terrainDimensions.x || worldPos.y < 0 || worldPos.y > terrainDimensions.y) {
            return;
        }
        
        foreach (var playerCursorInputComp
                 in SystemAPI.Query<RefRW<PlayerCursorInputComp>>().WithAll<GhostOwnerIsLocal>()) {
            float2 currentPos = playerCursorInputComp.ValueRO.Position;
            if (math.abs(currentPos.x - worldPos.x) > 0.01 ||  math.abs(currentPos.y - worldPos.y) > 0.01) {
                playerCursorInputComp.ValueRW.Position = worldPos;
            }
        }
    }
    
    public void OnStopRunning(ref SystemState state) {}
}