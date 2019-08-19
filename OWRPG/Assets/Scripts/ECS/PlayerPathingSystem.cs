using Unity;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct MovementSpeed : IComponentData
{
    public float value;
}

public struct Velocity : IComponentData
{
    public float3 Value;
}

public struct SleepingStatus : IComponentData
{
    public int Value;
}

public struct Gravity : IComponentData
{
    public float jumping;
}



public class PlayerPathingSystem : JobComponentSystem {

    public struct MoveGenData
    {
        public readonly int Length;
        public ComponentDataArray<Player> Inputs;
        public ComponentDataArray<NavAgent> NavAgent;
    }

    public struct PlayerMovementData
    {
        public readonly int Length;
        public ComponentDataArray<MovementSpeed> MovementSpeed;
        public ComponentDataArray<Rotation> Rotation;
        public ComponentDataArray<Velocity> Velocity;
        public ComponentDataArray<Player> Inputs;
        public ComponentDataArray<SleepingStatus> Sleeping;
        public ComponentDataArray<Gravity> Gravity;
    }

    public struct MovementData
    {
        public readonly int Length;
        public ComponentDataArray<Rotation> Rotation;
        public ComponentDataArray<Position> Position;
        public ComponentDataArray<Velocity> Velocity;
        public ComponentDataArray<SleepingStatus> Sleeping;
        public ComponentDataArray<Gravity> Gravity;
    }

    [Inject] MovementData movementData;
    [Inject] MoveGenData data;
    [Inject] PlayerMovementData freeMovementData;

    public int slopeLimit = 55;
    public float yOffset = -0.4f;
    public float stepLimit = 0.45f;
    public float JumpHeight = 4.0f;

    [BurstCompile]
    public struct NavAgentPathingJob : IJobParallelFor
    {
        public ComponentDataArray<Player> Inputs;
        public ComponentDataArray<NavAgent> NavAgent;

        public void Execute(int i)
        {
            if(Inputs[i].move.x != 0 || Inputs[i].move.y != 0 || Inputs[i].move.z != 0)
            {
                NavAgent agent = new NavAgent();
                agent = NavAgent[i];

                agent.destination = Inputs[i].move;
                agent.newDestination = 1;

                Player input = new Player();
                input = Inputs[i];
                input.move = float3.zero;

                NavAgent[i] = agent;
                Inputs[i] = input;
            }
        }
    }

    [BurstCompile]
    public struct PlayerInputJob : IJobParallelFor
    {
        public ComponentDataArray<MovementSpeed> MovementSpeed;
        public ComponentDataArray<Velocity> Velocity;
        public ComponentDataArray<Rotation> Rotation;
        public ComponentDataArray<Player> Inputs;
        public ComponentDataArray<SleepingStatus> Grounded;
        public ComponentDataArray<Gravity> Gravity;
        public float dt;
        public float jumpHeight;

        public void Execute(int i)
        {
            if(Inputs[i].move.x != 0 || Inputs[i].move.z != 0 || Inputs[i].jump != 0)
            {
                Player input = new Player();
                input = Inputs[i];

                var velocity = new Velocity();

                velocity = Velocity[i];
                velocity.Value += math.mul(Rotation[i].Value, input.move) * 0.01f * dt * MovementSpeed[i].value;

                if(input.jump == 1 && Grounded[i].Value == 1)
                {
                    var gvty = new Gravity();
                    gvty.jumping = jumpHeight;
                    Gravity[i] = gvty;
                }

                Velocity[i] = velocity;
                Inputs[i] = input;
            }
        }
    }

    //[BurstCompile]
    struct GravityJob : IJobParallelFor
    {
        [ReadOnly] public float dt;
        [ReadOnly] public float3 Gravity;

        public ComponentDataArray<Velocity> Velocity;
        [ReadOnly] public ComponentDataArray<SleepingStatus> Sleeping;
        public ComponentDataArray<Gravity> GravityComp;
        public void Execute(int i)
        {
            if(GravityComp[i].jumping > 0)
            {
                Velocity v = Velocity[i];
                v.Value -= Gravity * dt;
                Velocity[i] = v;

                var gvty = new Gravity();
                gvty = GravityComp[i];
                gvty.jumping += Gravity.y * dt;
                GravityComp[i] = gvty;
            }
            else if(Sleeping[i].Value == 0)
            {
                Velocity v = Velocity[i];
                v.Value += Gravity * dt;
                Velocity[i] = v;
            }
        }
    }

    struct PrepareGroundedRaycastCommands : IJobParallelFor
    {
        public NativeArray<RaycastCommand> Raycasts;
        [ReadOnly] public ComponentDataArray<Position> Position;

        public void Execute(int i)
        {
            float magnitude = 0.5f;
            Raycasts[i] = new RaycastCommand(Position[i].Value, new float3(0, -1, 0), magnitude);
        }
    }


    //[BurstCompile]
    struct GroundCheckJob : IJobParallelFor
    {
        public NativeArray<RaycastHit> Hits;
        public ComponentDataArray<Velocity> Velocity;
        public ComponentDataArray<SleepingStatus> Sleeping;
        public ComponentDataArray<Position> Position;
        public ComponentDataArray<Gravity> Gravity;

        public void Execute(int i)
        {
            if(Hits[i].normal != Vector3.zero && Sleeping[i].Value == 0)
            {
                if(Gravity[i].jumping <= 0)
                {
                    var velocity = Velocity[i];
                    velocity.Value.y = 0;
                    Velocity[i] = velocity;
                    Sleeping[i] = new SleepingStatus { Value = 1 };
                }
            }
            else
            {
                Sleeping[i] = new SleepingStatus { Value = 0 };
            }
        }
    }

    struct PrepareRaycastCommands : IJobParallelFor
    {
        public NativeArray<RaycastCommand> Raycasts;
        public NativeArray<RaycastCommand> SlopeRaycasts;
        [ReadOnly] public ComponentDataArray<Velocity> Velocity;
        [ReadOnly] public ComponentDataArray<Position> Position;
        [ReadOnly] public ComponentDataArray<Rotation> Rotation;
        [ReadOnly] public float stepHeight;
        [ReadOnly] public float yOffset;
        public void Execute(int i)
        {
            Raycasts[i] = new RaycastCommand(Position[i].Value + new float3(0, yOffset, 0), Velocity[i].Value, mag(Velocity[i].Value));
            SlopeRaycasts[i] = new RaycastCommand(Position[i].Value + new float3(0, stepHeight + yOffset, 0), Velocity[i].Value, mag(Velocity[i].Value) + 2.0f);
        }

        float mag(float3 v)
        {
            return math.sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        }
    }

    struct CalculateCollisionResponse : IJobParallelFor
    {
        public NativeArray<RaycastHit> Hits;
        public NativeArray<RaycastHit> SlopeHits;
        public NativeArray<RaycastHit> GroundHits;
        public ComponentDataArray<Velocity> Velocity;
        public ComponentDataArray<SleepingStatus> Sleeping;
        public ComponentDataArray<Position> Position;
        [ReadOnly] public float yOffset;
        [ReadOnly] public float stepHeight;
        [ReadOnly] public float slopeLimit;
        [ReadOnly] public float dt;
        public void Execute(int i)
        {
            //stabilization
            /*
            if(magnitude <= 2f)
            {
                var velocity = Velocity[i];
                velocity.Value = float3.zero;
                Velocity[i] = velocity;

               var sleeping = Sleeping[i];
                sleeping.Value = 1;
                Sleeping[i] = sleeping;
            }*/

            var velocity = Velocity[i];
            if(Hits[i].normal.x != 0 || Hits[i].normal.z != 0)
            {
                if(SlopeHits[i].normal != Vector3.zero)
                {
                    if(Hits[i].normal == SlopeHits[i].normal)
                    {
                        var hitPoint = Hits[i].point;
                        var stepPoint = SlopeHits[i].point;

                        var slopeVector = math.normalize(hitPoint - stepPoint);
                        if(math.abs(slopeVector.y) < (slopeLimit / 90))
                        {
                            float slopeAngle = Vector3.Angle(math.normalize(SlopeHits[i].normal), Vector3.up);
                            var slopeLerp = slopeAngle / 180f;
                            velocity = Velocity[i];
                            if(GroundHits[i].point.y >= Position[i].Value.y-slopeVector.y)
                            {
                            }
                            else
                            {
                                velocity.Value.y = -slopeVector.y * dt * 10 * Mathf.Lerp(0.5f, 1f, slopeLerp);
                            }
                            Velocity[i] = velocity;
                        }
                        else
                        {
                            var hitNormal = math.normalize(new float3(Hits[i].normal.x, 0, Hits[i].normal.z));
                            velocity.Value = math.reflect(Velocity[i].Value, hitNormal);
                            float angle = Vector3.Angle(hitNormal, Vector3.up);
                            var lerp = angle / 180f;
                            velocity.Value = velocity.Value * Mathf.Lerp(0.5f, 1f, lerp);
                            Velocity[i] = velocity;
                        }

                    }
                    else
                    {
                        if(Hits[i].normal.y == 0)
                        {
                            Debug.Log(Hits[i].normal);
                            velocity = Velocity[i];
                            velocity.Value.y = 0.5f;
                            Velocity[i] = velocity;
                        }
                        else
                        {
                            Debug.Log(Hits[i].normal);
                            var hitNormal = math.normalize(new float3(Hits[i].normal.x, 0, Hits[i].normal.z));
                            velocity.Value = math.reflect(Velocity[i].Value, hitNormal);
                            float angle = Vector3.Angle(hitNormal, Vector3.up);
                            var lerp = angle / 180f;
                            velocity.Value = velocity.Value * Mathf.Lerp(0.5f, 1f, lerp);
                            Velocity[i] = velocity;
                        }
                    }
                }
                else
                {

                    //deal with wallsurfing

                    Debug.Log("HELLO");
                    Debug.Log(Hits[i].normal);
                    var hitNormal = math.normalize(new float3(Hits[i].normal.x, 0, Hits[i].normal.z));
                    velocity.Value = math.reflect(Velocity[i].Value, hitNormal);
                    float angle = Vector3.Angle(hitNormal, Vector3.up);
                    var lerp = angle / 180f;
                    velocity.Value = velocity.Value * Mathf.Lerp(0.5f, 1f, lerp);
                    Velocity[i] = velocity;
                }
            }
        }


        float dot(float3 a, float3 b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }

        float mag(float3 v)
        {
            return math.sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        }
    }

    //[BurstCompile]
    struct IntegrateMovement : IJobParallelFor
    {
        public ComponentDataArray<Velocity> Velocity;
        public ComponentDataArray<Position> Position;

        public void Execute(int i)
        {

            var newPos = Position[i];

            newPos.Value += Velocity[i].Value;

            Position[i] = newPos;

            Velocity[i] = new Velocity { Value = float3.zero };
        }
    }


    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if(freeMovementData.Length > 0)
        {
            var raycastCommands = new NativeArray<RaycastCommand>(freeMovementData.Length, Allocator.TempJob);
            var raycastHits = new NativeArray<RaycastHit>(freeMovementData.Length, Allocator.TempJob);
            var groundRaycastCommands = new NativeArray<RaycastCommand>(freeMovementData.Length, Allocator.TempJob);
            var groundRaycastHits = new NativeArray<RaycastHit>(freeMovementData.Length, Allocator.TempJob);
            var slopeRaycastCommands = new NativeArray<RaycastCommand>(freeMovementData.Length, Allocator.TempJob);
            var slopeRaycastHits = new NativeArray<RaycastHit>(freeMovementData.Length, Allocator.TempJob);
            var stepRaycastCommands = new NativeArray<RaycastCommand>(freeMovementData.Length, Allocator.TempJob);
            var stepRaycastHits = new NativeArray<RaycastHit>(freeMovementData.Length, Allocator.TempJob);


            var gravityJob = new GravityJob
            {
                dt = Time.deltaTime,
                Velocity = movementData.Velocity,
                Sleeping = movementData.Sleeping,
                Gravity = new float3(0, -9.8f, 0),
                GravityComp = movementData.Gravity
            }.Schedule(movementData.Length, 32, inputDeps);
            gravityJob.Complete();

            var groundRayCasts = new PrepareGroundedRaycastCommands
            {
                Raycasts = groundRaycastCommands,
                Position = movementData.Position
            }.Schedule(movementData.Length, 32, gravityJob);

            groundRayCasts.Complete();
            var groundRaycastDependency = RaycastCommand.ScheduleBatch(groundRaycastCommands, groundRaycastHits, 32, groundRayCasts);

            var groundCheck = new GroundCheckJob
            {
                Position = movementData.Position,
                Hits = groundRaycastHits,
                Sleeping = movementData.Sleeping,
                Velocity = movementData.Velocity,
                 Gravity = movementData.Gravity
            }.Schedule(movementData.Length, 32, groundRaycastDependency);

            groundCheck.Complete();

            var playerInputJob = new PlayerInputJob
            {
                Inputs = freeMovementData.Inputs,
                MovementSpeed = freeMovementData.MovementSpeed,
                Velocity = freeMovementData.Velocity,
                Rotation = freeMovementData.Rotation,
                dt = Time.deltaTime,
                Grounded = freeMovementData.Sleeping,
                jumpHeight = JumpHeight,
                Gravity = freeMovementData.Gravity
            }.Schedule(freeMovementData.Length, 32, inputDeps);

            playerInputJob.Complete();

            var rayCastNavigation = new PrepareRaycastCommands
            {
                Velocity = movementData.Velocity,
                Position = movementData.Position,
                Raycasts = raycastCommands,
                SlopeRaycasts = slopeRaycastCommands,
                Rotation = movementData.Rotation,
                stepHeight = stepLimit,
                yOffset = yOffset
            }.Schedule(movementData.Length, 32, playerInputJob);

            rayCastNavigation.Complete();
            var raycastDependency = RaycastCommand.ScheduleBatch(raycastCommands, raycastHits, 32, rayCastNavigation);
            var slopeDependency = RaycastCommand.ScheduleBatch(slopeRaycastCommands, slopeRaycastHits, 32, raycastDependency);

            var collisionResponse = new CalculateCollisionResponse
            {
                Hits = raycastHits,
                SlopeHits = slopeRaycastHits,
                Velocity = movementData.Velocity,
                Sleeping = movementData.Sleeping,
                GroundHits = groundRaycastHits,
                stepHeight = stepLimit,
                yOffset = yOffset,
                dt = Time.deltaTime,
                slopeLimit = slopeLimit,
                Position = movementData.Position
            }.Schedule(movementData.Length, 32, slopeDependency);

            collisionResponse.Complete();

            var integrateMovement = new IntegrateMovement
            {
                Velocity = movementData.Velocity,
                Position = movementData.Position
            }.Schedule(movementData.Length, 32, collisionResponse);

            raycastCommands.Dispose();
            groundRaycastCommands.Dispose();
            slopeRaycastCommands.Dispose();
            stepRaycastCommands.Dispose();

            raycastHits.Dispose();
            slopeRaycastHits.Dispose();
            stepRaycastHits.Dispose();
            groundRaycastHits.Dispose();

            return integrateMovement;
        }
        return new NavAgentPathingJob { Inputs = data.Inputs, NavAgent = data.NavAgent }.Schedule(data.Length, 32, inputDeps);
    }

}
