using Unity;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


public struct EntityTag : IComponentData {
    public int id;
    public uint layer;
}
