using Unity;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

//Shape
public struct Shape : IComponentData
{
    public int id;
}

//ShapeData
public struct ShapeData : IBufferElementData
{
    public float3 vertice;
}