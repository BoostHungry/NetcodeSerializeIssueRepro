using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct SetHasGhostIdSystem : ISystem {

    [BurstCompile]
    public void OnCreate(ref SystemState state) {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<GhostInstance>().WithNone<HasGhostId>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
        state.RequireForUpdate<GhostRelevancy>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // Build a map of players to their zones of interest
        NativeHashMap<int, NativeHashSet<int>> playerToZonesMap = new(12, Allocator.Temp);
        foreach (var (playerGhostOwner, playerZoneInterests) 
                 in SystemAPI.Query<GhostOwner, DynamicBuffer<PlayerZoneInterestBufferComp>>()) {
            NativeHashSet<int> playerZoneInterestsSet = new(32, Allocator.Temp);

            foreach (PlayerZoneInterestBufferComp playerZoneInterest in playerZoneInterests) {
                playerZoneInterestsSet.Add(playerZoneInterest);
            }
            
            playerToZonesMap.Add(playerGhostOwner.NetworkId, playerZoneInterestsSet);
        }

        // Check all GhostComponents to see if they're fully initialized.  If they are, mark them and check for relevance needs.
        RefRW<GhostRelevancy> ghostRelevancy = SystemAPI.GetSingletonRW<GhostRelevancy>();
        foreach (var (ghostComponent, entity) in SystemAPI.Query<GhostInstance>().WithNone<HasGhostId>().WithEntityAccess()) {
            if (ghostComponent.ghostId > 0) {
                // Check if any players should gain relevancy of this new entity
                if (state.EntityManager.HasComponent<ZoneComp>(entity)) {
                    int zoneId = state.EntityManager.GetSharedComponent<ZoneComp>(entity).Value;
                    foreach (KVPair<int, NativeHashSet<int>> entry in playerToZonesMap) {
                        if (entry.Value.Contains(zoneId)) {
                            RelevantGhostForConnection relevantGhostForConnection = new(entry.Key, ghostComponent.ghostId);
                            ghostRelevancy.ValueRW.GhostRelevancySet.TryAdd(relevantGhostForConnection, 1);
                        }
                    }
                } else if (state.EntityManager.HasComponent<PlayerCursorComp>(entity)) {
                    Debug.Log($"New Player Cursor Found");
                    // New Player Cursors should be relevant to everyone
                    foreach (int playerNetworkId in playerToZonesMap.GetKeyArray(Allocator.Temp)) {
                        RelevantGhostForConnection relevantGhostForConnection = new(playerNetworkId, ghostComponent.ghostId);
                        ghostRelevancy.ValueRW.GhostRelevancySet.TryAdd(relevantGhostForConnection, 1);
                        Debug.Log($"Setting New Cursor Relevant for a Player");
                    }

                    // All previously existing player cursors should be made relevant to this new player
                    int newPlayerNetworkId = state.EntityManager.GetComponentData<GhostOwner>(entity).NetworkId;
                    foreach (var existingPlayerCursorGhostInstance
                             in SystemAPI.Query<GhostInstance>().WithAll<HasGhostId, PlayerCursorComp>()) {
                        
                        Debug.Log($"Setting Existing Cursor Relevant for New Player");
                        RelevantGhostForConnection relevantGhostForConnection = new(newPlayerNetworkId, existingPlayerCursorGhostInstance.ghostId);
                        ghostRelevancy.ValueRW.GhostRelevancySet.TryAdd(relevantGhostForConnection, 1);
                        
                    }
                }
                
                // Mark this entity has having a Ghost Id now
                ecb.AddComponent<HasGhostId>(entity);
            }
        }

        // Dispose the Native Containers
        foreach (KVPair<int, NativeHashSet<int>> entry in playerToZonesMap) {
            entry.Value.Dispose();
        }
        playerToZonesMap.Dispose();
    }
}