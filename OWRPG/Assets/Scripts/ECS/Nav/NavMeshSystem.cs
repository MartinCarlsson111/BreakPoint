using Unity;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class NavMeshPathFinderSystem : ComponentSystem
{
    public struct NavAgentData
    {
        public readonly int Length;
        public ComponentDataArray<NavAgent> NavAgent;
        public ComponentDataArray<Position> Position;
        public BufferArray<NavAgentPath> WayPointBuffer;
    }

    [Inject] NavAgentData data;

    protected override void OnUpdate()
    {
        for(int i = 0; i < data.Length; i++)
        {
            if(data.NavAgent[i].newDestination == 1)
            {
                data.WayPointBuffer[i].Clear();

                NavMeshPath path = new NavMeshPath();
                NavMesh.CalculatePath(data.Position[i].Value, data.NavAgent[i].destination, data.NavAgent[i].areaMask, path);

                for(int pi = 0; pi < path.corners.Length; pi++)
                    data.WayPointBuffer[i].Add(new NavAgentPath { waypoint = path.corners[pi] });

                NavAgent agent = new NavAgent();
                agent = data.NavAgent[i];
                agent.newDestination = 0;
                data.NavAgent[i] = agent;
            }
        }
    }
}


public class NavAgentMovementSystem : JobComponentSystem
{
    public struct NavAgentMovementData
    {
        public readonly int Length;
        public ComponentDataArray<MovementSpeed> MovementSpeed;
        public ComponentDataArray<Rotation> Rotation;
        public ComponentDataArray<Position> Position;
        public BufferArray<NavAgentPath> WayPointBuffer;
    }

    [Inject] NavAgentMovementData data;
    [BurstCompile]
    public struct PathfindingMovementJob : IJobParallelFor
    {
        public ComponentDataArray<MovementSpeed> MovementSpeed;
        public ComponentDataArray<Rotation> Rotation;
        public ComponentDataArray<Position> Position;
        public BufferArray<NavAgentPath> WayPointBuffer;
        public float deltaTime;

        public void Execute(int i)
        {
            if(WayPointBuffer[i].Length == 0)
            {
                return;
            }
            if(math.distance(WayPointBuffer[i][0].waypoint.xz, Position[i].Value.xz) <= 0.3f)
            {
                WayPointBuffer[i].RemoveAt(0);
                return;
            }

            Position pos = new Position();
            pos = Position[i];

            var dir =  WayPointBuffer[i][0].waypoint - Position[i].Value;
            dir = math.normalize(dir);

            pos.Value += dir * MovementSpeed[i].value * 0.01f * deltaTime;

            Rotation rot = new Rotation();
            rot.Value = quaternion.LookRotation(new float3(dir.x, 0, dir.y), new float3(0, 1, 0));
            
            Position[i] = pos;
            Rotation[i] = rot;
        }
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return new PathfindingMovementJob 
        { MovementSpeed = data.MovementSpeed, Rotation = data.Rotation, Position = data.Position, WayPointBuffer = data.WayPointBuffer, deltaTime = Time.deltaTime
        }.Schedule(data.Length, 32, inputDeps);
    }
}