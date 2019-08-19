using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
public struct Flipper : IComponentData {
    public quaternion extendedRotation;
    public quaternion oppositeRotation;
    public quaternion originRot;
    public float speed;
    public float time;
}

public class FlipperSystem : JobComponentSystem {
    protected override JobHandle OnUpdate (JobHandle inputDeps) {
        var job = new FlipperJob () {
            dt = Time.deltaTime,
            fireButtonPressed = Input.GetKey (KeyCode.Space)
        };

        return job.Schedule (this, inputDeps);
    }

    [BurstCompile]
    struct FlipperJob : IJobForEach<Flipper, Rotation> {
        public float dt;
        public bool fireButtonPressed;
        public void Execute (ref Flipper c0, ref Rotation c1) {
            if (fireButtonPressed) {
                if (c0.time > 1.0f) c0.time = 1.0f;
                c0.time += dt;
                c1.Value = math.slerp (c0.originRot, c0.extendedRotation, c0.time);
            } else if (c0.time > 0.0f) {
                c0.time -= dt;
                c1.Value = math.inverse (math.slerp (c0.originRot, c0.oppositeRotation, c0.time));
            } else if (c0.time < 0.0f) c0.time = 0.0f;
        }
    }
}

//system
/*
 *  if (isPressed)
 *      time += deltaTime;
 *      if(time > 1.0f) time = 1.0f;
 *   
 *      slerp(originRot, extendedRot, time * speed)
 *  else if ( time > 0.0f)
 *      time -= deltaTime;
 *      slerp(extendedRot, originRot, time * speed);
 * 
 */