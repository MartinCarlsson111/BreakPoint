using Unity;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct NavAgent : IComponentData {
    public float3 destination;
    public int areaMask;
    public int newDestination;
}

public struct NavAgentPath : IBufferElementData
{
   public float3 waypoint;
}
