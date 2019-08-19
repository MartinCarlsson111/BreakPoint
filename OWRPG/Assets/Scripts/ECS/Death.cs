using Unity;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;


public struct DeathReward : IComponentData
{

}

public struct DeathBehavior : IComponentData
{

}

public class DeathBarrier : BarrierSystem
{

}

public class DeathSystem : JobComponentSystem
{
    ComponentType[] lootCorpseList =
    {
        typeof(EntityTag),
        typeof(Position),
        typeof(Rotation),
        typeof(Scale),
        typeof(MeshInstanceRenderer),
        typeof(Shape),
        typeof(CollisionEvent),
        typeof(Lootable)
    };

    [Inject] private DeathBarrier deadBarrier;

    public struct DeadData
    {
        public readonly int Length;
        public ComponentDataArray<Health> Health;
        public EntityArray Entities;
    }

    const int BASEEXPERIENCE = 1;
    const int BASEMULTIPLIER = 5;


    [Inject] [ReadOnly] public DeadData deadData;
    [Inject] [ReadOnly] public ComponentDataFromEntity<DeathReward> DeathRewards;
    [Inject] [ReadOnly] public ComponentDataFromEntity<DeathBehavior> DeathBehaviors;
    [Inject] [ReadOnly] public ComponentDataFromEntity<Level> EntityLevels;


    [Inject] public BufferFromEntity<Experience> Experience;

    [BurstCompile]
    struct RemoveDeadJob : IJobParallelFor
    {

        [ReadOnly] public int baseExperience;
        [ReadOnly] public int baseMultiplier;
        [ReadOnly] public ComponentDataArray<Health> Health;
        [ReadOnly] public EntityArray Entities;
        [ReadOnly] public ComponentDataFromEntity<DeathReward> DeathRewards;
        [ReadOnly] public ComponentDataFromEntity<DeathBehavior> DeathBehaviors;

        [NativeDisableParallelForRestriction]
        public BufferFromEntity<Experience> Experience;

        [NativeDisableParallelForRestriction]
        [ReadOnly]public ComponentDataFromEntity<Level> EntityLevels;

        [NativeDisableParallelForRestriction]
        public EntityCommandBuffer Commands;

        public void Execute(int i)
        {
            if(Health[i].currentHealth <= 0)
            {
                //Change this to a buffer array on the player entity
                //When the entity dies, add 
                if(DeathRewards.Exists(Entities[i]))
                {
                    int x = 0;
                    for(; x < Entities.Length; x++)
                    {
                        if(Entities[x].Index == Health[i].lastChangeSource)
                        {
                            break;
                        }
                    }
                    if(Experience.Exists(Entities[x]))
                    {
                        var successA = EntityLevels.Exists(Entities[i]);
                        var successB = EntityLevels.Exists(Entities[x]);
                        if(successA && successB)
                        {
                            var killerLevel = EntityLevels[Entities[i]];
                            var thisLevel = EntityLevels[Entities[x]];
                            Experience[Entities[x]].Add(new Experience { experience = CalculateExperience(thisLevel.currentLevel, killerLevel.currentLevel)});
                        }
                    }
                }
                
                if(DeathBehaviors.Exists(Entities[i]))
                {
                    //Spawn spooky ghosts or similar
                }

                Commands.DestroyEntity(Entities[i]);
                //Create corpse entity
                //create entity should be fine, because I can set a list of archetypes
                //Commands.CreateEntity();
            }
        }

        int CalculateExperience(int aLevel, int bLevel)
        {
            return ((math.min(0, (bLevel - aLevel) + 1) * baseMultiplier) + baseExperience);
        }
    }


    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return new RemoveDeadJob
        {
            DeathRewards = DeathRewards,
            DeathBehaviors = DeathBehaviors,
            Entities = deadData.Entities,
            Health = deadData.Health,
            Commands = deadBarrier.CreateCommandBuffer(),
            Experience = Experience,
            EntityLevels = EntityLevels,
            baseExperience = BASEEXPERIENCE,
            baseMultiplier = BASEMULTIPLIER
        }.Schedule(deadData.Length, 32, inputDeps);
    }
}
