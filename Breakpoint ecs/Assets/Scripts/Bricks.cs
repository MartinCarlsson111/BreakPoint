using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
public struct Bricks : IComponentData
{
    public int state;
    public int value;
}
