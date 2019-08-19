using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

//TODO: Run this after collision system
public class BallMovementSystem : JobComponentSystem {
    struct MovementJob : IJobForEach<BallComponent, Translation> {
        [ReadOnly] public float dt;
        public void Execute (ref BallComponent c0, ref Translation c1) {
            c1.Value.xy += c0.dir * c0.speed * dt;
        }
    }

    protected override JobHandle OnUpdate (JobHandle inputDeps) {
        var movementJob = new MovementJob () {
            dt = Time.deltaTime
        };

        return movementJob.Schedule (this, inputDeps);
    }

    //collision response job

}