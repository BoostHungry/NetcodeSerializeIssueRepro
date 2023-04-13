using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
public partial struct GoInGameClientSystem : ISystem {
    
    [BurstCompile]
    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<PlayerSpawner>();
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<NetworkId>()
            .WithNone<NetworkStreamInGame>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state) { 
        EntityCommandBuffer ecb = new(Allocator.Temp);
        foreach (var (networkIdComp, entity) in 
                 SystemAPI.Query<RefRO<NetworkId>>().WithEntityAccess().WithNone<NetworkStreamInGame>()) {
            Debug.Log($"~~~ GoInGameClientSystem New Request for World: {state.WorldUnmanaged.Name} -- Network Id: {networkIdComp.ValueRO.Value} ~~~");
            ecb.AddComponent<NetworkStreamInGame>(entity);
            Entity request = ecb.CreateEntity();
            ecb.AddComponent<GoInGameRPC>(request);
            ecb.AddComponent(request, new SendRpcCommandRequest { TargetConnection = entity });
        }
        ecb.Playback(state.EntityManager);
    }
}

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct GoInGameServerSystem : ISystem {

    private ComponentLookup<NetworkId> _networkId;
    private int _nextPlayerNumber;

    private bool isGhostDistanceStuffSetup;
    private PortableFunctionPointer<GhostImportance.ScaleImportanceDelegate> _scaleFunctionPointer;
    
    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<PlayerSpawner>();
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<GoInGameRPC>()
            .WithAll<ReceiveRpcCommandRequest>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
        _networkId = state.GetComponentLookup<NetworkId>(true);
        _nextPlayerNumber = 1;
        
        // Relevancy and distance importance
        SystemAPI.GetSingletonRW<GhostRelevancy>().ValueRW.GhostRelevancyMode = GhostRelevancyMode.SetIsRelevant;
        
        // _scaleFunctionPointer = GhostDistanceImportance.ScaleFunctionPointer;
    }
     
    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        
        // if (!isGhostDistanceStuffSetup) { 
        //     var gridSingleton = state.EntityManager.CreateSingleton(new GhostDistanceData {
        //         TileSize = new int3(256, 256, 1),
        //         TileCenter = new int3(128, 128, 0),
        //         TileBorderWidth = new float3(16f, 16f, 16f),
        //     });
        //     state.EntityManager.AddComponentData(gridSingleton, new GhostImportance {
        //         ScaleImportanceFunction = _scaleFunctionPointer,
        //         GhostConnectionComponentType = ComponentType.ReadOnly<GhostConnectionPosition>(),
        //         GhostImportanceDataType = ComponentType.ReadOnly<GhostDistanceData>(),
        //         GhostImportancePerChunkDataType = ComponentType.ReadOnly<GhostDistancePartitionShared>(),
        //     });
        //     
        //     // TODO: Some possible values to help with Ghost stuff...
        // RefRW<GhostSendSystemData> ghostSendSystemData = SystemAPI.GetSingletonRW<GhostSendSystemData>();
        // ghostSendSystemData.ValueRW.MaxSendEntities = 2500;
        // ghostSendSystemData.ValueRW.MinSendImportance = 20;
        // ghostSendSystemData.ValueRW.IrrelevantImportanceDownScale = 50;
        //     
        //     isGhostDistanceStuffSetup = true;
        // }

        // var worldName = state.WorldUnmanaged.Name; // This is how could be useful for debugging server vs client
        
        Entity playerPrefab = SystemAPI.GetSingleton<PlayerSpawner>().PlayerPrefab;
        
        EntityCommandBuffer ecb = new(Allocator.Temp);
        _networkId.Update(ref state);
        
        
        foreach (var (requestSrc, requestEntity) in 
                 SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<GoInGameRPC>().WithEntityAccess()) {
            ecb.AddComponent<NetworkStreamInGame>(requestSrc.ValueRO.SourceConnection);

            NetworkId networkId = _networkId[requestSrc.ValueRO.SourceConnection];
            
            // Ghost Connection Position is used for distance based importance/relevance 
            // ecb.AddComponent<GhostConnectionPosition>(requestSrc.ValueRO.SourceConnection); 

            Debug.Log($"~~~ GoInGameServerSystem Received Request for World: {state.WorldUnmanaged.Name} - Player Number: {_nextPlayerNumber} -- Network Id: {networkId.Value} ~~~");

            Entity player = ecb.Instantiate(playerPrefab);
            ecb.SetComponent(player, new GhostOwner { NetworkId = networkId.Value });
            ecb.SetComponent(player, new PlayerCursorComp {
                PlayerNumber = _nextPlayerNumber,
                // PlayerConnectionEntity = requestSrc.ValueRO.SourceConnection,
            });
            _nextPlayerNumber++;
            ecb.AppendToBuffer(requestSrc.ValueRO.SourceConnection, new LinkedEntityGroup { Value = player });
            
            ecb.DestroyEntity(requestEntity);
        }
        ecb.Playback(state.EntityManager);
    }
    
}