using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class ZoneInterestManagementClientSystem : SystemBase {
    
    private static Vector3 BOTTOM_LEFT = new Vector3(0, 0, 0);
    private static Vector3 TOP_RIGHT = new Vector3(1, 1, 0);

    private NativeHashSet<int> _activeZones;
    private EntityArchetype _rpcRequestArchetype;
    
    private double _nextSecond = 1;
    

    // TODO: This could be an ISystem if the camera object wasn't directly used here
    private Camera _camera;

    protected override void OnCreate() {
        RequireForUpdate<NetworkStreamInGame>();
        _activeZones = new(24, Allocator.Persistent);
        _rpcRequestArchetype = EntityManager.CreateArchetype(typeof(InterestInZoneRPC), typeof(SendRpcCommandRequest));
    }

    protected override void OnStartRunning() {
        _camera = Camera.main;
    }

    protected override void OnUpdate() {

        // TODO: Cursor Location Debugger
        // if (World.Time.ElapsedTime > _nextSecond) {
        //     _nextSecond++;
        //     Debug.Log($"CursorPosition = {_camera.ScreenToWorldPoint(Input.mousePosition)}");
        // }
        
        TerrainProperties terrainProperties = SystemAPI.GetSingleton<TerrainProperties>();
        
        // TODO: Move camera to a dedicate SystemBase that only checks the camera
        Vector3 bottomLeft = _camera.ViewportToWorldPoint(BOTTOM_LEFT);
        Vector3 topRight = _camera.ViewportToWorldPoint(TOP_RIGHT);

        bottomLeft /= terrainProperties.QuadSize;
        topRight /= terrainProperties.QuadSize;

        int xMaxZone = (terrainProperties.TerrainDimensions.x - 1) / terrainProperties.QuadSize;
        int xStartZone = math.clamp((int)bottomLeft.x - 1, 0, xMaxZone);
        int xEndZone = math.clamp((int)topRight.x + 1, 0, xMaxZone);
        
        int yMaxZone = (terrainProperties.TerrainDimensions.y - 1) / terrainProperties.QuadSize;
        int yStartZone = math.clamp((int)bottomLeft.y - 1, 0, yMaxZone);
        int yEndZone = math.clamp((int)topRight.y + 1, 0, yMaxZone);


        NativeHashSet<int> expectedActiveZoneIds = new(20, Allocator.Temp);
        for (int x = xStartZone; x <= xEndZone; x += 1) {
            for (int y = yStartZone; y <= yEndZone; y += 1) {
                expectedActiveZoneIds.Add(CoordUtil.GetZoneIdFromZoneCoords(x, y));
            }
        }

        NativeHashSet<int> inactiveZoneIds = new(20, Allocator.Temp);
        foreach (int currentlyActiveZone in _activeZones) {
            if (!expectedActiveZoneIds.Remove(currentlyActiveZone)) {
                inactiveZoneIds.Add(currentlyActiveZone);
            }
        }

        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(World.Unmanaged);

        // Request new zones
        foreach (int expectedActiveZoneId in expectedActiveZoneIds) {
            Entity request = ecb.CreateEntity(_rpcRequestArchetype);
            ecb.SetComponent(request, new InterestInZoneRPC {
                ZoneId = expectedActiveZoneId,
                IsInterested = true,
            });
            _activeZones.Add(expectedActiveZoneId);
        }
        
        // Clear out old zones
        foreach (int inactiveZoneId in inactiveZoneIds) {
            Entity request = ecb.CreateEntity(_rpcRequestArchetype);
            ecb.SetComponent(request, new InterestInZoneRPC {
                ZoneId = inactiveZoneId,
                IsInterested = false,
            });
            _activeZones.Remove(inactiveZoneId);
        }

        inactiveZoneIds.Dispose();
        expectedActiveZoneIds.Dispose();
    }

}

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ZoneInterestManagementServerSystem : ISystem {

    private ComponentLookup<NetworkId> _networkIdLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state) {
        _networkIdLookup = state.GetComponentLookup<NetworkId>(true);

        state.RequireForUpdate<InterestInZoneRPC>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        _networkIdLookup.Update(ref state);
        
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        
        RefRW<GhostRelevancy> ghostRelevancy = SystemAPI.GetSingletonRW<GhostRelevancy>();
        
        foreach (var (interestInZoneRPC, rpcCommandRequest, entity) 
                 in SystemAPI.Query<InterestInZoneRPC, ReceiveRpcCommandRequest>().WithEntityAccess()) {
            
            int networkId = _networkIdLookup[rpcCommandRequest.SourceConnection].Value;

            // TODO: This is quick and dirty.  Should be cached and looked up instead.  
            Entity player = Entity.Null;
            foreach (var (ghostOwnerComponent, playerEntity) 
                     in SystemAPI.Query<GhostOwner>().WithAll<PlayerCursorComp>().WithEntityAccess()) {
                if (ghostOwnerComponent.NetworkId == networkId) {
                    player = playerEntity;
                    break;
                }
            }

            if (player == Entity.Null) {
                 Debug.LogError($"No player found for Network Id: {networkId}");
                 ecb.DestroyEntity(entity);
                 continue;
            }

            // Update the player's interest in this zone
            DynamicBuffer<PlayerZoneInterestBufferComp> playerZoneInterest =
                state.EntityManager.GetBuffer<PlayerZoneInterestBufferComp>(player);
            if (interestInZoneRPC.IsInterested) {
                playerZoneInterest.Add(interestInZoneRPC.ZoneId);
            } else {
                int indexToRemove = -1;
                for (int i = 0; i < playerZoneInterest.Length; i++) {
                    if (playerZoneInterest[i].Value == interestInZoneRPC.ZoneId) {
                        indexToRemove = i;
                        break;
                    }
                }

                if (indexToRemove >= 0) {
                    playerZoneInterest.RemoveAt(indexToRemove);
                }
            }

            // Update relevancy for all objects in that zone
            ZoneComp zone = new ZoneComp { Value = interestInZoneRPC.ZoneId };
            foreach (var ghostComponent 
                     in SystemAPI.Query<GhostInstance>().WithSharedComponentFilter(zone).WithAll<HasGhostId>()) {
                
                RelevantGhostForConnection relevantGhostForConnection = new(networkId, ghostComponent.ghostId);
                
                if (interestInZoneRPC.IsInterested) {
                    ghostRelevancy.ValueRW.GhostRelevancySet.TryAdd(relevantGhostForConnection, 1);
                } else {
                    ghostRelevancy.ValueRW.GhostRelevancySet.Remove(relevantGhostForConnection);
                }
            }
            
            ecb.DestroyEntity(entity); 
        }
        
        // TODO: Debug logging the number of Relevant Ghosts
        Debug.Log($"Zone Interest Changed -- Number of Relevant Ghosts: {ghostRelevancy.ValueRO.GhostRelevancySet.Count()}");
    }

}