using Unity;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;


public enum BuffEffectorTypes
{
    Health,
    HealthPercent,
};

public struct BuffStack : IBufferElementData
{
    public int nameIndex;
    public int fromEntityId;
    public BuffEffectorTypes type;
    public int value;
    public float durationLeft;
}

public struct BuffEffect : IBufferElementData
{ 
    public int nameIndex;
    public int fromEntityId;
    public BuffEffectorTypes type;
    public int value;
    public float durationLeft;
}

public class BuffSystem : JobComponentSystem
{
    public struct EffectBufferData
    {
        public readonly int Length;
       // public BufferArray<HealthEffect> Buffers;
       // public ComponentDataArray<Health> Health;
    }

    [Inject] EffectBufferData m_Data;

    [BurstCompile]
    public struct AddBuffJob : IJobParallelFor
    {
        public float deltaTime;
        public void Execute(int i)
        {
           
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return new AddBuffJob {deltaTime = Time.deltaTime }.Schedule(m_Data.Length, 32, inputDeps);
    }
}