using Unity;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct SpellEffectZone : IComponentData
{
    public int damage;
    public int slowPercent;
    public float interval;
    public int fromEntity;
    public float currentTime;
    public float duration;
    public DamageEffectTypes type;

    //TODO: Implement factions matrix
    public int faction;
}

public class SpellEffectZoneSystem : JobComponentSystem
{
    public struct EffectBufferData
    {
        public BufferArray<HealthEffect> buffer;
        public EntityArray entities;
    }

    public struct SpellEffectZoneData
    {
        public readonly int Length;
        public ComponentDataArray<SpellEffectZone> zones;
        public BufferArray<CollisionEvent> Collisions;
    }

    [Inject] SpellEffectZoneData zoneData;
    [Inject] EffectBufferData characters;

    [BurstCompile]
    public struct SpellEffectZoneJob : IJobParallelFor
    {
        public ComponentDataArray<SpellEffectZone> zones;
        public BufferArray<CollisionEvent> zoneCollisions;
        
        public EntityArray Entities;
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<HealthEffect> Health;

        public float deltaTime;

        public void Execute(int i)
        {
            var zone = zones[i];
            zone.currentTime += deltaTime;
            if(zone.currentTime >= zone.interval)
            {
                zone.currentTime -= zone.interval;
                for(int x = 0; x < zoneCollisions[i].Length; x++)
                {
                    //TODO
                    //TODO For some reason, the entity array is not sorted
                    //TODO
                    //TODO: Pref to remove this in the future
                    int f = 0;
                    bool found = false;
                    for(; f < Entities.Length; f++)
                    {
                        if(Entities[f].Index == zoneCollisions[i][x].entityID)
                        {
                            found = true;
                            break;
                        }
                    }
                    if(Entities.Length > f && found)
                    {
                        Health[Entities[f]].Add(
                        new HealthEffect
                        {
                            value = zones[i].damage,
                            type = DamageEffectTypes.DamageOverTime,
                            interval = zones[i].interval,
                            durationLeft = zones[i].interval,
                            fromEntityId = zones[i].fromEntity,
                            time = 0
                        });
                    }
                }
            }
            zones[i] = zone;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return new SpellEffectZoneJob { 
            Entities = characters.entities,
            Health = GetBufferFromEntity<HealthEffect>(false), 
            zoneCollisions = zoneData.Collisions, 
            deltaTime = Time.deltaTime,
            zones = zoneData.zones }.Schedule(zoneData.Length, 32, inputDeps);
    }

}