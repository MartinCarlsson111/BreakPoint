using Unity;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

public struct Stats : IComponentData {
    public int strength;
    public int agility;
    public int movementSpeed;
    public int intellect;
    public int spirit;
}
