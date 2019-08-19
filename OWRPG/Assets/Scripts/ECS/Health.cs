using Unity;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

using UnityEngine;
public struct Health : IComponentData
{
    public int maxHP;
    public int currentHealth;
    public int lastChangeSource;
}

public enum DamageEffectTypes
{
    DirectDamage,
    DamageOverTime,
    DirectHealing,
    HealingOverTime
};

public struct HealthEffect : IBufferElementData
{ 
    public int nameIndex;
    public int fromEntityId;
    public DamageEffectTypes type;
    public int value;
    public float interval;
    public float durationLeft;
    public float time;
}

public class HealthSystem : JobComponentSystem
{
    public struct EffectBufferData
    {
        public readonly int Length;
        public BufferArray<HealthEffect> Buffers;
        public ComponentDataArray<Health> Health;
    }

    [Inject] EffectBufferData m_Data;

    [BurstCompile]
    public struct CurrentHealthChangeJob : IJobParallelFor
    {
        public BufferArray<HealthEffect> Buffers;
        public ComponentDataArray<Health> Health;
        public float deltaTime;
        public void Execute(int i)
        {
            int count = Buffers[i].Length;
            for(int c = 0; c < count; c++)
            {
                if(Buffers[i][c].type == DamageEffectTypes.DirectDamage || Buffers[i][c].type == DamageEffectTypes.DirectHealing)
                {
                    Health health = new Health();
                    health.currentHealth = Health[i].currentHealth;
                    if(Buffers[i][c].type == DamageEffectTypes.DirectHealing)
                    {
                        health.currentHealth += Buffers[i][c].value;

                        if(health.currentHealth > health.maxHP)
                        {
                            health.currentHealth = health.maxHP;
                        }
                    }
                    else
                    {
                        health.currentHealth -= Buffers[i][c].value;
                        if(health.currentHealth < 0)
                        {
                            health.currentHealth = 0;
                        }
                        else
                        {
                            health.lastChangeSource = Buffers[i][c].fromEntityId;
                        }
                    }
                    Health[i] = health;
                    Buffers[i].RemoveAt(c);
                }

                if(Buffers[i][c].type == DamageEffectTypes.DamageOverTime || Buffers[i][c].type == DamageEffectTypes.HealingOverTime)
                {
                    HealthEffect newEffect = new HealthEffect();
                    newEffect = Buffers[i][c];

                    if(Buffers[i][c].time > Buffers[i][c].interval)
                    {
                        Health health = Health[i];
                        if(Buffers[i][c].type == DamageEffectTypes.HealingOverTime)
                        {
                            health.currentHealth += Buffers[i][c].value;
                            if(health.currentHealth > health.maxHP)
                            {
                                health.currentHealth = health.maxHP;
                            }
                        }
                        else
                        {
                            health.currentHealth -= Buffers[i][c].value;
                            if(health.currentHealth < 0)
                            {
                                health.currentHealth = 0;
                            }
                            else
                            {
                                health.lastChangeSource = Buffers[i][c].fromEntityId;
                            }
                        }
                        Health[i] = health;
                        newEffect.time = Buffers[i][c].time - Buffers[i][c].interval;
                    }

                    if(Buffers[i][c].durationLeft <= 0)
                    {
                        Buffers[i].RemoveAt(c);
                        return;
                    }
                    else
                    {
                        newEffect.time += deltaTime;
                        newEffect.durationLeft -= deltaTime;
                        Buffers[i].Add(newEffect);
                        Buffers[i].RemoveAt(c);
                    }
                }
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return new CurrentHealthChangeJob { Buffers = m_Data.Buffers, Health = m_Data.Health, deltaTime = Time.deltaTime }.Schedule(m_Data.Length, 32, inputDeps);
    }
}