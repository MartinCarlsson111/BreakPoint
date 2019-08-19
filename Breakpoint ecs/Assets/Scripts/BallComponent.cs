using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public struct BallComponent : IComponentData
{
    public float2 dir;
    public float speed;
}