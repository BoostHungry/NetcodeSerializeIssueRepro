using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(InitializationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class TreeSpawnerSystem : SystemBase {
    protected override void OnCreate() {
        RequireForUpdate<TerrainProperties>();
    }

    protected override void OnUpdate() {
        // Run Once
        Enabled = false;
        
        
        // TODO: Disbaled
        // return;

        
        // EntityCommandBufferSystem ecbSystem =
        //     World.GetExistingSystemManaged<EndInitializationEntityCommandBufferSystem>();
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        TreeSpawnerComponent treeSpawnerComponent = SystemAPI.GetSingleton<TreeSpawnerComponent>();
        TreeSpawningJob job = new TreeSpawningJob {
            TerrainProps = SystemAPI.GetSingleton<TerrainProperties>(),
            RandomSeed = 12345,
            TreePrefab = treeSpawnerComponent.Prefab,
            SpawnChance = treeSpawnerComponent.SpawnChance,
            // ECB = ecbSystem.CreateCommandBuffer().AsParallelWriter(),
            ECB = ecb.AsParallelWriter(),
        };
        JobHandle jobHandle = job.Schedule(SystemAPI.GetSingleton<TerrainProperties>().GetTotalSize(), 1024);
        jobHandle.Complete();
        
        // ecbSystem.AddJobHandleForProducer(Dependency);
        ecb.Playback(EntityManager);
        
        Debug.Log("~~~ TreeSpawnerSystem Complete ~~~");
        int numTrees = SystemAPI.QueryBuilder().WithAll<TreeComp>().Build().CalculateEntityCount();
        Debug.Log($"Number of Trees: {numTrees}");
    }
}

[BurstCompile]
public struct TreeSpawningJob : IJobParallelFor {

    [ReadOnly] public TerrainProperties TerrainProps;

    [ReadOnly] public uint RandomSeed;
    
    [ReadOnly] public Entity TreePrefab;
    [ReadOnly] public float SpawnChance;
    
    public EntityCommandBuffer.ParallelWriter ECB;

    [BurstCompile]
    public void Execute(int index) {
        float randomValue = Random.CreateFromIndex((uint) (RandomSeed * (index + 1))).NextFloat();
        if (randomValue <= SpawnChance) {
            spawnTree(index);
        }
    }

    private void spawnTree(int index) {
        Entity entity = ECB.Instantiate(index, TreePrefab);
        float3 coords = CoordUtil.GetCoordsFloat3(index, TerrainProps.TerrainDimensions);
        ECB.SetComponent(index, entity, new WorldPosition2D { value = coords.xy });
        ECB.SetComponent(index, entity, LocalTransform.FromPosition(coords));
        ECB.SetSharedComponent(index, entity, new ZoneComp {
            Value = CoordUtil.GetZoneId(index, TerrainProps.TerrainDimensions, TerrainProps.QuadSize)
        });
    }
}