using Unity;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

public struct CollisionEvent : IBufferElementData
{
    public float3 point;
    public int entityID;
    public uint otherLayer;
}
