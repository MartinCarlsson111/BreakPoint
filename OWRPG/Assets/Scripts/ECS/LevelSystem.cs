using Unity;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct Level : IComponentData
{
    public int maxLevel;
    public int currentLevel;
    public int experienceToNextLevel;
}

public struct Experience : IBufferElementData
{
    public int experience;
}

public struct LevelUp : IComponentData
{
    public int levelUps;
}

public class LevelBarrier : BarrierSystem
{

}

public class LevelSystem : JobComponentSystem
{
    [Inject] private LevelBarrier LevelBarrier;

    public struct ExperienceBuffer
    {
        public readonly int Length;
        public BufferArray<Experience> Buffer;
        public ComponentDataArray<Level> Level;
        public EntityArray Entities;
    }

    public struct LevelBuffer
    {
        public readonly int Length;
        public ComponentDataArray<Level> Level;
        public ComponentDataArray<LevelUp> LevelUp;
        public EntityArray Entities;
    }

    [Inject] public LevelBuffer levelBuffer;
    [Inject] public ExperienceBuffer experienceBuffer;


    //[BurstCompile]
    public struct AddExperienceJob : IJobParallelFor
    {
        public BufferArray<Experience> Buffer;
        public ComponentDataArray<Level> Level;

        [NativeDisableParallelForRestriction]
        public EntityCommandBuffer.Concurrent Commands;

        public EntityArray Entities;
        public ComponentType type;

        public void Execute(int i)
        {
            if(Level[i].currentLevel != Level[i].maxLevel)
            {
                var level = Level[i];
                for(int x = 0; x < Buffer[i].Length; x++)
                {
                    level.experienceToNextLevel -= Buffer[i][x].experience;
                    Buffer[i].RemoveAt(x);
                }
                if(level.experienceToNextLevel <= 0)
                {
                    LevelUp levelUp = new LevelUp();
                    while(level.experienceToNextLevel <= 0)
                    {
                        var extra = level.experienceToNextLevel;
                        level.experienceToNextLevel = GetNextExperienceRequired(level.currentLevel, level.maxLevel);
                        level.experienceToNextLevel -= extra;
                        levelUp.levelUps++;
                    }
                    Commands.AddComponent(i, Entities[i], levelUp);
                }
                Level[i] = level;
            }
        }

        public int GetNextExperienceRequired(int level, int maxLevel)
        {
            var multiplier = 5;
            var k = 100;

            return multiplier * (k * (level / maxLevel));
        }
    }

    [BurstCompile]
    public struct LevelUpJob : IJobProcessComponentDataWithEntity<Level, LevelUp>
    {        
        [NativeDisableParallelForRestriction]
        public EntityCommandBuffer Commands;
        public ComponentType LevelUpType;

        public void Execute(Entity e, int i, ref Level level, ref LevelUp levelUp)
        {
            level.currentLevel += levelUp.levelUps;
            if(level.currentLevel > level.maxLevel)
            {
                level.currentLevel = level.maxLevel;
            }
            Commands.RemoveComponent(e, LevelUpType);
        }
    }



    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var levelJob = new LevelUpJob
        {
            LevelUpType = typeof(LevelUp),
            Commands = LevelBarrier.CreateCommandBuffer(),
        }.ScheduleSingle(this, inputDeps);
        levelJob.Complete();

        return new AddExperienceJob
        {
            Entities = experienceBuffer.Entities,
            Buffer = experienceBuffer.Buffer,
            Level = experienceBuffer.Level,
            Commands = LevelBarrier.CreateCommandBuffer().ToConcurrent(),
        }.Schedule(experienceBuffer.Length, 32, levelJob);
    }
}
