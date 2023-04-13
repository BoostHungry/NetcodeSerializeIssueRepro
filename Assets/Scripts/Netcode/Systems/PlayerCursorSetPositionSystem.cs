using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct PlayerCursorSetPositionSystem : ISystem {


    // private ComponentLookup<GhostConnectionPosition> _ghostConnectionPosLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state) {
        // _ghostConnectionPosLookup = SystemAPI.GetComponentLookup<GhostConnectionPosition>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        // _ghostConnectionPosLookup.Update(ref state);
        
        foreach (var (playerCursorComp, playerCursorInputComp, transform) 
                 in SystemAPI.Query<RefRW<PlayerCursorComp>, RefRO<PlayerCursorInputComp>, RefRW<LocalTransform>>().WithAll<Simulate>()) {
            float2 playerCursorPosition = playerCursorInputComp.ValueRO.Position;
            float3 position = new float3(playerCursorPosition.xy, 0);
            playerCursorComp.ValueRW.Position = playerCursorInputComp.ValueRO.Position;
            transform.ValueRW.Position = position;
            
            
            // TODO: This probably shouldn't be based off of cursor position, but we already have that information...
            // RefRW<GhostConnectionPosition> ghostPos =
            //     _ghostConnectionPosLookup.GetRefRW(playerCursorComp.ValueRO.PlayerConnectionEntity, false);
            // ghostPos.ValueRW.Position = position;

        }
    }
}